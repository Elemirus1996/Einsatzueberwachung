using System;
using System.Collections.Generic;
using System.Windows;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;
using Einsatzueberwachung.ViewModels;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// TeamWarningSettingsWindow - MVVM-Implementation v1.9.0
    /// Minimales Code-Behind mit ViewModel-Integration
    /// </summary>
    public partial class TeamWarningSettingsWindow : Window
    {
        private TeamWarningSettingsViewModel? _viewModel;

        public bool SettingsChanged => _viewModel?.SettingsChanged ?? false;

        public TeamWarningSettingsWindow(List<Team> teams, int globalFirstWarning, int globalSecondWarning)
        {
            InitializeComponent();
            
            try
            {
                // Initialize ViewModel
                _viewModel = new TeamWarningSettingsViewModel(teams, globalFirstWarning, globalSecondWarning);
                DataContext = _viewModel;

                // Subscribe to ViewModel events for dialog management
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                _viewModel.RequestClose += ViewModel_RequestClose;

                ApplyCurrentTheme();
                
                LoggingService.Instance.LogInfo($"TeamWarningSettingsWindow initialized with MVVM pattern v1.9.0 for {teams?.Count ?? 0} teams");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing TeamWarningSettingsWindow with MVVM", ex);
                MessageBox.Show($"Fehler beim Laden des Fensters: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == nameof(TeamWarningSettingsViewModel.DialogResult) && _viewModel != null)
                {
                    // Handle dialog result from ViewModel
                    if (_viewModel.DialogResult.HasValue)
                    {
                        DialogResult = _viewModel.DialogResult.Value;
                        
                        if (_viewModel.DialogResult.Value)
                        {
                            LoggingService.Instance.LogInfo("Team warning settings saved successfully via MVVM");
                        }
                        else
                        {
                            LoggingService.Instance.LogInfo("Team warning settings cancelled via MVVM");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling ViewModel property change", ex);
            }
        }

        private void ViewModel_RequestClose()
        {
            try
            {
                // Ensure DialogResult is set if not already
                if (_viewModel?.DialogResult.HasValue == true)
                {
                    DialogResult = _viewModel.DialogResult.Value;
                }
                
                // Close the window
                Close();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error closing TeamWarningSettingsWindow via RequestClose", ex);
            }
        }

        private void ApplyCurrentTheme()
        {
            try
            {
                // Apply current theme based on ThemeService
                var isDarkMode = Services.ThemeService.Instance.IsDarkMode;
                
                if (isDarkMode)
                {
                    Background = (System.Windows.Media.Brush)FindResource("Surface");
                }
                else
                {
                    Background = (System.Windows.Media.Brush)FindResource("SurfaceContainer");
                }
                
                LoggingService.Instance.LogInfo($"Applied {(isDarkMode ? "dark" : "light")} theme to TeamWarningSettingsWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to TeamWarningSettingsWindow", ex);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Clean up ViewModel subscriptions
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                    _viewModel.RequestClose -= ViewModel_RequestClose;
                }
                
                LoggingService.Instance.LogInfo("TeamWarningSettingsWindow closed - MVVM cleanup completed");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during TeamWarningSettingsWindow cleanup", ex);
            }
            
            base.OnClosed(e);
        }
    }
}
