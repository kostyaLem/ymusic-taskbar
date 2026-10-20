using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace YMusicTaskbar;

public sealed class ArtworkConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not byte[] data) return null;
        try
        {
            using var stream = new MemoryStream(data);
            var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 96; bitmap.StreamSource = stream; bitmap.EndInit(); bitmap.Freeze(); return bitmap;
        }
        catch (Exception e) when (e is NotSupportedException or ArgumentException or IOException) { return null; }
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
