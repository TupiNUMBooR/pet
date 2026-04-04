using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
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

    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    private const int ULW_ALPHA = 0x00000002;
    private const byte AC_SRC_OVER = 0x00;
    private const byte AC_SRC_ALPHA = 0x01;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= WS_EX_LAYERED | WS_EX_TOOLWINDOW;
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

        UpdateLayered();
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

        x += 1000;
        y += 1000;

        UpdateLayered();
    }

    private void UpdateLayered()
    {
        IntPtr screenDc = GetDC(IntPtr.Zero);
        IntPtr memDc = CreateCompatibleDC(screenDc);
        IntPtr hBitmap = IntPtr.Zero;
        IntPtr oldBitmap = IntPtr.Zero;

        try
        {
            hBitmap = sprite.GetHbitmap(Color.FromArgb(0));
            oldBitmap = SelectObject(memDc, hBitmap);

            SIZE size = new(sprite.Width, sprite.Height);
            POINT sourcePoint = new(0, 0);
            POINT topPos = new((int)Math.Round(x), (int)Math.Round(y));

            BLENDFUNCTION blend = new()
            {
                BlendOp = AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = WindowOpacity,
                AlphaFormat = AC_SRC_ALPHA
            };

            UpdateLayeredWindow(
                Handle,
                screenDc,
                ref topPos,
                ref size,
                memDc,
                ref sourcePoint,
                0,
                ref blend,
                ULW_ALPHA
            );
        }
        finally
        {
            if (oldBitmap != IntPtr.Zero)
            {
                SelectObject(memDc, oldBitmap);
            }

            if (hBitmap != IntPtr.Zero)
            {
                DeleteObject(hBitmap);
            }

            DeleteDC(memDc);
            ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UpdateLayeredWindow(
        IntPtr hwnd,
        IntPtr hdcDst,
        ref POINT pptDst,
        ref SIZE psize,
        IntPtr hdcSrc,
        ref POINT pprSrc,
        int crKey,
        ref BLENDFUNCTION pblend,
        int dwFlags
    );

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SIZE
    {
        public int CX;
        public int CY;

        public SIZE(int cx, int cy)
        {
            CX = cx;
            CY = cy;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct BLENDFUNCTION
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }
}
