/* Notify - A utility to display the wireless connection speed
    Copyright (C) 2017-2026  Steven Connelly
    GPL-3.0-or-later. See License.txt.
*/
using System.Reflection;

namespace Notify;

/// <summary>Settings window: adapter, refresh rate, which rate the icon shows, number colour, start with Windows.</summary>
internal sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly Func<SpeedReading> _currentReading;
    private readonly ComboBox _adapter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly NumericUpDown _refresh = new() { Minimum = 1, Maximum = 3600, TextAlign = HorizontalAlignment.Right };
    private readonly ComboBox _shows = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox _colour = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, DrawMode = DrawMode.OwnerDrawFixed };
    private readonly CheckBox _startup = new() { Text = "Start with Windows", AutoSize = true };
    private readonly PictureBox _preview = new() { SizeMode = PictureBoxSizeMode.CenterImage };

    public bool Saved { get; private set; }

    /// <summary>Logical (96-DPI) pixels to device pixels for this window's monitor.</summary>
    private int D(int logical) => LogicalToDeviceUnits(logical);

    public SettingsForm(AppSettings settings, Func<SpeedReading> currentReading)
    {
        _settings = settings;
        _currentReading = currentReading;
        _refresh.Width = D(84);
        _preview.Size = new Size(D(64), D(64));
        _adapter.DropDownWidth = D(520);
        _shows.DropDownWidth = D(330);

        Text = "Notify settings";
        Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? Application.ExecutablePath);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.None; // hand-set sizes are scaled with D() below
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(D(20));
        BackColor = Theme.Background;
        ForeColor = Theme.Text;

        var root = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, BackColor = Theme.Background, Location = new Point(Padding.Left, Padding.Top) };

        // Header with live icon preview
        var header = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, Margin = new Padding(0, 0, 0, D(14)) };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _preview.BackColor = Theme.TaskbarDark ? Color.FromArgb(28, 28, 28) : Color.FromArgb(238, 238, 238);
        header.Controls.Add(_preview, 0, 0);
        header.SetRowSpan(_preview, 2);
        header.Controls.Add(new Label
        {
            Text = "Notify", AutoSize = true, Font = new Font("Segoe UI Variable Display", 18f, FontStyle.Bold),
            ForeColor = Theme.Text, Margin = new Padding(D(14), D(2), 0, 0)
        }, 1, 0);
        header.Controls.Add(new Label
        {
            Text = "Wi-Fi link speed in your system tray", AutoSize = true, ForeColor = Theme.SubtleText,
            Margin = new Padding(D(16), 0, 0, 0)
        }, 1, 1);
        root.Controls.Add(header);

        root.Controls.Add(Card("Adapter", "Which network adapter to watch", _adapter));
        root.Controls.Add(Card("Refresh every", "Seconds between readings", WithSuffix(_refresh, "seconds")));
        root.Controls.Add(Card("Icon shows", "Which link rate appears on the icon", _shows));
        root.Controls.Add(Card("Number colour", "Automatic matches your light or dark taskbar", _colour));
        root.Controls.Add(Card("Startup", null, _startup));

        // Buttons
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Anchor = AnchorStyles.Right, Margin = new Padding(0, D(14), 0, 0) };
        var save = new Button { Text = "Save", Width = D(100), Height = D(34), FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Margin = new Padding(D(8), 0, 0, 0) };
        save.FlatAppearance.BorderSize = 0;
        var cancel = new Button { Text = "Cancel", Width = D(100), Height = D(34), FlatStyle = FlatStyle.Flat, BackColor = Theme.Surface, ForeColor = Theme.Text, DialogResult = DialogResult.Cancel, Margin = Padding.Empty };
        cancel.FlatAppearance.BorderColor = Theme.Border;
        save.Click += (_, _) => SaveAndClose();
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons);
        AcceptButton = save;
        CancelButton = cancel;
        Controls.Add(root);

        LoadValues();
        _colour.DrawItem += DrawColourItem;
        _colour.SelectedIndexChanged += (_, _) => RefreshPreview();
        _shows.SelectedIndexChanged += (_, _) => RefreshPreview();
        RefreshPreview();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Theme.ApplyWindowChrome(this, mica: false);
    }

    private Control Card(string title, string? hint, Control editor)
    {
        var card = new TableLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, BackColor = Theme.Surface,
            Padding = new Padding(D(16), D(12), D(16), D(12)), Margin = new Padding(0, 0, 0, D(4)), Anchor = AnchorStyles.Left | AnchorStyles.Right
        };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(230)));
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(330)));
        var labels = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = Padding.Empty, BackColor = Theme.Surface };
        labels.Controls.Add(new Label { Text = title, AutoSize = true, ForeColor = Theme.Text, Font = new Font(Font, FontStyle.Bold), Margin = Padding.Empty });
        if (hint is not null)
            labels.Controls.Add(new Label { Text = hint, AutoSize = true, MaximumSize = new Size(D(215), 0), ForeColor = Theme.SubtleText, Margin = new Padding(0, D(2), 0, 0) });
        card.Controls.Add(labels, 0, 0);
        editor.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        editor.Margin = new Padding(0, D(4), 0, D(4));
        card.Controls.Add(editor, 1, 0);
        card.Paint += (_, e) => { using var p = new Pen(Theme.Border); e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1); };
        return card;
    }

    private Control WithSuffix(Control c, string suffix)
    {
        var f = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty, BackColor = Theme.Surface };
        c.Margin = new Padding(0);
        f.Controls.Add(c);
        f.Controls.Add(new Label { Text = suffix, AutoSize = true, ForeColor = Theme.SubtleText, Margin = new Padding(D(8), D(5), 0, 0) });
        return f;
    }

    private void LoadValues()
    {
        var choices = NetworkReader.GetChoices();
        _adapter.Items.AddRange(choices.Cast<object>().ToArray());
        _adapter.SelectedItem = choices.FirstOrDefault(c => c.Id.Equals(_settings.AdapterId, StringComparison.OrdinalIgnoreCase)) ?? choices[0];

        _refresh.Value = Math.Clamp(_settings.RefreshSeconds, 1, 3600);

        _shows.Items.AddRange(new object[] { "Receive speed (download link rate)", "Transmit speed (upload link rate)" });
        _shows.SelectedIndex = _settings.ShowReceive ? 0 : 1;

        // Named colours (as the original did), minus Transparent which can't show on an icon.
        var names = typeof(Color).GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(p => p.PropertyType == typeof(Color) && p.Name != nameof(Color.Transparent))
            .Select(p => p.Name).OrderBy(n => n).ToList();
        _colour.Items.Add(AppSettings.AutoColour);
        _colour.Items.AddRange(names.Cast<object>().ToArray());
        _colour.SelectedItem = _colour.Items.Contains(_settings.Colour) ? _settings.Colour : AppSettings.AutoColour;

        _startup.Checked = AppSettings.StartsWithWindows;
    }

    private void DrawColourItem(object? sender, DrawItemEventArgs e)
    {
        e.DrawBackground();
        if (e.Index < 0) return;
        string name = (string)_colour.Items[e.Index]!;
        var swatch = new Rectangle(e.Bounds.X + 4, e.Bounds.Y + 3, e.Bounds.Height - 6, e.Bounds.Height - 6);
        using (var b = new SolidBrush(Theme.TrayText(name))) e.Graphics.FillRectangle(b, swatch);
        using (var p = new Pen(Color.Gray)) e.Graphics.DrawRectangle(p, swatch);
        string label = name == AppSettings.AutoColour ? "Automatic (match taskbar)" : name;
        TextRenderer.DrawText(e.Graphics, label, e.Font ?? Font, new Point(swatch.Right + 8, e.Bounds.Y + 1), e.ForeColor);
        e.DrawFocusRectangle();
    }

    private void RefreshPreview()
    {
        var r = _currentReading();
        double mbps = _shows.SelectedIndex == 1 ? r.TxMbps : r.RxMbps;
        string colour = _colour.SelectedItem as string ?? AppSettings.AutoColour;
        var old = _preview.Image;
        using var icon = IconRenderer.Render(r.Connected ? NetworkReader.ShortSpeed(mbps) : "–", Theme.TrayText(colour), !r.Connected);
        _preview.Image = icon.ToBitmap();
        old?.Dispose();
    }

    private void SaveAndClose()
    {
        _settings.AdapterId = (_adapter.SelectedItem as AdapterChoice)?.Id ?? NetworkReader.Automatic;
        _settings.RefreshSeconds = (int)_refresh.Value;
        _settings.ShowReceive = _shows.SelectedIndex != 1;
        _settings.Colour = _colour.SelectedItem as string ?? AppSettings.AutoColour;
        _settings.FirstRunDone = true;
        try
        {
            _settings.Save();
            AppSettings.StartsWithWindows = _startup.Checked;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            MessageBox.Show(this, $"Couldn't save settings: {ex.Message}", "Notify", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        Saved = true;
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _preview.Image?.Dispose();
        base.Dispose(disposing);
    }
}
