namespace FileDownloadManager.UI;

public sealed record DownloadRow(string Destination, Uri Source, string Description, LibraryFilter Category, string Status, string Size)
{
    // Repeated attempts can share the same destination and status in the session list.
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime LastTry { get; } = DateTime.Now;
    public TransferMetrics? Metrics { get; init; }
    public bool IsActive => Metrics is not null;
    public string FileName => Path.GetFileName(Destination);
}
