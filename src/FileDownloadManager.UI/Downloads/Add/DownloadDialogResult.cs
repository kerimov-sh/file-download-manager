using FileDownloadManager.TransferModel.Transfers;

namespace FileDownloadManager.UI.Downloads.Add;

internal sealed record DownloadDialogResult(DownloadRequest Request, string Description);
