using System.Diagnostics;
using System.Text;

namespace ShaderStudio.Core.Updates;

/// <summary>
/// Installs a downloaded build in place. A separate PowerShell process waits for the app to
/// exit, replaces the executable and starts it again, so the running file is never locked.
/// </summary>
public static class UpdateInstaller
{
    /// <summary>Folder where staged downloads and update scripts are kept.</summary>
    public static string StagingDirectory => Path.Combine(AppPaths.AppDataDirectory, "updates");

    /// <summary>Path of the staged download for a given version.</summary>
    public static string GetStagedExecutablePath(ReleaseVersion version) =>
        Path.Combine(StagingDirectory, $"ShaderStudio-{version}.exe");

    /// <summary>Path of the executable currently running.</summary>
    public static string CurrentExecutablePath
    {
        get
        {
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(processPath) && processPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return processPath;
            }

            return Path.Combine(AppContext.BaseDirectory, "ShaderStudio.exe");
        }
    }

    /// <summary>
    /// Writes the helper script that performs the swap. Kept as a script (not done in-process)
    /// because Windows cannot overwrite a running executable.
    /// </summary>
    public static string WriteApplyScript(string stagedExecutable, string targetExecutable, int processId)
    {
        Directory.CreateDirectory(StagingDirectory);

        var scriptPath = Path.Combine(StagingDirectory, $"apply-update-{Guid.NewGuid():N}.ps1");
        var logPath = Path.Combine(StagingDirectory, "apply-update.log");

        var script = new StringBuilder()
            .AppendLine("$ErrorActionPreference = 'Stop'")
            .AppendLine($"$processId = {processId}")
            .AppendLine($"$staged = '{Escape(stagedExecutable)}'")
            .AppendLine($"$target = '{Escape(targetExecutable)}'")
            .AppendLine($"$log = '{Escape(logPath)}'")
            .AppendLine("$deadline = (Get-Date).AddSeconds(90)")
            .AppendLine("while ((Get-Date) -lt $deadline) {")
            .AppendLine("  if (-not (Get-Process -Id $processId -ErrorAction SilentlyContinue)) { break }")
            .AppendLine("  Start-Sleep -Milliseconds 400")
            .AppendLine("}")
            .AppendLine("try {")
            .AppendLine("  Start-Sleep -Milliseconds 600")
            .AppendLine("  Copy-Item -LiteralPath $staged -Destination $target -Force")
            .AppendLine("  Remove-Item -LiteralPath $staged -Force -ErrorAction SilentlyContinue")
            .AppendLine("  Start-Process -FilePath $target -WorkingDirectory (Split-Path -Parent $target)")
            .AppendLine("  Add-Content -LiteralPath $log -Value ((Get-Date -Format o) + ' OK ' + $target)")
            .AppendLine("} catch {")
            .AppendLine("  Add-Content -LiteralPath $log -Value ((Get-Date -Format o) + ' FAIL ' + $_.Exception.Message)")
            .AppendLine("}")
            .AppendLine("Remove-Item -LiteralPath $PSCommandPath -Force -ErrorAction SilentlyContinue")
            .ToString();

        File.WriteAllText(scriptPath, script, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        return scriptPath;
    }

    /// <summary>Starts the swap script detached from the app; the caller then shuts the app down.</summary>
    public static void LaunchApplyScript(string scriptPath)
    {
        var info = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };

        info.ArgumentList.Add("-NoProfile");
        info.ArgumentList.Add("-ExecutionPolicy");
        info.ArgumentList.Add("Bypass");
        info.ArgumentList.Add("-WindowStyle");
        info.ArgumentList.Add("Hidden");
        info.ArgumentList.Add("-File");
        info.ArgumentList.Add(scriptPath);

        Process.Start(info);
    }

    private static string Escape(string path) => path.Replace("'", "''", StringComparison.Ordinal);
}
