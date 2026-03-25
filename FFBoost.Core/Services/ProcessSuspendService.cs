using System.Diagnostics;
using System.Runtime.InteropServices;
using FFBoost.Core.Models;

namespace FFBoost.Core.Services;

public class ProcessSuspendService
{
    private const uint ProcessSuspendResume = 0x0800;

    public List<string> SuspendProcesses(IEnumerable<Process> processes, OptimizationSession session)
    {
        var suspended = new List<string>();

        foreach (var process in processes)
        {
            IntPtr handle = IntPtr.Zero;

            try
            {
                if (process.HasExited || session.SuspendedProcesses.ContainsKey(process.Id))
                    continue;

                handle = OpenProcess(ProcessSuspendResume, false, process.Id);
                if (handle == IntPtr.Zero)
                    continue;

                if (NtSuspendProcess(handle) == 0)
                {
                    session.SuspendedProcesses[process.Id] = process.ProcessName;
                    suspended.Add(process.ProcessName);
                }
            }
            catch
            {
            }
            finally
            {
                if (handle != IntPtr.Zero)
                    CloseHandle(handle);
            }
        }

        return suspended;
    }

    public List<string> ResumeProcesses(OptimizationSession session)
    {
        var resumed = new List<string>();
        var suspendedEntries = session.SuspendedProcesses.ToList();

        foreach (var entry in suspendedEntries)
        {
            IntPtr handle = IntPtr.Zero;

            try
            {
                handle = OpenProcess(ProcessSuspendResume, false, entry.Key);
                if (handle == IntPtr.Zero)
                    continue;

                if (NtResumeProcess(handle) == 0)
                {
                    resumed.Add(entry.Value);
                }
            }
            catch
            {
            }
            finally
            {
                if (handle != IntPtr.Zero)
                    CloseHandle(handle);
            }
        }

        session.SuspendedProcesses.Clear();
        return resumed;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtSuspendProcess(IntPtr processHandle);

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtResumeProcess(IntPtr processHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
}
