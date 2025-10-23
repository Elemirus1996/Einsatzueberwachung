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
        private bool? _dialogResult;

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
        private bool _inAusbildung; // NEU

        // Validation Properties
        private bool _isFormValid;
        private string _validationMessage = string.Empty;

        // Collections
        public ObservableCollection<PersonalEntry> HundefuehrerList { get; }

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // Events
        public event Action? RequestClose;

        // Properties
        public string WindowTitle => _isEditMode ? "Hund bearbeiten" : "Neuer Hund";

        public string Name
        {
            get => _name;
            set 
            { 
                if (SetProperty(ref _name, value))
                {
                    ValidateForm();
                }
            }
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

        // Specializations with validation triggers
        public bool Flaechensuche
        {
            get => _flaechensuche;
            set 
            { 
                if (SetProperty(ref _flaechensuche, value))
                {
                    ValidateForm();
                }
            }
        }

        public bool Truemmersuche
        {
            get => _truemmersuche;
            set 
            { 
                if (SetProperty(ref _truemmersuche, value))
                {
                    ValidateForm();
                }
            }
        }

        public bool Mantrailing
        {
            get => _mantrailing;
            set 
            { 
                if (SetProperty(ref _mantrailing, value))
                {
                    ValidateForm();
                }
            }
        }

        public bool Wasserortung
        {
            get => _wasserortung;
            set 
            { 
                if (SetProperty(ref _wasserortung, value))
                {
                    ValidateForm();
                }
            }
        }

        public bool Lawinensuche
        {
            get => _lawinensuche;
            set 
            { 
                if (SetProperty(ref _lawinensuche, value))
                {
                    ValidateForm();
                }
            }
        }

        public bool Gelaendesuche
        {
            get => _gelaendesuche;
            set 
            { 
                if (SetProperty(ref _gelaendesuche, value))
                {
                    ValidateForm();
                }
            }
        }

        public bool Leichensuche
        {
            get => _leichensuche;
            set 
            { 
                if (SetProperty(ref _leichensuche, value))
                {
                    ValidateForm();
                }
            }
        }

        public bool InAusbildung
        {
            get => _inAusbildung;
            set 
            { 
                if (SetProperty(ref _inAusbildung, value))
                {
                    ValidateForm();
                }
            }
        }

        #region Validation Properties

        /// <summary>
        /// Ist das Formular gültig und kann gespeichert werden?
        /// </summary>
        public bool IsFormValid
        {
            get => _isFormValid;
            set
            {
                if (SetProperty(ref _isFormValid, value))
                {
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Aktuelle Validierungsnachricht
        /// </summary>
        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        /// <summary>
        /// Gibt es Validierungsfehler?
        /// </summary>
        public bool HasValidationError => !string.IsNullOrEmpty(ValidationMessage);

        #endregion

        // Result
        public DogEntry DogEntry => _dogEntry;
        
        public bool? DialogResult
        {
            get => _dialogResult;
            private set => SetProperty(ref _dialogResult, value);
        }

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
            
            ValidateForm(); // Initial validation

            LoggingService.Instance.LogInfo($"DogEditViewModel initialized - Mode: {(_isEditMode ? "Edit" : "New")}, Entry ID: {_dogEntry.Id}");
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
                
                LoggingService.Instance.LogInfo($"Loaded {hundefuehrer.Count} Hundeführer for dog selection");
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
                InAusbildung = _dogEntry.Specializations.HasFlag(DogSpecialization.InAusbildung);

                // Select hundefuehrer if set
                if (!string.IsNullOrEmpty(_dogEntry.HundefuehrerId))
                {
                    var hundefuehrer = _masterDataService.GetPersonalById(_dogEntry.HundefuehrerId);
                    if (hundefuehrer != null)
                    {
                        SelectedHundefuehrer = HundefuehrerList.FirstOrDefault(h => h.Id == hundefuehrer.Id);
                    }
                }
                
                LoggingService.Instance.LogInfo($"Loaded existing DogEntry data: {_dogEntry.Name}, Specializations: {_dogEntry.Specializations}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading dog data", ex);
            }
        }

        private bool CanExecuteSave()
        {
            return IsFormValid;
        }

        private bool ValidateForm()
        {
            var errors = new System.Collections.Generic.List<string>();

            if (string.IsNullOrWhiteSpace(Name))
            {
                errors.Add("Name ist erforderlich");
            }

            var hasSpecializations = HasAnySpecializationSelected();
            LoggingService.Instance.LogInfo($"Dog Validation: HasAnySpecializationSelected = {hasSpecializations}, Specs: FL={Flaechensuche}, TR={Truemmersuche}, MT={Mantrailing}, WO={Wasserortung}, LA={Lawinensuche}, GE={Gelaendesuche}, LS={Leichensuche}, IA={InAusbildung}");

            if (!hasSpecializations)
            {
                errors.Add("Mindestens eine Spezialisierung muss ausgewählt werden");
            }

            ValidationMessage = errors.Count > 0 ? string.Join(", ", errors) : string.Empty;
            IsFormValid = errors.Count == 0;

            LoggingService.Instance.LogInfo($"Dog Validation result: IsFormValid={IsFormValid}, ValidationMessage='{ValidationMessage}'");

            return IsFormValid;
        }

        private bool HasAnySpecializationSelected()
        {
            var result = Flaechensuche || Truemmersuche || Mantrailing || Wasserortung || 
                        Lawinensuche || Gelaendesuche || Leichensuche || InAusbildung;
            
            LoggingService.Instance.LogInfo($"HasAnySpecializationSelected: result={result}, individual specs: FL={Flaechensuche}, TR={Truemmersuche}, MT={Mantrailing}, WO={Wasserortung}, LA={Lawinensuche}, GE={Gelaendesuche}, LS={Leichensuche}, IA={InAusbildung}");
            
            return result;
        }

        private void ExecuteSave()
        {
            try
            {
                LoggingService.Instance.LogInfo($"Starting save process - IsEditMode: {_isEditMode}, DogEntry ID: {_dogEntry.Id}");

                // Validation
                if (!ValidateForm())
                {
                    LoggingService.Instance.LogWarning("Save cancelled due to validation errors");
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
                if (InAusbildung) specs |= DogSpecialization.InAusbildung;

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

                // Generate new ID for new entries
                if (!_isEditMode && string.IsNullOrEmpty(_dogEntry.Id))
                {
                    _dogEntry.Id = Guid.NewGuid().ToString();
                    LoggingService.Instance.LogInfo($"Generated new ID for DogEntry: {_dogEntry.Id}");
                }

                LoggingService.Instance.LogInfo($"DogEntry prepared for save: ID={_dogEntry.Id}, Name={_dogEntry.Name}, Specializations={specs}");

                DialogResult = true;
                LoggingService.Instance.LogInfo($"Dog data ready for return to caller - Mode: {(_isEditMode ? "Edit" : "New")}");
                
                // Trigger window close
                RequestClose?.Invoke();
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
                
                // Trigger window close
                RequestClose?.Invoke();
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
