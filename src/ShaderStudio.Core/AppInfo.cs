using System.Reflection;

namespace ShaderStudio.Core;

/// <summary>
/// Static information about the application: name, version and the GitHub repository used
/// for automatic updates.
/// </summary>
public static class AppInfo
{
    /// <summary>Name shown in the window title and the UI header.</summary>
    public const string ProductName = "Shader Studio for FiveM";

    public const string ProductTagline = "Ustawienia shaderów ReShade / FiveM";

    /// <summary>Repository that publishes the updates (override with SHADERSTUDIO_REPO_OWNER).</summary>
    public const string DefaultRepositoryOwner = "Kaofan1";

    public const string DefaultRepositoryName = "ShaderStudio";

    /// <summary>Owner value that means "no repository configured yet".</summary>
    private const string UnconfiguredOwnerMarker = "OWNER";

    /// <summary>Folder created under %APPDATA% for application state.</summary>
    public const string AppDataFolderName = "ShaderStudio";

    public static string RepositoryOwner { get; } = ReadEnv("SHADERSTUDIO_REPO_OWNER", DefaultRepositoryOwner);

    public static string RepositoryName { get; } = ReadEnv("SHADERSTUDIO_REPO_NAME", DefaultRepositoryName);

    public static string RepositoryUrl => $"https://github.com/{RepositoryOwner}/{RepositoryName}";

    /// <summary>
    /// True when a real GitHub repository is configured. Until then update checks are skipped
    /// instead of failing with a confusing network error.
    /// </summary>
    public static bool IsUpdateChannelConfigured =>
        !string.IsNullOrWhiteSpace(RepositoryOwner)
        && !string.Equals(RepositoryOwner, UnconfiguredOwnerMarker, StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(RepositoryName);

    /// <summary>Version of the running build, e.g. "1.0.0" (build metadata after '+' is stripped).</summary>
    public static string Version { get; } = ResolveVersion();

    /// <summary>Text used everywhere the version is displayed.</summary>
    public static string VersionDisplay => $"v{Version}";

    private static string ResolveVersion()
    {
        var assembly = typeof(AppInfo).Assembly;

        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            var plus = informational.IndexOf('+');
            return plus > 0 ? informational[..plus] : informational;
        }

        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }

    private static string ReadEnv(string name, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
