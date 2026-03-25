namespace FFBoost.Core.Models;

public class OptimizationResult
{
    public bool Success { get; init; }
    public bool GameModeActive { get; init; }
    public int KilledCount { get; init; }
    public int IgnoredCount { get; init; }
    public bool BlueStacksDetected { get; init; }
    public bool DiscordDetected { get; init; }
    public bool RecorderDetected { get; init; }
    public bool EmulatorPrioritized { get; init; }
    public bool PowerPlanChanged { get; init; }
    public string? PreviousPowerPlanName { get; init; }
    public IReadOnlyList<string> KilledProcesses { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> IgnoredProcesses { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> DetectedEmulators { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> DetectedRecorders { get; init; } = Array.Empty<string>();
    public string Message { get; init; } = string.Empty;
}
