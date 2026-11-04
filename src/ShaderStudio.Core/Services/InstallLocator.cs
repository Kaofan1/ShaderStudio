namespace ShaderStudio.Core.Services;

/// <summary>
/// A folder that looks like an installed shader/ReShade setup: it contains
/// <c>CustomShaders\settings.json</c> plus the addon and ReShade binaries.
/// </summary>
public sealed record ShaderInstallation
{
    /// <summary>Folder containing <c>CustomShaders</c> - normally the FiveM <c>plugins</c> folder.</summary>
    public required string RootDirectory { get; init; }

    /// <summary>Full path of the editable <c>settings.json</c>.</summary>
    public required string SettingsPath { get; init; }

    /// <summary>Shader addon binaries found next to the shaders (e.g. <c>CoreFX.addon64</c>).</summary>
    public IReadOnlyList<string> AddonFiles { get; init; } = Array.Empty<string>();

    public int ShaderFileCount { get; init; }

    public bool HasReShade { get; init; }

    public bool IsFiveM { get; init; }

    public string CustomShadersDirectory => Path.Combine(RootDirectory, "CustomShaders");

    public string? AddonName => AddonFiles
        .Select(Path.GetFileNameWithoutExtension)
        .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

    public string Kind => IsFiveM ? "FiveM" : "GTA V";

    public string Summary =>
        $"{Kind} - {ShaderFileCount} shaderów, addon: {(AddonName ?? "brak")}, ReShade: {(HasReShade ? "tak" : "brak")}";
}

/// <summary>Finds shader installations on this machine and validates a user-picked folder.</summary>
public static class InstallLocator
{
    /// <summary>Environment variable with an extra path to probe first (useful for tests/portable setups).</summary>
    public const string PathOverrideVariable = "SHADERSTUDIO_FIVEM_PATH";

    /// <summary>Known locations of FiveM / GTA V shader folders on Windows.</summary>
    public static IReadOnlyList<string> GetCandidateFolders()
    {
        var candidates = new List<string>();

        void Add(string? path)
        {
            if (!string.IsNullOrWhiteSpace(path) && !candidates.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                candidates.Add(path);
            }
        }

        Add(Environment.GetEnvironmentVariable(PathOverrideVariable));

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // FiveM stores its "application data" folder here; plugins live inside FiveM.app.
        foreach (var baseFolder in new[] { localAppData, roamingAppData })
        {
            if (string.IsNullOrWhiteSpace(baseFolder))
            {
                continue;
            }

            Add(Path.Combine(baseFolder, "FiveM", "FiveM.app", "plugins"));
            Add(Path.Combine(baseFolder, "FiveM", "FiveM.app"));
            Add(Path.Combine(baseFolder, "FiveM", "FiveM.app", "mods"));
        }

        return candidates;
    }

    /// <summary>
    /// Validates a folder picked by the user. Accepts the <c>plugins</c> folder, the
    /// <c>FiveM.app</c> folder, the <c>CustomShaders</c> folder or a path to <c>settings.json</c>.
    /// </summary>
    public static ShaderInstallation? TryCreate(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        path = path.Trim().Trim('"');
        if (File.Exists(path))
        {
            path = Path.GetDirectoryName(path) ?? path;
        }

        if (!Directory.Exists(path))
        {
            return null;
        }

        var root = ResolveRoot(path);
        if (root is null)
        {
            return null;
        }

        var customShaders = Path.Combine(root, "CustomShaders");
        var settingsPath = Path.Combine(customShaders, "settings.json");

        int shaderCount;
        try
        {
            shaderCount = Directory.EnumerateFiles(customShaders, "*.cso").Count();
        }
        catch (IOException)
        {
            shaderCount = 0;
        }
        catch (UnauthorizedAccessException)
        {
            shaderCount = 0;
        }

        List<string> addonFiles = new();
        try
        {
            addonFiles.AddRange(Directory.EnumerateFiles(root, "*.addon*"));
        }
        catch (IOException)
        {
            // Ignored: an unreadable folder is reported through the missing addon name.
        }
        catch (UnauthorizedAccessException)
        {
        }

        var isFiveM = root.Contains("FiveM", StringComparison.OrdinalIgnoreCase)
                      || root.Contains("CitizenFX", StringComparison.OrdinalIgnoreCase);

        return new ShaderInstallation
        {
            RootDirectory = root,
            SettingsPath = settingsPath,
            AddonFiles = addonFiles,
            ShaderFileCount = shaderCount,
            HasReShade = File.Exists(Path.Combine(root, "dxgi.dll")) || File.Exists(Path.Combine(root, "ReShade.ini")),
            IsFiveM = isFiveM,
        };
    }

    /// <summary>Probes every known location and returns the valid installations found.</summary>
    public static IReadOnlyList<ShaderInstallation> FindAll()
    {
        var found = new List<ShaderInstallation>();
        foreach (var candidate in GetCandidateFolders())
        {
            var installation = TryCreate(candidate);
            if (installation is not null
                && !found.Any(f => string.Equals(f.SettingsPath, installation.SettingsPath, StringComparison.OrdinalIgnoreCase)))
            {
                found.Add(installation);
            }
        }

        return found;
    }

    private static string? ResolveRoot(string path)
    {
        // 1. The folder itself holds CustomShaders\settings.json (plugins folder / game root).
        if (File.Exists(Path.Combine(path, "CustomShaders", "settings.json")))
        {
            return path;
        }

        // 2. The custom shaders folder was picked directly.
        if (string.Equals(Path.GetFileName(path), "CustomShaders", StringComparison.OrdinalIgnoreCase)
            && File.Exists(Path.Combine(path, "settings.json")))
        {
            return Path.GetDirectoryName(path);
        }

        // 3. A parent folder such as FiveM.app or the FiveM application data folder was picked.
        foreach (var child in new[] { "plugins", Path.Combine("FiveM.app", "plugins") })
        {
            var candidate = Path.Combine(path, child);
            if (File.Exists(Path.Combine(candidate, "CustomShaders", "settings.json")))
            {
                return candidate;
            }
        }

        return null;
    }
}
