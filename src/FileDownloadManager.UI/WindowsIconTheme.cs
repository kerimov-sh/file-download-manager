using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace FileDownloadManager.UI;

internal static class WindowsIconTheme
{
    private const string FluentIcons = "Segoe Fluent Icons";
    private const string Mdl2Icons = "Segoe MDL2 Assets";
    private const int Windows11Build = 22000;
    private const double GlyphSize = 16;

    // These Microsoft glyph identifiers are shared by Fluent Icons and MDL2 Assets.
    private static readonly Dictionary<string, char> Glyphs = new()
    {
        ["IconAddLink"] = '\uE71B',
        ["IconStop"] = '\uE71A',
        ["IconRemove"] = '\uE74D',
        ["IconLibrary"] = '\uE8B7',
        ["IconActive"] = '\uE896',
        ["IconFinished"] = '\uE73E',
        ["IconIncomplete"] = '\uE7BA',
        ["IconFile"] = '\uE7C3',
        ["IconSettings"] = '\uE713'
    };

    public static FontFamily? FindInstalledFamily()
    {
        if (!OperatingSystem.IsWindows())
            return null;

        var installed = FontManager.Current.SystemFonts;
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, Windows11Build) &&
            installed.FirstOrDefault(font => font.Name == FluentIcons) is { } fluent)
            return fluent;

        return installed.FirstOrDefault(font => font.Name == Mdl2Icons);
    }

    public static void Apply(IResourceDictionary resources, FontFamily? family)
    {
        foreach (var (key, symbol) in Glyphs)
        {
            if (family is null)
            {
                // Removing the window override reveals the bundled application icon.
                resources.Remove(key);
                continue;
            }

            var glyph = new FormattedText(symbol.ToString(), CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, new Typeface(family), GlyphSize, Brushes.Black);
            if (glyph.BuildGeometry(default) is { } shape)
                resources[key] = shape;
        }
    }
}
