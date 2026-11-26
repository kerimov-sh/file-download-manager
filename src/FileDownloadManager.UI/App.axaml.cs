using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FileDownloadManager.Engine;

namespace FileDownloadManager.UI;

public sealed partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var client = new HttpClient();
            desktop.MainWindow = new MainWindow { Downloader = new HttpFileDownloader(client) };
            desktop.Exit += (_, _) => client.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
