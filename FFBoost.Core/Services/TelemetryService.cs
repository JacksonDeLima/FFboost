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
        entry.KilledProcesses = entry.KilledProcesses
            .Select(NormalizeProcessName)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        entry.RelaunchedProcesses = entry.RelaunchedProcesses
            .Select(NormalizeProcessName)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        history.Add(entry);

        if (history.Count > 50)
            history = history.OrderByDescending(static x => x.Timestamp).Take(50).OrderBy(static x => x.Timestamp).ToList();

        File.WriteAllText(_telemetryPath, JsonSerializer.Serialize(history, JsonOptions));
    }

    public List<TelemetryEntry> GetHistory()
    {
        return LoadHistory();
    }

    public BenchmarkSummary BuildBenchmarkSummary(string profile, bool freeFireModeEnabled, double currentScore)
    {
        var matchingHistory = LoadHistory()
            .Where(x =>
                x.Profile.Equals(profile, StringComparison.OrdinalIgnoreCase) &&
                x.FreeFireModeEnabled == freeFireModeEnabled)
            .OrderByDescending(x => x.Timestamp)
            .Take(20)
            .ToList();

        if (matchingHistory.Count == 0)
        {
            return new BenchmarkSummary
            {
                SessionCount = 0,
                LastScoreDelta = currentScore
            };
        }

        var avgCpuGain = matchingHistory.Average(x => x.CpuBefore - x.CpuAfter);
        var avgRamGain = matchingHistory.Average(x => x.RamBefore - x.RamAfter);
        var avgScore = matchingHistory.Average(x => x.SessionScore);

        return new BenchmarkSummary
        {
            SessionCount = matchingHistory.Count,
            AvgCpuGain = Math.Round(avgCpuGain, 2),
            AvgRamGain = Math.Round(avgRamGain, 2),
            AvgScore = Math.Round(avgScore, 2),
            LastScoreDelta = Math.Round(currentScore - avgScore, 2)
        };
    }

    public ProfileRecommendation GetRecommendedProfile(bool preferFreeFireMode)
    {
        var history = LoadHistory();
        var candidateGroups = history
            .Where(x => x.FreeFireModeEnabled == preferFreeFireMode)
            .GroupBy(x => x.Profile, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Profile = group.Key,
                Count = group.Count(),
                Score = group.Average(x => x.SessionScore),
                CpuGain = group.Average(x => x.CpuBefore - x.CpuAfter),
                RamGain = group.Average(x => x.RamBefore - x.RamAfter)
            })
            .Where(x => x.Count >= 2)
            .OrderByDescending(x => x.Score)
            .ToList();

        if (candidateGroups.Count == 0)
        {
            return new ProfileRecommendation
            {
                RecommendedProfile = "Seguro",
                UseFreeFirePreset = preferFreeFireMode,
                Reason = preferFreeFireMode
                    ? "Sem historico suficiente no preset Free Fire. Mantendo Seguro."
                    : "Sem historico suficiente. Mantendo Seguro."
            };
        }

        var best = candidateGroups[0];
        return new ProfileRecommendation
        {
            RecommendedProfile = best.Profile,
            UseFreeFirePreset = preferFreeFireMode,
            Score = Math.Round(best.Score, 2),
            Reason = $"Historico local: CPU {best.CpuGain:0.##}% e RAM {best.RamGain:0.##} GB com media {best.Score:0.##}."
        };
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
                var normalizedName = NormalizeProcessName(process);
                if (running.Contains(normalizedName))
                {
                    relaunchCounts[normalizedName] = relaunchCounts.TryGetValue(normalizedName, out var count) ? count + 1 : 1;
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

    private static string NormalizeProcessName(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return string.Empty;

        const string pidToken = " (PID ";
        var pidIndex = processName.IndexOf(pidToken, StringComparison.OrdinalIgnoreCase);
        return pidIndex > 0
            ? processName[..pidIndex].Trim()
            : processName.Trim();
    }
}
