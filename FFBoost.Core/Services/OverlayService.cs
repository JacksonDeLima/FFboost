using System.Diagnostics;

namespace FFBoost.Core.Services;

public class OverlayService
{
    private static readonly string[] KnownOverlayProcesses =
    {
        "GameBar",
        "GameBarFTServer",
        "NVIDIA Share",
        "NVIDIA Web Helper",
        "steam",
        "steamwebhelper",
        "Discord",
        "DiscordPTB",
        "Overwolf",
        "RTSS"
    };

    public List<string> DetectOverlays()
    {
        var processes = Process.GetProcesses();

        return processes
            .Select(static p => p.ProcessName)
            .Where(name => KnownOverlayProcesses.Contains(name, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
