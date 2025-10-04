using System;
using System.Windows;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung
{
    public partial class TeamDetailWindow : Window
    {
        private Team? _team;
        private TeamControl? _teamControl;

        public TeamDetailWindow()
        {
            InitializeComponent();
        }

        public TeamDetailWindow(Team team) : this()
        {
            _team = team;
            InitializeTeamControl();
            ApplyTheme(ThemeService.Instance.IsDarkMode);
            
            // Theme-Änderungen abonnieren
            ThemeService.Instance.ThemeChanged += OnThemeChanged;
            
            // Fenster-Titel aktualisieren
            this.Title = $"Team Details - {team.TeamName}";
            TeamNameText.Text = team.TeamName;
            TeamTypeText.Text = team.TeamTypeDisplayName;
        }

        private void InitializeTeamControl()
        {
            if (_team == null) return;

            try
            {
                _teamControl = new TeamControl { Team = _team };
                _teamControl.Margin = new Thickness(0);
                
                // TeamControl zum Container hinzufügen
                TeamControlContainer.Child = _teamControl;
                
                LoggingService.Instance.LogInfo($"TeamDetailWindow initialized for team {_team.TeamName}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing TeamControl in DetailWindow", ex);
                MessageBox.Show($"Fehler beim Laden der Team-Details: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnThemeChanged(bool isDarkMode)
        {
            Dispatcher.Invoke(() => ApplyTheme(isDarkMode));
        }

        private void ApplyTheme(bool isDarkMode)
        {
            try
            {
                // Theme auf TeamControl anwenden
                _teamControl?.ApplyTheme(isDarkMode);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to TeamDetailWindow", ex);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Theme-Event abmelden
                ThemeService.Instance.ThemeChanged -= OnThemeChanged;
                
                LoggingService.Instance.LogInfo($"TeamDetailWindow closed for team {_team?.TeamName ?? "Unknown"}");
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
