using Avalonia;
using Avalonia.Media;
using FileDownloadManager.UI.Resources.Localization;

namespace FileDownloadManager.UI.Downloads.Library;

public sealed record DownloadCategory(string Name, LibraryFilter Filter, string IconKey = "IconFolder", string[]? Extensions = null)
{
    public Geometry? Icon => Application.Current?.TryGetResource(IconKey, null, out var resource) == true
        ? resource as Geometry
        : null;

    public IReadOnlyList<DownloadCategory> Children { get; private init; } = [];

    public static DownloadCategory Group(string name, LibraryFilter filter) => new(name, filter)
    {
        Children =
        [
            new(Strings.Compressed, filter, "IconFolderArchive", [".zip", ".7z", ".rar", ".gz", ".bz2", ".tar", ".xz"]),
            new(Strings.Documents, filter, "IconFolderDocument", [".pdf", ".txt", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".odt"]),
            new(Strings.Music, filter, "IconFolderMusic", [".mp3", ".wav", ".flac", ".ogg", ".m4a", ".aac"]),
            new(Strings.Programs, filter, "IconFolderProgram", [".exe", ".msi", ".deb", ".rpm", ".appimage", ".apk"]),
            new(Strings.Video, filter, "IconFolderVideo", [".mp4", ".mkv", ".webm", ".mov", ".avi"])
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
