using System;
using System.Windows;
using System.Windows.Input;
using Einsatzueberwachung.Services;
using Einsatzueberwachung.ViewModels;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// MVVM-basiertes Update-Benachrichtigungs-Fenster für GitHub-Updates
    /// </summary>
    public partial class UpdateNotificationWindow : Window
    {
        private readonly UpdateNotificationViewModel _viewModel = null!;

        public UpdateNotificationWindow(UpdateInfo updateInfo)
        {
            InitializeComponent();
            
            try
            {
                _viewModel = new UpdateNotificationViewModel(updateInfo);
                DataContext = _viewModel;
                
                // Subscribe to ViewModel events
                _viewModel.RequestClose += ViewModel_RequestClose;
                
                LoggingService.Instance.LogInfo($"UpdateNotificationWindow (MVVM) initialized for version {updateInfo.Version}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing UpdateNotificationWindow", ex);
                MessageBox.Show($"Fehler beim Initialisieren des Update-Fensters: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewModel_RequestClose()
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error closing UpdateNotificationWindow via ViewModel", ex);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            try
            {
                // Enter to download (if enabled)
                if (e.Key == Key.Enter && _viewModel.DownloadUpdateCommand.CanExecute(null))
                {
                    _viewModel.DownloadUpdateCommand.Execute(null);
                    e.Handled = true;
                    return;
                }
                
                // Escape to close (remind later if not mandatory, otherwise close)
                if (e.Key == Key.Escape)
                {
                    if (!_viewModel.IsMandatoryUpdate && _viewModel.RemindLaterCommand.CanExecute(null))
                    {
                        _viewModel.RemindLaterCommand.Execute(null);
                    }
                    else
                    {
                        Close();
                    }
                    e.Handled = true;
                    return;
                }
                
                // Ctrl+R for Release Notes
                if (e.Key == Key.R && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    if (_viewModel.ShowReleaseNotesCommand.CanExecute(null))
                    {
                        _viewModel.ShowReleaseNotesCommand.Execute(null);
                    }
                    e.Handled = true;
                    return;
                }
                
                // S for Skip (if not mandatory)
                if (e.Key == Key.S && !_viewModel.IsMandatoryUpdate && _viewModel.SkipUpdateCommand.CanExecute(null))
                {
                    _viewModel.SkipUpdateCommand.Execute(null);
                    e.Handled = true;
                    return;
                }
                
                base.OnKeyDown(e);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling key down in UpdateNotificationWindow", ex);
                base.OnKeyDown(e);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // ✅ FIXED: Null-Check für _viewModel hinzugefügt
                if (_viewModel != null)
                {
                    // For mandatory updates, prevent closing unless download is complete
                    if (_viewModel.IsMandatoryUpdate && _viewModel.IsDownloadEnabled)
                    {
                        var result = MessageBox.Show(
                            "Dies ist ein wichtiges Update und kann nicht übersprungen werden.\n\n" +
                            "Möchten Sie das Fenster wirklich schließen ohne das Update zu installieren?",
                            "Wichtiges Update",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);
                        
                        if (result == MessageBoxResult.No)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                }
                
                LoggingService.Instance.LogInfo("UpdateNotificationWindow closing via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during UpdateNotificationWindow closing", ex);
            }
            finally
            {
                base.OnClosing(e);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Unsubscribe from ViewModel events
                if (_viewModel != null)
                {
                    _viewModel.RequestClose -= ViewModel_RequestClose;
                    _viewModel.Dispose();
                }
                
                LoggingService.Instance.LogInfo("UpdateNotificationWindow (MVVM) closed and disposed");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error closing UpdateNotificationWindow", ex);
            }
            finally
            {
                base.OnClosed(e);
            }
        }
    }
}
