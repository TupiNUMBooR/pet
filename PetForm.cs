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

    private PetMenuForm? menuForm;
    private bool isPaused;
    private string menuText = string.Empty;

    private double x;
    private double y;
    private double vx;
    private double vy;

    private float moveAngle;
    private float moveScale = 1f;

    private const int OffsetX = 120;
    private const int OffsetY = -70;
    private const byte WindowOpacity = 220;
    private const int TickMs = 8;
    private const float MaxScale = 1.4f;

    private const double Acceleration = 0.005;
    private const double Damping = 0.90;
    private const double MaxSpeed = 18.0;
    private const double StopDistance = 0.5;

    private const float MaxTiltAngle = 10f;
    private const float TiltResponse = 0.18f;
    private const float IdleScaleResponse = 0.12f;
    private const float MaxMoveScaleBoost = 0.06f;

    private const int MenuOffsetX = 24;
    private const int MenuOffsetY = 12;

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

        x = Cursor.Position.X - Width / 2.0 + OffsetX;
        y = Cursor.Position.Y - Height / 2.0 + OffsetY;

        trayIcon.Icon = new Icon(iconPath);
        trayIcon.Text = "Clip";
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

        if (menuForm is not null)
        {
            menuForm.FormClosed -= OnMenuClosed;

            if (!menuForm.IsDisposed)
            {
                menuForm.Close();
                menuForm.Dispose();
            }

            menuForm = null;
        }

        trayIcon.Visible = false;
        trayIcon.Dispose();
        trayMenu.Dispose();

        sprite.Dispose();

        base.OnFormClosed(e);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        animator.Update(Environment.TickCount64);

        if (!isPaused)
        {
            UpdateMovement();
        }
        else
        {
            UpdatePausedMotionEffects();
        }

        UpdateMenuPosition();
        UpdateLayered();
    }

    private void UpdateMovement()
    {
        Point mouse = Cursor.Position;

        double targetX = mouse.X - Width / 2.0 + OffsetX;
        double targetY = mouse.Y - Height / 2.0 + OffsetY;

        double dx = targetX - x;
        double dy = targetY - y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance > StopDistance)
        {
            vx += dx * Acceleration;
            vy += dy * Acceleration;
        }

        vx *= Damping;
        vy *= Damping;

        double speed = Math.Sqrt(vx * vx + vy * vy);

        if (speed > MaxSpeed)
        {
            double k = MaxSpeed / speed;
            vx *= k;
            vy *= k;
            speed = MaxSpeed;
        }

        if (distance <= StopDistance && speed < 0.1)
        {
            x = targetX;
            y = targetY;
            vx = 0.0;
            vy = 0.0;
        }
        else
        {
            x += vx;
            y += vy;
        }

        UpdateMotionEffects(speed);
    }

    private void UpdatePausedMotionEffects()
    {
        vx = 0.0;
        vy = 0.0;

        moveAngle += (0f - moveAngle) * TiltResponse;
        moveScale += (1f - moveScale) * IdleScaleResponse;
    }

    private void OnPetMouseDown(object? sender, MouseEventArgs e)
    {
        switch (e.Button)
        {
            case MouseButtons.Left:
                if (!isPaused)
                {
                    vx += 8.0;
                    vy -= 6.0;
                    UpdateLayered();
                }
                break;

            case MouseButtons.Right:
                ToggleMenu();
                break;

            case MouseButtons.Middle:
                Close();
                break;
        }
    }

    private void ToggleMenu()
    {
        if (menuForm is not null && !menuForm.IsDisposed)
        {
            menuText = menuForm.EditorText;
            menuForm.Close();
            return;
        }

        menuForm = new PetMenuForm
        {
            EditorText = menuText
        };

        menuForm.FormClosed += OnMenuClosed;

        isPaused = true;
        UpdatePausedMotionEffects();
        UpdateMenuPosition();

        menuForm.Show();
        menuForm.BringToFront();
    }

    private void OnMenuClosed(object? sender, FormClosedEventArgs e)
    {
        if (sender is PetMenuForm closedMenu)
        {
            menuText = closedMenu.EditorText;
            closedMenu.FormClosed -= OnMenuClosed;
        }

        if (ReferenceEquals(menuForm, sender))
        {
            menuForm = null;
        }

        isPaused = false;
    }

    private void UpdateMenuPosition()
    {
        if (menuForm is null || menuForm.IsDisposed)
        {
            return;
        }

        int menuX = (int)Math.Round(x) + Width + MenuOffsetX;
        int menuY = (int)Math.Round(y) + MenuOffsetY;

        Rectangle area = Screen.FromPoint(new Point((int)Math.Round(x), (int)Math.Round(y))).WorkingArea;

        if (menuX + menuForm.Width > area.Right)
        {
            menuX = (int)Math.Round(x) - menuForm.Width - MenuOffsetX;
        }

        if (menuY + menuForm.Height > area.Bottom)
        {
            menuY = area.Bottom - menuForm.Height;
        }

        if (menuY < area.Top)
        {
            menuY = area.Top;
        }

        menuForm.Location = new Point(menuX, menuY);
    }

    private void UpdateMotionEffects(double speed)
    {
        float normalizedSpeed = (float)Math.Min(speed / MaxSpeed, 1.0);

        float targetAngle = (float)(vx / MaxSpeed) * MaxTiltAngle;
        float targetScale = 1f + normalizedSpeed * MaxMoveScaleBoost;

        moveAngle += (targetAngle - moveAngle) * TiltResponse;
        moveScale += (targetScale - moveScale) * IdleScaleResponse;
    }

    private Bitmap RenderFrame()
    {
        Bitmap frame = new(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using Graphics g = Graphics.FromImage(frame);
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.Transparent);

        float angle = animator.Angle + moveAngle;
        float scale = animator.Scale * moveScale;

        g.TranslateTransform(Width / 2f, Height / 2f);
        g.RotateTransform(angle);
        g.ScaleTransform(scale, scale);
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
