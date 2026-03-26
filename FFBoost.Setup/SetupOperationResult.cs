namespace FFBoost.Setup;

internal sealed class SetupOperationResult
{
    public bool Success { get; init; }
    public List<string> Messages { get; init; } = new();
    public string DisplayText => string.Join(Environment.NewLine, Messages);
}
