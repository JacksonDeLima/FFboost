using System.Diagnostics;
using FFBoost.Core.Models;

namespace FFBoost.Core.Services;

public class ProcessKiller
{
    public ProcessKillResult KillProcesses(IEnumerable<Process> processes)
    {
        var result = new ProcessKillResult();

        foreach (var process in processes)
        {
            try
            {
                if (process.HasExited)
                    continue;

                var displayName = $"{process.ProcessName} (PID {process.Id})";

                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    try
                    {
                        process.CloseMainWindow();

                        if (process.WaitForExit(2500))
                        {
                            result.KilledProcesses.Add(displayName);
                            continue;
                        }
                    }
                    catch
                    {
                    }
                }

                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);

                if (process.HasExited)
                {
                    result.KilledProcesses.Add(displayName);
                }
                else
                {
                    result.FailedProcesses.Add($"{displayName} - nao finalizou dentro do tempo esperado");
                }
            }
            catch (Exception ex)
            {
                result.FailedProcesses.Add($"{process.ProcessName} (PID {process.Id}) - {ex.Message}");
            }
        }

        return result;
    }

    public List<string> KillProcessesByName(string processName)
    {
        var killed = new List<string>();

        foreach (var process in Process.GetProcessesByName(processName))
        {
            try
            {
                if (process.HasExited)
                    continue;

                var displayName = $"{process.ProcessName} (PID {process.Id})";

                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    try
                    {
                        process.CloseMainWindow();

                        if (process.WaitForExit(2500))
                        {
                            killed.Add(displayName);
                            continue;
                        }
                    }
                    catch
                    {
                    }
                }

                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);

                if (process.HasExited)
                    killed.Add(displayName);
            }
            catch
            {
            }
        }

        return killed;
    }
}
