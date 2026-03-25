using FFBoost.Core.Models;
using FFBoost.Core.Rules;

namespace FFBoost.Core.Services;

public class OptimizerService
{
    private readonly ConfigService _configService;
    private readonly ProcessScanner _scanner;
    private readonly ProcessKiller _killer;
    private readonly PerformanceManager _performance;
    private readonly ProcessRules _rules;

    private readonly OptimizationSession _session = new();

    public OptimizerService(
        ConfigService configService,
        ProcessScanner scanner,
        ProcessKiller killer,
        PerformanceManager performance,
        ProcessRules rules)
    {
        _configService = configService;
        _scanner = scanner;
        _killer = killer;
        _performance = performance;
        _rules = rules;
    }

    public (string status, List<string> logs) StartGameMode()
    {
        var logs = new List<string>();

        if (_session.IsGameModeActive)
        {
            logs.Add("Modo jogo ja esta ativo.");
            return ("Modo jogo ja ativo.", logs);
        }

        var config = _configService.Load();
        var running = _scanner.GetRunningProcesses();
        var emulatorProcesses = _scanner.FindProcessesByNames(config.EmulatorProcesses);
        var activeBlacklist = GetBlacklistByProfile(config);

        if (!emulatorProcesses.Any())
        {
            logs.Add("BlueStacks nao detectado.");
            return ("BlueStacks nao detectado.", logs);
        }

        logs.Add($"Emulador detectado: {string.Join(", ", emulatorProcesses.Select(static p => p.ProcessName).Distinct())}");
        logs.Add($"Perfil selecionado: {config.SelectedProfile}");
        logs.Add($"Blacklist ativa: {activeBlacklist.Count} processo(s) configurado(s).");

        var toClose = running
            .Where(p =>
                !_rules.IsCritical(p.ProcessName) &&
                !_rules.IsAllowed(p.ProcessName, config.AllowedProcesses) &&
                activeBlacklist.Contains(p.ProcessName, StringComparer.OrdinalIgnoreCase))
            .ToList();

        foreach (var process in toClose)
            logs.Add($"Encerrando: {process.ProcessName}");

        var killResult = _killer.KillProcesses(toClose);
        _session.KilledProcesses.AddRange(killResult.KilledProcesses);

        foreach (var failed in killResult.FailedProcesses.Distinct(StringComparer.OrdinalIgnoreCase))
            logs.Add($"Falha ao encerrar: {failed}");

        if (config.SetEmulatorHighPriority)
        {
            _performance.SetHighPriority(emulatorProcesses, _session);
            logs.Add("Prioridade alta aplicada ao emulador.");
        }

        if (config.UseHighPerformancePlan)
        {
            _performance.EnableHighPerformancePowerPlan(_session);
            logs.Add("Plano de energia de alto desempenho ativado.");
        }

        logs.Add($"Total encerrado: {killResult.KilledProcesses.Count}");
        logs.Add($"Falhas ao encerrar: {killResult.FailedProcesses.Count}");
        _session.IsGameModeActive = true;

        return ($"Modo jogo ativado. {killResult.KilledProcesses.Count} processo(s) encerrado(s).", logs);
    }

    public (string status, List<string> logs) Restore()
    {
        var logs = new List<string>();

        if (!_session.IsGameModeActive)
        {
            logs.Add("Modo jogo nao estava ativo.");
            return ("Modo normal.", logs);
        }

        _performance.RestorePriorities(_session);
        logs.Add("Prioridades restauradas.");

        _performance.RestorePowerPlan(_session);
        logs.Add("Plano de energia anterior restaurado.");

        _session.IsGameModeActive = false;
        _session.ChangedPriorities.Clear();
        _session.KilledProcesses.Clear();
        _session.PreviousPowerSchemeGuid = null;

        return ("Modo normal restaurado.", logs);
    }

    public List<string> GetRunningProcesses()
    {
        return _scanner.GetRunningProcessNames();
    }

    private static List<string> GetBlacklistByProfile(AppConfig config)
    {
        var result = new List<string>();

        result.AddRange(config.SafeBlacklist);

        if (config.SelectedProfile.Equals("Forte", StringComparison.OrdinalIgnoreCase) ||
            config.SelectedProfile.Equals("Ultra", StringComparison.OrdinalIgnoreCase))
        {
            result.AddRange(config.StrongBlacklist);
        }

        if (config.SelectedProfile.Equals("Ultra", StringComparison.OrdinalIgnoreCase))
        {
            result.AddRange(config.UltraBlacklist);
        }

        return result
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
