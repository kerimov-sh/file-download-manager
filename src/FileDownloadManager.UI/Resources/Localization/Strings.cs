namespace FileDownloadManager.UI.Resources.Localization;

public static class Strings
{
    private static IReadOnlyDictionary<string, string> _fallback = new Dictionary<string, string>();
    private static IReadOnlyDictionary<string, string> _translations = new Dictionary<string, string>();

    internal static void Load(IReadOnlyDictionary<string, string> fallback, IReadOnlyDictionary<string, string> translations)
    {
        _fallback = fallback;
        _translations = translations;
    }

    internal static string Get(string key) =>
        _translations.TryGetValue(key, out var text) ? text :
        _fallback.TryGetValue(key, out text) ? text : key;

    public static string Title => Get(nameof(Title));
    public static string Heading => Get(nameof(Heading));
    public static string Url => Get(nameof(Url));
    public static string Destination => Get(nameof(Destination));
    public static string Browse => Get(nameof(Browse));
    public static string Start => Get(nameof(Start));
    public static string Cancel => Get(nameof(Cancel));
    public static string Exit => Get(nameof(Exit));
    public static string InvalidUrl => Get(nameof(InvalidUrl));
    public static string InvalidDestination => Get(nameof(InvalidDestination));
    public static string AlreadyExists => Get(nameof(AlreadyExists));
    public static string Downloading => Get(nameof(Downloading));
    public static string Completed => Get(nameof(Completed));
    public static string Cancelled => Get(nameof(Cancelled));
    public static string Failure => Get(nameof(Failure));
    public static string PickerTitle => Get(nameof(PickerTitle));
    public static string LocalOnly => Get(nameof(LocalOnly));
    public static string DestinationHint => Get(nameof(DestinationHint));
    public static string UrlHint => Get(nameof(UrlHint));
    public static string AddUrl => Get(nameof(AddUrl));
    public static string StopTransfer => Get(nameof(StopTransfer));
    public static string RemoveEntry => Get(nameof(RemoveEntry));
    public static string Tasks => Get(nameof(Tasks));
    public static string Downloads => Get(nameof(Downloads));
    public static string Categories => Get(nameof(Categories));
    public static string AllDownloads => Get(nameof(AllDownloads));
    public static string ActiveTransfers => Get(nameof(ActiveTransfers));
    public static string FinishedTransfers => Get(nameof(FinishedTransfers));
    public static string IncompleteTransfers => Get(nameof(IncompleteTransfers));
    public static string FileName => Get(nameof(FileName));
    public static string FileSize => Get(nameof(FileSize));
    public static string TransferStatus => Get(nameof(TransferStatus));
    public static string Source => Get(nameof(Source));
    public static string Dismiss => Get(nameof(Dismiss));
    public static string Ready => Get(nameof(Ready));
    public static string LibraryCount => Get(nameof(LibraryCount));
    public static string Failed => Get(nameof(Failed));
    public static string Stopped => Get(nameof(Stopped));
    public static string ByteSize => Get(nameof(ByteSize));
    public static string View => Get(nameof(View));
    public static string SystemIcons => Get(nameof(SystemIcons));
    public static string BundledIcons => Get(nameof(BundledIcons));
    public static string ShowDownloads => Get(nameof(ShowDownloads));
    public static string Compressed => Get(nameof(Compressed));
    public static string Documents => Get(nameof(Documents));
    public static string Music => Get(nameof(Music));
    public static string Programs => Get(nameof(Programs));
    public static string Video => Get(nameof(Video));
    public static string Retry => Get(nameof(Retry));
    public static string StopAll => Get(nameof(StopAll));
    public static string ClearFinished => Get(nameof(ClearFinished));
    public static string Options => Get(nameof(Options));
    public static string Description => Get(nameof(Description));
    public static string TimeLeft => Get(nameof(TimeLeft));
    public static string Rate => Get(nameof(Rate));
    public static string LastTry => Get(nameof(LastTry));
    public static string PerSecond => Get(nameof(PerSecond));
    public static string Kilobytes => Get(nameof(Kilobytes));
    public static string Megabytes => Get(nameof(Megabytes));
    public static string Gigabytes => Get(nameof(Gigabytes));
    public static string CancelDialog => Get(nameof(CancelDialog));
    public static string Confirm => Get(nameof(Confirm));
    public static string IconStyle => Get(nameof(IconStyle));
    public static string FileType => Get(nameof(FileType));
    public static string AddedDate => Get(nameof(AddedDate));
    public static string GroupBy => Get(nameof(GroupBy));
    public static string NoGrouping => Get(nameof(NoGrouping));
    public static string NoExtension => Get(nameof(NoExtension));
    public static string SortAscending => Get(nameof(SortAscending));
    public static string SortDescending => Get(nameof(SortDescending));
    public static string AppearanceSettings => Get(nameof(AppearanceSettings));
    public static string CategoriesSettings => Get(nameof(CategoriesSettings));
    public static string Theme => Get(nameof(Theme));
    public static string SystemTheme => Get(nameof(SystemTheme));
    public static string DarkTheme => Get(nameof(DarkTheme));
    public static string LightTheme => Get(nameof(LightTheme));
    public static string Language => Get(nameof(Language));
    public static string LanguageFiles => Get(nameof(LanguageFiles));
    public static string LanguageRestart => Get(nameof(LanguageRestart));
    public static string CategoryEditorHelp => Get(nameof(CategoryEditorHelp));
    public static string SettingsFailure => Get(nameof(SettingsFailure));
    public static string SettingsLoadFailure => Get(nameof(SettingsLoadFailure));
    public static string InvalidCategory => Get(nameof(InvalidCategory));
    public static string InvalidExtension => Get(nameof(InvalidExtension));
    public static string InvalidCategories => Get(nameof(InvalidCategories));
    public static string SaveSettings => Get(nameof(SaveSettings));
    public static string CategoryName => Get(nameof(CategoryName));
    public static string CategoryExtensions => Get(nameof(CategoryExtensions));
    public static string CategoryExtensionsHint => Get(nameof(CategoryExtensionsHint));
    public static string CategoryFilter => Get(nameof(CategoryFilter));
    public static string CategoryIcon => Get(nameof(CategoryIcon));
    public static string DefaultFolder => Get(nameof(DefaultFolder));
    public static string InheritFilter => Get(nameof(InheritFilter));
    public static string AddCategory => Get(nameof(AddCategory));
    public static string AddSubcategory => Get(nameof(AddSubcategory));
    public static string RemoveCategory => Get(nameof(RemoveCategory));
    public static string MoveCategoryUp => Get(nameof(MoveCategoryUp));
    public static string MoveCategoryDown => Get(nameof(MoveCategoryDown));
    public static string NewCategory => Get(nameof(NewCategory));
}
