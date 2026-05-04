using AsusLuxKeys.Ambient;
using AsusLuxKeys.Configuration;
using AsusLuxKeys.Hardware;
using AsusLuxKeys.Logging;
using AsusLuxKeys.Rules;
using AsusLuxKeys.UI;

namespace AsusLuxKeys.Tray;

public sealed class AsusLuxKeysAppContext : ApplicationContext
{
    private readonly SettingsStore _settingsStore;
    private readonly LightSensorService _lightSensor;
    private readonly AsusStaticKeyboardLightController _keyboard;
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _icon;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly SemaphoreSlim _reconcileGate = new(1, 1);

    private AppSettings _settings;
    private bool _runOnStartup;
    private OptionsForm? _optionsForm;

    public AsusLuxKeysAppContext(LightSensorService lightSensor, AsusStaticKeyboardLightController keyboard, Icon icon)
    {
        _settingsStore = new SettingsStore();
        _settings = _settingsStore.Load();
        _runOnStartup = StartupManager.IsEnabled();
        _lightSensor = lightSensor;
        _keyboard = keyboard;
        _icon = icon;

        _notifyIcon = new NotifyIcon
        {
            Icon = _icon,
            Text = AppInfo.DisplayName,
            ContextMenuStrip = BuildContextMenu(),
            Visible = true
        };
        _notifyIcon.MouseUp += NotifyIcon_MouseUp;

        _timer = new System.Windows.Forms.Timer
        {
            Interval = AppTiming.ReconcileIntervalMilliseconds
        };
        _timer.Tick += async (_, _) => await ReconcileAsync();
        _timer.Start();

        _ = ReconcileAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _icon.Dispose();
            _lightSensor.Dispose();
            _keyboard.Dispose();
            _reconcileGate.Dispose();
        }

        base.Dispose(disposing);
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Options", null, (_, _) => ShowOptions());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => ExitThread());
        return menu;
    }

    private void NotifyIcon_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ShowOptions();
        }
    }

    private void ShowOptions()
    {
        if (_optionsForm is { IsDisposed: false })
        {
            _optionsForm.Activate();
            return;
        }

        _optionsForm = new OptionsForm(
            _settings,
            () => _lightSensor.GetCurrentLux(),
            _keyboard.CanSetStaticColor,
            _runOnStartup,
            _icon);
        _optionsForm.SettingsSaved += (_, args) =>
        {
            _settings = args.Settings;
            _settingsStore.Save(_settings);
            if (args.RunOnStartup != _runOnStartup && !StartupManager.SetEnabled(args.RunOnStartup))
            {
                MessageBox.Show(
                    _optionsForm,
                    "The settings were saved, but the startup setting could not be updated.",
                    AppInfo.DisplayName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            else
            {
                _runOnStartup = args.RunOnStartup;
            }
        };
        _optionsForm.Show();
        _optionsForm.Activate();
    }

    private async Task ReconcileAsync()
    {
        if (!await _reconcileGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            if (!_settings.Enabled)
            {
                return;
            }

            var lux = _lightSensor.GetCurrentLux();
            if (lux is null)
            {
                return;
            }

            var brightness = BrightnessRuleEngine.GetBrightness(lux.Value, _settings.Rules);
            var color = _keyboard.CanSetStaticColor ? SettingsStore.ParseColor(_settings.Color) : (Color?)null;
            if (_keyboard.CanReadBrightness && _keyboard.GetCurrentBrightness() == brightness)
            {
                return;
            }

            await _keyboard.SetAsync(brightness, color);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Reconcile failed: {ex.Message}");
        }
        finally
        {
            _reconcileGate.Release();
        }
    }
}
