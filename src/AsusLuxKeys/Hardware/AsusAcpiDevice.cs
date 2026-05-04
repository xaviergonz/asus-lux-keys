using System.Drawing;
using System.Runtime.InteropServices;
using AsusLuxKeys.Logging;

namespace AsusLuxKeys.Hardware;

public sealed class AsusAcpiDevice : IDisposable
{
    private const string FileName = @"\\.\\ATKACPI";
    private const uint ControlCode = 0x0022240C;
    private const uint Dsts = 0x53545344;
    private const uint Devs = 0x53564544;

    private const uint TufKeyboardBrightness = 0x00050021;
    private const uint TufKeyboard = 0x00100056;
    private const uint TufKeyboard2 = 0x0010005A;

    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint OpenExisting = 3;
    private const uint FileAttributeNormal = 0x80;
    private const uint FileShareRead = 1;
    private const uint FileShareWrite = 2;

    private readonly IntPtr _handle;
    private readonly Dictionary<uint, bool> _supportCache = [];

    public AsusAcpiDevice()
    {
        _handle = CreateFile(
            FileName,
            GenericRead | GenericWrite,
            FileShareRead | FileShareWrite,
            IntPtr.Zero,
            OpenExisting,
            FileAttributeNormal,
            IntPtr.Zero);

        if (!IsConnected)
        {
            AppLog.Write("ASUS ACPI device is not available.");
        }
    }

    public bool IsConnected => _handle != IntPtr.Zero && _handle != new IntPtr(-1);

    public bool CanSetBrightness => IsSupported(TufKeyboardBrightness);

    public bool CanSetStaticColor => IsSupported(TufKeyboard) || IsSupported(TufKeyboard2);

    public KeyboardBrightness? GetBrightness()
    {
        var status = DeviceGet(TufKeyboardBrightness);
        if (status < 0)
        {
            return null;
        }

        if (status == 0x8000)
        {
            return KeyboardBrightness.Off;
        }

        return (KeyboardBrightness)Math.Clamp(status & 0x7F, 0, 3);
    }

    public void SetBrightnessLevel(int level)
    {
        if (!IsConnected)
        {
            return;
        }

        var parameter = 0x80 | (level & 0x7F);
        DeviceSet(TufKeyboardBrightness, parameter, "ACPI keyboard brightness");
    }

    public void SetStaticColor(Color color)
    {
        if (!IsConnected)
        {
            return;
        }

        byte[] setting =
        [
            0xB4,
            (byte)AuraMode.Static,
            color.R,
            color.G,
            color.B,
            0xEB
        ];

        var result = DeviceSet(TufKeyboard, setting, "ACPI static keyboard color", logFailure: false);
        if (result != 1)
        {
            setting[0] = 0xB3;
            DeviceSet(TufKeyboard2, setting, "ACPI static keyboard color fallback prepare");
            setting[0] = 0xB4;
            DeviceSet(TufKeyboard2, setting, "ACPI static keyboard color fallback apply");
        }
    }

    public void Dispose()
    {
        if (IsConnected)
        {
            CloseHandle(_handle);
        }
    }

    private int DeviceSet(uint deviceId, int status, string logName)
    {
        byte[] args = new byte[8];
        BitConverter.GetBytes(deviceId).CopyTo(args, 0);
        BitConverter.GetBytes((uint)status).CopyTo(args, 4);

        var result = BitConverter.ToInt32(CallMethod(Devs, args), 0);
        if (result != 1)
        {
            AppLog.Write($"{logName} failed for {status}: {result}");
        }

        return result;
    }

    private bool IsSupported(uint deviceId)
    {
        if (!_supportCache.TryGetValue(deviceId, out var supported))
        {
            supported = DeviceGet(deviceId) >= 0;
            _supportCache[deviceId] = supported;
        }

        return supported;
    }

    private int DeviceGet(uint deviceId)
    {
        if (!IsConnected)
        {
            return -1;
        }

        byte[] args = new byte[8];
        BitConverter.GetBytes(deviceId).CopyTo(args, 0);
        byte[] status = CallMethod(Dsts, args);

        return BitConverter.ToInt32(status, 0) - 65536;
    }

    private int DeviceSet(uint deviceId, byte[] parameters, string logName, bool logFailure = true)
    {
        byte[] args = new byte[4 + parameters.Length];
        BitConverter.GetBytes(deviceId).CopyTo(args, 0);
        parameters.CopyTo(args, 4);

        var result = BitConverter.ToInt32(CallMethod(Devs, args), 0);
        if (logFailure && result != 1)
        {
            AppLog.Write($"{logName} failed for {BitConverter.ToString(parameters)}: {result}");
        }

        return result;
    }

    private byte[] CallMethod(uint methodId, byte[] args)
    {
        byte[] acpiBuffer = new byte[8 + args.Length];
        byte[] outBuffer = new byte[16];

        BitConverter.GetBytes(methodId).CopyTo(acpiBuffer, 0);
        BitConverter.GetBytes((uint)args.Length).CopyTo(acpiBuffer, 4);
        Array.Copy(args, 0, acpiBuffer, 8, args.Length);

        uint bytesReturned = 0;
        var success = DeviceIoControl(
            _handle,
            ControlCode,
            acpiBuffer,
            (uint)acpiBuffer.Length,
            outBuffer,
            (uint)outBuffer.Length,
            ref bytesReturned,
            IntPtr.Zero);

        if (!success)
        {
            AppLog.Write($"ACPI call failed with Win32 error {Marshal.GetLastWin32Error()}.");
        }

        return outBuffer;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        IntPtr hDevice,
        uint dwIoControlCode,
        byte[] lpInBuffer,
        uint nInBufferSize,
        byte[] lpOutBuffer,
        uint nOutBufferSize,
        ref uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
}
