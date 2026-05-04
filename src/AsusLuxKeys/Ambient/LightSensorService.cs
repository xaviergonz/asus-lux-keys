using AsusLuxKeys.Logging;
using Windows.Devices.Sensors;

namespace AsusLuxKeys.Ambient;

public sealed class LightSensorService : IDisposable
{
    private readonly LightSensor? _sensor;

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
            _sensor.ReportInterval = 0;
        }
    }
}
