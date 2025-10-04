using System;
using System.Windows;
using Einsatzueberwachung.Models;

namespace Einsatzueberwachung
{
    public partial class PersonalEditWindow : Window
    {
        public PersonalEntry PersonalEntry { get; private set; }
        private readonly bool _isEditMode;

        public PersonalEditWindow(PersonalEntry? existingEntry = null)
        {
            InitializeComponent();

            _isEditMode = existingEntry != null;
            PersonalEntry = existingEntry ?? new PersonalEntry();

            if (_isEditMode)
            {
                Title = "Personal bearbeiten";
                LoadData();
            }
            else
            {
                Title = "Neues Personal";
            }
        }

        private void LoadData()
        {
            TxtVorname.Text = PersonalEntry.Vorname;
            TxtNachname.Text = PersonalEntry.Nachname;
            TxtNotizen.Text = PersonalEntry.Notizen;
            ChkActive.IsChecked = PersonalEntry.IsActive;

            // Load skills
            ChkHundefuehrer.IsChecked = PersonalEntry.Skills.HasFlag(PersonalSkills.Hundefuehrer);
            ChkHelfer.IsChecked = PersonalEntry.Skills.HasFlag(PersonalSkills.Helfer);
            ChkFuehrungsassistent.IsChecked = PersonalEntry.Skills.HasFlag(PersonalSkills.Fuehrungsassistent);
            ChkGruppenfuehrer.IsChecked = PersonalEntry.Skills.HasFlag(PersonalSkills.Gruppenfuehrer);
            ChkZugfuehrer.IsChecked = PersonalEntry.Skills.HasFlag(PersonalSkills.Zugfuehrer);
            ChkVerbandsfuehrer.IsChecked = PersonalEntry.Skills.HasFlag(PersonalSkills.Verbandsfuehrer);
            ChkDrohnenpilot.IsChecked = PersonalEntry.Skills.HasFlag(PersonalSkills.Drohnenpilot);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(TxtVorname.Text))
                {
                    MessageBox.Show("Bitte geben Sie einen Vornamen ein.", 
                        "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtVorname.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtNachname.Text))
                {
                    MessageBox.Show("Bitte geben Sie einen Nachnamen ein.", 
                        "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtNachname.Focus();
                    return;
                }

                // Save data
                PersonalEntry.Vorname = TxtVorname.Text.Trim();
                PersonalEntry.Nachname = TxtNachname.Text.Trim();
                PersonalEntry.Notizen = TxtNotizen.Text.Trim();
                PersonalEntry.IsActive = ChkActive.IsChecked == true;

                // Build skills flags
                PersonalSkills skills = PersonalSkills.None;
                if (ChkHundefuehrer.IsChecked == true) skills |= PersonalSkills.Hundefuehrer;
                if (ChkHelfer.IsChecked == true) skills |= PersonalSkills.Helfer;
                if (ChkFuehrungsassistent.IsChecked == true) skills |= PersonalSkills.Fuehrungsassistent;
                if (ChkGruppenfuehrer.IsChecked == true) skills |= PersonalSkills.Gruppenfuehrer;
                if (ChkZugfuehrer.IsChecked == true) skills |= PersonalSkills.Zugfuehrer;
                if (ChkVerbandsfuehrer.IsChecked == true) skills |= PersonalSkills.Verbandsfuehrer;
                if (ChkDrohnenpilot.IsChecked == true) skills |= PersonalSkills.Drohnenpilot;

                PersonalEntry.Skills = skills;

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
