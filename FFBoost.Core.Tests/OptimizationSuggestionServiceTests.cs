using FFBoost.Core.Models;
using FFBoost.Core.Services;

namespace FFBoost.Core.Tests;

public sealed class OptimizationSuggestionServiceTests
{
    [Fact]
    public void BuildSuggestions_IncludesRecordingFreeFireDiscordAndSteamHints()
    {
        var config = new AppConfig
        {
            EnableFreeFireMode = true
        };

        var service = new OptimizationSuggestionService();
        var suggestions = service.BuildSuggestions(
            config,
            new[] { "Discord", "steamwebhelper" },
            Array.Empty<string>(),
            recordingMode: true);

        Assert.Contains(suggestions, static x => x.Contains("Modo gravacao ativo", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(suggestions, static x => x.Contains("Modo Free Fire ativo", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(suggestions, static x => x.Contains("Discord", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(suggestions, static x => x.Contains("Steam overlay", StringComparison.OrdinalIgnoreCase));
    }
}
