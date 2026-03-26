namespace FFBoost.Core.Models;

public class OptimizationPlan
{
    public bool RecordingModeDetected { get; init; }
    public List<string> EffectiveAllowedProcesses { get; init; } = new();
    public List<string> KillBlacklist { get; init; } = new();
    public List<string> SuspendBlacklist { get; init; } = new();
}
