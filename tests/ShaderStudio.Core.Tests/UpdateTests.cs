using ShaderStudio.Core.Updates;
using Xunit;

namespace ShaderStudio.Core.Tests;

public sealed class UpdateTests
{
    [Theory]
    [InlineData("v1.2.3", 1, 2, 3)]
    [InlineData("1.2.3", 1, 2, 3)]
    [InlineData("v2.0", 2, 0, 0)]
    [InlineData("1", 1, 0, 0)]
    [InlineData("v1.2.3-beta.1+build5", 1, 2, 3)]
    [InlineData("", 0, 0, 0)]
    [InlineData("nonsense", 0, 0, 0)]
    public void Parse_HandlesTagsAndSuffixes(string tag, int major, int minor, int patch)
    {
        var version = ReleaseVersion.Parse(tag);

        Assert.Equal(new ReleaseVersion(major, minor, patch), version);
        Assert.Equal($"{major}.{minor}.{patch}", version.ToString());
    }

    [Fact]
    public void Comparison_OrdersVersions()
    {
        Assert.True(ReleaseVersion.Parse("1.2.4") > ReleaseVersion.Parse("1.2.3"));
        Assert.True(ReleaseVersion.Parse("1.3.0") > ReleaseVersion.Parse("1.2.9"));
        Assert.True(ReleaseVersion.Parse("2.0.0") > ReleaseVersion.Parse("1.99.99"));
        Assert.False(ReleaseVersion.Parse("1.0.0") > ReleaseVersion.Parse("1.0.0"));
        Assert.True(ReleaseVersion.Parse("1.10.0") > ReleaseVersion.Parse("1.9.0"));
        Assert.True(ReleaseVersion.Parse("v1.0.0") >= ReleaseVersion.Parse("1.0.0"));
    }

    [Fact]
    public void FromJson_ReadsReleaseWithAssetDigest()
    {
        const string json = """
        {
          "tag_name": "v1.4.0",
          "name": "Shader Studio 1.4.0",
          "body": "Nowe zakladki i poprawki.",
          "html_url": "https://github.com/owner/repo/releases/tag/v1.4.0",
          "assets": [
            { "name": "SHA256SUMS.txt", "browser_download_url": "https://example.com/sums", "size": 90 },
            { "name": "ShaderStudio.exe", "browser_download_url": "https://example.com/app.exe", "size": 42111234, "digest": "sha256:ABC123" }
          ]
        }
        """;

        var release = ReleaseInfo.FromJson(json);

        Assert.Equal(new ReleaseVersion(1, 4, 0), release.Version);
        Assert.Equal("Shader Studio 1.4.0", release.Title);
        Assert.Equal(2, release.Assets.Count);

        var installer = release.Installer;
        Assert.NotNull(installer);
        Assert.Equal("ShaderStudio.exe", installer!.Name);
        Assert.Equal(42111234, installer.SizeBytes);
        Assert.Equal("ABC123", installer.Sha256);
    }

    [Fact]
    public void FromJson_WithoutAssetsOrDigest_IsTolerated()
    {
        var release = ReleaseInfo.FromJson("""{"tag_name":"v0.1.0"}""");

        Assert.Equal(new ReleaseVersion(0, 1, 0), release.Version);
        Assert.Empty(release.Assets);
        Assert.Null(release.Installer);
    }

    [Fact]
    public void StagedExecutablePath_IsVersionedInsideAppData()
    {
        var path = UpdateInstaller.GetStagedExecutablePath(new ReleaseVersion(2, 1, 0));

        Assert.EndsWith("ShaderStudio-2.1.0.exe", path, StringComparison.Ordinal);
        Assert.StartsWith(UpdateInstaller.StagingDirectory, path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AppInfo_UpdateChannelIsConfiguredForThisRepository()
    {
        // Regression guard: the "not configured" check must not compare the owner with itself
        // once a real repository is filled in, or updates would silently never run.
        Assert.True(AppInfo.IsUpdateChannelConfigured);
        Assert.Equal("https://github.com/Kaofan1/ShaderStudio", AppInfo.RepositoryUrl);
        Assert.False(string.IsNullOrWhiteSpace(AppInfo.Version));
    }

    [Fact]
    public void AppState_FreshInstanceChecksForUpdatesByDefault()
    {
        // A fresh instance is used on purpose: AppState.Load() reads the real user profile,
        // which would make this test depend on the machine it runs on.
        var state = new AppState();

        Assert.True(state.CheckForUpdatesOnStartup);
        Assert.Null(state.LastInstallPath);
        Assert.Null(state.SkippedVersion);
    }
}
