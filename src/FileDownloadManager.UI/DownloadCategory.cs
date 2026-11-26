using FileDownloadManager.UI.Resources.Localization;

namespace FileDownloadManager.UI;

public sealed record DownloadCategory(string Name, LibraryFilter Filter, string[]? Extensions = null)
{
    public IReadOnlyList<DownloadCategory> Children { get; private init; } = [];

    public static DownloadCategory Group(string name, LibraryFilter filter) => new(name, filter)
    {
        Children =
        [
            new(Strings.Compressed, filter, [".zip", ".7z", ".rar", ".gz", ".bz2", ".tar", ".xz"]),
            new(Strings.Documents, filter, [".pdf", ".txt", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".odt"]),
            new(Strings.Music, filter, [".mp3", ".wav", ".flac", ".ogg", ".m4a", ".aac"]),
            new(Strings.Programs, filter, [".exe", ".msi", ".deb", ".rpm", ".appimage", ".apk"]),
            new(Strings.Video, filter, [".mp4", ".mkv", ".webm", ".mov", ".avi"])
        ]
    };

    public bool Matches(DownloadRow row)
    {
        var matchesState = Filter == LibraryFilter.All || Filter == row.Category ||
            (Filter == LibraryFilter.Incomplete && row.Category == LibraryFilter.Active);
        return matchesState && (Extensions is null ||
            Extensions.Contains(Path.GetExtension(row.Destination), StringComparer.OrdinalIgnoreCase));
    }
}
