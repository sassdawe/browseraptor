using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace BrowserAptor;

/// <summary>
/// WPF value converter that extracts the first icon from a .exe file
/// and returns it as a <see cref="BitmapSource"/> for display in the UI.
/// </summary>
[ValueConversion(typeof(string), typeof(BitmapSource))]
public class ExeIconConverter : IValueConverter
{
    public static readonly ExeIconConverter Instance = new();

    private static readonly System.Windows.Media.Imaging.BitmapImage FallbackIcon = CreateFallback();

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrEmpty(path))
            return FallbackIcon;

        try
        {
            // Extract the first icon from the executable
            using var icon = Icon.ExtractAssociatedIcon(path);
            if (icon == null) return FallbackIcon;

            using var bitmap = icon.ToBitmap();
            using var stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            stream.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }
        catch
        {
            return FallbackIcon;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static System.Windows.Media.Imaging.BitmapImage CreateFallback()
    {
        // 1×1 transparent pixel as a placeholder
        var img = new System.Windows.Media.Imaging.BitmapImage();
        return img;
    }
}
