using System.Text.Json;
using FFBoost.Core.Models;

namespace FFBoost.Core.Services;

public class ConfigService
{
    private readonly string _configPath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public ConfigService(string configPath)
    {
        _configPath = configPath;
    }

    public AppConfig Load()
    {
        if (!File.Exists(_configPath))
        {
            var defaultConfig = CreateDefaultConfig();
            Save(defaultConfig);
            return defaultConfig;
        }

        var json = File.ReadAllText(_configPath);
        var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? CreateDefaultConfig();

        Normalize(config);
        return config;
    }

    public void Save(AppConfig config)
    {
        Normalize(config);
        BackupConfigIfExists();
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(_configPath, json);
    }

    private void BackupConfigIfExists()
    {
        if (!File.Exists(_configPath))
            return;

        var backupDirectory = Path.Combine(Path.GetDirectoryName(_configPath) ?? AppContext.BaseDirectory, "backups");
        Directory.CreateDirectory(backupDirectory);

        var backupFileName = $"config-{DateTime.Now:yyyyMMdd-HHmmss}.json.bak";
        var backupPath = Path.Combine(backupDirectory, backupFileName);
        File.Copy(_configPath, backupPath, overwrite: true);
    }

    private static void Normalize(AppConfig config)
    {
        config.AllowedProcesses = NormalizeList(config.AllowedProcesses);
        config.SafeBlacklist = NormalizeList(config.SafeBlacklist);
        config.StrongBlacklist = NormalizeList(config.StrongBlacklist);
        config.UltraBlacklist = NormalizeList(config.UltraBlacklist);
        config.RecordingProcesses = NormalizeList(config.RecordingProcesses);
        config.EmulatorProcesses = NormalizeList(config.EmulatorProcesses);
        config.SelectedProfile = string.IsNullOrWhiteSpace(config.SelectedProfile) ? "Seguro" : config.SelectedProfile.Trim();
    }

    private static List<string> NormalizeList(IEnumerable<string> items)
    {
        return items
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static AppConfig CreateDefaultConfig()
    {
        return new AppConfig
        {
            AllowedProcesses =
            {
                "HD-Player",
                "BlueStacks",
                "Bluestacks",
                "Discord",
                "obs64",
                "explorer",
                "dwm",
                "nvcontainer",
                "NVIDIA Share"
            },
            SafeBlacklist =
            {
                "chrome",
                "msedge",
                "opera",
                "firefox",
                "onedrive",
                "teams",
                "spotify",
                "brave"
            },
            StrongBlacklist =
            {
                "telegram",
                "whatsapp",
                "notion",
                "epicgameslauncher",
                "steam",
                "steamwebhelper"
            },
            UltraBlacklist =
            {
                "discordptb",
                "webex",
                "zoom",
                "uplay",
                "upc",
                "battle.net"
            },
            RecordingProcesses =
            {
                "obs64",
                "medal",
                "action",
                "streamlabsobs"
            },
            EmulatorProcesses =
            {
                "HD-Player",
                "HD-Frontend",
                "BlueStacks",
                "Bluestacks"
            },
            UseHighPerformancePlan = true,
            SetEmulatorHighPriority = true,
            AutoOptimizeOnStartup = true,
            SelectedProfile = "Seguro"
        };
    }
}
