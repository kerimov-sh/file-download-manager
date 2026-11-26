using System.Net;
using FileDownloadManager.Application;
using FileDownloadManager.Domain;

namespace FileDownloadManager.Infrastructure;

public sealed class HttpFileDownloader(HttpClient httpClient) : IFileDownloader
{
    private const string PartialFileExtension = ".partial";

    public async Task DownloadAsync(DownloadRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var response = await httpClient.GetAsync(
            request.Source, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        // An unsolicited partial response cannot establish that we have the whole file.
        if (response.StatusCode != HttpStatusCode.OK || response.Content.Headers.ContentRange is not null)
        {
            throw new InvalidDataException();
        }

        var directory = Path.GetDirectoryName(request.DestinationPath)!;
        var partialPath = Path.Combine(directory, Path.GetRandomFileName() + PartialFileExtension);
        var options = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous
        };

        var partial = new FileStream(partialPath, options);
        try
        {
            await using (partial)
            {
                await response.Content.CopyToAsync(partial, cancellationToken);
                if (response.Content.Headers.ContentLength is long expected && partial.Length != expected)
                {
                    throw new EndOfStreamException();
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(partialPath, request.DestinationPath, overwrite: false);
        }
        finally
        {
            File.Delete(partialPath);
        }
    }
}
