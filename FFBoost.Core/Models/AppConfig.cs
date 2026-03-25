namespace FFBoost.Core.Models;

public class AppConfig
{
    public List<string> AllowedProcesses { get; set; } = new();
    public List<string> SafeBlacklist { get; set; } = new();
    public List<string> StrongBlacklist { get; set; } = new();
    public List<string> UltraBlacklist { get; set; } = new();
    public List<string> RecordingProcesses { get; set; } = new();
    public List<string> EmulatorProcesses { get; set; } = new();
    public bool UseHighPerformancePlan { get; set; } = true;
    public bool SetEmulatorHighPriority { get; set; } = true;
    public bool AutoOptimizeOnStartup { get; set; } = true;
    public string SelectedProfile { get; set; } = "Seguro";
}
