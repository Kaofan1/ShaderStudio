namespace ShaderStudio.Core.Models;

/// <summary>Control type declared by a setting in <c>ui_settings</c>.</summary>
public enum SettingType
{
    /// <summary>Unrecognised type; rendered as read-only info.</summary>
    Unknown = 0,
    Bool,
    Slider,
    Int,
    Color,
}

public static class SettingTypeParser
{
    public static SettingType Parse(string? raw) => raw?.Trim().ToLowerInvariant() switch
    {
        "bool" => SettingType.Bool,
        "slider" => SettingType.Slider,
        "int" => SettingType.Int,
        "color" => SettingType.Color,
        _ => SettingType.Unknown,
    };
}
