using System.Text.Json;
using FFBoost.Core.Models;

namespace FFBoost.Core.Services;

public class TelemetryService
{
    private readonly string _telemetryPath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public TelemetryService(string basePath)
    {
        var telemetryDirectory = Path.Combine(basePath, "telemetry");
        Directory.CreateDirectory(telemetryDirectory);
        _telemetryPath = Path.Combine(telemetryDirectory, "history.json");
    }

    public void Append(TelemetryEntry entry)
    {
        var history = LoadHistory();
        history.Add(entry);

        if (history.Count > 50)
            history = history.OrderByDescending(static x => x.Timestamp).Take(50).OrderBy(static x => x.Timestamp).ToList();

        File.WriteAllText(_telemetryPath, JsonSerializer.Serialize(history, JsonOptions));
    }

    public List<string> GetWhitelistSuggestions(IEnumerable<string> runningProcesses)
    {
        var running = runningProcesses.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var history = LoadHistory();
        var relaunchCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in history)
        {
            foreach (var process in entry.KilledProcesses)
            {
                if (running.Contains(process))
                {
                    relaunchCounts[process] = relaunchCounts.TryGetValue(process, out var count) ? count + 1 : 1;
                }
            }
        }

        return relaunchCounts
            .Where(static x => x.Value >= 2)
            .OrderByDescending(static x => x.Value)
            .Select(static x => x.Key)
            .ToList();
    }

    private List<TelemetryEntry> LoadHistory()
    {
        if (!File.Exists(_telemetryPath))
            return new List<TelemetryEntry>();

        try
        {
            var json = File.ReadAllText(_telemetryPath);
            return JsonSerializer.Deserialize<List<TelemetryEntry>>(json) ?? new List<TelemetryEntry>();
        }
        catch
        {
            return new List<TelemetryEntry>();
        }
    }
}
