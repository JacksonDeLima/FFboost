using System.Runtime.InteropServices;
using FFBoost.Core.Models;

namespace FFBoost.Core.Services;

public class TimerResolutionService
{
    private const uint TargetResolution = 1;

    public bool Apply(OptimizationSession session)
    {
        try
        {
            var result = timeBeginPeriod(TargetResolution);
            session.TimerResolutionApplied = result == 0;
            return session.TimerResolutionApplied;
        }
        catch
        {
            return false;
        }
    }

    public void Restore(OptimizationSession session)
    {
        if (!session.TimerResolutionApplied)
            return;

        try
        {
            timeEndPeriod(TargetResolution);
        }
        catch
        {
        }

        session.TimerResolutionApplied = false;
    }

    [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    private static extern uint timeBeginPeriod(uint uMilliseconds);

    [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    private static extern uint timeEndPeriod(uint uMilliseconds);
}
