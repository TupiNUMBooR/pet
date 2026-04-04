using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Pet;

public sealed class PetForm : Form
{
    private readonly Timer timer = new();
    private readonly Bitmap sprite;
    private readonly NotifyIcon trayIcon = new();
    private readonly ContextMenuStrip trayMenu = new();

    private double x;
    private double y;

    private const int OffsetX = 48;
    private const int OffsetY = 96;
    private const double FollowSpeed = 0.05;
    private const byte WindowOpacity = 220;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    public PetForm()
    {
        string assetDir = Path.Combine(AppContext.BaseDirectory, "assets");
        string spritePath = Path.Combine(assetDir, "pet.png");
        string iconPath = Path.Combine(assetDir, "icon.ico");

        sprite = new Bitmap(spritePath);

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        Width = sprite.Width;
        Height = sprite.Height;

        x = Cursor.Position.X + OffsetX;
        y = Cursor.Position.Y - OffsetY;

        trayIcon.Icon = new Icon(iconPath);
        trayIcon.Text = "Pet";
        trayIcon.Visible = true;

        trayMenu.Items.Add("Exit", null, (_, _) => Close());
        trayIcon.ContextMenuStrip = trayMenu;

        MouseDown += OnPetMouseDown;

        timer.Interval = 8;
        timer.Tick += OnTick;
        timer.Start();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        UpdateLayered();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        timer.Stop();
        timer.Dispose();

        trayIcon.Visible = false;
        trayIcon.Dispose();
        trayMenu.Dispose();

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

        UpdateLayered();
    }

    private void OnPetMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        x += 100;
        UpdateLayered();
    }

    private void UpdateLayered()
    {
        IntPtr screenDc = NativeMethods.GetDC(IntPtr.Zero);
        IntPtr memDc = NativeMethods.CreateCompatibleDC(screenDc);
        IntPtr hBitmap = IntPtr.Zero;
        IntPtr oldBitmap = IntPtr.Zero;

        try
        {
            hBitmap = sprite.GetHbitmap(Color.FromArgb(0));
            oldBitmap = NativeMethods.SelectObject(memDc, hBitmap);

            NativeMethods.POINT position = new((int)Math.Round(x), (int)Math.Round(y));
            NativeMethods.POINT source = new(0, 0);
            NativeMethods.SIZE size = new(sprite.Width, sprite.Height);

            NativeMethods.BLENDFUNCTION blend = new()
            {
                BlendOp = NativeMethods.AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = WindowOpacity,
                AlphaFormat = NativeMethods.AC_SRC_ALPHA
            };

            NativeMethods.UpdateLayeredWindow(
                Handle,
                screenDc,
                ref position,
                ref size,
                memDc,
                ref source,
                0,
                ref blend,
                NativeMethods.ULW_ALPHA
            );
        }
        finally
        {
            if (oldBitmap != IntPtr.Zero)
            {
                NativeMethods.SelectObject(memDc, oldBitmap);
            }

            if (hBitmap != IntPtr.Zero)
            {
                NativeMethods.DeleteObject(hBitmap);
            }

            NativeMethods.DeleteDC(memDc);
            NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
        }
    }
}
