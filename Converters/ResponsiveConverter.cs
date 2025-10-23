using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Einsatzueberwachung.Converters
{
    /// <summary>
    /// Converter für responsive UI-Funktionalität basierend auf Fensterbreite
    /// </summary>
    public class WidthToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width && parameter is string thresholdString)
            {
                if (double.TryParse(thresholdString, out double threshold))
                {
                    return width < threshold;
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter für adaptive Sichtbarkeit basierend auf Fensterbreite
    /// </summary>
    public class WidthToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width && parameter is string thresholdString)
            {
                if (double.TryParse(thresholdString, out double threshold))
                {
                    return width < threshold ? Visibility.Collapsed : Visibility.Visible;
                }
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter für adaptive Spaltenanzahl in UniformGrid basierend auf Fensterbreite
    /// </summary>
    public class WidthToColumnsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width && parameter is string thresholdString)
            {
                if (double.TryParse(thresholdString, out double threshold))
                {
                    // Bestimme Spaltenanzahl basierend auf Fensterbreite
                    if (width >= 1600) return 5;  // Große Bildschirme
                    if (width >= 1400) return 4;  // Mittlere Bildschirme
                    if (width >= 1200) return 3;  // Kleinere Bildschirme
                    if (width >= 900) return 2;   // Sehr kleine Bildschirme
                    return 1;                      // Mobile Größen
                }
            }
            return 5; // Standard-Fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter für adaptive Schriftgrößen basierend auf verfügbarem Platz
    /// </summary>
    public class WidthToFontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                // Standard-Schriftgrößen basierend auf Fensterbreite
                if (width >= 1400) return 16.0;      // Große Bildschirme
                if (width >= 1200) return 14.0;      // Mittlere Bildschirme
                if (width >= 1000) return 13.0;      // Kleinere Bildschirme
                return 12.0;                          // Sehr kleine Bildschirme
            }
            return 14.0; // Standard-Fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter für adaptive Padding/Margin basierend auf verfügbarem Platz
    /// </summary>
    public class WidthToThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                // Adaptive Abstände basierend auf Fensterbreite
                if (width >= 1400) return new Thickness(20, 12, 20, 12);  // Große Bildschirme
                if (width >= 1200) return new Thickness(16, 10, 16, 10);  // Mittlere Bildschirme
                if (width >= 1000) return new Thickness(12, 8, 12, 8);   // Kleinere Bildschirme
                return new Thickness(8, 6, 8, 6);                       // Sehr kleine Bildschirme
            }
            return new Thickness(16, 10, 16, 10); // Standard-Fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Multi-Converter für adaptive Layouts mit mehreren Parametern
    /// </summary>
    public class ResponsiveLayoutConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is double width && values[1] is string layoutMode)
            {
                switch (layoutMode.ToLower())
                {
                    case "compact":
                        return width < 1400;
                    case "mobile":
                        return width < 800;
                    case "tablet":
                        return width >= 800 && width < 1200;
                    case "desktop":
                        return width >= 1200;
                    default:
                        return false;
                }
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
