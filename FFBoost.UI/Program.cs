using FFBoost.Core.Services;
using System.Text;
using System.Windows.Forms;

namespace FFBoost.UI;

internal static class Program
{
    private static LogService? _crashLog;
    private static int _exceptionDialogActive;

    [STAThread]
    private static void Main(string[] args)
    {
        ConfigureGlobalExceptionHandling();
        ApplicationConfiguration.Initialize();

        var startInTray = args.Any(x => string.Equals(x, "--tray", StringComparison.OrdinalIgnoreCase));
        if (!startInTray)
        {
            using var splash = new SplashForm();
            splash.ShowDialog();
        }

        Application.Run(AppBootstrapper.CreateMainForm(args));
    }

    private static void ConfigureGlobalExceptionHandling()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var logDirectory = Path.Combine(baseDirectory, "logs");
        Directory.CreateDirectory(logDirectory);
        _crashLog = new LogService(Path.Combine(logDirectory, "ui_crash.log"));

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) => HandleUnhandledException("UI Thread", args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            HandleUnhandledException("AppDomain", args.ExceptionObject as Exception ?? new Exception("Falha nao tratada sem detalhes."));
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            HandleUnhandledException("TaskScheduler", args.Exception);
            args.SetObserved();
        };
    }

    private static void HandleUnhandledException(string source, Exception exception)
    {
        try
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Origem: {source}");
            builder.AppendLine($"Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine($"Mensagem: {exception.Message}");
            builder.AppendLine("Stack:");
            builder.AppendLine(exception.ToString());
            _crashLog?.Error(builder.ToString());
        }
        catch
        {
        }

        if (Interlocked.Exchange(ref _exceptionDialogActive, 1) == 1)
            return;

        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "ui_crash.log");
            MessageBox.Show(
                "FF Boost encontrou um erro inesperado, mas bloqueou a caixa tecnica do .NET para evitar interrupcao bruta.\n\n" +
                $"Um log foi salvo em:\n{logPath}\n\n" +
                "Feche e abra novamente o app. Se o problema se repetir, envie esse log.",
                "FF Boost - Erro Controlado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Interlocked.Exchange(ref _exceptionDialogActive, 0);
        }
    }
}
