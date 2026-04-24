using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ScalyTails.Converters;

[ValueConversion(typeof(bool), typeof(Brush))]
public class BoolToColorConverter : IValueConverter
{
    public Brush TrueColor { get; set; } = Brushes.LimeGreen;
    public Brush FalseColor { get; set; } = Brushes.Gray;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? TrueColor : FalseColor;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
