namespace FFBoost.Core.Models;

public class ProcessResourceUsage
{
    public int ProcessId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public double CpuPercent { get; init; }
    public double RamMb { get; init; }
    public double DiskMbPerSecond { get; init; }
}
