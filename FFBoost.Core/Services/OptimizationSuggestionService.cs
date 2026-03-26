using FFBoost.Core.Models;

namespace FFBoost.Core.Services;

public class OptimizationSuggestionService
{
    public List<string> BuildSuggestions(
        AppConfig config,
        IReadOnlyCollection<string> runningProcesses,
        IReadOnlyCollection<string> effectiveAllowedProcesses,
        bool recordingMode)
    {
        var suggestions = new List<string>();

        if (recordingMode)
            suggestions.Add("Modo gravacao ativo: prefira o perfil Seguro ou Forte para estabilidade da captura.");

        if (config.EnableFreeFireMode)
            suggestions.Add("Modo Free Fire ativo: a whitelist protege BlueStacks, Discord e gravadores autorizados.");

        if (runningProcesses.Contains("Discord", StringComparer.OrdinalIgnoreCase) &&
            !effectiveAllowedProcesses.Contains("Discord", StringComparer.OrdinalIgnoreCase))
        {
            suggestions.Add("Considere permitir o Discord se ele for essencial durante a partida.");
        }

        if (runningProcesses.Contains("steamwebhelper", StringComparer.OrdinalIgnoreCase))
            suggestions.Add("Feche o Steam overlay se nao estiver usando recursos sociais.");

        return suggestions;
    }
}
