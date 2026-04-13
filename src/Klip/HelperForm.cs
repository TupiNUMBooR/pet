using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Klip;

public sealed class HelperForm : Form
{
    private readonly Timer closeTimer = new();
    private readonly TableLayoutPanel table = new();

    private const int AutoCloseMs = 12000;
    private const int HorizontalPadding = 16;
    private const int VerticalPadding = 12;
    private const int MinWindowWidth = 360;
    private const int MaxDescriptionWidth = 260;
    private const int CornerRadius = 14;
    private const double WindowOpacityValue = 0.94f;

    public HelperForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        DoubleBuffered = true;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Color.FromArgb(28, 28, 32);
        ForeColor = Color.FromArgb(235, 235, 235);
        Opacity = WindowOpacityValue;
        Padding = new Padding(HorizontalPadding, VerticalPadding, HorizontalPadding, VerticalPadding);

        BuildTable();
        Controls.Add(table);

        closeTimer.Interval = AutoCloseMs;
        closeTimer.Tick += OnCloseTimerTick;

        MouseDown += CloseOnInput;
        table.MouseDown += CloseOnInput;
        Shown += (_, _) =>
        {
            closeTimer.Start();
            UpdateRoundedRegion();
        };

        SizeChanged += (_, _) => UpdateRoundedRegion();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            closeTimer.Dispose();
            table.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BuildTable()
    {
        table.AutoSize = true;
        table.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        table.BackColor = Color.Transparent;
        table.ColumnCount = 2;
        table.RowCount = 4;
        table.Margin = Padding.Empty;

        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        AddTitleRow("Tiny pet manual");
        AddRow("🖱️ left click", "enjoys petting (maybe)");
        AddRow("🖱️ right click", "open memory cave");
        AddRow("🖱️ middle click", "send to shadow realm");
    }

    private void AddTitleRow(string text)
    {
        Label title = new()
        {
            AutoSize = true,
            Text = text,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ForeColor,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 8)
        };

        title.MouseDown += CloseOnInput;

        table.Controls.Add(title, 0, 0);
        table.SetColumnSpan(title, 2);
    }

    private void AddRow(string action, string description)
    {
        int row = table.RowCount - 1;

        Label actionLabel = new()
        {
            AutoSize = true,
            Text = action,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = ForeColor,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 12, 4)
        };

        Label descriptionLabel = new()
        {
            AutoSize = true,
            MaximumSize = new Size(MaxDescriptionWidth, 0),
            Text = description,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = ForeColor,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 4)
        };

        actionLabel.MouseDown += CloseOnInput;
        descriptionLabel.MouseDown += CloseOnInput;

        table.Controls.Add(actionLabel, 0, row);
        table.Controls.Add(descriptionLabel, 1, row);

        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowCount++;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (Width < MinWindowWidth)
        {
            Width = MinWindowWidth;
        }
    }

    private void OnCloseTimerTick(object? sender, EventArgs e)
    {
        closeTimer.Stop();
        Close();
    }

    private void CloseOnInput(object? sender, MouseEventArgs e)
    {
        Close();
    }

    private void UpdateRoundedRegion()
    {
        Region?.Dispose();
        Region = CreateRoundRegion(ClientRectangle, CornerRadius);
    }

    private static Region CreateRoundRegion(Rectangle bounds, int radius)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return new Region(Rectangle.Empty);
        }

        GraphicsPath path = new();

        path.AddArc(bounds.Left, bounds.Top, radius, radius, 180, 90);
        path.AddArc(bounds.Right - radius, bounds.Top, radius, radius, 270, 90);
        path.AddArc(bounds.Right - radius, bounds.Bottom - radius, radius, radius, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - radius, radius, radius, 90, 90);
        path.CloseFigure();

        return new Region(path);
    }
}
