using ShaderStudio.Core.Localization;
using ShaderStudio.Core.Services;
using Xunit;

namespace ShaderStudio.Core.Tests;

public sealed class PolishTranslationTests
{
    [Theory]
    [InlineData("World", "Świat")]
    [InlineData("Vehicles & Peds", "Pojazdy i postacie")]
    [InlineData("Lighting", "Oświetlenie")]
    [InlineData("Image", "Obraz")]
    [InlineData("Options", "Opcje")]
    public void Tabs_AreTranslated(string raw, string expected)
    {
        Assert.Equal(expected, PolishSchemaTranslator.Instance.TranslateTab(raw));
    }

    [Theory]
    [InlineData("Sky", "Niebo")]
    [InlineData("Texture Detail", "Szczegóły tekstur")]
    [InlineData("HDR Color Grading", "Grading kolorów HDR")]
    public void Sections_AreTranslated(string raw, string expected)
    {
        Assert.Equal(expected, PolishSchemaTranslator.Instance.TranslateSection(raw));
    }

    [Fact]
    public void KnownSetting_HasPolishLabelAndTooltip()
    {
        var translation = PolishSchemaTranslator.Instance.TranslateSetting("EnableVolumetricClouds", "Volumetric Clouds", "Toggle clouds.");

        Assert.Equal("Chmury wolumetryczne", translation.Label);
        Assert.NotNull(translation.Tooltip);
        Assert.NotEqual("Toggle clouds.", translation.Tooltip);
    }

    [Fact]
    public void UnknownSetting_KeepsSchemaText()
    {
        var translation = PolishSchemaTranslator.Instance.TranslateSetting("SomeBrandNewSetting", "Brand new", "From the schema.");

        Assert.Equal("Brand new", translation.Label);
        Assert.Equal("From the schema.", translation.Tooltip);
    }

    [Fact]
    public void UnknownTabAndSection_ArePassedThrough()
    {
        Assert.Equal("Something", PolishSchemaTranslator.Instance.TranslateTab("Something"));
        Assert.Equal("Somewhere", PolishSchemaTranslator.Instance.TranslateSection("Somewhere"));
    }

    [Fact]
    public void Catalog_UsesTranslatorForTabsSectionsAndLabels()
    {
        var catalog = ShaderSettingsCatalog.Load(
            TestSchema.CreateSettingsFile(out var folder),
            PolishSchemaTranslator.Instance);

        try
        {
            Assert.Equal(new[] { "Świat", "Opcje" }, catalog.Tabs);

            var clouds = catalog.Settings.Single(setting => setting.Key == "EnableClouds");
            Assert.Equal("Niebo", clouds.Section);
            Assert.Equal("Świat", clouds.Tab);

            // Keys are never translated: the file and profiles stay compatible.
            Assert.Equal("EnableClouds", clouds.Key);
        }
        finally
        {
            TestSchema.Cleanup(folder);
        }
    }

    [Fact]
    public void Translator_CoversMostCoreFxSettings()
    {
        // Guards against the dictionary silently losing entries.
        Assert.True(PolishSchemaTranslator.KnownSettingKeys.Count >= 55,
            $"Slownik ma tylko {PolishSchemaTranslator.KnownSettingKeys.Count} kluczy.");
    }
}
