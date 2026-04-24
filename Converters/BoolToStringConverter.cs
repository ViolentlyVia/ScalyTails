using System.Globalization;
using System.Windows.Data;

namespace ScalyTails.Converters;

[ValueConversion(typeof(bool), typeof(string))]
public class BoolToStringConverter : IValueConverter
{
    public string TrueValue { get; set; } = "Yes";
    public string FalseValue { get; set; } = "No";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? TrueValue : FalseValue;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value?.ToString() == TrueValue;
}
