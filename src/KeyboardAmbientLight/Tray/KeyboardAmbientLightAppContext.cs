using KeyboardAmbientLight.Ambient;
using KeyboardAmbientLight.Configuration;
using KeyboardAmbientLight.Hardware;
using KeyboardAmbientLight.Logging;
using KeyboardAmbientLight.Rules;
using KeyboardAmbientLight.UI;

namespace KeyboardAmbientLight.Tray;

public sealed class KeyboardAmbientLightAppContext : ApplicationContext
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

    public KeyboardAmbientLightAppContext(LightSensorService lightSensor, AsusStaticKeyboardLightController keyboard)
    {
        _settingsStore = new SettingsStore();
        _settings = _settingsStore.Load();
        _runOnStartup = StartupManager.IsEnabled();
        _lightSensor = lightSensor;
        _keyboard = keyboard;
        _icon = AppIconFactory.CreateIcon();

        _notifyIcon = new NotifyIcon
        {
            Icon = _icon,
            Text = "Keyboard Ambient Light",
            ContextMenuStrip = BuildContextMenu(),
            Visible = true
        };
        _notifyIcon.MouseUp += NotifyIcon_MouseUp;

        _timer = new System.Windows.Forms.Timer
        {
            Interval = AppTiming.ReconcileIntervalMilliseconds
        };
        _timer.Tick += async (_, _) => await ReconcileAsync(forceApply: true);
        _lightSensor.ReadingChanged += async (_, _) => await ReconcileAsync(forceApply: false);
        _timer.Start();

        _ = ReconcileAsync(forceApply: true);
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
            _runOnStartup);
        _optionsForm.SettingsSaved += async (_, args) =>
        {
            _settings = args.Settings;
            _settingsStore.Save(_settings);
            if (args.RunOnStartup != _runOnStartup && !StartupManager.SetEnabled(args.RunOnStartup))
            {
                MessageBox.Show(
                    _optionsForm,
                    "The settings were saved, but the startup setting could not be updated.",
                    "Keyboard Ambient Light",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            else
            {
                _runOnStartup = args.RunOnStartup;
            }

            await ReconcileAsync(forceApply: true);
        };
        _optionsForm.Show();
        _optionsForm.Activate();
    }

    private async Task ReconcileAsync(bool forceApply)
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
            var desired = new KeyboardLightState(color, brightness);
            if (!forceApply && _keyboard.LastApplied == desired)
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
