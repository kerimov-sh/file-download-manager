namespace FileDownloadManager.TransferModel.Transfers;

public readonly record struct TransferProgress(long BytesWritten, long? TotalBytes, TimeSpan Elapsed);
