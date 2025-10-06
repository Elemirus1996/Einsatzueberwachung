using System.Windows;
using System.Windows.Input;
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
            // Initialize theme BEFORE InitializeComponent to ensure correct initial rendering
            InitializeThemeEarly();
            
            InitializeComponent();
            
            _viewModel = new StartViewModel();
            DataContext = _viewModel;
            
            // Subscribe to ViewModel events
            _viewModel.RequestClose += OnRequestClose;
            _viewModel.ShowMessage += OnShowMessage;
            
            // Apply theme after component initialization
            FinalizeThemeInitialization();
            
            LoggingService.Instance?.LogInfo($"StartWindow v1.9 initialized - Theme: {ThemeService.Instance.CurrentThemeStatus}, ScrollViewer optimized, Readability enhanced");
        }

        /// <summary>
        /// Frühe Theme-Initialisierung vor InitializeComponent
        /// </summary>
        private void InitializeThemeEarly()
        {
            try
            {
                // Stelle sicher dass ThemeService initialisiert ist
                var themeService = ThemeService.Instance;
                
                // ZUSÄTZLICHE SICHERHEIT: Theme auch hier nochmals anwenden
                var currentTime = DateTime.Now.TimeOfDay;
                bool shouldBeDark = currentTime >= new TimeSpan(18, 0, 0) || currentTime < new TimeSpan(8, 0, 0);
                
                System.Diagnostics.Debug.WriteLine($"=== StartWindow Theme Check ===");
                System.Diagnostics.Debug.WriteLine($"Current Time: {currentTime:hh\\:mm\\:ss}");
                System.Diagnostics.Debug.WriteLine($"Should be Dark: {shouldBeDark}");
                System.Diagnostics.Debug.WriteLine($"ThemeService says Dark: {themeService.IsDarkMode}");
                
                // FORCE korrekte Theme-Anwendung falls Diskrepanz
                if (themeService.IsDarkMode != shouldBeDark)
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 FORCING theme correction in StartWindow");
                    LoggingService.Instance.LogWarning($"StartWindow forcing theme correction: {shouldBeDark}");
                    
                    // Direkte Ressourcen-Anwendung
                    var app = Application.Current;
                    if (app?.Resources != null)
                    {
                        if (shouldBeDark)
                        {
                            app.Resources["Surface"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(18, 18, 18));
                            app.Resources["SurfaceContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));
                            app.Resources["OnSurface"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(227, 227, 227));
                            app.Resources["Primary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 183, 77));
                            app.Resources["OnPrimary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 15, 0));
                        }
                        else
                        {
                            app.Resources["Surface"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 254, 254));
                            app.Resources["SurfaceContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
                            app.Resources["OnSurface"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 28, 30));
                            app.Resources["Primary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 124, 0));
                            app.Resources["OnPrimary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                        }
                    }
                }
                
                LoggingService.Instance.LogInfo($"StartWindow theme early init completed - Should be: {(shouldBeDark ? "Dark" : "Light")}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in early theme initialization: {ex.Message}");
                LoggingService.Instance.LogError("Error in early theme initialization", ex);
            }
        }

        /// <summary>
        /// Finale Theme-Initialisierung nach InitializeComponent
        /// </summary>
        private void FinalizeThemeInitialization()
        {
            try
            {
                var themeService = ThemeService.Instance;
                
                // Apply current theme to start window
                ApplyTheme(themeService.IsDarkMode);
                
                // Subscribe to theme changes
                themeService.ThemeChanged += OnThemeChanged;
                
                LoggingService.Instance?.LogInfo($"StartWindow theme finalized - Applied: {(themeService.IsDarkMode ? "Dark" : "Light")} mode");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error finalizing theme initialization", ex);
            }
        }

        /// <summary>
        /// Event-Handler für TextBox MouseWheel-Events
        /// Leitet das MouseWheel-Event an den übergeordneten ScrollViewer weiter
        /// </summary>
        private void TextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                // Verhindert dass TextBox das MouseWheel-Event konsumiert
                // und leitet es an den MainScrollViewer weiter
                if (sender is System.Windows.Controls.TextBox textBox && !textBox.IsFocused)
                {
                    e.Handled = true;
                    
                    // Erstelle neues MouseWheel-Event für den MainScrollViewer
                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                    {
                        RoutedEvent = UIElement.MouseWheelEvent,
                        Source = sender
                    };
                    
                    // Leite Event an MainScrollViewer weiter
                    MainScrollViewer?.RaiseEvent(eventArg);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error handling TextBox MouseWheel event", ex);
            }
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
                        
                        LoggingService.Instance?.LogInfo("✅ Transition from StartWindow to MainWindow completed with theme and ScrollViewer optimizations");
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

        private void OnThemeChanged(bool isDarkMode)
        {
            Dispatcher.Invoke(() => ApplyTheme(isDarkMode));
        }

        private void ApplyTheme(bool isDarkMode)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Applying theme to StartWindow: {(isDarkMode ? "Dark" : "Light")}");

                // Force visual update to ensure theme changes are applied
                this.InvalidateVisual();
                this.UpdateLayout();
                
                LoggingService.Instance?.LogInfo($"Theme applied to StartWindow: {(isDarkMode ? "Dark" : "Light")} mode");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
                LoggingService.Instance?.LogError("Error applying theme to StartWindow", ex);
            }
        }

        /// <summary>
        /// DEBUG: Event-Handler für Theme-Test Button (temporär)
        /// </summary>
        private void DebugThemeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var themeService = ThemeService.Instance;
                
                System.Diagnostics.Debug.WriteLine($"\n=== MANUAL THEME TEST ===");
                System.Diagnostics.Debug.WriteLine($"Before Toggle - IsDarkMode: {themeService.IsDarkMode}");
                
                // Toggle theme manually
                themeService.ToggleTheme();
                
                System.Diagnostics.Debug.WriteLine($"After Toggle - IsDarkMode: {themeService.IsDarkMode}");
                System.Diagnostics.Debug.WriteLine($"After Toggle - Status: {themeService.CurrentThemeStatus}");
                System.Diagnostics.Debug.WriteLine("=== END MANUAL TEST ===\n");
                
                LoggingService.Instance?.LogInfo($"DEBUG: Manual theme toggle - New mode: {(themeService.IsDarkMode ? "Dark" : "Light")}");
                
                // Show simple success message
                var message = $"Theme gewechselt zu: {(themeService.IsDarkMode ? "Dark Mode" : "Light Mode")}\n\n" +
                             $"Status: {themeService.CurrentThemeStatus}\n" +
                             "TextBoxen sollten jetzt automatisch die korrekten Farben verwenden.";
                
                MessageBox.Show(message, "Theme Debug Test", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in debug theme button: {ex.Message}");
                LoggingService.Instance?.LogError("Error in debug theme button", ex);
                MessageBox.Show($"Fehler beim Theme-Test: {ex.Message}", "Debug Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
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
