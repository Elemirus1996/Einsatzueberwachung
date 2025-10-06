using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace Einsatzueberwachung.Services
{
    public class ThemeService : INotifyPropertyChanged
    {
        private static ThemeService? _instance;
        private bool _isDarkMode;
        private bool _isAutoMode = true;
        private DispatcherTimer? _timeCheckTimer;
        private TimeSpan _darkModeStartTime = new TimeSpan(18, 0, 0);
        private TimeSpan _lightModeStartTime = new TimeSpan(8, 0, 0);

        public static ThemeService Instance => _instance ??= new ThemeService();

        private ThemeService()
        {
            // Standard: Automatischer Modus aktiviert
            _isAutoMode = true;
            
            // Initiales Theme basierend auf aktueller Uhrzeit setzen - SOFORT anwenden
            CheckAutoTheme();
            
            // Theme sofort anwenden
            ApplyThemeToApplication();
            
            // Timer für automatische Überprüfung starten
            StartTimeCheckTimer();
            
            LoggingService.Instance.LogInfo($"ThemeService initialized in Auto-Mode: {CurrentThemeStatus} - Applied immediately");
        }

        #region Properties

        public bool IsDarkMode
        {
            get => _isDarkMode;
            private set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    OnPropertyChanged();
                    ApplyThemeToApplication();
                    ThemeChanged?.Invoke(value);
                    
                    LoggingService.Instance.LogInfo($"Theme changed to: {(value ? "Dark" : "Light")}");
                }
            }
        }

        public bool IsAutoMode
        {
            get => _isAutoMode;
            set
            {
                if (_isAutoMode != value)
                {
                    _isAutoMode = value;
                    OnPropertyChanged();
                    
                    if (value)
                    {
                        LoggingService.Instance.LogInfo("Auto-Mode enabled");
                        CheckAutoTheme();
                        StartTimeCheckTimer();
                    }
                    else
                    {
                        LoggingService.Instance.LogInfo("Auto-Mode disabled");
                        StopTimeCheckTimer();
                    }
                }
            }
        }

        public TimeSpan DarkModeStartTime
        {
            get => _darkModeStartTime;
            set
            {
                if (_darkModeStartTime != value)
                {
                    _darkModeStartTime = value;
                    OnPropertyChanged();
                    
                    // Sofort prüfen wenn Auto-Mode aktiviert ist
                    if (IsAutoMode)
                    {
                        CheckAutoTheme();
                    }
                    
                    LoggingService.Instance.LogInfo($"Dark mode start time changed to: {value:hh\\:mm}");
                }
            }
        }

        public TimeSpan LightModeStartTime
        {
            get => _lightModeStartTime;
            set
            {
                if (_lightModeStartTime != value)
                {
                    _lightModeStartTime = value;
                    OnPropertyChanged();
                    
                    // Sofort prüfen wenn Auto-Mode aktiviert ist
                    if (IsAutoMode)
                    {
                        CheckAutoTheme();
                    }
                    
                    LoggingService.Instance.LogInfo($"Light mode start time changed to: {value:hh\\:mm}");
                }
            }
        }

        public string CurrentThemeStatus => IsAutoMode 
            ? $"Auto ({(IsDarkMode ? "Dunkel" : "Hell")}, {DarkModeStartTime:hh\\:mm}-{LightModeStartTime:hh\\:mm})" 
            : (IsDarkMode ? "Dunkel (Manuell)" : "Hell (Manuell)");

        #endregion

        #region Events

        public event Action<bool>? ThemeChanged;

        #endregion

        #region Public Methods

        public void SetDarkMode(bool isDark)
        {
            // Deaktiviere automatischen Modus beim manuellen Setzen
            IsAutoMode = false;
            IsDarkMode = isDark;
            
            LoggingService.Instance.LogInfo($"Theme manually set to: {(isDark ? "Dark" : "Light")}");
        }

        public void ToggleTheme()
        {
            if (IsAutoMode)
            {
                // Beim ersten Toggle: Automatik ausschalten und manuell wechseln
                LoggingService.Instance.LogInfo("Toggling from Auto-Mode to Manual");
                IsAutoMode = false;
                IsDarkMode = !IsDarkMode;
            }
            else
            {
                // Normaler Toggle zwischen Hell und Dunkel
                IsDarkMode = !IsDarkMode;
            }
        }

        public void EnableAutoMode()
        {
            IsAutoMode = true;
            LoggingService.Instance.LogInfo("Auto-Mode re-enabled");
        }

        public void SetAutoModeTimes(TimeSpan darkStart, TimeSpan lightStart)
        {
            // Validierung der Zeiten
            if (darkStart == lightStart)
            {
                throw new ArgumentException("Dark and light start times cannot be the same");
            }

            DarkModeStartTime = darkStart;
            LightModeStartTime = lightStart;
            
            LoggingService.Instance.LogInfo($"Auto-mode times updated - Dark: {darkStart:hh\\:mm}, Light: {lightStart:hh\\:mm}");
        }

        #endregion

        #region Private Methods

        private void CheckAutoTheme()
        {
            if (!IsAutoMode) return;

            var now = DateTime.Now.TimeOfDay;
            
            // Bestimme ob aktuell Dunkel-Modus aktiv sein sollte
            bool shouldBeDark;
            
            if (DarkModeStartTime < LightModeStartTime)
            {
                // Normale Zeiten: z.B. Dark 20:00, Light 06:00 
                // -> Dunkel zwischen 20:00 und 06:00 (nächster Tag)
                shouldBeDark = now >= DarkModeStartTime || now < LightModeStartTime;
            }
            else
            {
                // Umgekehrte Zeiten: z.B. Dark 06:00, Light 20:00
                // -> Dunkel zwischen 06:00 und 20:00 (gleicher Tag)
                shouldBeDark = now >= DarkModeStartTime && now < LightModeStartTime;
            }
            
            if (IsDarkMode != shouldBeDark)
            {
                IsDarkMode = shouldBeDark;
                LoggingService.Instance.LogInfo($"Auto-Theme changed to {(shouldBeDark ? "Dark" : "Light")} mode at {now:hh\\:mm\\:ss}");
            }
        }

        private void StartTimeCheckTimer()
        {
            if (_timeCheckTimer == null)
            {
                _timeCheckTimer = new DispatcherTimer();
                _timeCheckTimer.Interval = TimeSpan.FromMinutes(1); // Prüfung jede Minute
                _timeCheckTimer.Tick += (s, e) => CheckAutoTheme();
            }
            
            if (!_timeCheckTimer.IsEnabled)
            {
                _timeCheckTimer.Start();
                LoggingService.Instance.LogInfo("Auto-theme timer started - checking every minute");
            }
        }

        private void StopTimeCheckTimer()
        {
            if (_timeCheckTimer != null && _timeCheckTimer.IsEnabled)
            {
                _timeCheckTimer.Stop();
                LoggingService.Instance.LogInfo("Auto-theme timer stopped");
            }
        }

        private void ApplyThemeToApplication()
        {
            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    var app = Application.Current;
                    if (app?.Resources == null) return;

                    // Clear existing theme dictionaries
                    var resourcesToRemove = new List<ResourceDictionary>();
                    foreach (ResourceDictionary dict in app.Resources.MergedDictionaries)
                    {
                        if (dict.Source?.ToString().Contains("Light") == true || 
                            dict.Source?.ToString().Contains("Dark") == true ||
                            dict.Source?.ToString().Contains("DesignSystem") == true)
                        {
                            resourcesToRemove.Add(dict);
                        }
                    }

                    foreach (var dict in resourcesToRemove)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }

                    // Apply the design system (contains both themes)
                    var designSystemDict = new ResourceDictionary();
                    designSystemDict.Source = new Uri("pack://application:,,,/Resources/DesignSystem.xaml", UriKind.Absolute);
                    app.Resources.MergedDictionaries.Add(designSystemDict);

                    // Set theme-specific resources directly in application resources
                    if (IsDarkMode)
                    {
                        // Dark theme colors - ORANGE-FOCUSED
                        app.Resources["Surface"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(18, 18, 18)); // #121212
                        app.Resources["SurfaceVariant"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)); // #1E1E1E
                        app.Resources["SurfaceContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26)); // #1A1A1A
                        app.Resources["SurfaceContainerHigh"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(36, 36, 36)); // #242424
                        app.Resources["SurfaceContainerHighest"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 44, 44)); // #2C2C2C
                        app.Resources["OnSurface"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(227, 227, 227)); // #E3E3E3
                        app.Resources["OnSurfaceVariant"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(179, 179, 179)); // #B3B3B3
                        
                        // Orange Primary Colors for Dark Mode
                        app.Resources["Primary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 183, 77)); // #FFB74D - Helles Orange für Dark Mode
                        app.Resources["PrimaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 81, 0)); // #E65100 - Dunkles Orange Container
                        app.Resources["OnPrimary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 15, 0)); // #1A0F00 - Sehr dunkles Braun
                        app.Resources["OnPrimaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 224, 178)); // #FFE0B2 - Helles Orange-Beige
                        
                        // Orange Tertiary Colors for Dark Mode
                        app.Resources["Tertiary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 204, 128)); // #FFCC80 - Noch helleres Orange für Akzente
                        app.Resources["TertiaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 124, 0)); // #F57C00 - Orange Container
                        app.Resources["OnTertiary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 22, 0)); // #2E1600 - Dunkles Braun
                        app.Resources["OnTertiaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 224, 178)); // #FFE0B2 - Helles Orange-Beige
                        
                        // Dark Secondary Colors
                        app.Resources["Secondary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(176, 190, 197)); // #B0BEC5
                        app.Resources["SecondaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(55, 71, 79)); // #37474F
                        app.Resources["OnSecondary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(28, 49, 58)); // #1C313A
                        app.Resources["OnSecondaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(207, 216, 220)); // #CFD8DC
                        
                        // Dark Outline Colors
                        app.Resources["Outline"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64)); // #404040
                        app.Resources["OutlineVariant"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 46, 46)); // #2E2E2E
                        
                        // Dark Semantic Colors
                        app.Resources["Success"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(129, 199, 132)); // #81C784 - Helleres Grün für Dark Mode
                        app.Resources["SuccessContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 125, 50)); // #2E7D32
                        app.Resources["OnSuccess"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(27, 94, 32)); // #1B5E20
                        app.Resources["OnSuccessContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 230, 201)); // #C8E6C9
                        
                        app.Resources["Warning"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 204, 128)); // #FFCC80 - Orange für Warnungen in Dark Mode
                        app.Resources["WarningContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 124, 0)); // #F57C00
                        app.Resources["OnWarning"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 22, 0)); // #2E1600
                        app.Resources["OnWarningContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 224, 178)); // #FFE0B2
                        
                        app.Resources["Error"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 83, 80)); // #EF5350 - Helleres Rot für Dark Mode
                        app.Resources["ErrorContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(198, 40, 40)); // #C62828
                        app.Resources["OnError"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 235, 238)); // #FFEBEE
                        app.Resources["OnErrorContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 205, 210)); // #FFCDD2
                        
                        // Dark Mode Gradients - ORANGE
                        var darkPrimaryGradient = new System.Windows.Media.LinearGradientBrush();
                        darkPrimaryGradient.StartPoint = new System.Windows.Point(0, 0);
                        darkPrimaryGradient.EndPoint = new System.Windows.Point(1, 1);
                        darkPrimaryGradient.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(230, 81, 0), 0)); // #E65100
                        darkPrimaryGradient.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(255, 183, 77), 1)); // #FFB74D
                        app.Resources["PrimaryGradient"] = darkPrimaryGradient;
                    }
                    else
                    {
                        // Light theme colors - ORANGE-FOCUSED
                        app.Resources["Surface"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 254, 254)); // #FEFEFE - Fast weiß für besseren Kontrast
                        app.Resources["SurfaceVariant"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245)); // #F5F5F5 - Hellgrau
                        app.Resources["SurfaceContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)); // #F0F0F0 - Etwas dunkler grau
                        app.Resources["SurfaceContainerHigh"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(235, 235, 235)); // #EBEBEB - Mittlerer Grauton
                        app.Resources["SurfaceContainerHighest"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 229, 229)); // #E5E5E5 - Dunkler Grauton
                        app.Resources["OnSurface"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 28, 30)); // #1A1C1E
                        app.Resources["OnSurfaceVariant"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(68, 71, 74)); // #44474A
                        
                        // Orange Primary Colors for Light Mode
                        app.Resources["Primary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 124, 0)); // #F57C00 - Orange als Hauptfarbe
                        app.Resources["PrimaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 224, 178)); // #FFE0B2 - Helles Orange Container
                        app.Resources["OnPrimary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)); // #FFFFFF
                        app.Resources["OnPrimaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 81, 0)); // #E65100
                        
                        // Orange Tertiary Colors for Light Mode
                        app.Resources["Tertiary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 152, 0)); // #FF9800 - Helleres Orange für Akzente
                        app.Resources["TertiaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 243, 224)); // #FFF3E0 - Sehr helles Orange
                        app.Resources["OnTertiary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)); // #FFFFFF
                        app.Resources["OnTertiaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 124, 0)); // #F57C00
                        
                        // Light Secondary Colors - Komplementäre Blau-Grau Töne
                        app.Resources["Secondary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(69, 90, 100)); // #455A64 - Blue Grey
                        app.Resources["SecondaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(207, 216, 220)); // #CFD8DC
                        app.Resources["OnSecondary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)); // #FFFFFF
                        app.Resources["OnSecondaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 50, 56)); // #263238
                        
                        // Light Outline Colors
                        app.Resources["Outline"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(116, 119, 122)); // #74777A
                        app.Resources["OutlineVariant"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(196, 199, 202)); // #C4C7CA
                        
                        // Light Semantic Colors
                        app.Resources["Success"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // #4CAF50 - Grün bleibt für Erfolg
                        app.Resources["SuccessContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 245, 232)); // #E8F5E8
                        app.Resources["OnSuccess"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)); // #FFFFFF
                        app.Resources["OnSuccessContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 125, 50)); // #2E7D32
                        
                        app.Resources["Warning"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 152, 0)); // #FF9800 - Orange für Warnungen
                        app.Resources["WarningContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 243, 224)); // #FFF3E0
                        app.Resources["OnWarning"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)); // #FFFFFF
                        app.Resources["OnWarningContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 81, 0)); // #E65100
                        
                        app.Resources["Error"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)); // #F44336 - Rot bleibt für Fehler
                        app.Resources["ErrorContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 235, 238)); // #FFEBEE
                        app.Resources["OnError"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)); // #FFFFFF
                        app.Resources["OnErrorContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(198, 40, 40)); // #C62828
                        
                        // Light Mode Gradients - ORANGE
                        var lightPrimaryGradient = new System.Windows.Media.LinearGradientBrush();
                        lightPrimaryGradient.StartPoint = new System.Windows.Point(0, 0);
                        lightPrimaryGradient.EndPoint = new System.Windows.Point(1, 1);
                        lightPrimaryGradient.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(255, 224, 178), 0)); // #FFE0B2
                        lightPrimaryGradient.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(255, 204, 128), 1)); // #FFCC80
                        app.Resources["PrimaryGradient"] = lightPrimaryGradient;
                    }
                    
                    LoggingService.Instance.LogInfo($"Theme applied to application: {(IsDarkMode ? "Dark" : "Light")}");
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to application", ex);
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            StopTimeCheckTimer();
            LoggingService.Instance.LogInfo("ThemeService disposed");
        }

        #endregion
    }
}
