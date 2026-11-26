using FileDownloadManager.TransferModel;

namespace FileDownloadManager.DownloadContracts;

public interface IFileDownloader
{
    Task DownloadAsync(DownloadRequest request, CancellationToken cancellationToken);
    Task DownloadAsync(DownloadRequest request, IProgress<TransferProgress>? progress, CancellationToken cancellationToken);
}
