using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BrowserAptor;

/// <summary>
/// Converts an <c>#RRGGBB</c> hex colour string to a <see cref="SolidColorBrush"/>.
/// Returns <see cref="FallbackBrush"/> when the value is <c>null</c> or cannot be parsed.
/// </summary>
[ValueConversion(typeof(string), typeof(SolidColorBrush))]
public class HexColorToBrushConverter : IValueConverter
{
    public static readonly HexColorToBrushConverter Instance = new();

    /// <summary>Brush returned for <c>null</c> / unrecognised input.</summary>
    public SolidColorBrush FallbackBrush { get; set; } =
        new(Color.FromRgb(0x2A, 0x2A, 0x3C)); // matches SurfaceBrush

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && hex.StartsWith('#') && hex.Length == 7)
        {
            try
            {
                byte r = System.Convert.ToByte(hex[1..3], 16);
                byte g = System.Convert.ToByte(hex[3..5], 16);
                byte b = System.Convert.ToByte(hex[5..7], 16);
                var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
                brush.Freeze();
                return brush;
            }
            catch { /* fall through */ }
        }

        return FallbackBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
