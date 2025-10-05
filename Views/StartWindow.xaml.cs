using System.Windows;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    public partial class StartWindow : Window
    {
        private readonly StartViewModel _viewModel;
        
        public EinsatzData? EinsatzData { get; private set; }
        public int FirstWarningMinutes { get; private set; }
        public int SecondWarningMinutes { get; private set; }

        public StartWindow()
        {
            InitializeComponent();
            
            _viewModel = new StartViewModel();
            DataContext = _viewModel;
            
            // Subscribe to ViewModel events
            _viewModel.RequestClose += OnRequestClose;
            _viewModel.ShowMessage += OnShowMessage;
            
            InitializeTheme();
            
            LoggingService.Instance?.LogInfo("StartWindow v1.9 initialized with MVVM pattern and Orange design");
        }

        private void OnRequestClose(object? sender, EventArgs e)
        {
            try
            {
                if (_viewModel.EinsatzData != null)
                {
                    // Einsatz started successfully
                    EinsatzData = _viewModel.EinsatzData;
                    FirstWarningMinutes = _viewModel.FirstWarningMinutes;
                    SecondWarningMinutes = _viewModel.SecondWarningMinutes;
                    
                    // Check if this window was opened as dialog or as startup window
                    if (Owner != null)
                    {
                        // Opened as dialog, can set DialogResult
                        DialogResult = true;
                    }
                    else
                    {
                        // Opened as startup window - create MainWindow
                        var mainWindow = new MainWindow(EinsatzData, FirstWarningMinutes, SecondWarningMinutes);
                        
                        // Set as Application.MainWindow BEFORE showing it
                        Application.Current.MainWindow = mainWindow;
                        
                        // Change ShutdownMode (now safe)
                        Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                        
                        // Show MainWindow
                        mainWindow.Show();
                        
                        // Close StartWindow (App won't exit because MainWindow is still open)
                        this.Close();
                        
                        LoggingService.Instance?.LogInfo("✅ Transition from StartWindow to MainWindow completed with MVVM");
                    }
                }
                else
                {
                    // Cancelled
                    if (Owner != null)
                    {
                        // Opened as dialog, can set DialogResult
                        DialogResult = false;
                    }
                    else
                    {
                        // Opened as startup window, close application
                        Application.Current.Shutdown();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error handling RequestClose event", ex);
                MessageBox.Show($"Fehler beim Verarbeiten der Anfrage: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnShowMessage(object? sender, string message)
        {
            try
            {
                MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error showing message", ex);
            }
        }

        private void InitializeTheme()
        {
            try
            {
                // Apply current theme to start window
                ApplyTheme(ThemeService.Instance.IsDarkMode);
                ThemeService.Instance.ThemeChanged += OnThemeChanged;
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error initializing theme in StartWindow", ex);
            }
        }

        private void OnThemeChanged(bool isDarkMode)
        {
            Dispatcher.Invoke(() => ApplyTheme(isDarkMode));
        }

        private void ApplyTheme(bool isDarkMode)
        {
            try
            {
                // Theme is applied automatically through DynamicResource bindings
                // No manual resource updates needed with MVVM and proper design system
                LoggingService.Instance?.LogInfo($"Theme applied to StartWindow: {(isDarkMode ? "Dark" : "Light")} mode with Orange design");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error applying theme to StartWindow", ex);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events
            if (_viewModel != null)
            {
                _viewModel.RequestClose -= OnRequestClose;
                _viewModel.ShowMessage -= OnShowMessage;
            }
            
            ThemeService.Instance.ThemeChanged -= OnThemeChanged;
            
            base.OnClosed(e);
        }
    }
}
