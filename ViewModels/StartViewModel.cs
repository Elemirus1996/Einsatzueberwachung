using System.Collections.ObjectModel;
using System.Windows.Input;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    public class StartViewModel : BaseViewModel
    {
        private readonly MasterDataService _masterDataService;
        
        // EinsatzData Properties
        private string _einsatzleiter = string.Empty;
        private string _fuehrungsassistent = string.Empty;
        private string _alarmiert = string.Empty;
        private string _einsatzort = string.Empty;
        private bool _istEinsatz = true;
        private DateTime _einsatzDatum = DateTime.Now;

        // Warning Settings
        private int _firstWarningMinutes = 10;
        private int _secondWarningMinutes = 20;

        // Validation and UI State
        private bool _isFormValid = false;
        private string _validationMessage = string.Empty;

        // Collections for ComboBoxes
        private ObservableCollection<PersonalEntry> _einsatzleiterList = new();
        private ObservableCollection<PersonalEntry> _fuehrungsassistentList = new();

        public string Einsatzleiter
        {
            get => _einsatzleiter;
            set
            {
                _einsatzleiter = value;
                OnPropertyChanged();
                ValidateForm();
            }
        }

        public string Fuehrungsassistent
        {
            get => _fuehrungsassistent;
            set
            {
                _fuehrungsassistent = value;
                OnPropertyChanged();
            }
        }

        public string Alarmiert
        {
            get => _alarmiert;
            set
            {
                _alarmiert = value;
                OnPropertyChanged();
            }
        }

        public string Einsatzort
        {
            get => _einsatzort;
            set
            {
                _einsatzort = value;
                OnPropertyChanged();
                ValidateForm();
            }
        }

        public bool IstEinsatz
        {
            get => _istEinsatz;
            set
            {
                _istEinsatz = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EinsatzTyp));
            }
        }

        public string EinsatzTyp => IstEinsatz ? "Einsatz" : "Übung";

        public DateTime EinsatzDatum
        {
            get => _einsatzDatum;
            set
            {
                _einsatzDatum = value;
                OnPropertyChanged();
            }
        }

        public int FirstWarningMinutes
        {
            get => _firstWarningMinutes;
            set
            {
                _firstWarningMinutes = Math.Max(1, value);
                OnPropertyChanged();
                ValidateWarningSettings();
            }
        }

        public int SecondWarningMinutes
        {
            get => _secondWarningMinutes;
            set
            {
                _secondWarningMinutes = Math.Max(_firstWarningMinutes + 1, value);
                OnPropertyChanged();
                ValidateWarningSettings();
            }
        }

        public bool IsFormValid
        {
            get => _isFormValid;
            set
            {
                _isFormValid = value;
                OnPropertyChanged();
                ((RelayCommand)StartCommand).RaiseCanExecuteChanged();
            }
        }

        public string ValidationMessage
        {
            get => _validationMessage;
            set
            {
                _validationMessage = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<PersonalEntry> EinsatzleiterList
        {
            get => _einsatzleiterList;
            set
            {
                _einsatzleiterList = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<PersonalEntry> FuehrungsassistentList
        {
            get => _fuehrungsassistentList;
            set
            {
                _fuehrungsassistentList = value;
                OnPropertyChanged();
            }
        }

        // Result properties
        public EinsatzData? EinsatzData { get; private set; }

        // Enhanced commands with parameter support
        public ICommand StartCommand { get; private set; } = null!;
        public ICommand CancelCommand { get; private set; } = null!;
        public ICommand ResetFormCommand { get; private set; } = null!;
        public ICommand LoadTemplateCommand { get; private set; } = null!;
        public ICommand SaveTemplateCommand { get; private set; } = null!;
        public ICommand SetCurrentTimeCommand { get; private set; } = null!;
        public ICommand SelectPersonCommand { get; private set; } = null!;

        // Events
        public event EventHandler? RequestClose;
        public event EventHandler<string>? ShowMessage;
        public event EventHandler<EinsatzData>? EinsatzStarted;
        public event EventHandler? Cancelled;

        public StartViewModel()
        {
            _masterDataService = MasterDataService.Instance;
            
            InitializeCommands();
            LoadMasterDataAsync();
            ValidateForm();
            
            LoggingService.Instance?.LogInfo("StartViewModel initialized with enhanced command support");
        }

        private void InitializeCommands()
        {
            StartCommand = new RelayCommand(ExecuteStart, CanExecuteStart);
            CancelCommand = new RelayCommand(ExecuteCancel);
            ResetFormCommand = new RelayCommand(ExecuteResetForm, CanExecuteResetForm);
            LoadTemplateCommand = new RelayCommand<EinsatzTemplate>(ExecuteLoadTemplate);
            SaveTemplateCommand = new RelayCommand(ExecuteSaveTemplate, CanExecuteSaveTemplate);
            SetCurrentTimeCommand = new RelayCommand(ExecuteSetCurrentTime);
            SelectPersonCommand = new RelayCommand<string>(ExecuteSelectPerson);
        }

        private async void LoadMasterDataAsync()
        {
            try
            {
                // Load personnel with leadership skills
                var einsatzleiter = _masterDataService.GetPersonalBySkill(PersonalSkills.Fuehrungsassistent)
                    .Concat(_masterDataService.GetPersonalBySkill(PersonalSkills.Hundefuehrer))
                    .DistinctBy(p => p.Id)
                    .OrderBy(p => p.FullName);

                EinsatzleiterList.Clear();
                foreach (var person in einsatzleiter)
                {
                    EinsatzleiterList.Add(person);
                }

                // Load assistants
                var assistenten = _masterDataService.GetPersonalBySkill(PersonalSkills.Fuehrungsassistent)
                    .OrderBy(p => p.FullName);

                FuehrungsassistentList.Clear();
                foreach (var person in assistenten)
                {
                    FuehrungsassistentList.Add(person);
                }

                LoggingService.Instance?.LogInfo($"Loaded personnel: {EinsatzleiterList.Count} potential leaders, {FuehrungsassistentList.Count} assistants");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error loading master data in StartViewModel", ex);
                ShowMessage?.Invoke(this, "Fehler beim Laden der Stammdaten");
            }
        }

        private void ValidateForm()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Einsatzleiter))
            {
                errors.Add("Einsatzleiter ist erforderlich");
            }

            if (string.IsNullOrWhiteSpace(Einsatzort))
            {
                errors.Add("Einsatzort ist erforderlich");
            }

            if (EinsatzDatum > DateTime.Now.AddHours(1))
            {
                errors.Add("Einsatzdatum liegt zu weit in der Zukunft");
            }

            ValidationMessage = errors.Count > 0 ? string.Join(", ", errors) : string.Empty;
            IsFormValid = errors.Count == 0;
        }

        private void ValidateWarningSettings()
        {
            if (SecondWarningMinutes <= FirstWarningMinutes)
            {
                SecondWarningMinutes = FirstWarningMinutes + 5;
            }
        }

        // Command implementations
        private bool CanExecuteStart()
        {
            return IsFormValid;
        }

        private void ExecuteStart()
        {
            try
            {
                EinsatzData = new EinsatzData
                {
                    Einsatzleiter = Einsatzleiter,
                    Fuehrungsassistent = Fuehrungsassistent,
                    Alarmiert = Alarmiert,
                    Einsatzort = Einsatzort,
                    IstEinsatz = IstEinsatz,
                    EinsatzDatum = EinsatzDatum
                };

                LoggingService.Instance?.LogInfo($"Starting {EinsatzTyp}: {Einsatzort} with leader {Einsatzleiter}");
                
                // Trigger events für UI-Handling
                EinsatzStarted?.Invoke(this, EinsatzData);
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error starting mission", ex);
                ShowMessage?.Invoke(this, $"Fehler beim Starten: {ex.Message}");
            }
        }

        private void ExecuteCancel()
        {
            try
            {
                LoggingService.Instance?.LogInfo("Start dialog cancelled by user");
                EinsatzData = null;
                
                // Trigger events für UI-Handling
                Cancelled?.Invoke(this, EventArgs.Empty);
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error cancelling start dialog", ex);
            }
        }

        private bool CanExecuteResetForm()
        {
            return !string.IsNullOrWhiteSpace(Einsatzleiter) || 
                   !string.IsNullOrWhiteSpace(Einsatzort) || 
                   !string.IsNullOrWhiteSpace(Fuehrungsassistent) || 
                   !string.IsNullOrWhiteSpace(Alarmiert);
        }

        private void ExecuteResetForm()
        {
            try
            {
                Einsatzleiter = string.Empty;
                Fuehrungsassistent = string.Empty;
                Alarmiert = string.Empty;
                Einsatzort = string.Empty;
                IstEinsatz = true;
                EinsatzDatum = DateTime.Now;
                FirstWarningMinutes = 10;
                SecondWarningMinutes = 20;

                LoggingService.Instance?.LogInfo("Start form reset by user");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error resetting form", ex);
            }
        }

        private void ExecuteLoadTemplate(EinsatzTemplate? template)
        {
            try
            {
                if (template == null) return;

                // Apply template values (implementation would depend on EinsatzTemplate structure)
                FirstWarningMinutes = template.FirstWarningMinutes;
                SecondWarningMinutes = template.SecondWarningMinutes;

                LoggingService.Instance?.LogInfo($"Template loaded: {template.Name}");
                ShowMessage?.Invoke(this, $"Vorlage '{template.Name}' geladen");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError($"Error loading template", ex);
                ShowMessage?.Invoke(this, $"Fehler beim Laden der Vorlage: {ex.Message}");
            }
        }

        private bool CanExecuteSaveTemplate()
        {
            return IsFormValid;
        }

        private void ExecuteSaveTemplate()
        {
            try
            {
                // Implementation would show a save template dialog
                LoggingService.Instance?.LogInfo("Save template requested");
                ShowMessage?.Invoke(this, "Vorlage-Speichern wird in einer zukünftigen Version implementiert");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error saving template", ex);
            }
        }

        private void ExecuteSetCurrentTime()
        {
            try
            {
                EinsatzDatum = DateTime.Now;
                LoggingService.Instance?.LogInfo("Mission time set to current time");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error setting current time", ex);
            }
        }

        private void ExecuteSelectPerson(string? role)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(role)) return;

                // This could trigger a person selection dialog or similar
                LoggingService.Instance?.LogInfo($"Person selection requested for role: {role}");
                
                // For now, just log the request - full implementation would depend on UI requirements
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError($"Error selecting person for role {role}", ex);
            }
        }
    }
}
