using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    public class TeamInputViewModel : BaseViewModel
    {
        private readonly MasterDataService _masterDataService;
        
        // Properties für Form-Daten
        private string _hundName = string.Empty;
        private string _hundefuehrer = string.Empty;
        private string _helfer = string.Empty;
        private string _suchgebiet = string.Empty;
        private DogEntry? _selectedDog;
        private PersonalEntry? _selectedHundefuehrer;
        private PersonalEntry? _selectedHelfer;
        private string _selectedSpecializationText = "Wird nach 'Weiter' ausgewählt";
        private System.Windows.Media.Brush? _specializationTextColor;

        // Collections für ComboBoxes
        private ObservableCollection<object> _hundeList = new();
        private ObservableCollection<object> _hundefuehrerList = new();
        private ObservableCollection<object> _helferList = new();

        // Properties für Validierung
        private bool _isFormValid = false;

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

        public DogEntry? SelectedDog
        {
            get => _selectedDog;
            set
            {
                _selectedDog = value;
                OnPropertyChanged();
                HandleDogSelection(value);
            }
        }

        public PersonalEntry? SelectedHundefuehrer
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

        public PersonalEntry? SelectedHelfer
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

        public ObservableCollection<object> HundeList
        {
            get => _hundeList;
            set
            {
                _hundeList = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<object> HundefuehrerList
        {
            get => _hundefuehrerList;
            set
            {
                _hundefuehrerList = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<object> HelferList
        {
            get => _helferList;
            set
            {
                _helferList = value;
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
                ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ClearCommand).RaiseCanExecuteChanged();
            }
        }

        // Computed Properties
        public string TeamNamePreview => string.IsNullOrWhiteSpace(HundName) 
            ? "Teamname: Team [Hundename]" 
            : $"Teamname: Team {HundName}";

        public string TeamName => string.IsNullOrWhiteSpace(HundName) 
            ? "Team [Hundename]" 
            : $"Team {HundName}";

        // Public Properties für Result
        public MultipleTeamTypes? PreselectedTeamTypes { get; private set; }

        // Commands - Enhanced with parameter support
        public ICommand NextCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand RefreshMasterDataCommand { get; }
        public ICommand SelectDogCommand { get; }
        public ICommand ClearDogSelectionCommand { get; }

        // Events für View-Kommunikation
        public event EventHandler? RequestClose;
        public event EventHandler? ShowTeamTypeSelection;

        public TeamInputViewModel()
        {
            _masterDataService = MasterDataService.Instance;
            
            // Initialize commands with proper parameter support
            NextCommand = new RelayCommand(ExecuteNext, CanExecuteNext);
            CancelCommand = new RelayCommand(ExecuteCancel);
            ClearCommand = new RelayCommand(ExecuteClear, CanExecuteClear);
            RefreshMasterDataCommand = new RelayCommand(ExecuteRefreshMasterData);
            SelectDogCommand = new RelayCommand<DogEntry>(ExecuteSelectDog);
            ClearDogSelectionCommand = new RelayCommand(ExecuteClearDogSelection, () => SelectedDog != null);

            // Lade Stammdaten
            LoadMasterDataAsync();
            
            LoggingService.Instance?.LogInfo("TeamInputViewModel initialized with enhanced MVVM pattern and command parameters");
        }

        private async void LoadMasterDataAsync()
        {
            try
            {
                // Hunde laden
                var dogs = _masterDataService.GetActiveDogs();
                HundeList.Clear();
                HundeList.Add(new { Name = "(Manuell eingeben)" });
                
                foreach (var dog in dogs)
                {
                    HundeList.Add(dog);
                }

                // Hundeführer laden (mit Hundeführer-Skill)
                var hundefuehrer = _masterDataService.GetPersonalBySkill(PersonalSkills.Hundefuehrer);
                HundefuehrerList.Clear();
                HundefuehrerList.Add(new { FullName = "(Manuell eingeben)" });
                
                foreach (var person in hundefuehrer)
                {
                    HundefuehrerList.Add(person);
                }

                // Helfer laden (mit Helfer-Skill)
                var helfer = _masterDataService.GetPersonalBySkill(PersonalSkills.Helfer);
                HelferList.Clear();
                HelferList.Add(new { FullName = "(Leer lassen / Manuell)" });
                
                foreach (var person in helfer)
                {
                    HelferList.Add(person);
                }

                LoggingService.Instance?.LogInfo($"Loaded master data: {dogs.Count} dogs, {hundefuehrer.Count} handlers, {helfer.Count} helpers");
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
                    PreselectedTeamTypes = ConvertDogSpecializationsToTeamTypes(selectedDog.Specializations);
                    
                    // Spezialisierungen anzeigen
                    if (PreselectedTeamTypes != null && PreselectedTeamTypes.SelectedTypes.Count > 0)
                    {
                        SelectedSpecializationText = $"Vorauswahl: {PreselectedTeamTypes.DisplayName}";
                        SpecializationTextColor = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("Success");
                        
                        LoggingService.Instance?.LogInfo($"Dog '{selectedDog.Name}' selected with specializations: {PreselectedTeamTypes.DisplayName}");
                    }
                    else
                    {
                        SelectedSpecializationText = "Wird im nächsten Schritt ausgewählt";
                        SpecializationTextColor = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("OnSurfaceVariant");
                    }

                    // Auto-Ausfüllen des Hundeführers falls zugewiesen
                    if (!string.IsNullOrEmpty(selectedDog.HundefuehrerId))
                    {
                        var hundefuehrer = _masterDataService.GetPersonalById(selectedDog.HundefuehrerId);
                        if (hundefuehrer != null)
                        {
                            // Suche den Hundeführer in der Liste
                            var matchingHandler = HundefuehrerList.OfType<PersonalEntry>()
                                .FirstOrDefault(p => p.Id == hundefuehrer.Id);
                            
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
                    // Manuelle Eingabe - keine Spezialisierungen vorausgewählt
                    PreselectedTeamTypes = null;
                    SelectedSpecializationText = "Wird im nächsten Schritt ausgewählt";
                    SpecializationTextColor = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("OnSurfaceVariant");
                }

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

            if (specializations.HasFlag(DogSpecialization.Gelaendesuche) || 
                specializations.HasFlag(DogSpecialization.Leichensuche))
            {
                if (teamTypes.SelectedTypes.Count == 0)
                {
                    teamTypes.SelectedTypes.Add(TeamType.Allgemein);
                }
            }

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

        // Command implementations with enhanced functionality
        private bool CanExecuteNext()
        {
            return IsFormValid;
        }

        private void ExecuteNext()
        {
            try
            {
                LoggingService.Instance?.LogInfo($"Team input completed - Team name: {TeamName}, " +
                    $"Suchgebiet: {Suchgebiet}, Preselected types: {PreselectedTeamTypes?.DisplayName ?? "None"}");
                
                // Event für View - öffne TeamTypeSelection
                ShowTeamTypeSelection?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error executing Next command", ex);
            }
        }

        private void ExecuteCancel()
        {
            LoggingService.Instance?.LogInfo("Team input cancelled by user");
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private bool CanExecuteClear()
        {
            return !string.IsNullOrWhiteSpace(HundName) || 
                   !string.IsNullOrWhiteSpace(Hundefuehrer) || 
                   !string.IsNullOrWhiteSpace(Helfer) || 
                   !string.IsNullOrWhiteSpace(Suchgebiet);
        }

        private void ExecuteClear()
        {
            try
            {
                HundName = string.Empty;
                Hundefuehrer = string.Empty;
                Helfer = string.Empty;
                Suchgebiet = string.Empty;
                SelectedDog = null;
                SelectedHundefuehrer = null;
                SelectedHelfer = null;
                PreselectedTeamTypes = null;
                SelectedSpecializationText = "Wird nach 'Weiter' ausgewählt";
                SpecializationTextColor = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("OnSurfaceVariant");
                
                LoggingService.Instance?.LogInfo("Team input form cleared");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error clearing form", ex);
            }
        }

        private void ExecuteRefreshMasterData()
        {
            try
            {
                LoggingService.Instance?.LogInfo("Refreshing master data...");
                LoadMasterDataAsync();
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
                    SelectedDog = dog;
                    LoggingService.Instance?.LogInfo($"Dog selected via command parameter: {dog.Name}");
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
        }
    }
}
