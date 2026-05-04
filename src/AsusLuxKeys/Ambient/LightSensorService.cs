using AsusLuxKeys.Logging;
using Windows.Devices.Sensors;

namespace AsusLuxKeys.Ambient;

public sealed class LightSensorService : IDisposable
{
    private readonly LightSensor? _sensor;

    public event EventHandler<double>? ReadingChanged;

    public LightSensorService()
    {
        try
        {
            _sensor = LightSensor.GetDefault();
            if (_sensor is null)
            {
                AppLog.Write("No ambient light sensor was found.");
                return;
            }

            _sensor.ReportInterval = Math.Max(_sensor.MinimumReportInterval, AppTiming.ReconcileIntervalMilliseconds);
            LatestLux = _sensor.GetCurrentReading()?.IlluminanceInLux;
            _sensor.ReadingChanged += SensorReadingChanged;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Failed to initialize ambient light sensor: {ex.Message}");
        }
    }

    public bool IsAvailable => _sensor is not null;

    public double? LatestLux { get; private set; }

    public double? GetCurrentLux()
    {
        if (_sensor is null)
        {
            return LatestLux;
        }

        try
        {
            LatestLux = _sensor.GetCurrentReading()?.IlluminanceInLux;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Failed to read ambient light sensor: {ex.Message}");
        }

        return LatestLux;
    }

    public void Dispose()
    {
        if (_sensor is not null)
        {
            _sensor.ReadingChanged -= SensorReadingChanged;
            _sensor.ReportInterval = 0;
        }
    }

    private void SensorReadingChanged(LightSensor sender, LightSensorReadingChangedEventArgs args)
    {
        LatestLux = args.Reading.IlluminanceInLux;
        ReadingChanged?.Invoke(this, LatestLux.Value);
    }
}
