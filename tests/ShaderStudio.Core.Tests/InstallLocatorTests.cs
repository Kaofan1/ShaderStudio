using System.Text.Json.Nodes;
using ShaderStudio.Core.Services;
using Xunit;

namespace ShaderStudio.Core.Tests;

public sealed class InstallLocatorTests
{
    [Fact]
    public void TryCreate_AcceptsPluginsFolder()
    {
        TestSchema.CreateSettingsFile(out var folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "Shader.addon64"), "x");
            File.WriteAllText(Path.Combine(folder, "dxgi.dll"), "x");
            File.WriteAllText(Path.Combine(folder, "CustomShaders", "0xAAAA0001.cso"), "x");
            File.WriteAllText(Path.Combine(folder, "CustomShaders", "0xAAAA0002.cso"), "x");

            var installation = InstallLocator.TryCreate(folder);

            Assert.NotNull(installation);
            Assert.Equal(folder, installation!.RootDirectory, ignoreCase: true);
            Assert.Equal(2, installation.ShaderFileCount);
            Assert.True(installation.HasReShade);
            Assert.Equal("Shader", installation.AddonName);
            Assert.True(File.Exists(installation.SettingsPath));
        }
        finally
        {
            TestSchema.Cleanup(folder);
        }
    }

    [Fact]
    public void TryCreate_AcceptsCustomShadersFolderAndSettingsFile()
    {
        var settingsPath = TestSchema.CreateSettingsFile(out var folder);
        try
        {
            var fromCustomShaders = InstallLocator.TryCreate(Path.GetDirectoryName(settingsPath));
            var fromFile = InstallLocator.TryCreate(settingsPath);

            Assert.NotNull(fromCustomShaders);
            Assert.NotNull(fromFile);
            Assert.Equal(fromCustomShaders!.RootDirectory, fromFile!.RootDirectory);
        }
        finally
        {
            TestSchema.Cleanup(folder);
        }
    }

    [Fact]
    public void TryCreate_AcceptsParentFolderContainingPlugins()
    {
        TestSchema.CreateSettingsFile(out var appDataFolder);
        try
        {
            var fivemApp = Path.Combine(appDataFolder, "FiveM.app");
            Directory.CreateDirectory(fivemApp);
            Directory.Move(Path.Combine(appDataFolder, "CustomShaders"), Path.Combine(fivemApp, "pluginsTemp"));
            Directory.CreateDirectory(Path.Combine(fivemApp, "plugins"));
            Directory.Move(Path.Combine(fivemApp, "pluginsTemp"), Path.Combine(fivemApp, "plugins", "CustomShaders"));

            var installation = InstallLocator.TryCreate(fivemApp);

            Assert.NotNull(installation);
            Assert.Equal(Path.Combine(fivemApp, "plugins"), installation!.RootDirectory, ignoreCase: true);
            Assert.True(installation.IsFiveM);
        }
        finally
        {
            TestSchema.Cleanup(appDataFolder);
        }
    }

    [Fact]
    public void TryCreate_RejectsFolderWithoutSchema()
    {
        var folder = Path.Combine(Path.GetTempPath(), "shaderstudio-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            Assert.Null(InstallLocator.TryCreate(folder));
            Assert.Null(InstallLocator.TryCreate(string.Empty));
            Assert.Null(InstallLocator.TryCreate(null));
            Assert.Null(InstallLocator.TryCreate(Path.Combine(folder, "does-not-exist")));
        }
        finally
        {
            TestSchema.Cleanup(folder);
        }
    }

    [Fact]
    public void Profile_RoundTripsThroughJson()
    {
        var catalog = TestSchema.LoadCatalog(out var folder);
        try
        {
            catalog.TrySetValue("CloudDensity", JsonValue.Create(4.5));
            catalog.TrySetValue("EnableClouds", JsonValue.Create(false));

            var profile = ProfileSerializer.Create(catalog, "Moj profil");
            var json = ProfileSerializer.Serialize(profile);
            var restored = ProfileSerializer.Deserialize(json);

            Assert.Equal("Moj profil", restored.Name);
            Assert.Equal(AppInfo.Version, restored.AppVersion);
            Assert.Equal(6, restored.Values.Count);
            Assert.Equal(4.5, TestSchema.AsDouble(restored.Values["CloudDensity"]), 3);
            Assert.False(restored.Values["EnableClouds"]!.GetValue<bool>());

            catalog.RestoreDefaults();
            var applied = catalog.ApplyValues(restored.Values);

            Assert.Equal(6, applied);
            Assert.Equal(4.5, TestSchema.AsDouble(catalog.GetValue("CloudDensity")), 3);
        }
        finally
        {
            TestSchema.Cleanup(folder);
        }
    }

    [Fact]
    public void Profile_IgnoresUnknownKeys()
    {
        var catalog = TestSchema.LoadCatalog(out var folder);
        try
        {
            var values = new Dictionary<string, JsonNode?>(StringComparer.OrdinalIgnoreCase)
            {
                ["CloudDensity"] = JsonValue.Create(3.0),
                ["RemovedInNewerVersion"] = JsonValue.Create(true),
            };

            Assert.Equal(1, catalog.ApplyValues(values));
        }
        finally
        {
            TestSchema.Cleanup(folder);
        }
    }

    [Fact]
    public void Profile_RejectsForeignJson()
    {
        Assert.Throws<InvalidDataException>(() => ProfileSerializer.Deserialize("""{"format":"something-else","values":{}}"""));
        Assert.Throws<InvalidDataException>(() => ProfileSerializer.Deserialize("""{"name":"no values"}"""));
        Assert.ThrowsAny<Exception>(() => ProfileSerializer.Deserialize("not json"));
    }
}
