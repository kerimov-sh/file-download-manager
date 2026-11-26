using System.Globalization;

namespace FileDownloadManager.UI.Appearance;

public sealed record LanguageChoice(string Code, string Name)
{
    internal static LanguageChoice? FromCode(string code)
    {
        try
        {
            return new LanguageChoice(code, CultureInfo.GetCultureInfo(code).DisplayName);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }
}
