using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SecurityLendingTool.Converters;

/// <summary>
/// Turns a 0-100 progress value into a row background: light green up to the progress point, transparent after it.
/// </summary>
public sealed class ProgressToBackgroundConverter : IValueConverter
{
    private static readonly Color FillColor = Color.FromRgb(0xC8, 0xF0, 0xC8);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double progress = value is IConvertible convertible ? convertible.ToDouble(CultureInfo.InvariantCulture) : 0;
        double offset = Math.Clamp(progress / 100.0, 0.0, 1.0);
        if (offset <= 0)
        {
            return Brushes.Transparent;
        }

        LinearGradientBrush brush = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(1, 0)
        };
        brush.GradientStops.Add(new GradientStop(FillColor, 0));
        brush.GradientStops.Add(new GradientStop(FillColor, offset));
        brush.GradientStops.Add(new GradientStop(Colors.Transparent, offset));
        brush.GradientStops.Add(new GradientStop(Colors.Transparent, 1));
        brush.Freeze();
        return brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
