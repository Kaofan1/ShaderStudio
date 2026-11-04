using System.Text.Json;
using System.Text.Json.Nodes;

namespace ShaderStudio.Core.Updates;

/// <summary>Small persisted state: last used shader folder and the "skip this version" choice.</summary>
public sealed class AppState
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public string? LastInstallPath { get; set; }

    public string? SkippedVersion { get; set; }

    public bool CheckForUpdatesOnStartup { get; set; } = true;

    private static string StateFile => Path.Combine(AppPaths.AppDataDirectory, "app-state.json");

    public static AppState Load()
    {
        try
        {
            if (!File.Exists(StateFile))
            {
                return new AppState();
            }

            var root = JsonNode.Parse(File.ReadAllText(StateFile)) as JsonObject;
            if (root is null)
            {
                return new AppState();
            }

            return new AppState
            {
                LastInstallPath = root["lastInstallPath"]?.GetValue<string>(),
                SkippedVersion = root["skippedVersion"]?.GetValue<string>(),
                CheckForUpdatesOnStartup = root["checkForUpdatesOnStartup"]?.GetValue<bool>() ?? true,
            };
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException)
        {
            return new AppState();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.AppDataDirectory);
            var root = new JsonObject
            {
                ["lastInstallPath"] = LastInstallPath,
                ["skippedVersion"] = SkippedVersion,
                ["checkForUpdatesOnStartup"] = CheckForUpdatesOnStartup,
            };

            File.WriteAllText(StateFile, root.ToJsonString(WriteOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Persisting state is best effort: losing it only costs the user one folder pick.
        }
    }
}
