using System;
using System.Windows;
using System.Windows.Input;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;
using Einsatzueberwachung.ViewModels;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// MVVM-basiertes Hunde-Bearbeitungsfenster
    /// </summary>
    public partial class DogEditWindow : Window
    {
        private readonly DogEditViewModel _viewModel = null!;

        public DogEntry DogEntry => _viewModel.DogEntry;

        public DogEditWindow(DogEntry? existingEntry = null)
        {
            InitializeComponent();
            
            try
            {
                _viewModel = new DogEditViewModel(existingEntry);
                DataContext = _viewModel;
                
                // Subscribe to ViewModel events
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                _viewModel.RequestClose += ViewModel_RequestClose;
                
                LoggingService.Instance.LogInfo($"DogEditWindow (MVVM) initialized for {(existingEntry != null ? "editing" : "creating")} dog");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing DogEditWindow", ex);
                MessageBox.Show($"Fehler beim Initialisieren des Fensters: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                // Handle DialogResult changes
                if (e.PropertyName == nameof(DogEditViewModel.DialogResult) && _viewModel.DialogResult.HasValue)
                {
                    DialogResult = _viewModel.DialogResult.Value;
                    
                    if (_viewModel.DialogResult.Value)
                    {
                        LoggingService.Instance.LogInfo($"Dog successfully saved via MVVM: {_viewModel.DogEntry.Name}");
                    }
                    else
                    {
                        LoggingService.Instance.LogInfo("Dog edit cancelled via MVVM");
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
                LoggingService.Instance.LogError("Error closing DogEditWindow via RequestClose", ex);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            try
            {
                // Ctrl+S to save
                if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    if (_viewModel.SaveCommand.CanExecute(null))
                    {
                        _viewModel.SaveCommand.Execute(null);
                    }
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
                
                base.OnKeyDown(e);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling key down in DogEditWindow", ex);
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
                
                LoggingService.Instance.LogInfo("DogEditWindow (MVVM) closed");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error closing DogEditWindow", ex);
            }
            finally
            {
                base.OnClosed(e);
            }
        }
    }
}
