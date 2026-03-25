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

                foreach (ProcessThread thread in process.Threads)
                {
                    try
                    {
                        thread.PriorityLevel = ThreadPriorityLevel.AboveNormal;
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }
    }

    public bool SetEmulatorAffinity(IEnumerable<Process> processes, OptimizationSession session)
    {
        var applied = false;
        var mask = GetPreferredAffinityMask();

        if (mask == IntPtr.Zero)
            return false;

        foreach (var process in processes)
        {
            try
            {
                if (process.HasExited)
                    continue;

                if (!session.ChangedAffinities.ContainsKey(process.Id))
                    session.ChangedAffinities[process.Id] = process.ProcessorAffinity;

                process.ProcessorAffinity = mask;
                applied = true;
            }
            catch
            {
            }
        }

        return applied;
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

    public void RestoreAffinities(OptimizationSession session)
    {
        foreach (var item in session.ChangedAffinities)
        {
            try
            {
                var process = Process.GetProcessById(item.Key);
                if (!process.HasExited)
                    process.ProcessorAffinity = item.Value;
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

    private static IntPtr GetPreferredAffinityMask()
    {
        var processorCount = Environment.ProcessorCount;
        if (processorCount <= 1)
            return IntPtr.Zero;

        long mask = 0;
        for (var i = 0; i < processorCount; i += 2)
            mask |= 1L << i;

        if (mask == 0)
            mask = (1L << Math.Min(processorCount, 1)) - 1;

        return new IntPtr(mask);
    }
}
