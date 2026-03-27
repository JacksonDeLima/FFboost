using FFBoost.Core.Rules;
using FFBoost.Core.Services;

namespace FFBoost.Core.Tests;

public sealed class MemoryOptimizerServiceTests
{
    [Theory]
    [InlineData("Seguro", 180, 6, false)]
    [InlineData("Forte", 110, 12, true)]
    [InlineData("Ultra", 60, 18, true)]
    public void GetPolicy_ReturnsExpectedThresholds(string profile, double minimumWorkingSetMb, int maxProcesses, bool compactCurrentProcess)
    {
        var service = new MemoryOptimizerService(new ProcessRules(), new SystemMetricsService());

        var policy = service.GetPolicy(profile);

        Assert.Equal(minimumWorkingSetMb, policy.MinimumWorkingSetMb);
        Assert.Equal(maxProcesses, policy.MaxProcesses);
        Assert.Equal(compactCurrentProcess, policy.CompactCurrentProcess);
    }
}
