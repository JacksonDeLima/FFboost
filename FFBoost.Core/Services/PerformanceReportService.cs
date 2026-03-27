using FFBoost.Core.Models;

namespace FFBoost.Core.Services;

public class PerformanceReportService
{
    public List<string> BuildReport(TechnicalReport report)
    {
        var lines = new List<string>
        {
            "===== RELATORIO FF BOOST =====",
            $"Perfil efetivo: {report.Profile}",
            $"Modo Free Fire: {(report.FreeFireModeEnabled ? "ativo" : "padrao")}",
            $"CPU: {report.CpuBefore}% -> {report.CpuAfter}%",
            $"RAM: {report.RamBefore} GB -> {report.RamAfter} GB",
            $"Carga RAM: {report.RamUsageBeforePercent:0.#}% -> {report.RamUsageAfterPercent:0.#}%",
            $"Processos: {report.ProcessesBefore} -> {report.ProcessesAfter}",
            $"Kill plan: {report.KillPlanCount}",
            $"Suspend plan: {report.SuspendPlanCount}",
            $"Encerrados: {report.KilledCount}",
            $"Suspensos: {report.SuspendedCount}",
            $"Memoria otimizada: {(report.MemoryOptimizationApplied ? "ativa" : "nao relevante")} | {report.MemoryOptimizedProcessCount} processo(s) | ~{report.MemoryRecoveredMb:0.#} MB",
            $"Turbo FPS: {(report.TurboModeApplied ? "ativo" : "nao aplicado")}",
            $"Windows basico (Ultra): {(report.UltraVisualTweaksApplied ? "ativo" : "nao aplicado")}",
            $"Score: {report.SessionScore:0.##}",
            $"Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
        };

        if (report.TopProcessesBefore.Count > 0)
        {
            lines.Add("Top processos antes:");
            lines.AddRange(report.TopProcessesBefore.Select(FormatUsageLine));
        }

        if (report.TopProcessesAfter.Count > 0)
        {
            lines.Add("Top processos depois:");
            lines.AddRange(report.TopProcessesAfter.Select(FormatUsageLine));
        }

        if (report.MemoryOptimizedProcesses.Count > 0)
        {
            lines.Add("Processos compactados na memoria:");
            lines.AddRange(report.MemoryOptimizedProcesses.Select(static x => $"- {x}"));
        }

        return lines;
    }

    private static string FormatUsageLine(ProcessResourceUsage usage)
    {
        return $"- {usage.Name}: CPU {usage.CpuPercent:0.#}% | RAM {usage.RamMb:0.#} MB | DISCO {usage.DiskMbPerSecond:0.#} MB/s";
    }
}
