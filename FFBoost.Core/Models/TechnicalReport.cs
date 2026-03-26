namespace FFBoost.Core.Models;

public class TechnicalReport
{
    public string Profile { get; set; } = "Seguro";
    public bool FreeFireModeEnabled { get; set; }
    public bool RecordingModeDetected { get; set; }
    public bool TimerResolutionApplied { get; set; }
    public bool PowerPlanActivated { get; set; }
    public bool AffinityApplied { get; set; }
    public int KillPlanCount { get; set; }
    public int SuspendPlanCount { get; set; }
    public int KilledCount { get; set; }
    public int SuspendedCount { get; set; }
    public int OverlayCount { get; set; }
    public int ProcessesBefore { get; set; }
    public int ProcessesAfter { get; set; }
    public double CpuBefore { get; set; }
    public double CpuAfter { get; set; }
    public double RamBefore { get; set; }
    public double RamAfter { get; set; }
    public double SessionScore { get; set; }
    public BenchmarkSummary Benchmark { get; set; } = new();
    public ProfileRecommendation Recommendation { get; set; } = new();
    public TimeSpan Elapsed { get; set; }
    public List<string> KilledProcesses { get; set; } = new();
    public List<string> SuspendedProcesses { get; set; } = new();
    public List<string> OverlayProcesses { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
}
