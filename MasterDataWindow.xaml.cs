using System;
using System.Linq;
using System.Windows;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung
{
    public partial class MasterDataWindow : Window
    {
        private readonly MasterDataService _masterDataService;

        public MasterDataWindow()
        {
            InitializeComponent();
            _masterDataService = MasterDataService.Instance;
            Loaded += MasterDataWindow_Loaded;
        }

        private async void MasterDataWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _masterDataService.LoadDataAsync();
                LoadData();
                UpdateStatistics();
                LoggingService.Instance.LogInfo("Master data window loaded");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading master data window", ex);
                MessageBox.Show($"Fehler beim Laden der Stammdaten:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadData()
        {
            PersonalDataGrid.ItemsSource = _masterDataService.PersonalList;
            DogsDataGrid.ItemsSource = _masterDataService.DogList;
            UpdateCounts();
        }

        private void UpdateCounts()
        {
            TxtCounts.Text = $"Personal: {_masterDataService.PersonalList.Count} | " +
                           $"Hunde: {_masterDataService.DogList.Count}";
        }

        private void UpdateStatistics()
        {
            // Personal statistics
            var activePersonal = _masterDataService.GetActivePersonal().Count;
            var hundefuehrer = _masterDataService.GetPersonalBySkill(PersonalSkills.Hundefuehrer).Count;
            var helfer = _masterDataService.GetPersonalBySkill(PersonalSkills.Helfer).Count;
            var fuehrung = _masterDataService.GetPersonalBySkill(PersonalSkills.Fuehrungsassistent).Count;

            TxtPersonalStats.Text = $"Gesamt: {_masterDataService.PersonalList.Count}\n" +
                                   $"Aktiv: {activePersonal}\n" +
                                   $"Hundeführer: {hundefuehrer}\n" +
                                   $"Helfer: {helfer}\n" +
                                   $"Führungsassistenten: {fuehrung}";

            // Dog statistics
            var activeDogs = _masterDataService.GetActiveDogs().Count;
            var flaechensuche = _masterDataService.GetDogsBySpecialization(DogSpecialization.Flaechensuche).Count;
            var truemmersuche = _masterDataService.GetDogsBySpecialization(DogSpecialization.Truemmersuche).Count;
            var mantrailer = _masterDataService.GetDogsBySpecialization(DogSpecialization.Mantrailing).Count;

            TxtDogStats.Text = $"Gesamt: {_masterDataService.DogList.Count}\n" +
                              $"Aktiv: {activeDogs}\n" +
                              $"Flächensuche: {flaechensuche}\n" +
                              $"Trümmersuche: {truemmersuche}\n" +
                              $"Mantrailing: {mantrailer}";
        }

        private void BtnAddPersonal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var editWindow = new PersonalEditWindow();
                if (editWindow.ShowDialog() == true)
                {
                    _masterDataService.AddPersonal(editWindow.PersonalEntry);
                    UpdateCounts();
                    UpdateStatistics();
                    TxtStatus.Text = $"Personal '{editWindow.PersonalEntry.FullName}' hinzugefügt";
                    LoggingService.Instance.LogInfo($"Personal added: {editWindow.PersonalEntry.FullName}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error adding personal", ex);
                MessageBox.Show($"Fehler beim Hinzufügen:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEditPersonal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PersonalDataGrid.SelectedItem is PersonalEntry selectedPersonal)
                {
                    var editWindow = new PersonalEditWindow(selectedPersonal);
                    if (editWindow.ShowDialog() == true)
                    {
                        _masterDataService.UpdatePersonal(editWindow.PersonalEntry);
                        PersonalDataGrid.Items.Refresh();
                        UpdateStatistics();
                        TxtStatus.Text = $"Personal '{editWindow.PersonalEntry.FullName}' aktualisiert";
                        LoggingService.Instance.LogInfo($"Personal updated: {editWindow.PersonalEntry.FullName}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error editing personal", ex);
                MessageBox.Show($"Fehler beim Bearbeiten:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeletePersonal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PersonalDataGrid.SelectedItem is PersonalEntry selectedPersonal)
                {
                    var result = MessageBox.Show(
                        $"Möchten Sie '{selectedPersonal.FullName}' wirklich löschen?",
                        "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _masterDataService.RemovePersonal(selectedPersonal.Id);
                        UpdateCounts();
                        UpdateStatistics();
                        TxtStatus.Text = $"Personal '{selectedPersonal.FullName}' gelöscht";
                        LoggingService.Instance.LogInfo($"Personal deleted: {selectedPersonal.FullName}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error deleting personal", ex);
                MessageBox.Show($"Fehler beim Löschen:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddDog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var editWindow = new DogEditWindow();
                if (editWindow.ShowDialog() == true)
                {
                    _masterDataService.AddDog(editWindow.DogEntry);
                    UpdateCounts();
                    UpdateStatistics();
                    TxtStatus.Text = $"Hund '{editWindow.DogEntry.Name}' hinzugefügt";
                    LoggingService.Instance.LogInfo($"Dog added: {editWindow.DogEntry.Name}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error adding dog", ex);
                MessageBox.Show($"Fehler beim Hinzufügen:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEditDog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DogsDataGrid.SelectedItem is DogEntry selectedDog)
                {
                    var editWindow = new DogEditWindow(selectedDog);
                    if (editWindow.ShowDialog() == true)
                    {
                        _masterDataService.UpdateDog(editWindow.DogEntry);
                        DogsDataGrid.Items.Refresh();
                        UpdateStatistics();
                        TxtStatus.Text = $"Hund '{editWindow.DogEntry.Name}' aktualisiert";
                        LoggingService.Instance.LogInfo($"Dog updated: {editWindow.DogEntry.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error editing dog", ex);
                MessageBox.Show($"Fehler beim Bearbeiten:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeleteDog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DogsDataGrid.SelectedItem is DogEntry selectedDog)
                {
                    var result = MessageBox.Show(
                        $"Möchten Sie '{selectedDog.Name}' wirklich löschen?",
                        "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _masterDataService.RemoveDog(selectedDog.Id);
                        UpdateCounts();
                        UpdateStatistics();
                        TxtStatus.Text = $"Hund '{selectedDog.Name}' gelöscht";
                        LoggingService.Instance.LogInfo($"Dog deleted: {selectedDog.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error deleting dog", ex);
                MessageBox.Show($"Fehler beim Löschen:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PersonalDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            BtnEditPersonal.IsEnabled = PersonalDataGrid.SelectedItem != null;
            BtnDeletePersonal.IsEnabled = PersonalDataGrid.SelectedItem != null;
        }

        private void DogsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            BtnEditDog.IsEnabled = DogsDataGrid.SelectedItem != null;
            BtnDeleteDog.IsEnabled = DogsDataGrid.SelectedItem != null;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
