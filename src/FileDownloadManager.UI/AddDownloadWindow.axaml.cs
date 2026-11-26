using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FileDownloadManager.TransferModel;
using FileDownloadManager.UI.Resources.Localization;

namespace FileDownloadManager.UI;

public sealed partial class AddDownloadWindow : Window
{
    public AddDownloadWindow()
    {
        InitializeComponent();
        Opened += (_, _) => SourceUrl.Focus();
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
            {
                Destination.Text = path;
                Feedback.IsVisible = false;
            }
            else
                ShowFeedback(Strings.LocalOnly);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            ShowFeedback(error.Message);
        }
    }

    private void StartClicked(object? sender, RoutedEventArgs args)
    {
        if (!Uri.TryCreate(SourceUrl.Text?.Trim(), UriKind.Absolute, out var source) ||
            (source.Scheme != Uri.UriSchemeHttp && source.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrEmpty(source.Host))
        {
            ShowFeedback(Strings.InvalidUrl);
            SourceUrl.Focus();
            return;
        }

        var path = Destination.Text;
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) ||
            string.IsNullOrEmpty(Path.GetFileName(path)) || !Directory.Exists(Path.GetDirectoryName(path)))
        {
            ShowFeedback(Strings.InvalidDestination);
            Destination.Focus();
            return;
        }

        try
        {
            var request = new DownloadRequest(source, path);
            if (File.Exists(request.DestinationPath) || Directory.Exists(request.DestinationPath))
            {
                ShowFeedback(Strings.AlreadyExists);
                Destination.Focus();
                return;
            }

            Close(new DownloadDialogResult(request, Notes.Text ?? string.Empty));
        }
        catch (Exception error) when (error is ArgumentException or IOException or NotSupportedException)
        {
            ShowFeedback(Strings.InvalidDestination);
            Destination.Focus();
        }
    }

    private void ShowFeedback(string message)
    {
        Feedback.Text = message;
        Feedback.IsVisible = true;
    }

    private void CancelClicked(object? sender, RoutedEventArgs args) => Close();
}
