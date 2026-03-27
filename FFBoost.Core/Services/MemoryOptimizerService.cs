using System.Diagnostics;
using System.Runtime.InteropServices;
using FFBoost.Core.Models;
using FFBoost.Core.Rules;

namespace FFBoost.Core.Services;

public class MemoryOptimizerService
{
    private readonly ProcessRules _rules;
    private readonly SystemMetricsService _metricsService;

    public MemoryOptimizerService(ProcessRules rules, SystemMetricsService metricsService)
    {
        _rules = rules;
        _metricsService = metricsService;
    }

    public MemoryOptimizationResult Optimize(
        string profile,
        IReadOnlyCollection<string> allowedProcesses,
        IReadOnlyCollection<string> emulatorProcesses)
    {
        var policy = GetPolicy(profile);
        var result = new MemoryOptimizationResult
        {
            Profile = profile,
            RamBeforeGb = _metricsService.GetUsedRamGb(),
            RamUsageBeforePercent = _metricsService.GetRamUsagePercentage()
        };

        if (policy.MaxProcesses <= 0)
        {
            result.RamAfterGb = result.RamBeforeGb;
            result.RamUsageAfterPercent = result.RamUsageBeforePercent;
            return result;
        }

        var protectedNames = new HashSet<string>(allowedProcesses, StringComparer.OrdinalIgnoreCase);
        foreach (var emulator in emulatorProcesses)
            protectedNames.Add(emulator);

        protectedNames.Add(Process.GetCurrentProcess().ProcessName);

        var candidates = GetTrimCandidates(policy, protectedNames);
        foreach (var candidate in candidates)
        {
            using (candidate.Process)
            {
                if (!TrimProcessWorkingSet(candidate.Process, out var freedMb))
                    continue;

                result.TrimmedProcessCount++;
                result.EstimatedFreedMb += freedMb;
                result.TrimmedProcesses.Add($"{candidate.Process.ProcessName} ({candidate.WorkingSetMb:0} MB)");
            }
        }

        if (policy.CompactCurrentProcess)
            CompactCurrentProcess();

        result.EstimatedFreedMb = Math.Round(result.EstimatedFreedMb, 1);
        result.Applied = result.TrimmedProcessCount > 0 || result.EstimatedFreedMb > 0.5;
        result.RamAfterGb = _metricsService.GetUsedRamGb();
        result.RamUsageAfterPercent = _metricsService.GetRamUsagePercentage();
        return result;
    }

    public MemoryTrimPolicy GetPolicy(string profile)
    {
        if (profile.Equals("Ultra", StringComparison.OrdinalIgnoreCase))
            return new MemoryTrimPolicy(60, 18, CompactCurrentProcess: true);

        if (profile.Equals("Forte", StringComparison.OrdinalIgnoreCase))
            return new MemoryTrimPolicy(110, 12, CompactCurrentProcess: true);

        return new MemoryTrimPolicy(180, 6, CompactCurrentProcess: false);
    }

    private List<ProcessTrimCandidate> GetTrimCandidates(
        MemoryTrimPolicy policy,
        IReadOnlySet<string> protectedNames)
    {
        var candidates = new List<ProcessTrimCandidate>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var processName = process.ProcessName;
                if (_rules.IsCritical(processName) || protectedNames.Contains(processName))
                {
                    process.Dispose();
                    continue;
                }

                if (process.Id <= 4 || process.SessionId == 0 || process.HasExited)
                {
                    process.Dispose();
                    continue;
                }

                var workingSetMb = Math.Max(0d, process.WorkingSet64 / 1024d / 1024d);
                if (workingSetMb < policy.MinimumWorkingSetMb)
                {
                    process.Dispose();
                    continue;
                }

                candidates.Add(new ProcessTrimCandidate(process, workingSetMb));
            }
            catch
            {
                process.Dispose();
            }
        }

        return candidates
            .OrderByDescending(static x => x.WorkingSetMb)
            .Take(policy.MaxProcesses)
            .ToList();
    }

    private static bool TrimProcessWorkingSet(Process process, out double freedMb)
    {
        freedMb = 0;

        try
        {
            process.Refresh();
            var beforeBytes = process.WorkingSet64;
            if (beforeBytes <= 0)
                return false;

            var handle = process.Handle;
            if (handle == IntPtr.Zero)
                return false;

            var trimmed = EmptyWorkingSet(handle);
            trimmed |= SetProcessWorkingSetSize(handle, new IntPtr(-1), new IntPtr(-1));
            if (!trimmed)
                return false;

            Thread.Sleep(20);
            process.Refresh();
            var afterBytes = process.WorkingSet64;
            freedMb = Math.Max(0d, (beforeBytes - afterBytes) / 1024d / 1024d);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void CompactCurrentProcess()
    {
        try
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();

            using var current = Process.GetCurrentProcess();
            if (current.Handle != IntPtr.Zero)
            {
                EmptyWorkingSet(current.Handle);
                SetProcessWorkingSetSize(current.Handle, new IntPtr(-1), new IntPtr(-1));
            }
        }
        catch
        {
        }
    }

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

    private sealed record ProcessTrimCandidate(Process Process, double WorkingSetMb);

    public sealed record MemoryTrimPolicy(double MinimumWorkingSetMb, int MaxProcesses, bool CompactCurrentProcess);
}
