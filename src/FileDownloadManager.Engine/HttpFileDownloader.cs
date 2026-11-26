using System.Net;
using System.Diagnostics;
using FileDownloadManager.DownloadContracts;
using FileDownloadManager.TransferModel;

namespace FileDownloadManager.Engine;

public sealed class HttpFileDownloader(HttpClient httpClient) : IFileDownloader
{
    private const string PartialFileExtension = ".partial";

    private const int CopyBufferBytes = 64 * 1024;
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(250);

    public Task DownloadAsync(DownloadRequest request, CancellationToken cancellationToken) =>
        DownloadAsync(request, null, cancellationToken);

    public async Task DownloadAsync(DownloadRequest request, IProgress<TransferProgress>? progress, CancellationToken cancellationToken)
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

        // Staging beside the destination keeps the final rename on the same filesystem.
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
                if (progress is null)
                    await response.Content.CopyToAsync(partial, cancellationToken);
                else
                    await CopyWithProgressAsync(response.Content, partial, progress, cancellationToken);
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

    private static async Task CopyWithProgressAsync(HttpContent content, FileStream output,
        IProgress<TransferProgress> progress, CancellationToken cancellationToken)
    {
        var clock = Stopwatch.StartNew();
        var nextUpdate = TimeSpan.Zero;
        var buffer = new byte[CopyBufferBytes];
        var total = content.Headers.ContentLength;
        using var input = await content.ReadAsStreamAsync(cancellationToken);
        int count;
        while ((count = await input.ReadAsync(buffer, cancellationToken)) != 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
            var elapsed = clock.Elapsed;
            if (elapsed >= nextUpdate)
            {
                progress.Report(new(output.Position, total, elapsed));
                nextUpdate = elapsed + ProgressInterval;
            }
        }
        progress.Report(new(output.Position, total, clock.Elapsed));
    }
}
