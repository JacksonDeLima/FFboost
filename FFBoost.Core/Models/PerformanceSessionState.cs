using System.Diagnostics;

namespace FFBoost.Core.Models;

public class PerformanceSessionState
{
    public DateTimeOffset SavedAtUtc { get; set; }
    public string ActiveProfile { get; set; } = "Seguro";
    public string? PreviousPowerSchemeGuid { get; set; }
    public string? PreviousPowerSchemeName { get; set; }
    public bool HighPerformanceApplied { get; set; }
    public bool TurboModeApplied { get; set; }
    public RegistryValueBackup? TurboUserPreferencesMaskBackup { get; set; }
    public bool UltraVisualTweaksApplied { get; set; }
    public WindowsVisualEffectsSnapshot? VisualEffectsSnapshot { get; set; }
    public Dictionary<int, ProcessPriorityClass> ProcessPriorities { get; set; } = new();
}
