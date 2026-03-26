namespace FFBoost.Core.Models;

public class BenchmarkSummary
{
    public int SessionCount { get; set; }
    public double AvgCpuGain { get; set; }
    public double AvgRamGain { get; set; }
    public double AvgScore { get; set; }
    public double LastScoreDelta { get; set; }
}
