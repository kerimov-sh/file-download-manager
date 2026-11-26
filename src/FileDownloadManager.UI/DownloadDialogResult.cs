using FileDownloadManager.TransferModel;

namespace FileDownloadManager.UI;

internal sealed record DownloadDialogResult(DownloadRequest Request, string Description);
