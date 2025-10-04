using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung
{
    public partial class DogEditWindow : Window
    {
        public DogEntry DogEntry { get; private set; }
        private readonly bool _isEditMode;
        private readonly MasterDataService _masterDataService;

        public DogEditWindow(DogEntry? existingEntry = null)
        {
            InitializeComponent();

            _masterDataService = MasterDataService.Instance;
            _isEditMode = existingEntry != null;
            DogEntry = existingEntry ?? new DogEntry();

            Loaded += DogEditWindow_Loaded;

            if (_isEditMode)
            {
                Title = "Hund bearbeiten";
            }
            else
            {
                Title = "Neuer Hund";
            }
        }

        private void DogEditWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadHundefuehrerList();

            if (_isEditMode)
            {
                LoadData();
            }
        }

        private void LoadHundefuehrerList()
        {
            var hundefuehrer = _masterDataService.GetPersonalBySkill(PersonalSkills.Hundefuehrer);
            
            // Add empty option
            CmbHundefuehrer.Items.Add(new { Id = "", FullName = "(Kein Hundeführer)" });
            
            foreach (var person in hundefuehrer)
            {
                CmbHundefuehrer.Items.Add(person);
            }

            CmbHundefuehrer.SelectedIndex = 0;
        }

        private void LoadData()
        {
            TxtName.Text = DogEntry.Name;
            TxtRasse.Text = DogEntry.Rasse;
            TxtAlter.Text = DogEntry.Alter.ToString();
            TxtNotizen.Text = DogEntry.Notizen;
            ChkActive.IsChecked = DogEntry.IsActive;

            // Load specializations
            ChkFlaechensuche.IsChecked = DogEntry.Specializations.HasFlag(DogSpecialization.Flaechensuche);
            ChkTruemmersuche.IsChecked = DogEntry.Specializations.HasFlag(DogSpecialization.Truemmersuche);
            ChkMantrailing.IsChecked = DogEntry.Specializations.HasFlag(DogSpecialization.Mantrailing);
            ChkWasserortung.IsChecked = DogEntry.Specializations.HasFlag(DogSpecialization.Wasserortung);
            ChkLawinensuche.IsChecked = DogEntry.Specializations.HasFlag(DogSpecialization.Lawinensuche);
            ChkGelaendesuche.IsChecked = DogEntry.Specializations.HasFlag(DogSpecialization.Gelaendesuche);
            ChkLeichensuche.IsChecked = DogEntry.Specializations.HasFlag(DogSpecialization.Leichensuche);

            // Select hundefuehrer if set
            if (!string.IsNullOrEmpty(DogEntry.HundefuehrerId))
            {
                var hundefuehrer = _masterDataService.GetPersonalById(DogEntry.HundefuehrerId);
                if (hundefuehrer != null)
                {
                    CmbHundefuehrer.SelectedItem = hundefuehrer;
                }
            }
        }

        private void TxtAlter_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Only allow numbers
            e.Handled = !IsTextNumeric(e.Text);
        }

        private static bool IsTextNumeric(string text)
        {
            return Regex.IsMatch(text, "^[0-9]+$");
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(TxtName.Text))
                {
                    MessageBox.Show("Bitte geben Sie einen Namen ein.", 
                        "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtName.Focus();
                    return;
                }

                // Save data
                DogEntry.Name = TxtName.Text.Trim();
                DogEntry.Rasse = TxtRasse.Text.Trim();
                
                if (int.TryParse(TxtAlter.Text, out int alter))
                {
                    DogEntry.Alter = alter;
                }
                else
                {
                    DogEntry.Alter = 0;
                }

                DogEntry.Notizen = TxtNotizen.Text.Trim();
                DogEntry.IsActive = ChkActive.IsChecked == true;

                // Build specializations flags
                DogSpecialization specs = DogSpecialization.None;
                if (ChkFlaechensuche.IsChecked == true) specs |= DogSpecialization.Flaechensuche;
                if (ChkTruemmersuche.IsChecked == true) specs |= DogSpecialization.Truemmersuche;
                if (ChkMantrailing.IsChecked == true) specs |= DogSpecialization.Mantrailing;
                if (ChkWasserortung.IsChecked == true) specs |= DogSpecialization.Wasserortung;
                if (ChkLawinensuche.IsChecked == true) specs |= DogSpecialization.Lawinensuche;
                if (ChkGelaendesuche.IsChecked == true) specs |= DogSpecialization.Gelaendesuche;
                if (ChkLeichensuche.IsChecked == true) specs |= DogSpecialization.Leichensuche;

                DogEntry.Specializations = specs;

                // Save hundefuehrer reference
                if (CmbHundefuehrer.SelectedItem is PersonalEntry selectedHundefuehrer)
                {
                    DogEntry.HundefuehrerId = selectedHundefuehrer.Id;
                }
                else
                {
                    DogEntry.HundefuehrerId = string.Empty;
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
