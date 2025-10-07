using System;
using System.Windows;
using System.Windows.Input;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    /// <summary>
    /// ViewModel für das TeamDetailWindow - MVVM-Implementation v1.9.0
    /// Verwaltet die Anzeige und Interaktion mit Team-Details
    /// </summary>
    public class TeamDetailViewModel : BaseViewModel, IDisposable
    {
        private Team? _team;
        private bool _isDarkMode;
        private string _windowTitle = "Team Details";
        private string _teamName = "Team";
        private string _teamType = "Teamtyp";
        private bool _isTeamLoaded;

        public TeamDetailViewModel()
        {
            InitializeCommands();
            
            // Theme-Service abonnieren
            ThemeService.Instance.ThemeChanged += OnThemeChanged;
            IsDarkMode = ThemeService.Instance.IsDarkMode;

            LoggingService.Instance.LogInfo("TeamDetailViewModel initialized with MVVM pattern");
        }

        public TeamDetailViewModel(Team team) : this()
        {
            LoadTeam(team);
        }

        #region Properties

        /// <summary>
        /// Das Team-Objekt für die Detailansicht
        /// </summary>
        public Team? Team
        {
            get => _team;
            private set
            {
                if (SetProperty(ref _team, value))
                {
                    UpdateTeamProperties();
                }
            }
        }

        /// <summary>
        /// Aktueller Theme-Modus (Dark/Light)
        /// </summary>
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set => SetProperty(ref _isDarkMode, value);
        }

        /// <summary>
        /// Fenster-Titel
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        /// <summary>
        /// Team-Name für Header-Anzeige
        /// </summary>
        public string TeamName
        {
            get => _teamName;
            set => SetProperty(ref _teamName, value);
        }

        /// <summary>
        /// Team-Typ für Header-Anzeige
        /// </summary>
        public string TeamType
        {
            get => _teamType;
            set => SetProperty(ref _teamType, value);
        }

        /// <summary>
        /// Gibt an, ob ein Team geladen wurde
        /// </summary>
        public bool IsTeamLoaded
        {
            get => _isTeamLoaded;
            set => SetProperty(ref _isTeamLoaded, value);
        }

        #endregion

        #region Commands

        public ICommand CloseWindowCommand { get; private set; } = null!;
        public ICommand RefreshTeamCommand { get; private set; } = null!;
        public ICommand OpenTeamEditCommand { get; private set; } = null!;
        public ICommand ToggleTimerCommand { get; private set; } = null!;
        public ICommand ResetTimerCommand { get; private set; } = null!;
        public ICommand DeleteTeamCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            CloseWindowCommand = new RelayCommand<Window>(ExecuteCloseWindow);
            RefreshTeamCommand = new RelayCommand(ExecuteRefreshTeam, CanExecuteRefreshTeam);
            OpenTeamEditCommand = new RelayCommand(ExecuteOpenTeamEdit, CanExecuteTeamOperations);
            ToggleTimerCommand = new RelayCommand(ExecuteToggleTimer, CanExecuteTeamOperations);
            ResetTimerCommand = new RelayCommand(ExecuteResetTimer, CanExecuteTeamOperations);
            DeleteTeamCommand = new RelayCommand(ExecuteDeleteTeam, CanExecuteTeamOperations);
        }

        #endregion

        #region Command Implementations

        private void ExecuteCloseWindow(Window? window)
        {
            try
            {
                window?.Close();
                LoggingService.Instance.LogInfo($"TeamDetailWindow closed via MVVM for team {Team?.TeamName ?? "Unknown"}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error closing TeamDetailWindow via MVVM", ex);
            }
        }

        private void ExecuteRefreshTeam()
        {
            try
            {
                if (Team != null)
                {
                    // Team-Properties aktualisieren
                    UpdateTeamProperties();
                    TeamRefreshed?.Invoke(Team);
                    LoggingService.Instance.LogInfo($"Team details refreshed for {Team.TeamName}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error refreshing team details", ex);
            }
        }

        private bool CanExecuteRefreshTeam() => Team != null;

        private void ExecuteOpenTeamEdit()
        {
            try
            {
                if (Team != null)
                {
                    // Team-Edit-Dialog öffnen (implementiert in einer zukünftigen Version)
                    LoggingService.Instance.LogInfo($"Team edit requested for {Team.TeamName}");
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Team-Bearbeitung wird in einer zukünftigen Version implementiert.", 
                            "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening team edit", ex);
            }
        }

        private void ExecuteToggleTimer()
        {
            try
            {
                if (Team != null)
                {
                    if (Team.IsRunning)
                    {
                        Team.StopTimer();
                        LoggingService.Instance.LogInfo($"Timer stopped via DetailWindow for team {Team.TeamName}");
                    }
                    else
                    {
                        Team.StartTimer();
                        LoggingService.Instance.LogInfo($"Timer started via DetailWindow for team {Team.TeamName}");
                    }
                    
                    // Properties aktualisieren
                    OnPropertyChanged(nameof(Team));
                    TeamTimerChanged?.Invoke(Team);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error toggling timer via DetailWindow", ex);
            }
        }

        private void ExecuteResetTimer()
        {
            try
            {
                if (Team != null)
                {
                    var result = Application.Current.Dispatcher.Invoke(() =>
                    {
                        return MessageBox.Show(
                            $"Möchten Sie den Timer für Team '{Team.TeamName}' wirklich zurücksetzen?",
                            "Timer zurücksetzen", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    });

                    if (result == MessageBoxResult.Yes)
                    {
                        Team.ResetTimer();
                        LoggingService.Instance.LogInfo($"Timer reset via DetailWindow for team {Team.TeamName}");
                        
                        // Properties aktualisieren
                        OnPropertyChanged(nameof(Team));
                        TeamTimerChanged?.Invoke(Team);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error resetting timer via DetailWindow", ex);
            }
        }

        private void ExecuteDeleteTeam()
        {
            try
            {
                if (Team != null)
                {
                    var result = Application.Current.Dispatcher.Invoke(() =>
                    {
                        return MessageBox.Show(
                            $"Möchten Sie das Team '{Team.TeamName}' wirklich permanent löschen?\n\n" +
                            "Diese Aktion kann nicht rückgängig gemacht werden!",
                            "Team löschen", 
                            MessageBoxButton.YesNo, 
                            MessageBoxImage.Warning,
                            MessageBoxResult.No);
                    });

                    if (result == MessageBoxResult.Yes)
                    {
                        var teamToDelete = Team;
                        
                        // Timer stoppen falls aktiv
                        teamToDelete.StopTimer();
                        
                        // Event für Team-Löschung auslösen
                        TeamDeleteRequested?.Invoke(teamToDelete);
                        
                        LoggingService.Instance.LogInfo($"Team deletion requested via DetailWindow: {teamToDelete.TeamName}");
                        
                        // Fenster schließen nach erfolgreichem Löschen
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            if (Application.Current.Windows.Count > 0)
                            {
                                foreach (Window window in Application.Current.Windows)
                                {
                                    if (window is Views.TeamDetailWindow detailWindow && 
                                        detailWindow.DataContext == this)
                                    {
                                        window.Close();
                                        break;
                                    }
                                }
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error deleting team via DetailWindow", ex);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Fehler beim Löschen des Teams: {ex.Message}", 
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private bool CanExecuteTeamOperations() => Team != null;

        #endregion

        #region Public Methods

        /// <summary>
        /// Lädt ein Team in das ViewModel
        /// </summary>
        /// <param name="team">Das zu ladende Team</param>
        public void LoadTeam(Team team)
        {
            try
            {
                Team = team ?? throw new ArgumentNullException(nameof(team));
                IsTeamLoaded = true;
                
                LoggingService.Instance.LogInfo($"Team loaded in DetailViewModel: {team.TeamName}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading team in DetailViewModel", ex);
                IsTeamLoaded = false;
            }
        }

        #endregion

        #region Private Methods

        private void UpdateTeamProperties()
        {
            try
            {
                if (Team != null)
                {
                    WindowTitle = $"Team Details - {Team.TeamName}";
                    TeamName = Team.TeamName;
                    TeamType = Team.TeamTypeDisplayName;
                    
                    // Commands aktualisieren
                    ((RelayCommand)RefreshTeamCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)OpenTeamEditCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)ToggleTimerCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)ResetTimerCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)DeleteTeamCommand).RaiseCanExecuteChanged();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating team properties", ex);
            }
        }

        private void OnThemeChanged(bool isDarkMode)
        {
            IsDarkMode = isDarkMode;
            LoggingService.Instance.LogInfo($"Theme changed in TeamDetailViewModel: {(isDarkMode ? "Dark" : "Light")} mode");
        }

        #endregion

        #region Events

        /// <summary>
        /// Event wird ausgelöst, wenn das Team gelöscht werden soll
        /// </summary>
        public event Action<Team>? TeamDeleteRequested;

        /// <summary>
        /// Event wird ausgelöst, wenn das Team aktualisiert wurde
        /// </summary>
        public event Action<Team>? TeamRefreshed;

        /// <summary>
        /// Event wird ausgelöst, wenn der Timer geändert wurde
        /// </summary>
        public event Action<Team>? TeamTimerChanged;

        #endregion

        #region IDisposable Implementation

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Theme-Service Event abmelden
                    ThemeService.Instance.ThemeChanged -= OnThemeChanged;
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
