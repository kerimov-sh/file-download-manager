namespace FileDownloadManager.UI;

public sealed record DownloadRow(string Destination, string Host, LibraryFilter Category, string Status, string Size)
{
    // Repeated attempts can share the same destination and status in the session list.
    public Guid Id { get; } = Guid.NewGuid();
    public string FileName => Path.GetFileName(Destination);
}
