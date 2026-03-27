namespace FFBoost.Core.Services;

public class AutomaticProfileService
{
    public string SelectProfile(double usedRamGb, double totalRamGb)
    {
        if (totalRamGb <= 0)
            return "Seguro";

        var ramUsagePercentage = (usedRamGb / totalRamGb) * 100d;

        if (ramUsagePercentage >= 80d)
            return "Ultra";

        if (ramUsagePercentage >= 60d)
            return "Forte";

        return "Seguro";
    }
}
