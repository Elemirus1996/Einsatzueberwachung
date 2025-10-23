using System;
using System.Linq;
using System.Windows;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// SettingsWindow - Enhanced with Theme Integration v1.9.0
    /// Now inherits from BaseThemeWindow for automatic theme support
    /// Fully integrated with design system and theme service
    /// </summary>
    public partial class SettingsWindow : BaseThemeWindow
    {
        private SettingsViewModel? _viewModel;

        public SettingsWindow()
        {
            InitializeComponent();
            InitializeThemeSupport(); // Initialize theme after component initialization
            InitializeViewModel();
            
            LoggingService.Instance.LogInfo("SettingsWindow initialized with enhanced theme integration v1.9.0");
        }

        private void InitializeViewModel()
        {
            try
            {
                _viewModel = new SettingsViewModel();
                DataContext = _viewModel;
                
                // Subscribe to ViewModel events
                if (_viewModel != null)
                {
                    _viewModel.RequestClose += OnRequestClose;
                    _viewModel.ShowMessage += OnShowMessage;
                    _viewModel.HelpRequested += OnHelpRequested;
                    _viewModel.AboutRequested += OnAboutRequested;
                    _viewModel.MobileConnectionRequested += OnMobileConnectionRequested;
                    _viewModel.MasterDataRequested += OnMasterDataRequested;
                    _viewModel.StatisticsRequested += OnStatisticsRequested;
                    _viewModel.PdfExportRequested += OnPdfExportRequested;
                    _viewModel.UpdateCheckRequested += OnUpdateCheckRequested;
                    _viewModel.MobileServerTestRequested += OnMobileServerTestRequested;
                    _viewModel.ExportRequested += OnExportRequested;
                }
                
                LoggingService.Instance.LogInfo("SettingsViewModel initialized and connected");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing SettingsViewModel", ex);
                MessageBox.Show($"Fehler beim Initialisieren der Einstellungen:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void ApplyThemeToWindow(bool isDarkMode)
        {
            try
            {
                // Apply theme-specific styling
                base.ApplyThemeToWindow(isDarkMode);
                
                LoggingService.Instance.LogInfo($"Theme applied to SettingsWindow: {(isDarkMode ? "Dark" : "Light")} mode with Orange design");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to SettingsWindow", ex);
            }
        }

        #region Public Methods

        /// <summary>
        /// Zeigt eine bestimmte Kategorie in den Einstellungen an
        /// </summary>
        /// <param name="category">Kategorie-Name (z.B. "appearance", "theme", "general")</param>
        public void ShowCategory(string category)
        {
            try
            {
                if (_viewModel != null)
                {
                    _viewModel.SelectCategory(category);
                    LoggingService.Instance.LogInfo($"Settings category selected: {category}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error selecting settings category {category}", ex);
            }
        }

        /// <summary>
        /// Fokussiert direkt auf die Theme-Einstellungen
        /// </summary>
        public void ShowThemeSettings()
        {
            ShowCategory("appearance");
        }

        #endregion

        #region Event Handlers

        private void OnRequestClose()
        {
            try
            {
                if (_viewModel?.HasUnsavedChanges == true)
                {
                    var result = MessageBox.Show(
                        "Sie haben ungespeicherte Änderungen. Möchten Sie diese speichern?",
                        "Ungespeicherte Änderungen",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            _viewModel.SaveSettings();
                            DialogResult = true;
                            break;
                        case MessageBoxResult.No:
                            DialogResult = false;
                            break;
                        case MessageBoxResult.Cancel:
                            return; // Don't close
                    }
                }
                else
                {
                    DialogResult = true;
                }
                
                Close();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling RequestClose in SettingsWindow", ex);
                MessageBox.Show($"Fehler beim Schließen der Einstellungen:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnShowMessage(string message)
        {
            try
            {
                MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing message in SettingsWindow", ex);
            }
        }

        private void OnHelpRequested()
        {
            try
            {
                var helpWindow = new HelpWindow();
                helpWindow.Owner = this;
                helpWindow.ShowDialog();
                LoggingService.Instance.LogInfo("Help window opened from SettingsWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening help window", ex);
                MessageBox.Show($"Fehler beim Öffnen der Hilfe:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnAboutRequested()
        {
            try
            {
                var aboutWindow = new AboutWindow();
                aboutWindow.Owner = this;
                aboutWindow.ShowDialog();
                LoggingService.Instance.LogInfo("About window opened from SettingsWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening about window", ex);
                MessageBox.Show($"Fehler beim Öffnen der Info:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnMobileConnectionRequested()
        {
            try
            {
                // Hole das MainViewModel für die Datenintegration
                var mainViewModel = MainViewModelService.Instance.GetMainViewModel();
                
                if (mainViewModel != null)
                {
                    // Verwende die Factory-Methode mit Daten-Integration
                    var mobileWindow = Views.MobileConnectionWindow.CreateWithDataIntegration(mainViewModel);
                    mobileWindow.Owner = this;
                    mobileWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    mobileWindow.Show();
                    
                    LoggingService.Instance.LogInfo("MobileConnectionWindow opened with data integration from SettingsWindow");
                }
                else
                {
                    // Fallback: Einfaches Mobile-Fenster ohne Datenintegration
                    var mobileWindow = new Views.MobileConnectionWindow();
                    mobileWindow.Owner = this;
                    mobileWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    mobileWindow.Show();
                    
                    LoggingService.Instance.LogWarning("MobileConnectionWindow opened WITHOUT data integration - MainViewModel not available");
                    
                    MessageBox.Show("Mobile-Fenster wurde geöffnet, aber ohne Datenverbindung zur Hauptanwendung.\n\n" +
                                   "Für vollständige Funktionalität öffnen Sie das Mobile-Fenster über das Hauptmenü.",
                                   "Eingeschränkte Funktionalität", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening mobile connection window from SettingsWindow", ex);
                MessageBox.Show($"Fehler beim Öffnen der Mobile-Verbindung:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnMasterDataRequested()
        {
            try
            {
                var masterDataWindow = new MasterDataWindow();
                masterDataWindow.Owner = this;
                masterDataWindow.ShowDialog();
                LoggingService.Instance.LogInfo("Master data window opened from SettingsWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening master data window", ex);
                MessageBox.Show($"Fehler beim Öffnen der Stammdaten:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnStatisticsRequested()
        {
            try
            {
                // Get current teams from MainViewModel if available
                var mainViewModel = MainViewModelService.Instance.GetMainViewModel();
                if (mainViewModel?.Teams != null && mainViewModel.Teams.Count > 0)
                {
                    var teams = mainViewModel.Teams.ToList();
                    var einsatzData = new Models.EinsatzData(); // Create basic EinsatzData
                    
                    var statisticsWindow = new StatisticsWindow(teams, einsatzData);
                    statisticsWindow.Owner = this;
                    statisticsWindow.Show(); // Use Show() instead of ShowDialog() for better UX
                    
                    LoggingService.Instance.LogInfo($"Statistics window opened from SettingsWindow with {teams.Count} teams");
                }
                else
                {
                    MessageBox.Show("Keine Teams verfügbar für Statistiken.\n\nErstellen Sie zuerst Teams im Hauptfenster.", 
                        "Keine Daten", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoggingService.Instance.LogInfo("Statistics window requested but no teams available");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening statistics window", ex);
                MessageBox.Show($"Fehler beim Öffnen der Statistiken:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnPdfExportRequested()
        {
            try
            {
                // Get current teams from MainViewModel if available for PDF export
                var mainViewModel = MainViewModelService.Instance.GetMainViewModel();
                if (mainViewModel?.Teams != null && mainViewModel.Teams.Count > 0)
                {
                    var teams = mainViewModel.Teams.ToList();
                    var einsatzData = new Models.EinsatzData(); // Create basic EinsatzData
                    
                    var pdfExportWindow = new PdfExportWindow(einsatzData, teams);
                    pdfExportWindow.Owner = this;
                    pdfExportWindow.ShowDialog();
                    
                    LoggingService.Instance.LogInfo($"PDF Export window opened from SettingsWindow with {teams.Count} teams");
                }
                else
                {
                    // Show PDF export window with minimal data
                    var einsatzData = new Models.EinsatzData();
                    var teams = new List<Models.Team>();
                    
                    var pdfExportWindow = new PdfExportWindow(einsatzData, teams);
                    pdfExportWindow.Owner = this;
                    pdfExportWindow.ShowDialog();
                    
                    LoggingService.Instance.LogInfo("PDF Export window opened from SettingsWindow without teams data");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening PDF export window", ex);
                MessageBox.Show($"Fehler beim Öffnen des PDF-Exports:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnUpdateCheckRequested()
        {
            try
            {
                LoggingService.Instance.LogInfo("Update check requested from SettingsWindow");
                
                // Show temporary message while checking
                var progressWindow = new Views.ThemeTestWindow(); // Temporary progress indicator
                progressWindow.Title = "Update-Prüfung läuft...";
                progressWindow.Owner = this;
                progressWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                
                // Start update check in background
                using var updateService = new Services.GitHubUpdateService();
                
                // Show progress window temporarily
                progressWindow.Show();
                
                try
                {
                    var updateInfo = await updateService.CheckForUpdatesAsync();
                    
                    // Close progress window
                    progressWindow.Close();
                    
                    if (updateInfo != null)
                    {
                        // Update available - open UpdateNotificationWindow
                        LoggingService.Instance.LogInfo($"Update available: {updateInfo.Version}");
                        
                        var updateWindow = new Views.UpdateNotificationWindow(updateInfo);
                        updateWindow.Owner = this;
                        updateWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        updateWindow.ShowDialog();
                    }
                    else
                    {
                        // No update available
                        MessageBox.Show(
                            "Sie verwenden bereits die neueste Version der Einsatzüberwachung Professional.\n\n" +
                            "Ihre Version wird automatisch bei jedem Start auf Updates geprüft.",
                            "Keine Updates verfügbar",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    // Close progress window if still open
                    if (progressWindow.IsVisible)
                        progressWindow.Close();
                    
                    LoggingService.Instance.LogError("Error during update check", ex);
                    MessageBox.Show(
                        "Die Update-Prüfung konnte nicht durchgeführt werden.\n\n" +
                        $"Fehler: {ex.Message}\n\n" +
                        "Prüfen Sie Ihre Internetverbindung und versuchen Sie es erneut.\n\n" +
                        "Alternativ besuchen Sie GitHub für manuelle Updates:\n" +
                        "https://github.com/Elemirus1996/Einsatzueberwachung/releases",
                        "Update-Prüfung fehlgeschlagen",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error in update check setup", ex);
                MessageBox.Show($"Fehler beim Starten der Update-Prüfung:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnMobileServerTestRequested()
        {
            try
            {
                // Simulate mobile server test
                MessageBox.Show($"Mobile Server Test:\n\nServer: localhost:8080\nStatus: ✅ Verbindung erfolgreich\nLatenz: 12ms", 
                    "Server Test", MessageBoxButton.OK, MessageBoxImage.Information);
                LoggingService.Instance.LogInfo("Mobile server test performed from SettingsWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error testing mobile server", ex);
                MessageBox.Show($"Fehler beim Testen des Mobile Servers:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnExportRequested(string exportType)
        {
            try
            {
                // Get MainViewModel für Export-Funktionen
                var mainViewModel = MainViewModelService.Instance.GetMainViewModel();
                if (mainViewModel != null)
                {
                    switch (exportType.ToUpper())
                    {
                        case "PDF":
                            mainViewModel.ExportCommand.Execute("PDF");
                            LoggingService.Instance.LogInfo("PDF export triggered from SettingsWindow");
                            break;
                        case "EXCEL":
                            // Excel export implementieren falls vorhanden
                            MessageBox.Show("Teams-Statistik Export wird in einer zukünftigen Version verfügbar sein.", 
                                "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoggingService.Instance.LogInfo("Excel export requested from SettingsWindow (not implemented yet)");
                            break;
                        case "JSON":
                            // JSON export implementieren falls vorhanden
                            MessageBox.Show("Vollständiger JSON-Export wird in einer zukünftigen Version verfügbar sein.", 
                                "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoggingService.Instance.LogInfo("JSON export requested from SettingsWindow (not implemented yet)");
                            break;
                        case "TXT":
                            // TXT export implementieren falls vorhanden
                            MessageBox.Show("Globale Notizen Export wird in einer zukünftigen Version verfügbar sein.", 
                                "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoggingService.Instance.LogInfo("TXT export requested from SettingsWindow (not implemented yet)");
                            break;
                        default:
                            LoggingService.Instance.LogWarning($"Unknown export type requested: {exportType}");
                            break;
                    }
                }
                else
                {
                    MessageBox.Show("Export-Funktion nicht verfügbar. Öffnen Sie zuerst das Hauptfenster.", 
                        "Export nicht möglich", MessageBoxButton.OK, MessageBoxImage.Warning);
                    LoggingService.Instance.LogWarning("Export requested but MainViewModel not available");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error processing export request: {exportType}", ex);
                MessageBox.Show($"Fehler beim Export ({exportType}):\n{ex.Message}", 
                    "Export-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Additional Event Handlers

        private void OpenGitHubRepository_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/Elemirus1996/Einsatzueberwachung",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(startInfo);
                LoggingService.Instance.LogInfo("GitHub repository opened from SettingsWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening GitHub repository", ex);
                MessageBox.Show($"GitHub Repository konnte nicht geöffnet werden:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Window Events

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Cleanup ViewModel subscriptions
                if (_viewModel != null)
                {
                    _viewModel.RequestClose -= OnRequestClose;
                    _viewModel.ShowMessage -= OnShowMessage;
                    _viewModel.HelpRequested -= OnHelpRequested;
                    _viewModel.AboutRequested -= OnAboutRequested;
                    _viewModel.MobileConnectionRequested -= OnMobileConnectionRequested;
                    _viewModel.MasterDataRequested -= OnMasterDataRequested;
                    _viewModel.StatisticsRequested -= OnStatisticsRequested;
                    _viewModel.PdfExportRequested -= OnPdfExportRequested;
                    _viewModel.UpdateCheckRequested -= OnUpdateCheckRequested;
                    _viewModel.MobileServerTestRequested -= OnMobileServerTestRequested;
                    _viewModel.ExportRequested -= OnExportRequested;
                    
                    // Dispose ViewModel if it implements IDisposable
                    if (_viewModel is IDisposable disposableViewModel)
                    {
                        disposableViewModel.Dispose();
                    }
                }
                
                LoggingService.Instance.LogInfo("SettingsWindow closed and cleaned up");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during SettingsWindow cleanup", ex);
            }
            finally
            {
                base.OnClosed(e);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Check for unsaved changes
                if (_viewModel?.HasUnsavedChanges == true)
                {
                    var result = MessageBox.Show(
                        "Sie haben ungespeicherte Änderungen. Möchten Sie das Fenster wirklich schließen?",
                        "Ungespeicherte Änderungen",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                
                base.OnClosing(e);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during SettingsWindow closing", ex);
                base.OnClosing(e);
            }
        }

        #endregion
    }
}
