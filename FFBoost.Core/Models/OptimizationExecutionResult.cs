namespace FFBoost.Core.Models;

public class OptimizationExecutionResult
{
    public string Status { get; init; } = string.Empty;
    public List<string> Logs { get; init; } = new();
    public TechnicalReport Report { get; init; } = new();
    public string LogPath { get; init; } = string.Empty;
    public List<string> WhitelistSuggestions { get; init; } = new();
}
