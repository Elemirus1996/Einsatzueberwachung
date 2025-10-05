using System.Windows;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    public partial class TeamInputWindow : Window
    {
        private readonly TeamInputViewModel _viewModel;

        // Public Properties für Backward-Compatibility
        public string HundName => _viewModel.HundName;
        public string Hundefuehrer => _viewModel.Hundefuehrer;
        public string Helfer => _viewModel.Helfer;
        public string Suchgebiet => _viewModel.Suchgebiet;
        public string TeamName => _viewModel.TeamName;
        public DogEntry? SelectedDog => _viewModel.SelectedDog;
        public MultipleTeamTypes? PreselectedTeamTypes => _viewModel.PreselectedTeamTypes;

        public TeamInputWindow()
        {
            InitializeComponent();
            
            _viewModel = new TeamInputViewModel();
            DataContext = _viewModel;
            
            // Subscribe to ViewModel events
            _viewModel.RequestClose += OnRequestClose;
            _viewModel.ShowTeamTypeSelection += OnShowTeamTypeSelection;
            
            LoggingService.Instance?.LogInfo("TeamInputWindow v1.9 initialized with MVVM pattern and Orange design");
        }

        private void OnRequestClose(object? sender, EventArgs e)
        {
            DialogResult = false;
        }

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
                    
                    LoggingService.Instance?.LogInfo($"Team input completed via MVVM - Team name: {TeamName}, " +
                        $"Selected types: {PreselectedTeamTypes?.DisplayName ?? "None"}");
                    
                    DialogResult = true;
                }
                // Wenn TeamTypeSelectionWindow abgebrochen wird, bleibt TeamInputWindow offen
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error showing team type selection", ex);
                MessageBox.Show($"Fehler beim Öffnen der Team-Typ-Auswahl: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events
            if (_viewModel != null)
            {
                _viewModel.RequestClose -= OnRequestClose;
                _viewModel.ShowTeamTypeSelection -= OnShowTeamTypeSelection;
            }
            
            base.OnClosed(e);
        }
    }
}
