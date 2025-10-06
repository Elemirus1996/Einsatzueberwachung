using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
    /// Konvertiert String zu Visibility für Settings Kategorien
    /// </summary>
    public class SettingsCategoryVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.ToString() == parameter?.ToString())
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Addiert einen Wert zu einem Integer
    /// </summary>
    public class AddValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue && int.TryParse(parameter?.ToString(), out int addValue))
            {
                return intValue + addValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Konvertiert Boolean zu Ja/Nein String
    /// </summary>
    public class BooleanToYesNoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? "Ja" : "Nein";
            return "Unbekannt";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString()?.ToLowerInvariant() == "ja";
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

    /// <summary>
    /// Converter-Sammlung für WPF Data-Binding
    /// </summary>
    
    /// <summary>
    /// Konvertiert byte[] zu BitmapImage für QR-Code-Anzeige
    /// </summary>
    public class ByteArrayToBitmapImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is byte[] byteArray && byteArray.Length > 0)
            {
                try
                {
                    using var stream = new MemoryStream(byteArray);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Konvertiert Enum zu String für Anzeige
    /// </summary>
    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum enumValue)
            {
                return enumValue.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Konvertiert TimeSpan zu lesbarem String-Format
    /// </summary>
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
            {
                return $"{(int)timeSpan.TotalHours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
            }
            return "00:00:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Multi-Converter für komplexe Boolean-Operationen
    /// </summary>
    public class MultiBooleanConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return false;

            var operation = parameter?.ToString()?.ToUpper() ?? "AND";
            
            switch (operation)
            {
                case "OR":
                    return values.OfType<bool>().Any(b => b);
                case "AND":
                default:
                    return values.OfType<bool>().All(b => b);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
