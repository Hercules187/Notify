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
    // Packaged (MSIX / Store) builds can't use the Run key: Windows virtualises their registry writes.
    // They use the StartupTask declared in Package.appxmanifest instead (Settings > Apps > Startup).
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValue = "Notify";
    private const string StartupTaskId = "NotifyStartup";

    public static bool StartsWithWindows
    {
        get
        {
            if (Packaging.IsPackaged)
                return RunOffUiThread(async () =>
                {
                    var task = await Windows.ApplicationModel.StartupTask.GetAsync(StartupTaskId);
                    return task.State is Windows.ApplicationModel.StartupTaskState.Enabled
                        or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy;
                });
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(RunValue) is string path &&
                   string.Equals(path.Trim('"'), Environment.ProcessPath, StringComparison.OrdinalIgnoreCase);
        }
        set
        {
            if (Packaging.IsPackaged)
            {
                RunOffUiThread(async () =>
                {
                    var task = await Windows.ApplicationModel.StartupTask.GetAsync(StartupTaskId);
                    if (value) await task.RequestEnableAsync(); // no effect if the user disabled it in Settings > Apps > Startup
                    else task.Disable();
                    return true;
                });
                return;
            }
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (value && Environment.ProcessPath is { } exe) key.SetValue(RunValue, $"\"{exe}\"");
            else key.DeleteValue(RunValue, throwOnMissingValue: false);
        }
    }

    // WinRT async calls are awaited on the thread pool so the UI thread never blocks on itself.
    private static T RunOffUiThread<T>(Func<Task<T>> work) => Task.Run(work).GetAwaiter().GetResult();
}

/// <summary>Whether Notify is running from an MSIX package (Store install) or as a plain exe.</summary>
internal static class Packaging
{
    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref int length, System.Text.StringBuilder? name);

    private const int AppModelErrorNoPackage = 15700;

    public static readonly bool IsPackaged = Detect();

    private static bool Detect()
    {
        try
        {
            int length = 0;
            return GetCurrentPackageFullName(ref length, null) != AppModelErrorNoPackage;
        }
        catch (EntryPointNotFoundException) { return false; } // Windows 7
    }
}
