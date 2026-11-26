using System.ComponentModel;
using System.Globalization;
using FileDownloadManager.TransferModel;
using FileDownloadManager.UI.Resources.Localization;

namespace FileDownloadManager.UI;

public sealed class TransferMetrics : INotifyPropertyChanged
{
    private const double BytesPerKilobyte = 1024;
    private const double BytesPerMegabyte = BytesPerKilobyte * BytesPerKilobyte;
    private const double BytesPerGigabyte = BytesPerMegabyte * BytesPerKilobyte;
    private TransferProgress _sample;
    public event PropertyChangedEventHandler? PropertyChanged;

    public long? TotalBytes => _sample.TotalBytes;
    public double BytesPerSecond => _sample.Elapsed.TotalSeconds > 0
        ? _sample.BytesWritten / _sample.Elapsed.TotalSeconds : 0;
    public string Rate => BytesPerSecond > 0 ? string.Format(Strings.PerSecond, FormatSize(BytesPerSecond)) : string.Empty;
    public string Size => _sample.TotalBytes is long total ? FormatSize(total) : string.Empty;
    public string Status => _sample.TotalBytes is > 0
        ? ((double)_sample.BytesWritten / _sample.TotalBytes.Value).ToString("P1", CultureInfo.CurrentCulture)
        : Strings.Downloading;
    public string TimeLeft => Remaining?.ToString("g") ?? string.Empty;
    public TimeSpan? Remaining
    {
        get
        {
            if (_sample.TotalBytes is not long total || BytesPerSecond <= 0)
                return null;
            var seconds = Math.Max(0, total - _sample.BytesWritten) / BytesPerSecond;
            return seconds < TimeSpan.MaxValue.TotalSeconds ? TimeSpan.FromSeconds(seconds) : null;
        }
    }

    public void Update(TransferProgress sample)
    {
        _sample = sample;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }

    public static string FormatSize(double bytes)
    {
        if (bytes >= BytesPerGigabyte) return string.Format(Strings.Gigabytes, bytes / BytesPerGigabyte);
        if (bytes >= BytesPerMegabyte) return string.Format(Strings.Megabytes, bytes / BytesPerMegabyte);
        if (bytes >= BytesPerKilobyte) return string.Format(Strings.Kilobytes, bytes / BytesPerKilobyte);
        return string.Format(Strings.ByteSize, bytes);
    }
}
