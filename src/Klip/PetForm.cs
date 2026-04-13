using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Media;
using System.Reflection;
using System.Windows.Forms;

namespace Klip;

public sealed class PetForm : Form
{
    private readonly Timer timer = new();
    private readonly Bitmap sprite;
    private readonly NotifyIcon trayIcon = new();
    private readonly ContextMenuStrip trayMenu = new();
    private readonly PetAnimator animator = new();

    private readonly MemoryStream[] calmMeowStreams;
    private readonly MemoryStream[] panicMeowStreams;
    private readonly SoundPlayer[] calmMeowPlayers;
    private readonly SoundPlayer[] panicMeowPlayers;

    private PetMenuForm? menuForm;
    private bool isPaused;
    private bool isExiting;

    private double x;
    private double y;
    private double vx;
    private double vy;

    private const string AppName = "Klip";
    private const string SpriteResourceName = "Klip.assets.pet.png";
    private const string IconResourceName = "Klip.assets.icon.ico";

    private static readonly string[] CalmMeowResourceName =
    [
        "Klip.assets.ethereal_meow_1.wav",
        "Klip.assets.ethereal_meow_4.wav",
        "Klip.assets.ethereal_meow_6.wav"
    ];

    private static readonly string[] PanicMeowResourceName =
    [
        "Klip.assets.ethereal_meow_5.wav",
        "Klip.assets.ethereal_meow_7.wav"
    ];

    private const int OffsetX = 170;
    private const int OffsetY = -90;
    private const byte WindowOpacity = 220;
    private const int TickMs = 8;
    private const float MaxScale = 1.4f;

    private const double Acceleration = 0.005;
    private const double Damping = 0.90;
    private const double MaxSpeed = 18.0;
    private const double KickSpeedX = 16.0;
    private const double KickSpeedY = 12.0;
    private const double StopDistance = 0.5;
    private const double SnapSpeed = 0.1;

    private const double ExitSpeedX = 18.0;
    private const double ExitSpeedY = 10.0;
    private const double ExitAcceleration = 1.02;

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
        sprite = Utils.LoadBitmapResource(SpriteResourceName);

        calmMeowStreams = LoadSoundResources(CalmMeowResourceName);
        panicMeowStreams = LoadSoundResources(PanicMeowResourceName);
        calmMeowPlayers = CreatePlayers(calmMeowStreams);
        panicMeowPlayers = CreatePlayers(panicMeowStreams);

        ConfigureWindow();
        InitializePosition();
        InitializeTray();
        InitializeTimer();

        MouseDown += OnPetMouseDown;
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

        CloseMenu();
        DisposeTray();

        DisposePlayers(calmMeowPlayers);
        DisposePlayers(panicMeowPlayers);
        DisposeStreams(calmMeowStreams);
        DisposeStreams(panicMeowStreams);

        sprite.Dispose();

        base.OnFormClosed(e);
    }

    private void ConfigureWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;

        Width = (int)Math.Ceiling(sprite.Width * MaxScale);
        Height = (int)Math.Ceiling(sprite.Height * MaxScale);
    }

    private void InitializePosition()
    {
        Point cursor = Cursor.Position;
        x = cursor.X - Width / 2.0 + OffsetX;
        y = cursor.Y - Height / 2.0 + OffsetY;
    }

    private void InitializeTray()
    {
        trayIcon.Icon = Utils.LoadIconResource(IconResourceName);
        trayIcon.Text = AppName;
        trayIcon.Visible = true;

        ToolStripMenuItem versionItem = new($"{AppName} v{Utils.GetAppVersion()}")
        {
            Enabled = false
        };

        trayMenu.Items.Add(versionItem);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Exit", null, (_, _) => Close());

        trayIcon.ContextMenuStrip = trayMenu;
    }

    private void InitializeTimer()
    {
        timer.Interval = TickMs;
        timer.Tick += OnTick;
        timer.Start();
    }

    private void DisposeTray()
    {
        trayIcon.Visible = false;
        trayIcon.Dispose();
        trayMenu.Dispose();
    }

    private void CloseMenu()
    {
        if (menuForm is null)
        {
            return;
        }

        menuForm.FormClosed -= OnMenuClosed;

        if (!menuForm.IsDisposed)
        {
            menuForm.Close();
            menuForm.Dispose();
        }

        menuForm = null;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        double speed;

        if (isExiting)
        {
            speed = UpdateExitMovement();

            animator.Update(Environment.TickCount64, vx, speed, false);
            UpdateLayered();

            if (IsOutsideScreen())
            {
                Close();
            }

            return;
        }

        if (isPaused)
        {
            vx = 0.0;
            vy = 0.0;
            speed = 0.0;
        }
        else
        {
            speed = UpdateMovement();
        }

        animator.Update(Environment.TickCount64, vx, speed, isPaused);

        UpdateMenuPosition();
        UpdateLayered();
    }

    private double UpdateMovement()
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
            double factor = MaxSpeed / speed;
            vx *= factor;
            vy *= factor;
            speed = MaxSpeed;
        }

        if (distance <= StopDistance && speed < SnapSpeed)
        {
            x = targetX;
            y = targetY;
            vx = 0.0;
            vy = 0.0;
            return 0.0;
        }

        x += vx;
        y += vy;

        return speed;
    }

    private double UpdateExitMovement()
    {
        vx *= ExitAcceleration;
        vy *= ExitAcceleration;

        x += vx;
        y += vy;

        return Math.Sqrt(vx * vx + vy * vy);
    }

    private bool IsOutsideScreen()
    {
        int petX = (int)Math.Round(x);
        int petY = (int)Math.Round(y);

        Rectangle bounds = Screen.FromPoint(new Point(
            Math.Clamp(petX, -32000, 32000),
            Math.Clamp(petY, -32000, 32000))).Bounds;

        return petX + Width < bounds.Left
            || petX > bounds.Right
            || petY + Height < bounds.Top
            || petY > bounds.Bottom;
    }

    private void OnPetMouseDown(object? sender, MouseEventArgs e)
    {
        if (isExiting)
        {
            return;
        }

        switch (e.Button)
        {
            case MouseButtons.Left:
                if (!isPaused)
                {
                    vx += KickSpeedX;
                    vy -= KickSpeedY;
                    PlayRandomSound(calmMeowPlayers, calmMeowStreams);
                    UpdateLayered();
                }
                break;

            case MouseButtons.Right:
                PlayRandomSound(calmMeowPlayers, calmMeowStreams);
                ToggleMenu();
                break;

            case MouseButtons.Middle:
                StartExit();
                break;
        }
    }

    private void ToggleMenu()
    {
        if (menuForm is not null && !menuForm.IsDisposed)
        {
            menuForm.Close();
            return;
        }

        menuForm = new PetMenuForm();
        menuForm.FormClosed += OnMenuClosed;

        isPaused = true;

        menuForm.Show();
        UpdateMenuPosition();
        menuForm.BringToFront();
        menuForm.Activate();
    }

    private void StartExit()
    {
        CloseMenu();

        isPaused = false;
        isExiting = true;

        Rectangle bounds = Screen.FromPoint(new Point((int)Math.Round(x), (int)Math.Round(y))).Bounds;
        double centerX = x + Width / 2.0;

        vx = centerX < bounds.Left + bounds.Width / 2.0 ? -ExitSpeedX : ExitSpeedX;
        vy = -ExitSpeedY;

        PlayRandomSound(panicMeowPlayers, panicMeowStreams);
    }

    private void OnMenuClosed(object? sender, FormClosedEventArgs e)
    {
        if (sender is PetMenuForm closedMenu)
        {
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
        if (menuForm is null || menuForm.IsDisposed || isExiting)
        {
            return;
        }

        int petX = (int)Math.Round(x);
        int petY = (int)Math.Round(y);

        int menuX = petX + Width + MenuOffsetX;
        int menuY = petY + MenuOffsetY;

        Rectangle area = Screen.FromPoint(new Point(petX, petY)).WorkingArea;

        if (menuX + menuForm.Width > area.Right)
        {
            menuX = petX - menuForm.Width - MenuOffsetX;
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

    private static MemoryStream[] LoadSoundResources(string[] resourceNames)
    {
        MemoryStream[] streams = new MemoryStream[resourceNames.Length];

        for (int i = 0; i < resourceNames.Length; i++)
        {
            streams[i] = LoadSoundResource(resourceNames[i]);
        }

        return streams;
    }

    private static SoundPlayer[] CreatePlayers(MemoryStream[] streams)
    {
        SoundPlayer[] players = new SoundPlayer[streams.Length];

        for (int i = 0; i < streams.Length; i++)
        {
            players[i] = new SoundPlayer(streams[i]);
        }

        return players;
    }

    private static MemoryStream LoadSoundResource(string resourceName)
    {
        Stream? resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

        if (resource is null)
        {
            throw new InvalidOperationException($"Resource not found: {resourceName}");
        }

        MemoryStream stream = new();
        resource.CopyTo(stream);
        stream.Position = 0;
        resource.Dispose();

        return stream;
    }

    private static void PlayRandomSound(SoundPlayer[] players, MemoryStream[] streams)
    {
        int index = Random.Shared.Next(players.Length);
        streams[index].Position = 0;
        players[index].Play();
    }

    private static void DisposePlayers(SoundPlayer[] players)
    {
        foreach (SoundPlayer player in players)
        {
            player.Dispose();
        }
    }

    private static void DisposeStreams(MemoryStream[] streams)
    {
        foreach (MemoryStream stream in streams)
        {
            stream.Dispose();
        }
    }
}
