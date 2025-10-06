using System;
using System.Windows;
using System.Windows.Input;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;
using Einsatzueberwachung.ViewModels;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// MVVM-basiertes Team-Typ-Auswahl-Fenster
    /// </summary>
    public partial class TeamTypeSelectionWindow : Window
    {
        private readonly TeamTypeSelectionViewModel _viewModel = null!;

        public MultipleTeamTypes SelectedMultipleTeamTypes => _viewModel.SelectedMultipleTeamTypes;

        public TeamTypeSelectionWindow(MultipleTeamTypes? currentSelection = null)
        {
            InitializeComponent();
            
            try
            {
                _viewModel = new TeamTypeSelectionViewModel(currentSelection);
                DataContext = _viewModel;
                
                // Subscribe to ViewModel events
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                _viewModel.RequestClose += ViewModel_RequestClose;
                
                LoggingService.Instance.LogInfo($"TeamTypeSelectionWindow (MVVM) initialized for {(currentSelection != null ? "editing" : "creating")} selection");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing TeamTypeSelectionWindow", ex);
                MessageBox.Show($"Fehler beim Initialisieren des Fensters: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                // Handle DialogResult changes
                if (e.PropertyName == nameof(TeamTypeSelectionViewModel.DialogResult) && _viewModel.DialogResult.HasValue)
                {
                    DialogResult = _viewModel.DialogResult.Value;
                    
                    if (_viewModel.DialogResult.Value)
                    {
                        LoggingService.Instance.LogInfo($"Team types successfully selected via MVVM: {_viewModel.SelectedMultipleTeamTypes.DisplayName}");
                    }
                    else
                    {
                        LoggingService.Instance.LogInfo("Team type selection cancelled via MVVM");
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
                if (_viewModel.DialogResult.HasValue)
                {
                    DialogResult = _viewModel.DialogResult.Value;
                }
                
                // Close the window
                Close();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error closing TeamTypeSelectionWindow via RequestClose", ex);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            try
            {
                // Enter to confirm (if enabled)
                if (e.Key == Key.Enter && _viewModel.OkCommand.CanExecute(null))
                {
                    _viewModel.OkCommand.Execute(null);
                    e.Handled = true;
                    return;
                }
                
                // Escape to cancel
                if (e.Key == Key.Escape)
                {
                    _viewModel.CancelCommand.Execute(null);
                    e.Handled = true;
                    return;
                }
                
                // Ctrl+A to clear all
                if (e.Key == Key.A && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    _viewModel.ClearAllCommand.Execute(null);
                    e.Handled = true;
                    return;
                }
                
                base.OnKeyDown(e);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling key down in TeamTypeSelectionWindow", ex);
                base.OnKeyDown(e);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Unsubscribe from ViewModel events
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                    _viewModel.RequestClose -= ViewModel_RequestClose;
                }
                
                LoggingService.Instance.LogInfo("TeamTypeSelectionWindow (MVVM) closed");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error closing TeamTypeSelectionWindow", ex);
            }
            finally
            {
                base.OnClosed(e);
            }
        }
    }
}
