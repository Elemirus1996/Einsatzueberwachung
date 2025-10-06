using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;
using Einsatzueberwachung.ViewModels;

namespace Einsatzueberwachung
{
    /// <summary>
    /// MainWindow - MVVM-Implementation v1.9.0
    /// Minimales Code-Behind mit ViewModel-Integration für globale Anwendungssteuerung
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;
        private bool _isFullscreen = false;
        private WindowState _previousWindowState;
        private WindowStyle _previousWindowStyle;

        // Dashboard View Management
        private System.Collections.Generic.Dictionary<int, Views.TeamCompactCard> _teamCompactCards = new();

        public MainWindow()
        {
            InitializeComponent();
            InitializeViewModel();
            
            LoggingService.Instance.LogInfo("MainWindow initialized with MVVM pattern v1.9.0");
        }

        public MainWindow(EinsatzData einsatzData, int firstWarningMinutes, int secondWarningMinutes) : this()
        {
            try
            {
                // Initialize ViewModel with mission data
                _viewModel = new MainViewModel(einsatzData, firstWarningMinutes, secondWarningMinutes);
                DataContext = _viewModel;
                SubscribeToViewModelEvents();
                
                // MainViewModel für globalen Zugriff registrieren
                MainViewModelService.Instance.RegisterMainViewModel(_viewModel);
                
                LoggingService.Instance.LogInfo($"MainWindow initialized with mission data via MVVM: {einsatzData.EinsatzTyp}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing MainWindow with mission data", ex);
            }
        }

        private void InitializeViewModel()
        {
            try
            {
                _viewModel = new MainViewModel();
                DataContext = _viewModel;
                SubscribeToViewModelEvents();
                
                // MainViewModel für globalen Zugriff registrieren
                MainViewModelService.Instance.RegisterMainViewModel(_viewModel);
                
                // Check for recovery
                CheckForRecoveryAsync();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing MainWindow ViewModel", ex);
            }
        }

        private void SubscribeToViewModelEvents()
        {
            if (_viewModel != null)
            {
                _viewModel.TeamAddRequested += ViewModel_TeamAddRequested;
                _viewModel.HelpRequested += ViewModel_HelpRequested;
                _viewModel.ExportRequested += ViewModel_ExportRequested;
                _viewModel.MenuRequested += ViewModel_MenuRequested;
                _viewModel.ExportLogRequested += ViewModel_ExportLogRequested;
                _viewModel.FullscreenToggleRequested += ViewModel_FullscreenToggleRequested;
                _viewModel.ScrollToLatestNoteRequested += ViewModel_ScrollToLatestNoteRequested;
                _viewModel.ThemeChanged += ViewModel_ThemeChanged;
                _viewModel.Teams.CollectionChanged += Teams_CollectionChanged;
            }
        }

        #region ViewModel Event Handlers

        private void ViewModel_TeamAddRequested()
        {
            try
            {
                if (_viewModel?.Teams.Count >= 50)
                {
                    MessageBox.Show("Maximale Anzahl von 50 Teams erreicht!", 
                        "Limit erreicht", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var inputWindow = new Views.TeamInputWindow();
                inputWindow.Owner = this;
                inputWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                
                if (inputWindow.ShowDialog() == true)
                {
                    var newTeam = new Team
                    {
                        TeamName = inputWindow.TeamName,
                        HundName = inputWindow.HundName,
                        Hundefuehrer = inputWindow.Hundefuehrer,
                        Helfer = inputWindow.Helfer,
                        Suchgebiet = inputWindow.Suchgebiet,
                        MultipleTeamTypes = inputWindow.PreselectedTeamTypes ?? new MultipleTeamTypes(TeamType.Allgemein)
                    };
                    
                    _viewModel?.AddTeam(newTeam);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling team add request via MVVM", ex);
            }
        }

        private void ViewModel_HelpRequested()
        {
            try
            {
                var helpWindow = new Views.HelpWindow();
                helpWindow.Owner = this;
                helpWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                helpWindow.Show();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening help window via MVVM", ex);
            }
        }

        private void ViewModel_ExportRequested()
        {
            try
            {
                ShowExportDialog();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling export request via MVVM", ex);
            }
        }

        private void ViewModel_MenuRequested()
        {
            try
            {
                ShowContextMenu();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing menu via MVVM", ex);
            }
        }

        private void ViewModel_ExportLogRequested()
        {
            try
            {
                ExportEinsatzLog();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error exporting log via MVVM", ex);
            }
        }

        private void ViewModel_FullscreenToggleRequested()
        {
            try
            {
                ToggleFullscreen();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error toggling fullscreen via MVVM", ex);
            }
        }

        private void ViewModel_ScrollToLatestNoteRequested()
        {
            try
            {
                Dispatcher.BeginInvoke(() =>
                {
                    GlobalNotesScrollViewer?.ScrollToEnd();
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error scrolling to latest note via MVVM", ex);
            }
        }

        private void ViewModel_ThemeChanged(bool isDarkMode)
        {
            try
            {
                ApplyThemeToAllTeams(isDarkMode);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme changes via MVVM", ex);
            }
        }

        private void Teams_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (e.NewItems != null)
                {
                    foreach (Team team in e.NewItems)
                    {
                        CreateTeamCompactCard(team);
                    }
                }

                if (e.OldItems != null)
                {
                    foreach (Team team in e.OldItems)
                    {
                        RemoveTeamCompactCard(team);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling teams collection change via MVVM", ex);
            }
        }

        #endregion

        #region Team Management

        private void CreateTeamCompactCard(Team team)
        {
            try
            {
                var teamCompactCard = new Views.TeamCompactCard { Team = team };
                teamCompactCard.TeamClicked += OnTeamCompactCardClicked;
                teamCompactCard.ApplyTheme(ThemeService.Instance.IsDarkMode);
                
                _teamCompactCards[team.TeamId] = teamCompactCard;
                DashboardGrid?.Children.Add(teamCompactCard);
                
                LoggingService.Instance.LogInfo($"Team compact card created via MVVM: {team.TeamName}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error creating team compact card via MVVM: {team?.TeamName}", ex);
            }
        }

        private void RemoveTeamCompactCard(Team team)
        {
            try
            {
                if (_teamCompactCards.ContainsKey(team.TeamId))
                {
                    var teamCompactCard = _teamCompactCards[team.TeamId];
                    DashboardGrid?.Children.Remove(teamCompactCard);
                    _teamCompactCards.Remove(team.TeamId);
                    
                    if (teamCompactCard is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error removing team compact card via MVVM: {team?.TeamName}", ex);
            }
        }

        private void OnTeamCompactCardClicked(object? sender, Team team)
        {
            try
            {
                var detailWindow = new Views.TeamDetailWindow(team);
                detailWindow.Owner = this;
                detailWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                detailWindow.Show();
                
                LoggingService.Instance.LogInfo($"Team detail window opened via MVVM: {team.TeamName}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening team detail window via MVVM", ex);
            }
        }

        private void ApplyThemeToAllTeams(bool isDarkMode)
        {
            try
            {
                foreach (var teamCompactCard in _teamCompactCards.Values)
                {
                    teamCompactCard.ApplyTheme(isDarkMode);
                }
                
                LoggingService.Instance.LogInfo($"Applied theme to {_teamCompactCards.Count} team compact cards via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to teams via MVVM", ex);
            }
        }

        #endregion

        #region UI Operations (Delegated from ViewModel)

        private void ToggleFullscreen()
        {
            try
            {
                if (_isFullscreen)
                {
                    WindowState = _previousWindowState;
                    WindowStyle = _previousWindowStyle;
                    _isFullscreen = false;
                    
                    Width = 1600;
                    Height = 900;
                    Top = 100;
                    Left = 100;
                }
                else
                {
                    _previousWindowState = WindowState;
                    _previousWindowStyle = WindowStyle;
                    
                    WindowStyle = WindowStyle.None;
                    WindowState = WindowState.Normal;
                    _isFullscreen = true;
                    
                    var screenWidth = SystemParameters.PrimaryScreenWidth;
                    var screenHeight = SystemParameters.PrimaryScreenHeight;
                    Width = screenWidth;
                    Height = screenHeight;
                    Top = 0;
                    Left = 0;
                }
                
                LoggingService.Instance.LogInfo($"Fullscreen toggled via MVVM: {_isFullscreen}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error toggling fullscreen via MVVM", ex);
            }
        }

        private void ShowExportDialog()
        {
            try
            {
                // Überprüfen ob Einsatz-Daten verfügbar sind
                if (_viewModel?.Teams == null || !_viewModel.Teams.Any())
                {
                    MessageBox.Show("Keine Teams zum Exportieren vorhanden.", 
                        "PDF-Export", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Erstelle EinsatzData für den Export
                var einsatzData = CreateEinsatzDataForExport();
                var teams = _viewModel.Teams.ToList();

                // Öffne PDF-Export-Window
                var exportWindow = new Views.PdfExportWindow(einsatzData, teams);
                exportWindow.Owner = this;
                exportWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                exportWindow.ShowDialog();
                
                LoggingService.Instance.LogInfo("PDF export window opened via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing export dialog via MVVM", ex);
                MessageBox.Show($"Fehler beim Öffnen des Export-Dialogs:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Erstellt EinsatzData-Objekt für den PDF-Export aus den aktuellen ViewModel-Daten
        /// </summary>
        private EinsatzData CreateEinsatzDataForExport()
        {
            var einsatzData = new EinsatzData
            {
                EinsatzDatum = DateTime.Now,
                IstEinsatz = true, // Dies setzt EinsatzTyp automatisch auf "Einsatz"
                Einsatzort = _viewModel?.Einsatzort ?? "Unbekannt",
                Einsatzleiter = _viewModel?.Einsatzleiter ?? "Unbekannt"
            };

            // Kopiere globale Notizen wenn verfügbar
            if (_viewModel?.FilteredNotesCollection != null)
            {
                foreach (var note in _viewModel.FilteredNotesCollection)
                {
                    einsatzData.GlobalNotesEntries.Add(note);
                }
            }

            return einsatzData;
        }

        private void ExportEinsatzLog()
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Text Dateien (*.txt)|*.txt|Alle Dateien (*.*)|*.*",
                    FileName = $"Einsatzlog_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                    Title = "Einsatz-Log exportieren"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    LoggingService.Instance.LogInfo($"Log export completed via MVVM: {saveDialog.FileName}");
                    MessageBox.Show($"Log würde exportiert werden nach:\n{saveDialog.FileName}", 
                        "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error exporting log via MVVM", ex);
            }
        }

        private void ShowContextMenu()
        {
            try
            {
                // Direkt zu den Settings-Fenster weiterleiten
                OpenSettingsWindow();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing context menu via MVVM", ex);
            }
        }

        private void OpenMasterDataWindow()
        {
            try
            {
                var window = new Views.MasterDataWindow();
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Show();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening master data window via MVVM", ex);
            }
        }

        private void OpenMobileConnectionWindow()
        {
            try
            {
                // Verwende die neue Factory-Methode mit Daten-Integration
                var window = Views.MobileConnectionWindow.CreateWithDataIntegration(_viewModel!);
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Show();
                
                LoggingService.Instance.LogInfo("MobileConnectionWindow opened with data integration");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening mobile connection window with data integration", ex);
                
                // Fallback: Normale MobileConnectionWindow ohne Daten-Integration
                try
                {
                    var fallbackWindow = new Views.MobileConnectionWindow();
                    fallbackWindow.Owner = this;
                    fallbackWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    fallbackWindow.Show();
                    
                    LoggingService.Instance.LogWarning("MobileConnectionWindow opened without data integration (fallback)");
                }
                catch (Exception fallbackEx)
                {
                    LoggingService.Instance.LogError("Error opening fallback mobile connection window", fallbackEx);
                    MessageBox.Show($"Fehler beim Öffnen der Mobile-Verbindung:\n{ex.Message}", 
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenStatisticsWindow()
        {
            try
            {
                if (_viewModel?.Teams != null)
                {
                    var window = new Views.StatisticsWindow(_viewModel.Teams.ToList(), new EinsatzData());
                    window.Owner = this;
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.Show();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening statistics window via MVVM", ex);
            }
        }

        private void OpenWarningSettingsWindow()
        {
            try
            {
                if (_viewModel?.Teams != null)
                {
                    var window = new Views.TeamWarningSettingsWindow(_viewModel.Teams.ToList(), 
                        _viewModel.GlobalFirstWarningMinutes, _viewModel.GlobalSecondWarningMinutes);
                    window.Owner = this;
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening warning settings window via MVVM", ex);
            }
        }

        private void OpenAboutWindow()
        {
            try
            {
                var window = new Views.AboutWindow();
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening about window via MVVM", ex);
            }
        }

        private void OpenSettingsWindow()
        {
            try
            {
                var window = new Views.SettingsWindow();
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening settings window via MVVM", ex);
            }
        }

        #endregion

        #region Recovery and Startup

        private async void CheckForRecoveryAsync()
        {
            try
            {
                if (PersistenceService.Instance.HasCrashRecovery())
                {
                    var result = MessageBox.Show(
                        "Es wurde eine unterbrochene Sitzung gefunden. Möchten Sie diese wiederherstellen?",
                        "Wiederherstellung", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var sessionData = await PersistenceService.Instance.LoadCrashRecoveryAsync().ConfigureAwait(false);
                        if (sessionData != null)
                        {
                            PersistenceService.Instance.ClearCrashRecovery();
                            LoggingService.Instance.LogInfo("Session restored from crash recovery via MVVM");
                            return;
                        }
                    }
                    else
                    {
                        PersistenceService.Instance.ClearCrashRecovery();
                    }
                }

                ShowStartWindow();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during recovery check via MVVM", ex);
                ShowStartWindow();
            }
        }

        private void ShowStartWindow()
        {
            try
            {
                var startWindow = new Views.StartWindow();
                startWindow.Owner = this;
                
                var result = startWindow.ShowDialog();
                
                if (result == true && startWindow.EinsatzData != null)
                {
                    _viewModel?.Dispose();
                    _viewModel = new MainViewModel(startWindow.EinsatzData, 
                        startWindow.FirstWarningMinutes, startWindow.SecondWarningMinutes);
                    DataContext = _viewModel;
                    SubscribeToViewModelEvents();
                    
                    // Neues MainViewModel registrieren
                    MainViewModelService.Instance.RegisterMainViewModel(_viewModel);
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing StartWindow via MVVM", ex);
            }
        }

        #endregion

        #region Window Lifecycle

        protected override void OnClosing(CancelEventArgs e)
        {
            try
            {
                if (_viewModel?.Teams.Any(t => t.IsRunning) == true)
                {
                    var result = MessageBox.Show(
                        "Es sind noch aktive Timer vorhanden.\n\nMöchten Sie die Anwendung wirklich beenden?",
                        "Aktive Einsatzzeiten",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                
                LoggingService.Instance.LogInfo("MainWindow closing via MVVM - starting cleanup");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during window closing via MVVM", ex);
            }
            finally
            {
                base.OnClosing(e);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // MainViewModel unregistrieren
                MainViewModelService.Instance.UnregisterMainViewModel();
                
                _viewModel?.Dispose();
                _teamCompactCards.Clear();
                
                LoggingService.Instance.LogInfo("MainWindow closed via MVVM - cleanup completed");
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during window cleanup via MVVM", ex);
            }
            finally
            {
                base.OnClosed(e);
            }
        }

        #endregion
    }
}
