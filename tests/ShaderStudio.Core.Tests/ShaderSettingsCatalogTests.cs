using System.Text.Json.Nodes;
using ShaderStudio.Core.Models;
using ShaderStudio.Core.Services;
using Xunit;

namespace ShaderStudio.Core.Tests;

public sealed class ShaderSettingsCatalogTests
{
    [Fact]
    public void Load_ReadsTabsGroupsAndSettings()
    {
        var catalog = TestSchema.LoadCatalog(out var folder);
        try
        {
            Assert.Equal(new[] { "World", "Options" }, catalog.Tabs);
            Assert.Single(catalog.Groups);
            Assert.Equal(3, catalog.ShaderHashCount);
            Assert.Equal(6, catalog.Settings.Count);
            Assert.Single(catalog.Links);
            Assert.False(catalog.IsDirty);
        }
        finally
        {
            TestSchema.Cleanup(folder);
        }
    }

    [Fact]
    public void Load_MapsSettingTypesAndMetadata()
    {
        var catalog = TestSchema.LoadCatalog(out var folder);
        try
        {
            var density = catalog.Settings.Single(s => s.Key == "CloudDensity");
            Assert.Equal(SettingType.Slider, density.Type);
            Assert.Equal(0.0, density.Min);
            Assert.Equal(10.0, density.Max);
            Assert.Equal("%.2f", density.Format);
            Assert.Equal("Sky", density.Section);

            var toneMap = catalog.Settings.Single(s => s.Key == "ToneMap");
            Assert.Equal(SettingType.Int, toneMap.Type);
            Assert.True(toneMap.HdrOnly);
            Assert.Equal(new[] { "Vanilla", "RenoDRT" }, toneMap.Labels);

            var lamp = catalog.Settings.Single(s => s.Key == "Lamp");
            Assert.Equal(SettingType.Color, lamp.Type);

            Assert.True(catalog.Settings.Single(s => s.Key == "Secret").Hidden);
        }
        finally
        {
            TestSchema.Cleanup(folder);
        }
    }

    [Fact]
    public void Settings_WithoutTab_FallBackToFirstTab()
    {
        var catalog = TestSchema.LoadCatalog(out var folder);
        try
        {
            Assert.Equal("World", catalog.Settings.Single(s => s.Key == "NoTab").Tab);
        }
        finally
        {
            TestSchema.Cleanup(folder);
        }
    }

    [Fact]
    public void TrySetValue_UpdatesValueAndDirtyState()
    {
        var catalog = TestSchema.LoadCatalog(out var folder);
        try
        {
            var changes = 0;
            catalog.ValuesChanged += (_, _) => changes++;

            Assert.True(catalog.TrySetValue("CloudDensity", JsonValue.Create(7.25)));

            Assert.Equal(7.25, TestSchema.AsDouble(catalog.GetValue("CloudDensity")), 3);
            Assert.True(catalog.IsDirty);
            Assert.Equal(1, changes);

            // Unknown keys are rejected instead of creating orphan settings.
            Assert.False(catalog.TrySetValue("DoesNotExist", JsonValue.Create(1)));
        }
        finally
        {
            TestSchema.Cleanup(folder);
        }
    }

    [Fact]
    public void Save_WritesValuesKeepsUnknownDataAndCreatesBackup()
    {
        var settingsPath = TestSchema.CreateSettingsFile(out var folder);
        try
        {
            var catalog = ShaderSettingsCatalog.Load(settingsPath);
            catalog.TrySetValue("EnableClouds", JsonValue.Create(false));
            catalog.TrySetValue("ToneMap", JsonValue.Create(2));

            var backup = catalog.Save();

            Assert.NotNull(backup);
            Assert.True(File.Exists(backup));
            Assert.False(catalog.IsDirty);

            var reloaded = JsonNode.Parse(File.ReadAllText(settingsPath))!.AsObject();

            // Edited values landed in the "default" field the addon reads.
            var uiSettings = reloaded["ui_settings"]!.AsArray();
            var clouds = uiSettings.Single(node => node!["key"]!.GetValue<string>() == "EnableClouds")!;
            Assert.False(clouds["default"]!.GetValue<bool>());

            // Everything the app does not understand survived the round trip.
            Assert.Equal("me", reloaded["custom_unknown_section"]!["keep"]!.GetValue<string>());
            Assert.Equal(7, reloaded["custom_unknown_section"]!["number"]!.GetValue<int>());
            Assert.Equal(3, reloaded["groups"]![0]!["hashes"]!.AsArray().Count);
            Assert.Single(reloaded["links"]!.AsArray());

            var reloadedCatalog = ShaderSettingsCatalog.Load(settingsPath);
            Assert.False(reloadedCatalog.IsDirty);
            Assert.False(reloadedCatalog.GetValue("EnableClouds")!.GetValue<bool>());
        }
        finally
        {
            TestSchema.Cleanup(folder);
        }
    }

    [Fact]
    public void RestoreDefaults_RevertsEveryEdit()
    {
        var catalog = TestSchema.LoadCatalog(out var folder);
        try
        {
            catalog.TrySetValue("EnableClouds", JsonValue.Create(false));
            catalog.TrySetValue("Lamp", new JsonArray(0.0, 0.0, 0.0, 1.0));
            Assert.True(catalog.IsDirty);

            catalog.RestoreDefaults();

            Assert.False(catalog.IsDirty);
            Assert.True(catalog.GetValue("EnableClouds")!.GetValue<bool>());
            Assert.Equal(4, catalog.GetValue("Lamp")!.AsArray().Count);
            Assert.Equal(0.5, TestSchema.AsDouble(catalog.GetValue("Lamp")![1]), 3);
        }
        finally
        {
            TestSchema.Cleanup(folder);
        }
    }

    [Fact]
    public void RestoreDefault_OnlyTouchesOneKey()
    {
        var catalog = TestSchema.LoadCatalog(out var folder);
        try
        {
            catalog.TrySetValue("EnableClouds", JsonValue.Create(false));
            catalog.TrySetValue("CloudDensity", JsonValue.Create(9.0));

            catalog.RestoreDefault("CloudDensity");

            Assert.Equal(2.5, TestSchema.AsDouble(catalog.GetValue("CloudDensity")), 3);
            Assert.False(catalog.GetValue("EnableClouds")!.GetValue<bool>());
            Assert.True(catalog.IsDirty);
        }
        finally
        {
            TestSchema.Cleanup(folder);
        }
    }

    [Theory]
    [InlineData("%.0f nits", 203.0, "203 nits")]
    [InlineData("%.2f", 1.005, "1.00")]
    [InlineData(null, 0.12345, "0.123")]
    public void FormatValue_UsesSchemaFormat(string? format, double value, string expected)
    {
        var definition = new UiSettingDefinition
        {
            Key = "K",
            Label = "L",
            Type = SettingType.Slider,
            Format = format,
        };

        Assert.Equal(expected, ShaderSettingsCatalog.FormatValue(definition, value));
    }

    [Fact]
    public void Load_MissingFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() =>
            ShaderSettingsCatalog.Load(Path.Combine(Path.GetTempPath(), "shaderstudio-missing", "settings.json")));
    }
}
