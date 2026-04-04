using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Pet;

public sealed class PetForm : Form
{
    private readonly Timer timer = new();
    private readonly Bitmap sprite;
    private readonly NotifyIcon trayIcon = new();
    private readonly ContextMenuStrip trayMenu = new();
    private readonly PetAnimator animator = new();

    private double x;
    private double y;

    private const int OffsetX = 48;
    private const int OffsetY = 96;
    private const double FollowSpeed = 0.05;
    private const byte WindowOpacity = 220;
    private const int TickMs = 8;
    private const float MaxScale = 1.4f;

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

        Width = (int)Math.Ceiling(sprite.Width * MaxScale);
        Height = (int)Math.Ceiling(sprite.Height * MaxScale);

        x = Cursor.Position.X + OffsetX - Width / 2.0;
        y = Cursor.Position.Y - OffsetY - Height / 2.0;

        trayIcon.Icon = new Icon(iconPath);
        trayIcon.Text = "Pet";
        trayIcon.Visible = true;

        trayMenu.Items.Add("Exit", null, (_, _) => Close());
        trayIcon.ContextMenuStrip = trayMenu;

        MouseDown += OnPetMouseDown;

        timer.Interval = TickMs;
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
        double targetX = mouse.X + OffsetX - Width / 2.0;
        double targetY = mouse.Y - OffsetY - Height / 2.0;

        x += (targetX - x) * FollowSpeed;
        y += (targetY - y) * FollowSpeed;

        animator.Update(Environment.TickCount64);
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

    private Bitmap RenderFrame()
    {
        Bitmap frame = new(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using Graphics g = Graphics.FromImage(frame);
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.Transparent);

        g.TranslateTransform(Width / 2f, Height / 2f);
        g.RotateTransform(animator.Angle);
        g.ScaleTransform(animator.Scale, animator.Scale);
        g.TranslateTransform(-sprite.Width / 2f, -sprite.Height / 2f);
        g.DrawImage(sprite, 0, 0, sprite.Width, sprite.Height);

        return frame;
    }

    private void UpdateLayered()
    {
        using Bitmap frame = RenderFrame();

        IntPtr screenDc = NativeMethods.GetDC(IntPtr.Zero);
        IntPtr memDc = NativeMethods.CreateCompatibleDC(screenDc);
        IntPtr hBitmap = IntPtr.Zero;
        IntPtr oldBitmap = IntPtr.Zero;

        try
        {
            hBitmap = frame.GetHbitmap(Color.FromArgb(0));
            oldBitmap = NativeMethods.SelectObject(memDc, hBitmap);

            NativeMethods.POINT position = new((int)Math.Round(x), (int)Math.Round(y));
            NativeMethods.POINT source = new(0, 0);
            NativeMethods.SIZE size = new(frame.Width, frame.Height);

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
