using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace ShaderStudio.Core.Updates;

/// <summary>Result of an update check.</summary>
public sealed record UpdateCheckResult(bool IsUpdateAvailable, ReleaseInfo? Latest, ReleaseVersion Current);

/// <summary>
/// Talks to the public GitHub Releases API of this project: reads the newest release,
/// compares it with the running version and downloads the installer asset.
/// </summary>
public sealed class GitHubUpdateClient : IDisposable
{
    private static readonly Uri ApiBase = new("https://api.github.com");

    private readonly HttpClient _http;
    private readonly bool _ownsClient;
    private readonly string _owner;
    private readonly string _repository;

    public GitHubUpdateClient(string? owner = null, string? repository = null, HttpClient? httpClient = null)
    {
        _owner = string.IsNullOrWhiteSpace(owner) ? AppInfo.RepositoryOwner : owner;
        _repository = string.IsNullOrWhiteSpace(repository) ? AppInfo.RepositoryName : repository;

        if (httpClient is null)
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _ownsClient = true;
        }
        else
        {
            _http = httpClient;
        }
    }

    public bool IsConfigured =>
        AppInfo.IsUpdateChannelConfigured
        || (!string.IsNullOrWhiteSpace(_owner)
            && !string.Equals(_owner, "OWNER", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(_repository));

    public ReleaseVersion CurrentVersion => ReleaseVersion.Parse(AppInfo.Version);

    /// <summary>
    /// Returns the newest release, or null when the repository has no published release yet.
    /// A GitHub token (env <c>SHADERSTUDIO_GITHUB_TOKEN</c>) is optional but raises the rate limit.
    /// </summary>
    public async Task<ReleaseInfo?> GetLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest($"repos/{_owner}/{_repository}/releases/latest");

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == (HttpStatusCode)429)
        {
            throw new InvalidOperationException(
                "GitHub odrzucił zapytanie (limit API). Spróbuj ponownie później lub ustaw token w SHADERSTUDIO_GITHUB_TOKEN.");
        }

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return ReleaseInfo.FromJson(json);
    }

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var current = CurrentVersion;
        var latest = await GetLatestReleaseAsync(cancellationToken).ConfigureAwait(false);

        if (latest is null)
        {
            return new UpdateCheckResult(false, null, current);
        }

        return new UpdateCheckResult(latest.Version > current, latest, current);
    }

    /// <summary>
    /// Downloads an asset to <paramref name="destinationPath"/> and verifies its SHA-256 when the
    /// release metadata provides one. Returns the verified (or computed) hash.
    /// </summary>
    public async Task<string> DownloadAsync(
        ReleaseAsset asset,
        string destinationPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(asset);

        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, asset.DownloadUrl);
        AddHeaders(request, acceptJson: false);

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? asset.SizeBytes;

        await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
        await using (var target = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            var buffer = new byte[81920];
            long copied = 0;
            int read;

            while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                copied += read;

                if (total > 0)
                {
                    progress?.Report(Math.Clamp((double)copied / total, 0, 1));
                }
            }
        }

        var hash = await ComputeSha256Async(destinationPath, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(asset.Sha256)
            && !string.Equals(hash, asset.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            TryDelete(destinationPath);
            throw new InvalidDataException(
                "Pobrany plik ma inny odcisk SHA-256 niz opublikowana aktualizacja. Aktualizacja przerwana.");
        }

        progress?.Report(1);
        return hash;
    }

    public static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public void Dispose()
    {
        if (_ownsClient)
        {
            _http.Dispose();
        }
    }

    private HttpRequestMessage CreateRequest(string relativePath)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(ApiBase, relativePath));
        AddHeaders(request, acceptJson: true);
        return request;
    }

    private void AddHeaders(HttpRequestMessage request, bool acceptJson)
    {
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("ShaderStudio", SanitizeVersion(AppInfo.Version)));
        request.Headers.Accept.ParseAdd(acceptJson ? "application/vnd.github+json" : "application/octet-stream");

        var token = Environment.GetEnvironmentVariable("SHADERSTUDIO_GITHUB_TOKEN");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
        }
    }

    private static string SanitizeVersion(string version)
    {
        var cleaned = new string(version.Where(c => char.IsLetterOrDigit(c) || c == '.').ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "1.0.0" : cleaned;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Ignored: a leftover temp file is harmless.
        }
    }
}
