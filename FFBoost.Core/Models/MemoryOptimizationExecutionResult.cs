namespace FFBoost.Core.Models;

public class MemoryOptimizationExecutionResult
{
    public string Status { get; init; } = string.Empty;
    public List<string> Logs { get; init; } = new();
    public MemoryOptimizationResult Result { get; init; } = new();
}
