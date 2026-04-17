using System.Globalization;
using System.Windows.Data;

namespace BrowserAptor;

/// <summary>
/// Returns <see cref="TrueValue"/> when the bound boolean is <c>true</c>,
/// otherwise returns <see cref="FalseValue"/>.
/// </summary>
public class BoolToStringConverter : IValueConverter
{
    public string TrueValue  { get; set; } = string.Empty;
    public string FalseValue { get; set; } = string.Empty;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? TrueValue : FalseValue;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
