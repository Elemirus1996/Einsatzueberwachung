using System;
using System.Threading.Tasks;
using System.Windows;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // WICHTIG: ShutdownMode NICHT hier setzen!
                // Standard ist OnLastWindowClose, was wir für StartWindow -> MainWindow Transition brauchen
                
                // Initialize theme system VERY early and apply immediately
                LoggingService.Instance.LogInfo("🎨 Initializing Theme Service...");
                var themeService = ThemeService.Instance;
                
                // DIREKTE Theme-Anwendung - GARANTIERT korrekt
                await ApplyThemeDirectly(themeService.IsDarkMode);
                
                LoggingService.Instance.LogInfo($"🎨 Theme Service initialized - Status: {themeService.CurrentThemeStatus}");
                
                // Log startup mit zentraler Versionsverwaltung
                LoggingService.Instance.LogInfo($"🚀 {VersionService.FullProductName} starting up...");
                LoggingService.Instance.LogInfo($"📍 Startup Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                LoggingService.Instance.LogInfo($"💻 OS: {Environment.OSVersion}");
                LoggingService.Instance.LogInfo($"🔧 .NET: {Environment.Version}");
                LoggingService.Instance.LogInfo($"📦 Version: {VersionService.DisplayVersion} (Compiled: {VersionService.CompiledVersion})");
                
                // Version-Konsistenz prüfen
                if (!VersionService.IsVersionConsistent)
                {
                    LoggingService.Instance.LogWarning($"⚠️ Version-Inkonsistenz: Static={VersionService.Version}, Compiled={VersionService.CompiledVersion}");
                }
                
                // WICHTIG: Initialize master data service synchron vor StartWindow
                await InitializeMasterDataAsync();
                
                // Check for crash recovery
                CheckForCrashRecovery();
                
                // Check for updates - nur für Release-Versionen
                if (!VersionService.IsDevelopmentVersion)
                {
                    _ = CheckForUpdatesAsync();
                }
                else
                {
                    LoggingService.Instance.LogInfo("🚧 Development version detected - Update check disabled");
                }
                
                // JETZT erst das StartWindow öffnen
                var startWindow = new Views.StartWindow();
                startWindow.Show();
                
                LoggingService.Instance.LogInfo($"✅ StartWindow opened with theme: {(themeService.IsDarkMode ? "Dark" : "Light")}");
                
                base.OnStartup(e);
            }
            catch (System.Exception ex)
            {
                LoggingService.Instance.LogError("❌ Error during application startup", ex);
                
                // Still try to start the app with fallback
                try
                {
                    var startWindow = new Views.StartWindow();
                    startWindow.Show();
                }
                catch (System.Exception fallbackEx)
                {
                    LoggingService.Instance.LogError("❌ Even fallback startup failed", fallbackEx);
                    MessageBox.Show($"Schwerwiegender Fehler beim Starten der Anwendung:\n{ex.Message}\n\nFallback-Fehler:\n{fallbackEx.Message}", 
                        "Startup-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown(1);
                    return;
                }
                
                base.OnStartup(e);
            }
        }

        /// <summary>
        /// Direkte, zuverlässige Theme-Anwendung ohne Abhängigkeiten
        /// </summary>
        private async Task ApplyThemeDirectly(bool isDarkMode)
        {
            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    var app = Application.Current;
                    if (app?.Resources == null) return;

                    System.Diagnostics.Debug.WriteLine($"=== DIRECT THEME APPLICATION ===");
                    System.Diagnostics.Debug.WriteLine($"Current Time: {DateTime.Now:HH:mm:ss}");
                    System.Diagnostics.Debug.WriteLine($"Is Dark Mode: {isDarkMode}");

                    // DIREKTE Anwendung der Theme-Farben
                    if (isDarkMode)
                    {
                        // ===== DARK MODE FARBEN =====
                        app.Resources["Surface"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(18, 18, 18)); // #121212
                        app.Resources["SurfaceVariant"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)); // #1E1E1E
                        app.Resources["SurfaceContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26)); // #1A1A1A
                        app.Resources["SurfaceContainerHigh"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(36, 36, 36)); // #242424
                        app.Resources["SurfaceContainerHighest"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 44, 44)); // #2C2C2C
                        app.Resources["OnSurface"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(227, 227, 227)); // #E3E3E3
                        app.Resources["OnSurfaceVariant"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(179, 179, 179)); // #B3B3B3
                        
                        // Orange Primary Colors for Dark Mode
                        app.Resources["Primary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 183, 77)); // #FFB74D
                        app.Resources["PrimaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 81, 0)); // #E65100
                        app.Resources["OnPrimary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 15, 0)); // #1A0F00
                        app.Resources["OnPrimaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 224, 178)); // #FFE0B2
                        
                        // Orange Tertiary Colors for Dark Mode
                        app.Resources["Tertiary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 204, 128)); // #FFCC80
                        app.Resources["TertiaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 124, 0)); // #F57C00
                        app.Resources["OnTertiary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 22, 0)); // #2E1600
                        app.Resources["OnTertiaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 224, 178)); // #FFE0B2
                        
                        System.Diagnostics.Debug.WriteLine("✅ DARK MODE colors applied directly");
                    }
                    else
                    {
                        // ===== LIGHT MODE FARBEN =====
                        app.Resources["Surface"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 254, 254)); // #FEFEFE
                        app.Resources["SurfaceVariant"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245)); // #F5F5F5
                        app.Resources["SurfaceContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)); // #F0F0F0
                        app.Resources["SurfaceContainerHigh"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(235, 235, 235)); // #EBEBEB
                        app.Resources["SurfaceContainerHighest"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 229, 229)); // #E5E5E5
                        app.Resources["OnSurface"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 28, 30)); // #1A1C1E
                        app.Resources["OnSurfaceVariant"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(68, 71, 74)); // #44474A
                        
                        // Orange Primary Colors for Light Mode
                        app.Resources["Primary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 124, 0)); // #F57C00
                        app.Resources["PrimaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 224, 178)); // #FFE0B2
                        app.Resources["OnPrimary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)); // #FFFFFF
                        app.Resources["OnPrimaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 81, 0)); // #E65100
                        
                        // Orange Tertiary Colors for Light Mode
                        app.Resources["Tertiary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 152, 0)); // #FF9800
                        app.Resources["TertiaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 243, 224)); // #FFF3E0
                        app.Resources["OnTertiary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)); // #FFFFFF
                        app.Resources["OnTertiaryContainer"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 124, 0)); // #F57C00
                        
                        System.Diagnostics.Debug.WriteLine("✅ LIGHT MODE colors applied directly");
                    }

                    // Zusätzliche wichtige Farben für beide Modi
                    app.Resources["Secondary"] = isDarkMode 
                        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(176, 190, 197)) // #B0BEC5
                        : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(69, 90, 100)); // #455A64
                    
                    app.Resources["Outline"] = isDarkMode
                        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64)) // #404040
                        : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(116, 119, 122)); // #74777A
                    
                    app.Resources["Success"] = isDarkMode
                        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(129, 199, 132)) // #81C784
                        : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // #4CAF50

                    System.Diagnostics.Debug.WriteLine("=== THEME APPLICATION COMPLETED ===");
                    LoggingService.Instance.LogInfo($"🎨 Theme applied directly: {(isDarkMode ? "Dark" : "Light")} mode");

                }, System.Windows.Threading.DispatcherPriority.Send); // Höchste Priorität für sofortige Anwendung

                // Kurze Wartezeit um sicherzustellen dass die Ressourcen angewendet wurden
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error in direct theme application", ex);
                System.Diagnostics.Debug.WriteLine($"ERROR in direct theme application: {ex.Message}");
            }
        }

        private async Task InitializeMasterDataAsync()
        {
            try
            {
                LoggingService.Instance.LogInfo("📊 Initializing master data service...");
                
                // Synchron auf das Laden der Stammdaten warten
                await MasterDataService.Instance.LoadDataAsync();
                
                LoggingService.Instance.LogInfo($"✅ Master data service initialized - Personnel: {MasterDataService.Instance.PersonalList.Count}, Dogs: {MasterDataService.Instance.DogList.Count}");
            }
            catch (System.Exception ex)
            {
                LoggingService.Instance.LogError("❌ Error initializing master data service", ex);
                
                // Auch bei Fehlern versuchen Fallback-Daten zu erstellen
                try
                {
                    // Versuche nochmals zu laden oder erstelle Test-Daten
                    await MasterDataService.Instance.LoadDataAsync();
                    LoggingService.Instance.LogInfo("✅ Master data service initialized on retry");
                }
                catch (System.Exception retryEx)
                {
                    LoggingService.Instance.LogError("❌ Master data service retry failed", retryEx);
                }
            }
        }

        private void CheckForCrashRecovery()
        {
            try
            {
                if (PersistenceService.Instance.HasCrashRecovery())
                {
                    LoggingService.Instance.LogInfo("🔄 Crash recovery data found");
                    // MainWindow will handle recovery UI
                }
            }
            catch (System.Exception ex)
            {
                LoggingService.Instance.LogError("⚠️ Error checking crash recovery", ex);
            }
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                // Wait a few seconds after startup to not interfere with main window loading
                await Task.Delay(5000);

                LoggingService.Instance.LogInfo("🔍 Checking for updates...");
                
                using var updateService = new GitHubUpdateService();
                var updateInfo = await updateService.CheckForUpdatesAsync();

                if (updateInfo != null)
                {
                    LoggingService.Instance.LogInfo($"✨ Update available: v{updateInfo.Version}");
                    
                    // Show update notification on UI thread
                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            var updateWindow = new Views.UpdateNotificationWindow(updateInfo);
                            
                            // Find MainWindow to set as owner
                            var mainWindow = Current.MainWindow;
                            if (mainWindow != null && mainWindow.IsLoaded)
                            {
                                updateWindow.Owner = mainWindow;
                            }
                            
                            updateWindow.ShowDialog();
                        }
                        catch (System.Exception ex)
                        {
                            LoggingService.Instance.LogError("❌ Error showing update dialog", ex);
                        }
                    });
                }
                else
                {
                    LoggingService.Instance.LogInfo("✅ Application is up to date");
                }
            }
            catch (System.Exception ex)
            {
                // Don't show error to user, just log it
                LoggingService.Instance.LogWarning($"⚠️ Update check failed: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                LoggingService.Instance.LogInfo("👋 Application shutting down normally");
                
                // WICHTIG: Stoppe Mobile Server falls aktiv
                try
                {
                    MobileService.Instance.Disconnect();
                    LoggingService.Instance.LogInfo("✅ Mobile Server disconnected");
                }
                catch (System.Exception ex)
                {
                    LoggingService.Instance.LogWarning($"⚠️ Error disconnecting mobile server: {ex.Message}");
                }
                
                // Stop persistence auto-save
                PersistenceService.Instance.StopAutoSave();
                
                // Clear crash recovery since we're exiting normally
                PersistenceService.Instance.ClearCrashRecovery();
                
                // Dispose ThemeService to stop timers
                try
                {
                    ThemeService.Instance.Dispose();
                    LoggingService.Instance.LogInfo("✅ ThemeService disposed");
                }
                catch (System.Exception ex)
                {
                    LoggingService.Instance.LogWarning($"⚠️ Error disposing ThemeService: {ex.Message}");
                }
                
                // Gib Threads Zeit zum Beenden
                System.Threading.Thread.Sleep(500);
                
                LoggingService.Instance.LogInfo("✅ Cleanup completed");
            }
            catch (System.Exception ex)
            {
                LoggingService.Instance.LogError("❌ Error during application shutdown", ex);
            }
            finally
            {
                base.OnExit(e);
            }
        }
    }
}
