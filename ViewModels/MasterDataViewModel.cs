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
        private bool _hasUnsavedChanges = false;

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

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
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
                
                LoggingService.Instance.LogInfo("=== STARTING LOAD DATA ===");
                LoggingService.Instance.LogInfo($"MasterDataService PersonalList count BEFORE load: {_masterDataService.PersonalList.Count}");
                LoggingService.Instance.LogInfo($"MasterDataService DogList count BEFORE load: {_masterDataService.DogList.Count}");
                
                await _masterDataService.LoadDataAsync();
                
                LoggingService.Instance.LogInfo($"MasterDataService PersonalList count AFTER load: {_masterDataService.PersonalList.Count}");
                LoggingService.Instance.LogInfo($"MasterDataService DogList count AFTER load: {_masterDataService.DogList.Count}");
                
                LoadData();
                
                LoggingService.Instance.LogInfo($"ViewModel PersonalList count after LoadData: {PersonalList.Count}");
                LoggingService.Instance.LogInfo($"ViewModel DogList count after LoadData: {DogList.Count}");
                
                UpdateStatistics();
                
                StatusText = "Stammdaten erfolgreich geladen";
                LoggingService.Instance.LogInfo("=== LOAD DATA COMPLETED ===");
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
                LoggingService.Instance.LogInfo("Starting ExecuteAddPersonal");
                
                var editWindow = new Views.PersonalEditWindow();
                LoggingService.Instance.LogInfo($"Created PersonalEditWindow, showing dialog...");
                
                var result = editWindow.ShowDialog();
                LoggingService.Instance.LogInfo($"PersonalEditWindow closed with result: {result}");
                
                if (result == true)
                {
                    var personalEntry = editWindow.PersonalEntry;
                    LoggingService.Instance.LogInfo($"Retrieved PersonalEntry from window: ID={personalEntry.Id}, Name={personalEntry.FullName}, Skills={personalEntry.Skills}");
                    
                    LoggingService.Instance.LogInfo("Calling MasterDataService.AddPersonal...");
                    _masterDataService.AddPersonal(personalEntry);
                    LoggingService.Instance.LogInfo("MasterDataService.AddPersonal completed");
                    
                    LoggingService.Instance.LogInfo("Calling LoadData to refresh UI...");
                    LoadData();
                    LoggingService.Instance.LogInfo("LoadData completed");
                    
                    LoggingService.Instance.LogInfo("Calling UpdateStatistics...");
                    UpdateStatistics();
                    LoggingService.Instance.LogInfo("UpdateStatistics completed");
                    
                    StatusText = $"Personal '{personalEntry.FullName}' hinzugefügt";
                    LoggingService.Instance.LogInfo($"Personal added successfully via MVVM: {personalEntry.FullName}");
                }
                else
                {
                    LoggingService.Instance.LogInfo("PersonalEditWindow was cancelled by user");
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
                    LoggingService.Instance.LogInfo($"Starting ExecuteEditPersonal for: {SelectedPersonal.FullName}");
                    
                    var editWindow = new Views.PersonalEditWindow(SelectedPersonal);
                    LoggingService.Instance.LogInfo($"Created PersonalEditWindow for editing, showing dialog...");
                    
                    var result = editWindow.ShowDialog();
                    LoggingService.Instance.LogInfo($"PersonalEditWindow closed with result: {result}");
                    
                    if (result == true)
                    {
                        var personalEntry = editWindow.PersonalEntry;
                        LoggingService.Instance.LogInfo($"Retrieved updated PersonalEntry: ID={personalEntry.Id}, Name={personalEntry.FullName}, Skills={personalEntry.Skills}");
                        
                        LoggingService.Instance.LogInfo("Calling MasterDataService.UpdatePersonal...");
                        _masterDataService.UpdatePersonal(personalEntry);
                        LoggingService.Instance.LogInfo("MasterDataService.UpdatePersonal completed");
                        
                        LoggingService.Instance.LogInfo("Calling LoadData to refresh UI...");
                        LoadData();
                        LoggingService.Instance.LogInfo("LoadData completed");
                        
                        LoggingService.Instance.LogInfo("Calling UpdateStatistics...");
                        UpdateStatistics();
                        LoggingService.Instance.LogInfo("UpdateStatistics completed");
                        
                        StatusText = $"Personal '{personalEntry.FullName}' aktualisiert";
                        LoggingService.Instance.LogInfo($"Personal updated successfully via MVVM: {personalEntry.FullName}");
                    }
                    else
                    {
                        LoggingService.Instance.LogInfo("PersonalEditWindow editing was cancelled by user");
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
                    LoggingService.Instance.LogInfo($"Starting ExecuteDeletePersonal for: {SelectedPersonal.FullName}");
                    
                    var result = MessageBox.Show(
                        $"Möchten Sie '{SelectedPersonal.FullName}' wirklich löschen?",
                        "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var name = SelectedPersonal.FullName;
                        var id = SelectedPersonal.Id;
                        
                        LoggingService.Instance.LogInfo($"Calling MasterDataService.RemovePersonal for ID: {id}");
                        _masterDataService.RemovePersonal(id);
                        LoggingService.Instance.LogInfo("MasterDataService.RemovePersonal completed");
                        
                        LoggingService.Instance.LogInfo("Calling LoadData to refresh UI...");
                        LoadData();
                        LoggingService.Instance.LogInfo("LoadData completed");
                        
                        LoggingService.Instance.LogInfo("Calling UpdateStatistics...");
                        UpdateStatistics();
                        LoggingService.Instance.LogInfo("UpdateStatistics completed");
                        
                        StatusText = $"Personal '{name}' gelöscht";
                        LoggingService.Instance.LogInfo($"Personal deleted successfully via MVVM: {name}");
                    }
                    else
                    {
                        LoggingService.Instance.LogInfo("Personal deletion cancelled by user");
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
                LoggingService.Instance.LogInfo("Starting ExecuteAddDog");
                
                var editWindow = new Views.DogEditWindow();
                LoggingService.Instance.LogInfo($"Created DogEditWindow, showing dialog...");
                
                var result = editWindow.ShowDialog();
                LoggingService.Instance.LogInfo($"DogEditWindow closed with result: {result}");
                
                if (result == true)
                {
                    var dogEntry = editWindow.DogEntry;
                    LoggingService.Instance.LogInfo($"Retrieved DogEntry from window: ID={dogEntry.Id}, Name={dogEntry.Name}, Specializations={dogEntry.Specializations}");
                    
                    LoggingService.Instance.LogInfo("Calling MasterDataService.AddDog...");
                    _masterDataService.AddDog(dogEntry);
                    LoggingService.Instance.LogInfo("MasterDataService.AddDog completed");
                    
                    LoggingService.Instance.LogInfo("Calling LoadData to refresh UI...");
                    LoadData();
                    LoggingService.Instance.LogInfo("LoadData completed");
                    
                    LoggingService.Instance.LogInfo("Calling UpdateStatistics...");
                    UpdateStatistics();
                    LoggingService.Instance.LogInfo("UpdateStatistics completed");
                    
                    StatusText = $"Hund '{dogEntry.Name}' hinzugefügt";
                    LoggingService.Instance.LogInfo($"Dog added successfully via MVVM: {dogEntry.Name}");
                }
                else
                {
                    LoggingService.Instance.LogInfo("DogEditWindow was cancelled by user");
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
                    LoggingService.Instance.LogInfo($"Starting ExecuteEditDog for: {SelectedDog.Name}");
                    
                    var editWindow = new Views.DogEditWindow(SelectedDog);
                    LoggingService.Instance.LogInfo($"Created DogEditWindow for editing, showing dialog...");
                    
                    var result = editWindow.ShowDialog();
                    LoggingService.Instance.LogInfo($"DogEditWindow closed with result: {result}");
                    
                    if (result == true)
                    {
                        var dogEntry = editWindow.DogEntry;
                        LoggingService.Instance.LogInfo($"Retrieved updated DogEntry: ID={dogEntry.Id}, Name={dogEntry.Name}, Specializations={dogEntry.Specializations}");
                        
                        LoggingService.Instance.LogInfo("Calling MasterDataService.UpdateDog...");
                        _masterDataService.UpdateDog(dogEntry);
                        LoggingService.Instance.LogInfo("MasterDataService.UpdateDog completed");
                        
                        LoggingService.Instance.LogInfo("Calling LoadData to refresh UI...");
                        LoadData();
                        LoggingService.Instance.LogInfo("LoadData completed");
                        
                        LoggingService.Instance.LogInfo("Calling UpdateStatistics...");
                        UpdateStatistics();
                        LoggingService.Instance.LogInfo("UpdateStatistics completed");
                        
                        StatusText = $"Hund '{dogEntry.Name}' aktualisiert";
                        LoggingService.Instance.LogInfo($"Dog updated successfully via MVVM: {dogEntry.Name}");
                    }
                    else
                    {
                        LoggingService.Instance.LogInfo("DogEditWindow editing was cancelled by user");
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
                    LoggingService.Instance.LogInfo($"Starting ExecuteDeleteDog for: {SelectedDog.Name}");
                    
                    var result = MessageBox.Show(
                        $"Möchten Sie '{SelectedDog.Name}' wirklich löschen?",
                        "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var name = SelectedDog.Name;
                        var id = SelectedDog.Id;
                        
                        LoggingService.Instance.LogInfo($"Calling MasterDataService.RemoveDog for ID: {id}");
                        _masterDataService.RemoveDog(id);
                        LoggingService.Instance.LogInfo("MasterDataService.RemoveDog completed");
                        
                        LoggingService.Instance.LogInfo("Calling LoadData to refresh UI...");
                        LoadData();
                        LoggingService.Instance.LogInfo("LoadData completed");
                        
                        LoggingService.Instance.LogInfo("Calling UpdateStatistics...");
                        UpdateStatistics();
                        LoggingService.Instance.LogInfo("UpdateStatistics completed");
                        
                        StatusText = $"Hund '{name}' gelöscht";
                        LoggingService.Instance.LogInfo($"Dog deleted successfully via MVVM: {name}");
                    }
                    else
                    {
                        LoggingService.Instance.LogInfo("Dog deletion cancelled by user");
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
            try
            {
                LoggingService.Instance.LogInfo("Starting LoadData in MasterDataViewModel");
                LoggingService.Instance.LogInfo($"MasterDataService PersonalList count: {_masterDataService.PersonalList.Count}");
                LoggingService.Instance.LogInfo($"MasterDataService DogList count: {_masterDataService.DogList.Count}");
                
                // ObservableCollections aktualisieren
                LoggingService.Instance.LogInfo("Clearing and reloading PersonalList in ViewModel...");
                PersonalList.Clear();
                foreach (var personal in _masterDataService.PersonalList)
                {
                    PersonalList.Add(personal);
                }
                LoggingService.Instance.LogInfo($"PersonalList in ViewModel updated. New count: {PersonalList.Count}");

                LoggingService.Instance.LogInfo("Clearing and reloading DogList in ViewModel...");
                DogList.Clear();
                foreach (var dog in _masterDataService.DogList)
                {
                    DogList.Add(dog);
                }
                LoggingService.Instance.LogInfo($"DogList in ViewModel updated. New count: {DogList.Count}");

                UpdateCounts();
                LoggingService.Instance.LogInfo("LoadData completed successfully");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error in LoadData", ex);
            }
        }

        private void UpdateCounts()
        {
            var oldPersonalCount = PersonalCountText;
            var oldDogCount = DogCountText;
            
            PersonalCountText = $"Personal: {PersonalList.Count}";
            DogCountText = $"Hunde: {DogList.Count}";
            
            LoggingService.Instance.LogInfo($"UpdateCounts: Personal {oldPersonalCount} -> {PersonalCountText}");
            LoggingService.Instance.LogInfo($"UpdateCounts: Dogs {oldDogCount} -> {DogCountText}");
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
