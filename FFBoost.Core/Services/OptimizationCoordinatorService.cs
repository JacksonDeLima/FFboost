using FFBoost.Core.Models;

namespace FFBoost.Core.Services;

public class OptimizationCoordinatorService
{
    private readonly OptimizerService _optimizer;
    private readonly SystemMetricsService _metricsService;
    private readonly LogFileService _logFileService;
    private readonly TelemetryService _telemetryService;
    private readonly ConfigService _configService;

    public OptimizationCoordinatorService(
        OptimizerService optimizer,
        SystemMetricsService metricsService,
        LogFileService logFileService,
        TelemetryService telemetryService,
        ConfigService configService)
    {
        _optimizer = optimizer;
        _metricsService = metricsService;
        _logFileService = logFileService;
        _telemetryService = telemetryService;
        _configService = configService;
    }

    public OptimizationExecutionResult ExecuteOptimize(string profile, bool freeFireModeEnabled)
    {
        var runningBefore = _optimizer.GetRunningProcesses();
        var cpuBefore = _metricsService.GetCpuUsagePercentage();
        var ramBefore = _metricsService.GetUsedRamGb();
        var optimizationResult = _optimizer.StartGameMode();
        var cpuAfter = _metricsService.GetCpuUsagePercentage();
        var ramAfter = _metricsService.GetUsedRamGb();
        var optimizationSucceeded = _optimizer.IsGameModeActive();

        var report = _optimizer.LastTechnicalReport;
        report.CpuBefore = cpuBefore;
        report.CpuAfter = cpuAfter;
        report.RamBefore = ramBefore;
        report.RamAfter = ramAfter;
        report.ProcessesBefore = runningBefore.Count;
        report.ProcessesAfter = _optimizer.GetRunningProcesses().Count;
        report.SessionScore = optimizationSucceeded ? CalculateSessionScore(report) : 0;
        report.Benchmark = optimizationSucceeded
            ? _telemetryService.BuildBenchmarkSummary(profile, freeFireModeEnabled, report.SessionScore)
            : new BenchmarkSummary();

        var whitelistSuggestions = _telemetryService.GetWhitelistSuggestions(runningBefore);
        var combinedLogs = new List<string>(optimizationResult.logs);
        combinedLogs.AddRange(whitelistSuggestions.Select(static suggestion => $"Sugestao de whitelist: {suggestion}"));

        var fullLog = new List<string>
        {
            $"Status: {optimizationResult.status}",
            $"Perfil: {profile}",
            $"Modo FF: {(freeFireModeEnabled ? "ativo" : "padrao")}",
            $"Score: {report.SessionScore:0.##}",
            $"Benchmark delta: {report.Benchmark.LastScoreDelta:+0.##;-0.##;0}",
            $"CPU: {cpuBefore}% -> {cpuAfter}%",
            $"RAM: {ramBefore} GB -> {ramAfter} GB",
            $"Processos: {report.ProcessesBefore} -> {report.ProcessesAfter}",
            $"Plano: kill {report.KillPlanCount}, suspend {report.SuspendPlanCount}",
            $"Encerrados: {report.KilledCount}",
            $"Suspensos: {report.SuspendedCount}",
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
                Profile = profile,
                FreeFireModeEnabled = freeFireModeEnabled,
                CpuBefore = cpuBefore,
                CpuAfter = cpuAfter,
                RamBefore = ramBefore,
                RamAfter = ramAfter,
                SessionScore = report.SessionScore,
                KilledCount = report.KilledCount,
                SuspendedCount = report.SuspendedCount,
                KilledProcesses = report.KilledProcesses,
                RelaunchedProcesses = whitelistSuggestions
            });
        }

        report.Recommendation = optimizationSucceeded
            ? _telemetryService.GetRecommendedProfile(freeFireModeEnabled)
            : new ProfileRecommendation
            {
                RecommendedProfile = profile,
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
