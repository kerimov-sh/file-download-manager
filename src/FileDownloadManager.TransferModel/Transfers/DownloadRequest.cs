namespace FileDownloadManager.TransferModel.Transfers;

public sealed class DownloadRequest
{
    public DownloadRequest(Uri source, string destinationPath)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        if (!source.IsAbsoluteUri ||
            (source.Scheme != Uri.UriSchemeHttp && source.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentOutOfRangeException(nameof(source));
        }

        Source = source;
        DestinationPath = Path.GetFullPath(destinationPath);
    }

    public Uri Source { get; }
    public string DestinationPath { get; }
}
