using Avalonia;
using Avalonia.Media;
using FileDownloadManager.UI.Resources.Localization;
using FileDownloadManager.UI.Settings;

namespace FileDownloadManager.UI.Downloads.Library;

public sealed record DownloadCategory(string Name, LibraryFilter Filter, string IconKey, string[]? Extensions)
{
    private const string DefaultFolderIcon = "IconFolder";

    public Geometry? Icon => Application.Current?.TryGetResource(IconKey, null, out var resource) == true
        ? resource as Geometry
        : null;

    public IReadOnlyList<DownloadCategory> Children { get; private init; } = [];

    internal static DownloadCategory FromDefinition(CategoryDefinition definition, LibraryFilter parentFilter = LibraryFilter.All)
    {
        var filter = definition.Filter ?? parentFilter;
        var name = string.IsNullOrWhiteSpace(definition.Name) ? Strings.Get(definition.LabelKey!) : definition.Name;
        return new DownloadCategory(name, filter, definition.Icon ?? DefaultFolderIcon, definition.Extensions)
        {
            Children = definition.Children?.Select(child => FromDefinition(child, filter)).ToArray() ?? []
        };
    }

    public bool Matches(DownloadRow row)
    {
        var matchesState = Filter == LibraryFilter.All || Filter == row.Category ||
            (Filter == LibraryFilter.Incomplete && row.Category == LibraryFilter.Active);
        return matchesState && (Extensions is not { Length: > 0 } ||
            Extensions.Any(extension => row.FileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)));
    }
}
