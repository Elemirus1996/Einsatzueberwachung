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
        private int _warnung1 = 10;
        private int _warnung2 = 20;

        // Validation and UI State
        private bool _isFormValid = false;
        private string _validationMessage = string.Empty;

        // Collections for ComboBoxes
        private ObservableCollection<PersonalEntry> _einsatzleiterListe = new();
        private ObservableCollection<PersonalEntry> _fuehrungsassistentList = new();

        // Selected items for binding
        private PersonalEntry? _selectedEinsatzleiter;
        private PersonalEntry? _selectedFuehrungsassistent;

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

        public int Warnung1
        {
            get => _warnung1;
            set
            {
                _warnung1 = Math.Max(1, value);
                OnPropertyChanged();
                ValidateWarningSettings();
            }
        }

        public int Warnung2
        {
            get => _warnung2;
            set
            {
                _warnung2 = Math.Max(_warnung1 + 1, value);
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

        public ObservableCollection<PersonalEntry> EinsatzleiterListe
        {
            get => _einsatzleiterListe;
            set
            {
                _einsatzleiterListe = value;
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

        public PersonalEntry? SelectedEinsatzleiter
        {
            get => _selectedEinsatzleiter;
            set
            {
                _selectedEinsatzleiter = value;
                OnPropertyChanged();
                if (value != null)
                {
                    Einsatzleiter = value.FullName;
                    OnPropertyChanged(nameof(EinsatzleiterInfo));
                }
            }
        }

        public PersonalEntry? SelectedFuehrungsassistent
        {
            get => _selectedFuehrungsassistent;
            set
            {
                _selectedFuehrungsassistent = value;
                OnPropertyChanged();
                if (value != null)
                {
                    Fuehrungsassistent = value.FullName;
                }
            }
        }

        public string EinsatzleiterInfo
        {
            get
            {
                if (SelectedEinsatzleiter?.Skills != null)
                {
                    return $"✓ {SelectedEinsatzleiter.Skills.GetHighestLeadershipLevel()}";
                }
                return string.Empty;
            }
        }

        // Result properties
        public EinsatzData? EinsatzData { get; private set; }
        public int FirstWarningMinutes => Warnung1;
        public int SecondWarningMinutes => Warnung2;

        // Enhanced commands with parameter support
        public ICommand StartCommand { get; private set; } = null!;
        public ICommand CancelCommand { get; private set; } = null!;
        public ICommand ResetFormCommand { get; private set; } = null!;

        // Events
        public event EventHandler? RequestClose;
        public event EventHandler<string>? ShowMessage;

        public StartViewModel()
        {
            _masterDataService = MasterDataService.Instance;
            
            InitializeCommands();
            LoadMasterDataAsync();
            ValidateForm();
            
            LoggingService.Instance?.LogInfo("StartViewModel initialized with Einsatzleiter support");
        }

        private void InitializeCommands()
        {
            StartCommand = new RelayCommand(ExecuteStart, CanExecuteStart);
            CancelCommand = new RelayCommand(ExecuteCancel);
            ResetFormCommand = new RelayCommand(ExecuteResetForm, CanExecuteResetForm);
        }

        private async void LoadMasterDataAsync()
        {
            try
            {
                // Load personnel with leadership qualifications (GF, ZF, VF, EL)
                var allPersonal = _masterDataService.GetAllPersonal();
                var leadershipQualified = allPersonal
                    .Where(p => p.Skills.IsLeadershipQualified())
                    .OrderBy(p => p.FullName)
                    .ToList();

                EinsatzleiterListe.Clear();
                EinsatzleiterListe.Add(new PersonalEntry { Vorname = "(Manuell", Nachname = "eingeben)" });
                
                foreach (var person in leadershipQualified)
                {
                    EinsatzleiterListe.Add(person);
                }

                // Load assistants (Führungsassistenten)
                var assistenten = _masterDataService.GetPersonalBySkill(PersonalSkills.Fuehrungsassistent)
                    .OrderBy(p => p.FullName)
                    .ToList();

                FuehrungsassistentList.Clear();
                FuehrungsassistentList.Add(new PersonalEntry { Vorname = "(Leer", Nachname = "lassen)" });
                
                foreach (var person in assistenten)
                {
                    FuehrungsassistentList.Add(person);
                }

                LoggingService.Instance?.LogInfo($"Loaded personnel: {leadershipQualified.Count} qualified leaders, {assistenten.Count} assistants");
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
            if (Warnung2 <= Warnung1)
            {
                Warnung2 = Warnung1 + 5;
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
                Warnung1 = 10;
                Warnung2 = 20;
                SelectedEinsatzleiter = null;
                SelectedFuehrungsassistent = null;

                LoggingService.Instance?.LogInfo("Start form reset by user");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error resetting form", ex);
            }
        }
    }
}
