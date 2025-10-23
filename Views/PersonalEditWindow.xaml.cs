using System;
using System.Windows;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// PersonalEditWindow v2.0 - Enhanced with BaseThemeWindow Integration
    /// Now inherits from BaseThemeWindow for automatic theme support
    /// Fully integrated with design system and theme service
    /// Vollständig auf MVVM-Pattern umgestellt - minimales Code-Behind
    /// </summary>
    public partial class PersonalEditWindow : BaseThemeWindow
    {
        private readonly PersonalEditViewModel _viewModel;

        /// <summary>
        /// Das bearbeitete PersonalEntry-Objekt - wird vom ViewModel verwaltet
        /// </summary>
        public PersonalEntry PersonalEntry => _viewModel.PersonalEntry;

        public PersonalEditWindow(PersonalEntry? existingEntry = null)
        {
            InitializeComponent();
            InitializeThemeSupport(); // Initialize theme after component initialization
            
            try
            {
                // ViewModel mit bestehenden Daten initialisieren
                _viewModel = new PersonalEditViewModel(existingEntry);
                DataContext = _viewModel;
                
                LoggingService.Instance.LogInfo($"PersonalEditWindow v2.0 initialized with BaseThemeWindow and MVVM - Mode: {(existingEntry != null ? "Edit" : "New")}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing PersonalEditWindow with MVVM and theme support", ex);
                MessageBox.Show($"Fehler beim Laden des Personal-Bearbeitungs-Fensters: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Fallback: Fenster schließen bei kritischem Fehler
                Close();
            }
        }

        protected override void ApplyThemeToWindow(bool isDarkMode)
        {
            try
            {
                // Apply theme-specific styling
                base.ApplyThemeToWindow(isDarkMode);
                
                LoggingService.Instance.LogInfo($"Theme applied to PersonalEditWindow: {(isDarkMode ? "Dark" : "Light")} mode with Orange design");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to PersonalEditWindow", ex);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                LoggingService.Instance.LogInfo($"PersonalEditWindow v2.0 closed - DialogResult: {DialogResult}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during PersonalEditWindow closing", ex);
            }
            finally
            {
                base.OnClosed(e);
            }
        }
    }
}
