using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Klip;

internal static class Utils
{
    public static Bitmap LoadBitmapResource(string resourceName)
    {
        using Stream stream = OpenResourceStream(resourceName);
        using Image image = Image.FromStream(stream);
        return new Bitmap(image);
    }

    public static Icon LoadIconResource(string resourceName)
    {
        using Stream stream = OpenResourceStream(resourceName);
        return new Icon(stream);
    }

    public static string GetAppVersion()
    {
        Assembly assembly = typeof(Utils).Assembly;

        string? version =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString(3);

        if (string.IsNullOrEmpty(version))
        {
            return "unknown";
        }

        int plusIndex = version.IndexOf('+');

        return plusIndex > 0
            ? version[..plusIndex]
            : version;
    }

    private static Stream OpenResourceStream(string resourceName)
    {
        Assembly assembly = typeof(Utils).Assembly;
        Stream? stream = assembly.GetManifestResourceStream(resourceName);

        if (stream is not null)
        {
            return stream;
        }

        throw new InvalidOperationException(
            "Embedded resource not found: " + resourceName + Environment.NewLine +
            "Available resources:" + Environment.NewLine +
            string.Join(Environment.NewLine, assembly.GetManifestResourceNames()));
    }

    public static void WriteClipboardText(string text)
    {
        ExecuteClipboardAction(() =>
        {
            if (string.IsNullOrEmpty(text))
            {
                Clipboard.Clear();
                return;
            }

            Clipboard.SetText(text);
        });
    }

    public static void WriteClipboardImage(Image? image)
    {
        ExecuteClipboardAction(() =>
        {
            if (image is null)
            {
                Clipboard.Clear();
                return;
            }

            Clipboard.SetImage((Image)image.Clone());
        });
    }

    public static void ExecuteClipboardAction(Action action)
    {
        ExecuteClipboardFunc(() =>
        {
            action();
            return 0;
        });
    }

    public static T ExecuteClipboardFunc<T>(Func<T> func)
    {
        const int maxAttempts = 5;
        const int delayMs = 20;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                return func();
            }
            catch (ExternalException) when (attempt < maxAttempts - 1)
            {
                Thread.Sleep(delayMs);
            }
        }

        return func();
    }
}

