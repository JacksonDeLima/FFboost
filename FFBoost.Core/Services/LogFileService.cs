namespace FFBoost.Core.Services;

public class LogFileService
{
    private const int MaxLogFiles = 30;
    private readonly string _logDirectory;

    public LogFileService(string basePath)
    {
        _logDirectory = Path.Combine(basePath, "logs");
        Directory.CreateDirectory(_logDirectory);
    }

    public string SaveLog(IEnumerable<string> logs)
    {
        var fileName = $"ffboost_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        var filePath = Path.Combine(_logDirectory, fileName);

        File.WriteAllLines(filePath, logs);
        TrimOldLogs();
        return filePath;
    }

    private void TrimOldLogs()
    {
        var files = new DirectoryInfo(_logDirectory)
            .GetFiles("ffboost_*.txt")
            .OrderByDescending(static file => file.CreationTimeUtc)
            .ToList();

        foreach (var file in files.Skip(MaxLogFiles))
        {
            try
            {
                file.Delete();
            }
            catch
            {
            }
        }
    }
}
