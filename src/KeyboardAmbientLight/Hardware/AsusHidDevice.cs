using System.Drawing;
using HidSharp;
using HidSharp.Reports;
using KeyboardAmbientLight.Logging;

namespace KeyboardAmbientLight.Hardware;

public sealed class AsusHidDevice
{
    private const int AsusVendorId = 0x0B05;
    private const byte InputReportId = 0x5A;
    private const byte AuraReportId = 0x5D;

    private static readonly int[] MainAuraProductIds =
    [
        0x1A30, 0x1854, 0x1869, 0x1866, 0x19B6, 0x1822, 0x1837, 0x184A, 0x183D,
        0x8502, 0x1807, 0x17E0, 0x1ABE, 0x1B4C, 0x1B6E, 0x1B2C, 0x8854, 0x1CE7
    ];

    private static readonly byte[] MessageSet = [AuraReportId, 0xB5, 0, 0, 0];
    private static readonly byte[] MessageApply = [AuraReportId, 0xB4];

    public bool CanSetBrightness => FindDevices(InputReportId).Any();

    public bool CanSetStaticColor => FindDevices(AuraReportId, MainAuraProductIds).Any();

    public void SetBrightnessLevel(int level)
    {
        WriteInput([InputReportId, 0xBA, 0xC5, 0xC4, (byte)Math.Clamp(level, 0, 3)], "HID keyboard brightness");
    }

    public void SetStaticColor(Color color)
    {
        var message = AuraMessage(color);
        WriteAura([message, MessageSet, MessageApply], "HID static keyboard color");
    }

    private void WriteInput(byte[] data, string logName)
    {
        foreach (var device in FindDevices(InputReportId))
        {
            try
            {
                using var stream = device.Open();
                var payload = new byte[device.GetMaxFeatureReportLength()];
                Array.Copy(data, payload, Math.Min(data.Length, payload.Length));
                stream.SetFeature(payload);
                AppLog.Write($"{logName} {device.ProductID:X4}: {BitConverter.ToString(data)}");
            }
            catch (Exception ex)
            {
                AppLog.Write($"Failed to write {logName} to {device.ProductID:X4}: {ex.Message}");
            }
        }
    }

    private void WriteAura(IReadOnlyCollection<byte[]> messages, string logName)
    {
        foreach (var device in FindDevices(AuraReportId, MainAuraProductIds))
        {
            try
            {
                using var stream = device.Open();
                foreach (var message in messages)
                {
                    stream.Write(message);
                    AppLog.Write($"{logName} {device.ProductID:X4}: {BitConverter.ToString(message)}");
                }
            }
            catch (Exception ex)
            {
                AppLog.Write($"Failed to write {logName} to {device.ProductID:X4}: {ex.Message}");
            }
        }
    }

    private static byte[] AuraMessage(Color color)
    {
        byte[] message = new byte[17];
        message[0] = AuraReportId;
        message[1] = 0xB3;
        message[2] = 0x00;
        message[3] = (byte)AuraMode.Static;
        message[4] = color.R;
        message[5] = color.G;
        message[6] = color.B;
        message[7] = 0xEB;
        message[8] = 0x00;
        message[9] = 0x00;
        message[10] = color.R;
        message[11] = color.G;
        message[12] = color.B;
        return message;
    }

    private IEnumerable<HidDevice> FindDevices(byte reportId, int[]? productIds = null)
    {
        IEnumerable<HidDevice> devices;

        try
        {
            devices = DeviceList.Local.GetHidDevices(AsusVendorId)
                .Where(device => device.CanOpen)
                .Where(device => productIds is null || productIds.Contains(device.ProductID))
                .ToList();
        }
        catch (Exception ex)
        {
            AppLog.Write($"Failed to enumerate ASUS HID devices: {ex.Message}");
            yield break;
        }

        foreach (var device in devices)
        {
            var valid = false;
            try
            {
                valid = device.GetMaxFeatureReportLength() > 0 &&
                    device.GetReportDescriptor().TryGetReport(ReportType.Feature, reportId, out _);
            }
            catch (Exception ex)
            {
                AppLog.Write($"Failed to inspect ASUS HID device {device.ProductID:X4}: {ex.Message}");
            }

            if (valid)
            {
                yield return device;
            }
        }
    }
}
