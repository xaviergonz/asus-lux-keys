namespace AsusLuxKeys.Logging;

public static class AppLog
{
    private static readonly FileAppLog FileLog = new();

    public static void Write(string message)
    {
        FileLog.Write(message);
    }
}
