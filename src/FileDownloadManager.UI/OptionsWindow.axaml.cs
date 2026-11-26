using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FileDownloadManager.UI;

public sealed partial class OptionsWindow : Window
{
    public OptionsWindow() => InitializeComponent();

    internal void SetIconStyle(bool systemAvailable, bool useSystem)
    {
        SystemStyle.IsEnabled = systemAvailable;
        SystemStyle.IsChecked = useSystem;
        BundledStyle.IsChecked = !useSystem;
    }

    private void ConfirmClicked(object? sender, RoutedEventArgs args) => Close(SystemStyle.IsChecked == true);

    private void CancelClicked(object? sender, RoutedEventArgs args) => Close();
}
