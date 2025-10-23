using System;
using System.Windows;
using System.Windows.Input;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// StartWindow with Unified Theme System v5.0
    /// Now uses UnifiedThemeManager for clean, automatic theme support
    /// Zero-configuration theme integration
    /// </summary>
    public partial class StartWindow : BaseThemeWindow
    {
        private readonly StartViewModel _viewModel;
        
        public EinsatzData? EinsatzData { get; private set; }
        public int FirstWarningMinutes { get; private set; }
        public int SecondWarningMinutes { get; private set; }

        public StartWindow()
        {
            try
            {
                LoggingService.Instance.LogInfo("Creating StartWindow v5.0 with Unified Theme System...");
                
                InitializeComponent();
                // BaseThemeWindow handles all theme registration automatically!
                
                _viewModel = new StartViewModel();
                DataContext = _viewModel;
                
                // Subscribe to ViewModel events
                _viewModel.RequestClose += OnRequestClose;
                _viewModel.ShowMessage += OnShowMessage;
                
                LoggingService.Instance.LogInfo($"StartWindow v5.0 initialized with automatic theme support via UnifiedThemeManager");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error in StartWindow constructor", ex);
                throw; // Re-throw to see the actual error
            }
        }

        #region BaseThemeWindow Override - Optional Customizations

        /// <summary>
        /// Optional: StartWindow-spezifische Theme-Anpassungen
        /// BaseThemeWindow handled bereits die Standard-Theme-Anwendung
        /// </summary>
        protected override void ApplyThemeToWindow(bool isDarkMode)
        {
            try
            {
                // Call base implementation first (does the heavy lifting)
                base.ApplyThemeToWindow(isDarkMode);
                
                // Optional: StartWindow-spezifische Anpassungen hier hinzufügen
                // Zum Beispiel: Window-Titel-Farbe, spezielle Animationen, etc.
                
                LoggingService.Instance.LogInfo($"StartWindow theme applied: {(isDarkMode ? "Dark" : "Light")} mode with Orange design via UnifiedThemeManager");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to StartWindow", ex);
            }
        }

        #endregion

        #region UI Event Handlers

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
                LoggingService.Instance.LogError("Error handling TextBox MouseWheel event", ex);
            }
        }

        /// <summary>
        /// Design-Einstellungen Button geklickt
        /// Öffnet SettingsWindow mit Unified Theme System Integration
        /// </summary>
        private void ThemeSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new SettingsWindow();
                settingsWindow.Owner = this;
                
                LoggingService.Instance.LogInfo("🎨 Opening SettingsWindow with Unified Theme System");
                
                // Zeige direkt die Appearance/Theme-Kategorie an
                settingsWindow.ShowCategory("appearance");
                
                var result = settingsWindow.ShowDialog();
                
                if (result == true)
                {
                    LoggingService.Instance.LogInfo("✅ Settings saved successfully from StartWindow - Theme changes applied automatically");
                }
                else
                {
                    LoggingService.Instance.LogInfo("🚫 Settings dialog canceled from StartWindow");
                }
                
                // Theme-Updates erfolgen automatisch über UnifiedThemeManager!
                // Kein manueller Refresh nötig
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening settings window", ex);
                MessageBox.Show($"Fehler beim Öffnen der Einstellungen:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region ViewModel Event Handlers

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
                        
                        // Show MainWindow (gets automatic theme support from BaseThemeWindow)
                        mainWindow.Show();
                        
                        // Close StartWindow (App won't exit because MainWindow is still open)
                        this.Close();
                        
                        LoggingService.Instance.LogInfo("✅ Transition from StartWindow to MainWindow completed with automatic theme transfer");
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
                LoggingService.Instance.LogError("Error handling RequestClose event", ex);
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
                LoggingService.Instance.LogError("Error showing message", ex);
            }
        }

        #endregion

        #region Window Events & Cleanup

        protected override void OnWindowClosed(EventArgs e)
        {
            try
            {
                // Unsubscribe from ViewModel events
                if (_viewModel != null)
                {
                    _viewModel.RequestClose -= OnRequestClose;
                    _viewModel.ShowMessage -= OnShowMessage;
                }
                
                LoggingService.Instance.LogInfo("StartWindow v5.0 closed and cleaned up - theme cleanup handled by BaseThemeWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during StartWindow cleanup", ex);
            }
            finally
            {
                // BaseThemeWindow handles theme cleanup automatically
                base.OnWindowClosed(e);
            }
        }

        #endregion

        #region Theme Helper Methods (Optional)

        /// <summary>
        /// Beispiel: Theme-Farbe abrufen für spezielle UI-Elemente
        /// </summary>
        private void UpdateSpecialElements()
        {
            try
            {
                // Beispiel: Spezielle Farbanpassungen
                var primaryColor = GetThemeColor("Primary");
                var surfaceColor = GetThemeColor("Surface");
                
                // Verwende die Farben für spezielle UI-Anpassungen
                // (Meistens nicht nötig, da XAML-Bindings automatisch funktionieren)
                
                LoggingService.Instance.LogInfo($"Special theme elements updated - Primary: {primaryColor}, Surface: {surfaceColor}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating special theme elements", ex);
            }
        }

        #endregion
    }
}
