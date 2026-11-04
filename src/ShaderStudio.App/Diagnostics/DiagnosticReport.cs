using System.Text;
using ShaderStudio.Core;
using ShaderStudio.Core.Services;
using ShaderStudio.Core.Updates;

namespace ShaderStudio.App.Diagnostics;

/// <summary>
/// Headless self check used by CI and by developers: prints the detected installations, a schema
/// summary and, optionally, the result of an update check. Returns a non-zero exit code when no
/// installation is found or the schema cannot be parsed.
/// </summary>
internal static class DiagnosticReport
{
    public static int Run(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.WriteLine($"== {AppInfo.ProductName} ==");
        Console.WriteLine($"Wersja aplikacji : {AppInfo.Version}");
        Console.WriteLine($"Repozytorium     : {AppInfo.RepositoryUrl}");
        Console.WriteLine($"Kanał aktualizacji: {(AppInfo.IsUpdateChannelConfigured ? "skonfigurowany" : "NIE skonfigurowany")}");
        Console.WriteLine();

        var explicitPath = GetOption(args, "--path");
        var installations = explicitPath is not null
            ? Single(InstallLocator.TryCreate(explicitPath))
            : InstallLocator.FindAll();

        Console.WriteLine($"Znalezione instalacje: {installations.Count}");
        foreach (var installation in installations)
        {
            Console.WriteLine($"  - {installation.RootDirectory}");
            Console.WriteLine($"    {installation.Summary}");
        }

        if (installations.Count == 0)
        {
            Console.WriteLine();
            Console.WriteLine("BŁĄD: nie znaleziono żadnej instalacji shaderów.");
            return 2;
        }

        var exitCode = 0;

        foreach (var installation in installations)
        {
            Console.WriteLine();
            Console.WriteLine($"--- Schema: {installation.SettingsPath}");

            try
            {
                var catalog = ShaderSettingsCatalog.Load(installation.SettingsPath, ShaderStudio.Core.Localization.PolishSchemaTranslator.Instance);
                var visible = catalog.Settings.Count(setting => !setting.Hidden);

                Console.WriteLine($"  zakładki        : {string.Join(", ", catalog.Tabs)}");
                Console.WriteLine($"  grupy           : {catalog.Groups.Count} ({catalog.ShaderHashCount} hashy)");
                Console.WriteLine($"  ustawienia      : {catalog.Settings.Count} (widoczne: {visible})");
                Console.WriteLine($"  HDR-only        : {catalog.Settings.Count(setting => setting.HdrOnly)}");
                Console.WriteLine($"  linki           : {catalog.Links.Count}");
                Console.WriteLine($"  zmiany w pliku  : {(catalog.IsDirty ? "tak" : "nie")}");

                var byType = catalog.Settings
                    .Where(setting => !setting.Hidden)
                    .GroupBy(setting => setting.Type)
                    .OrderBy(group => group.Key.ToString())
                    .Select(group => $"{group.Key}={group.Count()}");

                Console.WriteLine($"  typy kontrolek  : {string.Join(", ", byType)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BŁĄD: {ex.Message}");
                exitCode = 3;
            }
        }

        if (args.Any(arg => string.Equals(arg, "--check-updates", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine();
            Console.WriteLine("--- Sprawdzanie aktualizacji");

            if (!AppInfo.IsUpdateChannelConfigured)
            {
                Console.WriteLine("POMINIĘTE: repozytorium nie jest skonfigurowane.");
            }
            else
            {
                try
                {
                    using var client = new GitHubUpdateClient();
                    var result = client.CheckAsync().GetAwaiter().GetResult();
                    Console.WriteLine($"  zainstalowana : {result.Current}");
                    Console.WriteLine($"  najnowsza     : {result.Latest?.TagName ?? "brak wydan"}");
                    Console.WriteLine($"  aktualizacja  : {(result.IsUpdateAvailable ? "dostępna" : "brak")}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  BŁĄD: {ex.Message}");
                    exitCode = 4;
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine(exitCode == 0 ? "OK" : $"Zakończono z kodem {exitCode}");
        return exitCode;
    }

    private static IReadOnlyList<ShaderInstallation> Single(ShaderInstallation? installation) =>
        installation is null ? Array.Empty<ShaderInstallation>() : new[] { installation };

    private static string? GetOption(string[] args, string name)
    {
        for (var index = 0; index < args.Length; index++)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                return args[index + 1];
            }

            if (args[index].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
            {
                return args[index][(name.Length + 1)..];
            }
        }

        return null;
    }
}
