using System.Diagnostics;
using FFBoost.Core.Models;
using FFBoost.Core.Rules;

namespace FFBoost.Core.Services;

public class OptimizerService
{
    private static readonly HashSet<string> BrowserTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome",
        "msedge",
        "firefox",
        "opera",
        "brave"
    };

    private readonly ConfigService _configService;
    private readonly ProcessScanner _scanner;
    private readonly ProcessKiller _killer;
    private readonly ProcessSuspendService _suspendService;
    private readonly PerformanceManager _performance;
    private readonly TimerResolutionService _timerResolution;
    private readonly OverlayService _overlayService;
    private readonly ExplorerWindowService _explorerWindowService;
    private readonly OptimizationPlanBuilder _planBuilder;
    private readonly OptimizationSuggestionService _suggestionService;
    private readonly ProcessAnalyzerService _processAnalyzer;
    private readonly SystemMetricsService _metricsService;
    private readonly AutomaticProfileService _automaticProfileService;
    private readonly TurboModeService _turboModeService;
    private readonly MemoryOptimizerService _memoryOptimizerService;
    private readonly ProcessRules _rules;
    private readonly PerformanceSessionStateStore _sessionStateStore;
    private readonly OptimizationSession _session = new();

    public TechnicalReport LastTechnicalReport { get; private set; } = new();

    public OptimizerService(
        ConfigService configService,
        ProcessScanner scanner,
        ProcessKiller killer,
        ProcessSuspendService suspendService,
        PerformanceManager performance,
        TimerResolutionService timerResolution,
        OverlayService overlayService,
        ExplorerWindowService explorerWindowService,
        OptimizationPlanBuilder planBuilder,
        OptimizationSuggestionService suggestionService,
        ProcessAnalyzerService processAnalyzer,
        SystemMetricsService metricsService,
        AutomaticProfileService automaticProfileService,
        TurboModeService turboModeService,
        MemoryOptimizerService memoryOptimizerService,
        ProcessRules rules,
        PerformanceSessionStateStore sessionStateStore)
    {
        _configService = configService;
        _scanner = scanner;
        _killer = killer;
        _suspendService = suspendService;
        _performance = performance;
        _timerResolution = timerResolution;
        _overlayService = overlayService;
        _explorerWindowService = explorerWindowService;
        _planBuilder = planBuilder;
        _suggestionService = suggestionService;
        _processAnalyzer = processAnalyzer;
        _metricsService = metricsService;
        _automaticProfileService = automaticProfileService;
        _turboModeService = turboModeService;
        _memoryOptimizerService = memoryOptimizerService;
        _rules = rules;
        _sessionStateStore = sessionStateStore;
    }

    public (string status, List<string> logs) StartGameMode()
    {
        var stopwatch = Stopwatch.StartNew();
        var logs = new List<string>();
        var report = new TechnicalReport();

        if (_session.IsGameModeActive)
        {
            logs.Add("Modo jogo ja esta ativo.");
            LastTechnicalReport = report;
            return ("Modo jogo ja ativo.", logs);
        }

        var config = _configService.Load();
        var effectiveProfile = ResolveEffectiveProfile(config, logs);
        var running = _scanner.GetRunningProcesses();
        var runningNames = running
            .Select(p => p.ProcessName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var emulatorProcesses = _scanner.FindProcessesByNames(config.EmulatorProcesses);
        var recorderProcesses = _scanner.FindProcessesByNames(config.RecordingProcesses);

        report.Profile = effectiveProfile;
        report.FreeFireModeEnabled = config.EnableFreeFireMode;
        report.ProcessesBefore = running.Count;
        report.RecordingModeDetected = recorderProcesses.Count > 0;
        report.TopProcessesBefore = _processAnalyzer.GetTopProcesses();

        if (!emulatorProcesses.Any())
        {
            logs.Add("BlueStacks nao detectado.");
            LastTechnicalReport = report;
            return ("BlueStacks nao detectado. Abra o emulador antes de otimizar.", logs);
        }

        var effectiveConfig = CloneWithEffectiveProfile(config, effectiveProfile);
        var plan = _planBuilder.Build(effectiveConfig, report.RecordingModeDetected);
        var effectiveAllowedProcesses = plan.EffectiveAllowedProcesses;

        _session.ActiveProfile = effectiveProfile;

        logs.Add($"Emulador detectado: {string.Join(", ", emulatorProcesses.Select(p => p.ProcessName).Distinct())}");
        logs.Add($"Perfil selecionado: {effectiveProfile}");
        logs.Add(config.EnableFreeFireMode
            ? "Modo Free Fire + BlueStacks ativo."
            : "Modo padrao de otimizacao ativo.");

        if (report.RecordingModeDetected)
        {
            logs.Add($"Modo gravacao detectado: {string.Join(", ", recorderProcesses.Select(p => p.ProcessName).Distinct())}");
        }

        var activeKillBlacklist = plan.KillBlacklist;
        var activeSuspendBlacklist = plan.SuspendBlacklist;

        report.KillPlanCount = activeKillBlacklist.Count;
        report.SuspendPlanCount = activeSuspendBlacklist.Count;

        logs.Add($"Blacklist de encerramento: {activeKillBlacklist.Count} processo(s).");
        logs.Add($"Blacklist de suspensao: {activeSuspendBlacklist.Count} processo(s).");

        var killCandidates = new List<Process>();
        var suspendCandidates = new List<Process>();

        foreach (var process in running)
        {
            if (_rules.IsCritical(process.ProcessName))
            {
                logs.Add($"Ignorado por risco alto: {process.ProcessName}");
                continue;
            }

            if (_rules.IsAllowed(process.ProcessName, effectiveAllowedProcesses))
            {
                logs.Add($"Ignorado por whitelist: {process.ProcessName}");
                continue;
            }

            if (activeKillBlacklist.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase))
            {
                logs.Add($"Encerrando [{_rules.GetRiskLevel(process.ProcessName, effectiveAllowedProcesses, activeKillBlacklist)}]: {process.ProcessName}");
                killCandidates.Add(process);
                continue;
            }

            if (activeSuspendBlacklist.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase))
            {
                logs.Add($"Suspendendo [{_rules.GetRiskLevel(process.ProcessName, effectiveAllowedProcesses, activeSuspendBlacklist)}]: {process.ProcessName}");
                suspendCandidates.Add(process);
            }
        }

        var killResult = _killer.KillProcesses(killCandidates);

        foreach (var browserName in BrowserTargets)
        {
            if (!activeKillBlacklist.Contains(browserName, StringComparer.OrdinalIgnoreCase))
                continue;

            var extraKilled = _killer.KillProcessesByName(browserName);

            foreach (var item in extraKilled)
            {
                if (!killResult.KilledProcesses.Contains(item, StringComparer.OrdinalIgnoreCase))
                    killResult.KilledProcesses.Add(item);

                logs.Add($"Encerrado na segunda passada: {item}");
            }
        }

        var explorerClosed = _explorerWindowService.CloseExplorerWindows();
        if (explorerClosed > 0)
        {
            logs.Add($"Janelas do Explorer fechadas: {explorerClosed}");
        }

        var suspended = _suspendService.SuspendProcesses(suspendCandidates, _session);

        _session.KilledProcesses.AddRange(killResult.KilledProcesses);

        report.KilledProcesses = killResult.KilledProcesses
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        report.SuspendedProcesses = suspended
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        report.KilledCount = report.KilledProcesses.Count;
        report.SuspendedCount = report.SuspendedProcesses.Count;

        foreach (var failed in killResult.FailedProcesses.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            logs.Add($"Falha ao encerrar: {failed}");
        }

        if (config.SetEmulatorHighPriority)
        {
            _performance.SetHighPriority(emulatorProcesses, _session);
            logs.Add("Prioridade alta aplicada ao emulador.");
        }

        if (config.EnableAffinityTuning)
        {
            report.AffinityApplied = _performance.SetEmulatorAffinity(emulatorProcesses, _session);
            logs.Add(report.AffinityApplied
                ? "Afinidade de CPU aplicada ao emulador."
                : "Afinidade de CPU nao foi aplicada.");
        }

        if (config.EnableTimerResolution)
        {
            report.TimerResolutionApplied = _timerResolution.Apply(_session);
            logs.Add(report.TimerResolutionApplied
                ? "Timer resolution ajustado para baixa latencia."
                : "Timer resolution mantido.");
        }

        if (config.UseHighPerformancePlan)
        {
            _performance.EnableHighPerformancePowerPlan(_session);
            report.PowerPlanActivated = true;
            logs.Add("Plano de energia de alto desempenho ativado.");
            PersistRecoverableSessionState();
        }

        if (effectiveProfile.Equals("Ultra", StringComparison.OrdinalIgnoreCase))
        {
            report.UltraVisualTweaksApplied = _performance.ApplyUltraVisualTweaks(_session);
            logs.Add(report.UltraVisualTweaksApplied
                ? "Perfil Ultra: efeitos visuais do Windows reduzidos para modo basico."
                : "Perfil Ultra: ajustes visuais do Windows nao puderam ser aplicados por completo.");

            if (report.UltraVisualTweaksApplied)
                PersistRecoverableSessionState();
        }

        if (effectiveConfig.EnableTurboMode)
        {
            report.TurboModeApplied = _turboModeService.ApplyTurboMode(_session);
            logs.Add(report.TurboModeApplied
                ? "Modo TURBO FPS ativado."
                : "Modo TURBO FPS nao foi aplicado por completo.");

            if (report.TurboModeApplied)
                PersistRecoverableSessionState();
        }
        else
        {
            logs.Add("Modo TURBO FPS desativado na configuracao.");
        }

        var memoryOptimization = _memoryOptimizerService.Optimize(
            effectiveProfile,
            effectiveAllowedProcesses,
            emulatorProcesses.Select(static x => x.ProcessName).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        ApplyMemoryOptimizationToReport(report, memoryOptimization);
        logs.Add(memoryOptimization.Applied
            ? $"Memoria otimizada: {memoryOptimization.TrimmedProcessCount} processo(s) compactado(s), ~{memoryOptimization.EstimatedFreedMb:0.#} MB liberados."
            : $"Memoria otimizada: nenhuma compactacao relevante no perfil {effectiveProfile}.");

        if (config.EnableOverlayDetection)
        {
            var overlays = _overlayService.DetectOverlays();
            _session.DetectedOverlays = overlays;
            report.OverlayProcesses = overlays;
            report.OverlayCount = overlays.Count;

            if (overlays.Count > 0)
            {
                logs.Add($"Overlays detectados: {string.Join(", ", overlays)}");
                logs.Add("Sugestao: desative overlays nao essenciais para reduzir latencia.");
            }
            else
            {
                logs.Add("Nenhum overlay conhecido detectado.");
            }
        }

        report.Suggestions = BuildSuggestions(
            config,
            runningNames,
            effectiveAllowedProcesses,
            recorderProcesses.Count > 0);

        foreach (var suggestion in report.Suggestions)
        {
            logs.Add($"Sugestao: {suggestion}");
        }

        _session.IsGameModeActive = true;
        report.ProcessesAfter = Math.Max(0, report.ProcessesBefore - report.KilledCount);
        report.TopProcessesAfter = _processAnalyzer.GetTopProcesses();

        stopwatch.Stop();
        report.Elapsed = stopwatch.Elapsed;
        LastTechnicalReport = report;

        logs.Add($"Total encerrado: {report.KilledCount}");
        logs.Add($"Total suspenso: {report.SuspendedCount}");
        logs.Add($"Overlays detectados: {report.OverlayCount}");
        logs.Add($"Tempo da otimizacao: {report.Elapsed.TotalMilliseconds:0} ms");

        return ($"Modo jogo ativado.\n{report.KilledCount} encerrado(s), {report.SuspendedCount} suspenso(s).", logs);
    }

    public MemoryOptimizationExecutionResult OptimizeMemory(string profile, bool freeFireModeEnabled)
    {
        var logs = new List<string>();
        var config = _configService.Load();
        config.SelectedProfile = profile;
        config.EnableFreeFireMode = freeFireModeEnabled;

        var effectiveProfile = ResolveEffectiveProfile(config, logs);
        var effectiveConfig = CloneWithEffectiveProfile(config, effectiveProfile);
        var plan = _planBuilder.Build(effectiveConfig, recordingMode: false);
        var emulatorProcesses = _scanner.FindProcessesByNames(config.EmulatorProcesses);
        var result = _memoryOptimizerService.Optimize(
            effectiveProfile,
            plan.EffectiveAllowedProcesses,
            emulatorProcesses.Select(static x => x.ProcessName).Distinct(StringComparer.OrdinalIgnoreCase).ToList());

        logs.Add(result.Applied
            ? $"Memoria otimizada com perfil {effectiveProfile}: {result.TrimmedProcessCount} processo(s), ~{result.EstimatedFreedMb:0.#} MB liberados."
            : $"Memoria otimizada com perfil {effectiveProfile}: nenhuma sobra relevante foi encontrada.");

        if (result.TrimmedProcesses.Count > 0)
            logs.Add($"Compactados: {string.Join(", ", result.TrimmedProcesses.Take(6))}");

        return new MemoryOptimizationExecutionResult
        {
            Status = result.Applied
                ? $"Memoria otimizada. ~{result.EstimatedFreedMb:0.#} MB liberados."
                : "Memoria ja estava enxuta para este perfil.",
            Logs = logs,
            Result = result
        };
    }

    public (string status, List<string> logs) Restore()
    {
        var logs = new List<string>();

        if (!_session.IsGameModeActive)
        {
            logs.Add("Modo jogo nao estava ativo.");
            return ("Modo normal.", logs);
        }

        var resumed = _suspendService.ResumeProcesses(_session);
        if (resumed.Count > 0)
        {
            logs.Add($"Processos retomados: {string.Join(", ", resumed.Distinct(StringComparer.OrdinalIgnoreCase))}");
        }

        _performance.RestoreAffinities(_session);
        logs.Add("Afinidades restauradas.");

        _performance.RestorePriorities(_session);
        logs.Add("Prioridades restauradas.");

        _timerResolution.Restore(_session);
        logs.Add("Timer resolution restaurado.");

        _turboModeService.RestoreTurboMode(_session);
        logs.Add("Modo TURBO FPS restaurado.");

        _performance.RestorePowerPlan(_session);
        logs.Add("Plano de energia anterior restaurado.");

        _performance.RestoreUltraVisualTweaks(_session);
        logs.Add("Efeitos visuais do Windows restaurados.");

        _sessionStateStore.Clear();

        _session.IsGameModeActive = false;
        _session.ChangedPriorities.Clear();
        _session.ChangedAffinities.Clear();
        _session.KilledProcesses.Clear();
        _session.PreviousPowerSchemeGuid = null;
        _session.DetectedOverlays.Clear();
        _session.TurboUserPreferencesMaskBackup = null;
        _session.TurboModeApplied = false;
        _session.PreviousCurrentProcessPriority = null;
        _session.VisualEffectsSnapshot = null;
        _session.UltraVisualTweaksApplied = false;
        _session.ActiveProfile = "Seguro";

        return ("Modo normal restaurado.", logs);
    }

    public List<string> RecoverPendingSystemState()
    {
        var logs = new List<string>();
        var pendingState = _sessionStateStore.Load();

        if (pendingState is null)
            return logs;

        logs.Add($"Recuperacao detectada: sessao anterior encontrada ({pendingState.ActiveProfile}, {pendingState.SavedAtUtc:yyyy-MM-dd HH:mm:ss} UTC).");

        if (pendingState.UltraVisualTweaksApplied)
        {
            _performance.RestoreUltraVisualTweaks(pendingState);
            logs.Add("Recuperacao: efeitos visuais do Windows restaurados.");
        }

        if (pendingState.TurboModeApplied)
        {
            _turboModeService.RestoreTurboMode(pendingState);
            logs.Add("Recuperacao: modo TURBO FPS restaurado.");
        }

        if (pendingState.HighPerformanceApplied)
        {
            _performance.RestorePowerPlan(pendingState);
            logs.Add("Recuperacao: plano de energia anterior restaurado.");
        }

        _sessionStateStore.Clear();
        logs.Add("Recuperacao concluida.");
        return logs;
    }

    public bool IsGameModeActive()
    {
        return _session.IsGameModeActive;
    }

    public List<string> GetRunningProcesses()
    {
        return _scanner.GetRunningProcessNames();
    }

    public bool IsEmulatorRunning()
    {
        var config = _configService.Load();
        return _scanner.FindProcessesByNames(config.EmulatorProcesses).Any();
    }

    private List<string> BuildSuggestions(
        AppConfig config,
        IReadOnlyCollection<string> runningProcesses,
        IReadOnlyCollection<string> effectiveAllowedProcesses,
        bool recordingMode)
    {
        return _suggestionService.BuildSuggestions(
            config,
            runningProcesses,
            effectiveAllowedProcesses,
            recordingMode);
    }

    private void PersistRecoverableSessionState()
    {
        var state = new PerformanceSessionState
        {
            ActiveProfile = _session.ActiveProfile,
            PreviousPowerSchemeGuid = _session.PreviousPowerSchemeGuid,
            HighPerformanceApplied = !string.IsNullOrWhiteSpace(_session.PreviousPowerSchemeGuid),
            TurboModeApplied = _session.TurboModeApplied,
            TurboUserPreferencesMaskBackup = _session.TurboUserPreferencesMaskBackup,
            UltraVisualTweaksApplied = _session.UltraVisualTweaksApplied,
            VisualEffectsSnapshot = _session.VisualEffectsSnapshot
        };

        _sessionStateStore.Save(state);
    }

    private static void ApplyMemoryOptimizationToReport(TechnicalReport report, MemoryOptimizationResult result)
    {
        report.MemoryOptimizationApplied = result.Applied;
        report.MemoryOptimizedProcessCount = result.TrimmedProcessCount;
        report.MemoryRecoveredMb = result.EstimatedFreedMb;
        report.RamUsageBeforePercent = result.RamUsageBeforePercent;
        report.RamUsageAfterPercent = result.RamUsageAfterPercent;
        report.MemoryOptimizedProcesses = result.TrimmedProcesses;
    }

    private string ResolveEffectiveProfile(AppConfig config, List<string> logs)
    {
        if (!config.SelectedProfile.Equals("Auto", StringComparison.OrdinalIgnoreCase))
            return config.SelectedProfile;

        var totalRamGb = _metricsService.GetTotalRamGb();
        var usedRamGb = _metricsService.GetUsedRamGb();
        var effectiveProfile = _automaticProfileService.SelectProfile(usedRamGb, totalRamGb);
        logs.Add($"Perfil automatico escolhido: {effectiveProfile} (RAM {usedRamGb:0.##}/{totalRamGb:0.##} GB).");
        return effectiveProfile;
    }

    private static AppConfig CloneWithEffectiveProfile(AppConfig config, string effectiveProfile)
    {
        return new AppConfig
        {
            AllowedProcesses = new List<string>(config.AllowedProcesses),
            FreeFireAllowedProcesses = new List<string>(config.FreeFireAllowedProcesses),
            SafeBlacklist = new List<string>(config.SafeBlacklist),
            StrongBlacklist = new List<string>(config.StrongBlacklist),
            UltraBlacklist = new List<string>(config.UltraBlacklist),
            FreeFireSafeBlacklist = new List<string>(config.FreeFireSafeBlacklist),
            FreeFireStrongBlacklist = new List<string>(config.FreeFireStrongBlacklist),
            FreeFireUltraBlacklist = new List<string>(config.FreeFireUltraBlacklist),
            RecordingProcesses = new List<string>(config.RecordingProcesses),
            EmulatorProcesses = new List<string>(config.EmulatorProcesses),
            UseHighPerformancePlan = config.UseHighPerformancePlan,
            SetEmulatorHighPriority = config.SetEmulatorHighPriority,
            EnableTimerResolution = config.EnableTimerResolution,
            EnableAffinityTuning = config.EnableAffinityTuning,
            EnableOverlayDetection = config.EnableOverlayDetection,
            EnableWatcher = config.EnableWatcher,
            EnableTurboMode = config.EnableTurboMode,
            TelemetryEnabled = config.TelemetryEnabled,
            AutoOptimizeOnStartup = config.AutoOptimizeOnStartup,
            LaunchOnWindowsStartup = config.LaunchOnWindowsStartup,
            StartupPreferenceInitialized = config.StartupPreferenceInitialized,
            EnableFreeFireMode = config.EnableFreeFireMode,
            SelectedProfile = effectiveProfile
        };
    }
}
