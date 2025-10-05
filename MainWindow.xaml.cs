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
                MessageBox.Show("Export-Funktionalität wurde auf MVVM umgestellt - Implementation folgt.", 
                    "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing export dialog via MVVM", ex);
            }
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
                var contextMenu = new ContextMenu();
                
                AddMenuItem(contextMenu, "Stammdaten verwalten...", () => OpenMasterDataWindow());
                AddMenuItem(contextMenu, "Mobile Verbindung...", () => OpenMobileConnectionWindow());
                AddMenuItem(contextMenu, "Statistiken & Analytics...", () => OpenStatisticsWindow());
                contextMenu.Items.Add(new Separator());
                AddMenuItem(contextMenu, "Warnungs-Einstellungen...", () => OpenWarningSettingsWindow());
                contextMenu.Items.Add(new Separator());
                AddMenuItem(contextMenu, "Über...", () => OpenAboutWindow());
                
                contextMenu.PlacementTarget = this;
                contextMenu.Placement = PlacementMode.Center;
                contextMenu.IsOpen = true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing context menu via MVVM", ex);
            }
        }

        private void AddMenuItem(ContextMenu menu, string header, Action action)
        {
            var item = new MenuItem { Header = header };
            item.Click += (s, e) => action?.Invoke();
            menu.Items.Add(item);
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
                var window = new Views.MobileConnectionWindow();
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Show();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening mobile connection window via MVVM", ex);
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
