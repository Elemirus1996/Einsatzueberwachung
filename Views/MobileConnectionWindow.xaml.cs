using System;
using System.Windows;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// MobileConnectionWindow - MVVM Implementation v1.9.0
    /// Vollständig auf MVVM umgestellt mit MobileConnectionViewModel
    /// </summary>
    public partial class MobileConnectionWindow : Window
    {
        private MobileConnectionViewModel? _viewModel;

        public MobileConnectionWindow()
        {
            InitializeComponent();
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            try
            {
                _viewModel = new MobileConnectionViewModel();
                DataContext = _viewModel;
                
                LoggingService.Instance.LogInfo("MobileConnectionWindow initialized with MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing MobileConnectionWindow ViewModel", ex);
                MessageBox.Show($"Fehler beim Initialisieren des Mobile-Fensters: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // ViewModel cleanup via IDisposable
                _viewModel?.Dispose();
                LoggingService.Instance.LogInfo("MobileConnectionWindow closed and cleaned up");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during MobileConnectionWindow cleanup", ex);
            }
            finally
            {
                base.OnClosed(e);
            }
        }

        // Public method für externes Force-Close (MainWindow Integration)
        public void ForceClose()
        {
            try
            {
                _viewModel?.ForceClose();
                Close();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during ForceClose", ex);
            }
        }
    }
}
