using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace KeyboardAmbientLight.UI;

public static class AppIconFactory
{
    public static Icon CreateIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var keyboardBrush = new SolidBrush(Color.FromArgb(38, 38, 38));
        using var keyBrush = new SolidBrush(Color.FromArgb(248, 248, 248));
        using var glowBrush = new SolidBrush(Color.FromArgb(0, 120, 215));
        using var borderPen = new Pen(Color.FromArgb(20, 20, 20), 1.5f);

        graphics.FillEllipse(glowBrush, 3, 4, 26, 26);
        graphics.FillRoundedRectangle(keyboardBrush, new Rectangle(5, 9, 22, 15), new Size(4, 4));
        graphics.DrawRoundedRectangle(borderPen, new Rectangle(5, 9, 22, 15), new Size(4, 4));

        for (var row = 0; row < 2; row++)
        {
            for (var column = 0; column < 4; column++)
            {
                graphics.FillRoundedRectangle(
                    keyBrush,
                    new Rectangle(8 + column * 5, 12 + row * 5, 3, 3),
                    new Size(1, 1));
            }
        }

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
