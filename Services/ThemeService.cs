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

        public static ThemeService Instance => _instance ??= new ThemeService();

        private ThemeService()
        {
            // Standard: Automatischer Modus aktiviert
            _isAutoMode = true;
            
            // Initiales Theme basierend auf aktueller Uhrzeit setzen
            CheckAutoTheme();
            
            // Timer für automatische Überprüfung starten
            StartTimeCheckTimer();
            
            LoggingService.Instance.LogInfo($"ThemeService initialized in Auto-Mode: {CurrentThemeStatus}");
        }

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

        public string CurrentThemeStatus => IsAutoMode 
            ? $"Auto ({(IsDarkMode ? "Dunkel" : "Hell")})" 
            : (IsDarkMode ? "Dunkel" : "Hell");

        public event Action<bool>? ThemeChanged;

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

        private void CheckAutoTheme()
        {
            if (!IsAutoMode) return;

            var now = DateTime.Now.TimeOfDay;
            var darkStart = new TimeSpan(18, 0, 0); // 18:00 Uhr
            var lightStart = new TimeSpan(8, 0, 0);  // 08:00 Uhr

            // Dunkel-Modus zwischen 18:00 und 08:00 Uhr
            bool shouldBeDark = now >= darkStart || now < lightStart;
            
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
                        // Dark theme colors
                        app.Resources["Surface"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(18, 18, 18)); // #121212
                        app.Resources["SurfaceVariant"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)); // #1E1E1E
                        app.Resources["SurfaceContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 24, 24)); // #181818
                        app.Resources["OnSurface"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224)); // #E0E0E0
                        app.Resources["OnSurfaceVariant"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(189, 189, 189)); // #BDBDBD
                        app.Resources["Primary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(187, 134, 252)); // #BB86FC
                        app.Resources["PrimaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 20, 140)); // #4A148C
                        app.Resources["OnPrimary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0)); // #000000
                        app.Resources["OnPrimaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224)); // #E0E0E0
                        app.Resources["Secondary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(207, 102, 121)); // #CF6679
                        app.Resources["Outline"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 51, 51)); // #333333
                        app.Resources["OutlineVariant"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(66, 66, 66)); // #424242
                    }
                    else
                    {
                        // Light theme colors
                        app.Resources["Surface"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)); // #FFFFFF
                        app.Resources["SurfaceVariant"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245)); // #F5F5F5
                        app.Resources["SurfaceContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(250, 250, 250)); // #FAFAFA
                        app.Resources["OnSurface"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 33, 33)); // #212121
                        app.Resources["OnSurfaceVariant"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(97, 97, 97)); // #616161
                        app.Resources["Primary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243)); // #2196F3
                        app.Resources["PrimaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(187, 222, 251)); // #BBDEFB
                        app.Resources["OnPrimary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)); // #FFFFFF
                        app.Resources["OnPrimaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(13, 71, 161)); // #0D47A1
                        app.Resources["Secondary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(96, 125, 139)); // #607D8B
                        app.Resources["Outline"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224)); // #E0E0E0
                        app.Resources["OutlineVariant"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(189, 189, 189)); // #BDBDBD
                    }
                    
                    LoggingService.Instance.LogInfo($"Theme applied to application: {(IsDarkMode ? "Dark" : "Light")}");
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to application", ex);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            StopTimeCheckTimer();
            LoggingService.Instance.LogInfo("ThemeService disposed");
        }
    }
}
