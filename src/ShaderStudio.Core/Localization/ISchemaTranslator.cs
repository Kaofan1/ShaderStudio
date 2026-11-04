namespace ShaderStudio.Core.Localization;

/// <summary>Translated label and tooltip of one setting.</summary>
public readonly record struct SettingTranslation(string Label, string? Tooltip);

/// <summary>
/// Translates the text that comes from the shader schema file (tabs, sections and setting
/// labels/tooltips). Keys are never translated, so saved values and profiles stay valid.
/// </summary>
public interface ISchemaTranslator
{
    string TranslateTab(string tab);

    string TranslateSection(string section);

    SettingTranslation TranslateSetting(string key, string label, string? tooltip);
}

/// <summary>Returns the schema text unchanged.</summary>
public sealed class PassthroughSchemaTranslator : ISchemaTranslator
{
    public static readonly PassthroughSchemaTranslator Instance = new();

    public string TranslateTab(string tab) => tab;

    public string TranslateSection(string section) => section;

    public SettingTranslation TranslateSetting(string key, string label, string? tooltip) => new(label, tooltip);
}
