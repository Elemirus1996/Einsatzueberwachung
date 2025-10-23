using System.Collections.ObjectModel;
using System.Windows.Input;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    public class StartViewModel : BaseViewModel
    {
        private readonly MasterDataService _masterDataService;
        private readonly FeatureHighlightService _featureHighlightService;
        
        // EinsatzData Properties
        private string _einsatzleiter = string.Empty;
        private string _fuehrungsassistent = string.Empty;
        private string _alarmiert = string.Empty;
        private string _einsatzort = string.Empty;
        private bool _istEinsatz = true;
        private DateTime _einsatzDatum = DateTime.Now;

        // Validation and UI State
        private bool _isFormValid = false;
        private string _validationMessage = string.Empty;
        private bool _isLoadingMasterData = true;
        private string _loadingMessage = "Stammdaten werden geladen...";
        private bool _showFeatureHighlight = false;

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

        public bool IsLoadingMasterData
        {
            get => _isLoadingMasterData;
            set
            {
                _isLoadingMasterData = value;
                OnPropertyChanged();
                ((RelayCommand)StartCommand).RaiseCanExecuteChanged();
            }
        }

        public string LoadingMessage
        {
            get => _loadingMessage;
            set
            {
                _loadingMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Bestimmt ob das Feature-Highlight angezeigt werden soll
        /// Wird nur bei den ersten 3 Starts einer Version gezeigt
        /// </summary>
        public bool ShowFeatureHighlight
        {
            get => _showFeatureHighlight;
            private set
            {
                _showFeatureHighlight = value;
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
        
        // Standard Timer-Werte (können in Einstellungen geändert werden)
        public int FirstWarningMinutes => 10;  // Standardwert
        public int SecondWarningMinutes => 20; // Standardwert

        // Enhanced commands with parameter support
        public ICommand StartCommand { get; private set; } = null!;
        public ICommand CancelCommand { get; private set; } = null!;
        public ICommand ResetFormCommand { get; private set; } = null!;
        public ICommand DismissFeatureHighlightCommand { get; private set; } = null!;

        // Events
        public event EventHandler? RequestClose;
        public event EventHandler<string>? ShowMessage;

        public StartViewModel()
        {
            _masterDataService = MasterDataService.Instance;
            _featureHighlightService = FeatureHighlightService.Instance;
            
            InitializeCommands();
            InitializeFeatureHighlight();
            LoadMasterDataAsync();
            ValidateForm();
            
            // 🔄 NEU: Automatischer Update-Check beim Start
            CheckForUpdatesAsync();
            
            LoggingService.Instance?.LogInfo("StartViewModel initialized with simplified form (no timer settings) and automatic update check");
        }

        /// <summary>
        /// 🔄 Prüft automatisch nach Updates beim Start der Anwendung
        /// </summary>
        private async void CheckForUpdatesAsync()
        {
            try
            {
                // Nur prüfen wenn es keine Development-Version ist
                if (VersionService.IsDevelopmentVersion)
                {
                    LoggingService.Instance.LogInfo("🔄 Update check skipped - Development version");
                    return;
                }

                LoggingService.Instance.LogInfo("🔄 Starting automatic update check on application startup...");
                
                // Warte kurz damit das Fenster zuerst geladen wird
                await Task.Delay(2000);

                using (var updateService = new GitHubUpdateService())
                {
                    var updateInfo = await updateService.CheckForUpdatesAsync();

                    if (updateInfo != null)
                    {
                        LoggingService.Instance.LogInfo($"✅ Update verfügbar: {updateInfo.Version}");
                        
                        // Zeige Update-Benachrichtigung auf dem UI-Thread
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            try
                            {
                                var updateWindow = new Views.UpdateNotificationWindow(updateInfo);
                                updateWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                                updateWindow.Topmost = true;
                                updateWindow.Show();
                                
                                LoggingService.Instance.LogInfo("✅ Update notification window opened");
                            }
                            catch (Exception ex)
                            {
                                LoggingService.Instance.LogError("❌ Error showing update notification", ex);
                            }
                        });
                    }
                    else
                    {
                        LoggingService.Instance.LogInfo("✅ No updates available - Application is up to date");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("❌ Error during automatic update check", ex);
                // Fehler beim Update-Check sind nicht kritisch, Anwendung läuft normal weiter
            }
        }

        private void InitializeCommands()
        {
            StartCommand = new RelayCommand(ExecuteStart, CanExecuteStart);
            CancelCommand = new RelayCommand(ExecuteCancel);
            ResetFormCommand = new RelayCommand(ExecuteResetForm, CanExecuteResetForm);
            DismissFeatureHighlightCommand = new RelayCommand(ExecuteDismissFeatureHighlight);
        }

        private void InitializeFeatureHighlight()
        {
            try
            {
                // Prüfe ob Feature-Highlight angezeigt werden soll
                ShowFeatureHighlight = _featureHighlightService.ShouldShowFeatureHighlight();
                
                if (ShowFeatureHighlight)
                {
                    // Markiere als angezeigt
                    _featureHighlightService.MarkAsShown();
                    var (showCount, lastVersion, _) = _featureHighlightService.GetStatistics();
                    LoggingService.Instance?.LogInfo($"FeatureHighlight displayed (count: {showCount}, version: {lastVersion})");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error initializing feature highlight", ex);
                // Bei Fehler: Nicht anzeigen
                ShowFeatureHighlight = false;
            }
        }

        private async void LoadMasterDataAsync()
        {
            try
            {
                LoadingMessage = "Stammdaten werden geladen...";
                IsLoadingMasterData = true;

                // Warte darauf, dass der MasterDataService seine Daten geladen hat
                // Falls noch nicht geladen, warte bis zu 10 Sekunden
                int maxWaitTime = 10000; // 10 Sekunden
                int waitInterval = 100; // 100ms Intervalle
                int totalWaited = 0;

                while (_masterDataService.PersonalList.Count == 0 && totalWaited < maxWaitTime)
                {
                    await Task.Delay(waitInterval);
                    totalWaited += waitInterval;
                    
                    // Update loading message
                    LoadingMessage = $"Stammdaten werden geladen... ({totalWaited / 1000}s)";
                }

                // Force reload if still empty
                if (_masterDataService.PersonalList.Count == 0)
                {
                    LoadingMessage = "Stammdaten werden neu geladen...";
                    await _masterDataService.LoadDataAsync();
                }

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

                IsLoadingMasterData = false;
                LoadingMessage = "Stammdaten erfolgreich geladen";

                LoggingService.Instance?.LogInfo($"Loaded personnel: {leadershipQualified.Count} qualified leaders, {assistenten.Count} assistants");
            }
            catch (Exception ex)
            {
                IsLoadingMasterData = false;
                LoadingMessage = "Fehler beim Laden der Stammdaten";
                LoggingService.Instance?.LogError("Error loading master data in StartViewModel", ex);
                ShowMessage?.Invoke(this, "Fehler beim Laden der Stammdaten");
                
                // Fallback: Erstelle mindestens einen manuellen Eintrag
                if (EinsatzleiterListe.Count == 0)
                {
                    EinsatzleiterListe.Add(new PersonalEntry { Vorname = "(Manuell", Nachname = "eingeben)" });
                }
                if (FuehrungsassistentList.Count == 0)
                {
                    FuehrungsassistentList.Add(new PersonalEntry { Vorname = "(Leer", Nachname = "lassen)" });
                }
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

        // Command implementations
        private bool CanExecuteStart()
        {
            return IsFormValid && !IsLoadingMasterData;
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

                LoggingService.Instance?.LogInfo($"Starting {EinsatzTyp}: {Einsatzort} with leader {Einsatzleiter} (using default timer values)");
                
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
                SelectedEinsatzleiter = null;
                SelectedFuehrungsassistent = null;

                LoggingService.Instance?.LogInfo("Start form reset by user");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error resetting form", ex);
            }
        }

        private void ExecuteDismissFeatureHighlight()
        {
            try
            {
                ShowFeatureHighlight = false;
                LoggingService.Instance?.LogInfo("Feature highlight manually dismissed by user");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error dismissing feature highlight", ex);
            }
        }
    }
}
