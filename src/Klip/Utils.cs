using System;
using System.Drawing;
using System.IO;
using System.Reflection;

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
}
