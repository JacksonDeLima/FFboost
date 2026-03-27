namespace FFBoost.Core.Models;

public class WindowsVisualEffectsSnapshot
{
    public bool? ClientAreaAnimation { get; set; }
    public bool? ComboBoxAnimation { get; set; }
    public bool? CursorShadow { get; set; }
    public bool? DropShadow { get; set; }
    public bool? HotTracking { get; set; }
    public bool? ListBoxSmoothScrolling { get; set; }
    public bool? MenuAnimation { get; set; }
    public bool? MenuFade { get; set; }
    public bool? SelectionFade { get; set; }
    public bool? ToolTipAnimation { get; set; }
    public bool? ToolTipFade { get; set; }
    public List<RegistryValueBackup> RegistryBackups { get; set; } = new();
}
