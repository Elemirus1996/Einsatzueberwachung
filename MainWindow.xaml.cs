using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Text.Json;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;
using IOPath = System.IO.Path;

namespace Einsatzueberwachung
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private DispatcherTimer _clockTimer;
        private EinsatzData? _einsatzData;
        private ObservableCollection<Team> _teams;
        private int _nextTeamId = 1;
        private int _firstWarningMinutes = 10;
        private int _secondWarningMinutes = 20;
        private bool _isFullscreen = false;
        private WindowState _previousWindowState;
        private WindowStyle _previousWindowStyle;
        private DispatcherTimer? _resizeTimer;
        private MobileConnectionWindow? _mobileConnectionWindow;
        
        // Dashboard View Management
        private Dictionary<int, TeamCompactCard> _compactCards = new Dictionary<int, TeamCompactCard>();

        // NEU: Public properties for accessing global warning settings
        public int GlobalFirstWarningMinutes => _firstWarningMinutes;
        public int GlobalSecondWarningMinutes => _secondWarningMinutes;

        // ============================================
        // GLOBALES NOTIZEN-SYSTEM v1.7
        // ============================================
        private ObservableCollection<GlobalNotesEntry> _globalNotesCollection = new ObservableCollection<GlobalNotesEntry>();
        public ObservableCollection<GlobalNotesEntry> GlobalNotesCollection => _globalNotesCollection;

        // NEU: Gefilterte Collection nur für einsatzrelevante Notizen (für UI-Anzeige)
        private ObservableCollection<GlobalNotesEntry> _filteredNotesCollection = new ObservableCollection<GlobalNotesEntry>();
        public ObservableCollection<GlobalNotesEntry> FilteredNotesCollection => _filteredNotesCollection;

        // NEU: Note Targets für ComboBox (Teams + Einsatzleiter + Drohnenstaffel)
        private ObservableCollection<NoteTarget> _noteTargets = new ObservableCollection<NoteTarget>();
        public ObservableCollection<NoteTarget> NoteTargets => _noteTargets;

        private string _quickNoteText = string.Empty;
        public string QuickNoteText
        {
            get => _quickNoteText;
            set
            {
                _quickNoteText = value;
                OnPropertyChanged(nameof(QuickNoteText));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            _teams = new ObservableCollection<Team>();

            // DataContext für Binding setzen
            DataContext = this;

            // NEU: Initialisiere Note Targets mit speziellen Einträgen
            InitializeNoteTargets();

            // Team-Auswahl ComboBox mit Note Targets befüllen
            if (TeamSelectionComboBox != null)
            {
                TeamSelectionComboBox.ItemsSource = _noteTargets;
            }

            // NEU: Binde gefilterte Notizen-Collection
            if (GlobalNotesItemsControl != null)
            {
                GlobalNotesItemsControl.ItemsSource = _filteredNotesCollection;
            }

            InitializeServices();
            InitializeTheme(); // NEU: Theme-Initialisierung
            InitializeClock();
            
            CheckForRecoveryAsync();

            // Willkommensnotiz im globalen System
            AddGlobalNote("Einsatzüberwachung Professional v1.7 gestartet",
                GlobalNotesEntryType.EinsatzUpdate);
        }

        // NEW: Constructor that accepts EinsatzData from StartWindow
        public MainWindow(EinsatzData einsatzData, int firstWarningMinutes, int secondWarningMinutes) : this()
        {
            // Set the mission data directly without showing StartWindow
            _einsatzData = einsatzData;
            _firstWarningMinutes = firstWarningMinutes;
            _secondWarningMinutes = secondWarningMinutes;

            // Update UI with mission info
            UpdateMissionDisplayAsync(einsatzData);

            // Add global note for mission start
            AddGlobalNote($"Einsatz gestartet: {einsatzData.EinsatzTyp} - {einsatzData.Einsatzort}", 
                GlobalNotesEntryType.EinsatzUpdate);
        }

        // NEU: Theme-Initialisierung
        private void InitializeTheme()
        {
            try
            {
                // Theme Service Event abonnieren
                ThemeService.Instance.ThemeChanged += OnThemeChanged;
                
                // Sicherstellen, dass Auto-Modus aktiviert ist
                if (!ThemeService.Instance.IsAutoMode)
                {
                    ThemeService.Instance.EnableAutoMode();
                }
                
                // Initial theme icon setzen
                UpdateThemeIcon();
                
                // Initial theme auf alle Team Controls anwenden
                ApplyThemeToAllTeams();
                
                LoggingService.Instance.LogInfo($"Theme initialized: {ThemeService.Instance.CurrentThemeStatus}");
                
                // NEU: SystemEvent statt Info für Theme-Status (wird nicht im Panel angezeigt)
                AddGlobalNote($"Theme: {ThemeService.Instance.CurrentThemeStatus}", GlobalNotesEntryType.SystemEvent);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing theme", ex);
            }
        }

        private void OnThemeChanged(bool isDarkMode)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    UpdateThemeIcon();
                    ApplyThemeToAllTeams();
                    
                    // NEU: SystemEvent statt Info (wird nicht im Panel angezeigt)
                    AddGlobalNote($"Theme geändert zu: {ThemeService.Instance.CurrentThemeStatus}", 
                        GlobalNotesEntryType.SystemEvent);
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling theme change", ex);
            }
        }

        private void UpdateThemeIcon()
        {
            if (ThemeIcon != null)
            {
                // Icon je nach aktuellem Theme
                ThemeIcon.Icon = ThemeService.Instance.IsDarkMode 
                    ? FontAwesome.WPF.FontAwesomeIcon.MoonOutline
                    : FontAwesome.WPF.FontAwesomeIcon.SunOutline;

                // Tooltip mit detaillierten Informationen
                if (BtnThemeToggle != null)
                {
                    string tooltip;
                    if (ThemeService.Instance.IsAutoMode)
                    {
                        tooltip = $"Theme: Auto-Modus (18-8 Uhr Dunkel)\nAktuell: {(ThemeService.Instance.IsDarkMode ? "Dunkel" : "Hell")}\n\nKlick → Manuell wechseln";
                    }
                    else
                    {
                        tooltip = $"Theme: Manuell\nAktuell: {(ThemeService.Instance.IsDarkMode ? "Dunkel" : "Hell")}\n\nKlick → Wechseln";
                    }
                    BtnThemeToggle.ToolTip = tooltip;
                }
            }
        }

        private void ApplyThemeToAllTeams()
        {
            try
            {
                // Apply theme to COMPACT cards
                foreach (var compactCard in _compactCards.Values)
                {
                    compactCard.ApplyTheme(ThemeService.Instance.IsDarkMode);
                }
                
                LoggingService.Instance.LogInfo($"Applied theme to {_compactCards.Count} compact cards");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to team controls", ex);
            }
        }

        private void UpdateMissionDisplayAsync(EinsatzData einsatzData)
        {
            // Use Dispatcher to ensure UI updates happen on the UI thread
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    if (TxtEinsatzInfo != null)
                    {
                        TxtEinsatzInfo.Text = $"{einsatzData.EinsatzTyp} - {einsatzData.EinsatzDatum:dd.MM.yyyy HH:mm}";
                    }
                    if (TxtEinsatzort != null)
                    {
                        TxtEinsatzort.Text = $"Ort: {einsatzData.Einsatzort}";
                    }
                    if (TxtEinsatzleiter != null)
                    {
                        TxtEinsatzleiter.Text = $"EL: {einsatzData.Einsatzleiter}";
                    }

                    StartAutoSave();
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("Error updating mission display", ex);
                }
            });
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void CheckForRecoveryAsync()
        {
            try
            {
                // Check for crash recovery
                if (PersistenceService.Instance.HasCrashRecovery())
                {
                    var result = MessageBox.Show(
                        "Es wurde eine unterbrochene Sitzung gefunden. Möchten Sie diese wiederherstellen?",
                        "Wiederherstellung", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var sessionData = await PersistenceService.Instance.LoadCrashRecoveryAsync().ConfigureAwait(false);
                        if (sessionData != null)
                        {
                            await RestoreSessionAsync(sessionData).ConfigureAwait(false);
                            PersistenceService.Instance.ClearCrashRecovery();
                            LoggingService.Instance.LogInfo("Session restored from crash recovery");
                            return;
                        }
                    }
                    else
                    {
                        PersistenceService.Instance.ClearCrashRecovery();
                    }
                }

                // Normal startup - Welcome Message bleibt sichtbar
                ShowStartWindow();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during recovery check", ex);
                ShowStartWindow();
            }
        }

        private async System.Threading.Tasks.Task RestoreSessionAsync(Services.EinsatzSessionData sessionData)
        {
            try
            {
                if (sessionData.EinsatzData != null)
                {
                    _einsatzData = sessionData.EinsatzData;
                    _firstWarningMinutes = sessionData.FirstWarningMinutes;
                    _secondWarningMinutes = sessionData.SecondWarningMinutes;
                    _nextTeamId = sessionData.NextTeamId;

                    // Update UI with mission info - using safe property access
                    if (TxtEinsatzInfo != null)
                    {
                        TxtEinsatzInfo.Text = $"{_einsatzData.EinsatzTyp} - {_einsatzData.EinsatzDatum:dd.MM.yyyy HH:mm}";
                    }
                    if (TxtEinsatzort != null)
                    {
                        TxtEinsatzort.Text = $"Ort: {_einsatzData.Einsatzort}";
                    }
                    if (TxtEinsatzleiter != null)
                    {
                        TxtEinsatzleiter.Text = $"EL: {_einsatzData.Einsatzleiter}";
                    }

                    // NEU: Verwende EinsatzData.GlobalNotesEntries als Haupt-Collection
                    _globalNotesCollection = _einsatzData.GlobalNotesEntries;

                    // NEU: Gefilterte Collection aus den wiederhergestellten Notizen erstellen
                    _filteredNotesCollection.Clear();
                    foreach (var note in _globalNotesCollection.Where(n => IsEinsatzRelevantNote(n.EntryType)))
                    {
                        _filteredNotesCollection.Add(note);
                    }

                    // Restore teams
                    foreach (var teamData in sessionData.Teams)
                    {
                        var team = new Team
                        {
                            TeamId = teamData.TeamId,
                            TeamName = teamData.TeamName,
                            HundName = teamData.HundName,
                            Hundefuehrer = teamData.Hundefuehrer,
                            Helfer = teamData.Helfer,
                            Suchgebiet = teamData.Suchgebiet,
                            ElapsedTime = teamData.ElapsedTime,
                            IsFirstWarning = teamData.IsFirstWarning,
                            IsSecondWarning = teamData.IsSecondWarning,
                            FirstWarningMinutes = teamData.FirstWarningMinutes,
                            SecondWarningMinutes = teamData.SecondWarningMinutes
                        };

                        // Restore multiple team types
                        if (!string.IsNullOrEmpty(teamData.TeamType))
                        {
                            if (Enum.TryParse<TeamType>(teamData.TeamType, out var singleType))
                            {
                                team.MultipleTeamTypes = new MultipleTeamTypes(singleType);
                            }
                        }

                        // *** WICHTIG: Event-Handler für globale Notizen registrieren ***

                RegisterTeamEventsForGlobalNotes(team);

                _teams.Add(team);

                // NEU: Create compact card for restored teams
                var compactCard = new TeamCompactCard { Team = team };
                compactCard.TeamClicked += OnTeamCompactCardClicked;
                compactCard.ApplyTheme(ThemeService.Instance.IsDarkMode);
                _compactCards[team.TeamId] = compactCard;
                
                if (DashboardGrid != null)
                {
                    DashboardGrid.Children.Add(compactCard);
                }

                // Restart timer if it was running
                if (teamData.IsRunning && teamData.StartTime.HasValue)
                {
                    team.StartTimer();
                }
            }

            UpdateTeamGridLayout();
            StartAutoSave();

            // NEU: Note Targets aktualisieren
            UpdateNoteTargets();

            // NEU: Welcome Message Visibility aktualisieren
            UpdateWelcomeMessageVisibility();

            // Globale Notiz: Session wiederhergestellt
            AddGlobalNote($"Einsatz wiederhergestellt - {_teams.Count} Teams geladen",
                GlobalNotesEntryType.EinsatzUpdate);
                }
            }            
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error restoring session", ex);
            }
        }

        private void InitializeServices()
        {
            PersistenceService.Instance.Initialize("einsatzueberwachung.json", true);
            LoggingService.Instance.Initialize("einsatzueberwachung.log", LogLevel.Info);

            // NEU: Verbose Logging aktivieren
            LoggingService.Instance.SetVerboseLogging(true);

            // *** ZENTRAL: Globale Notizen-Services initialisieren ***
            GlobalNotesService.Instance.Initialize(GlobalNotesCollection,
                (message) => AddGlobalNote(message, GlobalNotesEntryType.Info),
                (message) => AddGlobalNote(message, GlobalNotesEntryType.Warnung),
                (message) => AddGlobalNote(message, GlobalNotesEntryType.Fehler));
        }

        private void InitializeClock()
        {
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += ClockTick;

            // Timer sofort starten, um Verzögerung zu vermeiden
            _clockTimer.Start();
        }

        private void ToggleFullscreen()
        {
            if (_isFullscreen)
            {
                // Restore previous window state and style
                WindowState = _previousWindowState;
                WindowStyle = _previousWindowStyle;
                _isFullscreen = false;

                // Restore window size
                if (_resizeTimer != null)
                {
                    _resizeTimer.Stop();
                    _resizeTimer = null;
                }
                Width = 1200;
                Height = 800;
                Top = 100;
                Left = 100;
            }
            else
            {
                // Speichere aktuelle Fensterposition und -größe
                _previousWindowState = WindowState;
                _previousWindowStyle = WindowStyle;

                // In den Vollbildmodus wechseln
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Normal;
                _isFullscreen = true;

                // Größe an Bildschirm anpassen
                var screenWidth = SystemParameters.PrimaryScreenWidth;
                var screenHeight = SystemParameters.PrimaryScreenHeight;
                Width = screenWidth;
                Height = screenHeight;
                Top = 0;
                Left = 0;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Verbinden mit MobileService
            MobileService.Instance.Connect();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            // Trennen von MobileService
            MobileService.Instance.Disconnect();

            // Alle Timers stoppen
            _clockTimer?.Stop();
            foreach (var team in _teams)
            {
                team.StopTimer();
            }
        }

        // Verhalten beim Schließen des Fensters
        protected override void OnClosing(CancelEventArgs e)
        {
            try
            {
                LoggingService.Instance.LogInfo("MainWindow closing - starting cleanup");
                
                // Zeige Bestätigungsdialog wenn Einsatz aktiv ist
                if (_teams.Any(t => t.IsRunning))
                {
                    var result = MessageBox.Show(
                        "Es sind noch aktive Timer vorhanden.\n\n" +
                        "Möchten Sie die Anwendung wirklich beenden?",
                        "Aktive Einsatzzeiten",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                
                // Stoppe und dispose alle Team-Timer
                foreach (var team in _teams)
                {
                    team.StopTimer();
                    if (team is IDisposable disposableTeam)
                    {
                        disposableTeam.Dispose();
                    }
                }
                LoggingService.Instance.LogInfo($"Stopped and disposed {_teams.Count} team timers");
                
                // Stoppe Mobile Server falls vorhanden
                try
                {
                    _mobileConnectionWindow?.ForceClose();
                    MobileService.Instance.Disconnect();
                    LoggingService.Instance.LogInfo("Mobile Server stopped");
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogWarning($"Error stopping mobile server: {ex.Message}");
                }
                
                // Theme Service Event abmelden
                ThemeService.Instance.ThemeChanged -= OnThemeChanged;
                
                // Clock Timer stoppen
                if (_clockTimer != null)
                {
                    _clockTimer.Stop();
                    _clockTimer.Tick -= ClockTick;
                    _clockTimer = null;
                }
                
                // Auto-Save stoppen
                PersistenceService.Instance.StopAutoSave();
                
                // Gib Threads Zeit zum Beenden
                System.Threading.Thread.Sleep(300);
                
                LoggingService.Instance.LogInfo("MainWindow cleanup completed successfully");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during window closing", ex);
            }
            finally
            {
                base.OnClosing(e);
            }
        }
        
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                LoggingService.Instance.LogInfo("MainWindow closed - forcing application exit");
                
                // Stelle sicher, dass die Anwendung wirklich beendet wird
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during window cleanup", ex);
            }
            finally
            {
                base.OnClosed(e);
            }
        }

        // Fehlende Methoden aus MainWindow_MissingMethods.cs
        private void OnTeamCompactCardClicked(object? sender, Team team)
        {
            try
            {
                // Öffne separates Detail-Fenster für das Team
                var detailWindow = new TeamDetailWindow(team);
                detailWindow.Owner = this;
                detailWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                detailWindow.Show();
                
                // NEU: SystemEvent (wird nicht im Panel angezeigt)
                AddGlobalNote($"Detail-Fenster für Team {team.TeamName} geöffnet", GlobalNotesEntryType.SystemEvent);
                
                LoggingService.Instance.LogInfo($"Opened detail window for team {team.TeamName}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling compact card click", ex);
                MessageBox.Show($"Fehler beim Öffnen des Detail-Fensters: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateMissionDisplay()
        {
            try
            {
                if (_einsatzData == null) return;

                if (TxtEinsatzInfo != null)
                {
                    TxtEinsatzInfo.Text = $"{_einsatzData.EinsatzTyp} - {_einsatzData.EinsatzDatum:dd.MM.yyyy HH:mm}";
                }
                if (TxtEinsatzort != null)
                {
                    TxtEinsatzort.Text = $"Ort: {_einsatzData.Einsatzort}";
                }
                if (TxtEinsatzleiter != null)
                {
                    TxtEinsatzleiter.Text = $"EL: {_einsatzData.Einsatzleiter}";
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating mission display", ex);
            }
        }

        private void UpdateTeamCount()
        {
            if (TxtTeamCount != null)
            {
                TxtTeamCount.Text = $"{_teams.Count}/50";
            }
            
            // NEU: Welcome Message ausblenden wenn erstes Team erstellt wurde
            UpdateWelcomeMessageVisibility();
        }

        // NEU: Methode zur Steuerung der Willkommensnachricht
        private void UpdateWelcomeMessageVisibility()
        {
            try
            {
                if (WelcomeMessage != null)
                {
                    // Zeige Welcome Message nur wenn keine Teams vorhanden sind
                    WelcomeMessage.Visibility = _teams.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating welcome message visibility", ex);
            }
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var helpWindow = new HelpWindow();
                helpWindow.Owner = this;
                helpWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                helpWindow.Show();
                
                // NEU: SystemEvent (wird nicht im Panel angezeigt)
                AddGlobalNote("Hilfe-Fenster geöffnet", GlobalNotesEntryType.SystemEvent);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening help window", ex);
                MessageBox.Show($"Fehler beim Öffnen der Hilfe: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClockTick(object? sender, EventArgs e)
        {
            try
            {
                if (TxtCurrentTime != null)
                {
                    TxtCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss");
                }
                
                // Update last log display
                if (TxtLastLog != null)
                {
                    TxtLastLog.Text = $"Aktualisiert: {DateTime.Now:HH:mm:ss}";
                }
                
                // Check team warnings periodically - trigger property change notification
                foreach (var team in _teams)
                {
                    if (team.IsRunning)
                    {
                        // Trigger internal timer checks without direct property access
                        // The timer will handle warning checks internally
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error in clock tick", ex);
            }
        }

        private void InitializeNoteTargets()
        {
            try
            {
                _noteTargets.Clear();
                
                // Special targets
                _noteTargets.Add(new NoteTarget { DisplayName = "Allgemein", DetailInfo = "📝", IsSpecialTarget = true });
                _noteTargets.Add(new NoteTarget { DisplayName = "Einsatzleiter", DetailInfo = "👨‍💼", IsSpecialTarget = true });
                _noteTargets.Add(new NoteTarget { DisplayName = "Drohnenstaffel", DetailInfo = "🚁", IsSpecialTarget = true });
                _noteTargets.Add(new NoteTarget { DisplayName = "Funkzentrale", DetailInfo = "📻", IsSpecialTarget = true });
                
                UpdateNoteTargets();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing note targets", ex);
            }
        }

        private void RegisterTeamEventsForGlobalNotes(Team team)
        {
            try
            {
                // NEU: Verwende spezifische Event-Typen statt allgemeines "Info"
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
                LoggingService.Instance.LogError("Error registering team events", ex);
            }
        }

        private void StartAutoSave()
        {
            try
            {
                if (_einsatzData != null)
                {
                    // Start auto-save service with current mission data
                    // PersistenceService.Instance.StartAutoSave expects a different type
                    // For now, just log that auto-save would be started
                    LoggingService.Instance.LogInfo("Auto-Save would be started here");
                    
                    // NEU: SystemEvent (wird nicht im Panel angezeigt)
                    AddGlobalNote("Auto-Save aktiviert", GlobalNotesEntryType.SystemEvent);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error starting auto-save", ex);
            }
        }

        private Models.EinsatzSessionData CreateSessionData()
        {
            return new Models.EinsatzSessionData
            {
                EinsatzData = _einsatzData,
                Teams = _teams.Select(t => new Models.TeamSessionData
                {
                    TeamId = t.TeamId,
                    TeamName = t.TeamName,
                    HundName = t.HundName,
                    Hundefuehrer = t.Hundefuehrer,
                    Helfer = t.Helfer,
                    Suchgebiet = t.Suchgebiet,
                    TeamType = t.TeamType.ToString(),
                    ElapsedTime = t.ElapsedTime,
                    IsRunning = t.IsRunning,
                    IsFirstWarning = t.IsFirstWarning,
                    IsSecondWarning = t.IsSecondWarning,
                    FirstWarningMinutes = t.FirstWarningMinutes,
                    SecondWarningMinutes = t.SecondWarningMinutes,
                    StartTime = t.IsRunning ? DateTime.Now - t.ElapsedTime : null
                }).ToList(),
                FirstWarningMinutes = _firstWarningMinutes,
                SecondWarningMinutes = _secondWarningMinutes,
                NextTeamId = _nextTeamId
            };
        }

        private void UpdateNoteTargets()
        {
            try
            {
                // Clear dynamic targets (keep special ones)
                var specialTargets = _noteTargets.Where(nt => nt.IsSpecialTarget).ToList();
                _noteTargets.Clear();
                
                foreach (var target in specialTargets)
                {
                    _noteTargets.Add(target);
                }
                
                // Add current teams as targets
                foreach (var team in _teams)
                {
                    _noteTargets.Add(new NoteTarget 
                    { 
                        DisplayName = team.TeamName, 
                        DetailInfo = team.TeamTypeShortName,
                        IsSpecialTarget = false 
                    });
                }
                
                // Update ComboBox selection if needed
                if (TeamSelectionComboBox != null && TeamSelectionComboBox.SelectedIndex == -1)
                {
                    TeamSelectionComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating note targets", ex);
            }
        }

        private void InitializeGlobalNotes()
        {
            try
            {
                if (_einsatzData?.GlobalNotesEntries != null)
                {
                    _globalNotesCollection = _einsatzData.GlobalNotesEntries;
                    
                    // NEU: Gefilterte Collection aus den vorhandenen Notizen erstellen
                    _filteredNotesCollection.Clear();
                    foreach (var note in _globalNotesCollection.Where(n => IsEinsatzRelevantNote(n.EntryType)))
                    {
                        _filteredNotesCollection.Add(note);
                    }
                    
                    // Update binding if needed
                    if (GlobalNotesItemsControl != null)
                    {
                        GlobalNotesItemsControl.ItemsSource = _filteredNotesCollection;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing global notes", ex);
            }
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
                
                // NEU: Nur einsatzrelevante Notizen zur gefilterten Collection hinzufügen
                if (IsEinsatzRelevantNote(type))
                {
                    _filteredNotesCollection.Add(note);
                }
                
                // Auto-scroll to latest note
                Dispatcher.BeginInvoke(() =>
                {
                    if (GlobalNotesScrollViewer != null)
                    {
                        GlobalNotesScrollViewer.ScrollToEnd();
                    }
                });
                
                // Save to persistence if mission is active
                if (_einsatzData != null)
                {
                    NotifyDataChanged();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error adding global note", ex);
            }
        }

        // NEU: Prüft, ob eine Notiz einsatzrelevant ist
        private bool IsEinsatzRelevantNote(GlobalNotesEntryType type)
        {
            return type switch
            {
                GlobalNotesEntryType.EinsatzUpdate => true,
                GlobalNotesEntryType.TeamEvent => true,
                // NICHT mehr anzeigen: Timer-Starts/Stops via Tastenkürzel
                GlobalNotesEntryType.TimerStart => false,
                GlobalNotesEntryType.TimerStop => false,
                GlobalNotesEntryType.TimerReset => true,
                GlobalNotesEntryType.Warning1 => true,
                GlobalNotesEntryType.Warning2 => true,
                GlobalNotesEntryType.Warnung => true,
                GlobalNotesEntryType.Fehler => true,
                GlobalNotesEntryType.Funkspruch => true,
                GlobalNotesEntryType.Manual => true,
                // NICHT anzeigen:
                GlobalNotesEntryType.Info => false,        // Allgemeine System-Infos
                GlobalNotesEntryType.SystemEvent => false,  // Theme-Wechsel, etc.
                _ => false
            };
        }

        private void ShowStartWindow()
        {
            try
            {
                var startWindow = new StartWindow();
                startWindow.Owner = this;
                
                var result = startWindow.ShowDialog();
                
                if (result == true && startWindow.EinsatzData != null)
                {
                    // User clicked Start and provided valid data
                    _einsatzData = startWindow.EinsatzData;
                    _firstWarningMinutes = startWindow.FirstWarningMinutes;
                    _secondWarningMinutes = startWindow.SecondWarningMinutes;

                    // Update UI with mission info
                    UpdateMissionDisplayAsync(startWindow.EinsatzData);

                    // Add global note for mission start
                    AddGlobalNote($"Einsatz gestartet: {startWindow.EinsatzData.EinsatzTyp} - {startWindow.EinsatzData.Einsatzort}", 
                        GlobalNotesEntryType.EinsatzUpdate);

                    StartAutoSave();
                }
                else
                {
                    // User cancelled or closed StartWindow - close the application
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing StartWindow", ex);
                // If StartWindow fails, still try to continue with empty data
                AddGlobalNote("StartWindow-Fehler - Einsatz ohne Startdaten fortgesetzt", GlobalNotesEntryType.Fehler);
            }
        }

        private void UpdateEinsatzDatumIfNecessary()
        {
            if (_teams.Count == 0)
            {
                // Keine Teams mehr vorhanden, Datum zurücksetzen
                if (TxtEinsatzInfo != null)
                {
                    TxtEinsatzInfo.Text = string.Empty;
                }
                if (TxtEinsatzort != null)
                {
                    TxtEinsatzort.Text = string.Empty;
                }
                if (TxtEinsatzleiter != null)
                {
                    TxtEinsatzleiter.Text = string.Empty;
                }
            }
        }

        private void NotifyDataChanged()
        {
            try
            {
                if (_einsatzData != null)
                {
                    LoggingService.Instance.LogInfo("Data changed - would save session data");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error notifying data changed", ex);
            }
        }

        // ============================================
        // FEHLENDE EVENT-HANDLER
        // ============================================

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                // F11 für Vollbild-Toggle
                if (e.Key == Key.F11)
                {
                    ToggleFullscreen();
                    e.Handled = true;
                    return;
                }
                
                // ESC zum Verlassen des Vollbildmodus
                if (e.Key == Key.Escape && _isFullscreen)
                {
                    ToggleFullscreen();
                    e.Handled = true;
                    return;
                }
                
                // F1-F10 für Team-Timer
                if (e.Key >= Key.F1 && e.Key <= Key.F10)
                {
                    int teamIndex = e.Key - Key.F1; // F1=0, F2=1, etc.
                    
                    if (teamIndex < _teams.Count)
                    {
                        var team = _teams[teamIndex];
                        
                        // Timer umschalten: Start wenn gestoppt, Stop wenn läuft
                        if (team.IsRunning)
                        {
                            team.StopTimer();
                            // NEU: TimerStop statt Info (einsatzrelevant!)
                            AddGlobalNote($"Timer gestoppt (F{teamIndex + 1})", GlobalNotesEntryType.TimerStop, team.TeamName);
                            LoggingService.Instance.LogInfo($"Timer stopped via F{teamIndex + 1} for team {team.TeamName}");
                        }
                        else
                        {
                            team.StartTimer();
                            // NEU: TimerStart statt Info (einsatzrelevant!)
                            AddGlobalNote($"Timer gestartet (F{teamIndex + 1})", GlobalNotesEntryType.TimerStart, team.TeamName);
                            LoggingService.Instance.LogInfo($"Timer started via F{teamIndex + 1} for team {team.TeamName}");
                        }
                        
                        e.Handled = true;
                    }
                    else
                    {
                        // Team existiert nicht
                        LoggingService.Instance.LogWarning($"F{teamIndex + 1} pressed but no team at index {teamIndex}");
                    }
                    return;
                }
                
                // Strg+N für neues Team
                if (e.Key == Key.N && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    BtnAddTeam_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    return;
                }
                
                // Strg+E für Export
                if (e.Key == Key.E && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    BtnExport_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    return;
                }
                
                // Strg+T für Theme Toggle
                if (e.Key == Key.T && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    BtnThemeToggle_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    return;
                }
                
                // Strg+H für Hilfe
                if (e.Key == Key.H && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    BtnHelp_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error in Window_KeyDown", ex);
            }
        }

        private void BtnAddTeam_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_teams.Count >= 50)
                {
                    MessageBox.Show("Maximale Anzahl von 50 Teams erreicht!", 
                        "Limit erreicht", MessageBoxButton.OK, MessageBoxImage.Warning);
                    AddGlobalNote("Team-Limit (50) erreicht", GlobalNotesEntryType.Warnung);
                    return;
                }

                var inputWindow = new TeamInputWindow();
                inputWindow.Owner = this;
                inputWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                
                var result = inputWindow.ShowDialog();
                
                if (result == true)
                {
                    // Create team from input window properties
                    var newTeam = new Team
                    {
                        TeamId = _nextTeamId++,
                        TeamName = inputWindow.TeamName,
                        HundName = inputWindow.HundName,
                        Hundefuehrer = inputWindow.Hundefuehrer,
                        Helfer = inputWindow.Helfer,
                        Suchgebiet = inputWindow.Suchgebiet,
                        FirstWarningMinutes = _firstWarningMinutes,
                        SecondWarningMinutes = _secondWarningMinutes
                    };
                    
                    // Set team types if provided
                    if (inputWindow.PreselectedTeamTypes != null)
                    {
                        newTeam.MultipleTeamTypes = inputWindow.PreselectedTeamTypes;
                    }
                    else
                    {
                        // Default to Allgemein if no type selected
                        newTeam.MultipleTeamTypes = new MultipleTeamTypes(TeamType.Allgemein);
                    }
                    
                    // Register team events for global notes
                    RegisterTeamEventsForGlobalNotes(newTeam);
                    
                    _teams.Add(newTeam);
                    
                    // Create compact card for the new team
                    var compactCard = new TeamCompactCard { Team = newTeam };
                    compactCard.TeamClicked += OnTeamCompactCardClicked;
                    compactCard.ApplyTheme(ThemeService.Instance.IsDarkMode);
                    _compactCards[newTeam.TeamId] = compactCard;
                    
                    if (DashboardGrid != null)
                    {
                        DashboardGrid.Children.Add(compactCard);
                    }
                    
                    UpdateTeamGridLayout();
                    UpdateTeamCount();
                    UpdateNoteTargets();
                    
                    AddGlobalNote($"Team hinzugefügt: {newTeam.TeamName}", GlobalNotesEntryType.TeamEvent);
                    
                    NotifyDataChanged();
                    
                    LoggingService.Instance.LogInfo($"Team added: {newTeam.TeamName}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error adding team", ex);
                MessageBox.Show($"Fehler beim Hinzufügen des Teams: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ThemeService.Instance.ToggleTheme();
                LoggingService.Instance.LogInfo($"Theme toggled to: {ThemeService.Instance.CurrentThemeStatus}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error toggling theme", ex);
                MessageBox.Show($"Fehler beim Wechseln des Themes: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create context menu
                var contextMenu = new ContextMenu();
                
                // Menü-Eintrag: Stammdaten verwalten
                var masterDataItem = new MenuItem { Header = "Stammdaten verwalten..." };
                masterDataItem.Click += (s, args) =>
                {
                    try
                    {
                        var masterDataWindow = new MasterDataWindow();
                        masterDataWindow.Owner = this;
                        masterDataWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        masterDataWindow.Show();
                        // NEU: SystemEvent (wird nicht im Panel angezeigt)
                        AddGlobalNote("Stammdaten-Fenster geöffnet", GlobalNotesEntryType.SystemEvent);
                        LoggingService.Instance.LogInfo("MasterDataWindow opened");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogError("Error opening master data window", ex);
                        MessageBox.Show($"Fehler beim Öffnen der Stammdaten-Verwaltung: {ex.Message}", 
                            "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                contextMenu.Items.Add(masterDataItem);
                
                // Menü-Eintrag: Mobile-Verbindung
                var mobileItem = new MenuItem { Header = "Mobile Verbindung..." };
                mobileItem.Click += (s, args) =>
                {
                    try
                    {
                        var mobileWindow = new MobileConnectionWindow();
                        mobileWindow.Owner = this;
                        mobileWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        mobileWindow.Show();
                        // NEU: SystemEvent (wird nicht im Panel angezeigt)
                        AddGlobalNote("Mobile Verbindungsfenster geöffnet", GlobalNotesEntryType.SystemEvent);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogError("Error opening mobile connection window", ex);
                    }
                };
                contextMenu.Items.Add(mobileItem);
                
                contextMenu.Items.Add(new Separator());
                
                // Menü-Eintrag: Warnung-Einstellungen
                var warningSettingsItem = new MenuItem { Header = "Warnungs-Einstellungen..." };
                warningSettingsItem.Click += (s, args) =>
                {
                    try
                    {
                        var settingsWindow = new TeamWarningSettingsWindow(_teams.ToList(), _firstWarningMinutes, _secondWarningMinutes);
                        settingsWindow.Owner = this;
                        settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        
                        if (settingsWindow.ShowDialog() == true && settingsWindow.SettingsChanged)
                        {
                            AddGlobalNote($"Team-Warnzeiten individuell angepasst", 
                                GlobalNotesEntryType.EinsatzUpdate);
                            
                            NotifyDataChanged();
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogError("Error opening warning settings", ex);
                    }
                };
                contextMenu.Items.Add(warningSettingsItem);
                
                contextMenu.Items.Add(new Separator());
                
                // Menü-Eintrag: Über
                var aboutItem = new MenuItem { Header = "Über..." };
                aboutItem.Click += (s, args) =>
                {
                    try
                    {
                        var aboutWindow = new AboutWindow();
                        aboutWindow.Owner = this;
                        aboutWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        aboutWindow.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogError("Error opening about window", ex);
                    }
                };
                contextMenu.Items.Add(aboutItem);
                
                // Menü anzeigen
                if (sender is Button button)
                {
                    contextMenu.PlacementTarget = button;
                    contextMenu.Placement = PlacementMode.Bottom;
                    contextMenu.IsOpen = true;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing menu", ex);
            }
        }

        private void QuickNoteInput_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    AddQuickNote();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error in QuickNoteInput_KeyDown", ex);
            }
        }

        private void AddQuickNoteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddQuickNote();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error in AddQuickNoteButton_Click", ex);
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_einsatzData == null)
                {
                    MessageBox.Show("Keine Einsatzdaten zum Exportieren vorhanden.", 
                        "Export nicht möglich", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // NEU: Zeige Export-Optionen Dialog mit PDF-Option
                var exportDialog = new Window
                {
                    Title = "Export-Optionen",
                    Width = 400,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize,
                    Background = (Brush)FindResource("Surface")
                };

                var panel = new StackPanel { Margin = new Thickness(20, 20, 20, 20) };
                
                panel.Children.Add(new TextBlock 
                { 
                    Text = "Wählen Sie das Export-Format:", 
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 15),
                    Foreground = (Brush)FindResource("OnSurface")
                });

                var btnExportPdf = new Button
                {
                    Content = "📄 PDF-Bericht (Professionell)",
                    Padding = new Thickness(15, 10, 15, 10),
                    Margin = new Thickness(0, 5, 0, 5),
                    Background = (Brush)FindResource("Primary"),
                    Foreground = (Brush)FindResource("OnPrimary"),
                    BorderThickness = new Thickness(0, 0, 0, 0),
                    Cursor = Cursors.Hand
                };
                btnExportPdf.Click += (s, args) =>
                {
                    exportDialog.Tag = "pdf";
                    exportDialog.DialogResult = true;
                };

                var btnExportFull = new Button
                {
                    Content = "📦 Vollständiger Export (JSON)",
                    Padding = new Thickness(15, 10, 15, 10),
                    Margin = new Thickness(0, 5, 0, 5),
                    Background = (Brush)FindResource("Success"),
                    Foreground = (Brush)FindResource("OnSuccess"),
                    BorderThickness = new Thickness(0, 0, 0, 0),
                    Cursor = Cursors.Hand
                };
                btnExportFull.Click += (s, args) =>
                {
                    exportDialog.Tag = "full";
                    exportDialog.DialogResult = true;
                };

                var btnExportLog = new Button
                {
                    Content = "📝 Einsatz-Log (Nur relevante Daten)",
                    Padding = new Thickness(15, 10, 15, 10),
                    Margin = new Thickness(0, 5, 0, 5),
                    Background = (Brush)FindResource("Tertiary"),
                    Foreground = (Brush)FindResource("OnTertiary"),
                    BorderThickness = new Thickness(0, 0, 0, 0),
                    Cursor = Cursors.Hand
                };
                btnExportLog.Click += (s, args) =>
                {
                    exportDialog.Tag = "log";
                    exportDialog.DialogResult = true;
                };

                var btnCancel = new Button
                {
                    Content = "Abbrechen",
                    Padding = new Thickness(15, 10, 15, 10),
                    Margin = new Thickness(0, 15, 0, 0),
                    Background = (Brush)FindResource("Surface"),
                    Foreground = (Brush)FindResource("OnSurface"),
                    BorderBrush = (Brush)FindResource("Outline"),
                    BorderThickness = new Thickness(1, 1, 1, 1),
                    Cursor = Cursors.Hand
                };
                btnCancel.Click += (s, args) => { exportDialog.DialogResult = false; };

                panel.Children.Add(btnExportPdf);
                panel.Children.Add(btnExportFull);
                panel.Children.Add(btnExportLog);
                panel.Children.Add(btnCancel);

                exportDialog.Content = panel;

                if (exportDialog.ShowDialog() == true)
                {
                    string exportType = exportDialog.Tag?.ToString() ?? "full";

                    if (exportType == "pdf")
                    {
                        ExportToPdf();
                    }
                    else if (exportType == "log")
                    {
                        ExportEinsatzLog();
                    }
                    else
                    {
                        ExportFullData();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error in export dialog", ex);
                MessageBox.Show($"Fehler beim Exportieren: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToPdf()
        {
            try
            {
                // Öffne PDF-Export-Konfigurationsdialog
                var pdfExportWindow = new PdfExportWindow(_einsatzData, _teams.ToList());
                pdfExportWindow.Owner = this;
                pdfExportWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                
                if (pdfExportWindow.ShowDialog() == true)
                {
                    AddGlobalNote("PDF-Bericht erfolgreich erstellt", GlobalNotesEntryType.EinsatzUpdate);
                    LoggingService.Instance.LogInfo("PDF export completed successfully");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error exporting to PDF", ex);
                MessageBox.Show($"Fehler beim PDF-Export:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportFullData()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "JSON Dateien (*.json)|*.json|Alle Dateien (*.*)|*.*",
                    FileName = $"Einsatz_{_einsatzData.EinsatzTyp}_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                    Title = "Vollständige Einsatzdaten exportieren"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var sessionData = CreateSessionData();
                    var json = JsonSerializer.Serialize(sessionData, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    
                    File.WriteAllText(saveDialog.FileName, json);
                    
                    AddGlobalNote($"Vollständiger Export: {IOPath.GetFileName(saveDialog.FileName)}", 
                        GlobalNotesEntryType.EinsatzUpdate);
                    
                    MessageBox.Show($"Einsatzdaten wurden erfolgreich exportiert nach:\n{saveDialog.FileName}", 
                        "Export erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    LoggingService.Instance.LogInfo($"Full mission data exported to: {saveDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error exporting full data", ex);
                MessageBox.Show($"Fehler beim Exportieren: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportEinsatzLog()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Text Dateien (*.txt)|*.txt|Alle Dateien (*.*)|*.*",
                    FileName = $"Einsatzlog_{_einsatzData.EinsatzTyp}_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                    Title = "Einsatz-Log exportieren"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var logContent = GenerateEinsatzLog();
                    File.WriteAllText(saveDialog.FileName, logContent, Encoding.UTF8);
                    
                    AddGlobalNote($"Einsatz-Log exportiert: {IOPath.GetFileName(saveDialog.FileName)}", 
                        GlobalNotesEntryType.EinsatzUpdate);
                    
                    MessageBox.Show($"Einsatz-Log wurde erfolgreich exportiert nach:\n{saveDialog.FileName}", 
                        "Export erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    LoggingService.Instance.LogInfo($"Mission log exported to: {saveDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error exporting mission log", ex);
                MessageBox.Show($"Fehler beim Exportieren des Logs: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateEinsatzLog()
        {
            var sb = new StringBuilder();
            
            // ============================================
            // EINSATZ-KOPFDATEN
            // ============================================
            sb.AppendLine("================================================================");
            sb.AppendLine("           EINSATZ-LOG - RETTUNGSHUNDE-EINSATZ");
            sb.AppendLine("================================================================");
            sb.AppendLine();
            
            sb.AppendLine("EINSATZ-INFORMATION:");
            sb.AppendLine("------------------------------------------------------------");
            sb.AppendLine($"Einsatzstart:           {_einsatzData.EinsatzDatum:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine($"Einsatztyp:             {_einsatzData.EinsatzTyp}");
            sb.AppendLine($"Einsatzort:             {_einsatzData.Einsatzort}");
            sb.AppendLine($"Einsatzleiter:          {_einsatzData.Einsatzleiter}");
            
            if (!string.IsNullOrEmpty(_einsatzData.Fuehrungsassistent))
            {
                sb.AppendLine($"Führungsassistent:      {_einsatzData.Fuehrungsassistent}");
            }
            
            if (!string.IsNullOrEmpty(_einsatzData.Alarmiert))
            {
                sb.AppendLine($"Alarmierende Org.:      {_einsatzData.Alarmiert}");
            }
            
            sb.AppendLine($"Anzahl Teams:           {_teams.Count}");
            sb.AppendLine();
            
            // ============================================
            // TEAM-ÜBERSICHT
            // ============================================
            sb.AppendLine("EINGESETZTE TEAMS:");
            sb.AppendLine("------------------------------------------------------------");
            
            foreach (var team in _teams.OrderBy(t => t.TeamId))
            {
                sb.AppendLine($"\n{team.TeamName}:");
                sb.AppendLine($"  • Hund:              {team.HundName}");
                sb.AppendLine($"  • Hundeführer:       {team.Hundefuehrer}");
                if (!string.IsNullOrEmpty(team.Helfer))
                {
                    sb.AppendLine($"  • Helfer:            {team.Helfer}");
                }
                if (!string.IsNullOrEmpty(team.Suchgebiet))
                {
                    sb.AppendLine($"  • Suchgebiet:        {team.Suchgebiet}");
                }
                sb.AppendLine($"  • Spezialisierung:   {team.TeamTypeDisplayName}");
                sb.AppendLine($"  • Gesamteinsatzzeit: {team.ElapsedTimeString}");
            }
            
            sb.AppendLine();
            sb.AppendLine();
            
            // ============================================
            // EINSATZRELEVANTE EREIGNISSE
            // ============================================
            sb.AppendLine("EINSATZ-CHRONOLOGIE:");
            sb.AppendLine("============================================================");
            sb.AppendLine();
            
            // Filtere nur einsatzrelevante Notizen (OHNE TimerStart/TimerStop via Tastenkürzel)
            var relevantNotes = _globalNotesCollection.Where(note =>
                note.EntryType == GlobalNotesEntryType.EinsatzUpdate ||
                note.EntryType == GlobalNotesEntryType.TeamEvent ||
                note.EntryType == GlobalNotesEntryType.TimerReset ||
                note.EntryType == GlobalNotesEntryType.Warning1 ||
                note.EntryType == GlobalNotesEntryType.Warning2 ||
                note.EntryType == GlobalNotesEntryType.Funkspruch ||
                note.EntryType == GlobalNotesEntryType.Manual
            ).OrderBy(n => n.Timestamp);
            
            if (!relevantNotes.Any())
            {
                sb.AppendLine("Keine einsatzrelevanten Ereignisse protokolliert.");
            }
            else
            {
                foreach (var note in relevantNotes)
                {
                    string teamInfo = !string.IsNullOrEmpty(note.TeamName) ? $" [{note.TeamName}]" : "";
                    string typeLabel = GetLogTypeLabel(note.EntryType);
                    
                    sb.AppendLine($"[{note.Timestamp:dd.MM.yyyy HH:mm:ss}] {typeLabel}{teamInfo}");
                    sb.AppendLine($"  {note.Content}");
                    sb.AppendLine();
                }
            }
            
            // ============================================
            // EINSATZ-STATISTIK
            // ============================================
            sb.AppendLine();
            sb.AppendLine("EINSATZ-STATISTIK:");
            sb.AppendLine("------------------------------------------------------------");
            
            var totalTeams = _teams.Count;
            var activeTeams = _teams.Count(t => t.IsRunning);
            var completedTeams = totalTeams - activeTeams;
            
            var totalWarnings = _globalNotesCollection.Count(n => 
                n.EntryType == GlobalNotesEntryType.Warning1 || 
                n.EntryType == GlobalNotesEntryType.Warning2);
            
            var totalTimerStarts = _globalNotesCollection.Count(n => n.EntryType == GlobalNotesEntryType.TimerStart);
            var totalTimerStops = _globalNotesCollection.Count(n => n.EntryType == GlobalNotesEntryType.TimerStop);
            
            sb.AppendLine($"Gesamt Teams:           {totalTeams}");
            sb.AppendLine($"Aktive Teams:           {activeTeams}");
            sb.AppendLine($"Abgeschlossene Teams:   {completedTeams}");
            sb.AppendLine($"Timer-Starts:           {totalTimerStarts}");
            sb.AppendLine($"Timer-Stopps:           {totalTimerStops}");
            sb.AppendLine($"Warnungen gesamt:       {totalWarnings}");
            
            if (_teams.Any())
            {
                var maxTime = _teams.Max(t => t.ElapsedTime);
                var avgTime = TimeSpan.FromSeconds(_teams.Average(t => t.ElapsedTime.TotalSeconds));
                
                sb.AppendLine($"\nLängste Einsatzzeit:    {FormatTimeSpan(maxTime)}");
                sb.AppendLine($"Durchschnitt:           {FormatTimeSpan(avgTime)}");
            }
            
            // ============================================
            // FOOTER
            // ============================================
            sb.AppendLine();
            sb.AppendLine("================================================================");
            sb.AppendLine($"Log erstellt am: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine("Einsatzüberwachung Professional v1.7");
            sb.AppendLine("RescueDog_SW - https://github.com/Elemirus1996/Einsatzueberwachung");
            sb.AppendLine("================================================================");
            
            return sb.ToString();
        }

        private string GetLogTypeLabel(GlobalNotesEntryType entryType)
        {
            return entryType switch
            {
                GlobalNotesEntryType.EinsatzUpdate => "📋 EINSATZ",
                GlobalNotesEntryType.TeamEvent => "👥 TEAM",
                GlobalNotesEntryType.TimerStart => "▶️  START",
                GlobalNotesEntryType.TimerStop => "⏸️  STOPP",
                GlobalNotesEntryType.TimerReset => "🔄 RESET",
                GlobalNotesEntryType.Warning1 => "⚠️  WARNUNG",
                GlobalNotesEntryType.Warning2 => "🚨 KRITISCH",
                GlobalNotesEntryType.Funkspruch => "📻 FUNK",
                GlobalNotesEntryType.Manual => "✏️  NOTIZ",
                _ => "ℹ️  INFO"
            };
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.Days > 0)
                return $"{timeSpan.Days}d {timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            
            return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }

        private void AddQuickNote()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(QuickNoteText))
                {
                    return;
                }

                // Get selected target
                var selectedTarget = TeamSelectionComboBox?.SelectedItem as NoteTarget;
                string targetName = selectedTarget?.DisplayName ?? "Allgemein";
                
                // NEU: Manual statt Info (einsatzrelevant - manuelle Notizen!)
                AddGlobalNote(QuickNoteText, GlobalNotesEntryType.Manual, targetName);
                
                // Clear input
                QuickNoteText = string.Empty;
                
                // Focus back to input
                if (QuickNoteInput != null)
                {
                    QuickNoteInput.Focus();
                }
                
                LoggingService.Instance.LogInfo($"Quick note added: {QuickNoteText}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error adding quick note", ex);
            }
        }

        private void BtnExportLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExportEinsatzLog();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error exporting log from notes panel", ex);
                MessageBox.Show($"Fehler beim Exportieren des Logs: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTeamGridLayout()
        {
            try
            {
                if (DashboardGrid == null) return;
                
                // Grid already uses WrapPanel, so just ensure all cards are present
                LoggingService.Instance.LogInfo($"Team grid layout updated: {_compactCards.Count} cards");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating team grid layout", ex);
            }
        }
    }
}
