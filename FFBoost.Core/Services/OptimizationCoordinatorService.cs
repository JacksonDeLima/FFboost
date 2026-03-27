using FFBoost.Core.Models;

namespace FFBoost.Core.Services;

public class OptimizationCoordinatorService
{
    private readonly OptimizerService _optimizer;
    private readonly SystemMetricsService _metricsService;
    private readonly LogFileService _logFileService;
    private readonly TelemetryService _telemetryService;
    private readonly ConfigService _configService;
    private readonly PerformanceReportService _performanceReportService;

    public OptimizationCoordinatorService(
        OptimizerService optimizer,
        SystemMetricsService metricsService,
        LogFileService logFileService,
        TelemetryService telemetryService,
        ConfigService configService,
        PerformanceReportService performanceReportService)
    {
        _optimizer = optimizer;
        _metricsService = metricsService;
        _logFileService = logFileService;
        _telemetryService = telemetryService;
        _configService = configService;
        _performanceReportService = performanceReportService;
    }

    public OptimizationExecutionResult ExecuteOptimize(string profile, bool freeFireModeEnabled)
    {
        var runningBefore = _optimizer.GetRunningProcesses();
        var cpuBefore = _metricsService.GetCpuUsagePercentage();
        var ramBefore = _metricsService.GetUsedRamGb();
        var ramUsageBeforePercent = _metricsService.GetRamUsagePercentage();
        var optimizationResult = _optimizer.StartGameMode();
        var cpuAfter = _metricsService.GetCpuUsagePercentage();
        var ramAfter = _metricsService.GetUsedRamGb();
        var ramUsageAfterPercent = _metricsService.GetRamUsagePercentage();
        var optimizationSucceeded = _optimizer.IsGameModeActive();

        var report = _optimizer.LastTechnicalReport;
        report.CpuBefore = cpuBefore;
        report.CpuAfter = cpuAfter;
        report.RamBefore = ramBefore;
        report.RamAfter = ramAfter;
        if (report.RamUsageBeforePercent <= 0)
            report.RamUsageBeforePercent = ramUsageBeforePercent;

        report.RamUsageAfterPercent = ramUsageAfterPercent;
        report.ProcessesBefore = runningBefore.Count;
        report.ProcessesAfter = _optimizer.GetRunningProcesses().Count;
        report.SessionScore = optimizationSucceeded ? CalculateSessionScore(report) : 0;
        var effectiveProfile = report.Profile;
        report.Benchmark = optimizationSucceeded
            ? _telemetryService.BuildBenchmarkSummary(effectiveProfile, freeFireModeEnabled, report.SessionScore)
            : new BenchmarkSummary();
        report.PerformanceReport = _performanceReportService.BuildReport(report);

        var whitelistSuggestions = _telemetryService.GetWhitelistSuggestions(runningBefore);
        var combinedLogs = new List<string>(optimizationResult.logs);
        combinedLogs.Insert(0, $"Status: {optimizationResult.status}");
        combinedLogs.AddRange(whitelistSuggestions.Select(static suggestion => $"Sugestao de whitelist: {suggestion}"));

        var fullLog = new List<string>
        {
            $"Status: {optimizationResult.status}",
            $"Perfil: {effectiveProfile}",
            $"Modo FF: {(freeFireModeEnabled ? "ativo" : "padrao")}",
            $"Score: {report.SessionScore:0.##}",
            $"Benchmark delta: {report.Benchmark.LastScoreDelta:+0.##;-0.##;0}",
            $"CPU: {cpuBefore}% -> {cpuAfter}%",
            $"RAM: {ramBefore} GB -> {ramAfter} GB",
            $"Carga RAM: {report.RamUsageBeforePercent:0.#}% -> {report.RamUsageAfterPercent:0.#}%",
            $"Processos: {report.ProcessesBefore} -> {report.ProcessesAfter}",
            $"Plano: kill {report.KillPlanCount}, suspend {report.SuspendPlanCount}",
            $"Encerrados: {report.KilledCount}",
            $"Suspensos: {report.SuspendedCount}",
            $"Memoria otimizada: {report.MemoryOptimizedProcessCount} processo(s), ~{report.MemoryRecoveredMb:0.#} MB",
            $"Overlays: {report.OverlayCount}",
            $"Tempo: {report.Elapsed.TotalMilliseconds:0} ms"
        };
        fullLog.AddRange(combinedLogs);

        var logPath = _logFileService.SaveLog(fullLog);
        combinedLogs.Add($"Log salvo em: {logPath}");

        var config = _configService.Load();
        if (optimizationSucceeded && config.TelemetryEnabled)
        {
            _telemetryService.Append(new TelemetryEntry
            {
                Timestamp = DateTime.Now,
                Profile = effectiveProfile,
                FreeFireModeEnabled = freeFireModeEnabled,
                CpuBefore = cpuBefore,
                CpuAfter = cpuAfter,
                RamBefore = ramBefore,
                RamAfter = ramAfter,
                SessionScore = report.SessionScore,
                KilledCount = report.KilledCount,
                SuspendedCount = report.SuspendedCount,
                TurboModeApplied = report.TurboModeApplied,
                KilledProcesses = report.KilledProcesses,
                RelaunchedProcesses = whitelistSuggestions
            });
        }

        report.Recommendation = optimizationSucceeded
            ? _telemetryService.GetRecommendedProfile(freeFireModeEnabled)
            : new ProfileRecommendation
            {
                RecommendedProfile = effectiveProfile,
                UseFreeFirePreset = freeFireModeEnabled,
                Reason = "Otimizacao nao concluida. Recomendacao mantida ate haver uma sessao valida."
            };
        combinedLogs.Add($"Perfil recomendado: {report.Recommendation.RecommendedProfile}");

        return new OptimizationExecutionResult
        {
            Status = optimizationResult.status,
            Logs = combinedLogs,
            Report = report,
            LogPath = logPath,
            WhitelistSuggestions = whitelistSuggestions
        };
    }

    public RestoreExecutionResult ExecuteRestore()
    {
        var restoreResult = _optimizer.Restore();
        return new RestoreExecutionResult
        {
            Status = restoreResult.status,
            Logs = restoreResult.logs
        };
    }

    public MemoryOptimizationExecutionResult ExecuteMemoryOptimize(string profile, bool freeFireModeEnabled)
    {
        var result = _optimizer.OptimizeMemory(profile, freeFireModeEnabled);
        var fullLog = new List<string>
        {
            $"Status: {result.Status}",
            $"Perfil: {result.Result.Profile}",
            $"RAM: {result.Result.RamBeforeGb:0.##} GB -> {result.Result.RamAfterGb:0.##} GB",
            $"Carga RAM: {result.Result.RamUsageBeforePercent:0.#}% -> {result.Result.RamUsageAfterPercent:0.#}%",
            $"Memoria otimizada: {result.Result.TrimmedProcessCount} processo(s), ~{result.Result.EstimatedFreedMb:0.#} MB"
        };
        fullLog.AddRange(result.Logs);
        var logPath = _logFileService.SaveLog(fullLog);

        var logs = new List<string>(result.Logs)
        {
            $"Log salvo em: {logPath}"
        };

        return new MemoryOptimizationExecutionResult
        {
            Status = result.Status,
            Logs = logs,
            Result = result.Result
        };
    }

    public RestoreExecutionResult RecoverPendingSystemState()
    {
        var logs = _optimizer.RecoverPendingSystemState();
        return new RestoreExecutionResult
        {
            Status = logs.Count > 0 ? "Estado anterior restaurado." : "Nenhuma recuperacao pendente.",
            Logs = logs
        };
    }

    private static double CalculateSessionScore(TechnicalReport report)
    {
        var cpuGain = Math.Max(0, report.CpuBefore - report.CpuAfter);
        var ramGain = Math.Max(0, report.RamBefore - report.RamAfter) * 8;
        var processGain = Math.Max(0, report.KilledCount * 1.2) + Math.Max(0, report.SuspendedCount * 0.8);
        var overlayPenalty = report.OverlayCount * 0.5;
        var freeFireBonus = report.FreeFireModeEnabled ? 2.0 : 0.0;
        return Math.Round(cpuGain + ramGain + processGain + freeFireBonus - overlayPenalty, 2);
    }
}
