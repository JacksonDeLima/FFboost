namespace FFBoost.Core.Models;

public class TelemetryEntry
{
    public DateTime Timestamp { get; set; }
    public string Profile { get; set; } = "Seguro";
    public bool FreeFireModeEnabled { get; set; }
    public double CpuBefore { get; set; }
    public double CpuAfter { get; set; }
    public double RamBefore { get; set; }
    public double RamAfter { get; set; }
    public double SessionScore { get; set; }
    public int KilledCount { get; set; }
    public int SuspendedCount { get; set; }
    public List<string> KilledProcesses { get; set; } = new();
    public List<string> RelaunchedProcesses { get; set; } = new();
}
