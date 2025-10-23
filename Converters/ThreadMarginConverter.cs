using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Einsatzueberwachung.Converters
{
    /// <summary>
    /// Converter für Thread-Einrückung basierend auf ThreadDepth
    /// </summary>
    public class ThreadMarginConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 4 && 
                values[0] is int threadDepth &&
                values[1] is double top &&
                values[2] is double right &&
                values[3] is double bottom)
            {
                var leftMargin = threadDepth * 20.0; // 20px pro Thread-Ebene
                return new Thickness(leftMargin, top, right, bottom);
            }
            
            return new Thickness(0, 0, 0, 8);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
