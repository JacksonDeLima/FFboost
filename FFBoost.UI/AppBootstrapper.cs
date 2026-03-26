using FFBoost.Core.Rules;
using FFBoost.Core.Services;

namespace FFBoost.UI;

internal static class AppBootstrapper
{
    public static MainForm CreateMainForm()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var configPath = Path.Combine(baseDirectory, "config.json");

        var configService = new ConfigService(configPath);
        var processScanner = new ProcessScanner();
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
            new ProcessRules());
        var adminService = new AdminService();
        var metricsService = new SystemMetricsService();
        var logFileService = new LogFileService(baseDirectory);
        var telemetryService = new TelemetryService(baseDirectory);
        var coordinator = new OptimizationCoordinatorService(
            optimizer,
            metricsService,
            logFileService,
            telemetryService,
            configService);
        var watcherService = new GameWatcherService(processScanner, configService);

        return new MainForm(
            optimizer,
            adminService,
            configService,
            metricsService,
            logFileService,
            telemetryService,
            coordinator,
            watcherService);
    }
}
