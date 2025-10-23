using System;
using System.Windows;

namespace Einsatzueberwachung.Services
{
    /// <summary>
    /// Service für responsive UI-Funktionalität und Display-Anpassungen
    /// </summary>
    public class ResponsiveUIService
    {
        private static ResponsiveUIService _instance;
        public static ResponsiveUIService Instance => _instance ??= new ResponsiveUIService();

        /// <summary>
        /// Event für Änderungen der Display-Größe
        /// </summary>
        public event EventHandler<DisplaySizeChangedEventArgs> DisplaySizeChanged;

        /// <summary>
        /// Bestimmt den Display-Typ basierend auf der Fensterbreite
        /// </summary>
        /// <param name="width">Aktuelle Fensterbreite</param>
        /// <returns>Display-Typ</returns>
        public DisplayType GetDisplayType(double width)
        {
            if (width < 600) return DisplayType.Mobile;
            if (width < 900) return DisplayType.Tablet;
            if (width < 1200) return DisplayType.SmallDesktop;
            if (width < 1600) return DisplayType.Desktop;
            return DisplayType.LargeDesktop;
        }

        /// <summary>
        /// Bestimmt die optimale Spaltenanzahl für UniformGrid basierend auf Fensterbreite
        /// </summary>
        /// <param name="width">Aktuelle Fensterbreite</param>
        /// <returns>Empfohlene Spaltenanzahl</returns>
        public int GetOptimalColumnCount(double width)
        {
            return GetDisplayType(width) switch
            {
                DisplayType.Mobile => 1,
                DisplayType.Tablet => 2,
                DisplayType.SmallDesktop => 3,
                DisplayType.Desktop => 4,
                DisplayType.LargeDesktop => 5,
                _ => 3
            };
        }

        /// <summary>
        /// Bestimmt die optimale Schriftgröße basierend auf Display-Typ
        /// </summary>
        /// <param name="baseSize">Basis-Schriftgröße</param>
        /// <param name="width">Aktuelle Fensterbreite</param>
        /// <returns>Angepasste Schriftgröße</returns>
        public double GetAdaptiveFontSize(double baseSize, double width)
        {
            var displayType = GetDisplayType(width);
            return displayType switch
            {
                DisplayType.Mobile => baseSize * 0.85,
                DisplayType.Tablet => baseSize * 0.9,
                DisplayType.SmallDesktop => baseSize * 0.95,
                DisplayType.Desktop => baseSize,
                DisplayType.LargeDesktop => baseSize * 1.05,
                _ => baseSize
            };
        }

        /// <summary>
        /// Bestimmt das optimale Padding/Margin basierend auf Display-Typ
        /// </summary>
        /// <param name="baseThickness">Basis-Thickness</param>
        /// <param name="width">Aktuelle Fensterbreite</param>
        /// <returns>Angepasste Thickness</returns>
        public Thickness GetAdaptiveThickness(Thickness baseThickness, double width)
        {
            var displayType = GetDisplayType(width);
            var factor = displayType switch
            {
                DisplayType.Mobile => 0.7,
                DisplayType.Tablet => 0.8,
                DisplayType.SmallDesktop => 0.9,
                DisplayType.Desktop => 1.0,
                DisplayType.LargeDesktop => 1.1,
                _ => 1.0
            };

            return new Thickness(
                baseThickness.Left * factor,
                baseThickness.Top * factor,
                baseThickness.Right * factor,
                baseThickness.Bottom * factor
            );
        }

        /// <summary>
        /// Prüft ob ein Layout kompakt dargestellt werden sollte
        /// </summary>
        /// <param name="width">Aktuelle Fensterbreite</param>
        /// <param name="threshold">Schwellenwert für kompaktes Layout (Standard: 800px)</param>
        /// <returns>True wenn kompaktes Layout verwendet werden sollte</returns>
        public bool ShouldUseCompactLayout(double width, double threshold = 800)
        {
            return width < threshold;
        }

        /// <summary>
        /// Prüft ob eine Sidebar ausgeblendet werden sollte
        /// </summary>
        /// <param name="width">Aktuelle Fensterbreite</param>
        /// <param name="threshold">Schwellenwert für Sidebar-Ausblendung (Standard: 1000px)</param>
        /// <returns>True wenn Sidebar ausgeblendet werden sollte</returns>
        public bool ShouldHideSidebar(double width, double threshold = 1000)
        {
            return width < threshold;
        }

        /// <summary>
        /// Bestimmt ob Buttons vertikal angeordnet werden sollten
        /// </summary>
        /// <param name="width">Aktuelle Fensterbreite</param>
        /// <param name="threshold">Schwellenwert für vertikale Anordnung (Standard: 500px)</param>
        /// <returns>True wenn vertikale Anordnung verwendet werden sollte</returns>
        public bool ShouldUseVerticalButtonLayout(double width, double threshold = 500)
        {
            return width < threshold;
        }

        /// <summary>
        /// Löst das DisplaySizeChanged-Event aus
        /// </summary>
        /// <param name="oldSize">Alte Fenstergröße</param>
        /// <param name="newSize">Neue Fenstergröße</param>
        public void NotifyDisplaySizeChanged(Size oldSize, Size newSize)
        {
            var oldType = GetDisplayType(oldSize.Width);
            var newType = GetDisplayType(newSize.Width);

            if (oldType != newType)
            {
                DisplaySizeChanged?.Invoke(this, new DisplaySizeChangedEventArgs
                {
                    OldSize = oldSize,
                    NewSize = newSize,
                    OldDisplayType = oldType,
                    NewDisplayType = newType
                });
            }
        }

        /// <summary>
        /// Ermittelt die primäre Bildschirmgröße
        /// </summary>
        /// <returns>Größe des primären Bildschirms</returns>
        public Size GetPrimaryScreenSize()
        {
            return new Size(
                SystemParameters.PrimaryScreenWidth,
                SystemParameters.PrimaryScreenHeight
            );
        }

        /// <summary>
        /// Bestimmt die optimale Fenstergröße basierend auf Bildschirmgröße
        /// </summary>
        /// <param name="requestedSize">Gewünschte Fenstergröße</param>
        /// <returns>Optimierte Fenstergröße</returns>
        public Size GetOptimalWindowSize(Size requestedSize)
        {
            var screenSize = GetPrimaryScreenSize();
            
            // Maximal 90% der Bildschirmgröße verwenden
            var maxWidth = screenSize.Width * 0.9;
            var maxHeight = screenSize.Height * 0.9;
            
            return new Size(
                Math.Min(requestedSize.Width, maxWidth),
                Math.Min(requestedSize.Height, maxHeight)
            );
        }

        /// <summary>
        /// Prüft ob ein Fenster zu groß für den Bildschirm ist
        /// </summary>
        /// <param name="windowSize">Fenstergröße</param>
        /// <returns>True wenn das Fenster zu groß ist</returns>
        public bool IsWindowTooLarge(Size windowSize)
        {
            var screenSize = GetPrimaryScreenSize();
            return windowSize.Width > screenSize.Width * 0.95 || 
                   windowSize.Height > screenSize.Height * 0.95;
        }
    }

    /// <summary>
    /// Display-Typen für responsive Design
    /// </summary>
    public enum DisplayType
    {
        Mobile,        // < 600px
        Tablet,        // 600-899px
        SmallDesktop,  // 900-1199px
        Desktop,       // 1200-1599px
        LargeDesktop   // >= 1600px
    }

    /// <summary>
    /// Event-Argumente für Display-Größenänderungen
    /// </summary>
    public class DisplaySizeChangedEventArgs : EventArgs
    {
        public Size OldSize { get; set; }
        public Size NewSize { get; set; }
        public DisplayType OldDisplayType { get; set; }
        public DisplayType NewDisplayType { get; set; }
    }
}
