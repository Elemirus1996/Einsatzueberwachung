using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
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
        public string Notizen
        {
            get => Team?.Notizen ?? "";
            set { if (Team != null) { Team.Notizen = value; OnPropertyChanged(nameof(Notizen)); } }
        }
        public string ElapsedTimeString => Team?.ElapsedTimeString ?? "00:00:00";
        public string QuickNoteText { get; set; } = string.Empty;

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
                // Dark theme colors
                Resources["TeamBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30)); // #1E1E1E
                Resources["TeamBorderBrush"] = new SolidColorBrush(Color.FromRgb(51, 51, 51)); // #333333
                Resources["TeamTextBrush"] = new SolidColorBrush(Color.FromRgb(224, 224, 224)); // #E0E0E0
            }
            else
            {
                // Light theme colors
                Resources["TeamBackgroundBrush"] = new SolidColorBrush(Colors.White);
                Resources["TeamBorderBrush"] = new SolidColorBrush(Color.FromRgb(224, 224, 224)); // #E0E0E0
                Resources["TeamTextBrush"] = new SolidColorBrush(Color.FromRgb(33, 33, 33)); // #212121
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
                case nameof(Team.Notizen):
                    OnPropertyChanged(nameof(Notizen));
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
                // Red blinking background - same for both themes (urgent)
                TeamBorder.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Bright Red
                TeamBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(211, 47, 47)); // Darker Red Border
                TeamBorder.BorderThickness = new Thickness(3);
                
                // Update warning indicators
                WarningIndicator.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red dot
                WarningStatusText.Text = "KRITISCH!";
                WarningStatusText.Foreground = new SolidColorBrush(Colors.White);
                
                StartBlinking();
            }
            else if (_team.IsFirstWarning)
            {
                // Different warning colors for light vs dark theme
                if (_isDarkMode)
                {
                    // Dark mode: Orange/Amber - very visible on dark background
                    TeamBorder.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Bright Orange
                    TeamBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Golden border
                    
                    // Dark mode warning indicators
                    WarningIndicator.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Golden dot
                    WarningStatusText.Text = "WARNUNG!";
                    WarningStatusText.Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)); // Dark text
                }
                else
                {
                    // Light mode: Yellow/Amber - good contrast on white
                    TeamBorder.Background = new SolidColorBrush(Color.FromRgb(255, 235, 59)); // Yellow
                    TeamBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Amber border
                    
                    // Light mode warning indicators
                    WarningIndicator.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange dot
                    WarningStatusText.Text = "WARNUNG!";
                    WarningStatusText.Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)); // Dark text
                }
                
                TeamBorder.BorderThickness = new Thickness(2);
                StopBlinking();
            }
            else
            {
                // Normal background - use theme colors
                if (_isDarkMode)
                {
                    TeamBorder.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)); // #1E1E1E
                    TeamBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51)); // #333333
                }
                else
                {
                    TeamBorder.Background = new SolidColorBrush(Colors.White);
                    TeamBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)); // #E0E0E0
                }
                
                // Normal state - no warning indicators
                WarningIndicator.Background = new SolidColorBrush(Colors.Transparent);
                WarningStatusText.Text = "";
                
                TeamBorder.BorderThickness = new Thickness(1);
                StopBlinking();
            }
        }

        private void StartBlinking()
        {
            if (_blinkingStoryboard == null)
            {
                _blinkingStoryboard = FindResource("BlinkingAnimation") as Storyboard;
            }

            _blinkingStoryboard?.Begin(TeamBorder);
        }

        private void StopBlinking()
        {
            try
            {
                _blinkingStoryboard?.Stop(TeamBorder);
                // Explizit Opacity auf 1.0 setzen für den Fall, dass die Animation unterbrochen wurde
                TeamBorder.Opacity = 1.0;
                
                // Sicherstellen, dass alle Transform-Eigenschaften zurückgesetzt werden
                TeamBorder.RenderTransform = null;
            }
            catch (Exception ex)
            {
                // Fallback: Mindestens Opacity zurücksetzen
                TeamBorder.Opacity = 1.0;
                LoggingService.Instance.LogError("Error stopping blinking animation", ex);
            }
        }

        private void UpdateUI()
        {
            OnPropertyChanged(nameof(TeamName));
            OnPropertyChanged(nameof(HundName));
            OnPropertyChanged(nameof(Hundefuehrer));
            OnPropertyChanged(nameof(Helfer));
            OnPropertyChanged(nameof(Notizen));
            OnPropertyChanged(nameof(ElapsedTimeString));
            UpdateTeamTypeBadge();
            UpdateWarningState();
            
            // Trigger entrance animation
            TriggerEntranceAnimation();
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
                    TeamTypeBadge.Background = new SolidColorBrush(System.Windows.Media.Colors.Gray);
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

        private void BtnAddNote_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddQuickNote();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error adding note for team {_team?.TeamName}", ex);
            }
        }

        private void TxtQuickNote_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(QuickNoteText))
                {
                    AddQuickNote();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error handling quick note keydown for team {_team?.TeamName}", ex);
            }
        }

        private void AddQuickNote()
        {
            if (_team != null && !string.IsNullOrWhiteSpace(QuickNoteText))
            {
                _team.AddTimestampedNote(QuickNoteText.Trim());
                QuickNoteText = string.Empty;
                OnPropertyChanged(nameof(QuickNoteText));
                NotifyDataChanged();
                
                // Auto-scroll to bottom of notes
                Dispatcher.BeginInvoke(() =>
                {
                    if (NotesItemsControl.Items.Count > 0)
                    {
                        var scrollViewer = FindVisualChild<ScrollViewer>(NotesItemsControl);
                        scrollViewer?.ScrollToEnd();
                    }
                }, DispatcherPriority.Background);
            }
        }

        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
                
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event EventHandler<Team>? TeamDeleteRequested;
    }
}
