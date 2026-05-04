namespace AsusLuxKeys.Logging;

internal sealed class FileAppLog
{
    private const long MaxLogBytes = 500 * 1024;

    private readonly object _lock = new();

    public string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppInfo.DisplayName);

    public string LogPath => Path.Combine(LogDirectory, "log.txt");

    public string PreviousLogPath => Path.Combine(LogDirectory, "log.previous.txt");

    public void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            lock (_lock)
            {
                RotateIfNeeded();
                File.AppendAllText(LogPath, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never bring down a tray utility.
        }
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(LogPath) || new FileInfo(LogPath).Length < MaxLogBytes)
        {
            return;
        }

        File.Delete(PreviousLogPath);
        File.Move(LogPath, PreviousLogPath);
    }
}
