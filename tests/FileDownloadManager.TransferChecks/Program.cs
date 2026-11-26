using System.Net;
using FileDownloadManager.Application;
using FileDownloadManager.Domain;
using FileDownloadManager.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FileDownloadManager.TransferChecks;

internal static class Program
{
    private const string FileRoute = "/file";
    private const string RedirectRoute = "/redirect";
    private const string SlowRoute = "/slow";
    private const string TruncatedRoute = "/truncated";
    private const string PartialRoute = "/partial";
    private const int DynamicPort = 0;
    private static readonly byte[] Payload = "FileDownloadManager transfer fixture"u8.ToArray();

    public static async Task Main()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var token = deadline.Token;
        var slowResponseStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, DynamicPort));
        await using var server = builder.Build();
        server.MapGet(FileRoute, async context =>
        {
            // Flush headers before writing to exercise unknown-length streaming.
            await context.Response.StartAsync(context.RequestAborted);
            await context.Response.Body.WriteAsync(Payload, context.RequestAborted);
        });
        server.MapGet(RedirectRoute, () => Results.Redirect(FileRoute));
        server.MapGet(PartialRoute, () => Results.StatusCode(StatusCodes.Status206PartialContent));
        server.MapGet(TruncatedRoute, async context =>
        {
            context.Response.ContentLength = Payload.Length + 1;
            await context.Response.Body.WriteAsync(Payload, context.RequestAborted);
            await context.Response.Body.FlushAsync(context.RequestAborted);
            context.Abort();
        });
        server.MapGet(SlowRoute, async context =>
        {
            await context.Response.Body.WriteAsync(Payload, context.RequestAborted);
            await context.Response.Body.FlushAsync(context.RequestAborted);
            slowResponseStarted.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, context.RequestAborted);
        });
        await server.StartAsync(token);
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            using var client = new HttpClient();
            IFileDownloader downloader = new HttpFileDownloader(client);
            var baseUri = new Uri(server.Urls.Single());
            var destination = Path.Combine(directory.FullName, "download.bin");
            DownloadRequest Request(string route) => new(new Uri(baseUri, route), destination);

            await downloader.DownloadAsync(Request(RedirectRoute), token);
            await ExpectAsync<IOException>(() => downloader.DownloadAsync(Request(FileRoute), token));
            var bytes = await File.ReadAllBytesAsync(destination, token);
            if (!Payload.SequenceEqual(bytes))
            {
                throw new InvalidDataException("Completed download did not match fixture bytes.");
            }

            File.Delete(destination);
            await ExpectAsync<InvalidDataException>(() => downloader.DownloadAsync(Request(PartialRoute), token));
            await ExpectAsync<HttpRequestException>(() => downloader.DownloadAsync(Request("/missing"), token));
            await ExpectAsync<HttpRequestException>(() => downloader.DownloadAsync(Request(TruncatedRoute), token));

            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
            var transfer = downloader.DownloadAsync(Request(SlowRoute), cancellation.Token);
            await slowResponseStarted.Task.WaitAsync(token);
            await cancellation.CancelAsync();
            await ExpectAsync<OperationCanceledException>(() => transfer);
            token.ThrowIfCancellationRequested();
            if (directory.EnumerateFileSystemInfos().Any())
            {
                throw new IOException("A failed or cancelled transfer left output behind.");
            }

            Console.WriteLine("Passed transfer, redirect, collision, HTTP error, partial response, truncation, and cancellation checks.");
        }
        finally
        {
            directory.Delete(recursive: true);
            await server.StopAsync(token);
        }
    }

    private static async Task ExpectAsync<TException>(Func<Task> action) where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }
}
