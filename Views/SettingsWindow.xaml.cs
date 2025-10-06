using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// Interaktionslogik für SettingsWindow.xaml
    /// Zentrales Einstellungen-Fenster für alle Konfigurationsoptionen
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsWindow()
        {
            InitializeComponent();
            _viewModel = (SettingsViewModel)DataContext;
            
            // Event-Handler registrieren mit Debug-Ausgaben
            _viewModel.SettingsChanged += OnSettingsChanged;
            _viewModel.HelpRequested += OnHelpRequested;
            _viewModel.AboutRequested += OnAboutRequested;
            _viewModel.MobileConnectionRequested += OnMobileConnectionRequested;
            _viewModel.UpdateCheckRequested += OnUpdateCheckRequested;
            _viewModel.MobileServerTestRequested += OnMobileServerTestRequested;
            _viewModel.MasterDataRequested += OnMasterDataRequested;

            // ThemeService Event abonnieren für automatische UI-Updates
            ThemeService.Instance.ThemeChanged += OnThemeChanged;

            // Start mit Appearance Settings
            ShowAppearanceSettings(null, null);
            
            // Theme RadioButtons initial setzen
            UpdateThemeRadioButtons();
            
            // Debug-Ausgabe zur Überprüfung
            System.Diagnostics.Debug.WriteLine("SettingsWindow initialized with ViewModel events registered");
        }

        private void UpdateThemeRadioButtons()
        {
            try
            {
                if (_viewModel != null)
                {
                    LightModeRadio.IsChecked = !_viewModel.IsDarkMode;
                    var darkRadio = ManualModePanel.Children.OfType<RadioButton>()
                        .FirstOrDefault(r => r.Content?.ToString()?.Contains("Dunkel") == true);
                    if (darkRadio != null)
                    {
                        darkRadio.IsChecked = _viewModel.IsDarkMode;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Aktualisieren der Theme RadioButtons: {ex.Message}");
            }
        }

        #region Navigation Event Handlers

        private void ShowAppearanceSettings(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            AppearancePanel.Visibility = Visibility.Visible;
        }

        private void ShowWarningSettings(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            WarningPanel.Visibility = Visibility.Visible;
        }

        private void ShowMobileSettings(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            MobilePanel.Visibility = Visibility.Visible;
        }

        private void ShowUpdateSettings(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            UpdatePanel.Visibility = Visibility.Visible;
        }

        private void ShowMasterDataSettings(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            MasterDataPanel.Visibility = Visibility.Visible;
        }

        private void ShowInfoSettings(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            InfoPanel.Visibility = Visibility.Visible;
        }

        private void HideAllPanels()
        {
            AppearancePanel.Visibility = Visibility.Collapsed;
            WarningPanel.Visibility = Visibility.Collapsed;
            MobilePanel.Visibility = Visibility.Collapsed;
            UpdatePanel.Visibility = Visibility.Collapsed;
            MasterDataPanel.Visibility = Visibility.Collapsed;
            InfoPanel.Visibility = Visibility.Collapsed;
        }

        private void ManualModeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Wechsel in den manuellen Modus, wenn der entsprechende Radio-Button aktiviert ist
                if (_viewModel != null)
                {
                    _viewModel.IsAutoMode = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Wechsel in den manuellen Modus: {ex.Message}");
            }
        }

        private void LightModeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel != null && !_viewModel.IsAutoMode)
                {
                    _viewModel.IsDarkMode = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Setzen des hellen Modus: {ex.Message}");
            }
        }

        private void DarkModeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel != null && !_viewModel.IsAutoMode)
                {
                    _viewModel.IsDarkMode = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Setzen des dunklen Modus: {ex.Message}");
            }
        }

        #endregion

        #region ViewModel Event Handlers

        private void OnSettingsChanged()
        {
            try
            {
                // Settings wurden gespeichert - Parent Window benachrichtigen falls gewünscht
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, 
                    $"Fehler beim Speichern der Einstellungen:\n{ex.Message}", 
                    "Einstellungen", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private void OnHelpRequested()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("OnHelpRequested called - creating HelpWindow");
                var helpWindow = new HelpWindow();
                helpWindow.Owner = this;
                helpWindow.ShowDialog();
                System.Diagnostics.Debug.WriteLine("HelpWindow should be displayed now");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in OnHelpRequested: {ex.Message}");
                MessageBox.Show(this, 
                    $"Fehler beim Öffnen der Hilfe:\n{ex.Message}", 
                    "Hilfe", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private void OnAboutRequested()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("OnAboutRequested called - creating AboutWindow");
                var aboutWindow = new AboutWindow();
                aboutWindow.Owner = this;
                aboutWindow.ShowDialog();
                System.Diagnostics.Debug.WriteLine("AboutWindow should be displayed now");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in OnAboutRequested: {ex.Message}");
                MessageBox.Show(this, 
                    $"Fehler beim Öffnen der Info:\n{ex.Message}", 
                    "Info", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private void OnMobileConnectionRequested()
        {
            try
            {
                // Versuche das MainViewModel über den Service zu erreichen
                var mainViewModel = MainViewModelService.Instance.GetMainViewModel();
                
                Views.MobileConnectionWindow mobileWindow;
                
                if (mainViewModel != null)
                {
                    // Mit Daten-Integration wenn MainViewModel verfügbar
                    mobileWindow = Views.MobileConnectionWindow.CreateWithDataIntegration(mainViewModel);
                    LoggingService.Instance.LogInfo("MobileConnectionWindow opened with data integration from SettingsWindow");
                }
                else
                {
                    // Fallback ohne Daten-Integration
                    mobileWindow = new Views.MobileConnectionWindow();
                    LoggingService.Instance.LogWarning("MobileConnectionWindow opened without data integration - MainViewModel not available");
                }
                
                mobileWindow.Owner = this;
                mobileWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening mobile connection from SettingsWindow", ex);
                MessageBox.Show(this, 
                    $"Fehler beim Öffnen der Mobile-Verbindung:\n{ex.Message}", 
                    "Mobile Verbindung", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private void OnUpdateCheckRequested()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("OnUpdateCheckRequested called - creating dummy UpdateInfo");
                // Erstelle eine Dummy UpdateInfo für Testing
                var dummyUpdate = new UpdateInfo 
                { 
                    Version = "1.7.0",
                    ReleaseNotes = new[] { "Test Update Check" }
                };
                
                System.Diagnostics.Debug.WriteLine("Creating UpdateNotificationWindow with dummy data");
                var updateWindow = new UpdateNotificationWindow(dummyUpdate);
                updateWindow.Owner = this;
                updateWindow.ShowDialog();
                System.Diagnostics.Debug.WriteLine("UpdateNotificationWindow should be displayed now");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in OnUpdateCheckRequested: {ex.Message}");
                MessageBox.Show(this, 
                    $"Fehler beim Prüfen auf Updates:\n{ex.Message}", 
                    "Update-Prüfung", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private void OnMobileServerTestRequested()
        {
            try
            {
                // Test der Mobile Server Verbindung
                MessageBox.Show(this,
                    $"🧪 Server-Test wird gestartet...\n\n" +
                    $"Port: {_viewModel.MobileServerPort}\n" +
                    $"URL: http://localhost:{_viewModel.MobileServerPort}\n\n" +
                    $"Prüfe, ob der Server erreichbar ist...",
                    "Server Test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // TODO: Hier könnte ein echter Server-Test implementiert werden
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, 
                    $"Fehler beim Testen des Mobile Servers:\n{ex.Message}", 
                    "Server Test", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private void OnMasterDataRequested()
        {
            try
            {
                var masterDataWindow = new MasterDataWindow();
                masterDataWindow.Owner = this;
                masterDataWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, 
                    $"Fehler beim Öffnen der Stammdaten:\n{ex.Message}", 
                    "Stammdaten", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Time Preset Event Handlers

        private void SetTimePreset_EarlySummer(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel != null)
                {
                    _viewModel.LightModeStartTime = new TimeSpan(6, 0, 0);
                    _viewModel.DarkModeStartTime = new TimeSpan(20, 0, 0);
                    
                    // Theme sofort anwenden wenn Automatik-Modus aktiv ist
                    if (_viewModel.IsAutoMode)
                    {
                        ThemeService.Instance.SetAutoModeTimes(_viewModel.DarkModeStartTime, _viewModel.LightModeStartTime);
                    }
                    
                    System.Diagnostics.Debug.WriteLine("Zeit-Preset 'Früher Sommer' angewendet: 06:00-20:00");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Setzen des Zeit-Presets 'Früher Sommer': {ex.Message}");
            }
        }

        private void SetTimePreset_Standard(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel != null)
                {
                    _viewModel.LightModeStartTime = new TimeSpan(8, 0, 0);
                    _viewModel.DarkModeStartTime = new TimeSpan(18, 0, 0);
                    
                    // Theme sofort anwenden wenn Automatik-Modus aktiv ist
                    if (_viewModel.IsAutoMode)
                    {
                        ThemeService.Instance.SetAutoModeTimes(_viewModel.DarkModeStartTime, _viewModel.LightModeStartTime);
                    }
                    
                    System.Diagnostics.Debug.WriteLine("Zeit-Preset 'Standard' angewendet: 08:00-18:00");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Setzen des Zeit-Presets 'Standard': {ex.Message}");
            }
        }

        private void SetTimePreset_Winter(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel != null)
                {
                    _viewModel.LightModeStartTime = new TimeSpan(9, 0, 0);
                    _viewModel.DarkModeStartTime = new TimeSpan(17, 0, 0);
                    
                    // Theme sofort anwenden wenn Automatik-Modus aktiv ist
                    if (_viewModel.IsAutoMode)
                    {
                        ThemeService.Instance.SetAutoModeTimes(_viewModel.DarkModeStartTime, _viewModel.LightModeStartTime);
                    }
                    
                    System.Diagnostics.Debug.WriteLine("Zeit-Preset 'Winter' angewendet: 09:00-17:00");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Setzen des Zeit-Presets 'Winter': {ex.Message}");
            }
        }

        #endregion

        #region DEBUG Test Methods

        private void TestHelpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("TEST: Direct HelpWindow creation");
                MessageBox.Show("Test: Öffne HelpWindow direkt", "Debug", MessageBoxButton.OK);
                
                var helpWindow = new HelpWindow();
                helpWindow.Owner = this;
                helpWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"FEHLER beim direkten HelpWindow-Test: {ex.Message}", "Debug Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TestUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("TEST: Direct UpdateNotificationWindow creation");
                MessageBox.Show("Test: Öffne UpdateNotificationWindow direkt", "Debug", MessageBoxButton.OK);
                
                var dummyUpdate = new UpdateInfo 
                { 
                    Version = "1.7.0",
                    ReleaseNotes = new[] { "Test Update Check (Direct)" }
                };
                
                var updateWindow = new UpdateNotificationWindow(dummyUpdate);
                updateWindow.Owner = this;
                updateWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"FEHLER beim direkten UpdateWindow-Test: {ex.Message}", "Debug Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Window Events

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Event-Handler entregistrieren
                if (_viewModel != null)
                {
                    _viewModel.SettingsChanged -= OnSettingsChanged;
                    _viewModel.HelpRequested -= OnHelpRequested;
                    _viewModel.AboutRequested -= OnAboutRequested;
                    _viewModel.MobileConnectionRequested -= OnMobileConnectionRequested;
                    _viewModel.UpdateCheckRequested -= OnUpdateCheckRequested;
                    _viewModel.MobileServerTestRequested -= OnMobileServerTestRequested;
                    _viewModel.MasterDataRequested -= OnMasterDataRequested;
                }
            }
            catch (Exception ex)
            {
                // Fehler beim Cleanup loggen, aber nicht anzeigen
                System.Diagnostics.Debug.WriteLine($"Error during SettingsWindow cleanup: {ex.Message}");
            }
            finally
            {
                base.OnClosed(e);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Öffnet das Settings-Window mit einer bestimmten Kategorie
        /// </summary>
        /// <param name="category">Die anzuzeigende Kategorie</param>
        public void ShowCategory(string category)
        {
            switch (category?.ToLower())
            {
                case "appearance":
                case "darstellung":
                    ShowAppearanceSettings(null, null);
                    break;
                case "warnings":
                case "warnungen":
                    ShowWarningSettings(null, null);
                    break;
                case "mobile":
                    ShowMobileSettings(null, null);
                    break;
                case "updates":
                    ShowUpdateSettings(null, null);
                    break;
                case "masterdata":
                case "stammdaten":
                    ShowMasterDataSettings(null, null);
                    break;
                case "info":
                    ShowInfoSettings(null, null);
                    break;
                default:
                    ShowAppearanceSettings(null, null);
                    break;
            }
            
            Show();
        }

        #endregion

        #region Theme Event Handler

        private void OnThemeChanged(bool isDarkMode)
        {
            try
            {
                // Aktualisiere die RadioButtons entsprechend dem neuen Theme
                UpdateThemeRadioButtons();
                System.Diagnostics.Debug.WriteLine($"Theme geändert: {(isDarkMode ? "Dunkelmodus" : "Hellmodus")}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Theme-Wechsel: {ex.Message}");
            }
        }

        #endregion
    }
}
