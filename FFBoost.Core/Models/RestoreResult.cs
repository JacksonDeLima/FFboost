namespace FFBoost.Core.Models;

public class RestoreResult
{
    public bool Success { get; init; }
    public bool PowerPlanRestored { get; init; }
    public bool PriorityRestored { get; init; }
    public bool GameModeWasActive { get; init; }
    public string Message { get; init; } = string.Empty;
}
