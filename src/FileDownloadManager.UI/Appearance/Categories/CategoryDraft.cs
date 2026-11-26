using System.Collections.ObjectModel;
using System.ComponentModel;
using FileDownloadManager.UI.Resources.Localization;
using FileDownloadManager.UI.Settings;

namespace FileDownloadManager.UI.Appearance.Categories;

public sealed class CategoryDraft : INotifyPropertyChanged
{
    private static readonly char[] ExtensionSeparators = [',', ';', ' ', '\r', '\n', '\t'];
    private readonly CategoryDefinition _original;
    private readonly string _defaultName;
    private string _name;

    internal CategoryDraft(CategoryDefinition definition, CategoryDraft? parent,
        IReadOnlyList<CategoryFilterChoice> filters, IReadOnlyList<CategoryIconChoice> icons)
    {
        _original = definition;
        Parent = parent;
        _defaultName = definition.LabelKey is { } labelKey ? Strings.Get(labelKey) : string.Empty;
        _name = definition.Name ?? _defaultName;
        Extensions = string.Join(", ", definition.Extensions ?? []);
        Filters = filters;
        Filter = filters.First(choice => choice.Filter == definition.Filter);
        Icons = icons.Any(choice => choice.Key == definition.Icon) || definition.Icon is null
            ? icons
            : [.. icons, new CategoryIconChoice(definition.Icon, definition.Icon)];
        Icon = Icons.First(choice => choice.Key == definition.Icon);
        Children = new ObservableCollection<CategoryDraft>(
            (definition.Children ?? []).Select(child => new CategoryDraft(child, this, filters, icons)));
    }

    internal CategoryDraft? Parent { get; }
    public ObservableCollection<CategoryDraft> Children { get; }
    public IReadOnlyList<CategoryFilterChoice> Filters { get; }
    public IReadOnlyList<CategoryIconChoice> Icons { get; }
    public CategoryFilterChoice Filter { get; set; }
    public CategoryIconChoice Icon { get; set; }
    public string? Extensions { get; set; }
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name
    {
        get => _name;
        set
        {
            _name = value ?? string.Empty;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
        }
    }

    internal CategoryDefinition ToDefinition()
    {
        var name = Name.Trim();
        var extensions = (Extensions ?? string.Empty).Split(ExtensionSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(extension => extension.StartsWith('.') ? extension : "." + extension)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        // Keep built-in category names translatable unless the user supplies a custom name.
        return _original with
        {
            Name = _original.LabelKey is not null && (name.Length == 0 || name == _defaultName) ? null : name,
            Filter = Filter.Filter,
            Icon = Icon.Key,
            Extensions = extensions.Length == 0 ? null : extensions,
            Children = Children.Count == 0 ? null : Children.Select(child => child.ToDefinition()).ToArray()
        };
    }
}
