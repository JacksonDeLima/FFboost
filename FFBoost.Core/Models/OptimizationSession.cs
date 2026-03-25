using System.Diagnostics;

namespace FFBoost.Core.Models;

public class OptimizationSession
{
    public bool IsGameModeActive { get; set; }
    public string? PreviousPowerSchemeGuid { get; set; }
    public Dictionary<int, ProcessPriorityClass> ChangedPriorities { get; set; } = new();
    public List<string> KilledProcesses { get; set; } = new();
}
