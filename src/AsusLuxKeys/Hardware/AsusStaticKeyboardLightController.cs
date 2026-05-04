using System.Drawing;

namespace AsusLuxKeys.Hardware;

public sealed class AsusStaticKeyboardLightController : IDisposable
{
    private readonly AsusHidDevice _hid;
    private readonly AsusAcpiDevice _acpi;
    private readonly bool _canSetHidBrightness;
    private readonly bool _canSetAcpiBrightness;
    private readonly bool _canSetHidStaticColor;
    private readonly bool _canSetAcpiStaticColor;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public AsusStaticKeyboardLightController()
    {
        _hid = new AsusHidDevice();
        _acpi = new AsusAcpiDevice();
        _canSetHidBrightness = _hid.CanSetBrightness;
        _canSetAcpiBrightness = _acpi.CanSetBrightness;
        _canSetHidStaticColor = _hid.CanSetStaticColor;
        _canSetAcpiStaticColor = _acpi.CanSetStaticColor;

        if (_canSetHidStaticColor)
        {
            _hid.InitializeAuraLighting();
        }
    }

    public bool CanSetBrightness => _canSetHidBrightness || _canSetAcpiBrightness;

    public bool CanReadBrightness => _canSetAcpiBrightness;

    public bool CanSetStaticColor => _canSetHidStaticColor || _canSetAcpiStaticColor;

    public KeyboardBrightness? GetCurrentBrightness()
    {
        return _canSetAcpiBrightness ? _acpi.GetBrightness() : null;
    }

    public async Task SetAsync(KeyboardBrightness brightness, Color? color, CancellationToken cancellationToken = default)
    {
        var level = brightness.ToHardwareLevel();
        var colorToSet = CanSetStaticColor ? color : null;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_canSetHidBrightness)
                {
                    _hid.SetBrightnessLevel(level);
                }

                if (_canSetHidStaticColor)
                {
                    _hid.SetAuraBrightnessLevel(level);
                }

                if (_canSetAcpiBrightness)
                {
                    _acpi.SetBrightnessLevel(level);
                }

                if (level > 0 && colorToSet is { } staticColor)
                {
                    // Static is the only supported effect, so every color write forces static mode.
                    if (_canSetHidStaticColor)
                    {
                        _hid.SetStaticColor(staticColor);
                    }

                    if (_canSetAcpiStaticColor)
                    {
                        _acpi.SetStaticColor(staticColor);
                    }
                }
            }, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _gate.Dispose();
        _acpi.Dispose();
    }
}
