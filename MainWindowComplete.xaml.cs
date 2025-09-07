using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Input;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung
{
    public partial class MainWindow : Window
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
        private DispatcherTimer? _layoutUpdateTimer;

        public MainWindow()
        {
            InitializeComponent();
            _teams = new ObservableCollection<Team>();
            
            InitializeServices();
            InitializeClock();
            CheckForRecoveryAsync();
        }

        private async void CheckForRecoveryAsync()
        {
            try
            {
                if (PersistenceService.Instance.HasCrashRecovery())
                {
                    var result = MessageBox.Show(
                        "Es wurde eine unterbrochene Sitzung gefunden. Möchten Sie diese wiederherstellen?",
                        "Wiederherstellung", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var sessionData = await PersistenceService.Instance.LoadCrashRecoveryAsync();
                        if (sessionData != null)
                        {
                            await RestoreSessionAsync(sessionData);
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

                ShowStartWindow();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during recovery check", ex);
                ShowStartWindow();
            }
        }

        private async System.Threading.Tasks.Task RestoreSessionAsync(EinsatzSessionData sessionData)
        {
            try
            {
                if (sessionData.EinsatzData != null)
                {
                    _einsatzData = sessionData.EinsatzData;
                    _firstWarningMinutes = sessionData.FirstWarningMinutes;
                    _secondWarningMinutes = sessionData.SecondWarningMinutes;
                    _nextTeamId = sessionData.NextTeamId;

                    TxtEinsatzInfo.Text = $"{_einsatzData.EinsatzTyp} - {_einsatzData.EinsatzDatum:dd.MM.yyyy HH:mm}";
                    TxtEinsatzort.Text = $"Ort: {_einsatzData.Einsatzort}";
                    TxtEinsatzleiter.Text = $"EL: {_einsatzData.Einsatzleiter}";

                    foreach (var teamData in sessionData.Teams)
                    {
                        var team = new Team
                        {
                            TeamId = teamData.TeamId,
                            TeamName = teamData.TeamName,
                            TeamType = (TeamType)Enum.Parse(typeof(TeamType), teamData.TeamType ?? "Allgemein"),
                            HundName = teamData.HundName,
                            Hundefuehrer = teamData.Hundefuehrer,
                            Helfer = teamData.Helfer,
                            Notizen = teamData.Notizen,
                            ElapsedTime = teamData.ElapsedTime,
                            IsFirstWarning = teamData.IsFirstWarning,
                            IsSecondWarning = teamData.IsSecondWarning,
                            FirstWarningMinutes = teamData.FirstWarningMinutes,
                            SecondWarningMinutes = teamData.SecondWarningMinutes
                        };

                        _teams.Add(team);

                        var teamControl = new TeamControl { Team = team };
                        teamControl.TeamDeleteRequested += OnTeamDeleteRequested;
                        teamControl.ApplyTheme(ThemeService.Instance.IsDarkMode);
                        TeamsGrid.Children.Add(teamControl);

                        if (teamData.IsRunning && teamData.StartTime.HasValue)
                        {
                            team.StartTimer();
                        }
                    }

                    UpdateTeamGridLayout();
                    StartAutoSave();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error restoring session", ex);
                throw;
            }
        }

        private void InitializeServices()
        {
            try
            {
                PerformanceService.Instance.LogPerformanceMetrics();
                ThemeService.Instance.ThemeChanged += OnThemeChanged;
                ApplyTheme(ThemeService.Instance.IsDarkMode);

                LoggingService.Instance.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(LoggingService.LastLogEntry))
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            TxtLastLog.Text = LoggingService.Instance.LastLogEntry;
                        }, DispatcherPriority.Normal);
                    }
                };

                LoggingService.Instance.LogInfo("MainWindow initialized");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing services", ex);
            }
        }

        private void InitializeClock()
        {
            _clockTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += (s, e) =>
            {
                TxtCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss");
            };
            _clockTimer.Start();
            TxtCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void StartAutoSave()
        {
            PersistenceService.Instance.StartAutoSave(() => GetCurrentSessionData());
        }

        private EinsatzSessionData GetCurrentSessionData()
        {
            return new EinsatzSessionData
            {
                EinsatzData = _einsatzData,
                Teams = _teams.Select(t => new TeamSessionData
                {
                    TeamId = t.TeamId,
                    TeamName = t.TeamName,
                    TeamType = t.TeamType.ToString(),
                    HundName = t.HundName,
                    Hundefuehrer = t.Hundefuehrer,
                    Helfer = t.Helfer,
                    Notizen = t.Notizen,
                    ElapsedTime = t.ElapsedTime,
                    IsRunning = t.IsRunning,
                    IsFirstWarning = t.IsFirstWarning,
                    IsSecondWarning = t.IsSecondWarning,
                    FirstWarningMinutes = t.FirstWarningMinutes,
                    SecondWarningMinutes = t.SecondWarningMinutes,
                    StartTime = t.IsRunning ? DateTime.Now - t.ElapsedTime : null
                }).ToArray(),
                NextTeamId = _nextTeamId,
                FirstWarningMinutes = _firstWarningMinutes,
                SecondWarningMinutes = _secondWarningMinutes
            };
        }

        private void NotifyDataChanged()
        {
            PersistenceService.Instance.MarkDirty();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key >= Key.F1 && e.Key <= Key.F10)
                {
                    int teamIndex = (int)(e.Key - Key.F1);
                    if (teamIndex < _teams.Count)
                    {
                        var team = _teams[teamIndex];
                        if (team.IsRunning)
                        {
                            team.StopTimer();
                            TxtStatus.Text = $"Team {teamIndex + 1} gestoppt (F{teamIndex + 1})";
                        }
                        else
                        {
                            team.StartTimer();
                            TxtStatus.Text = $"Team {teamIndex + 1} gestartet (F{teamIndex + 1})";
                        }
                    }
                    e.Handled = true;
                }
                else if (e.Key == Key.F11)
                {
                    ToggleFullscreen();
                    e.Handled = true;
                }
                else if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    BtnAddTeam_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    BtnExport_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == Key.T && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    BtnThemeToggle_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == Key.H && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    BtnHelp_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    if (_isFullscreen)
                    {
                        ToggleFullscreen();
                    }
                    else
                    {
                        Close();
                    }
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling keyboard shortcut", ex);
            }
        }

        private void ToggleFullscreen()
        {
            try
            {
                if (_isFullscreen)
                {
                    WindowStyle = _previousWindowStyle;
                    WindowState = _previousWindowState;
                    _isFullscreen = false;
                    LoggingService.Instance.LogInfo("Exited fullscreen mode");
                }
                else
                {
                    _previousWindowStyle = WindowStyle;
                    _previousWindowState = WindowState;
                    WindowStyle = WindowStyle.None;
                    WindowState = WindowState.Maximized;
                    _isFullscreen = true;
                    LoggingService.Instance.LogInfo("Entered fullscreen mode");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error toggling fullscreen", ex);
            }
        }

        private void ShowStartWindow()
        {
            try
            {
                var startWindow = new StartWindow();
                var result = startWindow.ShowDialog();

                if (result == true && startWindow.EinsatzData != null)
                {
                    _einsatzData = startWindow.EinsatzData;
                    _firstWarningMinutes = startWindow.FirstWarningMinutes;
                    _secondWarningMinutes = startWindow.SecondWarningMinutes;
                    
                    InitializeEinsatz();
                    StartAutoSave();
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing start window", ex);
                MessageBox.Show($"Fehler beim Starten: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private void InitializeEinsatz()
        {
            if (_einsatzData == null) return;

            try
            {
                TxtEinsatzInfo.Text = $"{_einsatzData.EinsatzTyp} - {_einsatzData.EinsatzDatum:dd.MM.yyyy HH:mm}";
                TxtEinsatzort.Text = $"Ort: {_einsatzData.Einsatzort}";
                TxtEinsatzleiter.Text = $"EL: {_einsatzData.Einsatzleiter}";

                int teamsToCreate = Math.Min(_einsatzData.AnzahlTeams, 10);
                for (int i = 1; i <= teamsToCreate; i++)
                {
                    CreateTeam($"Team {i}", TeamType.Allgemein);
                }

                UpdateTeamCountStatus();
                LoggingService.Instance.LogInfo($"Mission initialized with {_teams.Count} teams");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing mission", ex);
            }
        }

        private void CreateTeam(string teamName, TeamType teamType = TeamType.Allgemein)
        {
            try
            {
                if (_teams.Count >= 10)
                {
                    MessageBox.Show("Maximale Anzahl von 10 Teams erreicht!", "Team-Limit", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var team = new Team
                {
                    TeamId = _nextTeamId++,
                    TeamName = teamName,
                    TeamType = teamType,
                    FirstWarningMinutes = _firstWarningMinutes,
                    SecondWarningMinutes = _secondWarningMinutes
                };

                _teams.Add(team);

                var teamControl = new TeamControl { Team = team };
                teamControl.TeamDeleteRequested += OnTeamDeleteRequested;
                teamControl.ApplyTheme(ThemeService.Instance.IsDarkMode);
                TeamsGrid.Children.Add(teamControl);
                UpdateTeamGridLayout();

                LoggingService.Instance.LogInfo($"Team created: {teamName} ({teamType})");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error creating team {teamName}", ex);
            }
        }

        private void OnTeamDeleteRequested(object? sender, Team team)
        {
            try
            {
                var teamControlToRemove = TeamsGrid.Children.OfType<TeamControl>()
                    .FirstOrDefault(tc => tc.Team?.TeamId == team.TeamId);

                if (teamControlToRemove != null)
                {
                    teamControlToRemove.TeamDeleteRequested -= OnTeamDeleteRequested;
                    TeamsGrid.Children.Remove(teamControlToRemove);
                    _teams.Remove(team);
                    UpdateTeamGridLayout();
                    LoggingService.Instance.LogInfo($"Team deleted: {team.TeamName}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error removing team {team.TeamName}", ex);
            }
        }

        private void UpdateTeamGridLayout()
        {
            try
            {
                int teamCount = _teams.Count;
                int columns;

                var windowWidth = ActualWidth > 0 ? ActualWidth : Width;
                
                if (windowWidth < 800)
                {
                    columns = 1;
                }
                else if (windowWidth < 1200)
                {
                    columns = teamCount switch
                    {
                        1 => 1,
                        2 or 3 or 4 => 2,
                        _ => Math.Min(2, teamCount)
                    };
                }
                else
                {
                    columns = teamCount switch
                    {
                        1 => 1,
                        2 => 2,
                        3 or 4 => 2,
                        5 or 6 => 3,
                        7 or 8 or 9 => 3,
                        10 => 2,
                        _ => 3
                    };
                }

                TeamsGrid.Columns = columns;
                UpdateTeamCountStatus();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating team grid layout", ex);
            }
        }

        private void UpdateTeamCountStatus()
        {
            TxtStatus.Text = $"Einsatz aktiv - {_teams.Count}/10 Teams";
            TxtTeamCount.Text = $"{_teams.Count}/10";
            BtnAddTeam.IsEnabled = _teams.Count < 10;
            
            if (_teams.Count >= 10)
            {
                TxtTeamCount.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54));
            }
            else
            {
                TxtTeamCount.Foreground = (System.Windows.Media.Brush)FindResource("TextBrush");
            }
        }

        private void OnThemeChanged(bool isDarkMode)
        {
            Dispatcher.Invoke(() => ApplyTheme(isDarkMode));
        }

        private void ApplyTheme(bool isDarkMode)
        {
            try
            {
                var themeDict = isDarkMode ? 
                    (ResourceDictionary)FindResource("DarkTheme") : 
                    (ResourceDictionary)FindResource("LightTheme");

                Resources["BackgroundBrush"] = themeDict["BackgroundBrush"];
                Resources["CardBackgroundBrush"] = themeDict["CardBackgroundBrush"];
                Resources["PrimaryBrush"] = themeDict["PrimaryBrush"];
                Resources["TextBrush"] = themeDict["TextBrush"];
                Resources["BorderBrush"] = themeDict["BorderBrush"];
                Resources["AccentBrush"] = themeDict["AccentBrush"];

                if (FindName("ThemeIcon") is FontAwesome.WPF.ImageAwesome themeIcon)
                {
                    themeIcon.Icon = isDarkMode ? FontAwesome.WPF.FontAwesomeIcon.SunOutline : FontAwesome.WPF.FontAwesomeIcon.MoonOutline;
                }

                UpdateTeamControlsTheme(isDarkMode);
                LoggingService.Instance.LogInfo($"Theme changed to {(isDarkMode ? "dark" : "light")} mode");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme", ex);
            }
        }

        private void UpdateTeamControlsTheme(bool isDarkMode)
        {
            try
            {
                foreach (TeamControl teamControl in TeamsGrid.Children.OfType<TeamControl>())
                {
                    teamControl.ApplyTheme(isDarkMode);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating team controls theme", ex);
            }
        }

        private void BtnAddTeam_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_teams.Count >= 10)
                {
                    MessageBox.Show("Maximale Anzahl von 10 Teams bereits erreicht!", "Team-Limit", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var teamTypeWindow = new TeamTypeSelectionWindow();
                if (teamTypeWindow.ShowDialog() == true)
                {
                    string teamName = $"Team {_nextTeamId}";
                    CreateTeam(teamName, teamTypeWindow.SelectedTeamType);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error adding team", ex);
                MessageBox.Show($"Fehler beim Hinzufügen des Teams: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ThemeService.Instance.IsAutoMode)
                {
                    var result = MessageBox.Show("Automatischer Dunkelmodus ist aktiv. Manuell umschalten?", 
                        "Theme", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        ThemeService.Instance.IsAutoMode = false;
                        ThemeService.Instance.ToggleTheme();
                    }
                }
                else
                {
                    ThemeService.Instance.ToggleTheme();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error toggling theme", ex);
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExportData();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during export", ex);
                MessageBox.Show($"Fehler beim Export: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportData()
        {
            if (_einsatzData == null) return;

            try
            {
                var exportData = new
                {
                    Einsatz = _einsatzData,
                    Teams = _teams.Select(t => new
                    {
                        t.TeamId,
                        t.TeamName,
                        TeamType = t.TeamType.ToString(),
                        TeamTypeInfo = t.TeamTypeInfo,
                        t.HundName,
                        t.Hundefuehrer,
                        t.Helfer,
                        t.Notizen,
                        ElapsedTime = t.ElapsedTime.ToString(),
                        IsRunning = t.IsRunning,
                        IsFirstWarning = t.IsFirstWarning,
                        IsSecondWarning = t.IsSecondWarning
                    }).ToArray(),
                    ExportTime = DateTime.Now,
                    ExportVersion = "2.1.0"
                };

                var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
                
                var fileName = $"Einsatz_{_einsatzData.EinsatzDatum:yyyy-MM-dd_HH-mm-ss}.json";
                var filePath = Path.Combine(_einsatzData.ExportPfad, fileName);
                
                Directory.CreateDirectory(_einsatzData.ExportPfad);
                File.WriteAllText(filePath, json);

                TxtStatus.Text = $"Export erstellt: {fileName}";
                LoggingService.Instance.LogInfo($"Data exported to {filePath}");

                MessageBox.Show($"Daten erfolgreich exportiert:\n{filePath}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Export failed", ex);
                throw;
            }
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var helpText = @"?? EINSATZÜBERWACHUNG PROFESSIONAL - HILFE

=== SCHNELLSTART ===
1. Einsatzleiter, Ort und Teams eingeben
2. Warnschwellen einstellen (Standard: 10/20 Min)
3. '+ Team' für weitere Teams hinzufügen
4. Timer mit Start/Stop/Reset bedienen

=== TASTENKÜRZEL ===
F1-F10:    Team 1-10 Timer Start/Stop
F11:       Vollbild ein/aus
Strg+N:    Neues Team
Strg+E:    Export
Strg+T:    Theme umschalten
Esc:       Vollbild beenden

=== TEAM-TYPEN ===
• Flächensuchhund (Blau)
• Trümmersuchhund (Orange)  
• Mantrailer (Grün)
• Wasserortung (Cyan)
• Lawinensuchhund (Lila)
• Allgemein (Grau)

=== NOTIZEN-SYSTEM ===
• Automatische Zeitstempel bei Timer-Aktionen
• Schnellnotizen mit Enter oder '+' Button
• Format: [14:25:30] ? Timer gestartet

=== AUTO-SAVE ===
• Speichert alle 30 Sekunden automatisch
• Crash-Recovery beim nächsten Start
• Export als JSON für Dokumentation

Version 2.1.0 - Professional Edition";

                MessageBox.Show(helpText, "Hilfe - Einsatzüberwachung Professional", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing help", ex);
            }
        }

        private void BtnMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var contextMenu = new ContextMenu();
                
                var aboutItem = new MenuItem
                {
                    Header = "Über Einsatzüberwachung...",
                    Icon = new FontAwesome.WPF.ImageAwesome 
                    { 
                        Icon = FontAwesome.WPF.FontAwesomeIcon.InfoCircle, 
                        Width = 16, 
                        Height = 16 
                    }
                };
                aboutItem.Click += (s, args) =>
                {
                    var aboutWindow = new AboutWindow { Owner = this };
                    aboutWindow.ShowDialog();
                };
                
                var helpItem = new MenuItem
                {
                    Header = "Hilfe & Tastenkürzel...",
                    Icon = new FontAwesome.WPF.ImageAwesome 
                    { 
                        Icon = FontAwesome.WPF.FontAwesomeIcon.QuestionCircle, 
                        Width = 16, 
                        Height = 16 
                    }
                };
                helpItem.Click += (s, args) =>
                {
                    BtnHelp_Click(this, new RoutedEventArgs());
                };
                
                var perfItem = new MenuItem
                {
                    Header = "Performance-Metriken",
                    Icon = new FontAwesome.WPF.ImageAwesome 
                    { 
                        Icon = FontAwesome.WPF.FontAwesomeIcon.BarChart, 
                        Width = 16, 
                        Height = 16 
                    }
                };
                perfItem.Click += (s, args) =>
                {
                    PerformanceService.Instance.LogPerformanceMetrics();
                    TimerDiagnosticService.Instance.LogAllTimerPerformance();
                    MessageBox.Show("Performance-Metriken wurden in das Log geschrieben.", 
                        "Performance", MessageBoxButton.OK, MessageBoxImage.Information);
                };
                
                contextMenu.Items.Add(helpItem);
                contextMenu.Items.Add(new Separator());
                contextMenu.Items.Add(perfItem);
                contextMenu.Items.Add(new Separator());
                contextMenu.Items.Add(aboutItem);
                
                contextMenu.PlacementTarget = sender as Button;
                contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                contextMenu.IsOpen = true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening menu", ex);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                TimerDiagnosticService.Instance.LogAllTimerPerformance();

                if (_einsatzData != null)
                {
                    var sessionData = GetCurrentSessionData();
                    _ = PersistenceService.Instance.SaveCrashRecoveryAsync(sessionData);
                }

                _clockTimer?.Stop();
                PersistenceService.Instance.StopAutoSave();
                PerformanceService.Instance.StopCleanup();
                
                foreach (var team in _teams)
                {
                    team.StopTimer();
                }

                PerformanceService.Instance.LogPerformanceMetrics();
                LoggingService.Instance.LogInfo("Application closing");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during application shutdown", ex);
            }

            base.OnClosed(e);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            
            if (sizeInfo.WidthChanged && _teams.Count > 0)
            {
                if (_layoutUpdateTimer == null)
                {
                    _layoutUpdateTimer = new DispatcherTimer(DispatcherPriority.Background)
                    {
                        Interval = TimeSpan.FromMilliseconds(250)
                    };
                    _layoutUpdateTimer.Tick += (s, e) =>
                    {
                        _layoutUpdateTimer.Stop();
                        UpdateTeamGridLayout();
                    };
                }
                
                _layoutUpdateTimer.Stop();
                _layoutUpdateTimer.Start();
            }
        }
    }
}