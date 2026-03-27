namespace FFBoost.Core.Models;

public class TechnicalReport
{
    public string Profile { get; set; } = "Seguro";
    public bool FreeFireModeEnabled { get; set; }
    public bool RecordingModeDetected { get; set; }
    public bool TimerResolutionApplied { get; set; }
    public bool PowerPlanActivated { get; set; }
    public bool AffinityApplied { get; set; }
    public bool TurboModeApplied { get; set; }
    public bool UltraVisualTweaksApplied { get; set; }
    public bool MemoryOptimizationApplied { get; set; }
    public int KillPlanCount { get; set; }
    public int SuspendPlanCount { get; set; }
    public int KilledCount { get; set; }
    public int SuspendedCount { get; set; }
    public int MemoryOptimizedProcessCount { get; set; }
    public int OverlayCount { get; set; }
    public int ProcessesBefore { get; set; }
    public int ProcessesAfter { get; set; }
    public double CpuBefore { get; set; }
    public double CpuAfter { get; set; }
    public double RamBefore { get; set; }
    public double RamAfter { get; set; }
    public double RamUsageBeforePercent { get; set; }
    public double RamUsageAfterPercent { get; set; }
    public double MemoryRecoveredMb { get; set; }
    public double SessionScore { get; set; }
    public BenchmarkSummary Benchmark { get; set; } = new();
    public ProfileRecommendation Recommendation { get; set; } = new();
    public TimeSpan Elapsed { get; set; }
    public List<string> KilledProcesses { get; set; } = new();
    public List<string> SuspendedProcesses { get; set; } = new();
    public List<string> OverlayProcesses { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public List<string> PerformanceReport { get; set; } = new();
    public List<string> MemoryOptimizedProcesses { get; set; } = new();
    public List<ProcessResourceUsage> TopProcessesBefore { get; set; } = new();
    public List<ProcessResourceUsage> TopProcessesAfter { get; set; } = new();
}
