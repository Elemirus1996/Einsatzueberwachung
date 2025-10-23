using System;
using System.Collections.Generic;
using System.Windows;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// Interaction logic for StatisticsWindow.xaml - MVVM Implementation v2.0
    /// Updated for Unified Theme System and BaseThemeWindow integration
    /// </summary>
    public partial class StatisticsWindow : BaseThemeWindow
    {
        private StatisticsViewModel? _viewModel;

        public StatisticsWindow(List<Team> teams, EinsatzData einsatzData)
        {
            InitializeComponent();
            InitializeViewModel(teams, einsatzData);
            
            LoggingService.Instance.LogInfo("StatisticsWindow initialized with Unified Theme System v2.0");
        }

        private void InitializeViewModel(List<Team> teams, EinsatzData einsatzData)
        {
            try
            {
                _viewModel = new StatisticsViewModel(teams, einsatzData);
                DataContext = _viewModel;
                
                LoggingService.Instance.LogInfo("StatisticsViewModel initialized and connected with Unified Theme System");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing StatisticsViewModel", ex);
                
                // Fallback: Show error message and close window
                MessageBox.Show($"Fehler beim Initialisieren des Statistics ViewModels:\n{ex.Message}", 
                               "Initialisierung fehlgeschlagen", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
            }
        }

        #region BaseThemeWindow Override

        /// <summary>
        /// Applies theme to StatisticsWindow - now handled automatically by BaseThemeWindow
        /// Optional customizations can be added here
        /// </summary>
        protected override void ApplyThemeToWindow(bool isDarkMode)
        {
            try
            {
                // Call base implementation first (handles standard theme application)
                base.ApplyThemeToWindow(isDarkMode);
                
                // Optional: StatisticsWindow-specific theme customizations
                LoggingService.Instance.LogInfo($"StatisticsWindow theme applied: {(isDarkMode ? "Dark" : "Light")} mode via Unified Theme System");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to StatisticsWindow", ex);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Aktualisiert die Statistiken mit neuen Team-Daten (für externe Updates)
        /// </summary>
        /// <param name="teams">Aktuelle Team-Liste</param>
        public void UpdateTeamData(List<Team> teams)
        {
            try
            {
                if (_viewModel != null)
                {
                    // Update the internal teams reference in ViewModel
                    // Note: This would require exposing a method in ViewModel
                    _viewModel.UpdateStatistics();
                    _viewModel.LoadData();
                    
                    LoggingService.Instance.LogInfo("Team data updated in StatisticsWindow via MVVM");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating team data in StatisticsWindow", ex);
            }
        }

        /// <summary>
        /// Aktualisiert die Statistiken (öffentliche Methode für externe Aufrufe)
        /// </summary>
        public void RefreshStatistics()
        {
            try
            {
                // Use the RefreshStatsCommand instead of directly calling ExecuteRefreshStats
                if (_viewModel?.RefreshStatsCommand.CanExecute(null) == true)
                {
                    _viewModel.RefreshStatsCommand.Execute(null);
                }
                LoggingService.Instance.LogInfo("Statistics refreshed externally via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error refreshing statistics externally", ex);
            }
        }

        #endregion

        #region Window Lifecycle

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Cleanup ViewModel
                if (_viewModel != null)
                {
                    _viewModel.Dispose();
                    _viewModel = null;
                }
                
                LoggingService.Instance.LogInfo("StatisticsWindow closed and cleaned up via Unified Theme System");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during StatisticsWindow cleanup", ex);
            }
            finally
            {
                // BaseThemeWindow handles theme cleanup automatically
                base.OnClosed(e);
            }
        }

        #endregion
    }
}
