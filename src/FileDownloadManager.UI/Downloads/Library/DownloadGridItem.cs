namespace FileDownloadManager.UI.Downloads.Library;

public sealed record DownloadGridItem(string Title, DownloadRow? Download = null)
{
    public bool IsGroup => Download is null;
}
