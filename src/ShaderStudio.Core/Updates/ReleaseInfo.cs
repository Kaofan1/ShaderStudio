using System.Text.Json;

namespace ShaderStudio.Core.Updates;

/// <summary>A downloadable file attached to a GitHub release.</summary>
public sealed record ReleaseAsset(string Name, string DownloadUrl, long SizeBytes, string? Sha256)
{
    public bool IsInstaller => Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                               && !Name.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Data of the newest published release.</summary>
public sealed record ReleaseInfo(
    ReleaseVersion Version,
    string TagName,
    string Title,
    string Notes,
    string HtmlUrl,
    IReadOnlyList<ReleaseAsset> Assets)
{
    /// <summary>The executable to download: the first <c>.exe</c> asset, if any.</summary>
    public ReleaseAsset? Installer => Assets.FirstOrDefault(asset => asset.IsInstaller);

    public static ReleaseInfo FromJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var tagName = GetString(root, "tag_name") ?? string.Empty;
        var assets = new List<ReleaseAsset>();

        if (root.TryGetProperty("assets", out var assetArray) && assetArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assetArray.EnumerateArray())
            {
                var name = GetString(asset, "name");
                var url = GetString(asset, "browser_download_url");
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
                {
                    continue;
                }

                assets.Add(new ReleaseAsset(
                    name,
                    url,
                    asset.TryGetProperty("size", out var size) && size.TryGetInt64(out var value) ? value : 0,
                    ParseDigest(GetString(asset, "digest"))));
            }
        }

        return new ReleaseInfo(
            ReleaseVersion.Parse(tagName),
            tagName,
            GetString(root, "name") ?? tagName,
            GetString(root, "body") ?? string.Empty,
            GetString(root, "html_url") ?? string.Empty,
            assets);
    }

    private static string? ParseDigest(string? digest)
    {
        if (string.IsNullOrWhiteSpace(digest))
        {
            return null;
        }

        const string prefix = "sha256:";
        return digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? digest[prefix.Length..].Trim()
            : null;
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
