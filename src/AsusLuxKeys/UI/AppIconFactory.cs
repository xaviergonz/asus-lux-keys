using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace AsusLuxKeys.UI;

public static class AppIconFactory
{
    public static Icon? CreateIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app-icon.png");
        return File.Exists(iconPath) ? CreateIconFromImage(iconPath) : null;
    }

    private static Icon? CreateIconFromImage(string path)
    {
        try
        {
            using var source = Image.FromFile(path);
            using var bitmap = new Bitmap(32, 32);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.Clear(Color.Transparent);
            graphics.DrawImage(source, new Rectangle(0, 0, 32, 32));

            return CreateIconFromBitmap(bitmap);
        }
        catch
        {
            return null;
        }
    }

    private static Icon CreateIconFromBitmap(Bitmap bitmap)
    {
        var handle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
