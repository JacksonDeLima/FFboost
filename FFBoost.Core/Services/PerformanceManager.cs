using System.Diagnostics;
using System.Text.RegularExpressions;
using FFBoost.Core.Models;

namespace FFBoost.Core.Services;

public class PerformanceManager
{
    public void SetHighPriority(IEnumerable<Process> processes, OptimizationSession session)
    {
        foreach (var process in processes)
        {
            try
            {
                if (process.HasExited)
                    continue;

                if (!session.ChangedPriorities.ContainsKey(process.Id))
                    session.ChangedPriorities[process.Id] = process.PriorityClass;

                process.PriorityClass = ProcessPriorityClass.High;
            }
            catch
            {
            }
        }
    }

    public string? GetActivePowerSchemeGuid()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = "/getactivescheme",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null)
                return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var match = Regex.Match(output, @"([a-fA-F0-9\-]{36})");
            return match.Success ? match.Value : null;
        }
        catch
        {
            return null;
        }
    }

    public void EnableHighPerformancePowerPlan(OptimizationSession session)
    {
        try
        {
            session.PreviousPowerSchemeGuid ??= GetActivePowerSchemeGuid();

            Process.Start(new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = "/setactive SCHEME_MIN",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        catch
        {
        }
    }

    public void RestorePowerPlan(OptimizationSession session)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(session.PreviousPowerSchemeGuid))
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = $"/setactive {session.PreviousPowerSchemeGuid}",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        catch
        {
        }
    }

    public void RestorePriorities(OptimizationSession session)
    {
        foreach (var item in session.ChangedPriorities)
        {
            try
            {
                var process = Process.GetProcessById(item.Key);
                if (!process.HasExited)
                    process.PriorityClass = item.Value;
            }
            catch
            {
            }
        }
    }
}
