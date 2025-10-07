using System;
using System.Windows;
using System.Windows.Controls;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;
using Einsatzueberwachung.ViewModels;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// Interaction logic for TeamDetailWindow.xaml
    /// Vollständig auf MVVM-Pattern umgestellt - minimales Code-Behind
    /// v1.9.0 mit Orange-Design-System und verbesserter Team-Löschung
    /// </summary>
    public partial class TeamDetailWindow : Window
    {
        private readonly TeamDetailViewModel _viewModel;
        private TeamControl? _teamControl;

        public TeamDetailWindow()
        {
            InitializeComponent();
            
            _viewModel = new TeamDetailViewModel();
            _viewModel.TeamDeleteRequested += OnTeamDeleteRequested;
            _viewModel.TeamRefreshed += OnTeamRefreshed;
            _viewModel.TeamTimerChanged += OnTeamTimerChanged;
            
            DataContext = _viewModel;
            
            LoggingService.Instance.LogInfo("TeamDetailWindow v1.9.0 initialized with MVVM pattern and Orange design");
        }

        public TeamDetailWindow(Team team) : this()
        {
            try
            {
                // Team ins ViewModel laden
                _viewModel.LoadTeam(team);
                
                // TeamControl initialisieren und einbetten
                InitializeTeamControl(team);
                
                LoggingService.Instance.LogInfo($"TeamDetailWindow initialized with team: {team.TeamName}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing TeamDetailWindow with team", ex);
                MessageBox.Show($"Fehler beim Laden der Team-Details: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeTeamControl(Team team)
        {
            try
            {
                // Bestehende TeamControl erstellen und konfigurieren
                _teamControl = new TeamControl { Team = team };
                
                // Margins für Detail-Window optimieren (weniger Abstand)
                _teamControl.Margin = new Thickness(0);
                
                // Theme auf TeamControl anwenden
                _teamControl.ApplyTheme(ThemeService.Instance.IsDarkMode);
                
                // TeamControl-Events abonnieren (für Datenänderungen)
                if (_teamControl.DataContext is TeamControlViewModel teamControlViewModel)
                {
                    teamControlViewModel.DataChanged += OnTeamControlDataChanged;
                }
                
                // TeamControl zum Container hinzufügen
                TeamControlContainer.Child = _teamControl;
                
                // Delete-Button der TeamControl ausblenden nach dem Laden
                _teamControl.Loaded += (s, e) =>
                {
                    try
                    {
                        var deleteButton = _teamControl.FindName("DeleteButton") as Button;
                        if (deleteButton != null)
                        {
                            deleteButton.Visibility = Visibility.Collapsed;
                            LoggingService.Instance.LogInfo("TeamControl delete button hidden in DetailWindow");
                        }
                        
                        // TeamControl für Detail-Ansicht optimieren
                        if (_teamControl.FindName("TeamBorder") is Border teamBorder)
                        {
                            // Margins in der Detail-Ansicht reduzieren
                            teamBorder.Margin = new Thickness(0);
                            teamBorder.Padding = new Thickness(16); // Weniger Padding als normal
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogError("Error optimizing TeamControl for DetailWindow", ex);
                    }
                };
                
                // Theme-Änderungen für TeamControl abonnieren
                ThemeService.Instance.ThemeChanged += OnThemeChanged;
                
                LoggingService.Instance.LogInfo($"TeamControl embedded in DetailWindow for team {team.TeamName} with optimized layout");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing TeamControl in DetailWindow", ex);
                throw; // Re-throw für Fehlerbehandlung im Constructor
            }
        }

        #region Event Handlers

        private void OnTeamDeleteRequested(Team team)
        {
            try
            {
                // Event an Parent-Window (MainWindow) weiterleiten
                if (Owner is MainWindow mainWindow && 
                    mainWindow.DataContext is MainViewModel mainViewModel)
                {
                    mainViewModel.RemoveTeam(team);
                    LoggingService.Instance.LogInfo($"Team {team.TeamName} successfully deleted via DetailWindow");
                }
                else
                {
                    // Fallback: Direkt über Application-Context versuchen
                    if (Application.Current.MainWindow is MainWindow appMainWindow &&
                        appMainWindow.DataContext is MainViewModel appMainViewModel)
                    {
                        appMainViewModel.RemoveTeam(team);
                        LoggingService.Instance.LogInfo($"Team {team.TeamName} successfully deleted via DetailWindow (fallback)");
                    }
                    else
                    {
                        LoggingService.Instance.LogWarning("Could not find MainViewModel to delete team");
                        MessageBox.Show("Fehler beim Löschen des Teams: MainViewModel nicht gefunden.",
                            "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error handling team deletion request for {team.TeamName}", ex);
                MessageBox.Show($"Fehler beim Löschen des Teams: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnTeamRefreshed(Team team)
        {
            try
            {
                // TeamControl aktualisieren
                if (_teamControl != null)
                {
                    _teamControl.RefreshTeamData();
                }
                LoggingService.Instance.LogInfo($"Team {team.TeamName} refreshed in DetailWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error refreshing team {team.TeamName} in DetailWindow", ex);
            }
        }

        private void OnTeamTimerChanged(Team team)
        {
            try
            {
                // Event an MainWindow weiterleiten für globale Timer-Updates
                if (Owner is MainWindow mainWindow &&
                    mainWindow.DataContext is MainViewModel mainViewModel)
                {
                    // MainViewModel wird automatisch über Team.PropertyChanged benachrichtigt
                    LoggingService.Instance.LogInfo($"Timer changed for team {team.TeamName} in DetailWindow");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error handling timer change for team {team.TeamName}", ex);
            }
        }

        private void OnTeamControlDataChanged()
        {
            try
            {
                // Datenänderungen in der TeamControl wurden bereits automatisch 
                // über Data Binding am Team-Objekt vorgenommen
                LoggingService.Instance.LogInfo("TeamControl data changed in DetailWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling TeamControl data change", ex);
            }
        }

        private void OnThemeChanged(bool isDarkMode)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    // Theme auf eingebettete TeamControl anwenden
                    _teamControl?.ApplyTheme(isDarkMode);
                    
                    LoggingService.Instance.LogInfo($"Theme applied to TeamDetailWindow: {(isDarkMode ? "Dark" : "Light")} mode with Orange design");
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to TeamDetailWindow", ex);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Das aktuell angezeigte Team
        /// </summary>
        public Team? CurrentTeam => _viewModel.Team;

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Events abmelden
                ThemeService.Instance.ThemeChanged -= OnThemeChanged;
                _viewModel.TeamDeleteRequested -= OnTeamDeleteRequested;
                _viewModel.TeamRefreshed -= OnTeamRefreshed;
                _viewModel.TeamTimerChanged -= OnTeamTimerChanged;
                
                // TeamControl-Events abmelden
                if (_teamControl?.DataContext is TeamControlViewModel teamControlViewModel)
                {
                    teamControlViewModel.DataChanged -= OnTeamControlDataChanged;
                }
                
                // ViewModel aufräumen
                if (_viewModel is IDisposable disposableViewModel)
                {
                    disposableViewModel.Dispose();
                }
                
                LoggingService.Instance.LogInfo($"TeamDetailWindow closed for team {_viewModel.Team?.TeamName ?? "Unknown"}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error closing TeamDetailWindow", ex);
            }
            finally
            {
                base.OnClosed(e);
            }
        }
    }
}
