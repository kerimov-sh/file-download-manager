using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using FileDownloadManager.UI.Resources.Localization;
using FileDownloadManager.UI.Appearance.Categories;
using FileDownloadManager.UI.Downloads.Library;
using FileDownloadManager.UI.Settings;

namespace FileDownloadManager.UI.Appearance;

public sealed partial class OptionsWindow : Window
{
    private DesktopSettingsStore? _store;
    private DesktopPreferences? _preferences;
    private readonly ObservableCollection<CategoryDraft> _categories = [];
    private readonly CategoryFilterChoice[] _filters =
    [
        new(Strings.InheritFilter, null),
        new(Strings.AllDownloads, LibraryFilter.All),
        new(Strings.ActiveTransfers, LibraryFilter.Active),
        new(Strings.IncompleteTransfers, LibraryFilter.Incomplete),
        new(Strings.FinishedTransfers, LibraryFilter.Finished)
    ];
    private readonly CategoryIconChoice[] _icons =
    [
        new(Strings.DefaultFolder, null),
        new(Strings.DefaultFolder, "IconFolder"),
        new(Strings.Compressed, "IconFolderArchive"),
        new(Strings.Documents, "IconFolderDocument"),
        new(Strings.Music, "IconFolderMusic"),
        new(Strings.Programs, "IconFolderProgram"),
        new(Strings.Video, "IconFolderVideo")
    ];

    public OptionsWindow()
    {
        InitializeComponent();
        CategoryTree.ItemsSource = _categories;
    }

    internal void Edit(DesktopSettingsStore store, DesktopPreferences preferences, bool systemAvailable)
    {
        _store = store;
        _preferences = preferences;
        SystemStyle.IsEnabled = systemAvailable;
        SystemStyle.IsChecked = systemAvailable && preferences.UseSystemIcons;
        BundledStyle.IsChecked = !systemAvailable || !preferences.UseSystemIcons;
        ThemeChoices.SelectedItem = ThemeChoices.Items.OfType<ComboBoxItem>()
            .First(item => item.Tag is ThemePreference theme && theme == preferences.Theme);
        var languages = store.Languages().Select(LanguageChoice.FromCode).OfType<LanguageChoice>()
            .OrderBy(language => language.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        LanguageChoices.ItemsSource = languages;
        LanguageChoices.SelectedItem = languages.FirstOrDefault(language =>
            string.Equals(language.Code, preferences.Language, StringComparison.OrdinalIgnoreCase));
        LanguagePath.Text = store.LanguageDirectory;
        foreach (var category in preferences.Categories)
            _categories.Add(new CategoryDraft(category, null, _filters, _icons));
        CategoryTree.SelectedItem = _categories.FirstOrDefault();
    }

    private void CategorySelectionChanged(object? sender, SelectionChangedEventArgs args) => UpdateCategoryControls();

    private void UpdateCategoryControls()
    {
        var selected = CategoryTree.SelectedItem as CategoryDraft;
        CategoryFields.Content = selected;
        AddChildButton.IsEnabled = RemoveCategoryButton.IsEnabled = selected is not null;
        var siblings = selected?.Parent?.Children ?? _categories;
        var index = selected is null ? -1 : siblings.IndexOf(selected);
        MoveUpButton.IsEnabled = index > 0;
        MoveDownButton.IsEnabled = index >= 0 && index < siblings.Count - 1;
    }

    private void AddCategoryClicked(object? sender, RoutedEventArgs args) => AddCategory(null);

    private void AddSubcategoryClicked(object? sender, RoutedEventArgs args)
    {
        if (CategoryTree.SelectedItem is CategoryDraft parent)
            AddCategory(parent);
    }

    private void AddCategory(CategoryDraft? parent)
    {
        var draft = new CategoryDraft(new CategoryDefinition { Name = Strings.NewCategory }, parent, _filters, _icons);
        (parent?.Children ?? _categories).Add(draft);
        CategoryTree.SelectedItem = draft;
    }

    private void RemoveCategoryClicked(object? sender, RoutedEventArgs args)
    {
        if (CategoryTree.SelectedItem is not CategoryDraft selected)
            return;

        var siblings = selected.Parent?.Children ?? _categories;
        var index = siblings.IndexOf(selected);
        siblings.RemoveAt(index);
        CategoryTree.SelectedItem = siblings.Count == 0 ? selected.Parent : siblings[Math.Min(index, siblings.Count - 1)];
    }

    private void MoveCategoryUpClicked(object? sender, RoutedEventArgs args) => MoveCategory(-1);

    private void MoveCategoryDownClicked(object? sender, RoutedEventArgs args) => MoveCategory(1);

    private void MoveCategory(int offset)
    {
        if (CategoryTree.SelectedItem is not CategoryDraft selected)
            return;
        var siblings = selected.Parent?.Children ?? _categories;
        var current = siblings.IndexOf(selected);
        var target = current + offset;
        if (target < 0 || target >= siblings.Count)
            return;

        siblings.Move(current, target);
        CategoryTree.SelectedItem = selected;
        UpdateCategoryControls();
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
                Language = (LanguageChoices.SelectedItem as LanguageChoice)?.Code ?? _preferences.Language,
                UseSystemIcons = SystemStyle.IsEnabled ? SystemStyle.IsChecked == true : _preferences.UseSystemIcons,
                Categories = _categories.Select(category => category.ToDefinition()).ToArray()
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
