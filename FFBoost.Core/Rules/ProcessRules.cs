using FFBoost.Core.Models;

namespace FFBoost.Core.Rules;

public class ProcessRules
{
    private readonly HashSet<string> _criticalProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "system",
        "idle",
        "registry",
        "wininit",
        "winlogon",
        "csrss",
        "services",
        "svchost",
        "lsass",
        "explorer",
        "dwm",
        "audiodg",
        "spoolsv",
        "fontdrvhost",
        "sihost",
        "taskhostw",
        "searchhost",
        "shellexperiencehost"
    };

    public bool IsCritical(string processName)
    {
        return _criticalProcesses.Contains(processName);
    }

    public bool IsAllowed(string processName, IEnumerable<string> allowed)
    {
        return allowed.Contains(processName, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsSafeToClose(string processName, IEnumerable<string> safeBlacklist)
    {
        return safeBlacklist.Contains(processName, StringComparer.OrdinalIgnoreCase);
    }

    public ProcessRiskLevel GetRiskLevel(string processName, IEnumerable<string> allowed, IEnumerable<string> blacklist)
    {
        if (IsCritical(processName))
            return ProcessRiskLevel.Risky;

        if (IsAllowed(processName, allowed))
            return ProcessRiskLevel.Optional;

        if (blacklist.Contains(processName, StringComparer.OrdinalIgnoreCase))
            return ProcessRiskLevel.Safe;

        return ProcessRiskLevel.Optional;
    }
}
