namespace FFBoost.Core.Models;

public class MemoryOptimizationResult
{
    public string Profile { get; set; } = "Seguro";
    public bool Applied { get; set; }
    public int TrimmedProcessCount { get; set; }
    public double EstimatedFreedMb { get; set; }
    public double RamBeforeGb { get; set; }
    public double RamAfterGb { get; set; }
    public double RamUsageBeforePercent { get; set; }
    public double RamUsageAfterPercent { get; set; }
    public List<string> TrimmedProcesses { get; set; } = new();
}
