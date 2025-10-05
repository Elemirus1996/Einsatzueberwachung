using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    public class DogEditViewModel : BaseViewModel
    {
        private readonly MasterDataService _masterDataService;
        private readonly bool _isEditMode;
        private DogEntry _dogEntry;

        // Properties for UI binding
        private string _name = string.Empty;
        private string _rasse = string.Empty;
        private string _alter = "0";
        private string _notizen = string.Empty;
        private bool _isActive = true;
        private PersonalEntry? _selectedHundefuehrer;

        // Specialization flags
        private bool _flaechensuche;
        private bool _truemmersuche;
        private bool _mantrailing;
        private bool _wasserortung;
        private bool _lawinensuche;
        private bool _gelaendesuche;
        private bool _leichensuche;

        // Collections
        public ObservableCollection<PersonalEntry> HundefuehrerList { get; }

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // Properties
        public string WindowTitle => _isEditMode ? "Hund bearbeiten" : "Neuer Hund";

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Rasse
        {
            get => _rasse;
            set => SetProperty(ref _rasse, value);
        }

        public string Alter
        {
            get => _alter;
            set
            {
                // Validate numeric input
                if (IsValidNumericInput(value))
                {
                    SetProperty(ref _alter, value);
                }
            }
        }

        public string Notizen
        {
            get => _notizen;
            set => SetProperty(ref _notizen, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public PersonalEntry? SelectedHundefuehrer
        {
            get => _selectedHundefuehrer;
            set => SetProperty(ref _selectedHundefuehrer, value);
        }

        // Specializations
        public bool Flaechensuche
        {
            get => _flaechensuche;
            set => SetProperty(ref _flaechensuche, value);
        }

        public bool Truemmersuche
        {
            get => _truemmersuche;
            set => SetProperty(ref _truemmersuche, value);
        }

        public bool Mantrailing
        {
            get => _mantrailing;
            set => SetProperty(ref _mantrailing, value);
        }

        public bool Wasserortung
        {
            get => _wasserortung;
            set => SetProperty(ref _wasserortung, value);
        }

        public bool Lawinensuche
        {
            get => _lawinensuche;
            set => SetProperty(ref _lawinensuche, value);
        }

        public bool Gelaendesuche
        {
            get => _gelaendesuche;
            set => SetProperty(ref _gelaendesuche, value);
        }

        public bool Leichensuche
        {
            get => _leichensuche;
            set => SetProperty(ref _leichensuche, value);
        }

        // Result
        public DogEntry DogEntry => _dogEntry;
        public bool? DialogResult { get; private set; }

        public DogEditViewModel(DogEntry? existingEntry = null)
        {
            _masterDataService = MasterDataService.Instance;
            _isEditMode = existingEntry != null;
            _dogEntry = existingEntry ?? new DogEntry();

            // Initialize collections
            HundefuehrerList = new ObservableCollection<PersonalEntry>();

            // Initialize commands
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);

            // Load data
            LoadHundefuehrerList();
            if (_isEditMode)
            {
                LoadData();
            }

            LoggingService.Instance.LogInfo($"DogEditViewModel initialized in {(_isEditMode ? "edit" : "create")} mode");
        }

        private void LoadHundefuehrerList()
        {
            try
            {
                HundefuehrerList.Clear();
                
                // Add empty option - create a proper PersonalEntry
                var emptyOption = new PersonalEntry 
                { 
                    Id = "", 
                    Vorname = "(Kein",
                    Nachname = "Hundeführer)"
                };
                HundefuehrerList.Add(emptyOption);
                
                var hundefuehrer = _masterDataService.GetPersonalBySkill(PersonalSkills.Hundefuehrer);
                foreach (var person in hundefuehrer)
                {
                    HundefuehrerList.Add(person);
                }

                // Set default selection
                SelectedHundefuehrer = HundefuehrerList.FirstOrDefault();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading Hundeführer list", ex);
            }
        }

        private void LoadData()
        {
            try
            {
                Name = _dogEntry.Name;
                Rasse = _dogEntry.Rasse;
                Alter = _dogEntry.Alter.ToString();
                Notizen = _dogEntry.Notizen;
                IsActive = _dogEntry.IsActive;

                // Load specializations
                Flaechensuche = _dogEntry.Specializations.HasFlag(DogSpecialization.Flaechensuche);
                Truemmersuche = _dogEntry.Specializations.HasFlag(DogSpecialization.Truemmersuche);
                Mantrailing = _dogEntry.Specializations.HasFlag(DogSpecialization.Mantrailing);
                Wasserortung = _dogEntry.Specializations.HasFlag(DogSpecialization.Wasserortung);
                Lawinensuche = _dogEntry.Specializations.HasFlag(DogSpecialization.Lawinensuche);
                Gelaendesuche = _dogEntry.Specializations.HasFlag(DogSpecialization.Gelaendesuche);
                Leichensuche = _dogEntry.Specializations.HasFlag(DogSpecialization.Leichensuche);

                // Select hundefuehrer if set
                if (!string.IsNullOrEmpty(_dogEntry.HundefuehrerId))
                {
                    var hundefuehrer = _masterDataService.GetPersonalById(_dogEntry.HundefuehrerId);
                    if (hundefuehrer != null)
                    {
                        SelectedHundefuehrer = HundefuehrerList.FirstOrDefault(h => h.Id == hundefuehrer.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading dog data", ex);
            }
        }

        private bool CanExecuteSave()
        {
            return !string.IsNullOrWhiteSpace(Name);
        }

        private void ExecuteSave()
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(Name))
                {
                    LoggingService.Instance.LogWarning("Save attempted with empty dog name");
                    return;
                }

                // Save data to dog entry
                _dogEntry.Name = Name.Trim();
                _dogEntry.Rasse = Rasse.Trim();
                
                if (int.TryParse(Alter, out int alterValue))
                {
                    _dogEntry.Alter = alterValue;
                }
                else
                {
                    _dogEntry.Alter = 0;
                }

                _dogEntry.Notizen = Notizen.Trim();
                _dogEntry.IsActive = IsActive;

                // Build specializations flags
                DogSpecialization specs = DogSpecialization.None;
                if (Flaechensuche) specs |= DogSpecialization.Flaechensuche;
                if (Truemmersuche) specs |= DogSpecialization.Truemmersuche;
                if (Mantrailing) specs |= DogSpecialization.Mantrailing;
                if (Wasserortung) specs |= DogSpecialization.Wasserortung;
                if (Lawinensuche) specs |= DogSpecialization.Lawinensuche;
                if (Gelaendesuche) specs |= DogSpecialization.Gelaendesuche;
                if (Leichensuche) specs |= DogSpecialization.Leichensuche;

                _dogEntry.Specializations = specs;

                // Save hundefuehrer reference
                if (SelectedHundefuehrer != null && !string.IsNullOrEmpty(SelectedHundefuehrer.Id))
                {
                    _dogEntry.HundefuehrerId = SelectedHundefuehrer.Id;
                }
                else
                {
                    _dogEntry.HundefuehrerId = string.Empty;
                }

                DialogResult = true;
                LoggingService.Instance.LogInfo($"Dog {(_isEditMode ? "updated" : "created")}: {_dogEntry.Name}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error saving dog", ex);
                DialogResult = false;
            }
        }

        private void ExecuteCancel()
        {
            try
            {
                DialogResult = false;
                LoggingService.Instance.LogInfo("Dog edit cancelled");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error cancelling dog edit", ex);
            }
        }

        private static bool IsValidNumericInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            return Regex.IsMatch(input, @"^\d+$") && input.Length <= 3; // Max 3 digits for reasonable age
        }
    }
}
