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
        private ObservableCollection<Team> _teams = new();
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
        private bool _showAddTeamButton = true; // Add this property

        // Global Notes System
        private ObservableCollection<GlobalNotesEntry> _globalNotesCollection = new ObservableCollection<GlobalNotesEntry>();
        private ObservableCollection<GlobalNotesEntry> _filteredNotesCollection = new ObservableCollection<GlobalNotesEntry>();
        private ObservableCollection<NoteTarget> _noteTargets = new ObservableCollection<NoteTarget>();
        private NoteTarget? _selectedNoteTarget = null;
        private string _quickNoteText = string.Empty;

        // Theme Management
        private FontAwesome.WPF.FontAwesomeIcon _themeIcon = FontAwesome.WPF.FontAwesomeIcon.SunOutline;
        private string _themeTooltip = "Theme: Auto-Modus";

        /// <summary>
        /// NEU v1.9.2: Zugriff auf EinsatzData für externe Integration
        /// </summary>
        public EinsatzData? EinsatzData => _einsatzData;

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
            AddGlobalNoteInternal($"Einsatz gestartet: {einsatzData.EinsatzTyp} - {einsatzData.Einsatzort}", 
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

        public bool ShowAddTeamButton
        {
            get => _showAddTeamButton;
            set => SetProperty(ref _showAddTeamButton, value);
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
                UnifiedThemeManager.Instance.ToggleTheme();
                LoggingService.Instance.LogInfo($"Theme toggled to: {UnifiedThemeManager.Instance.CurrentThemeStatus}");
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
                
                AddGlobalNoteInternal(QuickNoteText, GlobalNotesEntryType.Manual, selectedTarget);
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
                        AddGlobalNoteInternal($"Timer gestoppt (F{teamIndex + 1})", GlobalNotesEntryType.TimerStop, team.TeamName);
                    }
                    else
                    {
                        team.StartTimer();
                        AddGlobalNoteInternal($"Timer gestartet (F{teamIndex + 1})", GlobalNotesEntryType.TimerStart, team.TeamName);
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

        #region Public Methods

        /// <summary>
        /// Adds a global note for external calls
        /// </summary>
        public void AddGlobalNote(string content, GlobalNotesEntryType entryType = GlobalNotesEntryType.Manual, string teamTarget = "Allgemein")
        {
            AddGlobalNoteInternal(content, entryType, teamTarget);
        }

        /// <summary>
        /// Adds a team to the collection
        /// </summary>
        public void AddTeam(Team team)
        {
            try
            {
                if (team != null)
                {
                    Teams.Add(team);
                    UpdateTeamCounter();
                    UpdateAddTeamButtonVisibility();
                    AddGlobalNoteInternal($"Team hinzugefügt: {team.TeamName}", GlobalNotesEntryType.TeamEvent);
                    LoggingService.Instance.LogInfo($"Team added via MVVM: {team.TeamName}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error adding team via MVVM", ex);
            }
        }

        /// <summary>
        /// Removes a team from the collection
        /// </summary>
        public void RemoveTeam(Team team)
        {
            try
            {
                if (team != null && Teams.Contains(team))
                {
                    Teams.Remove(team);
                    UpdateTeamCounter();
                    UpdateAddTeamButtonVisibility();
                    AddGlobalNoteInternal($"Team entfernt: {team.TeamName}", GlobalNotesEntryType.TeamEvent);
                    LoggingService.Instance.LogInfo($"Team removed via MVVM: {team.TeamName}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error removing team via MVVM", ex);
            }
        }

        /// <summary>
        /// Adds a reply to a global note
        /// </summary>
        public void AddReply(string parentId, string content, string? teamName = null)
        {
            try
            {
                var reply = GlobalNotesService.Instance.CreateReply(parentId, content, teamName);
                if (reply != null)
                {
                    UpdateFilteredNotes();
                    LoggingService.Instance.LogInfo($"Reply added via MVVM to note {parentId}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error adding reply via MVVM", ex);
            }
        }

        private void UpdateTeamCounter()
        {
            try
            {
                TeamCount = $"{Teams.Count}/50";
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating team counter", ex);
            }
        }

        private void UpdateAddTeamButtonVisibility()
        {
            try
            {
                // Show the Add Team button when there are teams (to add more) or always show it for adding the first team
                ShowAddTeamButton = true; // Always show the button since it's useful
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating add team button visibility", ex);
            }
        }

        #endregion

        #region Initialization Methods

        private void InitializeCollections()
        {
            try
            {
                _teams = new ObservableCollection<Team>();
                LoggingService.Instance.LogInfo("Collections initialized via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing collections via MVVM", ex);
            }
        }

        private void InitializeClock()
        {
            try
            {
                _clockTimer.Interval = TimeSpan.FromSeconds(1);
                _clockTimer.Tick += (s, e) => CurrentTime = DateTime.Now.ToString("HH:mm:ss");
                _clockTimer.Start();
                
                LoggingService.Instance.LogInfo("Clock timer initialized via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing clock via MVVM", ex);
            }
        }

        private void InitializeServices()
        {
            try
            {
                // Initialize logging service if needed
                LoggingService.Instance.LogInfo("Services initialized via MVVM");
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
                // Theme initialization
                UpdateThemeIcon();
                LoggingService.Instance.LogInfo("Theme system initialized via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing theme via MVVM", ex);
            }
        }

        private void InitializeNoteTargets()
        {
            try
            {
                // Initialize note targets
                _noteTargets.Clear();
                _noteTargets.Add(new NoteTarget { TeamId = 0, DisplayName = "Allgemein", IsSpecialTarget = true });
                _noteTargets.Add(new NoteTarget { TeamId = 0, DisplayName = "Einsatzleiter", IsSpecialTarget = true });
                _noteTargets.Add(new NoteTarget { TeamId = 0, DisplayName = "Drohnenstaffel", IsSpecialTarget = true });

                _selectedNoteTarget = _noteTargets.FirstOrDefault();
                
                LoggingService.Instance.LogInfo("Note targets initialized via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing note targets via MVVM", ex);
            }
        }

        private void UpdateMissionDisplay(EinsatzData einsatzData)
        {
            try
            {
                EinsatzInfo = $"{einsatzData.EinsatzTyp} - {einsatzData.Einsatzort}";
                Einsatzort = einsatzData.Einsatzort;
                Einsatzleiter = einsatzData.Einsatzleiter;
                WelcomeMessageVisibility = Visibility.Collapsed;
                
                LoggingService.Instance.LogInfo("Mission display updated via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating mission display via MVVM", ex);
            }
        }

        private void AddGlobalNoteInternal(string content, GlobalNotesEntryType entryType, string teamTarget = "Allgemein")
        {
            try
            {
                var note = new GlobalNotesEntry
                {
                    Content = content,
                    EntryType = entryType,
                    TeamName = teamTarget == "Allgemein" ? "" : teamTarget,
                    Timestamp = DateTime.Now
                };

                _globalNotesCollection.Add(note);
                
                // Keep only the last 500 notes
                while (_globalNotesCollection.Count > 500)
                {
                    _globalNotesCollection.RemoveAt(0);
                }
                
                UpdateFilteredNotes();
                LastLogText = $"Aktualisiert: {DateTime.Now:HH:mm:ss}";
                
                LoggingService.Instance.LogInfo($"Global note added via MVVM: {entryType} - {content}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error adding global note via MVVM", ex);
            }
        }

        private void UpdateFilteredNotes()
        {
            try
            {
                _filteredNotesCollection.Clear();
                
                // Show all notes or filter by selected target
                var notesToShow = _globalNotesCollection.AsEnumerable();
                
                if (_selectedNoteTarget != null && !_selectedNoteTarget.IsSpecialTarget)
                {
                    notesToShow = notesToShow.Where(n => n.TeamName == _selectedNoteTarget.DisplayName);
                }
                
                foreach (var note in notesToShow.TakeLast(100)) // Show last 100 notes
                {
                    _filteredNotesCollection.Add(note);
                }
                
                ScrollToLatestNoteRequested?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating filtered notes via MVVM", ex);
            }
        }

        private void UpdateThemeIcon()
        {
            try
            {
                // This would normally check the current theme and update the icon
                // For now, set a default
                ThemeIcon = FontAwesome.WPF.FontAwesomeIcon.SunOutline;
                ThemeTooltip = "Theme: Auto-Modus";
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating theme icon via MVVM", ex);
            }
        }

        #endregion

        #region IDisposable Support

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _clockTimer?.Stop();
                    _clockTimer = null!;
                }

                // Dispose unmanaged resources

                _disposed = true;
            }
        }

        #endregion
    }
}
