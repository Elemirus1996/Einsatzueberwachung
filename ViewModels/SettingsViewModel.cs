using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Einsatzueberwachung.Services;
using Einsatzueberwachung.Models;

namespace Einsatzueberwachung.ViewModels
{
    /// <summary>
    /// ViewModel für das zentrale Einstellungen-Fenster
    /// Vereint alle wichtigen Konfigurationsoptionen an einem Ort
    /// </summary>
    public class SettingsViewModel : BaseViewModel
    {
        private bool _isDarkMode;
        private bool _isAutoMode;
        private TimeSpan _darkModeStartTime = new TimeSpan(18, 0, 0);
        private TimeSpan _lightModeStartTime = new TimeSpan(8, 0, 0);
        private int _firstWarningMinutes = 10;
        private int _secondWarningMinutes = 20;
        private bool _enableUpdateNotifications = true;
        private bool _checkForBetaUpdates = false;
        private int _mobileServerPort = 8080;
        private bool _autoStartMobileServer = false;
        private string _selectedCategory = "Darstellung";
        private bool _autoOpenMasterDataOnTeamCreation = false;
        private bool _showRecentSuggestions = true;

        public SettingsViewModel()
        {
            InitializeCommands();
            LoadCurrentSettings();
            
            Categories = new ObservableCollection<SettingsCategory>
            {
                new SettingsCategory { Name = "Darstellung", Icon = "🎨", Description = "Theme und Anzeigeoptionen" },
                new SettingsCategory { Name = "Warnungen", Icon = "⚠️", Description = "Timer-Warnzeiten konfigurieren" },
                new SettingsCategory { Name = "Mobile", Icon = "📱", Description = "Mobile Server Einstellungen" },
                new SettingsCategory { Name = "Updates", Icon = "🔄", Description = "Update-Benachrichtigungen" },
                new SettingsCategory { Name = "Stammdaten", Icon = "🗄️", Description = "Personal und Hunde verwalten" },
                new SettingsCategory { Name = "Info", Icon = "ℹ️", Description = "Hilfe und Informationen" }
            };
        }

        #region Properties

        public ObservableCollection<SettingsCategory> Categories { get; }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        // Theme Settings
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (SetProperty(ref _isDarkMode, value))
                {
                    if (!IsAutoMode)
                    {
                        ThemeService.Instance.SetDarkMode(value);
                    }
                }
            }
        }

        public bool IsAutoMode
        {
            get => _isAutoMode;
            set
            {
                if (SetProperty(ref _isAutoMode, value))
                {
                    if (value)
                    {
                        ThemeService.Instance.EnableAutoMode();
                    }
                    else
                    {
                        ThemeService.Instance.SetDarkMode(IsDarkMode);
                    }
                    OnPropertyChanged(nameof(IsManualMode));
                }
            }
        }

        public bool IsManualMode => !IsAutoMode;

        public TimeSpan DarkModeStartTime
        {
            get => _darkModeStartTime;
            set 
            { 
                if (SetProperty(ref _darkModeStartTime, value))
                {
                    // Notify all dependent properties
                    OnPropertyChanged(nameof(DarkModeHours));
                    OnPropertyChanged(nameof(DarkModeMinutes));
                    
                    // Sofort anwenden wenn Auto-Mode aktiv ist
                    if (IsAutoMode)
                    {
                        ThemeService.Instance.SetAutoModeTimes(value, LightModeStartTime);
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
                if (SetProperty(ref _lightModeStartTime, value))
                {
                    // Notify all dependent properties
                    OnPropertyChanged(nameof(LightModeHours));
                    OnPropertyChanged(nameof(LightModeMinutes));
                    
                    // Sofort anwenden wenn Auto-Mode aktiv ist
                    if (IsAutoMode)
                    {
                        ThemeService.Instance.SetAutoModeTimes(DarkModeStartTime, value);
                    }
                    
                    LoggingService.Instance.LogInfo($"Light mode start time changed to: {value:hh\\:mm}");
                }
            }
        }

        // Simplified Properties für ComboBox Binding mit 5er-Schritte-Rundung
        public string DarkModeHours
        {
            get => _darkModeStartTime.Hours.ToString("00");
            set
            {
                if (int.TryParse(value, out int hours) && hours >= 0 && hours <= 23)
                {
                    var newTime = new TimeSpan(hours, _darkModeStartTime.Minutes, 0);
                    if (newTime != _darkModeStartTime)
                    {
                        DarkModeStartTime = newTime;
                    }
                }
                OnPropertyChanged();
            }
        }

        public string DarkModeMinutes
        {
            get => RoundToNearest5(_darkModeStartTime.Minutes).ToString("00");
            set
            {
                if (int.TryParse(value, out int minutes) && minutes >= 0 && minutes <= 59)
                {
                    var newTime = new TimeSpan(_darkModeStartTime.Hours, minutes, 0);
                    if (newTime != _darkModeStartTime)
                    {
                        DarkModeStartTime = newTime;
                    }
                }
                OnPropertyChanged();
            }
        }

        public string LightModeHours
        {
            get => _lightModeStartTime.Hours.ToString("00");
            set
            {
                if (int.TryParse(value, out int hours) && hours >= 0 && hours <= 23)
                {
                    var newTime = new TimeSpan(hours, _lightModeStartTime.Minutes, 0);
                    if (newTime != _lightModeStartTime)
                    {
                        LightModeStartTime = newTime;
                    }
                }
                OnPropertyChanged();
            }
        }

        public string LightModeMinutes
        {
            get => RoundToNearest5(_lightModeStartTime.Minutes).ToString("00");
            set
            {
                if (int.TryParse(value, out int minutes) && minutes >= 0 && minutes <= 59)
                {
                    var newTime = new TimeSpan(_lightModeStartTime.Hours, minutes, 0);
                    if (newTime != _lightModeStartTime)
                    {
                        LightModeStartTime = newTime;
                    }
                }
                OnPropertyChanged();
            }
        }

        // Warning Settings
        public int FirstWarningMinutes
        {
            get => _firstWarningMinutes;
            set => SetProperty(ref _firstWarningMinutes, Math.Max(1, Math.Min(60, value)));
        }

        public int SecondWarningMinutes
        {
            get => _secondWarningMinutes;
            set => SetProperty(ref _secondWarningMinutes, Math.Max(FirstWarningMinutes + 1, Math.Min(120, value)));
        }

        // Update Settings
        public bool EnableUpdateNotifications
        {
            get => _enableUpdateNotifications;
            set => SetProperty(ref _enableUpdateNotifications, value);
        }

        public bool CheckForBetaUpdates
        {
            get => _checkForBetaUpdates;
            set => SetProperty(ref _checkForBetaUpdates, value);
        }

        // Mobile Settings
        public int MobileServerPort
        {
            get => _mobileServerPort;
            set => SetProperty(ref _mobileServerPort, Math.Max(1024, Math.Min(65535, value)));
        }

        public bool AutoStartMobileServer
        {
            get => _autoStartMobileServer;
            set => SetProperty(ref _autoStartMobileServer, value);
        }

        // Master Data Settings
        public bool AutoOpenMasterDataOnTeamCreation
        {
            get => _autoOpenMasterDataOnTeamCreation;
            set => SetProperty(ref _autoOpenMasterDataOnTeamCreation, value);
        }

        public bool ShowRecentSuggestions
        {
            get => _showRecentSuggestions;
            set => SetProperty(ref _showRecentSuggestions, value);
        }

        // Feature Highlight Settings
        public string FeatureHighlightStatus
        {
            get
            {
                try
                {
                    var (showCount, lastVersion, lastShown) = FeatureHighlightService.Instance.GetStatistics();
                    var currentVersion = VersionService.Version;
                    var willShow = FeatureHighlightService.Instance.ShouldShowFeatureHighlight();
                    
                    return $"Version {lastVersion}: {showCount}/3 mal angezeigt" +
                           (currentVersion != lastVersion ? $" (Aktuelle Version: {currentVersion})" : "") +
                           (willShow ? " - Wird beim nächsten Start angezeigt" : " - Ausgeblendet bis neue Version");
                }
                catch
                {
                    return "Status unbekannt";
                }
            }
        }

        // Status Properties
        public string CurrentThemeStatus => ThemeService.Instance.CurrentThemeStatus;
        public string AppVersion => VersionService.DisplayVersion;
        public bool IsAdministrator => IsRunningAsAdministrator();

        #endregion

        #region Commands

        public ICommand SaveSettingsCommand { get; private set; } = null!;
        public ICommand ResetToDefaultsCommand { get; private set; } = null!;
        public ICommand OpenHelpCommand { get; private set; } = null!;
        public ICommand OpenAboutCommand { get; private set; } = null!;
        public ICommand OpenMobileConnectionCommand { get; private set; } = null!;
        public ICommand CheckForUpdatesCommand { get; private set; } = null!;
        public ICommand TestMobileServerCommand { get; private set; } = null!;
        public ICommand ApplyThemeCommand { get; private set; } = null!;
        public ICommand OpenMasterDataCommand { get; private set; } = null!;
        public ICommand DebugTestCommand { get; private set; } = null!;
        public ICommand ResetFeatureHighlightCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            SaveSettingsCommand = new RelayCommand(ExecuteSaveSettings);
            ResetToDefaultsCommand = new RelayCommand(ExecuteResetToDefaults);
            OpenHelpCommand = new RelayCommand(ExecuteOpenHelp);
            OpenAboutCommand = new RelayCommand(ExecuteOpenAbout);
            OpenMobileConnectionCommand = new RelayCommand(ExecuteOpenMobileConnection);
            CheckForUpdatesCommand = new RelayCommand(ExecuteCheckForUpdates);
            TestMobileServerCommand = new RelayCommand(ExecuteTestMobileServer);
            ApplyThemeCommand = new RelayCommand(ExecuteApplyTheme);
            OpenMasterDataCommand = new RelayCommand(ExecuteOpenMasterData);
            DebugTestCommand = new RelayCommand(ExecuteDebugTest);
            ResetFeatureHighlightCommand = new RelayCommand(ExecuteResetFeatureHighlight);
            
            System.Diagnostics.Debug.WriteLine("SettingsViewModel: All commands initialized");
            System.Diagnostics.Debug.WriteLine($"OpenHelpCommand is null: {OpenHelpCommand == null}");
            System.Diagnostics.Debug.WriteLine($"CheckForUpdatesCommand is null: {CheckForUpdatesCommand == null}");
        }

        #endregion

        #region Command Implementations

        private void ExecuteSaveSettings()
        {
            try
            {
                // Theme-Einstellungen speichern
                if (IsAutoMode)
                {
                    // Benutzerdefinierte Zeiten für Auto-Mode anwenden
                    ThemeService.Instance.SetAutoModeTimes(DarkModeStartTime, LightModeStartTime);
                    ThemeService.Instance.EnableAutoMode();
                }
                else
                {
                    ThemeService.Instance.SetDarkMode(IsDarkMode);
                }

                // Warning-Einstellungen speichern
                // TODO: In Konfigurationsdatei oder Registry speichern

                // Mobile-Einstellungen speichern
                // TODO: Mobile Server Konfiguration speichern

                // Update-Einstellungen speichern
                // TODO: Update-Service Konfiguration speichern

                LoggingService.Instance.LogInfo($"Settings saved successfully - Auto Mode: {IsAutoMode}, Times: {LightModeStartTime}-{DarkModeStartTime}");
                
                // Event für Parent Window
                SettingsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error saving settings", ex);
            }
        }

        private void ExecuteResetToDefaults()
        {
            try
            {
                IsAutoMode = true;
                DarkModeStartTime = new TimeSpan(18, 0, 0);
                LightModeStartTime = new TimeSpan(8, 0, 0);
                FirstWarningMinutes = 10;
                SecondWarningMinutes = 20;
                EnableUpdateNotifications = true;
                CheckForBetaUpdates = false;
                MobileServerPort = 8080;
                AutoStartMobileServer = false;
                AutoOpenMasterDataOnTeamCreation = false;
                ShowRecentSuggestions = true;

                LoggingService.Instance.LogInfo("Settings reset to defaults");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error resetting settings", ex);
            }
        }

        private void ExecuteOpenHelp()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ExecuteOpenHelp called - raising HelpRequested event");
                HelpRequested?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening help", ex);
            }
        }

        private void ExecuteOpenAbout()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ExecuteOpenAbout called - raising AboutRequested event");
                AboutRequested?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening about", ex);
            }
        }

        private void ExecuteOpenMobileConnection()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ExecuteOpenMobileConnection called - raising MobileConnectionRequested event");
                MobileConnectionRequested?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening mobile connection", ex);
            }
        }

        private void ExecuteCheckForUpdates()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ExecuteCheckForUpdates called - raising UpdateCheckRequested event");
                UpdateCheckRequested?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error checking for updates", ex);
            }
        }

        private void ExecuteTestMobileServer()
        {
            try
            {
                MobileServerTestRequested?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error testing mobile server", ex);
            }
        }

        private void ExecuteApplyTheme()
        {
            try
            {
                // Sofortige Anwendung der Theme-Einstellungen
                if (IsAutoMode)
                {
                    // Benutzerdefinierte Zeiten anwenden
                    ThemeService.Instance.SetAutoModeTimes(DarkModeStartTime, LightModeStartTime);
                    ThemeService.Instance.EnableAutoMode();
                }
                else
                {
                    ThemeService.Instance.SetDarkMode(IsDarkMode);
                }

                OnPropertyChanged(nameof(CurrentThemeStatus));
                
                LoggingService.Instance.LogInfo($"Theme applied - Auto Mode: {IsAutoMode}, Times: {LightModeStartTime}-{DarkModeStartTime}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme", ex);
            }
        }

        private void ExecuteOpenMasterData()
        {
            try
            {
                MasterDataRequested?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening master data", ex);
            }
        }

        private void ExecuteDebugTest()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("DEBUG TEST COMMAND EXECUTED!");
                System.Windows.MessageBox.Show("DEBUG: Command funktioniert!", "Debug Test", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Debug test command error", ex);
            }
        }

        private void ExecuteResetFeatureHighlight()
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "Möchten Sie die Feature-Highlight-Anzeige zurücksetzen?\n\n" +
                    "Das Feature-Highlight wird dann wieder bei den nächsten 3 Anwendungsstarts angezeigt.",
                    "Feature-Highlight zurücksetzen",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    FeatureHighlightService.Instance.ResetShowCount();
                    OnPropertyChanged(nameof(FeatureHighlightStatus));
                    
                    LoggingService.Instance.LogInfo("Feature highlight reset by user via settings");
                    
                    System.Windows.MessageBox.Show(
                        "Feature-Highlight wurde zurückgesetzt!\n\n" +
                        "Es wird bei den nächsten 3 Anwendungsstarts wieder angezeigt.",
                        "Zurückgesetzt",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error resetting feature highlight", ex);
                System.Windows.MessageBox.Show(
                    $"Fehler beim Zurücksetzen der Feature-Highlight-Anzeige:\n\n{ex.Message}",
                    "Fehler",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        #endregion

        #region Events

        public event Action? SettingsChanged;
        public event Action? HelpRequested;
        public event Action? AboutRequested;
        public event Action? MobileConnectionRequested;
        public event Action? UpdateCheckRequested;
        public event Action? MobileServerTestRequested;
        public event Action? MasterDataRequested;

        #endregion

        #region Private Methods

        // Hilfsmethode um Minuten auf 5er-Schritte zu runden
        private static int RoundToNearest5(int minutes)
        {
            return (int)(Math.Round(minutes / 5.0) * 5);
        }

        private void LoadCurrentSettings()
        {
            try
            {
                // Lade aktuelle Theme-Einstellungen
                _isDarkMode = ThemeService.Instance.IsDarkMode;
                _isAutoMode = ThemeService.Instance.IsAutoMode;
                _darkModeStartTime = ThemeService.Instance.DarkModeStartTime;
                _lightModeStartTime = ThemeService.Instance.LightModeStartTime;

                // Notify all time-related properties
                OnPropertyChanged(nameof(DarkModeStartTime));
                OnPropertyChanged(nameof(LightModeStartTime));
                OnPropertyChanged(nameof(DarkModeHours));
                OnPropertyChanged(nameof(DarkModeMinutes));
                OnPropertyChanged(nameof(LightModeHours));
                OnPropertyChanged(nameof(LightModeMinutes));
                OnPropertyChanged(nameof(FeatureHighlightStatus));

                // TODO: Lade andere Einstellungen aus Konfigurationsdatei
                
                // Theme Service Events abonnieren
                ThemeService.Instance.ThemeChanged += OnThemeChanged;

                LoggingService.Instance.LogInfo("Settings loaded successfully");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading settings", ex);
            }
        }

        private void OnThemeChanged(bool isDarkMode)
        {
            _isDarkMode = isDarkMode;
            _isAutoMode = ThemeService.Instance.IsAutoMode;
            
            OnPropertyChanged(nameof(IsDarkMode));
            OnPropertyChanged(nameof(IsAutoMode));
            OnPropertyChanged(nameof(IsManualMode));
            OnPropertyChanged(nameof(CurrentThemeStatus));
        }

        private bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (ThemeService.Instance != null)
            {
                ThemeService.Instance.ThemeChanged -= OnThemeChanged;
            }
        }

        #endregion
    }

    /// <summary>
    /// Repräsentiert eine Einstellungs-Kategorie
    /// </summary>
    public class SettingsCategory
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
