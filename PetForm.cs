using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Pet;

public sealed class PetForm : Form
{
    private readonly Timer timer = new();
    private readonly Image sprite;
    private readonly NotifyIcon trayIcon = new();

    private double x;
    private double y;

    private const int OffsetX = 48;
    private const int OffsetY = 96;
    private const double FollowSpeed = 0.05;

    private const int WS_EX_TOOLWINDOW = 0x00000080;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    public PetForm()
    {
        string assetDir = Path.Combine(AppContext.BaseDirectory, "assets");
        string spritePath = Path.Combine(assetDir, "pet.png");
        string iconPath = Path.Combine(assetDir, "icon.ico");

        sprite = Image.FromFile(spritePath);

        ClientSize = sprite.Size;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        DoubleBuffered = true;

        BackColor = Color.Black;
        TransparencyKey = Color.Black;

        x = Cursor.Position.X + OffsetX;
        y = Cursor.Position.Y - OffsetY;
        Location = new Point((int)x, (int)y);

        trayIcon.Icon = new Icon(iconPath);
        trayIcon.Text = "Pet";
        trayIcon.Visible = true;

        ContextMenuStrip trayMenu = new();
        trayMenu.Items.Add("Exit", null, (_, _) => Close());
        trayIcon.ContextMenuStrip = trayMenu;

        MouseDown += OnPetMouseDown;

        timer.Interval = 8;
        timer.Tick += OnTick;
        timer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.DrawImage(sprite, 0, 0, sprite.Width, sprite.Height);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        timer.Stop();
        timer.Dispose();

        trayIcon.Visible = false;
        trayIcon.Dispose();

        sprite.Dispose();

        base.OnFormClosed(e);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        Point mouse = Cursor.Position;

        double targetX = mouse.X + OffsetX;
        double targetY = mouse.Y - OffsetY;

        x += (targetX - x) * FollowSpeed;
        y += (targetY - y) * FollowSpeed;

        Location = new Point((int)Math.Round(x), (int)Math.Round(y));
    }

    private void OnPetMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        x += 1000;
        y += 1000;
        Location = new Point((int)Math.Round(x), (int)Math.Round(y));
    }
}
