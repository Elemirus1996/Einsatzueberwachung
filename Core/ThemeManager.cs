using System;
using System.ComponentModel;
using System.Windows;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Core
{
    /// <summary>
    /// 🎨 THEME MANAGER v4.0
    /// Zentrale Verwaltung für das globale Design-System
    /// Automatische Integration zwischen App.xaml und UnifiedThemeManager
    /// </summary>
    public static class ThemeManager
    {
        #region Private Fields

        private static bool _isInitialized = false;
        private static Application? _application;

        #endregion

        #region Public Properties

        /// <summary>
        /// Aktueller UnifiedThemeManager Instance
        /// </summary>
        public static UnifiedThemeManager UnifiedThemeManager => UnifiedThemeManager.Instance;

        /// <summary>
        /// Gibt an ob das Theme-System initialisiert wurde
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Aktuelle Anwendung
        /// </summary>
        public static Application? Application => _application;

        #endregion

        #region Events

        /// <summary>
        /// Event wird ausgelöst wenn das Theme-System initialisiert wurde
        /// </summary>
        public static event EventHandler? ThemeSystemInitialized;

        /// <summary>
        /// Event wird ausgelöst wenn ein Theme-Fehler auftritt
        /// </summary>
        public static event EventHandler<ThemeErrorEventArgs>? ThemeError;

        #endregion

        #region Public Methods

        /// <summary>
        /// Theme-System initialisieren
        /// </summary>
        public static void Initialize(Application application)
        {
            try
            {
                if (_isInitialized)
                {
                    LoggingService.Instance.LogWarning("🎨 ThemeManager already initialized - skipping");
                    return;
                }

                _application = application ?? throw new ArgumentNullException(nameof(application));
                
                LoggingService.Instance.LogInfo("🎨 ===============================================");
                LoggingService.Instance.LogInfo("🎨 THEME MANAGER v4.0 INITIALIZING");
                LoggingService.Instance.LogInfo("🎨 Global Design System with Auto Time Switching");
                LoggingService.Instance.LogInfo("🎨 ===============================================");

                // Design-System ResourceDictionary sicherstellen
                EnsureDesignSystemLoaded();

                // UnifiedThemeManager Event-Handler registrieren
                RegisterUnifiedThemeManagerEvents();

                // Initiales Theme anwenden
                ApplyCurrentTheme();

                _isInitialized = true;
                
                LoggingService.Instance.LogInfo("✅ ThemeManager v4.0 initialized successfully");
                LoggingService.Instance.LogInfo($"🎨 Current theme: {UnifiedThemeManager.CurrentThemeStatus}");
                
                ThemeSystemInitialized?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("🚨 Failed to initialize ThemeManager", ex);
                ThemeError?.Invoke(null, new ThemeErrorEventArgs("Initialization failed", ex));
                throw;
            }
        }

        /// <summary>
        /// Design-System ResourceDictionary neu laden
        /// </summary>
        public static void ReloadDesignSystem()
        {
            try
            {
                if (_application == null)
                {
                    LoggingService.Instance.LogWarning("⚠️ Cannot reload design system - application not set");
                    return;
                }

                LoggingService.Instance.LogInfo("🔄 Reloading design system...");

                // Alle ResourceDictionaries durchsuchen
                var designSystemUri = new Uri("pack://application:,,,/Resources/DesignSystem.xaml");
                ResourceDictionary? designSystemDict = null;

                foreach (ResourceDictionary dict in _application.Resources.MergedDictionaries)
                {
                    if (dict.Source == designSystemUri)
                    {
                        designSystemDict = dict;
                        break;
                    }
                }

                if (designSystemDict != null)
                {
                    // Dictionary entfernen und neu hinzufügen
                    _application.Resources.MergedDictionaries.Remove(designSystemDict);
                    
                    var newDesignSystem = new ResourceDictionary { Source = designSystemUri };
                    _application.Resources.MergedDictionaries.Add(newDesignSystem);
                    
                    LoggingService.Instance.LogInfo("✅ Design system reloaded successfully");
                }
                else
                {
                    LoggingService.Instance.LogWarning("⚠️ Design system ResourceDictionary not found");
                }

                // Theme neu anwenden
                ApplyCurrentTheme();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("🚨 Error reloading design system", ex);
                ThemeError?.Invoke(null, new ThemeErrorEventArgs("Design system reload failed", ex));
            }
        }

        /// <summary>
        /// Aktuelles Theme manuell anwenden
        /// </summary>
        public static void RefreshCurrentTheme()
        {
            try
            {
                LoggingService.Instance.LogInfo("🔄 Manual theme refresh requested");
                // UnifiedThemeManager handles theme refresh internally when properties change
                // Force an immediate theme check if in auto mode
                if (UnifiedThemeManager.IsAutoMode)
                {
                    UnifiedThemeManager.ForceImmediateThemeCheck();
                }
                else
                {
                    // Apply current theme again
                    ApplyCurrentTheme();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("🚨 Error refreshing theme", ex);
                ThemeError?.Invoke(null, new ThemeErrorEventArgs("Theme refresh failed", ex));
            }
        }

        /// <summary>
        /// Theme-System für Debugging zurücksetzen
        /// </summary>
        public static void Reset()
        {
            try
            {
                LoggingService.Instance.LogInfo("🔄 Resetting theme system...");

                // UnifiedThemeManager zurücksetzen
                UnifiedThemeManager.ResetToDefaults();

                // Design-System neu laden
                ReloadDesignSystem();

                LoggingService.Instance.LogInfo("✅ Theme system reset completed");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("🚨 Error resetting theme system", ex);
                ThemeError?.Invoke(null, new ThemeErrorEventArgs("Theme system reset failed", ex));
            }
        }

        /// <summary>
        /// Debug-Informationen über das aktuelle Theme-System
        /// </summary>
        public static ThemeDebugInfo GetDebugInfo()
        {
            try
            {
                // Create debug info from UnifiedThemeManager properties
                var themeDebug = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["IsDarkMode"] = UnifiedThemeManager.IsDarkMode,
                    ["IsAutoMode"] = UnifiedThemeManager.IsAutoMode,
                    ["DarkModeStartTime"] = UnifiedThemeManager.DarkModeStartTime.ToString(@"hh\:mm"),
                    ["LightModeStartTime"] = UnifiedThemeManager.LightModeStartTime.ToString(@"hh\:mm"),
                    ["EnableAnimations"] = UnifiedThemeManager.EnableAnimations,
                    ["CurrentThemeStatus"] = UnifiedThemeManager.CurrentThemeStatus,
                    ["ThemeColorsCount"] = UnifiedThemeManager.CurrentTheme.Count
                };
                
                return new ThemeDebugInfo
                {
                    IsInitialized = _isInitialized,
                    ThemeServiceDebugInfo = themeDebug,
                    ApplicationResourceCount = _application?.Resources?.Count ?? 0,
                    MergedDictionariesCount = _application?.Resources?.MergedDictionaries?.Count ?? 0,
                    HasDesignSystemLoaded = HasDesignSystemLoaded(),
                    LastError = null
                };
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error getting theme debug info", ex);
                return new ThemeDebugInfo
                {
                    IsInitialized = _isInitialized,
                    LastError = ex.Message
                };
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sicherstellen dass das Design-System geladen ist
        /// </summary>
        private static void EnsureDesignSystemLoaded()
        {
            if (_application?.Resources == null)
            {
                throw new InvalidOperationException("Application resources not available");
            }

            var designSystemUri = new Uri("pack://application:,,,/Resources/DesignSystem.xaml");
            bool hasDesignSystem = false;

            // Prüfen ob bereits geladen
            foreach (ResourceDictionary dict in _application.Resources.MergedDictionaries)
            {
                if (dict.Source == designSystemUri)
                {
                    hasDesignSystem = true;
                    break;
                }
            }

            // Falls nicht vorhanden, hinzufügen
            if (!hasDesignSystem)
            {
                try
                {
                    var designSystemDict = new ResourceDictionary { Source = designSystemUri };
                    _application.Resources.MergedDictionaries.Add(designSystemDict);
                    LoggingService.Instance.LogInfo("📄 Design system ResourceDictionary loaded");
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("🚨 Failed to load design system", ex);
                    throw;
                }
            }
            else
            {
                LoggingService.Instance.LogInfo("📄 Design system already loaded");
            }
        }

        /// <summary>
        /// Prüfen ob Design-System geladen ist
        /// </summary>
        private static bool HasDesignSystemLoaded()
        {
            if (_application?.Resources == null) return false;

            var designSystemUri = new Uri("pack://application:,,,/Resources/DesignSystem.xaml");
            
            foreach (ResourceDictionary dict in _application.Resources.MergedDictionaries)
            {
                if (dict.Source == designSystemUri)
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// UnifiedThemeManager Event-Handler registrieren
        /// </summary>
        private static void RegisterUnifiedThemeManagerEvents()
        {
            // Theme-Änderung Event
            UnifiedThemeManager.ThemeChanged += OnThemeChanged;
            UnifiedThemeManager.AutoModeChanged += OnAutoModeChanged;
            // Note: UnifiedThemeManager doesn't have TimeSettingsChanged event, 
            // but we can track it through property changes if needed

            LoggingService.Instance.LogInfo("📡 UnifiedThemeManager event handlers registered");
        }

        /// <summary>
        /// Aktuelles Theme anwenden
        /// </summary>
        private static void ApplyCurrentTheme()
        {
            try
            {
                // UnifiedThemeManager wendet Theme automatisch an
                // Hier können zusätzliche App-spezifische Theme-Anwendungen erfolgen
                
                LoggingService.Instance.LogInfo($"🎨 Current theme applied: {UnifiedThemeManager.CurrentThemeStatus}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying current theme", ex);
                throw;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handler für Theme-Änderungen
        /// </summary>
        private static void OnThemeChanged(bool isDark)
        {
            try
            {
                LoggingService.Instance.LogInfo($"🎨 ThemeManager: Theme changed to {(isDark ? "Dark" : "Light")} mode");
                
                // Hier können zusätzliche Theme-spezifische Aktionen durchgeführt werden
                // z.B. Fenster-spezifische Anpassungen, Icon-Änderungen, etc.
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling theme change", ex);
                ThemeError?.Invoke(null, new ThemeErrorEventArgs("Theme change handling failed", ex));
            }
        }

        /// <summary>
        /// Handler für Auto-Mode Änderungen
        /// </summary>
        private static void OnAutoModeChanged(bool isAutoMode)
        {
            LoggingService.Instance.LogInfo($"🕐 ThemeManager: Auto-mode {(isAutoMode ? "enabled" : "disabled")}");
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Debug-Informationen für das Theme-System
    /// </summary>
    public class ThemeDebugInfo
    {
        public bool IsInitialized { get; set; }
        public System.Collections.Generic.Dictionary<string, object>? ThemeServiceDebugInfo { get; set; }
        public int ApplicationResourceCount { get; set; }
        public int MergedDictionariesCount { get; set; }
        public bool HasDesignSystemLoaded { get; set; }
        public string? LastError { get; set; }

        public override string ToString()
        {
            return $"ThemeDebugInfo: Initialized={IsInitialized}, Resources={ApplicationResourceCount}, MergedDicts={MergedDictionariesCount}, DesignSystemLoaded={HasDesignSystemLoaded}";
        }
    }

    /// <summary>
    /// Event-Argumente für Theme-Fehler
    /// </summary>
    public class ThemeErrorEventArgs : EventArgs
    {
        public string Message { get; }
        public Exception? Exception { get; }

        public ThemeErrorEventArgs(string message, Exception? exception = null)
        {
            Message = message;
            Exception = exception;
        }
    }

    #endregion
}
