using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using ShaderStudio.Core.Localization;
using ShaderStudio.Core.Models;

namespace ShaderStudio.Core.Services;

/// <summary>
/// Loads a shader addon <c>settings.json</c>, exposes its schema and lets callers edit the
/// stored values while preserving every field the app does not understand.
/// </summary>
/// <remarks>
/// Values live in the <c>default</c> field of each <c>ui_settings</c> entry - that is what the
/// addon reads at load time. Edits therefore patch only that property, so unknown keys,
/// comments-free extras, groups, tabs and links survive a round trip untouched.
/// </remarks>
public sealed class ShaderSettingsCatalog
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly JsonObject _root;
    private readonly List<UiSettingDefinition> _settings = new();
    private readonly List<ShaderGroupInfo> _groups = new();
    private readonly List<string> _tabs = new();
    private readonly Dictionary<string, JsonObject> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, JsonNode?> _originalValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ShaderLinkInfo> _links = new();
    private readonly ISchemaTranslator _translator;

    private ShaderSettingsCatalog(string path, JsonObject root, ISchemaTranslator? translator)
    {
        FilePath = path;
        _translator = translator ?? PassthroughSchemaTranslator.Instance;
        _root = root;
        Parse();
    }

    public string FilePath { get; }

    public string FileName => Path.GetFileName(FilePath);

    public IReadOnlyList<UiSettingDefinition> Settings => _settings;

    public IReadOnlyList<ShaderGroupInfo> Groups => _groups;

    public IReadOnlyList<string> Tabs => _tabs;

    public IReadOnlyList<ShaderLinkInfo> Links => _links;

    public int ShaderHashCount => _groups.Sum(g => g.HashCount);

    public string? ShaderWarningLabel => _root["shader_warning"]?["label"]?.GetValue<string>();

    /// <summary>True when at least one value differs from what the file contained on load/save.</summary>
    public bool IsDirty
    {
        get
        {
            foreach (var (key, original) in _originalValues)
            {
                if (!JsonNode.DeepEquals(original, GetValue(key)))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Raised whenever a value changes, so the UI can refresh its dirty state.</summary>
    public event EventHandler? ValuesChanged;

    public static ShaderSettingsCatalog Load(string settingsPath, ISchemaTranslator? translator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);

        if (!File.Exists(settingsPath))
        {
            throw new FileNotFoundException("Nie znaleziono pliku settings.json.", settingsPath);
        }

        var text = ReadWithBomTolerance(settingsPath);
        var root = JsonNode.Parse(text) as JsonObject
                   ?? throw new InvalidDataException("settings.json nie zawiera obiektu JSON.");

        return new ShaderSettingsCatalog(settingsPath, root, translator);
    }

    public JsonNode? GetValue(string key)
    {
        if (!_entries.TryGetValue(key, out var entry))
        {
            return null;
        }

        return entry["default"]?.DeepClone();
    }

    public bool TrySetValue(string key, JsonNode? value)
    {
        if (!_entries.TryGetValue(key, out var entry))
        {
            return false;
        }

        entry["default"] = value?.DeepClone();
        ValuesChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Restores every value to what the file contained on load / last save.</summary>
    public void RestoreDefaults()
    {
        foreach (var (key, original) in _originalValues)
        {
            if (_entries.TryGetValue(key, out var entry))
            {
                entry["default"] = original?.DeepClone();
            }
        }

        ValuesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RestoreDefault(string key)
    {
        if (_entries.TryGetValue(key, out var entry) && _originalValues.TryGetValue(key, out var original))
        {
            entry["default"] = original?.DeepClone();
            ValuesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Current values keyed by setting key, ready to be stored as a profile.</summary>
    public Dictionary<string, JsonNode?> Snapshot()
    {
        var snapshot = new Dictionary<string, JsonNode?>(StringComparer.OrdinalIgnoreCase);
        foreach (var setting in _settings)
        {
            snapshot[setting.Key] = GetValue(setting.Key);
        }

        return snapshot;
    }

    /// <summary>
    /// Applies a profile. Keys the schema does not know are ignored, so an old profile keeps
    /// working after a shader update adds or removes settings.
    /// </summary>
    public int ApplyValues(IReadOnlyDictionary<string, JsonNode?> values)
    {
        var applied = 0;
        foreach (var (key, value) in values)
        {
            if (TrySetValue(key, value))
            {
                applied++;
            }
        }

        return applied;
    }

    /// <summary>
    /// Writes the file atomically: a <c>.bak</c> copy of the previous content is kept next to it
    /// and the new content replaces the original only once fully written.
    /// </summary>
    public string? Save()
    {
        var directory = Path.GetDirectoryName(FilePath)
                        ?? throw new InvalidOperationException("Nieprawidłowa ścieżka pliku ustawień.");

        Directory.CreateDirectory(directory);

        string? backupPath = null;
        if (File.Exists(FilePath))
        {
            backupPath = FilePath + ".bak";
            File.Copy(FilePath, backupPath, overwrite: true);
        }

        var tempPath = Path.Combine(directory, Path.GetFileName(FilePath) + ".tmp");
        File.WriteAllText(tempPath, _root.ToJsonString(WriteOptions), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        try
        {
            File.Move(tempPath, FilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }

        RememberCurrentValues();
        ValuesChanged?.Invoke(this, EventArgs.Empty);
        return backupPath;
    }

    /// <summary>
    /// Formats a slider value using the schema format string. The schema uses printf style
    /// placeholders (<c>%.2f</c>, <c>%.0f nits</c>), so they are translated to .NET format items.
    /// </summary>
    public static string FormatValue(UiSettingDefinition definition, double value)
    {
        var format = definition.Format;
        if (string.IsNullOrWhiteSpace(format))
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        try
        {
            var converted = PrintfPlaceholder.Replace(format, match =>
            {
                var kind = match.Groups["kind"].Value.ToUpperInvariant();
                var precision = match.Groups["precision"].Success ? match.Groups["precision"].Value : null;

                return precision is null
                    ? $"{{0:{kind}2}}"
                    : $"{{0:{kind}{precision}}}";
            });

            if (converted.Contains("{0", StringComparison.Ordinal))
            {
                return string.Format(CultureInfo.InvariantCulture, converted, value);
            }
        }
        catch (FormatException)
        {
            // Falls through to the plain number below.
        }

        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static readonly System.Text.RegularExpressions.Regex PrintfPlaceholder = new(
        @"%\.?(?<precision>\d+)?(?<kind>[fdgFDG])",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    private void Parse()
    {
        if (_root["tabs"] is JsonArray tabs)
        {
            foreach (var tab in tabs)
            {
                var rawName = tab?.GetValue<string>();
                var name = rawName is null ? null : _translator.TranslateTab(rawName);
                if (!string.IsNullOrWhiteSpace(name) && !_tabs.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    _tabs.Add(name);
                }
            }
        }

        if (_root["groups"] is JsonArray groups)
        {
            foreach (var node in groups.OfType<JsonObject>())
            {
                _groups.Add(new ShaderGroupInfo
                {
                    Key = ReadString(node, "key") ?? "?",
                    Label = ReadString(node, "label") ?? string.Empty,
                    IsDefault = ReadBool(node, "default"),
                    Hidden = ReadBool(node, "hidden"),
                    HdrGroup = ReadBool(node, "hdr_group"),
                    HashCount = node["hashes"] is JsonArray hashes ? hashes.Count : 0,
                });
            }
        }

        if (_root["links"] is JsonArray links)
        {
            foreach (var node in links.OfType<JsonObject>())
            {
                var url = ReadString(node, "url");
                if (string.IsNullOrWhiteSpace(url))
                {
                    continue;
                }

                _links.Add(new ShaderLinkInfo(ReadString(node, "label") ?? url, url, ReadString(node, "tooltip")));
            }
        }

        if (_root["ui_settings"] is not JsonArray uiSettings)
        {
            return;
        }

        foreach (var node in uiSettings.OfType<JsonObject>())
        {
            var key = ReadString(node, "key");
            if (string.IsNullOrWhiteSpace(key) || _entries.ContainsKey(key))
            {
                continue;
            }

            var type = SettingTypeParser.Parse(ReadString(node, "type"));
            var definition = new UiSettingDefinition
            {
                Key = key,
                Label = _translator.TranslateSetting(key, ReadString(node, "label") ?? string.Empty, ReadString(node, "tooltip")).Label,
                Tooltip = _translator.TranslateSetting(key, ReadString(node, "label") ?? string.Empty, ReadString(node, "tooltip")).Tooltip,
                Section = _translator.TranslateSection(ReadString(node, "section") ?? "Ogólne"),
                Tab = ReadString(node, "tab") is { } rawTab ? _translator.TranslateTab(rawTab) : null,
                Type = type,
                Hidden = ReadBool(node, "hidden"),
                HdrOnly = ReadBool(node, "hdr_only"),
                Min = ReadDouble(node, "min") ?? 0.0,
                Max = ReadDouble(node, "max") ?? (type == SettingType.Bool ? 1.0 : 100.0),
                Format = ReadString(node, "format"),
                Labels = ReadStringArray(node, "labels"),
                OriginalValue = node["default"]?.DeepClone(),
            };

            _settings.Add(definition);
            _entries[key] = node;

            if (string.IsNullOrWhiteSpace(definition.Tab))
            {
                var fallback = _tabs.FirstOrDefault() ?? "Ogólne";
                _settings[^1] = CloneWithTab(definition, fallback);
            }
        }

        // A schema without an explicit tab list still gets usable tabs.
        if (_tabs.Count == 0)
        {
            foreach (var setting in _settings)
            {
                var tab = setting.Tab ?? "Ogólne";
                if (!_tabs.Contains(tab, StringComparer.OrdinalIgnoreCase))
                {
                    _tabs.Add(tab);
                }
            }
        }

        RememberCurrentValues();
    }

    private static UiSettingDefinition CloneWithTab(UiSettingDefinition source, string tab) => new()
    {
        Key = source.Key,
        Label = source.Label,
        Tooltip = source.Tooltip,
        Section = source.Section,
        Tab = tab,
        Type = source.Type,
        Hidden = source.Hidden,
        HdrOnly = source.HdrOnly,
        Min = source.Min,
        Max = source.Max,
        Format = source.Format,
        Labels = source.Labels,
        OriginalValue = source.OriginalValue,
    };

    private void RememberCurrentValues()
    {
        _originalValues.Clear();
        foreach (var (key, entry) in _entries)
        {
            _originalValues[key] = entry["default"]?.DeepClone();
        }
    }

    private static string ReadWithBomTolerance(string path)
    {
        var bytes = File.ReadAllBytes(path);
        using var stream = new MemoryStream(bytes);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static string? ReadString(JsonObject node, string name)
    {
        var value = node[name];
        if (value is null)
        {
            return null;
        }

        try
        {
            return value.GetValue<string>();
        }
        catch (InvalidOperationException)
        {
            return value.ToJsonString();
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static bool ReadBool(JsonObject node, string name)
    {
        var value = node[name];
        return value is not null && value.GetValueKind() == JsonValueKind.True;
    }

    private static double? ReadDouble(JsonObject node, string name)
    {
        var value = node[name];
        if (value is null)
        {
            return null;
        }

        try
        {
            return value.GetValue<double>();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static IReadOnlyList<string> ReadStringArray(JsonObject node, string name)
    {
        if (node[name] is not JsonArray array)
        {
            return Array.Empty<string>();
        }

        var result = new List<string>(array.Count);
        foreach (var item in array)
        {
            result.Add(item?.ToJsonString().Trim('"') ?? string.Empty);
        }

        return result;
    }
}

/// <summary>A documentation / community link shipped inside the schema.</summary>
public sealed record ShaderLinkInfo(string Label, string Url, string? Tooltip);
