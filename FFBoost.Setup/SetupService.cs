using System.Diagnostics;
using System.Reflection;

namespace FFBoost.Setup;

internal sealed class SetupService
{
    private readonly string _targetDir;

    public SetupService(string targetDir)
    {
        _targetDir = targetDir;
    }

    public SetupOperationResult Install(Assembly assembly)
    {
        try
        {
            Directory.CreateDirectory(_targetDir);
            TryStopRunningApp();

            ExtractResource(assembly, "Payload.FFBoost.exe", Path.Combine(_targetDir, "FFBoost.exe"));
            ExtractResource(assembly, "Payload.config.json", Path.Combine(_targetDir, "config.json"));
            CreateDesktopShortcut();

            var exePath = Path.Combine(_targetDir, "FFBoost.exe");
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = _targetDir,
                UseShellExecute = true
            });

            return new SetupOperationResult
            {
                Success = true,
                Messages =
                {
                    "Instalacao concluida.",
                    $"Destino: {_targetDir}",
                    $"App: {exePath}",
                    "Atalho criado na area de trabalho."
                }
            };
        }
        catch (Exception ex)
        {
            return new SetupOperationResult
            {
                Success = false,
                Messages =
                {
                    "Falha na instalacao.",
                    ex.Message
                }
            };
        }
    }

    public SetupOperationResult Uninstall()
    {
        try
        {
            TryStopRunningApp();

            var desktopShortcut = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "FF Boost.lnk");

            if (File.Exists(desktopShortcut))
                File.Delete(desktopShortcut);

            if (Directory.Exists(_targetDir))
                Directory.Delete(_targetDir, recursive: true);

            return new SetupOperationResult
            {
                Success = true,
                Messages =
                {
                    "Desinstalacao concluida.",
                    "Arquivos removidos de %LocalAppData%\\FFBoost."
                }
            };
        }
        catch (Exception ex)
        {
            return new SetupOperationResult
            {
                Success = false,
                Messages =
                {
                    "Falha na desinstalacao.",
                    ex.Message
                }
            };
        }
    }

    private void TryStopRunningApp()
    {
        var targetExe = Path.Combine(_targetDir, "FFBoost.exe");

        foreach (var process in Process.GetProcessesByName("FFBoost"))
        {
            try
            {
                if (process.HasExited)
                    continue;

                var processPath = process.MainModule?.FileName;
                if (!string.Equals(processPath, targetExe, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    process.CloseMainWindow();
                    if (process.WaitForExit(3000))
                        continue;
                }

                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
            catch
            {
            }
        }
    }

    private static void ExtractResource(Assembly assembly, string resourceName, string outputPath)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Recurso nao encontrado: {resourceName}");
        using var file = File.Create(outputPath);
        stream.CopyTo(file);
    }

    private void CreateDesktopShortcut()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var shortcutPath = Path.Combine(desktop, "FF Boost.lnk");
        var targetExe = Path.Combine(_targetDir, "FFBoost.exe");

        var shell = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell indisponivel.");

        dynamic shellObject = Activator.CreateInstance(shell)
            ?? throw new InvalidOperationException("Nao foi possivel criar o shell.");

        try
        {
            dynamic shortcut = shellObject.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetExe;
            shortcut.WorkingDirectory = _targetDir;
            shortcut.IconLocation = targetExe;
            shortcut.Save();
        }
        finally
        {
            if (shellObject is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
