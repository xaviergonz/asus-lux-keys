using AsusLuxKeys.Configuration;

namespace AsusLuxKeys.UI;

public sealed class OptionsSavedEventArgs : EventArgs
{
    public required AppSettings Settings { get; init; }

    public required bool RunOnStartup { get; init; }
}
