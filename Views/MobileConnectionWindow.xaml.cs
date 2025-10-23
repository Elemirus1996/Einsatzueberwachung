using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Services;
using Einsatzueberwachung.Models;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// MobileConnectionWindow - Enhanced with Theme Integration v1.9.0
    /// Now inherits from BaseThemeWindow for automatic theme support
    /// Fully integrated with design system and theme service
    /// </summary>
    public partial class MobileConnectionWindow : BaseThemeWindow
    {
        private MobileConnectionViewModel? _viewModel;

        public MobileConnectionWindow()
        {
            InitializeComponent();
            InitializeThemeSupport(); // Initialize theme after component initialization
            InitializeViewModel();
        }

        /// <summary>
        /// Constructor mit MainViewModel für Daten-Integration
        /// </summary>
        /// <param name="mainViewModel">Das MainViewModel für Zugriff auf Teams und Einsatz-Daten</param>
        public MobileConnectionWindow(MainViewModel mainViewModel) : this()
        {
            SetupDataIntegration(mainViewModel);
        }

        protected override void ApplyThemeToWindow(bool isDarkMode)
        {
            try
            {
                // Apply theme-specific styling
                base.ApplyThemeToWindow(isDarkMode);
                
                LoggingService.Instance.LogInfo($"Theme applied to MobileConnectionWindow: {(isDarkMode ? "Dark" : "Light")} mode with Orange design");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to MobileConnectionWindow", ex);
            }
        }

        private void InitializeViewModel()
        {
            try
            {
                _viewModel = new MobileConnectionViewModel();
                DataContext = _viewModel;
                
                // Event-Subscriptions
                if (_viewModel != null)
                {
                    _viewModel.WindowMinimizeRequested += () => WindowState = WindowState.Minimized;
                    _viewModel.TrayNotificationRequested += () => ShowTrayNotification();
                    _viewModel.SystemDiagnoseRequested += () => PerformSystemDiagnose();
                }
                
                LoggingService.Instance.LogInfo("MobileConnectionWindow initialized with MVVM and theme integration");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing MobileConnectionWindow ViewModel", ex);
                MessageBox.Show($"Fehler beim Initialisieren des Mobile-Fensters: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Konfiguriert die Daten-Integration zwischen MainViewModel und MobileService
        /// </summary>
        private void SetupDataIntegration(MainViewModel mainViewModel)
        {
            try
            {
                LoggingService.Instance.LogInfo("Starting MobileConnectionWindow data integration setup...");
                
                // Validiere MainViewModel
                if (mainViewModel == null)
                {
                    LoggingService.Instance.LogError("MainViewModel is null - cannot setup data integration");
                    ShowDataIntegrationError("MainViewModel ist nicht verfügbar. Starten Sie die Anwendung neu.");
                    return;
                }
                
                // MobileService initialisieren mit Retry-Logik
                if (!InitializeMobileServiceWithRetry())
                {
                    ShowMobileServiceInitializationError();
                    return;
                }
                
                var mobileIntegrationService = MobileService.Instance.GetMobileIntegrationService();
                if (mobileIntegrationService != null)
                {
                    LoggingService.Instance.LogInfo("Setting up data delegates with enhanced validation...");
                    
                    // Setup Delegates für Daten-Zugriff mit verbesserter Fehlerbehandlung
                    mobileIntegrationService.GetCurrentTeams = () => 
                    {
                        try
                        {
                            LoggingService.Instance.LogInfo("GetCurrentTeams delegate called");
                            
                            // KORRIGIERT: Validiere MainViewModel und Teams-Collection
                            if (mainViewModel?.Teams == null)
                            {
                                LoggingService.Instance.LogWarning("MainViewModel.Teams is null - returning empty list (no fallback)");
                                return new List<Team>(); // Leere Liste statt Test-Teams
                            }
                            
                            var teams = mainViewModel.Teams.ToList();
                            LoggingService.Instance.LogInfo($"Mobile service accessed {teams.Count} real teams successfully");
                            
                            // KORRIGIERT: Gib echte Teams zurück, auch wenn die Liste leer ist
                            // Keine Test-Daten mehr erstellen - das verwirrt nur
                            if (teams.Count == 0)
                            {
                                LoggingService.Instance.LogInfo("No teams found - returning empty list (real data)");
                                return new List<Team>(); // Leere Liste zeigt korrekt "Keine Teams"
                            }
                            
                            return teams;
                        }
                        catch (Exception ex)
                        {
                            LoggingService.Instance.LogError("Error getting teams for mobile service", ex);
                            return new List<Team>(); // Leere Liste statt Test-Daten bei Fehlern
                        }
                    };
                    
                    mobileIntegrationService.GetEinsatzData = () =>
                    {
                        try
                        {
                            LoggingService.Instance.LogInfo("GetEinsatzData delegate called");
                            
                            if (mainViewModel == null)
                            {
                                LoggingService.Instance.LogWarning("MainViewModel is null - returning minimal EinsatzData");
                                return new EinsatzData
                                {
                                    Einsatzort = "Nicht gesetzt",
                                    Einsatzleiter = "Nicht gesetzt", 
                                    EinsatzDatum = DateTime.Now,
                                    IstEinsatz = true
                                };
                            }
                            
                            // Erstelle EinsatzData aus MainViewModel - verwende echte Daten oder "Nicht gesetzt"
                            var einsatzData = new EinsatzData
                            {
                                Einsatzort = ExtractEinsatzortFromViewModel(mainViewModel) ?? "Nicht gesetzt",
                                Einsatzleiter = ExtractEinsatzleiterFromViewModel(mainViewModel) ?? "Nicht gesetzt",
                                EinsatzDatum = DateTime.Now,
                                IstEinsatz = true
                            };
                            
                            LoggingService.Instance.LogInfo($"Mobile service accessed real einsatz data: {einsatzData.Einsatzort}");
                            return einsatzData;
                        }
                        catch (Exception ex)
                        {
                            LoggingService.Instance.LogError("Error getting einsatz data for mobile service", ex);
                            return new EinsatzData
                            {
                                Einsatzort = "Fehler beim Laden",
                                Einsatzleiter = "Fehler beim Laden",
                                EinsatzDatum = DateTime.Now,
                                IstEinsatz = true
                            };
                        }
                    };
                    
                    mobileIntegrationService.GetGlobalNotes = () =>
                    {
                        try
                        {
                            LoggingService.Instance.LogInfo("GetGlobalNotes delegate called");
                            
                            if (mainViewModel?.FilteredNotesCollection == null)
                            {
                                LoggingService.Instance.LogWarning("MainViewModel.FilteredNotesCollection is null - returning empty list");
                                return new List<GlobalNotesEntry>(); // Leere Liste statt Test-Notizen
                            }
                            
                            var notes = mainViewModel.FilteredNotesCollection.ToList();
                            LoggingService.Instance.LogInfo($"Mobile service accessed {notes.Count} real global notes successfully");
                            
                            // KORRIGIERT: Gib echte Notizen zurück, auch wenn die Liste leer ist
                            // Keine Test-Notizen mehr - das verwirrt den Benutzer
                            if (notes.Count == 0)
                            {
                                LoggingService.Instance.LogInfo("No notes found - returning empty list (real data)");
                                return new List<GlobalNotesEntry>(); // Leere Liste zeigt korrekt "Keine Notizen"
                            }
                            
                            return notes;
                        }
                        catch (Exception ex)
                        {
                            LoggingService.Instance.LogError("Error getting global notes for mobile service", ex);
                            return new List<GlobalNotesEntry>(); // Leere Liste statt Test-Notizen bei Fehlern
                        }
                    };
                    
                    // ViewModel mit MobileService verbinden
                    _viewModel?.SetMobileService(mobileIntegrationService);
                    
                    // Teste die Delegates nach der Konfiguration
                    TestDataDelegates(mobileIntegrationService);
                    
                    LoggingService.Instance.LogInfo("MobileConnectionWindow data integration setup completed successfully");
                    
                    // Zeige Erfolgs-Meldung über das ViewModel
                    if (_viewModel?.MobileService != null)
                    {
                        // Trigger Status-Update über das ViewModel
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            try
                            {
                                // Verwende eine sichere Methode um Status zu setzen
                                OnMobileServiceStatusChanged("✅ Datenintegration erfolgreich - Teams und Notizen verfügbar");
                            }
                            catch (Exception statusEx)
                            {
                                LoggingService.Instance.LogError("Error updating status after successful data integration", statusEx);
                            }
                        });
                    }
                }
                else
                {
                    LoggingService.Instance.LogError("MobileIntegrationService is null after initialization");
                    ShowMobileServiceInitializationError();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error setting up mobile data integration", ex);
                ShowDataIntegrationError($"Datenintegration fehlgeschlagen: {ex.Message}");
            }
        }

        /// <summary>
        /// Sichere Methode um Status-Updates an das ViewModel weiterzuleiten
        /// </summary>
        private void OnMobileServiceStatusChanged(string status)
        {
            try
            {
                // Verwende das ViewModel um Status-Updates zu verarbeiten - KORRIGIERT
                // Anstatt direkten Event-Zugriff verwenden wir eine sichere Methode
                if (_viewModel?.MobileService != null)
                {
                    // Verwende eine Hilfsmethode um den Status zu setzen
                    UpdateMobileServiceStatus(status);
                }
                LoggingService.Instance.LogInfo($"Status update sent to ViewModel: {status}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error sending status update to ViewModel", ex);
            }
        }

        /// <summary>
        /// Hilfsmethode um den Status sicher zu aktualisieren
        /// </summary>
        private void UpdateMobileServiceStatus(string status)
        {
            try
            {
                // Da wir nicht direkt auf das Event zugreifen können, verwenden wir die interne Methode des ViewModels
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    // Aktualisiere den Status im ViewModel durch Property-Setzen
                    if (_viewModel != null)
                    {
                        _viewModel.StatusText = status;
                        // Falls das ViewModel eine öffentliche Methode zum Status-Update hat, verwende sie hier
                        // Ansonsten wird der Status über die Property-Bindung aktualisiert
                    }
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating mobile service status", ex);
            }
        }

        /// <summary>
        /// Zeigt eine Fehlermeldung bei Datenintegrations-Problemen
        /// </summary>
        private void ShowDataIntegrationError(string errorDetails)
        {
            MessageBox.Show($"🚨 DATENINTEGRATION FEHLGESCHLAGENE\n\n" +
                           $"Die Mobile-Verbindung konnte nicht auf die Anwendungsdaten zugreifen.\n\n" +
                           $"Details: {errorDetails}\n\n" +
                           $"🔧 LÖSUNGSVORSCHLÄGE:\n\n" +
                           $"1️⃣ HAUPTFENSTER VERWENDEN:\n" +
                           $"   • Schließen Sie dieses Mobile-Fenster\n" +
                           $"   • Öffnen Sie es über das Hauptmenü\n\n" +
                           $"2️⃣ ANWENDUNG NEU STARTEN:\n" +
                           $"   • Beenden Sie die komplette Anwendung\n" +
                           $"   • Starten Sie als Administrator neu\n\n" +
                           $"3️⃣ NOTFALL-MODUS:\n" +
                           $"   • Das Mobile-Fenster funktioniert ohne Test-Daten\n" +
                           $"   • Für echte Daten: Teams im Hauptfenster erstellen\n\n" +
                           $"💡 Das Mobile-Fenster zeigt jetzt nur echte Daten an.",
                "Datenintegration fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Erstellt Fallback-EinsatzData
        /// </summary>
        private EinsatzData CreateFallbackEinsatzData()
        {
            return new EinsatzData
            {
                Einsatzort = "Demo-Einsatzort",
                Einsatzleiter = "Demo-Einsatzleiter",
                EinsatzDatum = DateTime.Now,
                IstEinsatz = true
            };
        }

        /// <summary>
        /// Erstellt Test-Notizen für Mobile-Anzeige
        /// </summary>
        private List<GlobalNotesEntry> CreateTestNotes()
        {
            var testNotes = new List<GlobalNotesEntry>();
            
            try
            {
                // Test Notiz 1
                testNotes.Add(new GlobalNotesEntry
                {
                    Content = "Einsatz gestartet - Mobile Demo aktiv",
                    TeamName = "System",
                    EntryType = GlobalNotesEntryType.EinsatzUpdate, // KORRIGIERT
                    Timestamp = DateTime.Now.AddMinutes(-30)
                });
                
                // Test Notiz 2
                testNotes.Add(new GlobalNotesEntry
                {
                    Content = "Demo Team Alpha im Sektor A eingesetzt",
                    TeamName = "Demo Team Alpha",
                    EntryType = GlobalNotesEntryType.TeamEvent, // KORRIGIERT
                    Timestamp = DateTime.Now.AddMinutes(-25)
                });
                
                // Test Notiz 3
                testNotes.Add(new GlobalNotesEntry
                {
                    Content = "Wetterbedingungen: Trocken, 18°C, leichter Wind",
                    TeamName = "Einsatzleitung",
                    EntryType = GlobalNotesEntryType.Manual,
                    Timestamp = DateTime.Now.AddMinutes(-20)
                });
                
                LoggingService.Instance.LogInfo($"Created {testNotes.Count} test notes for mobile display");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error creating test notes", ex);
            }
            
            return testNotes;
        }

        /// <summary>
        /// Testet die Daten-Delegates nach der Konfiguration
        /// </summary>
        private void TestDataDelegates(Services.MobileIntegrationService mobileService)
        {
            try
            {
                LoggingService.Instance.LogInfo("=== TESTING DATA DELEGATES ===");
                
                var testResults = new System.Text.StringBuilder();
                testResults.AppendLine("📊 DATENINTEGRATION TEST-ERGEBNISSE:");
                bool allTestsPassed = true;
                
                // Test GetCurrentTeams
                try
                {
                    LoggingService.Instance.LogInfo("Testing GetCurrentTeams delegate...");
                    var testTeams = mobileService.GetCurrentTeams?.Invoke();
                    
                    if (testTeams != null)
                    {
                        LoggingService.Instance.LogInfo($"✅ GetCurrentTeams test: SUCCESS - {testTeams.Count} teams");
                        testResults.AppendLine($"   ✅ Teams: {testTeams.Count} verfügbar");
                        
                        // Log team details for debugging
                        foreach (var team in testTeams.Take(3))
                        {
                            LoggingService.Instance.LogInfo($"      - Team: {team.TeamName} | Handler: {team.Hundefuehrer} | Status: {(team.IsRunning ? "Active" : "Ready")}");
                            testResults.AppendLine($"      • {team.TeamName} ({team.Hundefuehrer}) - {(team.IsRunning ? "Aktiv" : "Bereit")}");
                        }
                        
                        if (testTeams.Count > 3)
                        {
                            testResults.AppendLine($"      ... und {testTeams.Count - 3} weitere Teams");
                        }
                    }
                    else
                    {
                        LoggingService.Instance.LogError("❌ GetCurrentTeams test: FAILED - returned NULL");
                        testResults.AppendLine("   ❌ Teams: Delegate returned NULL");
                        allTestsPassed = false;
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("❌ GetCurrentTeams test failed with exception", ex);
                    testResults.AppendLine($"   ❌ Teams: Fehler - {ex.Message}");
                    allTestsPassed = false;
                }
                
                // Test GetEinsatzData
                try
                {
                    LoggingService.Instance.LogInfo("Testing GetEinsatzData delegate...");
                    var testEinsatz = mobileService.GetEinsatzData?.Invoke();
                    
                    if (testEinsatz != null)
                    {
                        LoggingService.Instance.LogInfo($"✅ GetEinsatzData test: SUCCESS");
                        LoggingService.Instance.LogInfo($"   - Einsatzort: {testEinsatz.Einsatzort}");
                        LoggingService.Instance.LogInfo($"   - Einsatzleiter: {testEinsatz.Einsatzleiter}");
                        LoggingService.Instance.LogInfo($"   - Datum: {testEinsatz.EinsatzDatum:yyyy-MM-dd HH:mm}");
                        
                        testResults.AppendLine($"   ✅ Einsatzdaten: Verfügbar");
                        testResults.AppendLine($"      • Ort: {testEinsatz.Einsatzort}");
                        testResults.AppendLine($"      • EL: {testEinsatz.Einsatzleiter}");
                        testResults.AppendLine($"      • Datum: {testEinsatz.EinsatzDatum:dd.MM.yyyy HH:mm}");
                    }
                    else
                    {
                        LoggingService.Instance.LogError("❌ GetEinsatzData test: FAILED - returned NULL");
                        testResults.AppendLine("   ❌ Einsatzdaten: Delegate returned NULL");
                        allTestsPassed = false;
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("❌ GetEinsatzData test failed with exception", ex);
                    testResults.AppendLine($"   ❌ Einsatzdaten: Fehler - {ex.Message}");
                    allTestsPassed = false;
                }
                
                // Test GetGlobalNotes
                try
                {
                    LoggingService.Instance.LogInfo("Testing GetGlobalNotes delegate...");
                    var testNotes = mobileService.GetGlobalNotes?.Invoke();
                    
                    if (testNotes != null)
                    {
                        LoggingService.Instance.LogInfo($"✅ GetGlobalNotes test: SUCCESS - {testNotes.Count} notes");
                        testResults.AppendLine($"   ✅ Notizen: {testNotes.Count} verfügbar");
                        
                        // Log note details for debugging
                        foreach (var note in testNotes.Take(3))
                        {
                            var notePreview = note.Content.Length > 50 ? note.Content.Substring(0, 47) + "..." : note.Content;
                            LoggingService.Instance.LogInfo($"      - Note: {notePreview} | Team: {note.TeamName} | Type: {note.EntryType}");
                            testResults.AppendLine($"      • {note.FormattedTimestamp}: {notePreview}");
                        }
                        
                        if (testNotes.Count > 3)
                        {
                            testResults.AppendLine($"      ... und {testNotes.Count - 3} weitere Notizen");
                        }
                    }
                    else
                    {
                        LoggingService.Instance.LogError("❌ GetGlobalNotes test: FAILED - returned NULL");
                        testResults.AppendLine("   ❌ Notizen: Delegate returned NULL");
                        allTestsPassed = false;
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("❌ GetGlobalNotes test failed with exception", ex);
                    testResults.AppendLine($"   ❌ Notizen: Fehler - {ex.Message}");
                    allTestsPassed = false;
                }
                
                // Zusammenfassung
                if (allTestsPassed)
                {
                    testResults.AppendLine();
                    testResults.AppendLine("🎉 ALLE TESTS ERFOLGREICH - Mobile Browser erhält vollständige Daten!");
                    LoggingService.Instance.LogInfo("🎉 All data delegate tests passed successfully");
                    
                    // Status-Update für positive Bestätigung über ViewModel
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        OnMobileServiceStatusChanged("✅ Datenintegration vollständig - Browser bekommt alle Anwendungsdaten");
                    });
                }
                else
                {
                    testResults.AppendLine();
                    testResults.AppendLine("⚠️ EINIGE TESTS FEHLGESCHLAGEN - Mobile Browser hat eingeschränkte Daten");
                    LoggingService.Instance.LogWarning("⚠️ Some data delegate tests failed");
                    
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        OnMobileServiceStatusChanged("⚠️ Datenintegration teilweise - Browser verwendet Demo-Daten");
                    });
                }
                
                LoggingService.Instance.LogInfo("Data delegates testing completed");
                LoggingService.Instance.LogInfo(testResults.ToString());
                
                // Zusätzlicher Test: API-Endpoints
                TestAPIEndpoints();
                
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Critical error testing data delegates", ex);
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    OnMobileServiceStatusChanged($"❌ Kritischer Test-Fehler: {ex.Message}");
                });
            }
        }

        /// <summary>
        /// Testet die API-Endpoints direkt
        /// </summary>
        private async void TestAPIEndpoints()
        {
            try
            {
                LoggingService.Instance.LogInfo("=== TESTING API ENDPOINTS ===");
                
                var baseUrl = $"http://localhost:8080";
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                
                // Test /api/teams mit Datenvalidierung
                try
                {
                    LoggingService.Instance.LogInfo("Testing /api/teams endpoint...");
                    var teamsResponse = await client.GetAsync($"{baseUrl}/api/teams");
                    
                    if (teamsResponse.IsSuccessStatusCode)
                    {
                        var teamsContent = await teamsResponse.Content.ReadAsStringAsync();
                        LoggingService.Instance.LogInfo($"✅ API /api/teams: SUCCESS - {teamsContent.Length} chars");
                        
                        // Versuche JSON zu parsen um Datenstruktur zu validieren
                        try
                        {
                            var teamsData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(teamsContent);
                            if (teamsData.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                var teamCount = teamsData.GetArrayLength();
                                LoggingService.Instance.LogInfo($"✅ Teams API: {teamCount} Teams im JSON gefunden");
                                
                                // Validiere ersten Team-Eintrag falls vorhanden
                                if (teamCount > 0)
                                {
                                    var firstTeam = teamsData[0];
                                    var hasName = firstTeam.TryGetProperty("name", out var nameProperty);
                                    var hasStatus = firstTeam.TryGetProperty("status", out var statusProperty);
                                    var hasHandler = firstTeam.TryGetProperty("handler", out var handlerProperty);
                                    
                                    LoggingService.Instance.LogInfo($"✅ Team-Datenstruktur: Name={hasName}, Status={hasStatus}, Handler={hasHandler}");
                                    
                                    if (hasName)
                                    {
                                        LoggingService.Instance.LogInfo($"   📋 Beispiel-Team: {nameProperty.GetString()}");
                                    }
                                }
                            }
                        }
                        catch (Exception jsonEx)
                        {
                            LoggingService.Instance.LogWarning($"Teams API JSON-Parse Fehler: {jsonEx.Message}");
                            LoggingService.Instance.LogInfo($"Raw API Response (first 200 chars): {teamsContent.Substring(0, Math.Min(200, teamsContent.Length))}");
                        }
                    }
                    else
                    {
                        LoggingService.Instance.LogWarning($"⚠️ API /api/teams: {teamsResponse.StatusCode} - {teamsResponse.ReasonPhrase}");
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("❌ API /api/teams failed", ex);
                }
                
                // Test /api/status mit Einsatzdaten-Validierung
                try
                {
                    LoggingService.Instance.LogInfo("Testing /api/status endpoint...");
                    var statusResponse = await client.GetAsync($"{baseUrl}/api/status");
                    
                    if (statusResponse.IsSuccessStatusCode)
                    {
                        var statusContent = await statusResponse.Content.ReadAsStringAsync();
                        LoggingService.Instance.LogInfo($"✅ API /api/status: SUCCESS - {statusContent.Length} chars");
                        
                        // Validiere Status-Daten
                        try
                        {
                            var statusData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(statusContent);
                            
                            if (statusData.TryGetProperty("mission", out var missionData))
                            {
                                var hasLocation = missionData.TryGetProperty("location", out var locationProperty);
                                var hasLeader = missionData.TryGetProperty("leader", out var leaderProperty);
                                var hasDuration = missionData.TryGetProperty("duration", out var durationProperty);
                                
                                LoggingService.Instance.LogInfo($"✅ Einsatz-Datenstruktur: Ort={hasLocation}, EL={hasLeader}, Dauer={hasDuration}");
                                
                                if (hasLocation && hasLeader)
                                {
                                    LoggingService.Instance.LogInfo($"   📍 Einsatzort: {locationProperty.GetString()}");
                                    LoggingService.Instance.LogInfo($"   👨‍💼 Einsatzleiter: {leaderProperty.GetString()}");
                                }
                            }
                            
                            if (statusData.TryGetProperty("teams", out var teamsData))
                            {
                                var hasTotal = teamsData.TryGetProperty("total", out var totalProperty);
                                var hasActive = teamsData.TryGetProperty("active", out var activeProperty);
                                
                                if (hasTotal && hasActive)
                                {
                                    LoggingService.Instance.LogInfo($"   🎯 Teams: {activeProperty.GetInt32()}/{totalProperty.GetInt32()} aktiv");
                                }
                            }
                        }
                        catch (Exception jsonEx)
                        {
                            LoggingService.Instance.LogWarning($"Status API JSON-Parse Fehler: {jsonEx.Message}");
                        }
                    }
                    else
                    {
                        LoggingService.Instance.LogWarning($"⚠️ API /api/status: {statusResponse.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("❌ API /api/status failed", ex);
                }
                
                // Test /api/notes mit Notizen-Validierung
                try
                {
                    LoggingService.Instance.LogInfo("Testing /api/notes endpoint...");
                    var notesResponse = await client.GetAsync($"{baseUrl}/api/notes");
                    
                    if (notesResponse.IsSuccessStatusCode)
                    {
                        var notesContent = await notesResponse.Content.ReadAsStringAsync();
                        LoggingService.Instance.LogInfo($"✅ API /api/notes: SUCCESS - {notesContent.Length} chars");
                        
                        // Validiere Notizen-Daten
                        try
                        {
                            var notesData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(notesContent);
                            if (notesData.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                var noteCount = notesData.GetArrayLength();
                                LoggingService.Instance.LogInfo($"✅ Notes API: {noteCount} Notizen im JSON gefunden");
                                
                                // Validiere erste Notiz falls vorhanden
                                if (noteCount > 0)
                                {
                                    var firstNote = notesData[0];
                                    var hasContent = firstNote.TryGetProperty("content", out var contentProperty);
                                    var hasTimestamp = firstNote.TryGetProperty("timestamp", out var timestampProperty);
                                    var hasTeam = firstNote.TryGetProperty("teamName", out var teamProperty);
                                    
                                    LoggingService.Instance.LogInfo($"✅ Notiz-Datenstruktur: Content={hasContent}, Time={hasTimestamp}, Team={hasTeam}");
                                    
                                    if (hasContent && hasTimestamp)
                                    {
                                        var notePreview = contentProperty.GetString();
                                        if (notePreview?.Length > 50)
                                        {
                                            notePreview = notePreview.Substring(0, 47) + "...";
                                        }
                                        LoggingService.Instance.LogInfo($"   📝 Beispiel-Notiz: {notePreview}");
                                    }
                                }
                            }
                        }
                        catch (Exception jsonEx)
                        {
                            LoggingService.Instance.LogWarning($"Notes API JSON-Parse Fehler: {jsonEx.Message}");
                        }
                    }
                    else
                    {
                        LoggingService.Instance.LogWarning($"⚠️ API /api/notes: {notesResponse.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("❌ API /api/notes failed", ex);
                }
                
                LoggingService.Instance.LogInfo("=== API ENDPOINT TESTING COMPLETED ===");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error testing API endpoints", ex);
            }
        }

        /// <summary>
        /// Extrahiert den Einsatzort aus dem MainViewModel
        /// </summary>
        private string? ExtractEinsatzortFromViewModel(MainViewModel mainViewModel)
        {
            try
            {
                // Das MainViewModel hat eine Einsatzort-Property
                var einsatzort = mainViewModel.Einsatzort;
                
                // Bereinige das Format (entferne "Ort: " Prefix falls vorhanden)
                if (!string.IsNullOrEmpty(einsatzort) && einsatzort.StartsWith("Ort: "))
                {
                    return einsatzort.Substring(5);
                }
                
                return einsatzort;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Extrahiert den Einsatzleiter aus dem MainViewModel
        /// </summary>
        private string? ExtractEinsatzleiterFromViewModel(MainViewModel mainViewModel)
        {
            try
            {
                // Das MainViewModel hat eine Einsatzleiter-Property
                var einsatzleiter = mainViewModel.Einsatzleiter;
                
                // Bereinige das Format (entferne "EL: " Prefix falls vorhanden)
                if (!string.IsNullOrEmpty(einsatzleiter) && einsatzleiter.StartsWith("EL: "))
                {
                    return einsatzleiter.Substring(4);
                }
                
                return einsatzleiter;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Initialisiert den MobileService mit mehreren Versuchen
        /// </summary>
        private bool InitializeMobileServiceWithRetry()
        {
            int maxAttempts = 3;
            
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    LoggingService.Instance.LogInfo($"MobileService initialization attempt {attempt}/{maxAttempts}");
                    
                    // Detaillierte Vorab-Diagnose
                    LogMobileServiceDiagnostics($"Before attempt {attempt}");
                    
                    // NEUE STRATEGIE: Verwende DiagnoseAndRepair für bessere Erfolgsrate
                    if (attempt == 1)
                    {
                        LoggingService.Instance.LogInfo("Using DiagnoseAndRepair for first attempt");
                        if (MobileService.Instance.DiagnoseAndRepair())
                        {
                            LoggingService.Instance.LogInfo("DiagnoseAndRepair successful on first attempt");
                            LogMobileServiceDiagnostics("After successful DiagnoseAndRepair");
                            return true;
                        }
                        else
                        {
                            LoggingService.Instance.LogWarning("DiagnoseAndRepair failed on first attempt - trying manual methods");
                        }
                    }
                    
                    // Versuche eine Neuverbindung wenn der Service nicht gesund ist
                    if (!MobileService.Instance.IsHealthy())
                    {
                        LoggingService.Instance.LogInfo("MobileService not healthy, attempting reconnect...");
                        
                        // Stoppe zuerst den alten Service
                        try
                        {
                            MobileService.Instance.Disconnect();
                            LoggingService.Instance.LogInfo("Previous service disconnected");
                        }
                        catch (Exception ex)
                        {
                            LoggingService.Instance.LogWarning($"Error disconnecting previous service: {ex.Message}");
                        }
                        
                        // Warte kurz
                        System.Threading.Thread.Sleep(500);
                        
                        // Versuche Reconnect
                        if (MobileService.Instance.TryReconnect())
                        {
                            LoggingService.Instance.LogInfo($"MobileService reconnection successful on attempt {attempt}");
                            
                            // Verifikation nach Reconnect
                            LogMobileServiceDiagnostics($"After successful reconnect attempt {attempt}");
                            return true;
                        }
                        else
                        {
                            LoggingService.Instance.LogWarning($"MobileService reconnection failed on attempt {attempt}");
                        }
                    }
                    else
                    {
                        LoggingService.Instance.LogInfo("MobileService is healthy");
                        return true;
                    }
                    
                    // Falls das nicht funktioniert, versuche normale Verbindung
                    LoggingService.Instance.LogInfo("Attempting fresh connection...");
                    MobileService.Instance.Connect();
                    
                    // Detaillierte Nachprüfung
                    LogMobileServiceDiagnostics($"After connect attempt {attempt}");
                    
                    if (MobileService.Instance.IsHealthy())
                    {
                        LoggingService.Instance.LogInfo($"MobileService initialization successful on attempt {attempt}");
                        return true;
                    }
                    else
                    {
                        LoggingService.Instance.LogWarning($"MobileService still not healthy after attempt {attempt}");
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError($"MobileService initialization attempt {attempt} failed", ex);
                    
                    if (attempt < maxAttempts)
                    {
                        LoggingService.Instance.LogInfo($"Waiting before retry attempt {attempt + 1}...");
                        System.Threading.Thread.Sleep(1000); // Wait 1 second before retry
                    }
                }
            }
            
            // Finale Diagnose bei Fehlschlag mit letztem Reparaturversuch
            LoggingService.Instance.LogWarning("All standard attempts failed - trying final DiagnoseAndRepair");
            if (MobileService.Instance.DiagnoseAndRepair())
            {
                LoggingService.Instance.LogInfo("Final DiagnoseAndRepair successful");
                return true;
            }
            
            // Finale Diagnose bei Fehlschlag
            LogMobileServiceDiagnostics("Final diagnosis after all attempts failed");
            LoggingService.Instance.LogError($"MobileService initialization failed after {maxAttempts} attempts");
            return false;
        }

        /// <summary>
        /// Loggt detaillierte MobileService-Diagnostik-Informationen
        /// </summary>
        private void LogMobileServiceDiagnostics(string context)
        {
            try
            {
                LoggingService.Instance.LogInfo($"=== Mobile Service Diagnostics - {context} ===");
                
                // Service-Health
                var isHealthy = MobileService.Instance.IsHealthy();
                LoggingService.Instance.LogInfo($"Service Healthy: {isHealthy}");
                
                // Service-Instance verfügbar?
                var serviceInstance = MobileService.Instance.GetMobileIntegrationService();
                LoggingService.Instance.LogInfo($"Service Instance Available: {serviceInstance != null}");
                
                if (serviceInstance != null)
                {
                    LoggingService.Instance.LogInfo($"Service Running: {serviceInstance.IsRunning}");
                    LoggingService.Instance.LogInfo($"Service Local IP: {serviceInstance.LocalIPAddress}");
                }
                
                // Detaillierte Service-Info
                var diagnosticInfo = MobileService.Instance.GetDiagnosticInfo();
                LoggingService.Instance.LogInfo($"Detailed Diagnostics:\n{diagnosticInfo}");
                
                LoggingService.Instance.LogInfo($"=== End Diagnostics - {context} ===");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error during diagnostics logging for {context}", ex);
            }
        }

        /// <summary>
        /// Zeigt eine detaillierte Fehlermeldung bei MobileService-Initialisierungsproblemen
        /// </summary>
        private void ShowMobileServiceInitializationError()
        {
            var diagnosticInfo = MobileService.Instance.GetDiagnosticInfo();
            
            var errorMessage = $"🚨 MOBILE SERVICE INITIALISIERUNG FEHLGESCHLAGENE\n\n" +
                             $"Der Mobile Service konnte nicht gestartet werden.\n\n" +
                             $"📊 DIAGNOSE-INFORMATIONEN:\n" +
                             $"{diagnosticInfo}\n" +
                             $"🔧 LÖSUNGSVORSCHLÄGE:\n\n" +
                             $"1️⃣ ALS ADMINISTRATOR STARTEN:\n" +
                             $"   • Rechtsklick auf die .exe-Datei\n" +
                             $"   • 'Als Administrator ausführen'\n\n" +
                             $"2️⃣ PORT-KONFLIKTE LÖSEN:\n" +
                             $"   • Andere Apps schließen die Port 8080 verwenden\n" +
                             $"   • Besonders: Skype, IIS, Apache, Jenkins\n\n" +
                             $"3️⃣ WINDOWS FIREWALL:\n" +
                             $"   • Port 8080 für die App freigeben\n" +
                             $"   • Temporär Firewall deaktivieren (Test)\n\n" +
                             $"4️⃣ SYSTEM-NEUSTART:\n" +
                             $"   • Computer neu starten\n" +
                             $"   • App erneut als Administrator starten\n\n" +
                             $"💡 Das Mobile-Fenster bleibt geöffnet, aber Server-Funktionen sind eingeschränkt.";
            
            MessageBox.Show(errorMessage, "Mobile Service Initialisierung fehlgeschlagen", 
                           MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowTrayNotification()
        {
            try
            {
                // Placeholder für Tray-Notification
                LoggingService.Instance.LogInfo("Tray notification requested (not implemented)");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing tray notification", ex);
            }
        }

        private void PerformSystemDiagnose()
        {
            try
            {
                var diagnoseMeldung = GenerateSystemDiagnose();
                
                MessageBox.Show(diagnoseMeldung, "System-Diagnose", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                LoggingService.Instance.LogInfo("System diagnose performed");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error performing system diagnose", ex);
                MessageBox.Show($"Fehler bei der System-Diagnose: {ex.Message}", 
                    "Diagnose-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateSystemDiagnose()
        {
            var diagnose = new System.Text.StringBuilder();
            diagnose.AppendLine("🔍 EINSATZÜBERWACHUNG MOBILE - SYSTEM-DIAGNOSE");
            diagnose.AppendLine("=" + new string('=', 50));
            diagnose.AppendLine();

            try
            {
                // Theme Service Status
                var themeManager = UnifiedThemeManager.Instance;
                diagnose.AppendLine($"🎨 Theme Service: {themeManager.CurrentThemeStatus}");
                diagnose.AppendLine($"   • Modus: {(themeManager.IsAutoMode ? "Automatisch" : "Manuell")}");
                diagnose.AppendLine($"   • Aktuell: {(themeManager.IsDarkMode ? "Dunkel" : "Hell")}");
                diagnose.AppendLine($"   • Animationen: {(themeManager.EnableAnimations ? "Aktiviert" : "Deaktiviert")}");
                diagnose.AppendLine();

                // Betriebssystem
                diagnose.AppendLine($"💻 Betriebssystem: {Environment.OSVersion}");
                diagnose.AppendLine($"🔧 .NET Version: {Environment.Version}");
                diagnose.AppendLine($"💾 Arbeitsspeicher: {GC.GetTotalMemory(false) / (1024 * 1024):N0} MB");
                diagnose.AppendLine();

                // MainViewModel Verfügbarkeit - mit detaillierterer Prüfung
                var mainViewModel = MainViewModelService.Instance.GetMainViewModel();
                diagnose.AppendLine($"📊 MainViewModel: {(mainViewModel != null ? "✅ Verfügbar" : "❌ Nicht verfügbar")}");
                
                if (mainViewModel != null)
                {
                    var teamCount = mainViewModel.Teams?.Count ?? 0;
                    var activeTeams = mainViewModel.Teams?.Count(t => t.IsRunning) ?? 0;
                    var notesCount = mainViewModel.FilteredNotesCollection?.Count ?? 0;
                    
                    diagnose.AppendLine($"   • Teams gesamt: {teamCount}");
                    diagnose.AppendLine($"   • Teams aktiv: {activeTeams}");
                    diagnose.AppendLine($"   • Globale Notizen: {notesCount}");
                    diagnose.AppendLine($"   • Einsatzort: {mainViewModel.Einsatzort ?? "Nicht gesetzt"}");
                    diagnose.AppendLine($"   • Einsatzleiter: {mainViewModel.Einsatzleiter ?? "Nicht gesetzt"}");
                    
                    // Zusätzliche MainViewModel-Tests
                    diagnose.AppendLine($"   • Teams Collection: {(mainViewModel.Teams != null ? "✅ OK" : "❌ NULL")}");
                    diagnose.AppendLine($"   • Notes Collection: {(mainViewModel.FilteredNotesCollection != null ? "✅ OK" : "❌ NULL")}");
                }
                diagnose.AppendLine();

                // Mobile Service Status - erweiterte Diagnose
                diagnose.AppendLine("📱 MOBILE SERVICE - DETAILLIERTE ANALYSE:");
                
                try
                {
                    var mobileServiceHealthy = MobileService.Instance.IsHealthy();
                    diagnose.AppendLine($"   • Service Health: {(mobileServiceHealthy ? "✅ Gesund" : "❌ Ungesund")}");
                    
                    var mobileService = MobileService.Instance.GetMobileIntegrationService();
                    diagnose.AppendLine($"   • Service Instance: {(mobileService != null ? "✅ Verfügbar" : "❌ Nicht verfügbar")}");
                    
                    if (mobileService != null)
                    {
                        diagnose.AppendLine($"   • Server Status: {(mobileService.IsRunning ? "✅ Läuft" : "⏸️ Gestoppt")}");
                        diagnose.AppendLine($"   • Lokale IP: {mobileService.LocalIPAddress}");
                        diagnose.AppendLine($"   • QR-Code URL: {mobileService.QRCodeUrl}");
                        
                        // ERWEITERTE Delegate-Tests
                        diagnose.AppendLine($"🔗 DATEN-DELEGATES ANALYSE:");
                        
                        // Test GetCurrentTeams
                        try
                        {
                            var testTeams = mobileService.GetCurrentTeams?.Invoke();
                            if (testTeams != null)
                            {
                                diagnose.AppendLine($"   • GetCurrentTeams: ✅ OK ({testTeams.Count} Teams)");
                                if (testTeams.Any())
                                {
                                    diagnose.AppendLine($"     - Beispiel Team: {testTeams.First().TeamName ?? "Unnamed"}");
                                }
                            }
                            else
                            {
                                diagnose.AppendLine($"   • GetCurrentTeams: ❌ Returned NULL");
                            }
                        }
                        catch (Exception ex)
                        {
                            diagnose.AppendLine($"   • GetCurrentTeams: ❌ Exception: {ex.Message}");
                        }
                        
                        // Test GetEinsatzData
                        try
                        {
                            var testEinsatz = mobileService.GetEinsatzData?.Invoke();
                            if (testEinsatz != null)
                            {
                                diagnose.AppendLine($"   • GetEinsatzData: ✅ OK");
                                diagnose.AppendLine($"     - Einsatzort: {testEinsatz.Einsatzort ?? "NULL"}");
                                diagnose.AppendLine($"     - Einsatzleiter: {testEinsatz.Einsatzleiter ?? "NULL"}");
                            }
                            else
                            {
                                diagnose.AppendLine($"   • GetEinsatzData: ❌ Returned NULL");
                            }
                        }
                        catch (Exception ex)
                        {
                            diagnose.AppendLine($"   • GetEinsatzData: ❌ Exception: {ex.Message}");
                        }
                        
                        // Test GetGlobalNotes
                        try
                        {
                            var testNotes = mobileService.GetGlobalNotes?.Invoke();
                            if (testNotes != null)
                            {
                                diagnose.AppendLine($"   • GetGlobalNotes: ✅ OK ({testNotes.Count} Notizen)");
                            }
                            else
                            {
                                diagnose.AppendLine($"   • GetGlobalNotes: ❌ Returned NULL");
                            }
                        }
                        catch (Exception ex)
                        {
                            diagnose.AppendLine($"   • GetGlobalNotes: ❌ Exception: {ex.Message}");
                        }
                    }
                    else
                    {
                        // Detaillierte Service-Reinitialisierungs-Analyse
                        diagnose.AppendLine("🔄 SERVICE-REINITIALISIERUNG ANALYSE:");
                        
                        try
                        {
                            // Versuche Diagnose der Service-Erstellung
                            diagnose.AppendLine("   Versuche Service-Neuinitialisierung...");
                            MobileService.Instance.Connect();
                            
                            mobileService = MobileService.Instance.GetMobileIntegrationService();
                            diagnose.AppendLine($"   • Service-Neuinitialisierung: {(mobileService != null ? "✅ Erfolgreich" : "❌ Fehlgeschlagen")}");
                            
                            if (mobileService == null)
                            {
                                // Tiefere Diagnose warum Service NULL ist
                                diagnose.AppendLine("   • PROBLEM: Service ist nach Connect() immer noch NULL");
                                diagnose.AppendLine("   • MÖGLICHE URSACHEN:");
                                diagnose.AppendLine("     - HttpListener.IsSupported = false");
                                diagnose.AppendLine("     - Network interface problems");
                                diagnose.AppendLine("     - Port conflicts");
                                diagnose.AppendLine("     - Security restrictions");
                            }
                        }
                        catch (Exception ex)
                        {
                            diagnose.AppendLine($"   • Service-Initialisierung FEHLER: {ex.GetType().Name}");
                            diagnose.AppendLine($"     - Message: {ex.Message}");
                            if (ex.InnerException != null)
                            {
                                diagnose.AppendLine($"     - Inner Exception: {ex.InnerException.Message}");
                            }
                        }
                    }
                    
                    // Service-Diagnostik-Details
                    diagnose.AppendLine();
                    diagnose.AppendLine("📋 SERVICE-DIAGNOSTIK-DETAILS:");
                    var diagnosticInfo = MobileService.Instance.GetDiagnosticInfo();
                    diagnose.AppendLine($"{diagnosticInfo}");
                }
                catch (Exception ex)
                {
                    diagnose.AppendLine($"❌ Mobile Service Diagnose-Fehler: {ex.Message}");
                }
                
                diagnose.AppendLine();

                // Administrator-Rechte
                var isAdmin = IsRunningAsAdministrator();
                diagnose.AppendLine($"🔐 Administrator-Rechte: {(isAdmin ? "✅ Verfügbar" : "❌ Nicht verfügbar")}");
                
                if (!isAdmin)
                {
                    diagnose.AppendLine("💡 Für optimale Netzwerk-Features als Administrator starten");
                }
                
                // HttpListener Support
                var httpListenerSupported = System.Net.HttpListener.IsSupported;
                diagnose.AppendLine($"🌐 HttpListener Support: {(httpListenerSupported ? "✅ Unterstützt" : "❌ NICHT unterstützt")}");
                
                if (!httpListenerSupported)
                {
                    diagnose.AppendLine("🚨 KRITISCH: HttpListener nicht unterstützt - Mobile Service kann nicht funktionieren!");
                }
                
                diagnose.AppendLine();

                // Netzwerk-Interfaces
                diagnose.AppendLine("🌐 Netzwerk-Interfaces:");
                var networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up);
                
                foreach (var ni in networkInterfaces.Take(5))
                {
                    var ipv4Addresses = ni.GetIPProperties().UnicastAddresses
                        .Where(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        .Select(addr => addr.Address.ToString());
                    
                    var ipInfo = ipv4Addresses.Any() ? string.Join(", ", ipv4Addresses) : "Keine IPv4";
                    diagnose.AppendLine($"   • {ni.Name} ({ni.NetworkInterfaceType}): {ipInfo}");
                }
                diagnose.AppendLine();

                // Port-Verfügbarkeit
                diagnose.AppendLine("🔌 PORT-VERFÜGBARKEIT:");
                var tcpConnections = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
                var relevantPorts = new[] { 8080, 8081, 8082, 8083 };
                
                foreach (var port in relevantPorts)
                {
                    var portInUse = tcpConnections.Any(endpoint => endpoint.Port == port);
                    diagnose.AppendLine($"   • Port {port}: {(portInUse ? "❌ Belegt" : "✅ Verfügbar")}");
                }
                diagnose.AppendLine();

                // Empfehlungen basierend auf Befunden
                diagnose.AppendLine("💡 SPEZIFISCHE EMPFEHLUNGEN:");
                
                if (!httpListenerSupported)
                {
                    diagnose.AppendLine("   🚨 KRITISCH: HttpListener nicht verfügbar");
                    diagnose.AppendLine("   • Windows-Features prüfen");
                    diagnose.AppendLine("   • .NET Installation reparieren");
                }
                
                if (mainViewModel == null)
                {
                    diagnose.AppendLine("   🚨 KRITISCH: MainViewModel nicht verfügbar - App-Neustart empfohlen");
                }
                
                var mobileServiceInstance = MobileService.Instance.GetMobileIntegrationService();
                if (mobileServiceInstance == null)
                {
                    diagnose.AppendLine("   🔧 MOBILE SERVICE PROBLEME:");
                    diagnose.AppendLine("   • Schließen Sie das Mobile-Fenster und öffnen es neu");
                    diagnose.AppendLine("   • Starten Sie die App als Administrator neu");
                    diagnose.AppendLine("   • Prüfen Sie andere Apps die Port 8080 verwenden");
                }
                
                if (!isAdmin)
                {
                    diagnose.AppendLine("   • Als Administrator starten für iPhone/Android-Zugriff");
                }
                
                diagnose.AppendLine("   • Firewall-Port 8080 freigeben");
                diagnose.AppendLine("   • Desktop und Mobile im gleichen WLAN");
                
                diagnose.AppendLine();
                diagnose.AppendLine("🔧 SOFORTMASSNAHMEN:");
                diagnose.AppendLine("   1. App als Administrator neu starten");
                diagnose.AppendLine("   2. Mobile-Fenster schließen und neu öffnen");
                diagnose.AppendLine("   3. 'Server starten' Button verwenden");
                diagnose.AppendLine("   4. Bei Fehlern: Computer neu starten");

            }
            catch (Exception ex)
            {
                diagnose.AppendLine($"❌ FEHLER BEI DIAGNOSE: {ex.Message}");
                diagnose.AppendLine($"📚 Stack-Trace: {ex.StackTrace}");
            }

            return diagnose.ToString();
        }

        private bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // ViewModel cleanup via IDisposable
                _viewModel?.Dispose();
                LoggingService.Instance.LogInfo("MobileConnectionWindow closed and cleaned up with theme support");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during MobileConnectionWindow cleanup", ex);
            }
            finally
            {
                base.OnClosed(e);
            }
        }

        /// <summary>
        /// Public method für externes Force-Close (MainWindow Integration)
        /// </summary>
        public void ForceClose()
        {
            try
            {
                _viewModel?.ForceClose();
                Close();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during ForceClose", ex);
            }
        }

        /// <summary>
        /// Statische Factory-Methode für einfache Integration aus MainWindow
        /// </summary>
        /// <param name="mainViewModel">Das MainViewModel für Daten-Zugriff</param>
        /// <returns>Konfigurierte MobileConnectionWindow-Instanz</returns>
        public static MobileConnectionWindow CreateWithDataIntegration(MainViewModel mainViewModel)
        {
            return new MobileConnectionWindow(mainViewModel);
        }
    }
}
