/* Notify - A utility to display the wireless connection speed
    Copyright (C) 2017-2026  Steven Connelly
    GPL-3.0-or-later. See License.txt.
*/
using Microsoft.Win32;
using Timer = System.Windows.Forms.Timer;

namespace Notify;

/// <summary>Owns the tray icon, its menu, the refresh timer and the flyout. No main window.</summary>
internal sealed class TrayContext : ApplicationContext
{
    private readonly AppSettings _settings = AppSettings.Load();
    private readonly NotifyIcon _tray = new() { Visible = true, Text = "Notify" };
    private readonly Timer _timer = new();
    private readonly ToolStripMenuItem _status = new() { Enabled = false };
    private readonly ToolStripMenuItem _startup = new("Start with Windows") { CheckOnClick = true };
    private SpeedReading _reading = SpeedReading.Disconnected("Starting…");
    private string _drawnKey = "";
    private FlyoutForm? _flyout;
    private SettingsForm? _settingsForm;

    public TrayContext()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(_status);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Settings…", null, (_, _) => OpenSettings());
        menu.Items.Add(_startup);
        menu.Items.Add("About Notify", null, (_, _) => ShowAbout());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
        menu.Opening += (_, _) => _startup.Checked = AppSettings.StartsWithWindows; // timer keeps running (old version froze here)
        _startup.CheckedChanged += (_, _) => { try { AppSettings.StartsWithWindows = _startup.Checked; } catch { /* policy may block HKCU Run */ } };
        _tray.ContextMenuStrip = menu;
        _tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ToggleFlyout(); };

        // Redraw when the taskbar theme or display scaling changes.
        SystemEvents.UserPreferenceChanged += OnSystemChanged;
        SystemEvents.DisplaySettingsChanged += OnSystemChanged;

        _timer.Tick += (_, _) => Refresh();
        ApplyTimer();
        Refresh();

        if (!_settings.FirstRunDone)
        {
            _tray.ShowBalloonTip(5000, "Notify is running",
                "Your Wi-Fi link speed is now in the system tray. Click it for details, right-click for settings.", ToolTipIcon.Info);
            _settings.FirstRunDone = true;
            try { _settings.Save(); } catch { }
        }
    }

    private void ApplyTimer()
    {
        _timer.Stop();
        _timer.Interval = Math.Clamp(_settings.RefreshSeconds, 1, 3600) * 1000;
        _timer.Start();
    }

    private void Refresh()
    {
        try { _reading = NetworkReader.Read(_settings.AdapterId); }
        catch (Exception ex) { _reading = SpeedReading.Disconnected("Error: " + ex.Message); }

        double shown = _settings.ShowReceive ? _reading.RxMbps : _reading.TxMbps;
        string text = _reading.Connected ? NetworkReader.ShortSpeed(shown) : "–";
        var colour = Theme.TrayText(_settings.Colour);
        string key = $"{text}|{colour.ToArgb()}|{_reading.Connected}|{IconKey()}";
        if (key != _drawnKey)
        {
            var old = _tray.Icon;
            _tray.Icon = IconRenderer.Render(text, colour, dimmed: !_reading.Connected);
            old?.Dispose();
            _drawnKey = key;
        }

        _tray.Text = Tooltip(_reading);
        _status.Text = _reading.Connected
            ? $"{_reading.Ssid ?? _reading.AdapterName}  ·  ↓ {NetworkReader.LongSpeed(_reading.RxMbps)}"
            : "Not connected";
        _flyout?.SetReading(_reading);
    }

    // Changes when DPI changes, so the icon is re-rendered at the new size.
    private static string IconKey() => SystemInformation.SmallIconSize.Width.ToString();

    private static string Tooltip(SpeedReading r)
    {
        string t = !r.Connected
            ? $"Notify: not connected ({r.AdapterName})"
            : $"{r.Ssid ?? r.AdapterName}\n↓ {NetworkReader.LongSpeed(r.RxMbps)}   ↑ {NetworkReader.LongSpeed(r.TxMbps)}"
              + (r.SignalQuality is int q ? $"\nSignal {q}%" : "");
        return t.Length > 127 ? t[..127] : t; // NotifyIcon.Text limit
    }

    private void ToggleFlyout()
    {
        if (_flyout is { IsDisposed: false })
        {
            _flyout.Close();
            return;
        }
        _flyout = new FlyoutForm(_reading, OpenSettings);
        _flyout.FormClosed += (_, _) => _flyout = null;
        _flyout.ShowNearTray();
    }

    private void OpenSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.Activate();
            return;
        }
        _settingsForm = new SettingsForm(_settings, () => _reading);
        _settingsForm.FormClosed += (_, _) =>
        {
            if (_settingsForm?.Saved == true)
            {
                _drawnKey = "";
                ApplyTimer();
                Refresh();
            }
            _settingsForm = null;
        };
        _settingsForm.Show();
        _settingsForm.Activate();
    }

    private static void ShowAbout() =>
        MessageBox.Show(
            "Notify 2.0\nShows your Wi-Fi link speed in the system tray.\n\n" +
            "Copyright (C) 2017-2026 Steven Connelly\nLicensed under the GNU GPL v3 or later.",
            "About Notify", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void OnSystemChanged(object? sender, EventArgs e)
    {
        _drawnKey = ""; // force a redraw in the new theme/size
        Refresh();
    }

    protected override void ExitThreadCore()
    {
        SystemEvents.UserPreferenceChanged -= OnSystemChanged;
        SystemEvents.DisplaySettingsChanged -= OnSystemChanged;
        _timer.Dispose();
        _flyout?.Close();
        _settingsForm?.Close();
        _tray.Visible = false;
        _tray.Icon?.Dispose();
        _tray.Dispose();
        base.ExitThreadCore();
    }
}
