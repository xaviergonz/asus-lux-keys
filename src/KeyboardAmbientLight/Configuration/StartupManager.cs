using KeyboardAmbientLight.Logging;
using Microsoft.Win32;

namespace KeyboardAmbientLight.Configuration;

public static class StartupManager
{
    private const string AppName = "KeyboardAmbientLight";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(AppName) is string;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Failed to read startup setting: {ex.Message}");
            return false;
        }
    }

    public static bool SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (enabled)
            {
                key.SetValue(AppName, Quote(Application.ExecutablePath), RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
            }

            return true;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Failed to update startup setting: {ex.Message}");
            return false;
        }
    }

    private static string Quote(string path)
    {
        return $"\"{path}\"";
    }
}
