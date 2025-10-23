using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    // Helper class to provide consistent ComboBox data structure
    public class ComboBoxDogItem
    {
        public string Name { get; set; } = string.Empty;
        public string Rasse { get; set; } = string.Empty;
        public bool IsManualEntry { get; set; }
        public DogEntry? DogEntry { get; set; }

        public ComboBoxDogItem(string name, bool isManualEntry = true)
        {
            Name = name;
            Rasse = string.Empty;
            IsManualEntry = isManualEntry;
            DogEntry = null;
        }

        public ComboBoxDogItem(DogEntry dogEntry)
        {
            Name = dogEntry.Name;
            Rasse = dogEntry.Rasse;
            IsManualEntry = false;
            DogEntry = dogEntry;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class ComboBoxPersonalItem
    {
        public string FullName { get; set; } = string.Empty;
        public bool IsManualEntry { get; set; }
        public PersonalEntry? PersonalEntry { get; set; }

        public ComboBoxPersonalItem(string fullName, bool isManualEntry = true)
        {
            FullName = fullName;
            IsManualEntry = isManualEntry;
            PersonalEntry = null;
        }

        public ComboBoxPersonalItem(PersonalEntry personalEntry)
        {
            FullName = personalEntry.FullName;
            IsManualEntry = false;
            PersonalEntry = personalEntry;
        }

        public override string ToString()
        {
            return FullName;
        }
    }

    public class TeamInputViewModel : BaseViewModel
    {
        private readonly MasterDataService _masterDataService;
        
        // Properties für Form-Daten
        private string _hundName = string.Empty;
        private string _hundefuehrer = string.Empty;
        private string _helfer = string.Empty;
        private string _suchgebiet = string.Empty;
        private ComboBoxDogItem? _selectedDog;
        private ComboBoxPersonalItem? _selectedHundefuehrer;
        private ComboBoxPersonalItem? _selectedHelfer;
        private string _selectedSpecializationText = "Wird nach 'Weiter' ausgewählt";
        private System.Windows.Media.Brush? _specializationTextColor;

        // Collections für ComboBoxes - Updated to use helper classes
        private ObservableCollection<ComboBoxDogItem> _dogsList = new();
        private ObservableCollection<ComboBoxPersonalItem> _personalList = new();
        
        // Suchgebiete für Dropdown (nur als Strings)
        private ObservableCollection<string> _suchgebiete = new();
        public ObservableCollection<string> Suchgebiete
        {
            get => _suchgebiete;
            set => SetProperty(ref _suchgebiete, value);
        }

        // Properties für Validierung
        private bool _isFormValid = false;
        private bool _canCreateTeam = false;

        // Missing properties for XAML compatibility
        private string _selectedTeamTypesDisplay = "Keine Auswahl";
        private bool _hasDogInfo = false;
        private string _dogInfo = string.Empty;
        private bool _hasPreselectedTeamTypes = false;
        private bool _isLoadingMasterData = false;
        private string _statusMessage = string.Empty;

        public string HundName
        {
            get => _hundName;
            set
            {
                _hundName = value;
                OnPropertyChanged();
                UpdateTeamNamePreview();
                ValidateForm();
            }
        }

        public string Hundefuehrer
        {
            get => _hundefuehrer;
            set
            {
                _hundefuehrer = value;
                OnPropertyChanged();
            }
        }

        public string Helfer
        {
            get => _helfer;
            set
            {
                _helfer = value;
                OnPropertyChanged();
            }
        }

        public string Suchgebiet
        {
            get => _suchgebiet;
            set
            {
                _suchgebiet = value;
                OnPropertyChanged();
            }
        }

        public ComboBoxDogItem? SelectedDog
        {
            get => _selectedDog;
            set
            {
                _selectedDog = value;
                OnPropertyChanged();
                HandleDogSelection(value?.DogEntry);
            }
        }

        public ComboBoxPersonalItem? SelectedHundefuehrer
        {
            get => _selectedHundefuehrer;
            set
            {
                _selectedHundefuehrer = value;
                OnPropertyChanged();
                if (value != null)
                {
                    Hundefuehrer = value.FullName;
                }
            }
        }

        public ComboBoxPersonalItem? SelectedHelfer
        {
            get => _selectedHelfer;
            set
            {
                _selectedHelfer = value;
                OnPropertyChanged();
                if (value != null)
                {
                    Helfer = value.FullName;
                }
            }
        }

        public string SelectedSpecializationText
        {
            get => _selectedSpecializationText;
            set
            {
                _selectedSpecializationText = value;
                OnPropertyChanged();
            }
        }

        public System.Windows.Media.Brush? SpecializationTextColor
        {
            get => _specializationTextColor;
            set
            {
                _specializationTextColor = value;
                OnPropertyChanged();
            }
        }

        // Updated to use helper classes
        public ObservableCollection<ComboBoxDogItem> DogsList
        {
            get => _dogsList;
            set
            {
                _dogsList = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ComboBoxPersonalItem> PersonalList
        {
            get => _personalList;
            set
            {
                _personalList = value;
                OnPropertyChanged();
            }
        }

        public bool IsFormValid
        {
            get => _isFormValid;
            set
            {
                _isFormValid = value;
                OnPropertyChanged();
                ValidateCanCreateTeam();
            }
        }

        // NEW: Added missing properties for XAML compatibility
        public bool CanCreateTeam
        {
            get => _canCreateTeam;
            set
            {
                _canCreateTeam = value;
                OnPropertyChanged();
                ((RelayCommand)CreateTeamCommand).RaiseCanExecuteChanged();
            }
        }

        public string SelectedTeamTypesDisplay
        {
            get => _selectedTeamTypesDisplay;
            set => SetProperty(ref _selectedTeamTypesDisplay, value);
        }

        public bool HasDogInfo
        {
            get => _hasDogInfo;
            set => SetProperty(ref _hasDogInfo, value);
        }

        public string DogInfo
        {
            get => _dogInfo;
            set => SetProperty(ref _dogInfo, value);
        }

        // NEW: Property für Button-Text Bindung
        public bool HasPreselectedTeamTypes
        {
            get => _hasPreselectedTeamTypes;
            set => SetProperty(ref _hasPreselectedTeamTypes, value);
        }

        public bool IsLoadingMasterData
        {
            get => _isLoadingMasterData;
            set => SetProperty(ref _isLoadingMasterData, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // Computed Properties
        public string TeamNamePreview => string.IsNullOrWhiteSpace(HundName) 
            ? "Teamname: Team [Hundename]" 
            : $"Teamname: Team {HundName}";

        private string _teamName = "Team [Hundename]";
        public string TeamName 
        { 
            get => string.IsNullOrWhiteSpace(HundName) 
                ? "Team [Hundename]" 
                : $"Team {HundName}";
            set 
            { 
                SetProperty(ref _teamName, value);
                OnPropertyChanged(nameof(TeamNamePreview));
            }
        }

        // Public Properties für Result
        public MultipleTeamTypes? PreselectedTeamTypes { get; private set; }

        // Commands - Enhanced with parameter support
        public ICommand SelectTeamTypeCommand { get; }
        public ICommand CreateTeamCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RefreshMasterDataCommand { get; }
        public ICommand SelectDogCommand { get; }
        public ICommand ClearDogSelectionCommand { get; }

        // Events für View-Kommunikation
        public event EventHandler? RequestClose;
        public event EventHandler? ShowTeamTypeSelection;

        /// <summary>
        /// Konstruktor für das TeamInputViewModel ohne Suchgebiete
        /// </summary>
        public TeamInputViewModel()
        {
            _masterDataService = MasterDataService.Instance;
            
            // Initialize commands with proper parameter support
            SelectTeamTypeCommand = new RelayCommand(ExecuteSelectTeamType);
            CreateTeamCommand = new RelayCommand(ExecuteCreateTeam, CanExecuteCreateTeam);
            CancelCommand = new RelayCommand(ExecuteCancel);
            RefreshMasterDataCommand = new RelayCommand(ExecuteRefreshMasterData);
            SelectDogCommand = new RelayCommand<DogEntry>(ExecuteSelectDog);
            ClearDogSelectionCommand = new RelayCommand(ExecuteClearDogSelection, () => SelectedDog != null);

            // Initialize master data service and load data
            _ = InitializeMasterDataAsync();
            
            LoggingService.Instance?.LogInfo("TeamInputViewModel initialized without search areas");
        }

        private async Task InitializeMasterDataAsync()
        {
            try
            {
                // Ensure master data is loaded
                await _masterDataService.LoadDataAsync();
                
                // Load data into UI collections
                LoadMasterDataToUI();
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error initializing master data service in TeamInputViewModel", ex);
            }
        }

        private void LoadMasterDataToUI()
        {
            try
            {
                // Hunde laden - using helper class
                var dogs = _masterDataService.GetActiveDogs();
                DogsList.Clear();
                DogsList.Add(new ComboBoxDogItem("(Manuell eingeben)", true));
                
                foreach (var dog in dogs)
                {
                    DogsList.Add(new ComboBoxDogItem(dog));
                }

                // Personal für beide ComboBoxes laden - using helper class
                var personal = _masterDataService.GetActivePersonal();
                PersonalList.Clear();
                PersonalList.Add(new ComboBoxPersonalItem("(Manuell eingeben)", true));
                
                foreach (var person in personal)
                {
                    PersonalList.Add(new ComboBoxPersonalItem(person));
                }

                LoggingService.Instance?.LogInfo($"Loaded master data: {dogs.Count} dogs, {personal.Count} personnel");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error loading master data in TeamInputViewModel", ex);
            }
        }

        private void HandleDogSelection(DogEntry? selectedDog)
        {
            try
            {
                if (selectedDog != null)
                {
                    HundName = selectedDog.Name;
                    
                    // Konvertiere Hunde-Spezialisierungen zu TeamTypes
                    var convertedTeamTypes = ConvertDogSpecializationsToTeamTypes(selectedDog.Specializations);
                    
                    if (convertedTeamTypes != null && convertedTeamTypes.SelectedTypes.Count > 0)
                    {
                        // Spezialisierungen vorhanden - direkte Zuweisung ohne TeamTypeSelectionWindow
                        PreselectedTeamTypes = convertedTeamTypes;
                        SelectedTeamTypesDisplay = convertedTeamTypes.DisplayName;
                        SelectedSpecializationText = $"Automatisch übernommen: {convertedTeamTypes.DisplayName}";
                        SpecializationTextColor = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("Success");
                        HasPreselectedTeamTypes = true; // Button zeigt "Team erstellen"
                        
                        LoggingService.Instance?.LogInfo($"Dog '{selectedDog.Name}' selected with automatic team types: {convertedTeamTypes.DisplayName}");
                    }
                    else
                    {
                        // Keine Spezialisierungen - manuell auswählen erforderlich  
                        PreselectedTeamTypes = null;
                        SelectedTeamTypesDisplay = "Keine Spezialisierung im Stammdatensystem hinterlegt";
                        SelectedSpecializationText = "Bitte Team-Typ manuell auswählen";
                        SpecializationTextColor = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("Warning");
                        HasPreselectedTeamTypes = false; // Button zeigt "Weiter zur Spezialisierung"
                        
                        LoggingService.Instance?.LogInfo($"Dog '{selectedDog.Name}' selected without specializations - manual selection required");
                    }
                    
                    // Dog info anzeigen
                    var rasse = !string.IsNullOrEmpty(selectedDog.Rasse) ? selectedDog.Rasse : "Unbekannt";
                    var spezialisierungInfo = selectedDog.Specializations != DogSpecialization.None 
                        ? selectedDog.SpecializationsDisplay 
                        : "Keine Spezialisierung hinterlegt";
                    DogInfo = $"Rasse: {rasse}, Alter: {selectedDog.Alter} Jahre, Spezialisierung: {spezialisierungInfo}";
                    HasDogInfo = true;

                    // Auto-Ausfüllen des Hundeführers falls zugewiesen
                    if (!string.IsNullOrEmpty(selectedDog.HundefuehrerId))
                    {
                        var hundefuehrer = _masterDataService.GetPersonalById(selectedDog.HundefuehrerId);
                        if (hundefuehrer != null)
                        {
                            // Suche den Hundeführer in der Liste
                            var matchingHandler = PersonalList
                                .FirstOrDefault(p => p.PersonalEntry?.Id == hundefuehrer.Id);
                            
                            if (matchingHandler != null)
                            {
                                SelectedHundefuehrer = matchingHandler;
                                LoggingService.Instance?.LogInfo($"Auto-filled handler: {hundefuehrer.FullName}");
                            }
                        }
                    }
                }
                else
                {
                    // Manuelle Eingabe oder keine Auswahl - keine Spezialisierungen vorausgewählt
                    PreselectedTeamTypes = null;
                    SelectedTeamTypesDisplay = "Wird im nächsten Schritt ausgewählt";
                    SelectedSpecializationText = "Wird im nächsten Schritt ausgewählt";
                    SpecializationTextColor = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("OnSurfaceVariant");
                    HasPreselectedTeamTypes = false; // Button zeigt "Weiter zur Spezialisierung"
                    HasDogInfo = false;
                    DogInfo = string.Empty;
                }

                // Validierung nach Hunde-Auswahl aktualisieren
                ValidateCanCreateTeam();
                
                // Update clear command state
                ((RelayCommand)ClearDogSelectionCommand).RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error handling dog selection in ViewModel", ex);
            }
        }

        private MultipleTeamTypes? ConvertDogSpecializationsToTeamTypes(DogSpecialization specializations)
        {
            // Ignoriere "None" - keine automatische Zuweisung zu "Allgemein"
            if (specializations == DogSpecialization.None)
                return null;

            var teamTypes = new MultipleTeamTypes();

            if (specializations.HasFlag(DogSpecialization.Flaechensuche))
                teamTypes.SelectedTypes.Add(TeamType.Flaechensuchhund);

            if (specializations.HasFlag(DogSpecialization.Truemmersuche))
                teamTypes.SelectedTypes.Add(TeamType.Truemmersuchhund);

            if (specializations.HasFlag(DogSpecialization.Mantrailing))
                teamTypes.SelectedTypes.Add(TeamType.Mantrailer);

            if (specializations.HasFlag(DogSpecialization.Wasserortung))
                teamTypes.SelectedTypes.Add(TeamType.Wasserrettungshund);

            if (specializations.HasFlag(DogSpecialization.Lawinensuche))
                teamTypes.SelectedTypes.Add(TeamType.Lawinensuchhund);

            // Geländesuche und Leichensuche werden NICHT automatisch zu "Allgemein" konvertiert
            // Diese müssen manuell ausgewählt werden, da sie spezifische Team-Typen haben sollten

            return teamTypes.SelectedTypes.Count > 0 ? teamTypes : null;
        }

        private void UpdateTeamNamePreview()
        {
            OnPropertyChanged(nameof(TeamNamePreview));
        }

        private void ValidateForm()
        {
            IsFormValid = !string.IsNullOrWhiteSpace(HundName);
        }

        private void ValidateCanCreateTeam()
        {
            // Team kann erstellt werden wenn:
            // 1. Form ist valide (HundName ist gesetzt)
            // 2. ENTWEDER PreselectedTeamTypes sind gesetzt (automatisch aus Stammdaten)
            //    ODER es ist manuelle Eingabe (dann wird später TeamTypeSelection geöffnet)
            bool hasValidDogName = !string.IsNullOrWhiteSpace(HundName);
            bool hasTeamTypesOrManualEntry = PreselectedTeamTypes != null || SelectedDog?.IsManualEntry == true || SelectedDog == null;
            
            CanCreateTeam = hasValidDogName && hasTeamTypesOrManualEntry;
            
            LoggingService.Instance?.LogInfo($"Team creation validation: DogName={hasValidDogName}, TeamTypes/Manual={hasTeamTypesOrManualEntry}, CanCreate={CanCreateTeam}");
        }

        // Command implementations with enhanced functionality
        private void ExecuteSelectTeamType()
        {
            try
            {
                LoggingService.Instance?.LogInfo("Team type selection requested");
                
                // Event für View - öffne TeamTypeSelection
                ShowTeamTypeSelection?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error executing SelectTeamType command", ex);
            }
        }

        private bool CanExecuteCreateTeam()
        {
            return CanCreateTeam;
        }

        private void ExecuteCreateTeam()
        {
            try
            {
                // Wenn keine PreselectedTeamTypes vorhanden sind, öffne TeamTypeSelection
                if (PreselectedTeamTypes == null)
                {
                    LoggingService.Instance?.LogInfo("No preselected team types - opening TeamTypeSelection");
                    ShowTeamTypeSelection?.Invoke(this, EventArgs.Empty);
                    return;
                }
                
                LoggingService.Instance?.LogInfo($"Team creation completed - Team name: {TeamName}, " +
                    $"Suchgebiet: {Suchgebiet}, Selected types: {PreselectedTeamTypes?.DisplayName ?? "None"}");
                
                // Event für View - schließe Window mit DialogResult = true
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error executing CreateTeam command", ex);
            }
        }

        private void ExecuteCancel()
        {
            LoggingService.Instance?.LogInfo("Team input cancelled by user");
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteRefreshMasterData()
        {
            try
            {
                LoggingService.Instance?.LogInfo("Refreshing master data...");
                LoadMasterDataToUI();
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error refreshing master data", ex);
            }
        }

        private void ExecuteSelectDog(DogEntry? dog)
        {
            try
            {
                if (dog != null)
                {
                    // Find the ComboBoxDogItem that corresponds to this DogEntry
                    var comboBoxItem = DogsList.FirstOrDefault(d => d.DogEntry?.Id == dog.Id);
                    if (comboBoxItem != null)
                    {
                        SelectedDog = comboBoxItem;
                        LoggingService.Instance?.LogInfo($"Dog selected via command parameter: {dog.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error executing SelectDog command", ex);
            }
        }

        private void ExecuteClearDogSelection()
        {
            try
            {
                SelectedDog = null;
                HundName = string.Empty;
                LoggingService.Instance?.LogInfo("Dog selection cleared");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error clearing dog selection", ex);
            }
        }

        public void SetSelectedTeamTypes(MultipleTeamTypes? selectedTypes)
        {
            PreselectedTeamTypes = selectedTypes;
            if (selectedTypes != null)
            {
                SelectedTeamTypesDisplay = selectedTypes.DisplayName;
                HasPreselectedTeamTypes = true; // Button zeigt "Team erstellen"
                ValidateCanCreateTeam();
            }
            else
            {
                HasPreselectedTeamTypes = false; // Button zeigt "Weiter zur Spezialisierung"
            }
        }
    }
}
