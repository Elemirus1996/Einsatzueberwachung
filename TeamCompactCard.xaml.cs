using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung
{
    public partial class TeamCompactCard : UserControl
    {
        private Team? _team;
        private bool _isDarkMode = false;

        public static readonly DependencyProperty TeamProperty =
            DependencyProperty.Register("Team", typeof(Team), typeof(TeamCompactCard),
                new PropertyMetadata(null, OnTeamChanged));

        public Team? Team
        {
            get => (Team?)GetValue(TeamProperty);
            set => SetValue(TeamProperty, value);
        }

        public string TeamName => Team?.TeamName ?? "";
        public string Hundefuehrer => Team?.Hundefuehrer ?? "";
        public string Helfer => Team?.Helfer ?? "";
        public string Suchgebiet => Team?.Suchgebiet ?? "";
        public string ElapsedTimeString => Team?.ElapsedTimeString ?? "00:00:00";

        public event EventHandler<Team>? TeamClicked;

        public TeamCompactCard()
        {
            InitializeComponent();
            DataContext = this;
        }

        private static void OnTeamChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TeamCompactCard control)
            {
                if (control._team != null)
                {
                    control._team.PropertyChanged -= control.Team_PropertyChanged;
                }

                control._team = e.NewValue as Team;

                if (control._team != null)
                {
                    control._team.PropertyChanged += control.Team_PropertyChanged;
                    control.UpdateUI();
                }
            }
        }

        private void Team_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Team.ElapsedTimeString):
                case nameof(Team.ElapsedTime):
                    Dispatcher.Invoke(() => UpdateTimerDisplay());
                    break;
                case nameof(Team.IsFirstWarning):
                case nameof(Team.IsSecondWarning):
                    Dispatcher.Invoke(() => UpdateWarningState());
                    break;
                case nameof(Team.IsRunning):
                    Dispatcher.Invoke(() => UpdateStatusDisplay());
                    break;
                case nameof(Team.Hundefuehrer):
                case nameof(Team.Helfer):
                case nameof(Team.Suchgebiet):
                    Dispatcher.Invoke(() => UpdateTeamInfo());
                    break;
            }
        }

        private void UpdateUI()
        {
            UpdateTeamTypeBadge();
            UpdateWarningState();
            UpdateStatusDisplay();
            UpdateTimerDisplay();
            UpdateTeamInfo();
        }

        private void UpdateTeamInfo()
        {
            // Text-Bindings werden automatisch aktualisiert
            // Diese Methode kann für zusätzliche UI-Updates verwendet werden
        }

        private void UpdateTeamTypeBadge()
        {
            if (_team?.MultipleTeamTypes != null)
            {
                try
                {
                    var colorHex = _team.TeamTypeColorHex;
                    var color = (Color)ColorConverter.ConvertFromString(colorHex);
                    TeamTypeBadge.Background = new SolidColorBrush(color);
                    TeamTypeText.Text = _team.TeamTypeShortName;
                }
                catch
                {
                    TeamTypeBadge.Background = (Brush)FindResource("OnSurfaceVariant");
                    TeamTypeText.Text = "AL";
                }
            }
        }

        private void UpdateWarningState()
        {
            if (_team == null) return;

            if (_team.IsSecondWarning)
            {
                // Critical warning - use design system Error colors
                CompactBorder.Background = (Brush)FindResource("Error");
                CompactBorder.BorderBrush = (Brush)FindResource("OnError");
                CompactBorder.BorderThickness = new Thickness(3);
                WarningIndicator.Background = (Brush)FindResource("OnError");
            }
            else if (_team.IsFirstWarning)
            {
                // First warning - use design system Warning colors
                CompactBorder.Background = (Brush)FindResource("Warning");
                CompactBorder.BorderBrush = (Brush)FindResource("WarningContainer");
                CompactBorder.BorderThickness = new Thickness(2);
                WarningIndicator.Background = (Brush)FindResource("OnWarning");
            }
            else
            {
                // Normal state - use theme colors
                if (_isDarkMode)
                {
                    CompactBorder.Background = (Brush)FindResource("DarkSurfaceContainer");
                    CompactBorder.BorderBrush = (Brush)FindResource("DarkOutline");
                }
                else
                {
                    CompactBorder.Background = (Brush)FindResource("Surface");
                    CompactBorder.BorderBrush = (Brush)FindResource("Outline");
                }
                CompactBorder.BorderThickness = new Thickness(1);
                WarningIndicator.Background = Brushes.Transparent;
            }
        }

        private void UpdateStatusDisplay()
        {
            if (_team == null) return;

            if (_team.IsRunning)
            {
                StatusIndicator.Background = (Brush)FindResource("Success");
                StatusText.Text = "AKTIV";
                StatusText.Foreground = (Brush)FindResource("OnSuccess");
            }
            else
            {
                StatusIndicator.Background = (Brush)FindResource("OnSurfaceVariant");
                StatusText.Text = "BEREIT";
                StatusText.Foreground = (Brush)FindResource("OnSurface");
            }
        }

        private void UpdateTimerDisplay()
        {
            // Timer updates are handled by binding
        }

        private void CompactBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            // Prüfe ob Warnzustand aktiv ist
            if (_team?.IsFirstWarning == true || _team?.IsSecondWarning == true)
            {
                // Keine Hover-Effekte bei Warnungen
                return;
            }
            
            // Theme-aware Hover-Effekt using design system
            if (_isDarkMode)
            {
                CompactBorder.Background = (Brush)FindResource("DarkSurfaceContainerHigh");
            }
            else
            {
                CompactBorder.Background = (Brush)FindResource("SurfaceVariant");
            }
        }

        private void CompactBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            // Stelle den originalen Zustand wieder her
            if (_team?.IsFirstWarning == true || _team?.IsSecondWarning == true)
            {
                UpdateWarningState(); // Restore warning state
            }
            else
            {
                // Restore normal background based on theme using design system
                if (_isDarkMode)
                {
                    CompactBorder.Background = (Brush)FindResource("DarkSurfaceContainer");
                }
                else
                {
                    CompactBorder.Background = (Brush)FindResource("Surface");
                }
            }
        }

        private void CompactBorder_Click(object sender, MouseButtonEventArgs e)
        {
            if (_team != null)
            {
                TeamClicked?.Invoke(this, _team);
            }
        }

        public void ApplyTheme(bool isDarkMode)
        {
            _isDarkMode = isDarkMode;
            
            // Theme application for compact cards using design system
            if ((_team?.IsFirstWarning ?? false) == false && (_team?.IsSecondWarning ?? false) == false)
            {
                if (isDarkMode)
                {
                    CompactBorder.Background = (Brush)FindResource("DarkSurfaceContainer");
                    CompactBorder.BorderBrush = (Brush)FindResource("DarkOutline");
                }
                else
                {
                    CompactBorder.Background = (Brush)FindResource("Surface");
                    CompactBorder.BorderBrush = (Brush)FindResource("Outline");
                }
            }
        }
    }
}
