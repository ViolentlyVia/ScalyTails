using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScalyTails.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Handles three binding shapes used in XAML:
        //   bool   — direct flag (most common)
        //   int    — string.Length bindings like {Binding SomeText.Length}
        //   string — direct string comparison for emptiness
        var visible = value switch
        {
            bool b   => b,
            int  n   => n != 0,
            string s => s.Length != 0,
            _        => value is not null,
        };
        if (Invert) visible = !visible;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Visible;
}
