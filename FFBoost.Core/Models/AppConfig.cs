namespace FFBoost.Core.Models;

public class AppConfig
{
    public List<string> AllowedProcesses { get; set; } = new();
    public List<string> FreeFireAllowedProcesses { get; set; } = new();
    public List<string> SafeBlacklist { get; set; } = new();
    public List<string> StrongBlacklist { get; set; } = new();
    public List<string> UltraBlacklist { get; set; } = new();
    public List<string> FreeFireSafeBlacklist { get; set; } = new();
    public List<string> FreeFireStrongBlacklist { get; set; } = new();
    public List<string> FreeFireUltraBlacklist { get; set; } = new();
    public List<string> RecordingProcesses { get; set; } = new();
    public List<string> EmulatorProcesses { get; set; } = new();
    public bool UseHighPerformancePlan { get; set; } = true;
    public bool SetEmulatorHighPriority { get; set; } = true;
    public bool EnableTimerResolution { get; set; } = true;
    public bool EnableAffinityTuning { get; set; } = true;
    public bool EnableOverlayDetection { get; set; } = true;
    public bool EnableWatcher { get; set; } = true;
    public bool TelemetryEnabled { get; set; } = true;
    public bool AutoOptimizeOnStartup { get; set; } = true;
    public bool EnableFreeFireMode { get; set; }
    public string SelectedProfile { get; set; } = "Seguro";
}
