using System.Text.Json;
using FFBoost.Core.Models;

namespace FFBoost.Core.Services;

public class PerformanceSessionStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _statePath;

    public PerformanceSessionStateStore(string basePath)
    {
        var stateDirectory = Path.Combine(basePath, "state");
        Directory.CreateDirectory(stateDirectory);
        _statePath = Path.Combine(stateDirectory, "optimization-session.json");
    }

    public void Save(PerformanceSessionState state)
    {
        state.SavedAtUtc = DateTimeOffset.UtcNow;
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(_statePath, json);
    }

    public PerformanceSessionState? Load()
    {
        if (!File.Exists(_statePath))
            return null;

        try
        {
            var json = File.ReadAllText(_statePath);
            return JsonSerializer.Deserialize<PerformanceSessionState>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void Clear()
    {
        if (!File.Exists(_statePath))
            return;

        try
        {
            File.Delete(_statePath);
        }
        catch
        {
        }
    }

    public bool HasPendingState()
    {
        return File.Exists(_statePath);
    }
}
