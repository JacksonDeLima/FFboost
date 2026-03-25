using System.Threading;

namespace FFBoost.Core.Services;

public class GameWatcherService : IDisposable
{
    private readonly ProcessScanner _scanner;
    private readonly ConfigService _configService;
    private Timer? _timer;
    private bool _wasRunning;

    public event Action? EmulatorStarted;
    public event Action? EmulatorStopped;

    public GameWatcherService(ProcessScanner scanner, ConfigService configService)
    {
        _scanner = scanner;
        _configService = configService;
    }

    public void Start()
    {
        _timer ??= new Timer(CheckState, null, 2000, 3000);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public void Dispose()
    {
        Stop();
    }

    private void CheckState(object? _)
    {
        var config = _configService.Load();
        var running = _scanner.FindProcessesByNames(config.EmulatorProcesses).Any();

        if (running && !_wasRunning)
            EmulatorStarted?.Invoke();
        else if (!running && _wasRunning)
            EmulatorStopped?.Invoke();

        _wasRunning = running;
    }
}
