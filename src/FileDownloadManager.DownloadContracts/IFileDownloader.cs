using FileDownloadManager.TransferModel;

namespace FileDownloadManager.DownloadContracts;

public interface IFileDownloader
{
    Task DownloadAsync(DownloadRequest request, CancellationToken cancellationToken);
}
