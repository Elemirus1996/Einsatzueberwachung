using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;
using System.Text.Json;

namespace Einsatzueberwachung.Services
{
    /// <summary>
    /// 🎨 UNIFIED THEME MANAGER v5.0 
    /// Vereint Design System und Theme Service in einer sauberen Lösung
    /// Material Design 3 + Orange Branding + Auto Theme Switching
    /// </summary>
    public class UnifiedThemeManager : INotifyPropertyChanged, IDisposable
    {
        #region Singleton Pattern
        
        private static UnifiedThemeManager? _instance;
        public static UnifiedThemeManager Instance => _instance ??= new UnifiedThemeManager();

        #endregion

        #region Private Fields

        private bool _isDarkMode;
        private bool _isAutoMode = true;
        private TimeSpan _darkModeStartTime = new TimeSpan(19, 0, 0);
        private TimeSpan _lightModeStartTime = new TimeSpan(7, 0, 0);
        private DispatcherTimer? _timeCheckTimer;
        private DispatcherTimer? _saveSettingsTimer; // Neu: Debounce Timer für Settings
        private bool _enableAnimations = true;
        private Dictionary<string, ThemeColorDefinition> _currentTheme = new();
        
        private readonly string _settingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Einsatzueberwachung", 
            "unified-theme-settings.json"
        );

        #endregion

        #region Constructor

        private UnifiedThemeManager()
        {
            LoggingService.Instance.LogInfo("=== UNIFIED THEME MANAGER v5.0 INITIALIZING ===");
            
            try
            {
                InitializeThemeDefinitions();
                LoadSettings();
                
                if (Application.Current != null)
                {
                    ApplyTheme();
                    
                    if (_isAutoMode)
                    {
                        CheckAutoTheme();
                        StartTimeCheckTimer();
                    }
                }
                
                LoggingService.Instance.LogInfo($"UnifiedThemeManager v5.0 initialized: {CurrentThemeStatus}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error in UnifiedThemeManager constructor", ex);
                ApplyFallbackTheme();
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Ist Dark Mode aktiv?
        /// </summary>
        public bool IsDarkMode
        {
            get => _isDarkMode;
            private set
            {
                if (_isDarkMode != value)
                {
                    var oldMode = _isDarkMode;
                    _isDarkMode = value;
                    OnPropertyChanged();
                    
                    ApplyTheme();
                    
                    ThemeChanged?.Invoke(value);
                    ModeChanged?.Invoke(oldMode, value);
                    
                    LoggingService.Instance.LogInfo($"🎨 Theme changed: {(oldMode ? "Dark" : "Light")} → {(value ? "Dark" : "Light")}");
                }
            }
        }

        /// <summary>
        /// Ist Auto-Mode aktiviert?
        /// </summary>
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
                        CheckAutoTheme();
                        StartTimeCheckTimer();
                    }
                    else
                    {
                        StopTimeCheckTimer();
                    }
                    
                    AutoModeChanged?.Invoke(value);
                    DebouncedSaveSettings(); // Verwende Debounced Save
                }
            }
        }

        /// <summary>
        /// Dark Mode Startzeit
        /// </summary>
        public TimeSpan DarkModeStartTime
        {
            get => _darkModeStartTime;
            set
            {
                if (_darkModeStartTime != value)
                {
                    _darkModeStartTime = value;
                    OnPropertyChanged();
                    
                    if (IsAutoMode)
                    {
                        CheckAutoTheme();
                    }
                    
                    DebouncedSaveSettings(); // Verwende Debounced Save
                }
            }
        }

        /// <summary>
        /// Light Mode Startzeit
        /// </summary>
        public TimeSpan LightModeStartTime
        {
            get => _lightModeStartTime;
            set
            {
                if (_lightModeStartTime != value)
                {
                    _lightModeStartTime = value;
                    OnPropertyChanged();
                    
                    if (IsAutoMode)
                    {
                        CheckAutoTheme();
                    }
                    
                    DebouncedSaveSettings(); // Verwende Debounced Save
                }
            }
        }

        /// <summary>
        /// Animationen aktiviert?
        /// </summary>
        public bool EnableAnimations
        {
            get => _enableAnimations;
            set
            {
                if (_enableAnimations != value)
                {
                    _enableAnimations = value;
                    OnPropertyChanged();
                    ApplyAnimationSettings();
                    DebouncedSaveSettings(); // Verwende Debounced Save
                }
            }
        }

        /// <summary>
        /// Aktueller Theme-Status als String
        /// </summary>
        public string CurrentThemeStatus
        {
            get
            {
                if (IsAutoMode)
                {
                    var nextSwitch = GetNextSwitchTime();
                    return $"Auto ({(IsDarkMode ? "🌙 Dunkel" : "☀️ Hell")}) - Nächster Wechsel: {nextSwitch.Hours:00}:{nextSwitch.Minutes:00}";
                }
                return IsDarkMode ? "🌙 Dunkel (Manuell)" : "☀️ Hell (Manuell)";
            }
        }

        /// <summary>
        /// Aktuelles Theme-Dictionary für UI-Komponenten
        /// </summary>
        public IReadOnlyDictionary<string, ThemeColorDefinition> CurrentTheme => _currentTheme;

        #endregion

        #region Events

        public event Action<bool>? ThemeChanged;
        public event Action<bool, bool>? ModeChanged;
        public event Action<bool>? AutoModeChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Public Methods

        /// <summary>
        /// Setzt Dark Mode manuell (deaktiviert Auto-Mode)
        /// </summary>
        public void SetDarkMode(bool isDark)
        {
            if (IsAutoMode)
            {
                IsAutoMode = false;
            }
            
            IsDarkMode = isDark;
        }

        /// <summary>
        /// Theme umschalten (Smart Toggle)
        /// </summary>
        public void ToggleTheme()
        {
            if (IsAutoMode)
            {
                IsAutoMode = false;
            }
            
            IsDarkMode = !IsDarkMode;
        }

        /// <summary>
        /// Auto-Mode aktivieren
        /// </summary>
        public void EnableAutoMode()
        {
            IsAutoMode = true;
        }

        /// <summary>
        /// Automatische Zeiten konfigurieren
        /// </summary>
        public void SetAutoModeTimes(TimeSpan darkStart, TimeSpan lightStart)
        {
            if (darkStart == lightStart)
            {
                throw new ArgumentException("Dark and light start times cannot be identical");
            }

            var oldDarkTime = _darkModeStartTime;
            var oldLightTime = _lightModeStartTime;
            
            _darkModeStartTime = darkStart;
            _lightModeStartTime = lightStart;
            
            OnPropertyChanged(nameof(DarkModeStartTime));
            OnPropertyChanged(nameof(LightModeStartTime));
            
            // Sichere String-Formatierung ohne Interpolation-Probleme
            LoggingService.Instance.LogInfo($"🕐 Auto mode times changed: Dark {oldDarkTime.Hours:00}:{oldDarkTime.Minutes:00}→{darkStart.Hours:00}:{darkStart.Minutes:00}, Light {oldLightTime.Hours:00}:{oldLightTime.Minutes:00}→{lightStart.Hours:00}:{lightStart.Minutes:00}");
            
            if (IsAutoMode)
            {
                LoggingService.Instance.LogInfo("🔄 Checking theme immediately due to time change...");
                ForceImmediateThemeCheck();
            }
            
            DebouncedSaveSettings(); // Verwende Debounced Save
        }

        /// <summary>
        /// Forciert eine sofortige Theme-Prüfung (für Zeit-Änderungen)
        /// </summary>
        public void ForceImmediateThemeCheck()
        {
            if (!IsAutoMode) return;
            
            var now = DateTime.Now.TimeOfDay;
            bool shouldBeDark = ShouldBeDarkAtTime(now);
            
            LoggingService.Instance.LogInfo($"🚀 FORCE IMMEDIATE CHECK: Current time: {now.Hours:00}:{now.Minutes:00}, " +
                $"Dark time: {DarkModeStartTime.Hours:00}:{DarkModeStartTime.Minutes:00}-{LightModeStartTime.Hours:00}:{LightModeStartTime.Minutes:00}, " +
                $"Should be dark: {shouldBeDark}, Currently dark: {IsDarkMode}");
            
            if (IsDarkMode != shouldBeDark)
            {
                LoggingService.Instance.LogInfo($"🎨 IMMEDIATE THEME SWITCH: {(IsDarkMode ? "Dark" : "Light")} → {(shouldBeDark ? "Dark" : "Light")}");
                IsDarkMode = shouldBeDark;
            }
            else
            {
                LoggingService.Instance.LogInfo($"✅ Theme is already correct: {(shouldBeDark ? "Dark" : "Light")} mode");
            }
        }

        /// <summary>
        /// Holt eine Theme-Farbe
        /// </summary>
        public SolidColorBrush GetThemeColor(string colorKey)
        {
            if (_currentTheme.TryGetValue(colorKey, out var colorDef))
            {
                return new SolidColorBrush(colorDef.Color);
            }
            
            // Fallback auf Orange
            return new SolidColorBrush(Colors.Orange);
        }

        /// <summary>
        /// Registriert eine UI-Komponente für automatische Theme-Updates
        /// </summary>
        public void RegisterThemeConsumer(IThemeConsumer consumer)
        {
            ThemeChanged += consumer.OnThemeChanged;
            consumer.OnThemeChanged(IsDarkMode); // Sofort anwenden
        }

        /// <summary>
        /// Theme auf Standard zurücksetzen
        /// </summary>
        public void ResetToDefaults()
        {
            IsAutoMode = true;
            DarkModeStartTime = new TimeSpan(19, 0, 0);
            LightModeStartTime = new TimeSpan(7, 0, 0);
            EnableAnimations = true;
            
            CheckAutoTheme();
            SaveSettings();
        }

        #endregion

        #region Private Methods - Theme Logic

        private void InitializeThemeDefinitions()
        {
            // Orange-focused theme definitions
            DefineThemeColors();
        }

        private void DefineThemeColors()
        {
            // Diese Methode definiert alle Farben für beide Modi
            var lightTheme = new Dictionary<string, ThemeColorDefinition>
            {
                // Primary Orange System
                ["Primary"] = new ThemeColorDefinition("#F57C00", "Main Orange"),
                ["PrimaryContainer"] = new ThemeColorDefinition("#FFE0B2", "Light Orange Container"),
                ["OnPrimary"] = new ThemeColorDefinition("#FFFFFF", "White text on orange"),
                ["OnPrimaryContainer"] = new ThemeColorDefinition("#E65100", "Dark orange text"),
                
                // Secondary System
                ["Secondary"] = new ThemeColorDefinition("#455A64", "Blue-grey"),
                ["SecondaryContainer"] = new ThemeColorDefinition("#CFD8DC", "Light blue-grey"),
                ["OnSecondary"] = new ThemeColorDefinition("#FFFFFF", "White text on secondary"),
                ["OnSecondaryContainer"] = new ThemeColorDefinition("#263238", "Dark text on container"),
                
                // Tertiary Orange System
                ["Tertiary"] = new ThemeColorDefinition("#FF9800", "Medium Orange"),
                ["TertiaryContainer"] = new ThemeColorDefinition("#FFF3E0", "Very light orange"),
                ["OnTertiary"] = new ThemeColorDefinition("#FFFFFF", "White text on tertiary"),
                ["OnTertiaryContainer"] = new ThemeColorDefinition("#F57C00", "Orange text on container"),
                
                // Surface System
                ["Surface"] = new ThemeColorDefinition("#FEFEFE", "Main surface"),
                ["SurfaceVariant"] = new ThemeColorDefinition("#F5F5F5", "Surface variant"),
                ["SurfaceContainer"] = new ThemeColorDefinition("#F0F0F0", "Container surface"),
                ["SurfaceContainerHigh"] = new ThemeColorDefinition("#EBEBEB", "High container"),
                ["SurfaceContainerHighest"] = new ThemeColorDefinition("#E5E5E5", "Highest container"),
                ["OnSurface"] = new ThemeColorDefinition("#1A1C1E", "Text on surface"),
                ["OnSurfaceVariant"] = new ThemeColorDefinition("#44474A", "Text on variant"),
                
                // Outline System
                ["Outline"] = new ThemeColorDefinition("#74777A", "Standard outline"),
                ["OutlineVariant"] = new ThemeColorDefinition("#C4C7CA", "Light outline"),
                
                // Semantic Colors
                ["Success"] = new ThemeColorDefinition("#4CAF50", "Green success"),
                ["SuccessContainer"] = new ThemeColorDefinition("#E8F5E8", "Light green container"),
                ["OnSuccess"] = new ThemeColorDefinition("#FFFFFF", "White text on success"),
                ["OnSuccessContainer"] = new ThemeColorDefinition("#2E7D32", "Dark green text"),
                
                ["Warning"] = new ThemeColorDefinition("#FF9800", "Orange warning"),
                ["WarningContainer"] = new ThemeColorDefinition("#FFF3E0", "Light orange container"),
                ["OnWarning"] = new ThemeColorDefinition("#FFFFFF", "White text on warning"),
                ["OnWarningContainer"] = new ThemeColorDefinition("#E65100", "Dark orange text"),
                
                ["Error"] = new ThemeColorDefinition("#F44336", "Red error"),
                ["ErrorContainer"] = new ThemeColorDefinition("#FFEBEE", "Light red container"),
                ["OnError"] = new ThemeColorDefinition("#FFFFFF", "White text on error"),
                ["OnErrorContainer"] = new ThemeColorDefinition("#C62828", "Dark red text"),
                
                // Emergency Team Colors (harmonized with orange)
                ["FlächeColor"] = new ThemeColorDefinition("#2196F3", "Blue"),
                ["TrümmerColor"] = new ThemeColorDefinition("#F57C00", "Main Orange"),
                ["MantrailerColor"] = new ThemeColorDefinition("#4CAF50", "Green"),
                ["WasserColor"] = new ThemeColorDefinition("#00BCD4", "Cyan"),
                ["LawineColor"] = new ThemeColorDefinition("#9C27B0", "Purple"),
                ["GeländeColor"] = new ThemeColorDefinition("#795548", "Brown"),
                ["LeichenColor"] = new ThemeColorDefinition("#607D8B", "Blue-Grey"),
                ["AllgemeinColor"] = new ThemeColorDefinition("#FF9800", "Light Orange")
            };

            var darkTheme = new Dictionary<string, ThemeColorDefinition>
            {
                // Primary Orange System (Dark)
                ["Primary"] = new ThemeColorDefinition("#FFB74D", "Dark Orange"),
                ["PrimaryContainer"] = new ThemeColorDefinition("#E65100", "Dark Container"),
                ["OnPrimary"] = new ThemeColorDefinition("#1A0F00", "Text on Primary"),
                ["OnPrimaryContainer"] = new ThemeColorDefinition("#FFE0B2", "Text on Container"),
                
                // Secondary System (Dark)
                ["Secondary"] = new ThemeColorDefinition("#B0BEC5", "Light blue-grey"),
                ["SecondaryContainer"] = new ThemeColorDefinition("#37474F", "Dark blue-grey container"),
                ["OnSecondary"] = new ThemeColorDefinition("#1C313A", "Dark text on secondary"),
                ["OnSecondaryContainer"] = new ThemeColorDefinition("#CFD8DC", "Light text on container"),
                
                // Tertiary Orange System (Dark)
                ["Tertiary"] = new ThemeColorDefinition("#FFCC80", "Light Orange"),
                ["TertiaryContainer"] = new ThemeColorDefinition("#F57C00", "Orange container"),
                ["OnTertiary"] = new ThemeColorDefinition("#2E1600", "Dark text on tertiary"),
                ["OnTertiaryContainer"] = new ThemeColorDefinition("#FFE0B2", "Light text on container"),
                
                // Surface System (Dark)
                ["Surface"] = new ThemeColorDefinition("#121212", "Main surface"),
                ["SurfaceVariant"] = new ThemeColorDefinition("#1E1E1E", "Surface variant"),
                ["SurfaceContainer"] = new ThemeColorDefinition("#1A1A1A", "Container surface"),
                ["SurfaceContainerHigh"] = new ThemeColorDefinition("#242424", "High container"),
                ["SurfaceContainerHighest"] = new ThemeColorDefinition("#2C2C2C", "Highest container"),
                ["OnSurface"] = new ThemeColorDefinition("#E3E3E3", "Text on surface"),
                ["OnSurfaceVariant"] = new ThemeColorDefinition("#B3B3B3", "Text on variant"),
                
                // Outline System (Dark)
                ["Outline"] = new ThemeColorDefinition("#404040", "Dark outline"),
                ["OutlineVariant"] = new ThemeColorDefinition("#2E2E2E", "Dark outline variant"),
                
                // Semantic Colors (Dark)
                ["Success"] = new ThemeColorDefinition("#81C784", "Light green success"),
                ["SuccessContainer"] = new ThemeColorDefinition("#2E7D32", "Dark green container"),
                ["OnSuccess"] = new ThemeColorDefinition("#1B5E20", "Dark text on success"),
                ["OnSuccessContainer"] = new ThemeColorDefinition("#C8E6C9", "Light text on container"),
                
                ["Warning"] = new ThemeColorDefinition("#FFCC80", "Light orange warning"),
                ["WarningContainer"] = new ThemeColorDefinition("#F57C00", "Orange container"),
                ["OnWarning"] = new ThemeColorDefinition("#2E1600", "Dark text on warning"),
                ["OnWarningContainer"] = new ThemeColorDefinition("#FFE0B2", "Light text on container"),
                
                ["Error"] = new ThemeColorDefinition("#EF5350", "Light red error"),
                ["ErrorContainer"] = new ThemeColorDefinition("#C62828", "Dark red container"),
                ["OnError"] = new ThemeColorDefinition("#FFEBEE", "Light text on error"),
                ["OnErrorContainer"] = new ThemeColorDefinition("#FFCDD2", "Light text on container"),
                
                // Emergency Team Colors (Dark harmonized)
                ["FlächeColor"] = new ThemeColorDefinition("#64B5F6", "Light Blue"),
                ["TrümmerColor"] = new ThemeColorDefinition("#FFB74D", "Light Orange"),
                ["MantrailerColor"] = new ThemeColorDefinition("#81C784", "Light Green"),
                ["WasserColor"] = new ThemeColorDefinition("#4DD0E1", "Light Cyan"),
                ["LawineColor"] = new ThemeColorDefinition("#BA68C8", "Light Purple"),
                ["GeländeColor"] = new ThemeColorDefinition("#A1887F", "Light Brown"),
                ["LeichenColor"] = new ThemeColorDefinition("#90A4AE", "Light Blue-Grey"),
                ["AllgemeinColor"] = new ThemeColorDefinition("#FFCC80", "Very Light Orange")
            };

            // Set current theme
            _currentTheme = IsDarkMode ? darkTheme : lightTheme;
        }

        private void ApplyTheme()
        {
            try
            {
                if (Application.Current?.Resources == null) return;

                // Update current theme definition
                DefineThemeColors();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Apply all colors to Application.Resources
                    foreach (var kvp in _currentTheme)
                    {
                        var brush = new SolidColorBrush(kvp.Value.Color);
                        brush.Freeze(); // Optimize performance
                        Application.Current.Resources[kvp.Key] = brush;
                    }
                    
                    ApplyAnimationSettings();
                    LoggingService.Instance.LogInfo($"✅ Unified theme applied: {(IsDarkMode ? "Dark" : "Light")} mode with {_currentTheme.Count} colors");
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying unified theme", ex);
                ApplyFallbackTheme();
            }
        }

        private void ApplyFallbackTheme()
        {
            try
            {
                if (Application.Current?.Resources == null) return;
                
                // Minimal fallback theme
                Application.Current.Resources["Primary"] = new SolidColorBrush(Colors.Orange);
                Application.Current.Resources["Surface"] = new SolidColorBrush(IsDarkMode ? Colors.DarkGray : Colors.White);
                Application.Current.Resources["OnSurface"] = new SolidColorBrush(IsDarkMode ? Colors.White : Colors.Black);
                
                LoggingService.Instance.LogInfo("Fallback theme applied");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Even fallback theme failed", ex);
            }
        }

        private void ApplyAnimationSettings()
        {
            try
            {
                if (Application.Current?.Resources == null) return;
                
                var duration = EnableAnimations ? "0:0:0.3" : "0:0:0.05";
                Application.Current.Resources["ThemeTransitionDuration"] = TimeSpan.Parse(duration);
                Application.Current.Resources["EnableAnimations"] = EnableAnimations;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying animation settings", ex);
            }
        }

        #endregion

        #region Private Methods - Auto Mode Logic

        private void CheckAutoTheme()
        {
            if (!IsAutoMode) return;

            var now = DateTime.Now.TimeOfDay;
            bool shouldBeDark = ShouldBeDarkAtTime(now);
            
            LoggingService.Instance.LogInfo($"🕐 Auto theme check: Current time: {now.Hours:00}:{now.Minutes:00}, " +
                $"Dark time: {DarkModeStartTime.Hours:00}:{DarkModeStartTime.Minutes:00}-{LightModeStartTime.Hours:00}:{LightModeStartTime.Minutes:00}, " +
                $"Should be dark: {shouldBeDark}, Currently dark: {IsDarkMode}");
            
            if (IsDarkMode != shouldBeDark)
            {
                LoggingService.Instance.LogInfo($"🎨 Auto theme switching: {(IsDarkMode ? "Dark" : "Light")} → {(shouldBeDark ? "Dark" : "Light")}");
                IsDarkMode = shouldBeDark;
            }
        }

        private bool ShouldBeDarkAtTime(TimeSpan time)
        {
            // Wenn DarkModeStartTime < LightModeStartTime bedeutet das, dass der Dark Mode über Mitternacht geht
            // z.B. Dark: 15:00, Light: 07:00 -> Dark von 15:00 bis 23:59 und 00:00 bis 06:59
            if (DarkModeStartTime > LightModeStartTime)
            {
                // Normal case: z.B. Dark: 19:00, Light: 07:00 
                // Dark von 19:00 bis 23:59 und von 00:00 bis 06:59
                return time >= DarkModeStartTime || time < LightModeStartTime;
            }
            else
            {
                // Edge case: z.B. Dark: 07:00, Light: 19:00
                // Dark von 07:00 bis 18:59
                return time >= DarkModeStartTime && time < LightModeStartTime;
            }
        }

        private TimeSpan GetNextSwitchTime()
        {
            var now = DateTime.Now.TimeOfDay;
            bool currentlyDark = ShouldBeDarkAtTime(now);
            
            if (DarkModeStartTime > LightModeStartTime)
            {
                // Normal case: Dark Mode geht über Mitternacht (z.B. 19:00 bis 07:00)
                if (currentlyDark)
                {
                    // Aktuell dunkel, nächster Wechsel ist zu Light Mode
                    if (now >= DarkModeStartTime || now < LightModeStartTime)
                    {
                        return LightModeStartTime;
                    }
                }
                else
                {
                    // Aktuell hell, nächster Wechsel ist zu Dark Mode
                    return DarkModeStartTime;
                }
            }
            else
            {
                // Edge case: Dark Mode geht NICHT über Mitternacht (z.B. 07:00 bis 19:00)
                if (currentlyDark)
                {
                    // Aktuell dunkel, nächster Wechsel ist zu Light Mode
                    return LightModeStartTime;
                }
                else
                {
                    // Aktuell hell, nächster Wechsel ist zu Dark Mode
                    return DarkModeStartTime;
                }
            }
            
            // Fallback
            return currentlyDark ? LightModeStartTime : DarkModeStartTime;
        }

        private void StartTimeCheckTimer()
        {
            if (_timeCheckTimer == null)
            {
                _timeCheckTimer = new DispatcherTimer();
                _timeCheckTimer.Interval = TimeSpan.FromSeconds(30); // Reduziere auf 30 Sekunden für bessere Reaktionszeit
                _timeCheckTimer.Tick += (s, e) => CheckAutoTheme();
            }
            
            if (!_timeCheckTimer.IsEnabled)
            {
                _timeCheckTimer.Start();
                LoggingService.Instance.LogInfo("⏱️ Auto-theme timer started (30 second intervals)");
            }
        }

        private void StopTimeCheckTimer()
        {
            if (_timeCheckTimer != null && _timeCheckTimer.IsEnabled)
            {
                _timeCheckTimer.Stop();
            }
        }

        #endregion

        #region Settings Management

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsFile)) 
                {
                    LoggingService.Instance.LogInfo($"Settings file not found: {_settingsFile}, using defaults");
                    return;
                }

                LoggingService.Instance.LogInfo($"📖 Loading theme settings from: {_settingsFile}");
                
                var json = File.ReadAllText(_settingsFile, System.Text.Encoding.UTF8);
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    LoggingService.Instance.LogWarning("Settings file is empty, using defaults");
                    return;
                }
                
                var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                
                if (settings == null) 
                {
                    LoggingService.Instance.LogWarning("Failed to deserialize settings, using defaults");
                    return;
                }

                // Lade alle Settings mit expliziter Fehlerbehandlung
                if (settings.TryGetValue("IsAutoMode", out var autoMode))
                {
                    try
                    {
                        _isAutoMode = autoMode.GetBoolean();
                        LoggingService.Instance.LogInfo($"Loaded IsAutoMode: {_isAutoMode}");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogWarning($"Error loading IsAutoMode: {ex.Message}, using default");
                    }
                }
                    
                if (settings.TryGetValue("IsDarkMode", out var darkMode))
                {
                    try
                    {
                        _isDarkMode = darkMode.GetBoolean();
                        LoggingService.Instance.LogInfo($"Loaded IsDarkMode: {_isDarkMode}");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogWarning($"Error loading IsDarkMode: {ex.Message}, using default");
                    }
                }
                    
                if (settings.TryGetValue("DarkModeStartTime", out var darkStart))
                {
                    try
                    {
                        var timeString = darkStart.GetString();
                        if (!string.IsNullOrEmpty(timeString) && TimeSpan.TryParse(timeString, out var parsedDarkStart))
                        {
                            _darkModeStartTime = parsedDarkStart;
                            LoggingService.Instance.LogInfo($"Loaded DarkModeStartTime: {_darkModeStartTime.Hours:00}:{_darkModeStartTime.Minutes:00}");
                        }
                        else
                        {
                            LoggingService.Instance.LogWarning($"Invalid DarkModeStartTime format: '{timeString}', using default");
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogWarning($"Error loading DarkModeStartTime: {ex.Message}, using default");
                    }
                }
                    
                if (settings.TryGetValue("LightModeStartTime", out var lightStart))
                {
                    try
                    {
                        var timeString = lightStart.GetString();
                        if (!string.IsNullOrEmpty(timeString) && TimeSpan.TryParse(timeString, out var parsedLightStart))
                        {
                            _lightModeStartTime = parsedLightStart;
                            LoggingService.Instance.LogInfo($"Loaded LightModeStartTime: {_lightModeStartTime.Hours:00}:{_lightModeStartTime.Minutes:00}");
                        }
                        else
                        {
                            LoggingService.Instance.LogWarning($"Invalid LightModeStartTime format: '{timeString}', using default");
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogWarning($"Error loading LightModeStartTime: {ex.Message}, using default");
                    }
                }
                    
                if (settings.TryGetValue("EnableAnimations", out var animations))
                {
                    try
                    {
                        _enableAnimations = animations.GetBoolean();
                        LoggingService.Instance.LogInfo($"Loaded EnableAnimations: {_enableAnimations}");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogWarning($"Error loading EnableAnimations: {ex.Message}, using default");
                    }
                }

                LoggingService.Instance.LogInfo($"✅ Unified theme settings loaded successfully - Auto: {_isAutoMode}, Dark: {_isDarkMode}, Times: {_darkModeStartTime.Hours:00}:{_darkModeStartTime.Minutes:00}-{_lightModeStartTime.Hours:00}:{_lightModeStartTime.Minutes:00}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error loading unified theme settings from '{_settingsFile}'", ex);
                LoggingService.Instance.LogInfo("Using default settings due to load error");
                
                // Zurück auf Standardwerte bei Fehlern
                _isAutoMode = true;
                _isDarkMode = false;
                _darkModeStartTime = new TimeSpan(19, 0, 0);
                _lightModeStartTime = new TimeSpan(7, 0, 0);
                _enableAnimations = true;
            }
        }

        private void SaveSettings()
        {
            try
            {
                // Einstellungen nur im Debounce-Modus speichern
                if (_saveSettingsTimer?.IsEnabled == true)
                {
                    LoggingService.Instance.LogInfo("⚙️ Settings save skipped (debounced)");
                    return;
                }
                
                LoggingService.Instance.LogInfo($"💾 Saving theme settings - Auto: {IsAutoMode}, Dark: {IsDarkMode}, DarkTime: {DarkModeStartTime.Hours:00}:{DarkModeStartTime.Minutes:00}, LightTime: {LightModeStartTime.Hours:00}:{LightModeStartTime.Minutes:00}");
                
                var directory = Path.GetDirectoryName(_settingsFile);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Erstelle Settings-Dictionary mit expliziter String-Konvertierung
                var settings = new Dictionary<string, object>();
                
                try
                {
                    settings["IsAutoMode"] = IsAutoMode;
                    settings["IsDarkMode"] = IsDarkMode;
                    
                    // Sichere TimeSpan-Serialisierung mit explizitem Format
                    settings["DarkModeStartTime"] = $"{DarkModeStartTime.Hours:00}:{DarkModeStartTime.Minutes:00}:00";
                    settings["LightModeStartTime"] = $"{LightModeStartTime.Hours:00}:{LightModeStartTime.Minutes:00}:00";
                    
                    settings["EnableAnimations"] = EnableAnimations;
                    
                    // Sichere DateTime-Serialisierung
                    settings["LastSaved"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("Error creating settings dictionary", ex);
                    throw;
                }

                // JSON-Serialisierung mit kulturunabhängigen Einstellungen
                var jsonOptions = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                
                var json = JsonSerializer.Serialize(settings, jsonOptions);
                File.WriteAllText(_settingsFile, json, System.Text.Encoding.UTF8);
                
                LoggingService.Instance.LogInfo($"✅ Theme settings saved successfully to: {_settingsFile}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error saving unified theme settings to '{_settingsFile}'", ex);
                LoggingService.Instance.LogError($"Settings details - Auto: {IsAutoMode}, Dark: {IsDarkMode}, " +
                    $"DarkStart: {DarkModeStartTime}, LightStart: {LightModeStartTime}, Animations: {EnableAnimations}");
            }
        }

        #endregion

        #region Settings Management - With Debouncing

        /// <summary>
        /// Debounced Settings Save - wartet 500ms nach der letzten Änderung bevor gespeichert wird
        /// </summary>
        private void DebouncedSaveSettings()
        {
            try
            {
                // Stop existing timer
                if (_saveSettingsTimer != null)
                {
                    _saveSettingsTimer.Stop();
                }
                else
                {
                    // Create timer on first use
                    _saveSettingsTimer = new DispatcherTimer();
                    _saveSettingsTimer.Interval = TimeSpan.FromMilliseconds(500); // 500ms debounce
                    _saveSettingsTimer.Tick += (s, e) =>
                    {
                        _saveSettingsTimer?.Stop();
                        SaveSettings();
                    };
                }
                
                // Start timer
                _saveSettingsTimer.Start();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error in DebouncedSaveSettings", ex);
                // Fallback: direktes Speichern
                SaveSettings();
            }
        }

        /// <summary>
        /// Forciert sofortiges Speichern (z.B. beim Dispose)
        /// </summary>
        public void ForceSaveSettings()
        {
            try
            {
                if (_saveSettingsTimer != null && _saveSettingsTimer.IsEnabled)
                {
                    _saveSettingsTimer.Stop();
                }
                SaveSettings();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error in ForceSaveSettings", ex);
            }
        }

        #endregion

        #region INotifyPropertyChanged & IDisposable

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            try
            {
                // Stop und cleanup timer
                StopTimeCheckTimer();
                
                if (_saveSettingsTimer != null)
                {
                    _saveSettingsTimer.Stop();
                    _saveSettingsTimer = null;
                }
                
                // Force final settings save
                ForceSaveSettings();
                
                LoggingService.Instance.LogInfo("UnifiedThemeManager disposed");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error disposing UnifiedThemeManager", ex);
            }
        }

        #endregion
    }

    /// <summary>
    /// Theme-Farb-Definition mit Metadaten
    /// </summary>
    public class ThemeColorDefinition
    {
        public Color Color { get; }
        public string Description { get; }
        public string HexValue { get; }

        public ThemeColorDefinition(string hexColor, string description)
        {
            HexValue = hexColor;
            Description = description;
            Color = (Color)ColorConverter.ConvertFromString(hexColor);
        }
    }

    /// <summary>
    /// Interface für Komponenten die Theme-Updates erhalten sollen
    /// </summary>
    public interface IThemeConsumer
    {
        void OnThemeChanged(bool isDarkMode);
    }
}
