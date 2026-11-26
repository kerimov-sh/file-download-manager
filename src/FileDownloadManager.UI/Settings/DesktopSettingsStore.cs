using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using FileDownloadManager.UI.Resources.Localization;

namespace FileDownloadManager.UI.Settings;

internal sealed class DesktopSettingsStore
{
    private const string ApplicationDirectoryName = "FileDownloadManager";
    private const string SettingsDirectoryVariable = "FILEDOWNLOADMANAGER_CONFIG_DIR";
    private const string SettingsFileName = "desktop.json";
    private const string DefaultsPath = "Settings/Defaults.json";
    private const string PackagedLanguagesPath = "Resources/Localization";
    private const string UserLanguagesDirectoryName = "Languages";
    private const string LanguageFilePattern = "*.json";
    private const string LanguageFileExtension = ".json";
    private const string English = "en";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private readonly string _settingsPath;
    private readonly string _packagedLanguages = Path.Combine(AppContext.BaseDirectory, PackagedLanguagesPath);

    public DesktopSettingsStore(string directory)
    {
        _settingsPath = Path.Combine(directory, SettingsFileName);
        LanguageDirectory = Path.Combine(directory, UserLanguagesDirectoryName);
    }

    public string LanguageDirectory { get; }
    public string? LoadError { get; private set; }

    public static DesktopSettingsStore ForCurrentUser()
    {
        var configured = Environment.GetEnvironmentVariable(SettingsDirectoryVariable);
        var directory = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationDirectoryName)
            : Path.GetFullPath(configured);
        return new DesktopSettingsStore(directory);
    }

    public DesktopPreferences Load()
    {
        var defaults = Read<DesktopPreferences>(Path.Combine(AppContext.BaseDirectory, DefaultsPath));
        var preferences = defaults;
        try
        {
            if (File.Exists(_settingsPath))
                preferences = Read<DesktopPreferences>(_settingsPath);
            ValidateCategories(preferences.Categories);
            LoadLanguage(preferences.Language);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException
                                      or ArgumentException or FormatException)
        {
            LoadError = error.Message;
            preferences = defaults;
            LoadLanguage(defaults.Language, useOverrides: false);
        }

        return preferences;
    }

    public IReadOnlyList<string> Languages() => PackNames(_packagedLanguages)
        .Concat(PackNames(LanguageDirectory))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public static string FormatCategories(CategoryDefinition[] categories) =>
        JsonSerializer.Serialize(categories, JsonOptions);

    public static CategoryDefinition[] ParseCategories(string text)
    {
        var categories = JsonSerializer.Deserialize<CategoryDefinition[]>(text, JsonOptions)
            ?? throw new JsonException(Strings.InvalidCategories);
        ValidateCategories(categories);
        return categories;
    }

    public void Save(DesktopPreferences preferences)
    {
        ValidateCategories(preferences.Categories);
        _ = ReadLanguage(preferences.Language, useOverrides: true);

        var directory = Path.GetDirectoryName(_settingsPath)!;
        Directory.CreateDirectory(LanguageDirectory);
        var temporaryPath = Path.Combine(directory, Path.GetRandomFileName());
        // Replace the saved file only after the new settings have been serialized and written.
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(preferences, JsonOptions));
            File.Move(temporaryPath, _settingsPath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    private void LoadLanguage(string language, bool useOverrides = true)
    {
        var fallback = ReadLanguage(English, useOverrides: false);
        var selected = ReadLanguage(language, useOverrides);
        Strings.Load(fallback, selected);
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(language);
        CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentUICulture;
    }

    private Dictionary<string, string> ReadLanguage(string language, bool useOverrides)
    {
        var culture = CultureInfo.GetCultureInfo(language);
        if (string.IsNullOrEmpty(culture.Name))
            throw new CultureNotFoundException(nameof(language));

        var fileName = culture.Name + LanguageFileExtension;
        var overridePath = Path.Combine(LanguageDirectory, fileName);
        var pack = Read<Dictionary<string, string>>(useOverrides && File.Exists(overridePath)
            ? overridePath : Path.Combine(_packagedLanguages, fileName));

        // A partial pack falls back to English. Reject malformed format strings before the UI uses them.
        var fallback = Read<Dictionary<string, string>>(Path.Combine(_packagedLanguages, English + LanguageFileExtension));
        foreach (var (key, value) in pack)
        {
            if (value is null)
                throw new JsonException(key);
            if (fallback.TryGetValue(key, out var original) && original.Contains('{'))
                ValidateFormat(original, value);
        }

        return pack;
    }

    private static void ValidateFormat(string original, string translation)
    {
        var source = System.Text.CompositeFormat.Parse(original);
        var target = System.Text.CompositeFormat.Parse(translation);
        if (target.MinimumArgumentCount > source.MinimumArgumentCount)
            throw new FormatException(translation);
    }

    private static IEnumerable<string> PackNames(string directory) =>
        Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, LanguageFilePattern).Select(Path.GetFileNameWithoutExtension).OfType<string>()
            : [];

    private static T Read<T>(string path) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions) ?? throw new JsonException(path);

    private static void ValidateCategories(CategoryDefinition[] categories)
    {
        if (categories is null)
            throw new JsonException(Strings.InvalidCategories);
        foreach (var category in categories)
        {
            if (category is null || (string.IsNullOrWhiteSpace(category.Name) && string.IsNullOrWhiteSpace(category.LabelKey)))
                throw new JsonException(Strings.InvalidCategory);
            foreach (var extension in category.Extensions ?? [])
            {
                if (string.IsNullOrWhiteSpace(extension) || !extension.StartsWith('.') ||
                    extension[1..].Split('.').Any(part => part.Length == 0 ||
                        !part.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '+' or '-')))
                    throw new JsonException(string.Format(CultureInfo.CurrentCulture, Strings.InvalidExtension, extension));
            }
            if (category.Children is not null)
                ValidateCategories(category.Children);
        }
    }
}
