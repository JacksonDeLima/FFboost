using FFBoost.Core.Models;
using FFBoost.Core.Services;

namespace FFBoost.Core.Tests;

public sealed class PerformanceReportServiceTests
{
    [Fact]
    public void BuildReport_IncludesTurboAndTopProcesses()
    {
        var service = new PerformanceReportService();
        var report = new TechnicalReport
        {
            Profile = "Ultra",
            CpuBefore = 44,
            CpuAfter = 28,
            RamBefore = 12.4,
            RamAfter = 9.8,
            RamUsageBeforePercent = 78,
            RamUsageAfterPercent = 61,
            ProcessesBefore = 187,
            ProcessesAfter = 151,
            KillPlanCount = 24,
            SuspendPlanCount = 0,
            KilledCount = 18,
            SuspendedCount = 0,
            MemoryOptimizationApplied = true,
            MemoryOptimizedProcessCount = 5,
            MemoryRecoveredMb = 640,
            TurboModeApplied = true,
            UltraVisualTweaksApplied = true,
            SessionScore = 32.5,
            TopProcessesBefore =
            {
                new ProcessResourceUsage { Name = "chrome", CpuPercent = 12.5, RamMb = 850, DiskMbPerSecond = 1.4 }
            }
        };

        var lines = service.BuildReport(report);

        Assert.Contains(lines, static x => x.Contains("Turbo FPS: ativo", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(lines, static x => x.Contains("Memoria otimizada: ativa", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(lines, static x => x.Contains("Top processos antes:", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(lines, static x => x.Contains("chrome", StringComparison.OrdinalIgnoreCase));
    }
}
