/* Notify - A utility to display the wireless connection speed
    Copyright (C) 2017-2026  Steven Connelly
    GPL-3.0-or-later. See License.txt.
*/
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Notify;

/// <summary>Windows 11 look: light/dark detection, accent colour, Mica backdrop and rounded corners.</summary>
internal static class Theme
{
    private const string Personalize = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    /// <summary>True if apps use the dark theme.</summary>
    public static bool AppsDark => ReadDword(Personalize, "AppsUseLightTheme", 1) == 0;

    /// <summary>True if the taskbar (where the icon lives) is dark.</summary>
    public static bool TaskbarDark => ReadDword(Personalize, "SystemUsesLightTheme", 0) == 0;

    public static Color Accent
    {
        get
        {
            // AccentColor is stored as ABGR.
            int abgr = ReadDword(@"Software\Microsoft\Windows\DWM", "AccentColor", unchecked((int)0xFFD47800));
            return Color.FromArgb(255, abgr & 0xFF, (abgr >> 8) & 0xFF, (abgr >> 16) & 0xFF);
        }
    }

    public static Color Background => AppsDark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(243, 243, 243);
    public static Color Surface => AppsDark ? Color.FromArgb(43, 43, 43) : Color.White;
    public static Color Border => AppsDark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(229, 229, 229);
    public static Color Text => AppsDark ? Color.White : Color.FromArgb(26, 26, 26);
    public static Color SubtleText => AppsDark ? Color.FromArgb(200, 200, 200) : Color.FromArgb(96, 96, 96);

    /// <summary>Colour for the number in the tray: auto-contrast against the taskbar unless the user picked one.</summary>
    public static Color TrayText(string setting) =>
        setting == AppSettings.AutoColour ? (TaskbarDark ? Color.White : Color.Black) : Color.FromName(setting);

    // ---- DWM (Windows 11): dark title bar, Mica, rounded corners. Harmless no-ops on older Windows. ----
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    public static void ApplyWindowChrome(Form form, bool mica, bool rounded = true)
    {
        try
        {
            int dark = AppsDark ? 1 : 0;
            DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
            if (rounded)
            {
                int round = 2; // DWMWCP_ROUND
                DwmSetWindowAttribute(form.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));
            }
            if (mica)
            {
                int backdrop = 2; // DWMSBT_MAINWINDOW (Mica)
                DwmSetWindowAttribute(form.Handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
            }
        }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }
    }

    private static int ReadDword(string path, string name, int fallback)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(path);
            return key?.GetValue(name) is int v ? v : fallback;
        }
        catch { return fallback; }
    }
}
