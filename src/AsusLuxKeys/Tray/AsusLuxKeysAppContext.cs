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
    private KeyboardLightState? _pendingState;
    private int _pendingStateTicks;
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
        _timer.Tick += async (_, _) => await ReconcileAsync(countAsStableTick: true);
        _lightSensor.ReadingChanged += async (_, _) => await ReconcileAsync(countAsStableTick: false);
        _timer.Start();

        _ = ReconcileAsync(countAsStableTick: true);
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
        _optionsForm.SettingsSaved += async (_, args) =>
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

            await ReconcileAsync(countAsStableTick: false);
        };
        _optionsForm.Show();
        _optionsForm.Activate();
    }

    private async Task ReconcileAsync(bool countAsStableTick)
    {
        if (!await _reconcileGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            if (!_settings.Enabled)
            {
                ResetPendingState();
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
            if (_keyboard.LastApplied == desired)
            {
                ResetPendingState();
                return;
            }

            if (!ShouldApplyDesiredState(desired, countAsStableTick, IsOptionsFormOpen))
            {
                return;
            }

            await _keyboard.SetAsync(brightness, color);
            ResetPendingState();
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

    private bool ShouldApplyDesiredState(KeyboardLightState desired, bool scheduledTick, bool optionsFormOpen)
    {
        if (optionsFormOpen)
        {
            ResetPendingState();
            return true;
        }

        if (_pendingState != desired)
        {
            _pendingState = desired;
            _pendingStateTicks = scheduledTick ? 1 : 0;
            AppLog.Write($"Pending keyboard state {desired.Brightness.ToDisplayText()}.");
            return false;
        }

        if (scheduledTick)
        {
            _pendingStateTicks++;
        }

        return _pendingStateTicks >= AppTiming.RequiredStableSignalTicks;
    }

    private void ResetPendingState()
    {
        _pendingState = null;
        _pendingStateTicks = 0;
    }

    private bool IsOptionsFormOpen => _optionsForm is { IsDisposed: false };
}
