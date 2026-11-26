using FileDownloadManager.UI.Resources.Localization;

namespace FileDownloadManager.UI.Downloads.Library;

public sealed record DownloadRow(string Destination, Uri Source, string Description, LibraryFilter Category, string Status)
{
    // Repeated attempts can share the same destination and status in the session list.
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime AddedDate { get; } = DateTime.Now;
    public DateTime LastTry { get; init; }
    public long? CompletedBytes { get; init; }
    public TransferMetrics? Metrics { get; init; }
    public string FileName => Path.GetFileName(Destination);
    public string FileType => Path.GetExtension(Destination) is { Length: > 0 } extension
        ? extension[1..].ToUpperInvariant() : Strings.NoExtension;
    public string Size => CompletedBytes is long bytes ? TransferMetrics.FormatSize(bytes) : string.Empty;
    public string GroupStatus => Category == LibraryFilter.Active ? Strings.ActiveTransfers : Status;
}
