/* Notify - A utility to display the wireless connection speed
    Copyright (C) 2017-2026  Steven Connelly
    GPL-3.0-or-later. See License.txt.
*/
using System.Text.Json;
using Microsoft.Win32;

namespace Notify;

/// <summary>User settings, stored as JSON in %APPDATA%\Notify\settings.json.</summary>
public sealed class AppSettings
{
    public const string AutoColour = "Auto";

    public string AdapterId { get; set; } = NetworkReader.Automatic;
    public int RefreshSeconds { get; set; } = 2;
    public string Colour { get; set; } = AutoColour;   // "Auto" = match the taskbar, or a named colour
    public bool ShowReceive { get; set; } = true;      // false = show transmit rate on the icon
    public bool FirstRunDone { get; set; }

    private static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Notify");
    private static readonly string FilePath = Path.Combine(Folder, "settings.json");
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
                s.RefreshSeconds = Math.Clamp(s.RefreshSeconds, 1, 3600);
                return s;
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Corrupt or unreadable settings: start from defaults rather than crash.
        }
        return new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
    }

    // ---- Start with Windows (per-user Run key, no admin rights needed) ----
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValue = "Notify";

    public static bool StartsWithWindows
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(RunValue) is string path &&
                   string.Equals(path.Trim('"'), Environment.ProcessPath, StringComparison.OrdinalIgnoreCase);
        }
        set
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (value && Environment.ProcessPath is { } exe) key.SetValue(RunValue, $"\"{exe}\"");
            else key.DeleteValue(RunValue, throwOnMissingValue: false);
        }
    }
}
