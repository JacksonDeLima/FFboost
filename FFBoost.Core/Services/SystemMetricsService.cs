using System.Runtime.InteropServices;

namespace FFBoost.Core.Services;

public class SystemMetricsService
{
    public double GetUsedRamGb()
    {
        var memoryStatus = new MEMORYSTATUSEX();
        if (!GlobalMemoryStatusEx(memoryStatus))
            return 0;

        ulong used = memoryStatus.ullTotalPhys - memoryStatus.ullAvailPhys;
        return Math.Round(used / 1024d / 1024d / 1024d, 2);
    }

    public double GetCpuUsagePercentage()
    {
        if (!TryGetSystemTimes(out var idle1, out var kernel1, out var user1))
            return 0;

        Thread.Sleep(700);

        if (!TryGetSystemTimes(out var idle2, out var kernel2, out var user2))
            return 0;

        var idle = idle2 - idle1;
        var kernel = kernel2 - kernel1;
        var user = user2 - user1;
        var total = kernel + user;

        if (total <= 0)
            return 0;

        var cpu = (1.0 - ((double)idle / total)) * 100.0;
        return Math.Round(cpu, 2);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

    private static bool TryGetSystemTimes(out ulong idle, out ulong kernel, out ulong user)
    {
        idle = 0;
        kernel = 0;
        user = 0;

        if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
            return false;

        idle = ((ulong)idleTime.dwHighDateTime << 32) | idleTime.dwLowDateTime;
        kernel = ((ulong)kernelTime.dwHighDateTime << 32) | kernelTime.dwLowDateTime;
        user = ((ulong)userTime.dwHighDateTime << 32) | userTime.dwLowDateTime;
        return true;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MEMORYSTATUSEX
    {
        public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }
}
