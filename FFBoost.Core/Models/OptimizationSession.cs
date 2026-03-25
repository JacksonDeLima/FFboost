using System.Diagnostics;

namespace FFBoost.Core.Models;

public class OptimizationSession
{
    public bool IsGameModeActive { get; set; }
    public string? PreviousPowerSchemeGuid { get; set; }
    public Dictionary<int, ProcessPriorityClass> ChangedPriorities { get; set; } = new();
    public Dictionary<int, IntPtr> ChangedAffinities { get; set; } = new();
    public List<string> KilledProcesses { get; set; } = new();
    public Dictionary<int, string> SuspendedProcesses { get; set; } = new();
    public List<string> DetectedOverlays { get; set; } = new();
    public bool TimerResolutionApplied { get; set; }
    public string ActiveProfile { get; set; } = "Seguro";
}
