using System;
using System.Linq;
using System.Windows;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung
{
    public partial class StartWindow : Window
    {
        public EinsatzData? EinsatzData { get; private set; }
        public int FirstWarningMinutes { get; private set; } = 10;
        public int SecondWarningMinutes { get; private set; } = 20;

        private readonly MasterDataService _masterDataService;

        public StartWindow()
        {
            InitializeComponent();
            
            _masterDataService = MasterDataService.Instance;
            
            InitializeTheme();
            LoadMasterData();
            
            LoggingService.Instance.LogInfo("StartWindow v1.7 initialized with master data integration");
        }

        private async Task LoadMasterData()
        {
            try
            {
                // Erzwinge das vollständige Neuladen der Daten
                await _masterDataService.RefreshDataAsync();
                
                // Kurze Verzögerung um sicherzustellen, dass Daten geladen sind
                await Task.Delay(100);
                
                // Load Einsatzleiter mit erweiterte Filterung
                var leaders = _masterDataService.PersonalList
                    .Where(p => p != null && p.IsActive && (
                        p.Skills.HasFlag(PersonalSkills.Gruppenfuehrer) ||
                        p.Skills.HasFlag(PersonalSkills.Zugfuehrer) ||
                        p.Skills.HasFlag(PersonalSkills.Verbandsfuehrer)))
                    .OrderBy(p => p.Nachname)
                    .ThenBy(p => p.Vorname)
                    .ToList();

                // ComboBox auf UI-Thread aktualisieren
                Dispatcher.Invoke(() =>
                {
                    CmbEinsatzleiter.Items.Clear();
                    CmbEinsatzleiter.Items.Add(new { FullName = "(Manuell eingeben)" });

                    foreach (var person in leaders)
                    {
                        CmbEinsatzleiter.Items.Add(person);
                    }

                    if (CmbEinsatzleiter.Items.Count > 0)
                        CmbEinsatzleiter.SelectedIndex = 0;
                });

                LoggingService.Instance.LogInfo($"Loaded {leaders.Count} potential mission leaders from master data");
                
                // Benutzer-Information
                if (leaders.Count == 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        TxtEinsatzleiterInfo.Text = "💡 Keine Führungskräfte in Stammdaten - Testdaten werden erstellt";
                        // Versuche erneut nach kurzer Verzögerung
                        Task.Delay(1000).ContinueWith(_ => Dispatcher.Invoke(async () => await LoadMasterData()));
                    });
                }
                else if (leaders.Any(l => l.Nachname == "Testperson"))
                {
                    Dispatcher.Invoke(() =>
                    {
                        TxtEinsatzleiterInfo.Text = "✅ Testdaten erfolgreich geladen";
                        TxtEinsatzleiterInfo.Foreground = (System.Windows.Media.Brush)FindResource("Success");
                    });
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading master data into StartWindow", ex);
                
                // Fallback mit Retry-Mechanismus
                Dispatcher.Invoke(() =>
                {
                    CmbEinsatzleiter.Items.Clear();
                    CmbEinsatzleiter.Items.Add(new { FullName = "(Manuell eingeben)" });
                    CmbEinsatzleiter.SelectedIndex = 0;
                    
                    TxtEinsatzleiterInfo.Text = "⚠️ Ladefehler - wird erneut versucht...";
                });
                
                // Erneuter Versuch nach 2 Sekunden
                await Task.Delay(2000);
                await LoadMasterData();
            }
        }

        private void CmbEinsatzleiter_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (CmbEinsatzleiter.SelectedItem is PersonalEntry selectedPerson)
                {
                    // Zeige zusätzliche Info über Qualifikationen
                    UpdateEinsatzleiterInfo(selectedPerson);
                }
                else if (CmbEinsatzleiter.IsEditable && !string.IsNullOrEmpty(CmbEinsatzleiter.Text))
                {
                    // Manual text entry
                    ClearEinsatzleiterInfo();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling Einsatzleiter selection", ex);
            }
        }

        private void UpdateEinsatzleiterInfo(PersonalEntry person)
        {
            try
            {
                // Zeige Qualifikationen des ausgewählten Einsatzleiters
                var qualifications = string.Join(", ", 
                    new[] 
                    {
                        person.Skills.HasFlag(PersonalSkills.Gruppenfuehrer) ? "GF" : null,
                        person.Skills.HasFlag(PersonalSkills.Zugfuehrer) ? "ZF" : null,
                        person.Skills.HasFlag(PersonalSkills.Verbandsfuehrer) ? "VF" : null
                    }.Where(s => s != null));

                if (!string.IsNullOrEmpty(qualifications))
                {
                    TxtEinsatzleiterInfo.Text = $"Qualifikation: {qualifications}";
                    TxtEinsatzleiterInfo.Foreground = (System.Windows.Media.Brush)FindResource("Success");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating Einsatzleiter info", ex);
            }
        }

        private void ClearEinsatzleiterInfo()
        {
            TxtEinsatzleiterInfo.Text = "";
        }

        private void InitializeTheme()
        {
            // Apply current theme to start window
            ApplyTheme(ThemeService.Instance.IsDarkMode);
            ThemeService.Instance.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged(bool isDarkMode)
        {
            Dispatcher.Invoke(() => ApplyTheme(isDarkMode));
        }

        private void ApplyTheme(bool isDarkMode)
        {
            try
            {
                if (isDarkMode)
                {
                    // Dark theme colors - use design system
                    Resources["BackgroundBrush"] = FindResource("DarkSurface");
                    Resources["CardBackgroundBrush"] = FindResource("DarkSurfaceContainer");
                    Resources["TextBrush"] = FindResource("DarkOnSurface");
                    Resources["BorderBrush"] = FindResource("DarkOutline");
                    Resources["PrimaryBrush"] = FindResource("DarkPrimary");
                    Resources["AccentBrush"] = FindResource("DarkTertiary");
                    
                    // Dark Mode Info Box - bessere Lesbarkeit
                    Resources["InfoBoxBackgroundBrush"] = FindResource("DarkSurfaceContainerHigh");
                    Resources["InfoBoxTextBrush"] = FindResource("DarkOnSurface");
                }
                else
                {
                    // Light theme colors - use design system
                    Resources["BackgroundBrush"] = FindResource("Surface");
                    Resources["CardBackgroundBrush"] = FindResource("SurfaceContainer");
                    Resources["TextBrush"] = FindResource("OnSurface");
                    Resources["BorderBrush"] = FindResource("Outline");
                    Resources["PrimaryBrush"] = FindResource("Primary");
                    Resources["AccentBrush"] = FindResource("Tertiary");
                    
                    // Light Mode Info Box
                    Resources["InfoBoxBackgroundBrush"] = FindResource("SurfaceContainerHigh");
                    Resources["InfoBoxTextBrush"] = FindResource("OnSurface");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to StartWindow", ex);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            LoggingService.Instance.LogInfo("Start canceled by user");
            
            // Check if this window was opened as dialog or as startup window
            if (Owner != null)
            {
                // Opened as dialog, can set DialogResult
                DialogResult = false;
            }
            else
            {
                // Opened as startup window, close application
                Application.Current.Shutdown();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            LoggingService.Instance.LogInfo("Start window closed by user");
            
            // Check if this window was opened as dialog or as startup window
            if (Owner != null)
            {
                // Opened as dialog, can set DialogResult
                DialogResult = false;
            }
            else
            {
                // Opened as startup window, close application
                Application.Current.Shutdown();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get Einsatzleiter from ComboBox
                string einsatzleiter = string.Empty;
                
                if (CmbEinsatzleiter.SelectedItem is PersonalEntry selectedPerson)
                {
                    einsatzleiter = selectedPerson.FullName;
                }
                else if (CmbEinsatzleiter.IsEditable && !string.IsNullOrEmpty(CmbEinsatzleiter.Text))
                {
                    einsatzleiter = CmbEinsatzleiter.Text.Trim();
                }

                // Validate essential inputs
                if (string.IsNullOrWhiteSpace(einsatzleiter))
                {
                    MessageBox.Show("Bitte wählen Sie einen Einsatzleiter aus oder geben Sie einen Namen ein.", 
                        "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CmbEinsatzleiter.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtEinsatzort.Text))
                {
                    MessageBox.Show("Bitte geben Sie einen Einsatzort ein.", "Validierungsfehler", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtEinsatzort.Focus();
                    return;
                }

                if (!int.TryParse(TxtWarning1.Text, out int warning1) || warning1 < 1)
                {
                    MessageBox.Show("Bitte geben Sie eine gültige Zeit für die erste Warnung ein (mindestens 1 Minute).", 
                        "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtWarning1.Focus();
                    return;
                }

                if (!int.TryParse(TxtWarning2.Text, out int warning2) || warning2 <= warning1)
                {
                    MessageBox.Show("Die zweite Warnung muss größer als die erste Warnung sein.", 
                        "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtWarning2.Focus();
                    return;
                }

                // Create simplified EinsatzData - teams will be added in MainWindow
                EinsatzData = new EinsatzData
                {
                    Einsatzleiter = einsatzleiter,
                    Alarmiert = TxtAlarmiert.Text.Trim(),
                    Einsatzort = TxtEinsatzort.Text.Trim(),
                    IstEinsatz = RbEinsatz.IsChecked == true,
                    AnzahlTeams = 0, // Teams will be added dynamically in MainWindow
                    EinsatzDatum = DateTime.Now,
                    // Set default export path - can be changed later in MainWindow
                    ExportPfad = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "Einsatzueberwachung"),
                    Fuehrungsassistent = string.Empty // Can be added later if needed
                };

                FirstWarningMinutes = warning1;
                SecondWarningMinutes = warning2;

                LoggingService.Instance.LogInfo($"Einsatz v1.7 started - Type: {EinsatzData.EinsatzTyp}, " +
                    $"Location: {EinsatzData.Einsatzort}, Leader: {EinsatzData.Einsatzleiter}, " +
                    $"Warnings: {warning1}/{warning2} minutes");

                // Check if this window was opened as dialog or as startup window
                if (Owner != null)
                {
                    // Opened as dialog, can set DialogResult
                    DialogResult = true;
                }
                else
                {
                    // WICHTIG: Opened as startup window
                    // 1. Erstelle MainWindow
                    var mainWindow = new MainWindow(EinsatzData, FirstWarningMinutes, SecondWarningMinutes);
                    
                    // 2. Setze es als Application.MainWindow BEVOR es angezeigt wird
                    Application.Current.MainWindow = mainWindow;
                    
                    // 3. Ändere ShutdownMode zu OnMainWindowClose (jetzt sicher)
                    Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    
                    // 4. Zeige MainWindow
                    mainWindow.Show();
                    
                    // 5. Schließe StartWindow (App wird NICHT beendet, weil MainWindow noch offen ist)
                    this.Close();
                    
                    LoggingService.Instance.LogInfo("✅ Transition from StartWindow to MainWindow completed");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error starting mission", ex);
                MessageBox.Show($"Fehler beim Starten des Einsatzes: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from theme changes
            ThemeService.Instance.ThemeChanged -= OnThemeChanged;
            base.OnClosed(e);
        }
    }
}
