using FFBoost.Core.Models;
using FFBoost.Core.Services;

namespace FFBoost.Core.Tests;

public sealed class TelemetryServiceTests : IDisposable
{
    private readonly string _basePath = Path.Combine(Path.GetTempPath(), "ffboost-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void GetWhitelistSuggestions_NormalizesNamesWithPid()
    {
        Directory.CreateDirectory(_basePath);
        var service = new TelemetryService(_basePath);

        service.Append(new TelemetryEntry
        {
            Timestamp = DateTime.UtcNow.AddMinutes(-2),
            Profile = "Seguro",
            KilledProcesses = new List<string> { "chrome (PID 100)" }
        });

        service.Append(new TelemetryEntry
        {
            Timestamp = DateTime.UtcNow.AddMinutes(-1),
            Profile = "Seguro",
            KilledProcesses = new List<string> { "chrome (PID 200)" }
        });

        var suggestions = service.GetWhitelistSuggestions(new[] { "chrome", "discord" });

        Assert.Contains("chrome", suggestions);
    }

    public void Dispose()
    {
        if (Directory.Exists(_basePath))
            Directory.Delete(_basePath, recursive: true);
    }
}
