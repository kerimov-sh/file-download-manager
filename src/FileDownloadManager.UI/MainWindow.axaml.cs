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
    private readonly List<DownloadRow> _library = [];
    private DownloadRow? _activeRow;
    private CancellationTokenSource? _transfer;
    private bool _exiting;

    public MainWindow()
    {
        InitializeComponent();
        Filters.SelectedIndex = 0;
        Closed += (_, _) => _lifetime.Dispose();
    }

    private void AddClicked(object? sender, RoutedEventArgs args)
    {
        Editor.IsVisible = true;
        SourceUrl.Focus();
    }

    private void DismissClicked(object? sender, RoutedEventArgs args) => Editor.IsVisible = false;

    private void FilterChanged(object? sender, SelectionChangedEventArgs args) => RefreshRows();

    private void RefreshRows(DownloadRow? select = null)
    {
        var filter = (Filters.SelectedItem as ListBoxItem)?.Tag as LibraryFilter? ?? LibraryFilter.All;
        var visible = _library.Where(row => filter == LibraryFilter.All || row.Category == filter).ToList();
        select ??= Rows.SelectedItem as DownloadRow;
        Rows.ItemsSource = visible;
        Rows.SelectedItem = select is not null && visible.Contains(select) ? select : null;
        Empty.IsVisible = visible.Count == 0;
        Count.Text = string.Format(CultureInfo.CurrentCulture, Strings.LibraryCount, visible.Count);
        UpdateActions();
    }

    private void RowChanged(object? sender, SelectionChangedEventArgs args) => UpdateActions();

    private void UpdateActions()
    {
        var selected = Rows.SelectedItem as DownloadRow;
        var active = selected is not null && ReferenceEquals(selected, _activeRow);
        StopButton.IsEnabled = StopMenu.IsEnabled = active;
        RemoveButton.IsEnabled = RemoveMenu.IsEnabled = selected is not null && !active;
    }

    private void RemoveClicked(object? sender, RoutedEventArgs args)
    {
        if (Rows.SelectedItem is DownloadRow row && !ReferenceEquals(row, _activeRow))
        {
            _library.Remove(row);
            RefreshRows();
        }
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
        var row = new DownloadRow(path, source.Host, LibraryFilter.Active, Strings.Downloading, string.Empty);
        _activeRow = row;
        _library.Insert(0, row);
        Editor.IsVisible = false;
        Filters.SelectedIndex = 0;
        RefreshRows(row);
        SetDownloading(true);
        Status.Text = Strings.Downloading;
        var result = row with { Category = LibraryFilter.Incomplete, Status = Strings.Failed };
        try
        {
            await Downloader.DownloadAsync(new DownloadRequest(source, path), transfer.Token);
            Status.Text = Strings.Completed;
            result = row with { Category = LibraryFilter.Finished, Status = Strings.FinishedTransfers,
                Size = string.Format(CultureInfo.CurrentCulture, Strings.ByteSize, new FileInfo(path).Length) };
        }
        catch (OperationCanceledException) when (transfer.IsCancellationRequested)
        {
            Status.Text = Strings.Cancelled;
            result = result with { Status = Strings.Stopped };
        }
        catch (Exception error) when (error is HttpRequestException or IOException or InvalidDataException
                                      or UnauthorizedAccessException or ArgumentException or OperationCanceledException)
        {
            ShowError(error);
        }
        finally
        {
            _library[_library.IndexOf(row)] = result;
            var selected = ReferenceEquals(Rows.SelectedItem, row) ? result : Rows.SelectedItem as DownloadRow;
            _activeRow = null;
            _transfer = null;
            SetDownloading(false);
            RefreshRows(selected);
            if (_exiting)
                Close();
        }
    }

    private void SetDownloading(bool downloading)
    {
        AddLinkButton.IsEnabled = AddMenu.IsEnabled = !downloading;
        Activity.IsVisible = Activity.IsIndeterminate = downloading;
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
