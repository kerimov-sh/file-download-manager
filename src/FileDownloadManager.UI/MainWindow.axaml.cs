using System.Globalization;
using System.Net.Http;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FileDownloadManager.DownloadContracts;
using FileDownloadManager.TransferModel;
using FileDownloadManager.UI.Resources.Localization;

namespace FileDownloadManager.UI;

public sealed partial class MainWindow : Window
{
    public required IFileDownloader Downloader { private get; init; }
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _transfer;
    private bool _exiting;

    public MainWindow()
    {
        InitializeComponent();
        Closed += (_, _) => _lifetime.Dispose();
    }

    private async void BrowseClicked(object? sender, RoutedEventArgs args)
    {
        try
        {
            using var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = Strings.PickerTitle,
                ShowOverwritePrompt = false
            });
            if (file is null)
                return;

            if (file.TryGetLocalPath() is { } path)
                Destination.Text = path;
            else
                Status.Text = Strings.LocalOnly;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            ShowError(error);
        }
    }

    private async void StartClicked(object? sender, RoutedEventArgs args)
    {
        if (_transfer is not null || _exiting)
            return;

        if (!Uri.TryCreate(SourceUrl.Text?.Trim(), UriKind.Absolute, out var source) ||
            (source.Scheme != Uri.UriSchemeHttp && source.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrEmpty(source.Host))
        {
            Status.Text = Strings.InvalidUrl;
            SourceUrl.Focus();
            return;
        }

        var path = Destination.Text;
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) ||
            string.IsNullOrEmpty(Path.GetFileName(path)) || !Directory.Exists(Path.GetDirectoryName(path)))
        {
            Status.Text = Strings.InvalidDestination;
            Destination.Focus();
            return;
        }

        if (File.Exists(path) || Directory.Exists(path))
        {
            Status.Text = Strings.AlreadyExists;
            return;
        }

        using var transfer = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _transfer = transfer;
        SetDownloading(true);
        Status.Text = Strings.Downloading;
        try
        {
            await Downloader.DownloadAsync(new DownloadRequest(source, path), transfer.Token);
            Status.Text = Strings.Completed;
        }
        catch (OperationCanceledException) when (transfer.IsCancellationRequested)
        {
            Status.Text = Strings.Cancelled;
        }
        catch (Exception error) when (error is HttpRequestException or IOException or InvalidDataException
                                      or UnauthorizedAccessException or ArgumentException or OperationCanceledException)
        {
            ShowError(error);
        }
        finally
        {
            _transfer = null;
            SetDownloading(false);
            if (_exiting)
                Close();
        }
    }

    private void SetDownloading(bool downloading)
    {
        Fields.IsEnabled = Start.IsEnabled = !downloading;
        Cancel.IsVisible = Activity.IsVisible = Activity.IsIndeterminate = downloading;
    }

    private void ShowError(Exception error) =>
        Status.Text = string.Format(CultureInfo.CurrentCulture, Strings.Failure, error.Message);

    private void CancelClicked(object? sender, RoutedEventArgs args) => _transfer?.Cancel();

    private void ExitClicked(object? sender, RoutedEventArgs args)
    {
        _exiting = true;
        _lifetime.Cancel();
        // Let the engine close the partial file before the last window ends the process.
        if (_transfer is null)
            Close();
    }

    protected override void OnClosing(WindowClosingEventArgs args)
    {
        base.OnClosing(args);
        if (!_exiting && args.CloseReason == WindowCloseReason.WindowClosing)
        {
            args.Cancel = true;
            WindowState = WindowState.Minimized;
        }
    }
}
