using FFBoost.Core.Models;
using FFBoost.Core.Services;

namespace FFBoost.Core.Tests;

public sealed class OptimizationPlanBuilderTests
{
    [Fact]
    public void Build_ForUltraAndRecording_MergesListsAndPreservesRecordingProcesses()
    {
        var config = new AppConfig
        {
            SelectedProfile = "Ultra",
            EnableFreeFireMode = true,
            AllowedProcesses = new List<string> { "discord" },
            FreeFireAllowedProcesses = new List<string> { "HD-Player" },
            SafeBlacklist = new List<string> { "chrome" },
            StrongBlacklist = new List<string> { "steam" },
            UltraBlacklist = new List<string> { "zoom" },
            FreeFireSafeBlacklist = new List<string> { "adobeipcbroker" },
            FreeFireStrongBlacklist = new List<string> { "riotclientservices" },
            FreeFireUltraBlacklist = new List<string> { "ea" }
        };

        var builder = new OptimizationPlanBuilder();
        var plan = builder.Build(config, recordingMode: true);

        Assert.Contains("chrome", plan.KillBlacklist);
        Assert.Contains("steam", plan.KillBlacklist);
        Assert.Contains("zoom", plan.KillBlacklist);
        Assert.Contains("adobeipcbroker", plan.KillBlacklist);
        Assert.Contains("riotclientservices", plan.KillBlacklist);
        Assert.Contains("ea", plan.KillBlacklist);
        Assert.Contains("discord", plan.EffectiveAllowedProcesses);
        Assert.Contains("HD-Player", plan.EffectiveAllowedProcesses);
        Assert.Contains("obs64", plan.EffectiveAllowedProcesses);
        Assert.DoesNotContain("obs64", plan.KillBlacklist);
    }
}
