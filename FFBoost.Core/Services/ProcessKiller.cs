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
                {
                    continue;
                }

                process.Kill(entireProcessTree: true);
                process.WaitForExit(3000);
                result.KilledProcesses.Add(process.ProcessName);
            }
            catch
            {
                result.FailedProcesses.Add(process.ProcessName);
            }
        }

        return result;
    }
}
