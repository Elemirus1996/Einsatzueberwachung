using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    /// <summary>
    /// MainViewModel für MainWindow - MVVM-Implementation v1.9.0
    /// Central ViewModel für globales State-Management der Hauptanwendung
    /// </summary>
    public class MainViewModel : BaseViewModel, IDisposable
    {
        private DispatcherTimer _clockTimer;
        private bool _disposed = false;

        // Core Data
        private EinsatzData? _einsatzData;
        private ObservableCollection<Team> _teams;
        private int _nextTeamId = 1;
        private int _firstWarningMinutes = 10;
        private int _secondWarningMinutes = 20;

        // UI State Properties
        private string _windowTitle = "🐕‍🦺 Einsatzüberwachung Professional v1.9.0";
        private string _currentTime = DateTime.Now.ToString("HH:mm:ss");
        private string _einsatzInfo = string.Empty;
        private string _einsatzort = string.Empty;
        private string _einsatzleiter = string.Empty;
        private string _teamCount = "0/50";
        private string _statusText = "Einsatzüberwachung v1.9 bereit";
        private string _lastLogText = $"Aktualisiert: {DateTime.Now:HH:mm:ss}";
        private Visibility _welcomeMessageVisibility = Visibility.Visible;
        private bool _isFullscreen = false;

        // Global Notes System
        private ObservableCollection<GlobalNotesEntry> _globalNotesCollection = new ObservableCollection<GlobalNotesEntry>();
        private ObservableCollection<GlobalNotesEntry> _filteredNotesCollection = new ObservableCollection<GlobalNotesEntry>();
        private ObservableCollection<NoteTarget> _noteTargets = new ObservableCollection<NoteTarget>();
        private NoteTarget? _selectedNoteTarget = null;
        private string _quickNoteText = string.Empty;

        // Theme Management
        private FontAwesome.WPF.FontAwesomeIcon _themeIcon = FontAwesome.WPF.FontAwesomeIcon.SunOutline;
        private string _themeTooltip = "Theme: Auto-Modus";

        public MainViewModel()
        {
            _clockTimer = new DispatcherTimer();
            InitializeCollections();
            InitializeCommands();
            InitializeClock();
            InitializeServices();
            InitializeTheme();
            InitializeNoteTargets();

            LoggingService.Instance.LogInfo("MainViewModel initialized with MVVM pattern v1.9.0");
        }

        public MainViewModel(EinsatzData einsatzData, int firstWarningMinutes, int secondWarningMinutes) : this()
        {
            _einsatzData = einsatzData;
            _firstWarningMinutes = firstWarningMinutes;
            _secondWarningMinutes = secondWarningMinutes;

            UpdateMissionDisplay(einsatzData);
            AddGlobalNote($"Einsatz gestartet: {einsatzData.EinsatzTyp} - {einsatzData.Einsatzort}", 
                GlobalNotesEntryType.EinsatzUpdate);
        }

        #region Properties

        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        public string CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public string EinsatzInfo
        {
            get => _einsatzInfo;
            set => SetProperty(ref _einsatzInfo, value);
        }

        public string Einsatzort
        {
            get => _einsatzort;
            set => SetProperty(ref _einsatzort, value);
        }

        public string Einsatzleiter
        {
            get => _einsatzleiter;
            set => SetProperty(ref _einsatzleiter, value);
        }

        public string TeamCount
        {
            get => _teamCount;
            set => SetProperty(ref _teamCount, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string LastLogText
        {
            get => _lastLogText;
            set => SetProperty(ref _lastLogText, value);
        }

        public Visibility WelcomeMessageVisibility
        {
            get => _welcomeMessageVisibility;
            set => SetProperty(ref _welcomeMessageVisibility, value);
        }

        public FontAwesome.WPF.FontAwesomeIcon ThemeIcon
        {
            get => _themeIcon;
            set => SetProperty(ref _themeIcon, value);
        }

        public string ThemeTooltip
        {
            get => _themeTooltip;
            set => SetProperty(ref _themeTooltip, value);
        }

        // Collections
        public ObservableCollection<Team> Teams => _teams;
        public ObservableCollection<GlobalNotesEntry> FilteredNotesCollection => _filteredNotesCollection;
        public ObservableCollection<NoteTarget> NoteTargets => _noteTargets;

        // Global Warning Settings
        public int GlobalFirstWarningMinutes => _firstWarningMinutes;
        public int GlobalSecondWarningMinutes => _secondWarningMinutes;

        // Quick Note
        public string QuickNoteText
        {
            get => _quickNoteText;
            set 
            { 
                if (SetProperty(ref _quickNoteText, value))
                {
                    // Notify command that CanExecute may have changed
                    if (AddQuickNoteCommand is RelayCommand command)
                    {
                        command.RaiseCanExecuteChanged();
                    }
                }
            }
        }

        // Selected Note Target for ComboBox binding
        public NoteTarget? SelectedNoteTarget
        {
            get => _selectedNoteTarget;
            set => SetProperty(ref _selectedNoteTarget, value);
        }

        #endregion

        #region Commands

        public ICommand AddTeamCommand { get; private set; } = null!;
        public ICommand ThemeToggleCommand { get; private set; } = null!;
        public ICommand HelpCommand { get; private set; } = null!;
        public ICommand ExportCommand { get; private set; } = null!;
        public ICommand MenuCommand { get; private set; } = null!;
        public ICommand AddQuickNoteCommand { get; private set; } = null!;
        public ICommand ExportLogCommand { get; private set; } = null!;

        // Keyboard Commands
        public ICommand FullscreenToggleCommand { get; private set; } = null!;
        public ICommand TeamTimerCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            AddTeamCommand = new RelayCommand(ExecuteAddTeam, CanExecuteAddTeam);
            ThemeToggleCommand = new RelayCommand(ExecuteThemeToggle);
            HelpCommand = new RelayCommand(ExecuteHelp);
            ExportCommand = new RelayCommand(ExecuteExport, CanExecuteExport);
            MenuCommand = new RelayCommand(ExecuteMenu);
            AddQuickNoteCommand = new RelayCommand(ExecuteAddQuickNote, CanExecuteAddQuickNote);
            ExportLogCommand = new RelayCommand(ExecuteExportLog, CanExecuteExportLog);
            FullscreenToggleCommand = new RelayCommand(ExecuteFullscreenToggle);
            TeamTimerCommand = new RelayCommand<int>(ExecuteTeamTimer);
        }

        #endregion

        #region Command Implementations

        private bool CanExecuteAddTeam() => _teams.Count < 50;

        private void ExecuteAddTeam()
        {
            try
            {
                TeamAddRequested?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error executing add team command via MVVM", ex);
            }
        }

        private void ExecuteThemeToggle()
        {
            try
            {
                ThemeService.Instance.ToggleTheme();
                LoggingService.Instance.LogInfo($"Theme toggled to: {ThemeService.Instance.CurrentThemeStatus}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error toggling theme via MVVM", ex);
            }
        }

        private void ExecuteHelp()
        {
            try
            {
                HelpRequested?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening help via MVVM", ex);
            }
        }

        private bool CanExecuteExport() => _einsatzData != null;

        private void ExecuteExport()
        {
            try
            {
                ExportRequested?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error executing export via MVVM", ex);
            }
        }

        private void ExecuteMenu()
        {
            try
            {
                MenuRequested?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening menu via MVVM", ex);
            }
        }

        private bool CanExecuteAddQuickNote() => !string.IsNullOrWhiteSpace(_quickNoteText);

        private void ExecuteAddQuickNote()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(QuickNoteText)) return;

                // Get selected target
                var selectedTarget = SelectedNoteTarget?.DisplayName ?? NoteTargets.FirstOrDefault()?.DisplayName ?? "Allgemein";
                
                AddGlobalNote(QuickNoteText, GlobalNotesEntryType.Manual, selectedTarget);
                QuickNoteText = string.Empty;
                
                LoggingService.Instance.LogInfo($"Quick note added via MVVM to target: {selectedTarget}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error adding quick note via MVVM", ex);
            }
        }

        private bool CanExecuteExportLog() => _globalNotesCollection.Any();

        private void ExecuteExportLog()
        {
            try
            {
                ExportLogRequested?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error executing export log via MVVM", ex);
            }
        }

        private void ExecuteFullscreenToggle()
        {
            try
            {
                FullscreenToggleRequested?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error toggling fullscreen via MVVM", ex);
            }
        }

        private void ExecuteTeamTimer(int teamIndex)
        {
            try
            {
                if (teamIndex >= 0 && teamIndex < _teams.Count)
                {
                    var team = _teams[teamIndex];
                    
                    if (team.IsRunning)
                    {
                        team.StopTimer();
                        AddGlobalNote($"Timer gestoppt (F{teamIndex + 1})", GlobalNotesEntryType.TimerStop, team.TeamName);
                    }
                    else
                    {
                        team.StartTimer();
                        AddGlobalNote($"Timer gestartet (F{teamIndex + 1})", GlobalNotesEntryType.TimerStart, team.TeamName);
                    }
                    
                    LoggingService.Instance.LogInfo($"Team timer toggled via MVVM (F{teamIndex + 1}): {team.TeamName}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error toggling team timer via MVVM (F{teamIndex + 1})", ex);
            }
        }

        #endregion

        #region Public Methods

        public void AddTeam(Team team)
        {
            try
            {
                if (team == null) return;

                // Set warning times
                team.FirstWarningMinutes = _firstWarningMinutes;
                team.SecondWarningMinutes = _secondWarningMinutes;
                team.TeamId = _nextTeamId++;

                // Register team events
                RegisterTeamEventsForGlobalNotes(team);

                _teams.Add(team);
                UpdateTeamCount();
                UpdateNoteTargets();

                AddGlobalNote($"Team hinzugefügt: {team.TeamName}", GlobalNotesEntryType.TeamEvent);
                LoggingService.Instance.LogInfo($"Team added via MVVM: {team.TeamName}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error adding team via MVVM", ex);
            }
        }

        public void RemoveTeam(Team team)
        {
            try
            {
                if (team == null) return;

                team.StopTimer();
                _teams.Remove(team);
                
                UpdateTeamCount();
                UpdateNoteTargets();

                AddGlobalNote($"Team gelöscht: {team.TeamName}", GlobalNotesEntryType.TeamEvent);
                LoggingService.Instance.LogInfo($"Team removed via MVVM: {team.TeamName}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error removing team via MVVM", ex);
            }
        }

        public void UpdateSelectedNoteTarget(NoteTarget target)
        {
            try
            {
                // Note target selection updated
                LoggingService.Instance.LogInfo($"Note target selected via MVVM: {target?.DisplayName}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating note target via MVVM", ex);
            }
        }

        #endregion

        #region Private Methods

        private void InitializeCollections()
        {
            _teams = new ObservableCollection<Team>();
            _teams.CollectionChanged += Teams_CollectionChanged;
        }

        private void Teams_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateTeamCount();
            UpdateWelcomeMessageVisibility();
            ((RelayCommand)AddTeamCommand).RaiseCanExecuteChanged();
        }

        private void InitializeClock()
        {
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                CurrentTime = DateTime.Now.ToString("HH:mm:ss");
                LastLogText = $"Aktualisiert: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error in clock timer tick via MVVM", ex);
            }
        }

        private void InitializeServices()
        {
            try
            {
                // Initialize services - LoggingService ist bereits initialisiert
                LoggingService.Instance.Initialize("einsatzueberwachung.log", LogLevel.Info);
                LoggingService.Instance.SetVerboseLogging(true);

                // Initialize global notes service
                GlobalNotesService.Instance.Initialize(_globalNotesCollection,
                    (message) => AddGlobalNote(message, GlobalNotesEntryType.Info),
                    (message) => AddGlobalNote(message, GlobalNotesEntryType.Warnung),
                    (message) => AddGlobalNote(message, GlobalNotesEntryType.Fehler));

                AddGlobalNote("Einsatzüberwachung Professional v1.9.0 gestartet", GlobalNotesEntryType.EinsatzUpdate);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing services via MVVM", ex);
            }
        }

        private void InitializeTheme()
        {
            try
            {
                ThemeService.Instance.ThemeChanged += OnThemeChanged;
                
                if (!ThemeService.Instance.IsAutoMode)
                {
                    ThemeService.Instance.EnableAutoMode();
                }
                
                UpdateThemeDisplay();
                AddGlobalNote($"Theme: {ThemeService.Instance.CurrentThemeStatus}", GlobalNotesEntryType.SystemEvent);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing theme via MVVM", ex);
            }
        }

        private void OnThemeChanged(bool isDarkMode)
        {
            try
            {
                UpdateThemeDisplay();
                AddGlobalNote($"Theme geändert zu: {ThemeService.Instance.CurrentThemeStatus}", 
                    GlobalNotesEntryType.SystemEvent);
                
                ThemeChanged?.Invoke(isDarkMode);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling theme change via MVVM", ex);
            }
        }

        private void UpdateThemeDisplay()
        {
            ThemeIcon = ThemeService.Instance.IsDarkMode 
                ? FontAwesome.WPF.FontAwesomeIcon.MoonOutline
                : FontAwesome.WPF.FontAwesomeIcon.SunOutline;

            if (ThemeService.Instance.IsAutoMode)
            {
                ThemeTooltip = $"Theme: Auto-Modus (18-8 Uhr Dunkel)\nAktuell: {(ThemeService.Instance.IsDarkMode ? "Dunkel" : "Hell")}\n\nKlick → Manuell wechseln";
            }
            else
            {
                ThemeTooltip = $"Theme: Manuell\nAktuell: {(ThemeService.Instance.IsDarkMode ? "Dunkel" : "Hell")}\n\nKlick → Wechseln";
            }
        }

        private void InitializeNoteTargets()
        {
            try
            {
                _noteTargets.Clear();
                _noteTargets.Add(new NoteTarget { DisplayName = "Allgemein", DetailInfo = "📝", IsSpecialTarget = true });
                _noteTargets.Add(new NoteTarget { DisplayName = "Einsatzleiter", DetailInfo = "👨‍💼", IsSpecialTarget = true });
                _noteTargets.Add(new NoteTarget { DisplayName = "Drohnenstaffel", DetailInfo = "🚁", IsSpecialTarget = true });
                _noteTargets.Add(new NoteTarget { DisplayName = "Funkzentrale", DetailInfo = "📻", IsSpecialTarget = true });
                
                // Set default selection to first item (Allgemein) - Note: SelectedNoteTarget property needed
                if (_noteTargets.Count > 0)
                {
                    SelectedNoteTarget = _noteTargets.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing note targets via MVVM", ex);
            }
        }

        private void UpdateNoteTargets()
        {
            try
            {
                var specialTargets = _noteTargets.Where(nt => nt.IsSpecialTarget).ToList();
                var currentSelection = SelectedNoteTarget;
                
                _noteTargets.Clear();
                
                foreach (var target in specialTargets)
                {
                    _noteTargets.Add(target);
                }
                
                foreach (var team in _teams)
                {
                    _noteTargets.Add(new NoteTarget 
                    { 
                        DisplayName = team.TeamName, 
                        DetailInfo = team.TeamTypeShortName,
                        IsSpecialTarget = false 
                    });
                }
                
                // Restore selection if it still exists, otherwise select first item
                if (currentSelection != null && _noteTargets.Any(nt => nt.DisplayName == currentSelection.DisplayName))
                {
                    SelectedNoteTarget = _noteTargets.First(nt => nt.DisplayName == currentSelection.DisplayName);
                }
                else
                {
                    SelectedNoteTarget = _noteTargets.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating note targets via MVVM", ex);
            }
        }

        private void RegisterTeamEventsForGlobalNotes(Team team)
        {
            try
            {
                team.TimerStarted += (t) => AddGlobalNote($"Timer gestartet", GlobalNotesEntryType.TimerStart, t.TeamName);
                team.TimerStopped += (t) => AddGlobalNote($"Timer gestoppt - Einsatzzeit: {t.ElapsedTimeString}", GlobalNotesEntryType.TimerStop, t.TeamName);
                team.TimerReset += (t) => AddGlobalNote($"Timer zurückgesetzt", GlobalNotesEntryType.TimerReset, t.TeamName);
                team.WarningTriggered += (t, isSecond) => 
                {
                    var warningType = isSecond ? GlobalNotesEntryType.Warning2 : GlobalNotesEntryType.Warning1;
                    var warningText = isSecond ? "Zweite Warnung" : "Erste Warnung";
                    AddGlobalNote($"{warningText} bei {t.ElapsedTimeString}", warningType, t.TeamName);
                };
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error registering team events via MVVM", ex);
            }
        }

        private void UpdateMissionDisplay(EinsatzData einsatzData)
        {
            try
            {
                EinsatzInfo = $"{einsatzData.EinsatzTyp} - {einsatzData.EinsatzDatum:dd.MM.yyyy HH:mm}";
                Einsatzort = $"Ort: {einsatzData.Einsatzort}";
                Einsatzleiter = $"EL: {einsatzData.Einsatzleiter}";
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating mission display via MVVM", ex);
            }
        }

        private void UpdateTeamCount()
        {
            TeamCount = $"{_teams.Count}/50";
        }

        private void UpdateWelcomeMessageVisibility()
        {
            WelcomeMessageVisibility = _teams.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AddGlobalNote(string message, GlobalNotesEntryType type, string? teamName = null)
        {
            try
            {
                var note = new GlobalNotesEntry
                {
                    Content = message,
                    EntryType = type,
                    Timestamp = DateTime.Now,
                    TeamName = teamName ?? ""
                };
                
                _globalNotesCollection.Add(note);
                
                if (IsEinsatzRelevantNote(type))
                {
                    _filteredNotesCollection.Add(note);
                }
                
                ScrollToLatestNoteRequested?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error adding global note via MVVM", ex);
            }
        }

        private bool IsEinsatzRelevantNote(GlobalNotesEntryType type)
        {
            return type switch
            {
                GlobalNotesEntryType.EinsatzUpdate => true,
                GlobalNotesEntryType.TeamEvent => true,
                GlobalNotesEntryType.TimerStart => false,
                GlobalNotesEntryType.TimerStop => false,
                GlobalNotesEntryType.TimerReset => true,
                GlobalNotesEntryType.Warning1 => true,
                GlobalNotesEntryType.Warning2 => true,
                GlobalNotesEntryType.Warnung => true,
                GlobalNotesEntryType.Fehler => true,
                GlobalNotesEntryType.Funkspruch => true,
                GlobalNotesEntryType.Manual => true,
                GlobalNotesEntryType.Info => false,
                GlobalNotesEntryType.SystemEvent => false,
                _ => false
            };
        }

        #endregion

        #region Events

        public event Action? TeamAddRequested;
        public event Action? HelpRequested;
        public event Action? ExportRequested;
        public event Action? MenuRequested;
        public event Action? ExportLogRequested;
        public event Action? FullscreenToggleRequested;
        public event Action? ScrollToLatestNoteRequested;
        public event Action<bool>? ThemeChanged;

        #endregion

        #region IDisposable Implementation

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        _clockTimer?.Stop();
                        ThemeService.Instance.ThemeChanged -= OnThemeChanged;
                        
                        foreach (var team in _teams)
                        {
                            team.StopTimer();
                            if (team is IDisposable disposableTeam)
                            {
                                disposableTeam.Dispose();
                            }
                        }
                        
                        LoggingService.Instance.LogInfo("MainViewModel disposed successfully");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogError("Error disposing MainViewModel", ex);
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
