using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    /// <summary>
    /// ViewModel für das MasterDataWindow - MVVM-Implementation für Stammdatenverwaltung
    /// </summary>
    public class MasterDataViewModel : BaseViewModel
    {
        private readonly MasterDataService _masterDataService;
        private string _statusText = "Bereit";
        private string _personalCountText = "Personal: 0";
        private string _dogCountText = "Hunde: 0";
        private string _personalStatsText = "";
        private string _dogStatsText = "";
        private PersonalEntry? _selectedPersonal;
        private DogEntry? _selectedDog;

        public MasterDataViewModel()
        {
            _masterDataService = MasterDataService.Instance;
            
            // Observable Collections für DataBinding
            PersonalList = new ObservableCollection<PersonalEntry>();
            DogList = new ObservableCollection<DogEntry>();

            // Commands initialisieren
            InitializeCommands();
        }

        #region Properties

        public ObservableCollection<PersonalEntry> PersonalList { get; }
        public ObservableCollection<DogEntry> DogList { get; }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string PersonalCountText
        {
            get => _personalCountText;
            set => SetProperty(ref _personalCountText, value);
        }

        public string DogCountText
        {
            get => _dogCountText;
            set => SetProperty(ref _dogCountText, value);
        }

        public string PersonalStatsText
        {
            get => _personalStatsText;
            set => SetProperty(ref _personalStatsText, value);
        }

        public string DogStatsText
        {
            get => _dogStatsText;
            set => SetProperty(ref _dogStatsText, value);
        }

        public PersonalEntry? SelectedPersonal
        {
            get => _selectedPersonal;
            set
            {
                SetProperty(ref _selectedPersonal, value);
                // CanExecute für Personal-Commands aktualisieren
                ((RelayCommand)EditPersonalCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeletePersonalCommand).RaiseCanExecuteChanged();
            }
        }

        public DogEntry? SelectedDog
        {
            get => _selectedDog;
            set
            {
                SetProperty(ref _selectedDog, value);
                // CanExecute für Dog-Commands aktualisieren
                ((RelayCommand)EditDogCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeleteDogCommand).RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; private set; } = null!;
        public ICommand CloseCommand { get; private set; } = null!;
        
        // Personal Commands
        public ICommand AddPersonalCommand { get; private set; } = null!;
        public ICommand EditPersonalCommand { get; private set; } = null!;
        public ICommand DeletePersonalCommand { get; private set; } = null!;
        
        // Dog Commands
        public ICommand AddDogCommand { get; private set; } = null!;
        public ICommand EditDogCommand { get; private set; } = null!;
        public ICommand DeleteDogCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            LoadDataCommand = new RelayCommand(async () => await ExecuteLoadDataAsync());
            CloseCommand = new RelayCommand<Window>(ExecuteClose);
            
            // Personal Commands
            AddPersonalCommand = new RelayCommand(ExecuteAddPersonal);
            EditPersonalCommand = new RelayCommand(ExecuteEditPersonal, CanExecuteEditPersonal);
            DeletePersonalCommand = new RelayCommand(ExecuteDeletePersonal, CanExecuteDeletePersonal);
            
            // Dog Commands
            AddDogCommand = new RelayCommand(ExecuteAddDog);
            EditDogCommand = new RelayCommand(ExecuteEditDog, CanExecuteEditDog);
            DeleteDogCommand = new RelayCommand(ExecuteDeleteDog, CanExecuteDeleteDog);
        }

        #endregion

        #region Command Implementations

        private async Task ExecuteLoadDataAsync()
        {
            try
            {
                StatusText = "Laden der Stammdaten...";
                
                await _masterDataService.LoadDataAsync();
                LoadData();
                UpdateStatistics();
                
                StatusText = "Stammdaten erfolgreich geladen";
                LoggingService.Instance.LogInfo("Master data loaded successfully via MVVM");
            }
            catch (Exception ex)
            {
                StatusText = "Fehler beim Laden der Stammdaten";
                LoggingService.Instance.LogError("Error loading master data via MVVM", ex);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Fehler beim Laden der Stammdaten:\n{ex.Message}", 
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void ExecuteClose(Window? window)
        {
            try
            {
                window?.Close();
                LoggingService.Instance.LogInfo("MasterDataWindow closed via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error closing MasterDataWindow", ex);
            }
        }

        #region Personal Commands

        private void ExecuteAddPersonal()
        {
            try
            {
                var editWindow = new Views.PersonalEditWindow();
                if (editWindow.ShowDialog() == true)
                {
                    _masterDataService.AddPersonal(editWindow.PersonalEntry);
                    LoadData();
                    UpdateStatistics();
                    StatusText = $"Personal '{editWindow.PersonalEntry.FullName}' hinzugefügt";
                    LoggingService.Instance.LogInfo($"Personal added via MVVM: {editWindow.PersonalEntry.FullName}");
                }
            }
            catch (Exception ex)
            {
                StatusText = "Fehler beim Hinzufügen des Personals";
                LoggingService.Instance.LogError("Error adding personal via MVVM", ex);
                MessageBox.Show($"Fehler beim Hinzufügen:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteEditPersonal()
        {
            try
            {
                if (SelectedPersonal != null)
                {
                    var editWindow = new Views.PersonalEditWindow(SelectedPersonal);
                    if (editWindow.ShowDialog() == true)
                    {
                        _masterDataService.UpdatePersonal(editWindow.PersonalEntry);
                        LoadData();
                        UpdateStatistics();
                        StatusText = $"Personal '{editWindow.PersonalEntry.FullName}' aktualisiert";
                        LoggingService.Instance.LogInfo($"Personal updated via MVVM: {editWindow.PersonalEntry.FullName}");
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText = "Fehler beim Bearbeiten des Personals";
                LoggingService.Instance.LogError("Error editing personal via MVVM", ex);
                MessageBox.Show($"Fehler beim Bearbeiten:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteDeletePersonal()
        {
            try
            {
                if (SelectedPersonal != null)
                {
                    var result = MessageBox.Show(
                        $"Möchten Sie '{SelectedPersonal.FullName}' wirklich löschen?",
                        "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var name = SelectedPersonal.FullName;
                        _masterDataService.RemovePersonal(SelectedPersonal.Id);
                        LoadData();
                        UpdateStatistics();
                        StatusText = $"Personal '{name}' gelöscht";
                        LoggingService.Instance.LogInfo($"Personal deleted via MVVM: {name}");
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText = "Fehler beim Löschen des Personals";
                LoggingService.Instance.LogError("Error deleting personal via MVVM", ex);
                MessageBox.Show($"Fehler beim Löschen:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteEditPersonal() => SelectedPersonal != null;
        private bool CanExecuteDeletePersonal() => SelectedPersonal != null;

        #endregion

        #region Dog Commands

        private void ExecuteAddDog()
        {
            try
            {
                var editWindow = new Views.DogEditWindow();
                if (editWindow.ShowDialog() == true)
                {
                    _masterDataService.AddDog(editWindow.DogEntry);
                    LoadData();
                    UpdateStatistics();
                    StatusText = $"Hund '{editWindow.DogEntry.Name}' hinzugefügt";
                    LoggingService.Instance.LogInfo($"Dog added via MVVM: {editWindow.DogEntry.Name}");
                }
            }
            catch (Exception ex)
            {
                StatusText = "Fehler beim Hinzufügen des Hundes";
                LoggingService.Instance.LogError("Error adding dog via MVVM", ex);
                MessageBox.Show($"Fehler beim Hinzufügen:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteEditDog()
        {
            try
            {
                if (SelectedDog != null)
                {
                    var editWindow = new Views.DogEditWindow(SelectedDog);
                    if (editWindow.ShowDialog() == true)
                    {
                        _masterDataService.UpdateDog(editWindow.DogEntry);
                        LoadData();
                        UpdateStatistics();
                        StatusText = $"Hund '{editWindow.DogEntry.Name}' aktualisiert";
                        LoggingService.Instance.LogInfo($"Dog updated via MVVM: {editWindow.DogEntry.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText = "Fehler beim Bearbeiten des Hundes";
                LoggingService.Instance.LogError("Error editing dog via MVVM", ex);
                MessageBox.Show($"Fehler beim Bearbeiten:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteDeleteDog()
        {
            try
            {
                if (SelectedDog != null)
                {
                    var result = MessageBox.Show(
                        $"Möchten Sie '{SelectedDog.Name}' wirklich löschen?",
                        "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var name = SelectedDog.Name;
                        _masterDataService.RemoveDog(SelectedDog.Id);
                        LoadData();
                        UpdateStatistics();
                        StatusText = $"Hund '{name}' gelöscht";
                        LoggingService.Instance.LogInfo($"Dog deleted via MVVM: {name}");
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText = "Fehler beim Löschen des Hundes";
                LoggingService.Instance.LogError("Error deleting dog via MVVM", ex);
                MessageBox.Show($"Fehler beim Löschen:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteEditDog() => SelectedDog != null;
        private bool CanExecuteDeleteDog() => SelectedDog != null;

        #endregion

        #endregion

        #region Private Methods

        private void LoadData()
        {
            // ObservableCollections aktualisieren
            PersonalList.Clear();
            foreach (var personal in _masterDataService.PersonalList)
            {
                PersonalList.Add(personal);
            }

            DogList.Clear();
            foreach (var dog in _masterDataService.DogList)
            {
                DogList.Add(dog);
            }

            UpdateCounts();
        }

        private void UpdateCounts()
        {
            PersonalCountText = $"Personal: {PersonalList.Count}";
            DogCountText = $"Hunde: {DogList.Count}";
        }

        private void UpdateStatistics()
        {
            try
            {
                // Personal statistics
                var activePersonal = _masterDataService.GetActivePersonal().Count;
                var hundefuehrer = _masterDataService.GetPersonalBySkill(PersonalSkills.Hundefuehrer).Count;
                var helfer = _masterDataService.GetPersonalBySkill(PersonalSkills.Helfer).Count;
                var fuehrung = _masterDataService.GetPersonalBySkill(PersonalSkills.Fuehrungsassistent).Count;

                PersonalStatsText = $"Gesamt: {PersonalList.Count}\n" +
                                   $"Aktiv: {activePersonal}\n" +
                                   $"Hundeführer: {hundefuehrer}\n" +
                                   $"Helfer: {helfer}\n" +
                                   $"Führungsassistenten: {fuehrung}";

                // Dog statistics
                var activeDogs = _masterDataService.GetActiveDogs().Count;
                var flaechensuche = _masterDataService.GetDogsBySpecialization(DogSpecialization.Flaechensuche).Count;
                var truemmersuche = _masterDataService.GetDogsBySpecialization(DogSpecialization.Truemmersuche).Count;
                var mantrailer = _masterDataService.GetDogsBySpecialization(DogSpecialization.Mantrailing).Count;

                DogStatsText = $"Gesamt: {DogList.Count}\n" +
                              $"Aktiv: {activeDogs}\n" +
                              $"Flächensuche: {flaechensuche}\n" +
                              $"Trümmersuche: {truemmersuche}\n" +
                              $"Mantrailing: {mantrailer}";
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating statistics", ex);
                PersonalStatsText = "Fehler beim Laden der Statistiken";
                DogStatsText = "Fehler beim Laden der Statistiken";
            }
        }

        #endregion
    }
}
