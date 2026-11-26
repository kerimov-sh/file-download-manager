namespace FileDownloadManager.UI.Settings;

internal sealed record DesktopPreferences(string Language, ThemePreference Theme, bool UseSystemIcons,
    CategoryDefinition[] Categories);
