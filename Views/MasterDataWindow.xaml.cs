using System;
using System.Windows;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// MasterDataWindow - Enhanced with Theme Integration v1.9.0
    /// Now inherits from BaseThemeWindow for automatic theme support
    /// Vollständig auf MVVM-Pattern umgestellt mit Unified Theme System
    /// </summary>
    public partial class MasterDataWindow : BaseThemeWindow
    {
        private MasterDataViewModel? _viewModel;

        public MasterDataWindow()
        {
            InitializeComponent();
            InitializeThemeSupport(); // Initialize theme after component initialization
            InitializeViewModel();
            
            LoggingService.Instance.LogInfo("MasterDataWindow initialized with Unified Theme System v1.9.0");
        }

        private void InitializeViewModel()
        {
            try
            {
                _viewModel = new MasterDataViewModel();
                DataContext = _viewModel;
                
                // Subscribe to ViewModel events if needed
                if (_viewModel != null)
                {
                    // Add event subscriptions here if ViewModel has events
                }
                
                LoggingService.Instance.LogInfo("MasterDataViewModel initialized and connected");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing MasterDataViewModel", ex);
                MessageBox.Show($"Fehler beim Initialisieren der Stammdaten:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void ApplyThemeToWindow(bool isDarkMode)
        {
            try
            {
                // Apply theme-specific styling
                base.ApplyThemeToWindow(isDarkMode);
                
                LoggingService.Instance.LogInfo($"Theme applied to MasterDataWindow: {(isDarkMode ? "Dark" : "Light")} mode with Orange design");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to MasterDataWindow", ex);
            }
        }

        private async void MasterDataWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // MVVM: LoadData-Command ausführen
                if (_viewModel?.LoadDataCommand.CanExecute(null) == true)
                {
                    await ((RelayCommand)_viewModel.LoadDataCommand).ExecuteAsync();
                    LoggingService.Instance.LogInfo("MasterData loaded successfully via MVVM");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading master data via MVVM", ex);
                MessageBox.Show($"Fehler beim Laden der Stammdaten:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Window Events

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Cleanup ViewModel subscriptions if needed
                if (_viewModel is IDisposable disposableViewModel)
                {
                    disposableViewModel.Dispose();
                }
                
                LoggingService.Instance.LogInfo("MasterDataWindow closed and cleaned up");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during MasterDataWindow cleanup", ex);
            }
            finally
            {
                base.OnClosed(e);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Check for unsaved changes if needed
                if (_viewModel?.HasUnsavedChanges == true)
                {
                    var result = MessageBox.Show(
                        "Sie haben ungespeicherte Änderungen. Möchten Sie das Fenster wirklich schließen?",
                        "Ungespeicherte Änderungen",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                
                base.OnClosing(e);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during MasterDataWindow closing", ex);
                base.OnClosing(e);
            }
        }

        #endregion
    }
}
