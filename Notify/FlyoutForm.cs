/* Notify - A utility to display the wireless connection speed
    Copyright (C) 2017-2026  Steven Connelly
    GPL-3.0-or-later. See License.txt.
*/
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Notify;

/// <summary>
/// Windows 11 style flyout shown above the tray on left-click: network name, receive/transmit
/// link speed, signal strength and Wi-Fi generation. Closes when it loses focus.
/// </summary>
internal sealed class FlyoutForm : Form
{
    private SpeedReading _reading;
    private readonly Action _openSettings;
    private Rectangle _settingsLink, _locationLink;
    private bool _hoverLink, _hoverLocation;

    public FlyoutForm(SpeedReading reading, Action openSettings)
    {
        _reading = reading;
        _openSettings = openSettings;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        DoubleBuffered = true;
        AutoScaleMode = AutoScaleMode.None; // sizes are scaled by hand with S()
        BackColor = Theme.Surface;
        Size = Scale(new Size(320, 196));
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        Deactivate += (_, _) => Close();
        MouseMove += (_, e) =>
        {
            bool h = _settingsLink.Contains(e.Location), l = _locationLink.Contains(e.Location);
            if (h != _hoverLink || l != _hoverLocation)
            {
                _hoverLink = h; _hoverLocation = l;
                Cursor = h || l ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        };
        MouseClick += (_, e) =>
        {
            if (_settingsLink.Contains(e.Location)) { Close(); _openSettings(); }
            else if (_locationLink.Contains(e.Location))
            {
                Close();
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:privacy-location") { UseShellExecute = true }); }
                catch { /* Settings app unavailable */ }
            }
        };
    }

    public void SetReading(SpeedReading reading)
    {
        _reading = reading;
        Invalidate();
    }

    private float Dpi => DeviceDpi / 96f;
    private int S(float v) => (int)Math.Round(v * Dpi);
    private Size Scale(Size s) => new(S(s.Width), S(s.Height));

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Theme.ApplyWindowChrome(this, mica: false);
    }

    /// <summary>Place the flyout just above the notification area, like Windows' own flyouts.</summary>
    public void ShowNearTray()
    {
        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        int margin = S(12);
        Location = new Point(area.Right - Width - margin, area.Bottom - Height - margin);
        Show();
        Activate();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
            return cp;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(Theme.Surface);

        var accent = Theme.Accent;
        var text = Theme.Text;
        var subtle = Theme.SubtleText;
        int pad = S(20), gap = S(10);
        int inner = Width - pad * 2;
        const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;

        using var title = new Font("Segoe UI Semibold", 16f * Dpi, GraphicsUnit.Pixel);
        using var big = new Font("Segoe UI Semibold", 30f * Dpi, GraphicsUnit.Pixel);
        using var small = new Font("Segoe UI", 12f * Dpi, GraphicsUnit.Pixel);
        int y = pad;

        // Row 1: network name + signal bars
        string heading = !_reading.Connected ? "Not connected"
            : _reading.Ssid is { Length: > 0 } ssid ? ssid
            : _reading.LocationBlocked ? "Wi-Fi connected"
            : _reading.AdapterName;
        int titleH = TextRenderer.MeasureText(g, "Ag", title, Size.Empty, TextFormatFlags.NoPadding).Height;
        var bars = new Rectangle(Width - pad - S(26), y + (titleH - S(16)) / 2, S(26), S(16));
        TextRenderer.DrawText(g, heading, title, new Rectangle(pad, y, inner - bars.Width - gap, titleH), text, flags);
        DrawSignal(g, bars, _reading.SignalQuality, accent, Theme.Border);
        y += titleH + S(2);

        // Row 2: standard and signal, or a link explaining why they are hidden
        int smallH = TextRenderer.MeasureText(g, "Ag", small, Size.Empty, TextFormatFlags.NoPadding).Height;
        if (_reading.Connected && _reading.LocationBlocked)
        {
            // Windows withholds the network name and signal from desktop apps unless location access is on.
            const string hint = "Allow location access to show name and signal ›";
            int w = Math.Min(inner, TextRenderer.MeasureText(g, hint, small, Size.Empty, TextFormatFlags.NoPadding).Width);
            _locationLink = new Rectangle(pad, y, w, smallH);
            using var hf = new Font(small, _hoverLocation ? FontStyle.Underline : FontStyle.Regular);
            TextRenderer.DrawText(g, hint, hf, _locationLink, accent, flags);
        }
        else
        {
            _locationLink = Rectangle.Empty;
            string sub = _reading.Connected
                ? string.Join("  ·  ", new[] { _reading.PhyName, _reading.SignalQuality is int q ? $"Signal {q}%" : null }.Where(x => x is not null))
                : _reading.AdapterName;
            TextRenderer.DrawText(g, sub, small, new Rectangle(pad, y, inner, smallH), subtle, flags);
        }
        y += smallH + S(14);

        // Row 3: receive and transmit tiles
        int bigH = TextRenderer.MeasureText(g, "0", big, Size.Empty, TextFormatFlags.NoPadding).Height;
        int tileH = S(12) + smallH + S(4) + bigH + S(12);
        int tileW = (inner - gap) / 2;
        DrawSpeedTile(g, new Rectangle(pad, y, tileW, tileH), "↓  Receive", _reading.RxMbps, big, small, accent, subtle, smallH, bigH);
        DrawSpeedTile(g, new Rectangle(pad + tileW + gap, y, tileW, tileH), "↑  Transmit", _reading.TxMbps, big, small, text, subtle, smallH, bigH);
        y += tileH + S(14);

        // Row 4: adapter name + Settings link
        var linkSize = TextRenderer.MeasureText(g, "Settings", small, Size.Empty, TextFormatFlags.NoPadding);
        _settingsLink = new Rectangle(Width - pad - linkSize.Width, y, linkSize.Width, smallH);
        TextRenderer.DrawText(g, _reading.AdapterName, small, new Rectangle(pad, y, inner - linkSize.Width - gap, smallH), subtle, flags);
        using (var lf = new Font(small, _hoverLink ? FontStyle.Underline : FontStyle.Regular))
            TextRenderer.DrawText(g, "Settings", lf, _settingsLink, accent, flags);
        y += smallH + pad;

        using (var pen = new Pen(Theme.Border)) g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

        // Size the window to its content, keeping the bottom edge where it was (just above the tray).
        if (y != Height)
        {
            int bottom = Bottom;
            Height = y;
            if (Visible) Top = bottom - Height;
        }
    }

    private void DrawSpeedTile(Graphics g, Rectangle r, string label, double mbps, Font big, Font small,
        Color valueColour, Color subtle, int smallH, int bigH)
    {
        using var path = Rounded(r, S(8));
        using var fill = new SolidBrush(Theme.AppsDark ? Color.FromArgb(50, 50, 50) : Color.FromArgb(247, 247, 247));
        using var border = new Pen(Theme.Border);
        g.FillPath(fill, path);
        g.DrawPath(border, path);

        int x = r.X + S(12), y = r.Y + S(12);
        TextRenderer.DrawText(g, label, small, new Point(x, y), subtle, TextFormatFlags.NoPadding);
        y += smallH + S(4);

        bool hasValue = _reading.Connected && mbps > 0;
        string value = !hasValue ? "–" : mbps >= 1000 ? $"{mbps / 1000:0.0#}" : $"{Math.Round(mbps):0}";
        var vs = TextRenderer.MeasureText(g, value, big, Size.Empty, TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g, value, big, new Point(x, y), valueColour, TextFormatFlags.NoPadding);
        if (hasValue)
        {
            // Unit sits on the same baseline as the number.
            string unit = mbps >= 1000 ? "Gbps" : "Mbps";
            TextRenderer.DrawText(g, unit, small, new Point(x + vs.Width + S(4), y + bigH - smallH - S(4)), subtle, TextFormatFlags.NoPadding);
        }
    }

    private void DrawSignal(Graphics g, Rectangle r, int? quality, Color on, Color off)
    {
        int bars = quality is int q ? (q >= 80 ? 4 : q >= 55 ? 3 : q >= 30 ? 2 : q > 0 ? 1 : 0) : 0;
        float w = (r.Width - S(3) * 3) / 4f;
        for (int i = 0; i < 4; i++)
        {
            float h = r.Height * (i + 1) / 4f;
            var bar = new RectangleF(r.X + i * (w + S(3)), r.Bottom - h, w, h);
            using var b = new SolidBrush(i < bars ? on : off);
            using var path = Rounded(Rectangle.Round(bar), S(2));
            g.FillPath(b, path);
        }
    }

    private static GraphicsPath Rounded(Rectangle r, int radius)
    {
        int d = Math.Max(1, radius * 2);
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}
