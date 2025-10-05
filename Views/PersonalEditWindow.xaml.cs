using System;
using System.Windows;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// Interaction logic for PersonalEditWindow.xaml
    /// Vollständig auf MVVM-Pattern umgestellt - minimales Code-Behind
    /// v1.9.0 mit Orange-Design-System
    /// </summary>
    public partial class PersonalEditWindow : Window
    {
        private readonly PersonalEditViewModel _viewModel;

        /// <summary>
        /// Das bearbeitete PersonalEntry-Objekt - wird vom ViewModel verwaltet
        /// </summary>
        public PersonalEntry PersonalEntry => _viewModel.PersonalEntry;

        public PersonalEditWindow(PersonalEntry? existingEntry = null)
        {
            InitializeComponent();
            
            try
            {
                // ViewModel mit bestehenden Daten initialisieren
                _viewModel = new PersonalEditViewModel(existingEntry);
                DataContext = _viewModel;
                
                LoggingService.Instance.LogInfo($"PersonalEditWindow v1.9.0 initialized with MVVM - Mode: {(existingEntry != null ? "Edit" : "New")}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing PersonalEditWindow with MVVM", ex);
                MessageBox.Show($"Fehler beim Laden des Personal-Bearbeitungs-Fensters: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Fallback: Fenster schließen bei kritischem Fehler
                Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                LoggingService.Instance.LogInfo($"PersonalEditWindow closed - DialogResult: {DialogResult}");
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
