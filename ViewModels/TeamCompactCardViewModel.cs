using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    /// <summary>
    /// ViewModel für das TeamCompactCard - MVVM-Implementation v1.9.0
    /// Dashboard-Hauptkomponente mit vollständiger Orange-Design-Integration
    /// </summary>
    public class TeamCompactCardViewModel : BaseViewModel, IDisposable
    {
        private Team? _team;
        private bool _isDarkMode = false;

        // Team Properties for UI Binding
        private string _teamName = string.Empty;
        private string _hundefuehrer = string.Empty;
        private string _helfer = string.Empty;
        private string _suchgebiet = string.Empty;
        private string _elapsedTimeString = "00:00:00";
        private string _teamTypeText = "AL";
        private string _teamTypeColorHex = "#F57C00";

        // Visual State Properties
        private Brush _cardBackground;
        private Brush _cardBorderBrush;
        private Thickness _cardBorderThickness = new Thickness(1);
        private Brush _warningIndicatorBackground;
        private Brush _statusIndicatorBackground;
        private string _statusText = "BEREIT";
        private Brush _statusTextForeground;
        private Brush _teamTypeBadgeBackground;

        // UI State Properties
        private bool _isHovered = false;
        private bool _isWarningActive = false;

        public TeamCompactCardViewModel()
        {
            InitializeCommands();
            InitializeTheme();
            
            LoggingService.Instance.LogInfo("TeamCompactCardViewModel initialized with MVVM pattern v1.9.0");
        }

        #region Properties

        #region Team Data Properties

        /// <summary>
        /// Team-Name für Display
        /// </summary>
        public string TeamName
        {
            get => _teamName;
            set => SetProperty(ref _teamName, value);
        }

        /// <summary>
        /// Hundeführer-Name
        /// </summary>
        public string Hundefuehrer
        {
            get => _hundefuehrer;
            set => SetProperty(ref _hundefuehrer, value);
        }

        /// <summary>
        /// Helfer-Name
        /// </summary>
        public string Helfer
        {
            get => _helfer;
            set => SetProperty(ref _helfer, value);
        }

        /// <summary>
        /// Suchgebiet-Bezeichnung
        /// </summary>
        public string Suchgebiet
        {
            get => _suchgebiet;
            set => SetProperty(ref _suchgebiet, value);
        }

        /// <summary>
        /// Formatierte Einsatzzeit
        /// </summary>
        public string ElapsedTimeString
        {
            get => _elapsedTimeString;
            set => SetProperty(ref _elapsedTimeString, value);
        }

        /// <summary>
        /// Team-Type-Text für Badge
        /// </summary>
        public string TeamTypeText
        {
            get => _teamTypeText;
            set => SetProperty(ref _teamTypeText, value);
        }

        /// <summary>
        /// Team-Type-Color als Hex-String
        /// </summary>
        public string TeamTypeColorHex
        {
            get => _teamTypeColorHex;
            set => SetProperty(ref _teamTypeColorHex, value);
        }

        #endregion

        #region Visual State Properties

        /// <summary>
        /// Card-Hintergrundfarbe
        /// </summary>
        public Brush CardBackground
        {
            get => _cardBackground;
            set => SetProperty(ref _cardBackground, value);
        }

        /// <summary>
        /// Card-Border-Farbe
        /// </summary>
        public Brush CardBorderBrush
        {
            get => _cardBorderBrush;
            set => SetProperty(ref _cardBorderBrush, value);
        }

        /// <summary>
        /// Card-Border-Thickness
        /// </summary>
        public Thickness CardBorderThickness
        {
            get => _cardBorderThickness;
            set => SetProperty(ref _cardBorderThickness, value);
        }

        /// <summary>
        /// Warning-Indicator-Hintergrundfarbe
        /// </summary>
        public Brush WarningIndicatorBackground
        {
            get => _warningIndicatorBackground;
            set => SetProperty(ref _warningIndicatorBackground, value);
        }

        /// <summary>
        /// Status-Indicator-Hintergrundfarbe
        /// </summary>
        public Brush StatusIndicatorBackground
        {
            get => _statusIndicatorBackground;
            set => SetProperty(ref _statusIndicatorBackground, value);
        }

        /// <summary>
        /// Status-Text (AKTIV/BEREIT)
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        /// <summary>
        /// Status-Text-Vordergrundfarbe
        /// </summary>
        public Brush StatusTextForeground
        {
            get => _statusTextForeground;
            set => SetProperty(ref _statusTextForeground, value);
        }

        /// <summary>
        /// Team-Type-Badge-Hintergrundfarbe
        /// </summary>
        public Brush TeamTypeBadgeBackground
        {
            get => _teamTypeBadgeBackground;
            set => SetProperty(ref _teamTypeBadgeBackground, value);
        }

        /// <summary>
        /// Ist die Card gehovered?
        /// </summary>
        public bool IsHovered
        {
            get => _isHovered;
            set => SetProperty(ref _isHovered, value);
        }

        /// <summary>
        /// Ist eine Warnung aktiv?
        /// </summary>
        public bool IsWarningActive
        {
            get => _isWarningActive;
            set => SetProperty(ref _isWarningActive, value);
        }

        #endregion

        #endregion

        #region Commands

        public ICommand CardClickCommand { get; private set; } = null!;
        public ICommand CardMouseEnterCommand { get; private set; } = null!;
        public ICommand CardMouseLeaveCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            CardClickCommand = new RelayCommand(ExecuteCardClick);
            CardMouseEnterCommand = new RelayCommand(ExecuteCardMouseEnter);
            CardMouseLeaveCommand = new RelayCommand(ExecuteCardMouseLeave);
        }

        #endregion

        #region Command Implementations

        private void ExecuteCardClick()
        {
            try
            {
                if (_team != null)
                {
                    TeamClicked?.Invoke(_team);
                    LoggingService.Instance.LogInfo($"TeamCompactCard clicked via MVVM: {_team.TeamName}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling card click via MVVM", ex);
            }
        }

        private void ExecuteCardMouseEnter()
        {
            try
            {
                IsHovered = true;
                
                // Keine Hover-Effekte bei Warnungen
                if (IsWarningActive) return;
                
                // Orange-aware Hover-Effekt
                CardBackground = GetThemeColor(_isDarkMode ? "DarkSurfaceContainerHigh" : "SurfaceVariant");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling mouse enter via MVVM", ex);
            }
        }

        private void ExecuteCardMouseLeave()
        {
            try
            {
                IsHovered = false;
                
                // Stelle den originalen Zustand wieder her
                if (IsWarningActive)
                {
                    UpdateWarningState(); // Restore warning state
                }
                else
                {
                    UpdateNormalState(); // Restore normal state
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling mouse leave via MVVM", ex);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Setzt das zu überwachende Team
        /// </summary>
        /// <param name="team">Das Team-Objekt</param>
        public void SetTeam(Team? team)
        {
            try
            {
                // Alte Team-Events abmelden
                if (_team != null)
                {
                    _team.PropertyChanged -= OnTeamPropertyChanged;
                }

                _team = team;

                // Neue Team-Events anmelden
                if (_team != null)
                {
                    _team.PropertyChanged += OnTeamPropertyChanged;
                    UpdateAllFromTeam();
                }
                else
                {
                    ClearTeamData();
                }
                
                LoggingService.Instance.LogInfo($"Team set in TeamCompactCardViewModel: {team?.TeamName ?? "null"}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error setting team in TeamCompactCardViewModel", ex);
            }
        }

        /// <summary>
        /// Wendet das Theme an
        /// </summary>
        /// <param name="isDarkMode">Ist Dark-Mode aktiv?</param>
        public void ApplyTheme(bool isDarkMode)
        {
            try
            {
                _isDarkMode = isDarkMode;
                
                if (!IsWarningActive)
                {
                    UpdateNormalState();
                }
                
                LoggingService.Instance.LogInfo($"Theme applied to TeamCompactCardViewModel: {(isDarkMode ? "Dark" : "Light")}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to TeamCompactCardViewModel", ex);
            }
        }

        #endregion

        #region Private Methods

        private void InitializeTheme()
        {
            try
            {
                // Initialize default brushes with Orange design system
                _cardBackground = GetThemeColor("SurfaceContainer");
                _cardBorderBrush = GetThemeColor("Primary");
                _warningIndicatorBackground = Brushes.Transparent;
                _statusIndicatorBackground = GetThemeColor("OnSurfaceVariant");
                _statusTextForeground = GetThemeColor("OnSurface");
                _teamTypeBadgeBackground = GetThemeColor("Primary");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing theme in TeamCompactCardViewModel", ex);
                
                // Fallback to simple brushes
                _cardBackground = Brushes.White;
                _cardBorderBrush = Brushes.DarkOrange;
                _warningIndicatorBackground = Brushes.Transparent;
                _statusIndicatorBackground = Brushes.Gray;
                _statusTextForeground = Brushes.Black;
                _teamTypeBadgeBackground = Brushes.DarkOrange;
            }
        }

        private void OnTeamPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    switch (e.PropertyName)
                    {
                        case nameof(Team.ElapsedTimeString):
                        case nameof(Team.ElapsedTime):
                            ElapsedTimeString = _team?.ElapsedTimeString ?? "00:00:00";
                            break;
                        case nameof(Team.IsFirstWarning):
                        case nameof(Team.IsSecondWarning):
                            UpdateWarningState();
                            break;
                        case nameof(Team.IsRunning):
                            UpdateStatusDisplay();
                            break;
                        case nameof(Team.TeamName):
                            TeamName = _team?.TeamName ?? string.Empty;
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
                        case nameof(Team.MultipleTeamTypes):
                            UpdateTeamTypeBadge();
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error handling team property change: {e.PropertyName}", ex);
            }
        }

        private void UpdateAllFromTeam()
        {
            if (_team == null) return;

            try
            {
                // Update all properties from team
                TeamName = _team.TeamName ?? string.Empty;
                Hundefuehrer = _team.Hundefuehrer ?? string.Empty;
                Helfer = _team.Helfer ?? string.Empty;
                Suchgebiet = _team.Suchgebiet ?? string.Empty;
                ElapsedTimeString = _team.ElapsedTimeString ?? "00:00:00";

                // Update visual states
                UpdateTeamTypeBadge();
                UpdateWarningState();
                UpdateStatusDisplay();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating all data from team", ex);
            }
        }

        private void ClearTeamData()
        {
            try
            {
                TeamName = string.Empty;
                Hundefuehrer = string.Empty;
                Helfer = string.Empty;
                Suchgebiet = string.Empty;
                ElapsedTimeString = "00:00:00";
                TeamTypeText = "AL";
                TeamTypeColorHex = "#F57C00";
                
                UpdateNormalState();
                UpdateStatusDisplay();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error clearing team data", ex);
            }
        }

        private void UpdateTeamTypeBadge()
        {
            try
            {
                if (_team?.MultipleTeamTypes != null)
                {
                    TeamTypeText = _team.TeamTypeShortName;
                    TeamTypeColorHex = _team.TeamTypeColorHex;
                    
                    var color = (Color)ColorConverter.ConvertFromString(TeamTypeColorHex);
                    TeamTypeBadgeBackground = new SolidColorBrush(color);
                }
                else
                {
                    TeamTypeText = "AL";
                    TeamTypeColorHex = "#F57C00";
                    TeamTypeBadgeBackground = GetThemeColor("Primary");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating team type badge", ex);
                
                // Fallback
                TeamTypeText = "AL";
                TeamTypeColorHex = "#F57C00";
                TeamTypeBadgeBackground = GetThemeColor("Primary");
            }
        }

        private void UpdateWarningState()
        {
            if (_team == null) return;

            try
            {
                if (_team.IsSecondWarning)
                {
                    // Critical warning - Orange Error colors
                    IsWarningActive = true;
                    CardBackground = GetThemeColor("Error");
                    CardBorderBrush = GetThemeColor("OnError");
                    CardBorderThickness = new Thickness(3);
                    WarningIndicatorBackground = GetThemeColor("OnError");
                }
                else if (_team.IsFirstWarning)
                {
                    // First warning - Orange Warning colors
                    IsWarningActive = true;
                    CardBackground = GetThemeColor("Warning");
                    CardBorderBrush = GetThemeColor("WarningContainer");
                    CardBorderThickness = new Thickness(2);
                    WarningIndicatorBackground = GetThemeColor("OnWarning");
                }
                else
                {
                    // Normal state
                    IsWarningActive = false;
                    UpdateNormalState();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating warning state", ex);
            }
        }

        private void UpdateStatusDisplay()
        {
            if (_team == null) return;

            try
            {
                if (_team.IsRunning)
                {
                    StatusIndicatorBackground = GetThemeColor("Success");
                    StatusText = "AKTIV";
                    StatusTextForeground = GetThemeColor("OnSuccess");
                }
                else
                {
                    StatusIndicatorBackground = GetThemeColor("OnSurfaceVariant");
                    StatusText = "BEREIT";
                    StatusTextForeground = GetThemeColor("OnSurface");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating status display", ex);
            }
        }

        private void UpdateNormalState()
        {
            try
            {
                // Orange-aware Normal state
                CardBackground = GetThemeColor(_isDarkMode ? "DarkSurfaceContainer" : "SurfaceContainer");
                CardBorderBrush = GetThemeColor("Primary");
                CardBorderThickness = new Thickness(1);
                WarningIndicatorBackground = Brushes.Transparent;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating normal state", ex);
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
            
            // Orange-focused fallback colors
            return resourceKey switch
            {
                "Primary" => Brushes.DarkOrange,
                "SurfaceContainer" => Brushes.White,
                "DarkSurfaceContainer" => new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                "SurfaceVariant" => new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                "DarkSurfaceContainerHigh" => new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                "Success" => Brushes.Green,
                "Error" => Brushes.Red,
                "Warning" => Brushes.Orange,
                "WarningContainer" => new SolidColorBrush(Color.FromRgb(255, 193, 128)),
                "OnError" => Brushes.White,
                "OnWarning" => Brushes.White,
                "OnSuccess" => Brushes.White,
                "OnSurface" => Brushes.Black,
                "OnSurfaceVariant" => Brushes.Gray,
                _ => Brushes.DarkOrange
            };
        }

        #endregion

        #region Events

        /// <summary>
        /// Event wird ausgelöst, wenn auf die Team-Card geklickt wird
        /// </summary>
        public event Action<Team>? TeamClicked;

        #endregion

        #region IDisposable Implementation

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Clean up managed resources
                    if (_team != null)
                    {
                        _team.PropertyChanged -= OnTeamPropertyChanged;
                        _team = null;
                    }
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
