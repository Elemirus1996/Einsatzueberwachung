using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    /// <summary>
    /// ViewModel für das Team-Control - MVVM-Implementation v1.9.0
    /// Zentrale Team-UI-Komponente mit vollständiger Orange-Design-Integration
    /// </summary>
    public class TeamControlViewModel : BaseViewModel, IDisposable
    {
        private Team? _team;
        private bool _isDarkMode;
        private Storyboard? _blinkingStoryboard;
        
        // UI State Properties
        private string _teamName = string.Empty;
        private string _hundName = string.Empty;
        private string _hundefuehrer = string.Empty;
        private string _helfer = string.Empty;
        private string _suchgebiet = string.Empty;
        private string _elapsedTimeString = "00:00:00";
        private string _teamTypeText = "AL";
        private string _teamTypeDisplayName = "Allgemein";
        private string _teamTypeColorHex = "#607D8B";
        private string _warningSettingsText = "10/20";
        private string _warningStatusText = string.Empty;
        
        // Visual State Properties
        private Brush _teamBorderBackground = Brushes.Transparent;
        private Brush _teamBorderBrush = Brushes.Gray;
        private Brush _warningIndicatorBackground = Brushes.Transparent;
        private Brush _warningStatusForeground = Brushes.Transparent;
        private Thickness _teamBorderThickness = new Thickness(1);
        private bool _isBlinking;

        public TeamControlViewModel()
        {
            InitializeCommands();
            
            // UnifiedThemeManager statt ThemeService verwenden
            UnifiedThemeManager.Instance.ThemeChanged += OnThemeChanged;
            IsDarkMode = UnifiedThemeManager.Instance.IsDarkMode;
            
            LoggingService.Instance.LogInfo("TeamControlViewModel initialized with MVVM pattern v1.9.0 + UnifiedThemeManager");
        }

        public TeamControlViewModel(Team team) : this()
        {
            SetTeam(team);
        }

        #region Properties

        /// <summary>
        /// Das Team-Objekt für diese Control-Instanz
        /// </summary>
        public Team? Team
        {
            get => _team;
            private set
            {
                if (_team != null)
                {
                    UnsubscribeFromTeamEvents();
                }
                
                SetProperty(ref _team, value);
                
                if (_team != null)
                {
                    SubscribeToTeamEvents();
                    UpdateAllProperties();
                }
            }
        }

        /// <summary>
        /// Aktueller Theme-Modus (Dark/Light)
        /// </summary>
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (SetProperty(ref _isDarkMode, value))
                {
                    UpdateVisualState();
                }
            }
        }

        #region Team Properties

        public string TeamName
        {
            get => _teamName;
            set => SetProperty(ref _teamName, value);
        }

        public string HundName
        {
            get => _hundName;
            set
            {
                if (SetProperty(ref _hundName, value) && _team != null)
                {
                    _team.HundName = value;
                    NotifyDataChanged();
                }
            }
        }

        public string Hundefuehrer
        {
            get => _hundefuehrer;
            set
            {
                if (SetProperty(ref _hundefuehrer, value) && _team != null)
                {
                    _team.Hundefuehrer = value;
                    NotifyDataChanged();
                }
            }
        }

        public string Helfer
        {
            get => _helfer;
            set
            {
                if (SetProperty(ref _helfer, value) && _team != null)
                {
                    _team.Helfer = value;
                    NotifyDataChanged();
                }
            }
        }

        public string Suchgebiet
        {
            get => _suchgebiet;
            set
            {
                if (SetProperty(ref _suchgebiet, value) && _team != null)
                {
                    _team.Suchgebiet = value;
                    NotifyDataChanged();
                }
            }
        }

        public string ElapsedTimeString
        {
            get => _elapsedTimeString;
            set => SetProperty(ref _elapsedTimeString, value);
        }

        #endregion

        #region Team Type Properties

        public string TeamTypeText
        {
            get => _teamTypeText;
            set => SetProperty(ref _teamTypeText, value);
        }

        public string TeamTypeDisplayName
        {
            get => _teamTypeDisplayName;
            set => SetProperty(ref _teamTypeDisplayName, value);
        }

        public string TeamTypeColorHex
        {
            get => _teamTypeColorHex;
            set => SetProperty(ref _teamTypeColorHex, value);
        }

        public Brush TeamTypeBadgeBackground
        {
            get
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(TeamTypeColorHex);
                    return new SolidColorBrush(color);
                }
                catch
                {
                    return new SolidColorBrush(Colors.Gray);
                }
            }
        }

        #endregion

        #region Warning Properties

        public string WarningSettingsText
        {
            get => _warningSettingsText;
            set => SetProperty(ref _warningSettingsText, value);
        }

        public string WarningStatusText
        {
            get => _warningStatusText;
            set => SetProperty(ref _warningStatusText, value);
        }

        #endregion

        #region Visual State Properties

        public Brush TeamBorderBackground
        {
            get => _teamBorderBackground;
            set => SetProperty(ref _teamBorderBackground, value);
        }

        public Brush TeamBorderBrush
        {
            get => _teamBorderBrush;
            set => SetProperty(ref _teamBorderBrush, value);
        }

        public Brush WarningIndicatorBackground
        {
            get => _warningIndicatorBackground;
            set => SetProperty(ref _warningIndicatorBackground, value);
        }

        public Brush WarningStatusForeground
        {
            get => _warningStatusForeground;
            set => SetProperty(ref _warningStatusForeground, value);
        }

        public Thickness TeamBorderThickness
        {
            get => _teamBorderThickness;
            set => SetProperty(ref _teamBorderThickness, value);
        }

        public bool IsBlinking
        {
            get => _isBlinking;
            set => SetProperty(ref _isBlinking, value);
        }

        #endregion

        #endregion

        #region Commands

        public ICommand StartTimerCommand { get; private set; } = null!;
        public ICommand StopTimerCommand { get; private set; } = null!;
        public ICommand ResetTimerCommand { get; private set; } = null!;
        public ICommand DeleteTeamCommand { get; private set; } = null!;
        public ICommand EditTeamTypeCommand { get; private set; } = null!;
        public ICommand EditWarningSettingsCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            StartTimerCommand = new RelayCommand(ExecuteStartTimer, CanExecuteStartTimer);
            StopTimerCommand = new RelayCommand(ExecuteStopTimer, CanExecuteStopTimer);
            ResetTimerCommand = new RelayCommand(ExecuteResetTimer, CanExecuteResetTimer);
            DeleteTeamCommand = new RelayCommand(ExecuteDeleteTeam, CanExecuteDeleteTeam);
            EditTeamTypeCommand = new RelayCommand(ExecuteEditTeamType, CanExecuteEditTeamType);
            EditWarningSettingsCommand = new RelayCommand(ExecuteEditWarningSettings, CanExecuteEditWarningSettings);
        }

        #endregion

        #region Command Implementations

        private bool CanExecuteStartTimer() => _team != null && !_team.IsRunning;

        private void ExecuteStartTimer()
        {
            try
            {
                _team?.StartTimer();
                LoggingService.Instance.LogInfo($"Timer started via MVVM for team {_team?.TeamName}");
                UpdateCommandStates();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error starting timer via MVVM for team {_team?.TeamName}", ex);
            }
        }

        private bool CanExecuteStopTimer() => _team != null && _team.IsRunning;

        private void ExecuteStopTimer()
        {
            try
            {
                _team?.StopTimer();
                LoggingService.Instance.LogInfo($"Timer stopped via MVVM for team {_team?.TeamName} at {_team?.ElapsedTimeString}");
                UpdateCommandStates();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error stopping timer via MVVM for team {_team?.TeamName}", ex);
            }
        }

        private bool CanExecuteResetTimer() => _team != null;

        private void ExecuteResetTimer()
        {
            try
            {
                if (_team != null)
                {
                    var result = Application.Current.Dispatcher.Invoke(() =>
                    {
                        return MessageBox.Show($"Timer für {_team.TeamName} zurücksetzen?", 
                            "Timer Reset", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    });
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        _team.ResetTimer();
                        UpdateVisualState();
                        LoggingService.Instance.LogInfo($"Timer reset via MVVM for team {_team.TeamName}");
                        UpdateCommandStates();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error resetting timer via MVVM for team {_team?.TeamName}", ex);
            }
        }

        private bool CanExecuteDeleteTeam() => _team != null;

        private void ExecuteDeleteTeam()
        {
            try
            {
                if (_team != null)
                {
                    var result = Application.Current.Dispatcher.Invoke(() =>
                    {
                        return MessageBox.Show($"Team '{_team.TeamName}' wirklich löschen?", 
                            "Team löschen", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    });
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        _team.StopTimer();
                        TeamDeleteRequested?.Invoke(_team);
                        LoggingService.Instance.LogInfo($"Team deletion requested via MVVM: {_team.TeamName}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error deleting team via MVVM {_team?.TeamName}", ex);
            }
        }

        private bool CanExecuteEditTeamType() => _team != null;

        private void ExecuteEditTeamType()
        {
            try
            {
                if (_team != null)
                {
                    var teamTypeDialog = new Views.TeamTypeSelectionWindow(_team?.MultipleTeamTypes);
                    if (teamTypeDialog.ShowDialog() == true)
                    {
                        _team.MultipleTeamTypes = teamTypeDialog.SelectedMultipleTeamTypes;
                        UpdateTeamTypeProperties();
                        NotifyDataChanged();
                        LoggingService.Instance.LogInfo($"Team types updated via MVVM for {_team.TeamName}: {_team.TeamTypeDisplayName}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error editing team types via MVVM for team {_team?.TeamName}", ex);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Fehler beim Bearbeiten der Team-Typen: {ex.Message}", 
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private bool CanExecuteEditWarningSettings() => _team != null;

        private void ExecuteEditWarningSettings()
        {
            try
            {
                if (_team != null)
                {
                    var teams = new List<Team> { _team };
                    
                    // Get global warning settings from MainViewModel via MainWindow
                    int globalFirst = 10;
                    int globalSecond = 20;
                    
                    // Try to get global settings from current application's MainWindow ViewModel
                    if (Application.Current.MainWindow is MainWindow mainWindow && 
                        mainWindow.DataContext is MainViewModel mainViewModel)
                    {
                        globalFirst = mainViewModel.GlobalFirstWarningMinutes;
                        globalSecond = mainViewModel.GlobalSecondWarningMinutes;
                    }
                    
                    var warningSettingsWindow = new Views.TeamWarningSettingsWindow(teams, globalFirst, globalSecond)
                    {
                        Owner = Application.Current.MainWindow,
                        Title = $"Warnschwellen - {_team.TeamName}"
                    };
                    
                    if (warningSettingsWindow.ShowDialog() == true)
                    {
                        UpdateWarningSettingsDisplay();
                        NotifyDataChanged();
                        LoggingService.Instance.LogInfo($"Warning settings updated via MVVM for {_team.TeamName}: " +
                            $"{_team.FirstWarningMinutes}/{_team.SecondWarningMinutes} minutes");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error opening warning settings via MVVM for team {_team?.TeamName}", ex);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Fehler beim Öffnen der Warnschwellen-Einstellungen: {ex.Message}", 
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Setzt ein neues Team für diese Control-Instanz
        /// </summary>
        public void SetTeam(Team team)
        {
            Team = team ?? throw new ArgumentNullException(nameof(team));
        }

        /// <summary>
        /// Wendet das aktuelle Theme auf die Control an
        /// </summary>
        public void ApplyTheme(bool isDarkMode)
        {
            IsDarkMode = isDarkMode;
        }

        /// <summary>
        /// Startet die Entrance-Animation für die Control
        /// </summary>
        public void TriggerEntranceAnimation()
        {
            // Animation wird über View-spezifische Implementierung gehandhabt
            EntranceAnimationRequested?.Invoke();
        }

        /// <summary>
        /// Startet oder stoppt die Blinking-Animation
        /// </summary>
        public void SetBlinkingAnimation(bool shouldBlink, Storyboard? blinkingStoryboard = null)
        {
            _blinkingStoryboard = blinkingStoryboard;
            
            if (shouldBlink && !IsBlinking)
            {
                IsBlinking = true;
                BlinkingAnimationRequested?.Invoke(true);
            }
            else if (!shouldBlink && IsBlinking)
            {
                IsBlinking = false;
                BlinkingAnimationRequested?.Invoke(false);
            }
        }

        #endregion

        #region Private Methods

        private void SubscribeToTeamEvents()
        {
            if (_team != null)
            {
                _team.PropertyChanged += OnTeamPropertyChanged;
                _team.WarningTriggered += OnTeamWarningTriggered;
            }
        }

        private void UnsubscribeFromTeamEvents()
        {
            if (_team != null)
            {
                _team.PropertyChanged -= OnTeamPropertyChanged;
                _team.WarningTriggered -= OnTeamWarningTriggered;
            }
        }

        private void OnTeamPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Team.ElapsedTimeString):
                case nameof(Team.ElapsedTime):
                    ElapsedTimeString = _team?.ElapsedTimeString ?? "00:00:00";
                    break;
                case nameof(Team.HundName):
                    HundName = _team?.HundName ?? string.Empty;
                    break;
                case nameof(Team.Hundefuehrer):
                    Hundefuehrer = _team?.Hundefuehrer ?? string.Empty;
                    break;
                case nameof(Team.Helfer):
                    Helfer = _team?.Helfer ?? string.Empty;
                    break;
                case nameof(Team.Suchgebiet):
                    Suchgebiet = _team?.Suchgebiet ?? string.Empty;
                    break;
                case nameof(Team.IsFirstWarning):
                case nameof(Team.IsSecondWarning):
                    UpdateVisualState();
                    break;
                case nameof(Team.IsRunning):
                    UpdateCommandStates();
                    NotifyDataChanged();
                    break;
                case nameof(Team.MultipleTeamTypes):
                case nameof(Team.TeamTypeDisplayName):
                case nameof(Team.TeamTypeColorHex):
                    UpdateTeamTypeProperties();
                    NotifyDataChanged();
                    break;
            }
        }

        private async void OnTeamWarningTriggered(Team team, bool isSecondWarning)
        {
            try
            {
                UpdateVisualState();
                
                // KORRIGIERT: Stelle sicher, dass Sound immer abgespielt wird
                LoggingService.Instance.LogInfo($"Warning triggered for team {team.TeamName}: {(isSecondWarning ? "Second" : "First")} warning");
                
                // Sound abspielen (async ohne ConfigureAwait(false) für UI-Thread)
                try
                {
                    await SoundService.Instance.PlayWarningSound(isSecondWarning);
                    LoggingService.Instance.LogInfo($"Warning sound played successfully for team {team.TeamName}");
                }
                catch (Exception soundEx)
                {
                    LoggingService.Instance.LogError($"Failed to play warning sound for team {team.TeamName}", soundEx);
                    
                    // Fallback: System-Sound
                    try
                    {
                        System.Media.SystemSounds.Exclamation.Play();
                    }
                    catch (Exception fallbackEx)
                    {
                        LoggingService.Instance.LogError("Even fallback system sound failed", fallbackEx);
                    }
                }
                
                string warningType = isSecondWarning ? "Second" : "First";
                LoggingService.Instance.LogWarning($"Warning triggered via MVVM for {team.TeamName}: {warningType} warning at {team.ElapsedTimeString}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error handling warning via MVVM for team {team.TeamName}", ex);
            }
        }

        private void UpdateAllProperties()
        {
            if (_team != null)
            {
                TeamName = _team.TeamName;
                HundName = _team.HundName;
                Hundefuehrer = _team.Hundefuehrer;
                Helfer = _team.Helfer;
                Suchgebiet = _team.Suchgebiet;
                ElapsedTimeString = _team.ElapsedTimeString;
                
                UpdateTeamTypeProperties();
                UpdateWarningSettingsDisplay();
                UpdateVisualState();
                UpdateCommandStates();
            }
        }

        private void UpdateTeamTypeProperties()
        {
            if (_team?.MultipleTeamTypes != null)
            {
                TeamTypeText = _team.TeamTypeShortName;
                TeamTypeDisplayName = _team.TeamTypeDisplayName;
                TeamTypeColorHex = _team.TeamTypeColorHex;
                OnPropertyChanged(nameof(TeamTypeBadgeBackground));
            }
        }

        private void UpdateWarningSettingsDisplay()
        {
            if (_team != null)
            {
                WarningSettingsText = $"{_team.FirstWarningMinutes}/{_team.SecondWarningMinutes}";
            }
        }

        private void UpdateVisualState()
        {
            if (_team == null) return;

            try
            {
                // Determine colors based on theme
                var errorColor = GetThemeColor("Error");
                var warningColor = GetThemeColor("Warning");
                var surfaceColor = GetThemeColor(IsDarkMode ? "DarkSurfaceContainer" : "Surface");
                var outlineColor = GetThemeColor(IsDarkMode ? "DarkOutline" : "Outline");

                if (_team.IsSecondWarning)
                {
                    // Critical state - red blinking
                    TeamBorderBackground = errorColor;
                    TeamBorderBrush = GetThemeColor("OnError");
                    TeamBorderThickness = new Thickness(3);
                    WarningIndicatorBackground = errorColor;
                    WarningStatusText = "KRITISCH!";
                    WarningStatusForeground = GetThemeColor("OnError");
                    SetBlinkingAnimation(true);
                }
                else if (_team.IsFirstWarning)
                {
                    // Warning state - orange/yellow
                    TeamBorderBackground = warningColor;
                    TeamBorderBrush = GetThemeColor("OnWarning");
                    TeamBorderThickness = new Thickness(2);
                    WarningIndicatorBackground = GetThemeColor("WarningContainer");
                    WarningStatusText = "WARNUNG!";
                    WarningStatusForeground = GetThemeColor("OnWarning");
                    SetBlinkingAnimation(false);
                }
                else
                {
                    // Normal state
                    TeamBorderBackground = surfaceColor;
                    TeamBorderBrush = outlineColor;
                    TeamBorderThickness = new Thickness(1);
                    WarningIndicatorBackground = Brushes.Transparent;
                    WarningStatusText = string.Empty;
                    WarningStatusForeground = Brushes.Transparent;
                    SetBlinkingAnimation(false);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating visual state in TeamControlViewModel", ex);
                // Fallback to default colors
                TeamBorderBackground = Brushes.White;
                TeamBorderBrush = Brushes.Gray;
                TeamBorderThickness = new Thickness(1);
                SetBlinkingAnimation(false);
            }
        }

        private Brush GetThemeColor(string resourceKey)
        {
            try
            {
                if (Application.Current?.FindResource(resourceKey) is Brush brush)
                {
                    return brush;
                }
            }
            catch
            {
                // Ignore and fall back
            }
            
            // Fallback colors
            return resourceKey switch
            {
                "Error" => Brushes.Red,
                "Warning" => Brushes.Orange,
                "OnError" => Brushes.White,
                "OnWarning" => Brushes.Black,
                "WarningContainer" => Brushes.LightYellow,
                _ => Brushes.Gray
            };
        }

        private void UpdateCommandStates()
        {
            ((RelayCommand)StartTimerCommand).RaiseCanExecuteChanged();
            ((RelayCommand)StopTimerCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ResetTimerCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DeleteTeamCommand).RaiseCanExecuteChanged();
            ((RelayCommand)EditTeamTypeCommand).RaiseCanExecuteChanged();
            ((RelayCommand)EditWarningSettingsCommand).RaiseCanExecuteChanged();
        }

        private void OnThemeChanged(bool isDarkMode)
        {
            IsDarkMode = isDarkMode;
            LoggingService.Instance.LogInfo($"Theme changed in TeamControlViewModel: {(isDarkMode ? "Dark" : "Light")} mode");
        }

        private void NotifyDataChanged()
        {
            DataChanged?.Invoke();
        }

        #endregion

        #region Events

        /// <summary>
        /// Event wird ausgelöst, wenn das Team gelöscht werden soll
        /// </summary>
        public event Action<Team>? TeamDeleteRequested;

        /// <summary>
        /// Event wird ausgelöst, wenn sich Team-Daten geändert haben
        /// </summary>
        public event Action? DataChanged;

        /// <summary>
        /// Event wird ausgelöst, wenn die Entrance-Animation gestartet werden soll
        /// </summary>
        public event Action? EntranceAnimationRequested;

        /// <summary>
        /// Event wird ausgelöst, wenn die Blinking-Animation gestartet/gestoppt werden soll
        /// </summary>
        public event Action<bool>? BlinkingAnimationRequested;

        #endregion

        #region IDisposable Implementation

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    UnsubscribeFromTeamEvents();
                    UnifiedThemeManager.Instance.ThemeChanged -= OnThemeChanged;
                    _blinkingStoryboard = null;
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
