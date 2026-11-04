using System.Text.Json.Nodes;

namespace ShaderStudio.Core.Models;

/// <summary>
/// Metadata of one entry from the <c>ui_settings</c> array of a shader
/// <c>settings.json</c>. The visible control is generated from this description.
/// </summary>
public sealed class UiSettingDefinition
{
    public required string Key { get; init; }

    public string Label { get; init; } = string.Empty;

    public string? Tooltip { get; init; }

    public string Section { get; init; } = "Ogólne";

    /// <summary>Tab the setting belongs to. Null when the schema does not declare one.</summary>
    public string? Tab { get; init; }

    public SettingType Type { get; init; } = SettingType.Unknown;

    public bool Hidden { get; init; }

    public bool HdrOnly { get; init; }

    public double Min { get; init; }

    public double Max { get; init; } = 1.0;

    /// <summary>Optional printf-style format such as <c>%.2f</c> or <c>%.0f nits</c>.</summary>
    public string? Format { get; init; }

    /// <summary>Value names for an <see cref="SettingType.Int"/> choice.</summary>
    public IReadOnlyList<string> Labels { get; init; } = Array.Empty<string>();

    /// <summary>Value the file shipped with; restored by "przywroc domyslne".</summary>
    public JsonNode? OriginalValue { get; init; }

    public string Title => string.IsNullOrWhiteSpace(Label) ? Key : Label;
}
