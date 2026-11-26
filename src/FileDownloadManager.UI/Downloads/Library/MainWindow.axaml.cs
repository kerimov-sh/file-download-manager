using System.Globalization;
using System.Net.Http;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using FileDownloadManager.DownloadContracts.Transfers;
using FileDownloadManager.TransferModel.Transfers;
using FileDownloadManager.UI.Appearance;
using FileDownloadManager.UI.Downloads.Add;
using FileDownloadManager.UI.Resources.Localization;

namespace FileDownloadManager.UI.Downloads.Library;

public sealed partial class MainWindow : Window
{
    private const string EmptyGridClass = "emptyGrid";
    public required IFileDownloader Downloader { private get; init; }
    private readonly CancellationTokenSource _lifetime = new();
    private readonly List<DownloadRow> _library = [];
    private DownloadRow? _activeRow;
    private CancellationTokenSource? _transfer;
    private bool _exiting;
    private Window? _dialog;
    private DownloadColumn? _sortColumn;
    private Button? _sortHeader;
    private object? _sortHeaderLabel;
    private bool _sortDescending;
    private readonly FontFamily? _systemIcons = WindowsIconTheme.FindInstalledFamily();

    public MainWindow()
    {
        InitializeComponent();
        if (OperatingSystem.IsWindows())
            FontFamily = new FontFamily("Segoe UI");
        SystemIcons.IsEnabled = _systemIcons is not null;
        SetIconTheme(_systemIcons);
        Filters.ItemsSource = new[]
        {
            DownloadCategory.Group(Strings.AllDownloads, LibraryFilter.All),
            DownloadCategory.Group(Strings.IncompleteTransfers, LibraryFilter.Incomplete),
            DownloadCategory.Group(Strings.FinishedTransfers, LibraryFilter.Finished),
            new DownloadCategory(Strings.ActiveTransfers, LibraryFilter.Active)
        };
        Filters.SelectedItem = Filters.Items[0];
        Grouping.SelectedIndex = 0;
        Closed += (_, _) => _lifetime.Dispose();
    }

    private void SetIconTheme(FontFamily? family)
    {
        WindowsIconTheme.Apply(Resources, family);
        SystemIcons.IsChecked = family is not null;
        BundledIcons.IsChecked = family is null;
    }

    private void SystemIconsClicked(object? sender, RoutedEventArgs args) => SetIconTheme(_systemIcons);

    private void BundledIconsClicked(object? sender, RoutedEventArgs args) => SetIconTheme(null);

    internal void RestoreFromTray()
    {
        Show();
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;
        Activate();
    }

    private async void AddClicked(object? sender, RoutedEventArgs args)
    {
        if (_dialog is not null || _transfer is not null || _exiting)
            return;

        var result = await ShowOwnedDialogAsync<DownloadDialogResult?>(new AddDownloadWindow());
        if (result is not null && !_exiting)
            await StartTransferAsync(result.Request, result.Description, null, _lifetime.Token);
    }

    private async Task<TResult> ShowOwnedDialogAsync<TResult>(Window dialog)
    {
        _dialog = dialog;
        dialog.Icon = Icon;
        dialog.FontFamily = FontFamily;
        try
        {
            return await dialog.ShowDialog<TResult>(this);
        }
        finally
        {
            _dialog = null;
        }
    }


    private void FilterChanged(object? sender, SelectionChangedEventArgs args) => RefreshRows();

    private void GroupingChanged(object? sender, SelectionChangedEventArgs args) => RefreshRows();

    private DownloadRow? SelectedDownload => (Rows.SelectedItem as DownloadGridItem)?.Download;

    private void SortClicked(object? sender, RoutedEventArgs args)
    {
        if (sender is not Button { Tag: DownloadColumn column } header)
            return;

        if (_sortHeader is not null)
            _sortHeader.Content = _sortHeaderLabel;

        _sortDescending = _sortColumn == column && !_sortDescending;
        _sortColumn = column;
        _sortHeader = header;
        _sortHeaderLabel = header.Content;
        header.Content = string.Format(CultureInfo.CurrentCulture,
            _sortDescending ? Strings.SortDescending : Strings.SortAscending, _sortHeaderLabel);
        RefreshRows();
    }

    private void RefreshRows(DownloadRow? select = null)
    {
        var filter = Filters.SelectedItem as DownloadCategory;
        var visible = _library.Where(row => filter?.Matches(row) ?? true).ToList();
        select ??= SelectedDownload;
        IEnumerable<DownloadRow> sorted = visible;
        if (_sortColumn is DownloadColumn column)
        {
            sorted = _sortDescending
                ? visible.OrderByDescending(row => SortKey(row, column))
                : visible.OrderBy(row => SortKey(row, column));
        }

        var grouping = (Grouping.SelectedItem as ComboBoxItem)?.Tag as DownloadGrouping? ?? DownloadGrouping.None;
        var items = grouping == DownloadGrouping.None
            ? sorted.Select(ToGridItem).ToList()
            : sorted.GroupBy(row => GroupKey(row, grouping)).SelectMany(group =>
                group.Select(ToGridItem).Prepend(new DownloadGridItem(group.Key))).ToList();
        Rows.ItemsSource = items;
        Rows.Classes.Set(EmptyGridClass, items.Count == 0);
        Rows.SelectedItem = select is null ? null : items.FirstOrDefault(item => item.Download?.Id == select.Id);
        Count.Text = string.Format(CultureInfo.CurrentCulture, Strings.LibraryCount, visible.Count);
        UpdateActions();
    }

    private static DownloadGridItem ToGridItem(DownloadRow row) => new(row.FileName, row);

    private static IComparable? SortKey(DownloadRow row, DownloadColumn column) => column switch
    {
        DownloadColumn.FileName => row.FileName,
        DownloadColumn.FileType => row.FileType,
        DownloadColumn.Size => row.CompletedBytes ?? row.Metrics?.TotalBytes,
        DownloadColumn.Status => row.GroupStatus,
        DownloadColumn.TimeLeft => row.Metrics?.Remaining,
        DownloadColumn.Rate => row.Metrics?.BytesPerSecond,
        DownloadColumn.AddedDate => row.AddedDate,
        DownloadColumn.LastTry => row.LastTry,
        DownloadColumn.Description => row.Description,
        _ => throw new ArgumentOutOfRangeException(nameof(column))
    };

    private static string GroupKey(DownloadRow row, DownloadGrouping grouping) => grouping switch
    {
        DownloadGrouping.FileType => row.FileType,
        DownloadGrouping.Status => row.GroupStatus,
        DownloadGrouping.AddedDate => row.AddedDate.ToString("d", CultureInfo.CurrentCulture),
        _ => throw new ArgumentOutOfRangeException(nameof(grouping))
    };

    private void RowChanged(object? sender, SelectionChangedEventArgs args) => UpdateActions();

    private void UpdateActions()
    {
        var selected = SelectedDownload;
        var active = selected is not null && ReferenceEquals(selected, _activeRow);
        StopButton.IsEnabled = StopMenu.IsEnabled = active;
        StopAllButton.IsEnabled = _activeRow is not null;
        RetryButton.IsEnabled = _activeRow is null && selected?.Category == LibraryFilter.Incomplete;
        ClearButton.IsEnabled = _library.Any(row => row.Category == LibraryFilter.Finished);
        RemoveButton.IsEnabled = RemoveMenu.IsEnabled = selected is not null && !active;
    }

    private void ClearFinishedClicked(object? sender, RoutedEventArgs args)
    {
        _library.RemoveAll(row => row.Category == LibraryFilter.Finished);
        RefreshRows();
    }

    private async void RetryClicked(object? sender, RoutedEventArgs args)
    {
        if (SelectedDownload is not { Category: LibraryFilter.Incomplete } row ||
            _transfer is not null || _exiting)
            return;

        if (File.Exists(row.Destination) || Directory.Exists(row.Destination))
        {
            Status.Text = Strings.AlreadyExists;
            return;
        }

        await StartTransferAsync(new DownloadRequest(row.Source, row.Destination), row.Description, row, _lifetime.Token);
    }

    private async void OptionsClicked(object? sender, RoutedEventArgs args)
    {
        if (_dialog is not null || _exiting)
            return;

        var options = new OptionsWindow();
        options.SetIconStyle(_systemIcons is not null, SystemIcons.IsChecked == true);
        var useSystem = await ShowOwnedDialogAsync<bool?>(options);
        if (useSystem is bool confirmed && !_exiting)
            SetIconTheme(confirmed ? _systemIcons : null);
    }


    private void RemoveClicked(object? sender, RoutedEventArgs args)
    {
        if (SelectedDownload is { } row && !ReferenceEquals(row, _activeRow))
        {
            _library.Remove(row);
            RefreshRows();
        }
    }

    private async Task StartTransferAsync(DownloadRequest request, string description, DownloadRow? previous,
        CancellationToken cancellationToken)
    {
        var path = request.DestinationPath;
        using var transfer = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _transfer = transfer;
        var metrics = new TransferMetrics();
        var row = (previous ?? new DownloadRow(path, request.Source, description, LibraryFilter.Active, Strings.Downloading))
            with { Category = LibraryFilter.Active, Status = Strings.Downloading, LastTry = DateTime.Now, Metrics = metrics };
        _activeRow = row;
        if (previous is null)
            _library.Insert(0, row);
        else
            _library[_library.IndexOf(previous)] = row;
        Filters.SelectedItem = Filters.Items[0];
        RefreshRows(row);
        SetDownloading(true);
        Status.Text = Strings.Downloading;
        var result = row with { Category = LibraryFilter.Incomplete, Status = Strings.Failed, Metrics = null };
        try
        {
            await Downloader.DownloadAsync(request,
                new Progress<TransferProgress>(metrics.Update), transfer.Token);
            Status.Text = Strings.Completed;
            result = result with { Category = LibraryFilter.Finished, Status = Strings.FinishedTransfers,
                CompletedBytes = new FileInfo(path).Length };
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
            var selected = ReferenceEquals(SelectedDownload, row) ? result : SelectedDownload;
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

    private void ExitClicked(object? sender, RoutedEventArgs args) => RequestExit();

    internal void RequestExit()
    {
        _exiting = true;
        _lifetime.Cancel();
        _dialog?.Close();
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
            // Linux shells can accept a tray registration without displaying a tray host.
            // Keep a taskbar entry there so the window is always reachable.
            if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
                Hide();
            else
                WindowState = WindowState.Minimized;
        }
    }
}
