using System.Diagnostics;
using System.Runtime.InteropServices;
using FFBoost.Core.Models;

namespace FFBoost.Core.Services;

public class ProcessAnalyzerService
{
    public List<ProcessResourceUsage> GetTopProcesses(int count = 10, int sampleMilliseconds = 350)
    {
        var snapshot = CaptureSnapshot();
        if (snapshot.Count == 0)
            return new List<ProcessResourceUsage>();

        Thread.Sleep(sampleMilliseconds);

        var processorCount = Math.Max(1, Environment.ProcessorCount);
        var elapsedSeconds = Math.Max(0.1d, sampleMilliseconds / 1000d);
        var result = new List<ProcessResourceUsage>();

        foreach (var item in snapshot)
        {
            try
            {
                using var process = Process.GetProcessById(item.ProcessId);
                if (process.HasExited)
                    continue;

                var cpuDelta = (process.TotalProcessorTime - item.CpuTime).TotalSeconds;
                var cpuPercent = Math.Max(0d, cpuDelta / (elapsedSeconds * processorCount) * 100d);
                var ramMb = Math.Max(0d, process.WorkingSet64 / 1024d / 1024d);
                var diskMbPerSecond = GetDiskDeltaMbPerSecond(process, item, elapsedSeconds);
                var filePath = GetProcessPath(process);
                var versionInfo = GetVersionInfo(filePath);

                result.Add(new ProcessResourceUsage
                {
                    ProcessId = process.Id,
                    Name = process.ProcessName,
                    FilePath = filePath,
                    Description = versionInfo?.FileDescription ?? string.Empty,
                    CompanyName = versionInfo?.CompanyName ?? string.Empty,
                    CpuPercent = Math.Round(cpuPercent, 1),
                    RamMb = Math.Round(ramMb, 1),
                    DiskMbPerSecond = Math.Round(diskMbPerSecond, 1)
                });
            }
            catch
            {
            }
        }

        return result
            .OrderByDescending(static x => x.CpuPercent * 3 + x.RamMb / 128d + x.DiskMbPerSecond * 2)
            .ThenByDescending(static x => x.RamMb)
            .Take(count)
            .ToList();
    }

    private static string GetProcessPath(Process process)
    {
        try
        {
            return process.MainModule?.FileName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static FileVersionInfo? GetVersionInfo(string filePath)
    {
        try
        {
            return string.IsNullOrWhiteSpace(filePath)
                ? null
                : FileVersionInfo.GetVersionInfo(filePath);
        }
        catch
        {
            return null;
        }
    }

    private static List<ProcessSnapshot> CaptureSnapshot()
    {
        var result = new List<ProcessSnapshot>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                using var current = process;
                result.Add(new ProcessSnapshot
                {
                    ProcessId = current.Id,
                    CpuTime = current.TotalProcessorTime,
                    TotalIoBytes = GetTotalIoBytes(current)
                });
            }
            catch
            {
            }
        }

        return result;
    }

    private static double GetDiskDeltaMbPerSecond(Process process, ProcessSnapshot snapshot, double elapsedSeconds)
    {
        var currentIoBytes = GetTotalIoBytes(process);
        if (currentIoBytes < snapshot.TotalIoBytes)
            return 0d;

        var bytesPerSecond = (currentIoBytes - snapshot.TotalIoBytes) / elapsedSeconds;
        return bytesPerSecond / 1024d / 1024d;
    }

    private static ulong GetTotalIoBytes(Process process)
    {
        try
        {
            return GetProcessIoCounters(process.Handle, out var counters)
                ? counters.ReadTransferCount + counters.WriteTransferCount
                : 0;
        }
        catch
        {
            return 0;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetProcessIoCounters(IntPtr hProcess, out IO_COUNTERS lpIoCounters);

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    private sealed class ProcessSnapshot
    {
        public int ProcessId { get; init; }
        public TimeSpan CpuTime { get; init; }
        public ulong TotalIoBytes { get; init; }
    }
}
