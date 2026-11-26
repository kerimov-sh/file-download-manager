using FileDownloadManager.UI.Downloads.Library;

namespace FileDownloadManager.UI.Settings;

internal sealed record CategoryDefinition
{
    public string? Name { get; init; }
    public string? LabelKey { get; init; }
    public LibraryFilter? Filter { get; init; }
    public string? Icon { get; init; }
    public string[]? Extensions { get; init; }
    public CategoryDefinition[]? Children { get; init; }
}
