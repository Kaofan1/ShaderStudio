using System.Text.Json.Nodes;
using ShaderStudio.Core.Services;

namespace ShaderStudio.Core.Tests;

internal static class TestSchema
{
    public const string Json = """
    {
      "groups": [
        {
          "key": "G",
          "label": "Group",
          "default": true,
          "hidden": true,
          "hashes": ["0xAAAA", "0xBBBB", "0xCCCC"]
        }
      ],
      "tabs": ["World", "Options"],
      "ui_settings": [
        { "key": "EnableClouds", "label": "Clouds", "tooltip": "Cloud toggle", "section": "Sky", "type": "bool", "default": true, "tab": "World" },
        { "key": "CloudDensity", "label": "Density", "section": "Sky", "type": "slider", "min": 0.0, "max": 10.0, "default": 2.5, "format": "%.2f", "tab": "World" },
        { "key": "ToneMap", "label": "Mapper", "section": "HDR", "type": "int", "labels": ["Vanilla", "RenoDRT"], "default": 0, "hdr_only": true, "tab": "Options" },
        { "key": "Lamp", "label": "Lamp", "section": "Lights", "type": "color", "default": [1.0, 0.5, 0.25, 1.0], "tab": "Options" },
        { "key": "Secret", "label": "Hidden", "section": "Sky", "type": "bool", "default": false, "hidden": true, "tab": "World" },
        { "key": "NoTab", "label": "No tab", "section": "Sky", "type": "bool", "default": false }
      ],
      "links": [
        { "label": "Docs", "url": "https://example.com/docs" }
      ],
      "custom_unknown_section": { "keep": "me", "number": 7 }
    }
    """;

    /// <summary>Creates a temp folder with a settings.json written from <see cref="Json"/>.</summary>
    public static string CreateSettingsFile(out string folder, string? json = null)
    {
        folder = Path.Combine(Path.GetTempPath(), "shaderstudio-tests", Guid.NewGuid().ToString("N"));
        var customShaders = Path.Combine(folder, "CustomShaders");
        Directory.CreateDirectory(customShaders);

        var path = Path.Combine(customShaders, "settings.json");
        File.WriteAllText(path, json ?? Json);
        return path;
    }

    public static ShaderSettingsCatalog LoadCatalog(out string folder) =>
        ShaderSettingsCatalog.Load(CreateSettingsFile(out folder));

    public static void Cleanup(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best effort cleanup of temp test data.
        }
    }

    public static double AsDouble(JsonNode? node) => node?.GetValue<double>() ?? double.NaN;
}
