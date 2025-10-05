using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// Konvertiert boolean-Werte zu Visibility (invertiert)
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public static readonly InverseBooleanConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return true;
        }
    }

    /// <summary>
    /// Konvertiert boolean-Werte zu Visibility mit Invert-Option
    /// Parameter "Inverted" invertiert das Ergebnis
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public static readonly BooleanToVisibilityConverter Instance = new();
        public static readonly BooleanToVisibilityConverter CollapsedWhenTrue = new() { InvertResult = true };

        public bool InvertResult { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;

            // Check für "Inverted" Parameter oder InvertResult Property
            if (parameter?.ToString()?.Equals("Inverted", StringComparison.OrdinalIgnoreCase) == true || InvertResult)
            {
                boolValue = !boolValue;
            }

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = value is Visibility v && v == Visibility.Visible;

            // Check für "Inverted" Parameter oder InvertResult Property
            if (parameter?.ToString()?.Equals("Inverted", StringComparison.OrdinalIgnoreCase) == true || InvertResult)
            {
                isVisible = !isVisible;
            }

            return isVisible;
        }
    }

    /// <summary>
    /// Konvertiert String zu Visibility - sichtbar wenn String nicht leer
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public static readonly StringToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return string.IsNullOrWhiteSpace(stringValue) ? Visibility.Collapsed : Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not needed for one-way binding
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Konvertiert Hex-Color-String zu SolidColorBrush
    /// </summary>
    public class ColorBrushConverter : IValueConverter
    {
        public static readonly ColorBrushConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hexColor && !string.IsNullOrWhiteSpace(hexColor))
            {
                try
                {
                    // Ensure hex color starts with #
                    if (!hexColor.StartsWith("#"))
                        hexColor = "#" + hexColor;

                    var color = (Color)ColorConverter.ConvertFromString(hexColor);
                    return new SolidColorBrush(color);
                }
                catch
                {
                    // Fallback to orange if conversion fails
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57C00"));
                }
            }

            // Default orange brush
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57C00"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color.ToString();
            }
            return "#F57C00";
        }
    }
}
