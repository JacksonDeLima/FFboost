namespace FFBoost.Core.Services;

public class LogService
{
    private readonly string _logPath;

    public LogService(string logPath)
    {
        _logPath = logPath;
    }

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Warning(string message)
    {
        Write("WARN", message);
    }

    public void Error(string message)
    {
        Write("ERROR", message);
    }

    public void InfoBlock(string title, IEnumerable<string> lines)
    {
        WriteBlock("INFO", title, lines);
    }

    private void Write(string level, string message)
    {
        try
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
            File.AppendAllText(_logPath, line);
        }
        catch
        {
        }
    }

    private void WriteBlock(string level, string title, IEnumerable<string> lines)
    {
        try
        {
            var content = new List<string>
            {
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {title}"
            };

            content.AddRange(lines.Select(static line => $"  - {line}"));
            content.Add(string.Empty);

            File.AppendAllText(_logPath, string.Join(Environment.NewLine, content));
        }
        catch
        {
        }
    }
}
