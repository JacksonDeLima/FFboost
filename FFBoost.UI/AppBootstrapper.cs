using FFBoost.Core.Rules;
using FFBoost.Core.Services;

namespace FFBoost.UI;

internal static class AppBootstrapper
{
    public static MainForm CreateMainForm(string[]? args = null)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var configPath = Path.Combine(baseDirectory, "config.json");

        var configService = new ConfigService(configPath);
        var sessionStateStore = new PerformanceSessionStateStore(baseDirectory);
        var processRules = new ProcessRules();
        var metricsService = new SystemMetricsService();
        var processAnalyzerService = new ProcessAnalyzerService();
        var automaticProfileService = new AutomaticProfileService();
        var turboModeService = new TurboModeService();
        var performanceReportService = new PerformanceReportService();
        var processScanner = new ProcessScanner();
        var memoryOptimizerService = new MemoryOptimizerService(processRules, metricsService);
        var optimizer = new OptimizerService(
            configService,
            processScanner,
            new ProcessKiller(),
            new ProcessSuspendService(),
            new PerformanceManager(),
            new TimerResolutionService(),
            new OverlayService(),
            new ExplorerWindowService(),
            new OptimizationPlanBuilder(),
            new OptimizationSuggestionService(),
            processAnalyzerService,
            metricsService,
            automaticProfileService,
            turboModeService,
            memoryOptimizerService,
            processRules,
            sessionStateStore);
        var adminService = new AdminService();
        var logFileService = new LogFileService(baseDirectory);
        var telemetryService = new TelemetryService(baseDirectory);
        var coordinator = new OptimizationCoordinatorService(
            optimizer,
            metricsService,
            logFileService,
            telemetryService,
            configService,
            performanceReportService);
        var watcherService = new GameWatcherService(processScanner, configService);
        var startupService = new StartupService();

        return new MainForm(
            optimizer,
            adminService,
            configService,
            startupService,
            metricsService,
            logFileService,
            telemetryService,
            coordinator,
            watcherService,
            processAnalyzerService,
            args?.Any(x => string.Equals(x, "--tray", StringComparison.OrdinalIgnoreCase)) == true);
    }
}
