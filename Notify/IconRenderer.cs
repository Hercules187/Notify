/* Notify - A utility to display the wireless connection speed
    Copyright (C) 2017-2026  Steven Connelly
    GPL-3.0-or-later. See License.txt.
*/
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace Notify;

/// <summary>Draws the speed as a crisp tray icon sized for the current display scaling.</summary>
internal static class IconRenderer
{
    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetricsForDpi(int index, uint dpi);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();

    private const int SM_CXSMICON = 49;

    /// <summary>Tray icon size in pixels at the current system DPI (16 at 100%, 24 at 150%, 32 at 200%).</summary>
    private static int IconSize()
    {
        try { return Math.Max(16, GetSystemMetricsForDpi(SM_CXSMICON, GetDpiForSystem())); }
        catch (EntryPointNotFoundException) { return SystemInformation.SmallIconSize.Width; }
    }

    /// <summary>Renders the text as a new Icon. The caller owns it and disposes the one it replaces.</summary>
    public static Icon Render(string text, Color colour, bool dimmed)
    {
        int size = IconSize();
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            // Largest bold font that fits the icon, so short numbers are big and readable.
            float fontSize = size;
            SizeF measured;
            using var format = new StringFormat(StringFormat.GenericTypographic)
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap
            };
            Font font;
            do
            {
                font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                measured = g.MeasureString(text, font, int.MaxValue, format);
                if (measured.Width <= size + 1 && measured.Height <= size * 1.25f) break;
                font.Dispose();
                fontSize -= 0.5f;
            } while (fontSize > 6);

            using (font)
            using (var brush = new SolidBrush(dimmed ? Color.FromArgb(150, colour) : colour))
            {
                var rect = new RectangleF(0, 0, size, size);
                g.DrawString(text, font, brush, rect, format);
            }
        }

        IntPtr hIcon = bmp.GetHicon();
        // Clone so the Icon owns its own copy, then free the native handle (the old code leaked one per tick).
        using var temp = Icon.FromHandle(hIcon);
        var icon = (Icon)temp.Clone();
        DestroyIcon(hIcon);
        return icon;
    }
}
