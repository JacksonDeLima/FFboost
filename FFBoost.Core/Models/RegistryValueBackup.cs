using Microsoft.Win32;

namespace FFBoost.Core.Models;

public class RegistryValueBackup
{
    public string KeyPath { get; init; } = string.Empty;
    public string ValueName { get; init; } = string.Empty;
    public bool Exists { get; init; }
    public RegistryValueKind ValueKind { get; init; } = RegistryValueKind.Unknown;
    public string? StringValue { get; init; }
    public int? DwordValue { get; init; }
    public long? QwordValue { get; init; }
    public byte[]? BinaryValue { get; init; }
    public List<string>? MultiStringValue { get; init; }
}
