using Microsoft.Win32;
using System.Diagnostics;

namespace FFBoost.Core.Services;

public class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string LegacyRunEntryName = "FFBoost";
    private const string StartupTaskName = "FFBoost_AutoStart";
    private const string StartupDelay = "0000:15";

    public bool IsEnabled()
    {
        if (ScheduledTaskExists())
            return true;

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var value = key?.GetValue(LegacyRunEntryName) as string;
        return !string.IsNullOrWhiteSpace(value);
    }

    public bool SetEnabled(bool enabled)
    {
        return enabled ? EnableStartup() : DisableStartup();
    }

    private bool EnableStartup()
    {
        var executablePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrWhiteSpace(executablePath))
            return false;

        DeleteLegacyRunEntry();
        DeleteScheduledTaskIfExists();

        var startArgument = $"\"{executablePath}\" --tray";

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "schtasks",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.StartInfo.ArgumentList.Add("/Create");
        process.StartInfo.ArgumentList.Add("/F");
        process.StartInfo.ArgumentList.Add("/TN");
        process.StartInfo.ArgumentList.Add(StartupTaskName);
        process.StartInfo.ArgumentList.Add("/SC");
        process.StartInfo.ArgumentList.Add("ONLOGON");
        process.StartInfo.ArgumentList.Add("/RL");
        process.StartInfo.ArgumentList.Add("HIGHEST");
        process.StartInfo.ArgumentList.Add("/DELAY");
        process.StartInfo.ArgumentList.Add(StartupDelay);
        process.StartInfo.ArgumentList.Add("/TR");
        process.StartInfo.ArgumentList.Add(startArgument);

        process.Start();
        process.WaitForExit();
        return process.ExitCode == 0 && ScheduledTaskExists();
    }

    private bool DisableStartup()
    {
        DeleteLegacyRunEntry();
        DeleteScheduledTaskIfExists();
        return !IsEnabled();
    }

    private static bool ScheduledTaskExists()
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "schtasks",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.StartInfo.ArgumentList.Add("/Query");
        process.StartInfo.ArgumentList.Add("/TN");
        process.StartInfo.ArgumentList.Add(StartupTaskName);
        process.Start();
        process.WaitForExit();
        return process.ExitCode == 0;
    }

    private static void DeleteScheduledTaskIfExists()
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "schtasks",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.StartInfo.ArgumentList.Add("/Delete");
        process.StartInfo.ArgumentList.Add("/TN");
        process.StartInfo.ArgumentList.Add(StartupTaskName);
        process.StartInfo.ArgumentList.Add("/F");
        process.Start();
        process.WaitForExit();
    }

    private static void DeleteLegacyRunEntry()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key?.DeleteValue(LegacyRunEntryName, throwOnMissingValue: false);
    }
}
