using FFBoost.Core.Models;

namespace FFBoost.Core.Services;

public class OptimizationPlanBuilder
{
    private static readonly HashSet<string> PreserveDuringRecording = new(StringComparer.OrdinalIgnoreCase)
    {
        "discord",
        "discordptb",
        "obs64",
        "medal",
        "action",
        "streamlabsobs"
    };

    public OptimizationPlan Build(AppConfig config, bool recordingMode)
    {
        return new OptimizationPlan
        {
            RecordingModeDetected = recordingMode,
            EffectiveAllowedProcesses = GetAllowedProcesses(config, recordingMode),
            KillBlacklist = GetKillBlacklistByProfile(config, recordingMode),
            SuspendBlacklist = GetSuspendBlacklistByProfile(config, recordingMode)
        };
    }

    private static List<string> GetKillBlacklistByProfile(AppConfig config, bool recordingMode)
    {
        var result = new List<string>(config.SafeBlacklist);

        if (config.EnableFreeFireMode)
            result.AddRange(config.FreeFireSafeBlacklist);

        if (config.SelectedProfile.Equals("Forte", StringComparison.OrdinalIgnoreCase) ||
            config.SelectedProfile.Equals("Ultra", StringComparison.OrdinalIgnoreCase))
        {
            result.AddRange(config.StrongBlacklist);

            if (config.EnableFreeFireMode)
                result.AddRange(config.FreeFireStrongBlacklist);
        }

        if (config.SelectedProfile.Equals("Ultra", StringComparison.OrdinalIgnoreCase))
        {
            result.AddRange(config.UltraBlacklist);

            if (config.EnableFreeFireMode)
                result.AddRange(config.FreeFireUltraBlacklist);
        }

        if (recordingMode)
            result.RemoveAll(ShouldPreserveWhileRecording);

        return result
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> GetSuspendBlacklistByProfile(AppConfig config, bool recordingMode)
    {
        var result = new List<string>();

        if (recordingMode)
            result.RemoveAll(ShouldPreserveWhileRecording);

        return result
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> GetAllowedProcesses(AppConfig config, bool recordingMode)
    {
        var result = new List<string>(config.AllowedProcesses);

        if (config.EnableFreeFireMode)
            result.AddRange(config.FreeFireAllowedProcesses);

        if (recordingMode)
            result.AddRange(PreserveDuringRecording);

        return result
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool ShouldPreserveWhileRecording(string processName)
    {
        return PreserveDuringRecording.Contains(processName);
    }
}
