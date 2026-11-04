using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ShaderStudio.Core.Services;

/// <summary>A named set of shader settings that can be exported to, and imported from, a file.</summary>
public sealed record ShaderProfile(
    string Name,
    string? AppVersion,
    DateTimeOffset SavedAt,
    string? SourceSettingsPath,
    IReadOnlyDictionary<string, JsonNode?> Values);

/// <summary>JSON format for sharing / backing up setting presets.</summary>
public static class ProfileSerializer
{
    private const string FormatId = "shader-studio-profile";
    private const int FormatVersion = 1;

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static ShaderProfile Create(ShaderSettingsCatalog catalog, string name) => new(
        name,
        AppInfo.Version,
        DateTimeOffset.Now,
        catalog.FilePath,
        catalog.Snapshot());

    public static string Serialize(ShaderProfile profile)
    {
        var values = new JsonObject();
        foreach (var (key, value) in profile.Values)
        {
            values[key] = value?.DeepClone();
        }

        var root = new JsonObject
        {
            ["format"] = FormatId,
            ["formatVersion"] = FormatVersion,
            ["name"] = profile.Name,
            ["appVersion"] = profile.AppVersion,
            ["savedAt"] = profile.SavedAt.ToString("O"),
            ["sourceSettingsPath"] = profile.SourceSettingsPath,
            ["values"] = values,
        };

        return root.ToJsonString(WriteOptions);
    }

    public static ShaderProfile Deserialize(string json)
    {
        var root = JsonNode.Parse(json) as JsonObject
                   ?? throw new InvalidDataException("Plik profilu nie jest obiektem JSON.");

        var format = root["format"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(format) && !string.Equals(format, FormatId, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Nieobsługiwany format profilu: {format}");
        }

        if (root["values"] is not JsonObject values)
        {
            throw new InvalidDataException("Plik profilu nie zawiera sekcji \"values\".");
        }

        var dictionary = new Dictionary<string, JsonNode?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
        {
            dictionary[key] = value?.DeepClone();
        }

        return new ShaderProfile(
            root["name"]?.GetValue<string>() ?? "Profil",
            root["appVersion"]?.GetValue<string>(),
            DateTimeOffset.TryParse(root["savedAt"]?.GetValue<string>(), out var savedAt) ? savedAt : DateTimeOffset.Now,
            root["sourceSettingsPath"]?.GetValue<string>(),
            dictionary);
    }
}
