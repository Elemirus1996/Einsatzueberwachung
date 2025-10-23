using System;
using System.ComponentModel;
using System.Windows;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// Base Window mit automatischer Theme-Integration v5.0
    /// Alle Fenster sollten von dieser Klasse erben für einheitliches Theme-Management
    /// Jetzt vollständig integriert mit UnifiedThemeManager für saubere Theme-Architektur
    /// </summary>
    public abstract class BaseThemeWindow : Window, IThemeConsumer, IDisposable
    {
        private bool _disposed = false;

        protected BaseThemeWindow()
        {
            try
            {
                // Auto-registrierung für Theme-Updates
                UnifiedThemeManager.Instance.RegisterThemeConsumer(this);
                
                LoggingService.Instance.LogInfo($"{GetType().Name} initialized with UnifiedThemeManager v5.0");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error initializing BaseThemeWindow in {GetType().Name}", ex);
            }
        }

        #region IThemeConsumer Implementation

        /// <summary>
        /// Wird automatisch vom UnifiedThemeManager aufgerufen bei Theme-Änderungen
        /// </summary>
        /// <param name="isDarkMode">Ist Dark Mode aktiv?</param>
        public virtual void OnThemeChanged(bool isDarkMode)
        {
            try
            {
                // Wird auf UI-Thread ausgeführt falls nötig
                if (Dispatcher.CheckAccess())
                {
                    ApplyThemeToWindow(isDarkMode);
                }
                else
                {
                    Dispatcher.Invoke(() => ApplyThemeToWindow(isDarkMode));
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error applying theme to {GetType().Name}", ex);
            }
        }

        /// <summary>
        /// Überschreibbar für fensterspezifische Theme-Anwendung
        /// </summary>
        /// <param name="isDarkMode">Ist Dark Mode aktiv?</param>
        protected virtual void ApplyThemeToWindow(bool isDarkMode)
        {
            // Base implementation - kann von abgeleiteten Klassen überschrieben werden
            LoggingService.Instance.LogInfo($"Theme applied to {GetType().Name}: {(isDarkMode ? "Dark" : "Light")} mode");
        }

        #endregion

        #region Theme Helper Methods

        /// <summary>
        /// Holt eine Theme-Farbe vom UnifiedThemeManager
        /// </summary>
        /// <param name="colorKey">Farb-Schlüssel (z.B. "Primary", "Surface")</param>
        /// <returns>SolidColorBrush für die aktuelle Theme-Farbe</returns>
        protected System.Windows.Media.SolidColorBrush GetThemeColor(string colorKey)
        {
            try
            {
                return UnifiedThemeManager.Instance.GetThemeColor(colorKey);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error getting theme color '{colorKey}' in {GetType().Name}", ex);
                // Fallback auf Orange
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
            }
        }

        /// <summary>
        /// Prüft ob aktuell Dark Mode aktiv ist
        /// </summary>
        protected bool IsDarkMode => UnifiedThemeManager.Instance.IsDarkMode;

        /// <summary>
        /// Prüft ob Auto-Mode aktiv ist
        /// </summary>
        protected bool IsAutoMode => UnifiedThemeManager.Instance.IsAutoMode;

        /// <summary>
        /// Holt den aktuellen Theme-Status als String
        /// </summary>
        protected string CurrentThemeStatus => UnifiedThemeManager.Instance.CurrentThemeStatus;

        #endregion

        #region Legacy ThemeService Compatibility

        /// <summary>
        /// Legacy-Methode für Rückwärtskompatibilität
        /// Leitet Theme-Initialisierung an UnifiedThemeManager weiter
        /// </summary>
        protected void InitializeThemeSupport()
        {
            try
            {
                // Auto-registrierung erfolgt bereits im Constructor
                // Theme sofort anwenden
                OnThemeChanged(UnifiedThemeManager.Instance.IsDarkMode);
                
                LoggingService.Instance.LogInfo($"Legacy theme support initialized for {GetType().Name}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error initializing legacy theme support for {GetType().Name}", ex);
            }
        }

        #endregion

        #region Window Lifecycle

        protected override void OnSourceInitialized(EventArgs e)
        {
            try
            {
                base.OnSourceInitialized(e);
                
                // Theme sofort anwenden nach Window-Initialisierung
                OnThemeChanged(UnifiedThemeManager.Instance.IsDarkMode);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error in OnSourceInitialized for {GetType().Name}", ex);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            try
            {
                // Cleanup in abgeleiteten Klassen
                OnWindowClosing(e);
                
                base.OnClosing(e);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error in OnClosing for {GetType().Name}", ex);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Cleanup in abgeleiteten Klassen
                OnWindowClosed(e);
                
                // Auto-Dispose
                Dispose();
                
                base.OnClosed(e);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error in OnClosed for {GetType().Name}", ex);
            }
        }

        /// <summary>
        /// Wird beim Schließen des Fensters aufgerufen - überschreibbar für Cleanup
        /// </summary>
        protected virtual void OnWindowClosing(CancelEventArgs e)
        {
            // Override in derived classes for custom closing logic
        }

        /// <summary>
        /// Wird nach dem Schließen des Fensters aufgerufen - überschreibbar für Cleanup
        /// </summary>
        protected virtual void OnWindowClosed(EventArgs e)
        {
            // Override in derived classes for custom cleanup
        }

        #endregion

        #region IDisposable Implementation

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        // Unregister from theme updates (falls der UnifiedThemeManager das unterstützt)
                        // Note: Aktuell ist kein Unregister implementiert, aber das könnte erweitert werden
                        
                        LoggingService.Instance.LogInfo($"{GetType().Name} disposed");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogError($"Error disposing {GetType().Name}", ex);
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Static Helper Methods

        /// <summary>
        /// Statische Hilfsmethode um Theme-Farben zu bekommen (für statische Kontexte)
        /// </summary>
        /// <param name="colorKey">Farb-Schlüssel</param>
        /// <returns>SolidColorBrush</returns>
        public static System.Windows.Media.SolidColorBrush GetStaticThemeColor(string colorKey)
        {
            try
            {
                return UnifiedThemeManager.Instance.GetThemeColor(colorKey);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error getting static theme color '{colorKey}'", ex);
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
            }
        }

        /// <summary>
        /// Theme manuell umschalten (für Debug/Test-Zwecke)
        /// </summary>
        public static void ToggleTheme()
        {
            try
            {
                UnifiedThemeManager.Instance.ToggleTheme();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error toggling theme", ex);
            }
        }

        #endregion
    }
}
