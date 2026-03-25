namespace FFBoost.Core.Models;

public class ProcessKillResult
{
    public List<string> KilledProcesses { get; set; } = new();
    public List<string> FailedProcesses { get; set; } = new();
}
