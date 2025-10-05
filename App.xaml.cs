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
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // WICHTIG: ShutdownMode NICHT hier setzen!
                // Standard ist OnLastWindowClose, was wir für StartWindow -> MainWindow Transition brauchen
                
                // Initialize theme system early
                var themeService = ThemeService.Instance;
                
                // Initialize master data service
                _ = InitializeMasterDataAsync();
                
                // Log startup
                LoggingService.Instance.LogInfo("🚀 Einsatzüberwachung Professional v1.9 starting up...");
                LoggingService.Instance.LogInfo($"📍 Startup Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                LoggingService.Instance.LogInfo($"💻 OS: {Environment.OSVersion}");
                LoggingService.Instance.LogInfo($"🔧 .NET: {Environment.Version}");
                
                // Check for crash recovery
                CheckForCrashRecovery();
                
                // Check for updates (async, don't block startup)
                _ = CheckForUpdatesAsync();
                
                base.OnStartup(e);
            }
            catch (System.Exception ex)
            {
                LoggingService.Instance.LogError("❌ Error during application startup", ex);
                
                // Still try to start the app
                base.OnStartup(e);
            }
        }

        private async Task InitializeMasterDataAsync()
        {
            try
            {
                LoggingService.Instance.LogInfo("📊 Initializing master data service...");
                await MasterDataService.Instance.LoadDataAsync();
                LoggingService.Instance.LogInfo("✅ Master data service initialized");
            }
            catch (System.Exception ex)
            {
                LoggingService.Instance.LogError("❌ Error initializing master data service", ex);
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
