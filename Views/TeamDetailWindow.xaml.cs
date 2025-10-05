using System;
using System.Windows;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;
using Einsatzueberwachung.ViewModels;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// Interaction logic for TeamDetailWindow.xaml
    /// Vollständig auf MVVM-Pattern umgestellt - minimales Code-Behind
    /// v1.9.0 mit Orange-Design-System
    /// </summary>
    public partial class TeamDetailWindow : Window
    {
        private readonly TeamDetailViewModel _viewModel;
        private TeamControl? _teamControl;

        public TeamDetailWindow()
        {
            InitializeComponent();
            
            _viewModel = new TeamDetailViewModel();
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
                _teamControl.Margin = new Thickness(0);
                
                // Theme auf TeamControl anwenden
                _teamControl.ApplyTheme(ThemeService.Instance.IsDarkMode);
                
                // TeamControl zum Container hinzufügen
                TeamControlContainer.Child = _teamControl;
                
                // Theme-Änderungen für TeamControl abonnieren
                ThemeService.Instance.ThemeChanged += OnThemeChanged;
                
                LoggingService.Instance.LogInfo($"TeamControl embedded in DetailWindow for team {team.TeamName}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing TeamControl in DetailWindow", ex);
                throw; // Re-throw für Fehlerbehandlung im Constructor
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

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Theme-Event abmelden
                ThemeService.Instance.ThemeChanged -= OnThemeChanged;
                
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
