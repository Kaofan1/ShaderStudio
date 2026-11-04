using Avalonia;
using ShaderStudio.App.Diagnostics;

namespace ShaderStudio.App;

internal static class Program
{
    /// <summary>
    /// Entry point. <c>--diagnose</c> runs a headless self check (folder detection, schema parsing
    /// and optionally an update check) so the app can be verified without opening a window.
    /// </summary>
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Any(arg => string.Equals(arg, "--diagnose", StringComparison.OrdinalIgnoreCase)))
        {
            return DiagnosticReport.Run(args);
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont();
}
