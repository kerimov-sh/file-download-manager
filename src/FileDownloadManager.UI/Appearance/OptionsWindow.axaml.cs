using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Globalization;
using System.Text.Json;
using FileDownloadManager.UI.Resources.Localization;
using FileDownloadManager.UI.Settings;

namespace FileDownloadManager.UI.Appearance;

public sealed partial class OptionsWindow : Window
{
    private DesktopSettingsStore? _store;
    private DesktopPreferences? _preferences;

    public OptionsWindow() => InitializeComponent();

    internal void Edit(DesktopSettingsStore store, DesktopPreferences preferences, bool systemAvailable)
    {
        _store = store;
        _preferences = preferences;
        SystemStyle.IsEnabled = systemAvailable;
        SystemStyle.IsChecked = systemAvailable && preferences.UseSystemIcons;
        BundledStyle.IsChecked = !systemAvailable || !preferences.UseSystemIcons;
        ThemeChoices.SelectedItem = ThemeChoices.Items.OfType<ComboBoxItem>()
            .First(item => item.Tag is ThemePreference theme && theme == preferences.Theme);
        LanguageChoices.ItemsSource = store.Languages();
        LanguageChoices.SelectedItem = preferences.Language;
        LanguagePath.Text = store.LanguageDirectory;
        CategoriesEditor.Text = DesktopSettingsStore.FormatCategories(preferences.Categories);
    }

    private void ConfirmClicked(object? sender, RoutedEventArgs args)
    {
        if (_store is null || _preferences is null)
            return;
        try
        {
            var preferences = _preferences with
            {
                Theme = (ThemeChoices.SelectedItem as ComboBoxItem)?.Tag as ThemePreference? ?? _preferences.Theme,
                Language = LanguageChoices.SelectedItem as string ?? _preferences.Language,
                UseSystemIcons = SystemStyle.IsEnabled ? SystemStyle.IsChecked == true : _preferences.UseSystemIcons,
                Categories = DesktopSettingsStore.ParseCategories(CategoriesEditor.Text ?? string.Empty)
            };
            _store.Save(preferences);
            Close(preferences);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException
                                      or ArgumentException or FormatException)
        {
            Feedback.Text = string.Format(CultureInfo.CurrentCulture, Strings.SettingsFailure, error.Message);
            Feedback.IsVisible = true;
        }
    }

    private void CancelClicked(object? sender, RoutedEventArgs args) => Close();
}
