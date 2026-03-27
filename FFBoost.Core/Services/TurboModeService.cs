using System.Diagnostics;
using FFBoost.Core.Models;
using Microsoft.Win32;

namespace FFBoost.Core.Services;

public class TurboModeService
{
    private const string DesktopKeyPath = @"Control Panel\Desktop";
    private const string UserPreferencesMaskValueName = "UserPreferencesMask";
    private static readonly byte[] TurboPreferencesMask = Convert.FromHexString("9012038010000000");

    public bool ApplyTurboMode(OptimizationSession session)
    {
        var applied = false;

        if (!session.TurboModeApplied)
        {
            session.TurboUserPreferencesMaskBackup = CaptureRegistryValueBackup();

            if (SetTurboPreferencesMask())
            {
                session.TurboModeApplied = true;
                applied = true;
            }
        }

        try
        {
            var currentProcess = Process.GetCurrentProcess();
            session.PreviousCurrentProcessPriority ??= currentProcess.PriorityClass;
            currentProcess.PriorityClass = ProcessPriorityClass.High;
            applied = true;
        }
        catch
        {
        }

        return applied;
    }

    public void RestoreTurboMode(OptimizationSession session)
    {
        RestoreRegistryValue(session.TurboUserPreferencesMaskBackup);

        try
        {
            if (session.PreviousCurrentProcessPriority.HasValue)
                Process.GetCurrentProcess().PriorityClass = session.PreviousCurrentProcessPriority.Value;
        }
        catch
        {
        }

        session.TurboModeApplied = false;
        session.TurboUserPreferencesMaskBackup = null;
        session.PreviousCurrentProcessPriority = null;
    }

    public void RestoreTurboMode(PerformanceSessionState state)
    {
        RestoreRegistryValue(state.TurboUserPreferencesMaskBackup);
    }

    private static RegistryValueBackup CaptureRegistryValueBackup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(DesktopKeyPath, writable: false);
            if (key is null || Array.IndexOf(key.GetValueNames(), UserPreferencesMaskValueName) < 0)
            {
                return new RegistryValueBackup
                {
                    KeyPath = DesktopKeyPath,
                    ValueName = UserPreferencesMaskValueName,
                    Exists = false
                };
            }

            var value = key.GetValue(UserPreferencesMaskValueName);
            var kind = key.GetValueKind(UserPreferencesMaskValueName);

            return new RegistryValueBackup
            {
                KeyPath = DesktopKeyPath,
                ValueName = UserPreferencesMaskValueName,
                Exists = true,
                ValueKind = kind,
                BinaryValue = value as byte[]
            };
        }
        catch
        {
            return new RegistryValueBackup
            {
                KeyPath = DesktopKeyPath,
                ValueName = UserPreferencesMaskValueName,
                Exists = false
            };
        }
    }

    private static bool SetTurboPreferencesMask()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(DesktopKeyPath, writable: true);
            if (key is null)
                return false;

            key.SetValue(UserPreferencesMaskValueName, TurboPreferencesMask, RegistryValueKind.Binary);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void RestoreRegistryValue(RegistryValueBackup? backup)
    {
        if (backup is null)
            return;

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(backup.KeyPath, writable: true);
            if (key is null)
                return;

            if (!backup.Exists)
            {
                key.DeleteValue(backup.ValueName, false);
                return;
            }

            if (backup.BinaryValue is not null)
                key.SetValue(backup.ValueName, backup.BinaryValue, RegistryValueKind.Binary);
        }
        catch
        {
        }
    }
}
