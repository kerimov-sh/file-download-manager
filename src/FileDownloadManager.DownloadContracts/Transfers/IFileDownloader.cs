using FileDownloadManager.TransferModel.Transfers;

namespace FileDownloadManager.DownloadContracts.Transfers;

public interface IFileDownloader
{
    Task DownloadAsync(DownloadRequest request, CancellationToken cancellationToken);
    Task DownloadAsync(DownloadRequest request, IProgress<TransferProgress>? progress, CancellationToken cancellationToken);
}
