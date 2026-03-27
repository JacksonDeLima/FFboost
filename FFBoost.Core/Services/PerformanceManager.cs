using System.Diagnostics;
using System.Text.RegularExpressions;
using FFBoost.Core.Models;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace FFBoost.Core.Services;

public class PerformanceManager
{
    private const uint SpiGetMenuAnimation = 0x1002;
    private const uint SpiSetMenuAnimation = 0x1003;
    private const uint SpiGetComboBoxAnimation = 0x1004;
    private const uint SpiSetComboBoxAnimation = 0x1005;
    private const uint SpiGetListBoxSmoothScrolling = 0x1006;
    private const uint SpiSetListBoxSmoothScrolling = 0x1007;
    private const uint SpiGetHotTracking = 0x100E;
    private const uint SpiSetHotTracking = 0x100F;
    private const uint SpiGetMenuFade = 0x1012;
    private const uint SpiSetMenuFade = 0x1013;
    private const uint SpiGetSelectionFade = 0x1014;
    private const uint SpiSetSelectionFade = 0x1015;
    private const uint SpiGetToolTipAnimation = 0x1016;
    private const uint SpiSetToolTipAnimation = 0x1017;
    private const uint SpiGetToolTipFade = 0x1018;
    private const uint SpiSetToolTipFade = 0x1019;
    private const uint SpiGetCursorShadow = 0x101A;
    private const uint SpiSetCursorShadow = 0x101B;
    private const uint SpiGetClientAreaAnimation = 0x1042;
    private const uint SpiSetClientAreaAnimation = 0x1043;
    private const uint SpiGetDropShadow = 0x1024;
    private const uint SpiSetDropShadow = 0x1025;
    private const uint SpifUpdateIniFile = 0x01;
    private const uint SpifSendChange = 0x02;
    private const int HwndBroadcast = 0xffff;
    private const int WmSettingChange = 0x001A;

    private static readonly (string KeyPath, string ValueName, object Value, RegistryValueKind ValueKind)[] UltraRegistryTweaks =
    {
        (@"Control Panel\Desktop", "DragFullWindows", "0", RegistryValueKind.String),
        (@"Control Panel\Desktop", "FontSmoothing", "0", RegistryValueKind.String),
        (@"Control Panel\Desktop", "MenuShowDelay", "0", RegistryValueKind.String),
        (@"Control Panel\Desktop\WindowMetrics", "MinAnimate", "0", RegistryValueKind.String),
        (@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ListviewAlphaSelect", 0, RegistryValueKind.DWord),
        (@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ListviewShadow", 0, RegistryValueKind.DWord),
        (@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations", 0, RegistryValueKind.DWord),
        (@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2, RegistryValueKind.DWord),
        (@"Software\Microsoft\Windows\DWM", "ColorPrevalence", 0, RegistryValueKind.DWord),
        (@"Software\Microsoft\Windows\DWM", "EnableAeroPeek", 0, RegistryValueKind.DWord)
    };

    public void SetHighPriority(IEnumerable<Process> processes, OptimizationSession session)
    {
        foreach (var process in processes)
        {
            try
            {
                if (process.HasExited)
                    continue;

                if (!session.ChangedPriorities.ContainsKey(process.Id))
                    session.ChangedPriorities[process.Id] = process.PriorityClass;

                process.PriorityClass = ProcessPriorityClass.High;

                foreach (ProcessThread thread in process.Threads)
                {
                    try
                    {
                        thread.PriorityLevel = ThreadPriorityLevel.AboveNormal;
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }
    }

    public bool SetEmulatorAffinity(IEnumerable<Process> processes, OptimizationSession session)
    {
        var applied = false;
        var mask = GetPreferredAffinityMask();

        if (mask == IntPtr.Zero)
            return false;

        foreach (var process in processes)
        {
            try
            {
                if (process.HasExited)
                    continue;

                if (!session.ChangedAffinities.ContainsKey(process.Id))
                    session.ChangedAffinities[process.Id] = process.ProcessorAffinity;

                process.ProcessorAffinity = mask;
                applied = true;
            }
            catch
            {
            }
        }

        return applied;
    }

    public void RestorePriorities(OptimizationSession session)
    {
        foreach (var item in session.ChangedPriorities)
        {
            try
            {
                var process = Process.GetProcessById(item.Key);
                if (!process.HasExited)
                    process.PriorityClass = item.Value;
            }
            catch
            {
            }
        }
    }

    public void RestoreAffinities(OptimizationSession session)
    {
        foreach (var item in session.ChangedAffinities)
        {
            try
            {
                var process = Process.GetProcessById(item.Key);
                if (!process.HasExited)
                    process.ProcessorAffinity = item.Value;
            }
            catch
            {
            }
        }
    }

    public string? GetActivePowerSchemeGuid()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = "/getactivescheme",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null)
                return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var match = Regex.Match(output, @"([a-fA-F0-9\-]{36})");
            return match.Success ? match.Value : null;
        }
        catch
        {
            return null;
        }
    }

    public void EnableHighPerformancePowerPlan(OptimizationSession session)
    {
        try
        {
            session.PreviousPowerSchemeGuid ??= GetActivePowerSchemeGuid();

            Process.Start(new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = "/setactive SCHEME_MIN",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        catch
        {
        }
    }

    public void RestorePowerPlan(OptimizationSession session)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(session.PreviousPowerSchemeGuid))
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = $"/setactive {session.PreviousPowerSchemeGuid}",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        catch
        {
        }
    }

    public void RestorePowerPlan(PerformanceSessionState state)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(state.PreviousPowerSchemeGuid))
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = $"/setactive {state.PreviousPowerSchemeGuid}",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        catch
        {
        }
    }

    public bool ApplyUltraVisualTweaks(OptimizationSession session)
    {
        if (session.UltraVisualTweaksApplied)
            return true;

        var snapshot = new WindowsVisualEffectsSnapshot
        {
            ClientAreaAnimation = GetSystemFlag(SpiGetClientAreaAnimation),
            ComboBoxAnimation = GetSystemFlag(SpiGetComboBoxAnimation),
            CursorShadow = GetSystemFlag(SpiGetCursorShadow),
            DropShadow = GetSystemFlag(SpiGetDropShadow),
            HotTracking = GetSystemFlag(SpiGetHotTracking),
            ListBoxSmoothScrolling = GetSystemFlag(SpiGetListBoxSmoothScrolling),
            MenuAnimation = GetSystemFlag(SpiGetMenuAnimation),
            MenuFade = GetSystemFlag(SpiGetMenuFade),
            SelectionFade = GetSystemFlag(SpiGetSelectionFade),
            ToolTipAnimation = GetSystemFlag(SpiGetToolTipAnimation),
            ToolTipFade = GetSystemFlag(SpiGetToolTipFade)
        };

        snapshot.RegistryBackups = CaptureRegistryBackups(UltraRegistryTweaks);
        session.VisualEffectsSnapshot = snapshot;

        var applied = false;

        foreach (var tweak in UltraRegistryTweaks)
            applied |= SetRegistryValue(tweak.KeyPath, tweak.ValueName, tweak.Value, tweak.ValueKind);

        applied |= SetSystemFlag(SpiSetClientAreaAnimation, false);
        applied |= SetSystemFlag(SpiSetComboBoxAnimation, false);
        applied |= SetSystemFlag(SpiSetCursorShadow, false);
        applied |= SetSystemFlag(SpiSetDropShadow, false);
        applied |= SetSystemFlag(SpiSetHotTracking, false);
        applied |= SetSystemFlag(SpiSetListBoxSmoothScrolling, false);
        applied |= SetSystemFlag(SpiSetMenuAnimation, false);
        applied |= SetSystemFlag(SpiSetMenuFade, false);
        applied |= SetSystemFlag(SpiSetSelectionFade, false);
        applied |= SetSystemFlag(SpiSetToolTipAnimation, false);
        applied |= SetSystemFlag(SpiSetToolTipFade, false);

        if (applied)
        {
            BroadcastSettingsChanged();
            session.UltraVisualTweaksApplied = true;
        }

        return applied;
    }

    public void RestoreUltraVisualTweaks(OptimizationSession session)
    {
        if (!session.UltraVisualTweaksApplied || session.VisualEffectsSnapshot is null)
            return;

        RestoreUltraVisualTweaks(session.VisualEffectsSnapshot);
        session.UltraVisualTweaksApplied = false;
        session.VisualEffectsSnapshot = null;
    }

    public void RestoreUltraVisualTweaks(PerformanceSessionState state)
    {
        if (!state.UltraVisualTweaksApplied || state.VisualEffectsSnapshot is null)
            return;

        RestoreUltraVisualTweaks(state.VisualEffectsSnapshot);
    }

    private static void RestoreUltraVisualTweaks(WindowsVisualEffectsSnapshot snapshot)
    {
        RestoreSystemFlag(SpiSetClientAreaAnimation, snapshot.ClientAreaAnimation);
        RestoreSystemFlag(SpiSetComboBoxAnimation, snapshot.ComboBoxAnimation);
        RestoreSystemFlag(SpiSetCursorShadow, snapshot.CursorShadow);
        RestoreSystemFlag(SpiSetDropShadow, snapshot.DropShadow);
        RestoreSystemFlag(SpiSetHotTracking, snapshot.HotTracking);
        RestoreSystemFlag(SpiSetListBoxSmoothScrolling, snapshot.ListBoxSmoothScrolling);
        RestoreSystemFlag(SpiSetMenuAnimation, snapshot.MenuAnimation);
        RestoreSystemFlag(SpiSetMenuFade, snapshot.MenuFade);
        RestoreSystemFlag(SpiSetSelectionFade, snapshot.SelectionFade);
        RestoreSystemFlag(SpiSetToolTipAnimation, snapshot.ToolTipAnimation);
        RestoreSystemFlag(SpiSetToolTipFade, snapshot.ToolTipFade);

        foreach (var backup in snapshot.RegistryBackups)
            RestoreRegistryValue(backup);

        BroadcastSettingsChanged();
    }

    private static IntPtr GetPreferredAffinityMask()
    {
        var processorCount = Environment.ProcessorCount;
        if (processorCount <= 1)
            return IntPtr.Zero;

        long mask = 0;
        for (var i = 0; i < processorCount; i += 2)
            mask |= 1L << i;

        if (mask == 0)
            mask = (1L << Math.Min(processorCount, 1)) - 1;

        return new IntPtr(mask);
    }

    private static List<RegistryValueBackup> CaptureRegistryBackups(
        IEnumerable<(string KeyPath, string ValueName, object Value, RegistryValueKind ValueKind)> tweaks)
    {
        var backups = new List<RegistryValueBackup>();

        foreach (var tweak in tweaks)
        {
            using var key = Registry.CurrentUser.OpenSubKey(tweak.KeyPath, writable: false);

            if (key is null || Array.IndexOf(key.GetValueNames(), tweak.ValueName) < 0)
            {
                backups.Add(new RegistryValueBackup
                {
                    KeyPath = tweak.KeyPath,
                    ValueName = tweak.ValueName,
                    Exists = false
                });
                continue;
            }

            var originalValue = key.GetValue(tweak.ValueName);
            var originalKind = key.GetValueKind(tweak.ValueName);

            backups.Add(new RegistryValueBackup
            {
                KeyPath = tweak.KeyPath,
                ValueName = tweak.ValueName,
                Exists = true,
                ValueKind = originalKind,
                StringValue = originalKind is RegistryValueKind.String or RegistryValueKind.ExpandString
                    ? originalValue?.ToString()
                    : null,
                DwordValue = originalKind == RegistryValueKind.DWord && originalValue is int dword ? dword : null,
                QwordValue = originalKind == RegistryValueKind.QWord && originalValue is long qword ? qword : null,
                BinaryValue = originalKind == RegistryValueKind.Binary && originalValue is byte[] binary ? binary.ToArray() : null,
                MultiStringValue = originalKind == RegistryValueKind.MultiString && originalValue is string[] multi
                    ? multi.ToList()
                    : null
            });
        }

        return backups;
    }

    private static bool SetRegistryValue(string keyPath, string valueName, object value, RegistryValueKind valueKind)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(keyPath, writable: true);
            if (key is null)
                return false;

            key.SetValue(valueName, value, valueKind);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void RestoreRegistryValue(RegistryValueBackup backup)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(backup.KeyPath, writable: true);
            if (key is null)
                return;

            if (!backup.Exists)
            {
                key.DeleteValue(backup.ValueName, throwOnMissingValue: false);
                return;
            }

            switch (backup.ValueKind)
            {
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    key.SetValue(backup.ValueName, backup.StringValue ?? string.Empty, backup.ValueKind);
                    break;
                case RegistryValueKind.DWord when backup.DwordValue.HasValue:
                    key.SetValue(backup.ValueName, backup.DwordValue.Value, backup.ValueKind);
                    break;
                case RegistryValueKind.QWord when backup.QwordValue.HasValue:
                    key.SetValue(backup.ValueName, backup.QwordValue.Value, backup.ValueKind);
                    break;
                case RegistryValueKind.Binary when backup.BinaryValue is not null:
                    key.SetValue(backup.ValueName, backup.BinaryValue, backup.ValueKind);
                    break;
                case RegistryValueKind.MultiString when backup.MultiStringValue is not null:
                    key.SetValue(backup.ValueName, backup.MultiStringValue.ToArray(), backup.ValueKind);
                    break;
            }
        }
        catch
        {
        }
    }

    private static bool? GetSystemFlag(uint action)
    {
        try
        {
            var value = false;
            return SystemParametersInfo(action, 0, ref value, 0) ? value : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool SetSystemFlag(uint action, bool enabled)
    {
        try
        {
            var value = enabled;
            return SystemParametersInfo(action, 0, ref value, SpifUpdateIniFile | SpifSendChange);
        }
        catch
        {
            return false;
        }
    }

    private static void RestoreSystemFlag(uint action, bool? previousValue)
    {
        if (!previousValue.HasValue)
            return;

        SetSystemFlag(action, previousValue.Value);
    }

    private static void BroadcastSettingsChanged()
    {
        try
        {
            _ = SendMessageTimeout(
                new IntPtr(HwndBroadcast),
                WmSettingChange,
                IntPtr.Zero,
                "UserPreferences",
                0,
                100,
                out _);
        }
        catch
        {
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref bool pvParam, uint fWinIni);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        string lParam,
        uint fuFlags,
        uint uTimeout,
        out IntPtr lpdwResult);
}
