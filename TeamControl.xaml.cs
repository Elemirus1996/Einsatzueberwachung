using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung
{
    public partial class TeamControl : UserControl, INotifyPropertyChanged
    {
        private Team? _team;
        private Storyboard? _blinkingStoryboard;
        private bool _isDarkMode = false;

        public static readonly DependencyProperty TeamProperty =
            DependencyProperty.Register("Team", typeof(Team), typeof(TeamControl),
                new PropertyMetadata(null, OnTeamChanged));

        public Team? Team
        {
            get => (Team?)GetValue(TeamProperty);
            set => SetValue(TeamProperty, value);
        }

        public string TeamName => Team?.TeamName ?? "";
        public string HundName
        {
            get => Team?.HundName ?? "";
            set { if (Team != null) { Team.HundName = value; OnPropertyChanged(nameof(HundName)); } }
        }
        public string Hundefuehrer
        {
            get => Team?.Hundefuehrer ?? "";
            set { if (Team != null) { Team.Hundefuehrer = value; OnPropertyChanged(nameof(Hundefuehrer)); } }
        }
        public string Helfer
        {
            get => Team?.Helfer ?? "";
            set { if (Team != null) { Team.Helfer = value; OnPropertyChanged(nameof(Helfer)); } }
        }
        public string Suchgebiet
        {
            get => Team?.Suchgebiet ?? "";
            set { if (Team != null) { Team.Suchgebiet = value; OnPropertyChanged(nameof(Suchgebiet)); } }
        }
        public string ElapsedTimeString => Team?.ElapsedTimeString ?? "00:00:00";

        public TeamControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void ApplyTheme(bool isDarkMode)
        {
            _isDarkMode = isDarkMode;
            
            if (isDarkMode)
            {
                // Dark theme colors - use design system
                Resources["TeamBackgroundBrush"] = FindResource("DarkSurfaceContainer");
                Resources["TeamBorderBrush"] = FindResource("DarkOutline");
                Resources["TeamTextBrush"] = FindResource("DarkOnSurface");
            }
            else
            {
                // Light theme colors - use design system
                Resources["TeamBackgroundBrush"] = FindResource("Surface");
                Resources["TeamBorderBrush"] = FindResource("Outline");
                Resources["TeamTextBrush"] = FindResource("OnSurface");
            }
            
            // Re-apply warning state to maintain warning colors
            UpdateWarningState();
        }

        private static void OnTeamChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TeamControl control)
            {
                if (control._team != null)
                {
                    control._team.PropertyChanged -= control.Team_PropertyChanged;
                    control._team.WarningTriggered -= control.Team_WarningTriggered;
                }

                control._team = e.NewValue as Team;

                if (control._team != null)
                {
                    control._team.PropertyChanged += control.Team_PropertyChanged;
                    control._team.WarningTriggered += control.Team_WarningTriggered;
                    control.UpdateUI();
                }
            }
        }

        private void Team_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Use Immediate UI updates for timer-related changes
            switch (e.PropertyName)
            {
                case nameof(Team.ElapsedTimeString):
                case nameof(Team.ElapsedTime):
                    // Timer updates need immediate response
                    OnPropertyChanged(nameof(ElapsedTimeString));
                    break;
                case nameof(Team.HundName):
                    OnPropertyChanged(nameof(HundName));
                    NotifyDataChanged();
                    break;
                case nameof(Team.Hundefuehrer):
                    OnPropertyChanged(nameof(Hundefuehrer));
                    NotifyDataChanged();
                    break;
                case nameof(Team.Helfer):
                    OnPropertyChanged(nameof(Helfer));
                    NotifyDataChanged();
                    break;
                case nameof(Team.Suchgebiet):
                    OnPropertyChanged(nameof(Suchgebiet));
                    NotifyDataChanged();
                    break;
                case nameof(Team.IsFirstWarning):
                case nameof(Team.IsSecondWarning):
                    UpdateWarningState();
                    break;
                case nameof(Team.IsRunning):
                    NotifyDataChanged();
                    break;
                // v1.5: Handle multiple team types changes
                case nameof(Team.MultipleTeamTypes):
                case nameof(Team.TeamTypeDisplayName):
                case nameof(Team.TeamTypeColorHex):
                    UpdateTeamTypeBadge();
                    NotifyDataChanged();
                    break;
            }
        }

        private void NotifyDataChanged()
        {
            // Find parent window and notify of data change
            if (Window.GetWindow(this) is MainWindow window)
            {
                // Use reflection to call private method
                var method = window.GetType().GetMethod("NotifyDataChanged", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(window, null);
            }
        }

        private async void Team_WarningTriggered(Team team, bool isSecondWarning)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    UpdateWarningState();
                    await SoundService.Instance.PlayWarningSound(isSecondWarning).ConfigureAwait(false);
                    
                    string warningType = isSecondWarning ? "Second" : "First";
                    LoggingService.Instance.LogWarning($"Warning triggered for {team.TeamName}: {warningType} warning at {team.ElapsedTimeString}");
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError($"Error handling warning for team {team.TeamName}", ex);
                }
            });
        }

        private void UpdateWarningState()
        {
            if (_team == null) return;

            if (_team.IsSecondWarning)
            {
                // Red blinking background - use design system Error colors
                TeamBorder.Background = (Brush)FindResource("Error");
                TeamBorder.BorderBrush = (Brush)FindResource("OnError");
                TeamBorder.BorderThickness = new Thickness(3);
                
                // Update warning indicators
                WarningIndicator.Background = (Brush)FindResource("Error");
                WarningStatusText.Text = "KRITISCH!";
                WarningStatusText.Foreground = (Brush)FindResource("OnError");
                
                StartBlinking();
            }
            else if (_team.IsFirstWarning)
            {
                // Warning colors - use design system Warning colors
                TeamBorder.Background = (Brush)FindResource("Warning");
                TeamBorder.BorderBrush = (Brush)FindResource("OnWarning");
                TeamBorder.BorderThickness = new Thickness(2);
                
                // Warning indicators
                WarningIndicator.Background = (Brush)FindResource("WarningContainer");
                WarningStatusText.Text = "WARNUNG!";
                WarningStatusText.Foreground = (Brush)FindResource("OnWarning");
                
                StopBlinking();
            }
            else
            {
                // Normal background - use theme colors
                if (_isDarkMode)
                {
                    TeamBorder.Background = (Brush)FindResource("DarkSurfaceContainer");
                    TeamBorder.BorderBrush = (Brush)FindResource("DarkOutline");
                }
                else
                {
                    TeamBorder.Background = (Brush)FindResource("Surface");
                    TeamBorder.BorderBrush = (Brush)FindResource("Outline");
                }
                
                // Normal state - no warning indicators
                WarningIndicator.Background = Brushes.Transparent;
                WarningStatusText.Text = "";
                
                TeamBorder.BorderThickness = new Thickness(1);
                StopBlinking();
            }
        }

        private void StartBlinking()
        {
            try
            {
                // Stoppe zuerst alle laufenden Animationen
                StopBlinking();
                
                if (_blinkingStoryboard == null)
                {
                    _blinkingStoryboard = FindResource("BlinkingAnimation") as Storyboard;
                }

                if (_blinkingStoryboard != null)
                {
                    _blinkingStoryboard.Begin(TeamBorder, true);
                    LoggingService.Instance.LogInfo($"Blinking animation started for team {_team?.TeamName}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error starting blinking animation for team {_team?.TeamName}", ex);
            }
        }

        private void StopBlinking()
        {
            try
            {
                if (_blinkingStoryboard != null)
                {
                    // Stop the storyboard completely and remove it
                    _blinkingStoryboard.Stop(TeamBorder);
                    _blinkingStoryboard.Remove(TeamBorder);
                    
                    LoggingService.Instance.LogInfo($"Blinking animation stopped for team {_team?.TeamName}");
                }
                
                // Explizit Opacity auf 1.0 setzen, um sicherzustellen, dass das Element vollständig sichtbar ist
                TeamBorder.BeginAnimation(UIElement.OpacityProperty, null);
                TeamBorder.Opacity = 1.0;
                
                // Sicherstellen, dass alle Transform-Eigenschaften zurückgesetzt werden
                TeamBorder.RenderTransform = null;
            }
            catch (Exception ex)
            {
                // Fallback: Mindestens Opacity zurücksetzen
                try
                {
                    TeamBorder.BeginAnimation(UIElement.OpacityProperty, null);
                    TeamBorder.Opacity = 1.0;
                }
                catch { }
                
                LoggingService.Instance.LogError($"Error stopping blinking animation for team {_team?.TeamName}", ex);
            }
        }

        private void UpdateUI()
        {
            OnPropertyChanged(nameof(TeamName));
            OnPropertyChanged(nameof(HundName));
            OnPropertyChanged(nameof(Hundefuehrer));
            OnPropertyChanged(nameof(Helfer));
            OnPropertyChanged(nameof(Suchgebiet));
            OnPropertyChanged(nameof(ElapsedTimeString));
            UpdateTeamTypeBadge();
            UpdateWarningSettingsDisplay();
            UpdateWarningState();
            
            // Trigger entrance animation
            TriggerEntranceAnimation();
        }

        // NEU: Update Warning Settings Display
        private void UpdateWarningSettingsDisplay()
        {
            if (_team != null)
            {
                WarningSettingsText.Text = $"{_team.FirstWarningMinutes}/{_team.SecondWarningMinutes}";
            }
        }

        // NEU: Warning Settings Button Click Handler
        private void BtnWarningSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_team != null)
                {
                    // Create a list with just this team for the settings window
                    var teams = new List<Team> { _team };
                    
                    // Get global warning settings from MainWindow
                    int globalFirst = 10;
                    int globalSecond = 20;
                    
                    if (Window.GetWindow(this) is MainWindow mainWindow)
                    {
                        globalFirst = mainWindow.GlobalFirstWarningMinutes;
                        globalSecond = mainWindow.GlobalSecondWarningMinutes;
                    }
                    
                    var warningSettingsWindow = new TeamWarningSettingsWindow(teams, globalFirst, globalSecond)
                    {
                        Owner = Window.GetWindow(this),
                        Title = $"Warnschwellen - {_team.TeamName}"
                    };
                    
                    if (warningSettingsWindow.ShowDialog() == true)
                    {
                        UpdateWarningSettingsDisplay();
                        NotifyDataChanged();
                        
                        LoggingService.Instance.LogInfo($"Warning settings updated for {_team.TeamName}: " +
                            $"{_team.FirstWarningMinutes}/{_team.SecondWarningMinutes} minutes");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error opening warning settings for team {_team?.TeamName}", ex);
                MessageBox.Show($"Fehler beim Öffnen der Warnschwellen-Einstellungen: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TriggerEntranceAnimation()
        {
            try
            {
                var storyboard = FindResource("CardEntranceAnimation") as Storyboard;
                if (storyboard != null)
                {
                    storyboard.Begin(TeamBorder);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error triggering entrance animation", ex);
            }
        }

        // v1.5: Updated for Multiple Team Types
        private void UpdateTeamTypeBadge()
        {
            if (_team?.MultipleTeamTypes != null)
            {
                try
                {
                    var colorHex = _team.TeamTypeColorHex;
                    var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex);
                    TeamTypeBadge.Background = new SolidColorBrush(color);
                    TeamTypeText.Text = _team.TeamTypeShortName;
                    TeamTypeBadge.ToolTip = $"{_team.TeamTypeDisplayName}: {_team.TeamTypeDescription}";
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("Error updating team type badge", ex);
                    TeamTypeBadge.Background = (Brush)FindResource("OnSurfaceVariant");
                    TeamTypeText.Text = "AL";
                }
            }
        }

        // v1.5: New method to handle team type badge click (edit multiple types)
        private void TeamTypeBadgeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_team != null)
                {
                    var teamTypeWindow = new TeamTypeSelectionWindow(_team.MultipleTeamTypes);
                    if (teamTypeWindow.ShowDialog() == true)
                    {
                        _team.MultipleTeamTypes = teamTypeWindow.SelectedMultipleTeamTypes;
                        UpdateTeamTypeBadge();
                        NotifyDataChanged();
                        
                        LoggingService.Instance.LogInfo($"Team types updated for {_team.TeamName}: {_team.TeamTypeDisplayName}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error editing team types for team {_team?.TeamName}", ex);
                MessageBox.Show($"Fehler beim Bearbeiten der Team-Typen: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _team?.StartTimer();
                LoggingService.Instance.LogInfo($"Timer started for team {_team?.TeamName}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error starting timer for team {_team?.TeamName}", ex);
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _team?.StopTimer();
                LoggingService.Instance.LogInfo($"Timer stopped for team {_team?.TeamName} at {_team?.ElapsedTimeString}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error stopping timer for team {_team?.TeamName}", ex);
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show($"Timer für {_team?.TeamName} zurücksetzen?", 
                    "Timer Reset", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _team?.ResetTimer();
                    
                    // Explizit das Warning State aktualisieren nach dem Reset
                    UpdateWarningState();
                    
                    LoggingService.Instance.LogInfo($"Timer reset for team {_team?.TeamName}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error resetting timer for team {_team?.TeamName}", ex);
            }
        }

        private void BtnDeleteTeam_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_team != null)
                {
                    var result = MessageBox.Show($"Team '{_team.TeamName}' wirklich löschen?", 
                        "Team löschen", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        _team.StopTimer();
                        TeamDeleteRequested?.Invoke(this, _team);
                        LoggingService.Instance.LogInfo($"Team deletion requested: {_team?.TeamName}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error deleting team {_team?.TeamName}", ex);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event EventHandler<Team>? TeamDeleteRequested;
    }
}
