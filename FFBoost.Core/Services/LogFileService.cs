namespace FFBoost.Core.Services;

public class LogFileService
{
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
        return filePath;
    }
}
