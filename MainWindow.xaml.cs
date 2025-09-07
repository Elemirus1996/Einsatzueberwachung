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
using System.Windows.Media;
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
        private DispatcherTimer? _resizeTimer;

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

                // Normal startup
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

                    // Update UI with mission info
                    TxtEinsatzInfo.Text = $"{_einsatzData.EinsatzTyp} - {_einsatzData.EinsatzDatum:dd.MM.yyyy HH:mm}";
                    TxtEinsatzort.Text = $"Ort: {_einsatzData.Einsatzort}";
                    TxtEinsatzleiter.Text = $"EL: {_einsatzData.Einsatzleiter}";

                    // Restore teams with multiple types support
                    foreach (var teamData in sessionData.Teams)
                    {
                        var team = new Team
                        {
                            TeamId = teamData.TeamId,
                            TeamName = teamData.TeamName,
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

                        // Restore multiple team types if available, otherwise use single type
                        if (!string.IsNullOrEmpty(teamData.TeamType))
                        {
                            if (Enum.TryParse<TeamType>(teamData.TeamType, out var singleType))
                            {
                                team.MultipleTeamTypes = new MultipleTeamTypes(singleType);
                            }
                        }

                        _teams.Add(team);

                        var teamControl = new TeamControl { Team = team };
                        teamControl.TeamDeleteRequested += OnTeamDeleteRequested;
                        teamControl.ApplyTheme(ThemeService.Instance.IsDarkMode);
                        TeamsGrid.Children.Add(teamControl);

                        // Restart timer if it was running
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
                // Initialize performance service
                PerformanceService.Instance.LogPerformanceMetrics();

                // Initialize theme service
                ThemeService.Instance.ThemeChanged += OnThemeChanged;
                ApplyTheme(ThemeService.Instance.IsDarkMode);

                // Initialize logging with Normal priority for important logs
                LoggingService.Instance.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(LoggingService.LastLogEntry))
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            TxtLastLog.Text = LoggingService.Instance.LastLogEntry;
                        }, DispatcherPriority.Normal); // Use Normal priority for logs
                    }
                };

                LoggingService.Instance.LogInfo("MainWindow v1.5 initialized");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing services", ex);
            }
        }

        private void InitializeClock()
        {
            // Use Normal Priority for clock timer too
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
                    TeamType = t.TeamType.ToString(), // For backward compatibility
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
                // Update UI with mission info
                TxtEinsatzInfo.Text = $"{_einsatzData.EinsatzTyp} - {_einsatzData.EinsatzDatum:dd.MM.yyyy HH:mm}";
                TxtEinsatzort.Text = $"Ort: {_einsatzData.Einsatzort}";
                TxtEinsatzleiter.Text = $"EL: {_einsatzData.Einsatzleiter}";

                // v1.5: No initial teams - user will add them manually
                // This allows for better customization of teams with multiple types
                UpdateTeamCountStatus();
                LoggingService.Instance.LogInfo($"Mission v1.5 initialized - teams will be added manually");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing mission", ex);
            }
        }

        private void CreateTeam(string teamName, MultipleTeamTypes? multipleTypes = null)
        {
            // Legacy method - redirect to new detailed method
            CreateTeamWithDetails(teamName, "Unbekannt", "", "", multipleTypes);
        }

        private Team CreateTeamWithDetails(string teamName, string hundName, string hundefuehrer, string helfer, MultipleTeamTypes multipleTypes)
        {
            try
            {
                // Check maximum team limit
                if (_teams.Count >= 10)
                {
                    MessageBox.Show("Maximale Anzahl von 10 Teams erreicht!", "Team-Limit", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return null;
                }

                var team = new Team
                {
                    TeamId = _nextTeamId++,
                    TeamName = teamName,
                    HundName = hundName,
                    Hundefuehrer = hundefuehrer,
                    Helfer = helfer,
                    MultipleTeamTypes = multipleTypes ?? new MultipleTeamTypes(),
                    FirstWarningMinutes = _firstWarningMinutes,
                    SecondWarningMinutes = _secondWarningMinutes
                };

                _teams.Add(team);

                var teamControl = new TeamControl { Team = team };
                
                // Subscribe to delete event
                teamControl.TeamDeleteRequested += OnTeamDeleteRequested;
                
                // Apply current theme to new team control
                teamControl.ApplyTheme(ThemeService.Instance.IsDarkMode);
                
                TeamsGrid.Children.Add(teamControl);

                // Improved grid layout based on team count
                UpdateTeamGridLayout();
                
                // v1.5: Hide welcome message after first team is added
                UpdateWelcomeMessageVisibility();

                LoggingService.Instance.LogInfo($"Team v1.5 created: {teamName} with dog {hundName} ({team.TeamTypeDisplayName})");
                return team;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error creating team {teamName}", ex);
                return null;
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

                // v1.5: Two-step process - first get team details, then specialization
                var teamInputWindow = new TeamInputWindow();
                if (teamInputWindow.ShowDialog() == true)
                {
                    // Now get the specialization
                    var teamTypeWindow = new TeamTypeSelectionWindow();
                    if (teamTypeWindow.ShowDialog() == true)
                    {
                        // Create team with Team + Dog name
                        string teamName = teamInputWindow.TeamName;
                        var team = CreateTeamWithDetails(teamName, teamInputWindow.HundName, 
                            teamInputWindow.Hundefuehrer, teamInputWindow.Helfer, 
                            teamTypeWindow.SelectedMultipleTeamTypes);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error adding team", ex);
                MessageBox.Show($"Fehler beim Hinzufügen des Teams: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var helpWindow = new HelpWindow
                {
                    Owner = this
                };
                helpWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening help window", ex);
                
                // Fallback simple help dialog for v1.5
                var helpText = @"🚀 EINSATZÜBERWACHUNG PROFESSIONAL v1.5

=== NEU IN v1.5 ===
✨ Vereinfachtes StartWindow - nur essenzielle Infos
✨ Automatische Teamname-Generierung: 'Team [Hundename]'
✨ Teams im Hauptfenster hinzufügen
✨ Multiple Typen pro Hund (Fläche + Trümmer + Mantrailer)
✨ Verbesserte Team-Verwaltung

=== SCHNELLSTART ====
1. Einsatzleiter und Ort eingeben
2. Warnschwellen einstellen (Standard: 10/20 Min)
3. '+ Team' für Hundeteams hinzufügen
4. Hundename eingeben (Teamname = 'Team [Hundename]')
5. Mehrfach-Spezialisierung auswählen
6. Timer mit Start/Stop/Reset bedienen

=== TASTENKÜRZEL ===
F1-F10:    Team 1-10 Timer Start/Stop
F11:       Vollbild ein/aus
Strg+N:    Neues Team
Strg+E:    Export
Strg+T:    Theme umschalten
Esc:       Vollbild beenden

=== TEAM-SPEZIALISIERUNGEN ===
• Flächensuchhund (Blau)
• Trümmersuchhund (Orange)  
• Mantrailer (Grün)
• Wasserortung (Cyan)
• Lawinensuchhund (Lila)
• Allgemein (Grau)

💡 Ein Hund kann mehrere Spezialisierungen haben!

=== FEATURES ===
• Automatische Zeitstempel bei Timer-Aktionen
• Schnellnotizen mit Enter oder '+' Button
• Speichert alle 30 Sekunden automatisch
• Crash-Recovery beim nächsten Start
• Export als JSON für Dokumentation

Version 1.5 - Professional Edition
© 2024 RescueDog_SW";

                MessageBox.Show(helpText, "Hilfe - Einsatzüberwachung Professional v1.5", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
                        // v1.5: Export multiple team types
                        TeamTypes = t.MultipleTeamTypes.SelectedTypes.Select(type => new
                        {
                            Type = type.ToString(),
                            Info = TeamTypeInfo.GetTypeInfo(type)
                        }).ToArray(),
                        TeamTypeDisplay = t.TeamTypeDisplayName,
                        TeamTypeColor = t.TeamTypeColorHex,
                        // Backward compatibility
                        PrimaryTeamType = t.TeamType.ToString(),
                        t.HundName,
                        t.Hundefuehrer,
                        t.Helfer,
                        t.Notizen,
                        ElapsedTime = t.ElapsedTime.ToString(),
                        IsRunning = t.IsRunning,
                        IsFirstWarning = t.IsFirstWarning,
                        IsSecondWarning = t.IsSecondWarning,
                        NotesEntries = t.NotesEntries.ToArray()
                    }).ToArray(),
                    ExportTime = DateTime.Now,
                    ExportVersion = "1.5.0",
                    Application = "Einsatzüberwachung Professional v1.5"
                };

                var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
                
                var fileName = $"Einsatz_{_einsatzData.EinsatzDatum:yyyy-MM-dd_HH-mm-ss}_v1.5.json";
                var filePath = Path.Combine(_einsatzData.ExportPfad, fileName);
                
                Directory.CreateDirectory(_einsatzData.ExportPfad);
                File.WriteAllText(filePath, json);

                TxtStatus.Text = $"Export erstellt: {fileName}";
                LoggingService.Instance.LogInfo($"Data v1.5 exported to {filePath}");

                MessageBox.Show($"Daten erfolgreich exportiert:\n{filePath}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Export failed", ex);
                throw;
            }
        }

        // Standard methods remain the same...
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                // F1-F10 for team control
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
                // F11 for fullscreen
                else if (e.Key == Key.F11)
                {
                    ToggleFullscreen();
                    e.Handled = true;
                }
                // Ctrl+N for New Team
                else if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    BtnAddTeam_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                }
                // Ctrl+E for Export
                else if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    BtnExport_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                }
                // Ctrl+T for Theme Toggle
                else if (e.Key == Key.T && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    BtnThemeToggle_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                }
                // Ctrl+H for Help
                else if (e.Key == Key.H && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    BtnHelp_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                }
                // Escape to exit fullscreen or close app
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
                    // Exit fullscreen
                    WindowStyle = _previousWindowStyle;
                    WindowState = _previousWindowState;
                    _isFullscreen = false;
                    LoggingService.Instance.LogInfo("Exited fullscreen mode");
                }
                else
                {
                    // Enter fullscreen
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

        private void OnTeamDeleteRequested(object? sender, Team team)
        {
            try
            {
                // Find and remove the team control
                var teamControlToRemove = TeamsGrid.Children.OfType<TeamControl>()
                    .FirstOrDefault(tc => tc.Team?.TeamId == team.TeamId);

                if (teamControlToRemove != null)
                {
                    // Unsubscribe from events
                    teamControlToRemove.TeamDeleteRequested -= OnTeamDeleteRequested;
                    
                    // Remove from UI
                    TeamsGrid.Children.Remove(teamControlToRemove);
                    
                    // Remove from teams collection
                    _teams.Remove(team);
                    
                    // Update layout
                    UpdateTeamGridLayout();
                    
                    // v1.5: Show welcome message again if no teams left
                    UpdateWelcomeMessageVisibility();
                    
                    LoggingService.Instance.LogInfo($"Team deleted: {team.TeamName}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error deleting team", ex);
            }
        }

        // v1.5: New method to handle welcome message visibility
        private void UpdateWelcomeMessageVisibility()
        {
            try
            {
                if (FindName("WelcomeMessage") is Border welcomeMessage)
                {
                    // Hide welcome message when teams exist, show when no teams
                    welcomeMessage.Visibility = _teams.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating welcome message visibility", ex);
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
            TxtStatus.Text = $"Einsatz v1.5 aktiv - {_teams.Count}/10 Teams";
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
                // Simple theme implementation using direct resource assignments
                if (isDarkMode)
                {
                    // Dark theme colors
                    Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30)); // #1E1E1E
                    Resources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45)); // #2D2D2D
                    Resources["PrimaryBrush"] = new SolidColorBrush(Color.FromRgb(144, 202, 249)); // #90CAF9
                    Resources["TextBrush"] = new SolidColorBrush(Color.FromRgb(224, 224, 224)); // #E0E0E0
                    Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(68, 68, 68)); // #444444
                    Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // #2196F3
                }
                else
                {
                    // Light theme colors
                    Resources["BackgroundBrush"] = new SolidColorBrush(Colors.White);
                    Resources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(248, 249, 250)); // #F8F9FA
                    Resources["PrimaryBrush"] = new SolidColorBrush(Color.FromRgb(21, 101, 192)); // #1565C0
                    Resources["TextBrush"] = new SolidColorBrush(Color.FromRgb(33, 33, 33)); // #212121
                    Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(224, 224, 224)); // #E0E0E0
                    Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(25, 118, 210)); // #1976D2
                }

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

        private void BtnMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var contextMenu = new ContextMenu();
                
                var aboutItem = new MenuItem
                {
                    Header = "Über Einsatzüberwachung v1.5...",
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
                helpItem.Click += (s, args) => BtnHelp_Click(s, new RoutedEventArgs());
                
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
                LoggingService.Instance.LogInfo("Application v1.5 closing");
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
                if (_resizeTimer == null)
                {
                    _resizeTimer = new DispatcherTimer(DispatcherPriority.Background)
                    {
                        Interval = TimeSpan.FromMilliseconds(250)
                    };
                    _resizeTimer.Tick += (s, e) =>
                    {
                        _resizeTimer.Stop();
                        UpdateTeamGridLayout();
                    };
                }
                
                _resizeTimer.Stop();
                _resizeTimer.Start();
            }
        }
    }
}
