using FFBoost.Core.Services;

namespace FFBoost.Core.Tests;

public sealed class AutomaticProfileServiceTests
{
    [Theory]
    [InlineData(13, 16, "Ultra")]
    [InlineData(10, 16, "Forte")]
    [InlineData(7, 16, "Seguro")]
    public void SelectProfile_UsesRamPressureThresholds(double usedRamGb, double totalRamGb, string expectedProfile)
    {
        var service = new AutomaticProfileService();

        var result = service.SelectProfile(usedRamGb, totalRamGb);

        Assert.Equal(expectedProfile, result);
    }
}
