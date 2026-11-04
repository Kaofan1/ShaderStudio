namespace ShaderStudio.Core;

/// <summary>Well known folders used by the app for its own data (never inside the game folder).</summary>
public static class AppPaths
{
    public static string AppDataDirectory
    {
        get
        {
            var baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(baseFolder))
            {
                baseFolder = Path.Combine(Path.GetTempPath(), "ShaderStudio");
            }

            return Path.Combine(baseFolder, AppInfo.AppDataFolderName);
        }
    }
}
