using System.Globalization;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;

namespace ScalyTails.Converters;

[ValueConversion(typeof(bool), typeof(PackIconKind))]
public class BoolToIconConverter : IValueConverter
{
    public static readonly BoolToIconConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? PackIconKind.CheckCircle : PackIconKind.CloseCircle;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
