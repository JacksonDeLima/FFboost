using System.Diagnostics;

namespace FFBoost.Core.Models;

public class PerformanceSessionState
{
    public string? PreviousPowerSchemeGuid { get; set; }
    public string? PreviousPowerSchemeName { get; set; }
    public bool HighPerformanceApplied { get; set; }
    public Dictionary<int, ProcessPriorityClass> ProcessPriorities { get; set; } = new();
}
