namespace FFBoost.Core.Models;

public class RestoreExecutionResult
{
    public string Status { get; init; } = string.Empty;
    public List<string> Logs { get; init; } = new();
}
