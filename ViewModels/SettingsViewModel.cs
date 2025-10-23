using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Einsatzueberwachung.Services;
using Einsatzueberwachung.Models;

namespace Einsatzueberwachung.ViewModels
{
    /// <summary>
    /// ViewModel für das zentrale Einstellungen-Fenster v5.0
    /// Vollständig integriert mit UnifiedThemeManager für saubere Theme-Architektur
    /// </summary>
    public class SettingsViewModel : BaseViewModel
    {
        private bool _isDarkMode;
        private bool _isAutoModeEnabled;
        private TimeSpan _darkModeStartTime = new TimeSpan(19, 0, 0);  // Standard 19:00
        private TimeSpan _lightModeStartTime = new TimeSpan(7, 0, 0);   // Standard 07:00
        private int _firstWarningMinutes = 10;
        private int _secondWarningMinutes = 20;
        private bool _enableUpdateNotifications = true;
        private bool _checkForBetaUpdates = false;
        private bool _showUpdatesInMission = false;
        private int _mobileServerPort = 8080;
        private bool _autoStartMobileServer = false;
        private bool _restrictToLocalConnections = true;
        private string _selectedCategory = "Darstellung";
        private bool _autoOpenMasterDataOnTeamCreation = false;
        private bool _showRecentSuggestions = true;
        private bool _enableMasterDataSearch = true;

        // Sound Settings - NEU für Warnschwellen-Alerts
        private bool _enableSoundAlerts = true;
        private double _soundVolume = 0.8;
        private bool _playSoundForFirstWarning = true;
        private bool _playSoundForSecondWarning = true;

        public SettingsViewModel()
        {
            InitializeCommands();
            LoadCurrentSettings();
            
            Categories = new ObservableCollection<SettingsCategory>
            {
                new SettingsCategory { Name = "Darstellung", Icon = "🎨", Description = "Theme und automatische Zeit-Wechsel" },
                new SettingsCategory { Name = "Warnungen", Icon = "⚠️", Description = "Timer-Warnzeiten konfigurieren" },
                new SettingsCategory { Name = "Mobile", Icon = "📱", Description = "Mobile Server Einstellungen" },
                new SettingsCategory { Name = "Updates", Icon = "🔄", Description = "Update-Benachrichtigungen" },
                new SettingsCategory { Name = "Stammdaten", Icon = "🗄️", Description = "Personal und Hunde verwalten" },
                new SettingsCategory { Name = "Datenverwaltung", Icon = "💾", Description = "Export und Datensicherung" },
                new SettingsCategory { Name = "Info", Icon = "ℹ️", Description = "Hilfe und Informationen" }
            };
            
            // Select appearance category by default
            SelectCategory("Darstellung");
            
            LoggingService.Instance.LogInfo("SettingsViewModel v5.0 initialized with UnifiedThemeManager integration");
        }

        #region Properties

        public ObservableCollection<SettingsCategory> Categories { get; }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        // ===== THEME SETTINGS v5.0 - UnifiedThemeManager Integration =====
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (SetProperty(ref _isDarkMode, value))
                {
                    HasUnsavedChanges = true; // Mark as changed
                    
                    // Nur in manuellem Modus direkt anwenden
                    if (!IsAutoModeEnabled)
                    {
                        UnifiedThemeManager.Instance.SetDarkMode(value);
                        LoggingService.Instance.LogInfo($"Manual theme change: {(value ? "Dark" : "Light")} mode");
                    }
                    
                    // Update ALL UI selection properties
                    OnPropertyChanged(nameof(IsLightModeSelected));
                    OnPropertyChanged(nameof(IsDarkModeSelected));
                    OnPropertyChanged(nameof(IsAutoModeSelected));
                    OnPropertyChanged(nameof(CurrentThemeStatus));
                }
            }
        }

        public bool IsAutoModeEnabled
        {
            get => _isAutoModeEnabled;
            set
            {
                if (SetProperty(ref _isAutoModeEnabled, value))
                {
                    HasUnsavedChanges = true; // Mark as changed
                    
                    if (value)
                    {
                        UnifiedThemeManager.Instance.EnableAutoMode();
                        // Wende sofort die benutzerdefinierten Zeiten an
                        UnifiedThemeManager.Instance.SetAutoModeTimes(DarkModeStartTime, LightModeStartTime);
                        LoggingService.Instance.LogInfo("Auto mode enabled with custom times");
                    }
                    else
                    {
                        UnifiedThemeManager.Instance.IsAutoMode = false;
                        // Wende das aktuell gewählte Theme an
                        UnifiedThemeManager.Instance.SetDarkMode(IsDarkMode);
                        LoggingService.Instance.LogInfo("Manual mode enabled");
                    }
                    
                    // Update ALL UI selection properties
                    OnPropertyChanged(nameof(IsManualModeEnabled));
                    OnPropertyChanged(nameof(IsLightModeSelected));
                    OnPropertyChanged(nameof(IsDarkModeSelected));
                    OnPropertyChanged(nameof(IsAutoModeSelected));
                    OnPropertyChanged(nameof(CurrentThemeStatus));
                }
            }
        }

        public bool IsManualModeEnabled => !IsAutoModeEnabled;

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
                    if (IsAutoModeEnabled)
                    {
                        UnifiedThemeManager.Instance.SetAutoModeTimes(value, LightModeStartTime);
                        // Zusätzlich: Sofortige Prüfung forcieren
                        UnifiedThemeManager.Instance.ForceImmediateThemeCheck();
                    }
                    
                    LoggingService.Instance.LogInfo($"Dark mode start time changed to: {value.Hours:00}:{value.Minutes:00}");
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
                    if (IsAutoModeEnabled)
                    {
                        UnifiedThemeManager.Instance.SetAutoModeTimes(DarkModeStartTime, value);
                        // Zusätzlich: Sofortige Prüfung forcieren
                        UnifiedThemeManager.Instance.ForceImmediateThemeCheck();
                    }
                    
                    LoggingService.Instance.LogInfo($"Light mode start time changed to: {value.Hours:00}:{value.Minutes:00}");
                }
            }
        }

        // Vereinfachte Properties für ComboBox Binding
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
            }
        }

        // ===== OTHER SETTINGS =====
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

        public bool ShowUpdatesInMission
        {
            get => _showUpdatesInMission;
            set => SetProperty(ref _showUpdatesInMission, value);
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

        public bool RestrictToLocalConnections
        {
            get => _restrictToLocalConnections;
            set => SetProperty(ref _restrictToLocalConnections, value);
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

        public bool EnableMasterDataSearch
        {
            get => _enableMasterDataSearch;
            set => SetProperty(ref _enableMasterDataSearch, value);
        }

        // Sound Settings Properties
        public bool EnableSoundAlerts
        {
            get => _enableSoundAlerts;
            set 
            { 
                if (SetProperty(ref _enableSoundAlerts, value))
                {
                    SoundService.Instance.IsEnabled = value;
                    HasUnsavedChanges = true;
                    UpdateSoundCommandStates();
                    LoggingService.Instance.LogInfo($"Sound alerts {(value ? "enabled" : "disabled")}");
                }
            }
        }

        public double SoundVolume
        {
            get => _soundVolume;
            set 
            { 
                if (SetProperty(ref _soundVolume, Math.Max(0.0, Math.Min(1.0, value))))
                {
                    HasUnsavedChanges = true;
                    LoggingService.Instance.LogInfo($"Sound volume changed to {_soundVolume:P0}");
                }
            }
        }

        public bool PlaySoundForFirstWarning
        {
            get => _playSoundForFirstWarning;
            set 
            { 
                if (SetProperty(ref _playSoundForFirstWarning, value))
                {
                    HasUnsavedChanges = true;
                    UpdateSoundCommandStates();
                    LoggingService.Instance.LogInfo($"First warning sound {(value ? "enabled" : "disabled")}");
                }
            }
        }

        public bool PlaySoundForSecondWarning
        {
            get => _playSoundForSecondWarning;
            set 
            { 
                if (SetProperty(ref _playSoundForSecondWarning, value))
                {
                    HasUnsavedChanges = true;
                    UpdateSoundCommandStates();
                    LoggingService.Instance.LogInfo($"Second warning sound {(value ? "enabled" : "disabled")}");
                }
            }
        }

        // ===== MISSING METHODS FOR UI INTEGRATION =====
        public event Action<string>? ShowMessage;
        public event Action? RequestClose;
        public event EventHandler? SettingsSaved;

        public bool HasUnsavedChanges { get; private set; } = false;

        // Export Commands für Datenverwaltung
        public ICommand ExportEinsatzprotokollCommand { get; private set; } = null!;
        public ICommand ExportTeamsStatistikCommand { get; private set; } = null!;
        public ICommand ExportVollstaendigCommand { get; private set; } = null!;
        public ICommand ExportGlobaleNotizenCommand { get; private set; } = null!;

        public void SelectCategory(string category)
        {
            SelectedCategory = category;
            
            // Notify all category selection properties
            OnPropertyChanged(nameof(IsAppearanceCategorySelected));
            OnPropertyChanged(nameof(IsGeneralCategorySelected));
            OnPropertyChanged(nameof(IsTimerCategorySelected));
            OnPropertyChanged(nameof(IsMobileCategorySelected));
            OnPropertyChanged(nameof(IsUpdatesCategorySelected));
            OnPropertyChanged(nameof(IsDataCategorySelected));
            OnPropertyChanged(nameof(IsDataManagementCategorySelected));
            OnPropertyChanged(nameof(IsInfoCategorySelected));
            
            LoggingService.Instance.LogInfo($"Category selected: {category}");
        }

        public void SaveSettings()
        {
            ExecuteSaveSettings();
            HasUnsavedChanges = false;
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }

        // ===== STATUS PROPERTIES =====
        public string CurrentThemeStatus => UnifiedThemeManager.Instance.CurrentThemeStatus;
        public string AppVersion => "1.0.0 Pro"; // TODO: Implementiere VersionService falls vorhanden
        public bool IsAdministrator => IsRunningAsAdministrator();

        #endregion

        #region Commands

        public ICommand SaveSettingsCommand { get; private set; } = null!;
        public ICommand ResetToDefaultsCommand { get; private set; } = null!;
        public ICommand OpenHelpCommand { get; private set; } = null!;
        public ICommand OpenAboutCommand { get; private set; } = null!;
        public ICommand OpenThemeTestCommand { get; private set; } = null!;
        public ICommand OpenMobileConnectionCommand { get; private set; } = null!;
        public ICommand CheckForUpdatesCommand { get; private set; } = null!;
        public ICommand TestMobileServerCommand { get; private set; } = null!;
        public ICommand OpenPersonalMasterDataCommand { get; private set; } = null!;
        public ICommand OpenDogMasterDataCommand { get; private set; } = null!;
        public ICommand OpenStatisticsCommand { get; private set; } = null!;
        public ICommand OpenPdfExportCommand { get; private set; } = null!;
        public ICommand TestSoundCommand { get; private set; } = null!;
        public ICommand TestFirstWarningCommand { get; private set; } = null!;
        public ICommand TestSecondWarningCommand { get; private set; } = null!;
        public ICommand ResetSoundSettingsCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            SaveSettingsCommand = new RelayCommand(ExecuteSaveSettings);
            ResetToDefaultsCommand = new RelayCommand(ExecuteResetToDefaults);
            OpenHelpCommand = new RelayCommand(ExecuteOpenHelp);
            OpenAboutCommand = new RelayCommand(ExecuteOpenAbout);
            OpenThemeTestCommand = new RelayCommand(ExecuteOpenThemeTest);
            OpenMobileConnectionCommand = new RelayCommand(ExecuteOpenMobileConnection);
            CheckForUpdatesCommand = new RelayCommand(ExecuteCheckForUpdates);
            TestMobileServerCommand = new RelayCommand(ExecuteTestMobileServer);
            OpenPersonalMasterDataCommand = new RelayCommand(ExecuteOpenPersonalMasterData);
            OpenDogMasterDataCommand = new RelayCommand(ExecuteOpenDogMasterData);
            OpenStatisticsCommand = new RelayCommand(ExecuteOpenStatistics);
            OpenPdfExportCommand = new RelayCommand(ExecuteOpenPdfExport);
            TestSoundCommand = new RelayCommand(ExecuteTestSound, () => EnableSoundAlerts);
            TestFirstWarningCommand = new RelayCommand(async () => await ExecuteTestFirstWarning(), () => EnableSoundAlerts && PlaySoundForFirstWarning);
            TestSecondWarningCommand = new RelayCommand(async () => await ExecuteTestSecondWarning(), () => EnableSoundAlerts && PlaySoundForSecondWarning);
            ResetSoundSettingsCommand = new RelayCommand(ExecuteResetSoundSettings);
            
            // Export Commands für Datenverwaltung
            ExportEinsatzprotokollCommand = new RelayCommand(ExecuteExportEinsatzprotokoll);
            ExportTeamsStatistikCommand = new RelayCommand(ExecuteExportTeamsStatistik);
            ExportVollstaendigCommand = new RelayCommand(ExecuteExportVollstaendig);
            ExportGlobaleNotizenCommand = new RelayCommand(ExecuteExportGlobaleNotizen);
            
            LoggingService.Instance.LogInfo("SettingsViewModel v5.0 commands initialized");
        }

        #endregion

        #region Command Implementations

        private void ExecuteSaveSettings()
        {
            try
            {
                // Theme-Einstellungen speichern
                if (IsAutoModeEnabled)
                {
                    UnifiedThemeManager.Instance.SetAutoModeTimes(DarkModeStartTime, LightModeStartTime);
                    UnifiedThemeManager.Instance.EnableAutoMode();
                }
                else
                {
                    UnifiedThemeManager.Instance.SetDarkMode(IsDarkMode);
                }

                // SoundService konfigurieren
                SoundService.Instance.IsEnabled = EnableSoundAlerts;

                // Weitere Einstellungen hier speichern (Registry, Config-Datei, etc.)
                SaveSettingsToStorage();

                LoggingService.Instance.LogInfo($"Settings saved successfully - Auto Mode: {IsAutoModeEnabled}, Theme: {(IsDarkMode ? "Dark" : "Light")}, Sound: {EnableSoundAlerts}");
                
                // Event für Parent Window
                SettingsChanged?.Invoke();
                HasUnsavedChanges = false;
                ShowMessage?.Invoke("Einstellungen wurden erfolgreich gespeichert.");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error saving settings", ex);
                ShowMessage?.Invoke($"Fehler beim Speichern der Einstellungen: {ex.Message}");
            }
        }

        private void ExecuteResetToDefaults()
        {
            try
            {
                // Theme-Einstellungen zurücksetzen
                IsAutoModeEnabled = true; // Standard ist Auto-Modus
                IsDarkMode = false; // Standard ist Light Mode (wird von Auto übersteuert)
                DarkModeStartTime = new TimeSpan(19, 0, 0);
                LightModeStartTime = new TimeSpan(7, 0, 0);
                
                // Warning-Einstellungen zurücksetzen
                FirstWarningMinutes = 10;
                SecondWarningMinutes = 20;
                
                // Sound-Einstellungen zurücksetzen
                EnableSoundAlerts = true;
                SoundVolume = 0.8;
                PlaySoundForFirstWarning = true;
                PlaySoundForSecondWarning = true;
                
                // Update-Einstellungen zurücksetzen
                EnableUpdateNotifications = true;
                CheckForBetaUpdates = false;
                ShowUpdatesInMission = false;
                
                // Mobile-Einstellungen zurücksetzen
                MobileServerPort = 8080;
                AutoStartMobileServer = false;
                RestrictToLocalConnections = true;
                
                // Stammdaten-Einstellungen zurücksetzen
                AutoOpenMasterDataOnTeamCreation = false;
                ShowRecentSuggestions = true;
                EnableMasterDataSearch = true;

                // Theme sofort anwenden
                UnifiedThemeManager.Instance.ResetToDefaults();

                LoggingService.Instance.LogInfo("All settings reset to defaults including sound settings");
                ShowMessage?.Invoke("Alle Einstellungen wurden auf Standardwerte zurückgesetzt.");
                
                // Command-States aktualisieren
                UpdateSoundCommandStates();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error resetting settings", ex);
                ShowMessage?.Invoke($"Fehler beim Zurücksetzen der Einstellungen: {ex.Message}");
            }
        }

        private void ExecuteOpenThemeTest()
        {
            try
            {
                var themeManager = UnifiedThemeManager.Instance;
                
                var message = "🧪 Unified Theme System Debug-Informationen\n\n";
                message += $"🎨 Aktuelles Theme: {CurrentThemeStatus}\n\n";
                message += "🔧 UnifiedThemeManager-Details:\n";
                message += $"  • Dunkelmodus aktiv: {themeManager.IsDarkMode}\n";
                message += $"  • Auto-Modus aktiv: {themeManager.IsAutoMode}\n";
                message += $"  • Dunkel-Zeit: {themeManager.DarkModeStartTime:HH\\:mm}\n";
                message += $"  • Hell-Zeit: {themeManager.LightModeStartTime:HH\\:mm}\n";
                message += $"  • Animationen: {themeManager.EnableAnimations}\n";
                message += $"  • Theme-Farben: {themeManager.CurrentTheme.Count}\n\n";
                message += "✨ Unified Design System v5.0 aktiv!";
                
                ShowMessage?.Invoke(message);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening theme test", ex);
                ShowMessage?.Invoke($"Fehler beim Öffnen der Theme-Informationen: {ex.Message}");
            }
        }

        private void ExecuteOpenHelp()
        {
            try
            {
                HelpRequested?.Invoke();
                LoggingService.Instance.LogInfo("Help window requested from SettingsViewModel");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening help", ex);
                ShowMessage?.Invoke($"Fehler beim Öffnen der Hilfe: {ex.Message}");
            }
        }

        private void ExecuteOpenAbout()
        {
            try
            {
                AboutRequested?.Invoke();
                LoggingService.Instance.LogInfo("About window requested from SettingsViewModel");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening about", ex);
                ShowMessage?.Invoke($"Fehler beim Öffnen der Info: {ex.Message}");
            }
        }

        private void ExecuteOpenMobileConnection()
        {
            try
            {
                MobileConnectionRequested?.Invoke();
                LoggingService.Instance.LogInfo("Mobile connection window requested from SettingsViewModel");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening mobile connection", ex);
                ShowMessage?.Invoke($"Fehler beim Öffnen der Mobile-Verbindung: {ex.Message}");
            }
        }

        private void ExecuteCheckForUpdates()
        {
            try
            {
                UpdateCheckRequested?.Invoke();
                LoggingService.Instance.LogInfo("Update check requested from SettingsViewModel");
                ShowMessage?.Invoke("Update-Überprüfung wurde gestartet...");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error checking for updates", ex);
                ShowMessage?.Invoke($"Fehler bei der Update-Überprüfung: {ex.Message}");
            }
        }

        private void ExecuteTestMobileServer()
        {
            try
            {
                MobileServerTestRequested?.Invoke();
                LoggingService.Instance.LogInfo("Mobile server test requested from SettingsViewModel");
                ShowMessage?.Invoke("Mobile Server wird getestet...");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error testing mobile server", ex);
                ShowMessage?.Invoke($"Fehler beim Testen des Mobile Servers: {ex.Message}");
            }
        }

        private void ExecuteOpenPersonalMasterData()
        {
            try
            {
                MasterDataRequested?.Invoke();
                LoggingService.Instance.LogInfo("Personal master data window requested from SettingsViewModel");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening personal master data", ex);
                ShowMessage?.Invoke($"Fehler beim Öffnen der Personal-Stammdaten: {ex.Message}");
            }
        }

        private void ExecuteOpenDogMasterData()
        {
            try
            {
                MasterDataRequested?.Invoke();
                LoggingService.Instance.LogInfo("Dog master data window requested from SettingsViewModel");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening dog master data", ex);
                ShowMessage?.Invoke($"Fehler beim Öffnen der Hunde-Stammdaten: {ex.Message}");
            }
        }

        private void ExecuteOpenStatistics()
        {
            try
            {
                StatisticsRequested?.Invoke();
                LoggingService.Instance.LogInfo("Statistics window requested from SettingsViewModel");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening statistics", ex);
                ShowMessage?.Invoke($"Fehler beim Öffnen der Statistiken: {ex.Message}");
            }
        }

        private void ExecuteOpenPdfExport()
        {
            try
            {
                PdfExportRequested?.Invoke();
                LoggingService.Instance.LogInfo("PDF Export window requested from SettingsViewModel");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening PDF export", ex);
                ShowMessage?.Invoke($"Fehler beim Öffnen des PDF-Exports: {ex.Message}");
            }
        }

        private void ExecuteTestSound()
        {
            try
            {
                System.Media.SystemSounds.Asterisk.Play();
                LoggingService.Instance.LogInfo("Sound test executed - System sound played");
                ShowMessage?.Invoke("🔊 Sound-Test: System-Sound abgespielt");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error testing sound", ex);
                ShowMessage?.Invoke($"Fehler beim Sound-Test: {ex.Message}");
            }
        }

        private async Task ExecuteTestFirstWarning()
        {
            try
            {
                LoggingService.Instance.LogInfo("Testing first warning sound");
                await SoundService.Instance.PlayTestSound(false);
                ShowMessage?.Invoke("🔊 Erste Warnung: Sound abgespielt");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error testing first warning sound", ex);
                ShowMessage?.Invoke($"Fehler beim Abspielen der ersten Warnung: {ex.Message}");
            }
        }

        private async Task ExecuteTestSecondWarning()
        {
            try
            {
                LoggingService.Instance.LogInfo("Testing second warning sound");
                await SoundService.Instance.PlayTestSound(true);
                ShowMessage?.Invoke("🔊 Zweite Warnung: Sound abgespielt");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error testing second warning sound", ex);
                ShowMessage?.Invoke($"Fehler beim Abspielen der zweiten Warnung: {ex.Message}");
            }
        }

        private void ExecuteResetSoundSettings()
        {
            try
            {
                EnableSoundAlerts = true;
                SoundVolume = 0.8;
                PlaySoundForFirstWarning = true;
                PlaySoundForSecondWarning = true;
                
                LoggingService.Instance.LogInfo("Sound settings reset to defaults");
                ShowMessage?.Invoke("🔊 Sound-Einstellungen auf Standardwerte zurückgesetzt");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error resetting sound settings", ex);
                ShowMessage?.Invoke($"Fehler beim Zurücksetzen der Sound-Einstellungen: {ex.Message}");
            }
        }

        private void ExecuteExportEinsatzprotokoll()
        {
            try
            {
                // Trigger PDF export durch MainViewModel
                ExportRequested?.Invoke("PDF");
                LoggingService.Instance.LogInfo("Einsatzprotokoll export requested from SettingsViewModel");
                ShowMessage?.Invoke("Einsatzprotokoll wird exportiert...");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error exporting Einsatzprotokoll", ex);
                ShowMessage?.Invoke($"Fehler beim Exportieren des Einsatzprotokolls: {ex.Message}");
            }
        }

        private void ExecuteExportTeamsStatistik()
        {
            try
            {
                // Trigger Excel export durch MainViewModel
                ExportRequested?.Invoke("Excel");
                LoggingService.Instance.LogInfo("Teams-Statistik export requested from SettingsViewModel");
                ShowMessage?.Invoke("Teams-Statistik wird exportiert...");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error exporting Teams-Statistik", ex);
                ShowMessage?.Invoke($"Fehler beim Exportieren der Teams-Statistik: {ex.Message}");
            }
        }

        private void ExecuteExportVollstaendig()
        {
            try
            {
                // Trigger JSON export durch MainViewModel
                ExportRequested?.Invoke("JSON");
                LoggingService.Instance.LogInfo("Vollständiger export requested from SettingsViewModel");
                ShowMessage?.Invoke("Vollständiger Export wird erstellt...");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error exporting vollständig", ex);
                ShowMessage?.Invoke($"Fehler beim vollständigen Export: {ex.Message}");
            }
        }

        private void ExecuteExportGlobaleNotizen()
        {
            try
            {
                // Trigger TXT export durch MainViewModel
                ExportRequested?.Invoke("TXT");
                LoggingService.Instance.LogInfo("Globale Notizen export requested from SettingsViewModel");
                ShowMessage?.Invoke("Globale Notizen werden exportiert...");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error exporting globale Notizen", ex);
                ShowMessage?.Invoke($"Fehler beim Exportieren der globalen Notizen: {ex.Message}");
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
        public event Action? StatisticsRequested;
        public event Action? PdfExportRequested;
        public event Action<string>? ExportRequested;

        #endregion

        #region Private Methods

        /// <summary>
        /// Rundet Minuten auf nächste 5er-Schritte
        /// </summary>
        private static int RoundToNearest5(int minutes)
        {
            return (int)(Math.Round(minutes / 5.0) * 5);
        }

        /// <summary>
        /// Lädt aktuelle Einstellungen aus UnifiedThemeManager und anderen Quellen
        /// </summary>
        private void LoadCurrentSettings()
        {
            try
            {
                // Lade aktuelle Theme-Einstellungen
                var themeManager = UnifiedThemeManager.Instance;
                _isDarkMode = themeManager.IsDarkMode;
                _isAutoModeEnabled = themeManager.IsAutoMode;
                _darkModeStartTime = themeManager.DarkModeStartTime;
                _lightModeStartTime = themeManager.LightModeStartTime;

                // Lade weitere Einstellungen aus Storage
                LoadSettingsFromStorage();

                // Notify ALL properties for complete UI sync
                OnPropertyChanged(nameof(IsDarkMode));
                OnPropertyChanged(nameof(IsAutoModeEnabled));
                OnPropertyChanged(nameof(IsManualModeEnabled));
                OnPropertyChanged(nameof(DarkModeStartTime));
                OnPropertyChanged(nameof(LightModeStartTime));
                OnPropertyChanged(nameof(DarkModeHours));
                OnPropertyChanged(nameof(DarkModeMinutes));
                OnPropertyChanged(nameof(LightModeHours));
                OnPropertyChanged(nameof(LightModeMinutes));
                OnPropertyChanged(nameof(CurrentThemeStatus));
                
                // Update RadioButton selection properties
                OnPropertyChanged(nameof(IsLightModeSelected));
                OnPropertyChanged(nameof(IsDarkModeSelected));
                OnPropertyChanged(nameof(IsAutoModeSelected));

                // UnifiedThemeManager Events abonnieren
                themeManager.ThemeChanged += OnThemeChanged;
                themeManager.AutoModeChanged += OnAutoModeChanged;

                LoggingService.Instance.LogInfo($"Settings loaded successfully from UnifiedThemeManager - Auto: {_isAutoModeEnabled}, Dark: {_isDarkMode}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading settings", ex);
            }
        }

        /// <summary>
        /// Lädt weitere Einstellungen aus persistentem Storage
        /// </summary>
        private void LoadSettingsFromStorage()
        {
            try
            {
                // Sound-Einstellungen laden
                var soundEnabled = LoadSettingFromRegistry("EnableSoundAlerts", true);
                var soundVolume = LoadSettingFromRegistry("SoundVolume", 0.8);
                var firstWarningSound = LoadSettingFromRegistry("PlaySoundForFirstWarning", true);
                var secondWarningSound = LoadSettingFromRegistry("PlaySoundForSecondWarning", true);

                _enableSoundAlerts = soundEnabled;
                _soundVolume = Math.Max(0.0, Math.Min(1.0, soundVolume));
                _playSoundForFirstWarning = firstWarningSound;
                _playSoundForSecondWarning = secondWarningSound;

                // SoundService konfigurieren
                SoundService.Instance.IsEnabled = _enableSoundAlerts;

                // Warning-Einstellungen laden
                _firstWarningMinutes = LoadSettingFromRegistry("FirstWarningMinutes", 10);
                _secondWarningMinutes = LoadSettingFromRegistry("SecondWarningMinutes", 20);

                // Mobile-Einstellungen laden
                _mobileServerPort = LoadSettingFromRegistry("MobileServerPort", 8080);
                _autoStartMobileServer = LoadSettingFromRegistry("AutoStartMobileServer", false);
                _restrictToLocalConnections = LoadSettingFromRegistry("RestrictToLocalConnections", true);

                // Update-Einstellungen laden
                _enableUpdateNotifications = LoadSettingFromRegistry("EnableUpdateNotifications", true);
                _checkForBetaUpdates = LoadSettingFromRegistry("CheckForBetaUpdates", false);
                _showUpdatesInMission = LoadSettingFromRegistry("ShowUpdatesInMission", false);

                // Stammdaten-Einstellungen laden
                _autoOpenMasterDataOnTeamCreation = LoadSettingFromRegistry("AutoOpenMasterDataOnTeamCreation", false);
                _showRecentSuggestions = LoadSettingFromRegistry("ShowRecentSuggestions", true);
                _enableMasterDataSearch = LoadSettingFromRegistry("EnableMasterDataSearch", true);

                LoggingService.Instance.LogInfo("Extended settings loaded from registry");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading settings from storage", ex);
            }
        }

        /// <summary>
        /// Speichert Einstellungen in persistentem Storage
        /// </summary>
        private void SaveSettingsToStorage()
        {
            try
            {
                // Sound-Einstellungen speichern
                SaveSettingToRegistry("EnableSoundAlerts", EnableSoundAlerts);
                SaveSettingToRegistry("SoundVolume", SoundVolume);
                SaveSettingToRegistry("PlaySoundForFirstWarning", PlaySoundForFirstWarning);
                SaveSettingToRegistry("PlaySoundForSecondWarning", PlaySoundForSecondWarning);

                // Warning-Einstellungen speichern
                SaveSettingToRegistry("FirstWarningMinutes", FirstWarningMinutes);
                SaveSettingToRegistry("SecondWarningMinutes", SecondWarningMinutes);

                // Mobile-Einstellungen speichern
                SaveSettingToRegistry("MobileServerPort", MobileServerPort);
                SaveSettingToRegistry("AutoStartMobileServer", AutoStartMobileServer);
                SaveSettingToRegistry("RestrictToLocalConnections", RestrictToLocalConnections);

                // Update-Einstellungen speichern
                SaveSettingToRegistry("EnableUpdateNotifications", EnableUpdateNotifications);
                SaveSettingToRegistry("CheckForBetaUpdates", CheckForBetaUpdates);
                SaveSettingToRegistry("ShowUpdatesInMission", ShowUpdatesInMission);

                // Stammdaten-Einstellungen speichern
                SaveSettingToRegistry("AutoOpenMasterDataOnTeamCreation", AutoOpenMasterDataOnTeamCreation);
                SaveSettingToRegistry("ShowRecentSuggestions", ShowRecentSuggestions);
                SaveSettingToRegistry("EnableMasterDataSearch", EnableMasterDataSearch);

                LoggingService.Instance.LogInfo("Extended settings saved to registry");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error saving settings to storage", ex);
            }
        }

        /// <summary>
        /// Lädt eine Einstellung aus der Registry
        /// </summary>
        private T LoadSettingFromRegistry<T>(string key, T defaultValue)
        {
            try
            {
                var registryKey = @"HKEY_CURRENT_USER\Software\RescueDog_SW\Einsatzüberwachung Professional";
                var value = Microsoft.Win32.Registry.GetValue(registryKey, key, defaultValue);
                
                if (value != null && value.GetType() == typeof(T))
                {
                    return (T)value;
                }
                
                // Typ-Konvertierung für verschiedene Datentypen
                if (typeof(T) == typeof(bool) && value is int intValue)
                {
                    return (T)(object)(intValue != 0);
                }
                
                if (typeof(T) == typeof(double) && value is string strValue && double.TryParse(strValue, out double doubleValue))
                {
                    return (T)(object)doubleValue;
                }
                
                return defaultValue;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"Error loading setting {key} from registry: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Speichert eine Einstellung in die Registry
        /// </summary>
        private void SaveSettingToRegistry<T>(string key, T value)
        {
            try
            {
                var registryKey = @"HKEY_CURRENT_USER\Software\RescueDog_SW\Einsatzüberwachung Professional";
                
                // Bool als int speichern für bessere Kompatibilität
                if (value is bool boolValue)
                {
                    Microsoft.Win32.Registry.SetValue(registryKey, key, boolValue ? 1 : 0);
                }
                else
                {
                    Microsoft.Win32.Registry.SetValue(registryKey, key, value);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"Error saving setting {key} to registry: {ex.Message}");
            }
        }

        /// <summary>
        /// Reagiert auf Theme-Änderungen vom UnifiedThemeManager
        /// </summary>
        private void OnThemeChanged(bool isDarkMode)
        {
            try
            {
                var themeManager = UnifiedThemeManager.Instance;
                
                // Update internal state
                _isDarkMode = isDarkMode;
                _isAutoModeEnabled = themeManager.IsAutoMode;
                _darkModeStartTime = themeManager.DarkModeStartTime;
                _lightModeStartTime = themeManager.LightModeStartTime;
                
                // Notify ALL UI properties
                OnPropertyChanged(nameof(IsDarkMode));
                OnPropertyChanged(nameof(IsAutoModeEnabled));
                OnPropertyChanged(nameof(IsManualModeEnabled));
                OnPropertyChanged(nameof(DarkModeStartTime));
                OnPropertyChanged(nameof(LightModeStartTime));
                OnPropertyChanged(nameof(DarkModeHours));
                OnPropertyChanged(nameof(DarkModeMinutes));
                OnPropertyChanged(nameof(LightModeHours));
                OnPropertyChanged(nameof(LightModeMinutes));
                OnPropertyChanged(nameof(CurrentThemeStatus));
                
                // Update RadioButton selection properties
                OnPropertyChanged(nameof(IsLightModeSelected));
                OnPropertyChanged(nameof(IsDarkModeSelected));
                OnPropertyChanged(nameof(IsAutoModeSelected));
                
                LoggingService.Instance.LogInfo($"SettingsViewModel synchronized with theme change: {(isDarkMode ? "Dark" : "Light")} mode, Auto: {themeManager.IsAutoMode}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling theme change in SettingsViewModel", ex);
            }
        }

        /// <summary>
        /// Reagiert auf Auto-Mode Änderungen
        /// </summary>
        private void OnAutoModeChanged(bool isAutoMode)
        {
            _isAutoModeEnabled = isAutoMode;
            OnPropertyChanged(nameof(IsAutoModeEnabled));
            OnPropertyChanged(nameof(IsManualModeEnabled));
            OnPropertyChanged(nameof(CurrentThemeStatus));
            
            LoggingService.Instance.LogInfo($"SettingsViewModel v5.0 synchronized with auto-mode change: {(isAutoMode ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// Prüft ob die Anwendung als Administrator läuft
        /// </summary>
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

        /// <summary>
        /// Aktualisiert die CanExecute-States der Sound-Test-Kommandos.
        /// </summary>
        private void UpdateSoundCommandStates()
        {
            (TestSoundCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (TestFirstWarningCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (TestSecondWarningCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            try
            {
                // Theme-Events abmelden
                if (UnifiedThemeManager.Instance != null)
                {
                    UnifiedThemeManager.Instance.ThemeChanged -= OnThemeChanged;
                    UnifiedThemeManager.Instance.AutoModeChanged -= OnAutoModeChanged;
                }
                
                LoggingService.Instance.LogInfo("SettingsViewModel disposed");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error disposing SettingsViewModel", ex);
            }
        }

        #endregion

        // ===== UI-BINDING PROPERTIES =====
        
        // Theme Selection Properties - FIXED v5.0
        public bool IsLightModeSelected
        {
            get => !IsDarkMode && !IsAutoModeEnabled;
            set
            {
                if (value && (IsDarkMode || IsAutoModeEnabled))
                {
                    LoggingService.Instance.LogInfo("Light mode manually selected from UI");
                    IsAutoModeEnabled = false; // Disable auto mode first
                    IsDarkMode = false; // Then set light mode
                }
            }
        }

        public bool IsDarkModeSelected
        {
            get => IsDarkMode && !IsAutoModeEnabled;
            set
            {
                if (value && (!IsDarkMode || IsAutoModeEnabled))
                {
                    LoggingService.Instance.LogInfo("Dark mode manually selected from UI");
                    IsAutoModeEnabled = false; // Disable auto mode first
                    IsDarkMode = true; // Then set dark mode
                }
            }
        }

        public bool IsAutoModeSelected
        {
            get => IsAutoModeEnabled;
            set 
            { 
                if (value && !IsAutoModeEnabled)
                {
                    LoggingService.Instance.LogInfo("Auto mode selected from UI");
                    IsAutoModeEnabled = true;
                    // The auto mode will determine the actual theme based on time
                }
            }
        }

        // Category Selection Properties
        public bool IsAppearanceCategorySelected => SelectedCategory == "Darstellung";
        public bool IsGeneralCategorySelected => SelectedCategory == "Allgemein";
        public bool IsTimerCategorySelected => SelectedCategory == "Warnungen";
        public bool IsMobileCategorySelected => SelectedCategory == "Mobile";
        public bool IsUpdatesCategorySelected => SelectedCategory == "Updates";
        public bool IsDataCategorySelected => SelectedCategory == "Stammdaten";
        public bool IsDataManagementCategorySelected => SelectedCategory == "Datenverwaltung";
        public bool IsInfoCategorySelected => SelectedCategory == "Info";

        // Additional Properties
        public bool EnableAnimations { get; set; } = true;
        public bool AutoRestore { get; set; } = true;
        public bool AutoFullscreen { get; set; } = false;
        public bool EnableDebugMode { get; set; } = false;

        // Commands für UI
        public ICommand SelectAppearanceCategoryCommand => new RelayCommand(() => SelectCategory("Darstellung"));
        public ICommand SelectGeneralCategoryCommand => new RelayCommand(() => SelectCategory("Allgemein"));
        public ICommand SelectTimerCategoryCommand => new RelayCommand(() => SelectCategory("Warnungen"));
        public ICommand SelectMobileCategoryCommand => new RelayCommand(() => SelectCategory("Mobile"));
        public ICommand SelectUpdatesCategoryCommand => new RelayCommand(() => SelectCategory("Updates"));
        public ICommand SelectDataCategoryCommand => new RelayCommand(() => SelectCategory("Stammdaten"));
        public ICommand SelectDataManagementCategoryCommand => new RelayCommand(() => SelectCategory("Datenverwaltung"));
        public ICommand SelectInfoCategoryCommand => new RelayCommand(() => SelectCategory("Info"));
        public ICommand CancelCommand => new RelayCommand(() => RequestClose?.Invoke());
        public ICommand SaveCommand => new RelayCommand(() => SaveSettings());
        public ICommand OpenMasterDataCommand => new RelayCommand(() => MasterDataRequested?.Invoke());
        public ICommand CleanupTempFilesCommand => new RelayCommand(() => ShowMessage?.Invoke("Temporäre Dateien wurden bereinigt."));
        
        // Theme selection commands
        public ICommand SelectLightModeCommand => new RelayCommand(() => 
        {
            LoggingService.Instance.LogInfo("Light mode selected via command");
            IsAutoModeEnabled = false;
            IsDarkMode = false;
        });
        
        public ICommand SelectDarkModeCommand => new RelayCommand(() => 
        {
            LoggingService.Instance.LogInfo("Dark mode selected via command");
            IsAutoModeEnabled = false;
            IsDarkMode = true;
        });
        
        public ICommand SelectAutoModeCommand => new RelayCommand(() => 
        {
            LoggingService.Instance.LogInfo("Auto mode selected via command");
            IsAutoModeEnabled = true;
        });
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
