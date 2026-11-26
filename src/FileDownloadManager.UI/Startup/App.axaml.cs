using System.Net.Http;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FileDownloadManager.Engine.Http;
using FileDownloadManager.UI.Downloads.Library;
using FileDownloadManager.UI.Resources.Localization;
using FileDownloadManager.UI.Settings;

namespace FileDownloadManager.UI.Startup;

public sealed partial class App : Application
{
    private readonly DesktopSettingsStore _settings = DesktopSettingsStore.ForCurrentUser();
    private DesktopPreferences? _preferences;
    public override void Initialize()
    {
        _preferences = _settings.Load();
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && _preferences is not null)
        {
            var client = new HttpClient();
            var window = new MainWindow { Downloader = new HttpFileDownloader(client) };
            window.ConfigureProperties(_settings, _preferences);
            desktop.MainWindow = window;
            using var asset = AssetLoader.Open(new Uri("avares://FileDownloadManager.UI/Resources/Icons/DownloadManager.png"));
            window.Icon = new WindowIcon(asset);
            var show = new NativeMenuItem(Strings.ShowDownloads);
            show.Click += (_, _) => window.RestoreFromTray();
            var exit = new NativeMenuItem(Strings.Exit);
            exit.Click += (_, _) => window.RequestExit();
            var tray = new TrayIcon
            {
                Icon = window.Icon,
                ToolTipText = Strings.Title,
                Menu = new NativeMenu { Items = { show, new NativeMenuItemSeparator(), exit } },
                IsVisible = true
            };
            tray.Clicked += (_, _) => window.RestoreFromTray();
            TrayIcon.SetIcons(this, new TrayIcons { tray });
            desktop.Exit += (_, _) =>
            {
                tray.Dispose();
                client.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
