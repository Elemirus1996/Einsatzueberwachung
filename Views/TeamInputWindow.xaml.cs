using System;
using System.Windows;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// TeamInputWindow - Enhanced with Theme Integration v1.9.0
    /// Now inherits from BaseThemeWindow for automatic theme support
    /// Fully integrated with design system and theme service
    /// </summary>
    public partial class TeamInputWindow : BaseThemeWindow
    {
        private readonly TeamInputViewModel _viewModel;

        // Public Properties für Backward-Compatibility
        public string HundName => _viewModel.HundName;
        public string Hundefuehrer => _viewModel.Hundefuehrer;
        public string Helfer => _viewModel.Helfer;
        public string Suchgebiet => _viewModel.Suchgebiet;
        public string TeamName => _viewModel.TeamName;
        public DogEntry? SelectedDog => _viewModel.SelectedDog?.DogEntry; // Updated to access the actual DogEntry
        public MultipleTeamTypes? PreselectedTeamTypes => _viewModel.PreselectedTeamTypes;

        public TeamInputWindow()
        {
            InitializeComponent();
            InitializeThemeSupport(); // Initialize theme after component initialization
            
            _viewModel = new TeamInputViewModel();
            DataContext = _viewModel;
            
            SubscribeToEvents();
            
            LoggingService.Instance.LogInfo("TeamInputWindow initialized without search areas");
        }

        private void SubscribeToEvents()
        {
            // Subscribe to ViewModel events
            _viewModel.RequestClose += (sender, e) => 
            {
                // Check if this is a successful completion or cancellation
                if (_viewModel.PreselectedTeamTypes != null && !string.IsNullOrWhiteSpace(_viewModel.HundName))
                {
                    DialogResult = true;  // Successful completion
                }
                else
                {
                    DialogResult = false; // Cancellation
                }
            };
            _viewModel.ShowTeamTypeSelection += OnShowTeamTypeSelection;
        }

        protected override void ApplyThemeToWindow(bool isDarkMode)
        {
            try
            {
                // Apply theme-specific styling
                base.ApplyThemeToWindow(isDarkMode);
                
                LoggingService.Instance?.LogInfo($"Theme applied to TeamInputWindow: {(isDarkMode ? "Dark" : "Light")} mode with Orange design");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error applying theme to TeamInputWindow", ex);
            }
        }

        #region Event Handlers

        private void OnShowTeamTypeSelection(object? sender, EventArgs e)
        {
            try
            {
                // Öffne TeamTypeSelectionWindow
                var teamTypeWindow = new Views.TeamTypeSelectionWindow(_viewModel.PreselectedTeamTypes);
                teamTypeWindow.Owner = this;
                teamTypeWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                
                if (teamTypeWindow.ShowDialog() == true)
                {
                    // Ausgewählte Team-Typen an ViewModel weitergeben
                    _viewModel.SetSelectedTeamTypes(teamTypeWindow.SelectedMultipleTeamTypes);
                    
                    LoggingService.Instance?.LogInfo($"Team types selected via MVVM - Team name: {TeamName}, " +
                        $"Selected types: {PreselectedTeamTypes?.DisplayName ?? "None"}");
                }
                // Wenn TeamTypeSelectionWindow abgebrochen wird, bleibt TeamInputWindow offen für weitere Bearbeitung
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error showing team type selection", ex);
                MessageBox.Show($"Fehler beim Öffnen der Team-Typ-Auswahl: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // NEW: Handle CreateTeam command completion
        private void OnCreateTeamCompleted(object? sender, EventArgs e)
        {
            try
            {
                // Validate that we have all required data
                if (string.IsNullOrWhiteSpace(_viewModel.HundName))
                {
                    MessageBox.Show("Bitte geben Sie einen Hundenamen ein.", 
                        "Pflichtfeld fehlt", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_viewModel.PreselectedTeamTypes == null || !_viewModel.PreselectedTeamTypes.SelectedTypes.Any())
                {
                    MessageBox.Show("Bitte wählen Sie mindestens einen Team-Typ aus.", 
                        "Team-Typ fehlt", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                LoggingService.Instance?.LogInfo($"Team input completed successfully via MVVM - {_viewModel.TeamName}");
                DialogResult = true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error handling create team completion", ex);
                MessageBox.Show($"Fehler beim Erstellen des Teams: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Unsubscribe from events
                if (_viewModel != null)
                {
                    _viewModel.ShowTeamTypeSelection -= OnShowTeamTypeSelection;
                }
                
                LoggingService.Instance?.LogInfo("TeamInputWindow closed and cleaned up");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error during TeamInputWindow cleanup", ex);
            }
            finally
            {
                base.OnClosed(e);
            }
        }
    }
}
