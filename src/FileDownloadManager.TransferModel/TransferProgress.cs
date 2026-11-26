namespace FileDownloadManager.TransferModel;

public readonly record struct TransferProgress(long BytesWritten, long? TotalBytes, TimeSpan Elapsed);
