using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Einsatzueberwachung.Converters
{
    /// <summary>
    /// Converter für String zu Visibility - zeigt Element nur wenn String nicht leer ist
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("StringToVisibilityConverter is one-way only");
        }

        /// <summary>
        /// Statische Hilfsmethode für direkten Zugriff
        /// </summary>
        public static Visibility Convert(object value)
        {
            if (value is string text)
            {
                return string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
            }
            
            return Visibility.Collapsed;
        }
    }
}
