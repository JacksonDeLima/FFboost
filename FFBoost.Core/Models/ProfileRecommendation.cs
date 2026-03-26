namespace FFBoost.Core.Models;

public class ProfileRecommendation
{
    public string RecommendedProfile { get; set; } = "Seguro";
    public bool UseFreeFirePreset { get; set; }
    public double Score { get; set; }
    public string Reason { get; set; } = "Sem historico suficiente.";
}
