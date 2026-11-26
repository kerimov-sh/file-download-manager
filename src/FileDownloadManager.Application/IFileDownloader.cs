using FileDownloadManager.Domain;

namespace FileDownloadManager.Application;

public interface IFileDownloader
{
    Task DownloadAsync(DownloadRequest request, CancellationToken cancellationToken);
}
