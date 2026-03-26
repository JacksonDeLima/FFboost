using System.Diagnostics;
using FFBoost.Core.Models;
using FFBoost.Core.Rules;

namespace FFBoost.Core.Services;

public class OptimizerService
{
    private static readonly HashSet<string> PreserveDuringRecording = new(StringComparer.OrdinalIgnoreCase)
    {
        "discord",
        "discordptb",
        "obs64",
        "medal",
        "action",
        "streamlabsobs"
    };

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
    private readonly ProcessRules _rules;
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
        ProcessRules rules)
    {
        _configService = configService;
        _scanner = scanner;
        _killer = killer;
        _suspendService = suspendService;
        _performance = performance;
        _timerResolution = timerResolution;
        _overlayService = overlayService;
        _explorerWindowService = explorerWindowService;
        _rules = rules;
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
        var running = _scanner.GetRunningProcesses();
        var runningNames = running
            .Select(p => p.ProcessName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var emulatorProcesses = _scanner.FindProcessesByNames(config.EmulatorProcesses);
        var recorderProcesses = _scanner.FindProcessesByNames(config.RecordingProcesses);

        report.Profile = config.SelectedProfile;
        report.FreeFireModeEnabled = config.EnableFreeFireMode;
        report.ProcessesBefore = running.Count;
        report.RecordingModeDetected = recorderProcesses.Count > 0;

        if (!emulatorProcesses.Any())
        {
            logs.Add("BlueStacks nao detectado.");
            LastTechnicalReport = report;
            return ("BlueStacks nao detectado. Abra o emulador antes de otimizar.", logs);
        }

        var effectiveAllowedProcesses = GetAllowedProcesses(config, report.RecordingModeDetected);

        _session.ActiveProfile = config.SelectedProfile;

        logs.Add($"Emulador detectado: {string.Join(", ", emulatorProcesses.Select(p => p.ProcessName).Distinct())}");
        logs.Add($"Perfil selecionado: {config.SelectedProfile}");
        logs.Add(config.EnableFreeFireMode
            ? "Modo Free Fire + BlueStacks ativo."
            : "Modo padrao de otimizacao ativo.");

        if (report.RecordingModeDetected)
        {
            logs.Add($"Modo gravacao detectado: {string.Join(", ", recorderProcesses.Select(p => p.ProcessName).Distinct())}");
        }

        var activeKillBlacklist = GetKillBlacklistByProfile(config, report.RecordingModeDetected);
        var activeSuspendBlacklist = GetSuspendBlacklistByProfile(config, report.RecordingModeDetected);

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
        }

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

        stopwatch.Stop();
        report.Elapsed = stopwatch.Elapsed;
        LastTechnicalReport = report;

        logs.Add($"Total encerrado: {report.KilledCount}");
        logs.Add($"Total suspenso: {report.SuspendedCount}");
        logs.Add($"Overlays detectados: {report.OverlayCount}");
        logs.Add($"Tempo da otimizacao: {report.Elapsed.TotalMilliseconds:0} ms");

        return ($"Modo jogo ativado.\n{report.KilledCount} encerrado(s), {report.SuspendedCount} suspenso(s).", logs);
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

        _performance.RestorePowerPlan(_session);
        logs.Add("Plano de energia anterior restaurado.");

        _session.IsGameModeActive = false;
        _session.ChangedPriorities.Clear();
        _session.ChangedAffinities.Clear();
        _session.KilledProcesses.Clear();
        _session.PreviousPowerSchemeGuid = null;
        _session.DetectedOverlays.Clear();
        _session.ActiveProfile = "Seguro";

        return ("Modo normal restaurado.", logs);
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

    private static List<string> GetKillBlacklistByProfile(AppConfig config, bool recordingMode)
    {
        var result = new List<string>(config.SafeBlacklist);

        if (config.EnableFreeFireMode)
            result.AddRange(config.FreeFireSafeBlacklist);

        if (config.SelectedProfile.Equals("Forte", StringComparison.OrdinalIgnoreCase) ||
            config.SelectedProfile.Equals("Ultra", StringComparison.OrdinalIgnoreCase))
        {
            result.AddRange(config.StrongBlacklist);

            if (config.EnableFreeFireMode)
                result.AddRange(config.FreeFireStrongBlacklist);
        }

        if (config.SelectedProfile.Equals("Ultra", StringComparison.OrdinalIgnoreCase))
        {
            result.AddRange(config.UltraBlacklist);

            if (config.EnableFreeFireMode)
                result.AddRange(config.FreeFireUltraBlacklist);
        }

        if (recordingMode)
            result.RemoveAll(ShouldPreserveWhileRecording);

        return result
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> GetSuspendBlacklistByProfile(AppConfig config, bool recordingMode)
    {
        var result = new List<string>();

        if (recordingMode)
            result.RemoveAll(ShouldPreserveWhileRecording);

        return result
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> BuildSuggestions(
        AppConfig config,
        IReadOnlyCollection<string> runningProcesses,
        IReadOnlyCollection<string> effectiveAllowedProcesses,
        bool recordingMode)
    {
        var suggestions = new List<string>();

        if (recordingMode)
            suggestions.Add("Modo gravacao ativo: prefira o perfil Seguro ou Forte para estabilidade da captura.");

        if (config.EnableFreeFireMode)
            suggestions.Add("Modo Free Fire ativo: a whitelist protege BlueStacks, Discord e gravadores autorizados.");

        if (runningProcesses.Contains("Discord", StringComparer.OrdinalIgnoreCase) &&
            !effectiveAllowedProcesses.Contains("Discord", StringComparer.OrdinalIgnoreCase))
        {
            suggestions.Add("Considere permitir o Discord se ele for essencial durante a partida.");
        }

        if (runningProcesses.Contains("steamwebhelper", StringComparer.OrdinalIgnoreCase))
            suggestions.Add("Feche o Steam overlay se nao estiver usando recursos sociais.");

        return suggestions;
    }

    private static List<string> GetAllowedProcesses(AppConfig config, bool recordingMode)
    {
        var result = new List<string>(config.AllowedProcesses);

        if (config.EnableFreeFireMode)
            result.AddRange(config.FreeFireAllowedProcesses);

        if (recordingMode)
            result.AddRange(PreserveDuringRecording);

        return result
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool ShouldPreserveWhileRecording(string processName)
    {
        return PreserveDuringRecording.Contains(processName);
    }
}
