using System.Text.Json;
using FFBoost.Core.Models;

namespace FFBoost.Core.Services;

public class ConfigService
{
    private const int MaxBackupFiles = 12;
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
        TrimOldBackups(backupDirectory);
    }

    private static void TrimOldBackups(string backupDirectory)
    {
        var files = new DirectoryInfo(backupDirectory)
            .GetFiles("config-*.json.bak")
            .OrderByDescending(static file => file.CreationTimeUtc)
            .ToList();

        foreach (var file in files.Skip(MaxBackupFiles))
        {
            try
            {
                file.Delete();
            }
            catch
            {
            }
        }
    }

    private static void Normalize(AppConfig config)
    {
        config.AllowedProcesses = NormalizeList(config.AllowedProcesses);
        config.FreeFireAllowedProcesses = NormalizeList(config.FreeFireAllowedProcesses);
        config.SafeBlacklist = NormalizeList(config.SafeBlacklist);
        config.StrongBlacklist = NormalizeList(config.StrongBlacklist);
        config.UltraBlacklist = NormalizeList(config.UltraBlacklist);
        config.FreeFireSafeBlacklist = NormalizeList(config.FreeFireSafeBlacklist);
        config.FreeFireStrongBlacklist = NormalizeList(config.FreeFireStrongBlacklist);
        config.FreeFireUltraBlacklist = NormalizeList(config.FreeFireUltraBlacklist);
        config.RecordingProcesses = NormalizeList(config.RecordingProcesses);
        config.EmulatorProcesses = NormalizeList(config.EmulatorProcesses);
        config.SelectedProfile = string.IsNullOrWhiteSpace(config.SelectedProfile) ? "Seguro" : config.SelectedProfile.Trim();
        if (!IsKnownProfile(config.SelectedProfile))
            config.SelectedProfile = "Seguro";
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

    private static bool IsKnownProfile(string profile)
    {
        return profile.Equals("Seguro", StringComparison.OrdinalIgnoreCase) ||
               profile.Equals("Forte", StringComparison.OrdinalIgnoreCase) ||
               profile.Equals("Ultra", StringComparison.OrdinalIgnoreCase) ||
               profile.Equals("Auto", StringComparison.OrdinalIgnoreCase);
    }

    private static AppConfig CreateDefaultConfig()
    {
        return new AppConfig
        {
            AllowedProcesses =
            {
                "HD-Player",
                "HD-Frontend",
                "BlueStacks",
                "Bluestacks",
                "Discord",
                "obs64",
                "medal",
                "action",
                "streamlabsobs",
                "explorer",
                "dwm",
                "nvcontainer",
                "NVIDIA Share"
            },
            FreeFireAllowedProcesses =
            {
                "HD-Player",
                "HD-Frontend",
                "BlueStacks",
                "Bluestacks",
                "Discord",
                "obs64",
                "medal",
                "action",
                "streamlabsobs",
                "explorer",
                "dwm",
                "nvcontainer",
                "NVIDIA Share"
            },
            SafeBlacklist =
            {
                "chrome",
                "msedge",
                "firefox",
                "opera",
                "brave",
                "onedrive",
                "teams",
                "spotify",
                "telegram",
                "whatsapp",
                "notion"
            },
            FreeFireSafeBlacklist =
            {
                "chrome",
                "msedge",
                "spotify",
                "telegram",
                "whatsapp"
            },
            StrongBlacklist =
            {
                "steam",
                "steamwebhelper",
                "epicgameslauncher",
                "battle.net",
                "uplay",
                "upc",
                "zoom",
                "webex",
                "anydesk",
                "parsecd",
                "dropbox",
                "googledrivefs",
                "adobeipcbroker",
                "creative cloud",
                "ccxprocess"
            },
            FreeFireStrongBlacklist =
            {
                "steam",
                "epicgameslauncher",
                "battle.net",
                "uplay",
                "zoom",
                "webex"
            },
            UltraBlacklist =
            {
                "discordptb",
                "slack",
                "skype",
                "qbittorrent",
                "utorrent",
                "javaw",
                "pythonw",
                "acrobat",
                "winword",
                "excel",
                "powerpnt"
            },
            FreeFireUltraBlacklist =
            {
                "discordptb",
                "slack",
                "qbittorrent",
                "javaw",
                "pythonw"
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
            EnableTimerResolution = true,
            EnableAffinityTuning = true,
            EnableOverlayDetection = true,
            EnableWatcher = true,
            EnableTurboMode = true,
            TelemetryEnabled = true,
            AutoOptimizeOnStartup = true,
            LaunchOnWindowsStartup = true,
            StartupPreferenceInitialized = false,
            EnableFreeFireMode = true,
            SelectedProfile = "Forte"
        };
    }
}
