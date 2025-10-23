using System;
using System.Threading.Tasks;
using System.Windows;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// Enhanced with ThemeService Integration
    /// </summary>
    public partial class App : Application
    {
        private static bool _startWindowCreated = false;
        
        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                LoggingService.Instance.LogInfo("🎨 ===============================================");
                LoggingService.Instance.LogInfo("🎨 EINSATZÜBERWACHUNG STARTUP");
                LoggingService.Instance.LogInfo("🎨 Modern Design System with Auto Time Switching");
                LoggingService.Instance.LogInfo("🎨 ===============================================");
                
                // Basis-Startup erst ausführen
                base.OnStartup(e);
                
                // ===== THEME SYSTEM INITIALIZATION =====
                LoggingService.Instance.LogInfo("🎨 Initializing Unified Theme System...");
                
                try 
                {
                    // Warte kurz damit Application.Current verfügbar ist
                    await Task.Delay(100);
                    
                    // UnifiedThemeManager initialisieren (statt ThemeService)
                    var themeManager = UnifiedThemeManager.Instance;
                    
                    LoggingService.Instance.LogInfo($"✅ Unified Theme System initialized successfully");
                    LoggingService.Instance.LogInfo($"🎨 Current status: {themeManager.CurrentThemeStatus}");
                }
                catch (Exception themeEx)
                {
                    LoggingService.Instance.LogError("🚨 Error initializing unified theme system", themeEx);
                    // Keine MessageBox hier - das könnte den Fehler verursachen
                }
                
                // Log startup
                LoggingService.Instance.LogInfo($"🚀 Einsatzüberwachung starting up...");
                LoggingService.Instance.LogInfo($"📍 Startup Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                
                // ===== MASTER DATA INITIALIZATION =====
                await InitializeMasterDataAsync();
                
                // ===== CHECK FOR RECOVERY FIRST =====
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
                            // Create MainWindow with recovery data
                            var mainWindow = Einsatzueberwachung.MainWindow.CreateForRecovery();
                            mainWindow.Show();
                            PersistenceService.Instance.ClearCrashRecovery();
                            LoggingService.Instance.LogInfo("✅ Session restored from crash recovery");
                            return;
                        }
                    }
                    else
                    {
                        PersistenceService.Instance.ClearCrashRecovery();
                    }
                }
                
                // ===== START WINDOW LAUNCH (ONLY ONCE) =====
                if (!_startWindowCreated && !HasStartWindowOpen())
                {
                    _startWindowCreated = true;
                    var startWindow = new Views.StartWindow();
                    startWindow.Closed += (s, args) => _startWindowCreated = false; // Reset when closed
                    startWindow.Show();
                    
                    LoggingService.Instance.LogInfo($"✅ StartWindow opened with unified theme system");
                    LoggingService.Instance.LogInfo($"🎨 Active theme: {UnifiedThemeManager.Instance.CurrentThemeStatus}");
                }
                else
                {
                    LoggingService.Instance.LogWarning("⚠️ StartWindow creation skipped - already created or open");
                }
            }
            catch (System.Exception ex)
            {
                LoggingService.Instance.LogError("❌ Error during application startup", ex);
                
                // Still try to start the app with fallback StartWindow
                try
                {
                    // Only create StartWindow if one doesn't already exist and we haven't created one yet
                    if (Application.Current.Windows.Count == 0 && !_startWindowCreated && !HasStartWindowOpen())
                    {
                        _startWindowCreated = true;
                        var startWindow = new Views.StartWindow();
                        startWindow.Closed += (s, args) => _startWindowCreated = false; // Reset when closed
                        startWindow.Show();
                        LoggingService.Instance.LogInfo("✅ Fallback startup completed");
                    }
                    else
                    {
                        LoggingService.Instance.LogInfo("✅ Startup window already exists, skipping fallback");
                    }
                }
                catch (System.Exception fallbackEx)
                {
                    LoggingService.Instance.LogError("❌ Even fallback startup failed", fallbackEx);
                    MessageBox.Show($"Schwerwiegender Fehler beim Starten der Anwendung:\n{ex.Message}\n\nFallback-Fehler:\n{fallbackEx.Message}", 
                        "Startup-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown(1);
                    return;
                }
            }
        }

        /// <summary>
        /// Checks if a StartWindow is already open
        /// </summary>
        private bool HasStartWindowOpen()
        {
            try
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is Views.StartWindow)
                    {
                        LoggingService.Instance.LogInfo("Found existing StartWindow");
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error checking for existing StartWindow", ex);
                return false;
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

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                LoggingService.Instance.LogInfo("👋 Application shutting down normally");
                
                // ===== THEME SYSTEM CLEANUP =====
                try
                {
                    UnifiedThemeManager.Instance.Dispose();
                    LoggingService.Instance.LogInfo("✅ Unified Theme System disposed");
                }
                catch (System.Exception ex)
                {
                    LoggingService.Instance.LogWarning($"⚠️ Error disposing unified theme system: {ex.Message}");
                }
                
                // ===== MOBILE SERVICE CLEANUP =====
                try
                {
                    MobileService.Instance.Disconnect();
                    LoggingService.Instance.LogInfo("✅ Mobile Server disconnected");
                }
                catch (System.Exception ex)
                {
                    LoggingService.Instance.LogWarning($"⚠️ Error disconnecting mobile server: {ex.Message}");
                }
                
                // ===== PERSISTENCE SERVICE CLEANUP =====
                try
                {
                    PersistenceService.Instance.StopAutoSave();
                    PersistenceService.Instance.ClearCrashRecovery();
                }
                catch (System.Exception ex)
                {
                    LoggingService.Instance.LogWarning($"⚠️ Error with persistence service: {ex.Message}");
                }
                
                // Gib Threads Zeit zum Beenden
                System.Threading.Thread.Sleep(500);
                
                LoggingService.Instance.LogInfo("✅ Cleanup completed successfully");
                LoggingService.Instance.LogInfo("🎨 ===============================================");
                LoggingService.Instance.LogInfo("🎨 EINSATZÜBERWACHUNG SHUTDOWN COMPLETE");
                LoggingService.Instance.LogInfo("🎨 ===============================================");
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
