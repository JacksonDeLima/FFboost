using System.Diagnostics;

namespace FFBoost.Core.Services;

public class ProcessScanner
{
    public List<Process> GetRunningProcesses()
    {
        return Process.GetProcesses().OrderBy(static p => p.ProcessName).ToList();
    }

    public List<Process> FindProcessesByNames(IEnumerable<string> processNames)
    {
        var names = processNames
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Process.GetProcesses()
            .Where(p => names.Contains(p.ProcessName))
            .ToList();
    }

    public List<string> GetRunningProcessNames()
    {
        return Process.GetProcesses()
            .Select(static p => p.ProcessName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
