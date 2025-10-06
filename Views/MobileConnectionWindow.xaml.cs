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
    /// MobileConnectionWindow - MVVM Implementation v1.9.0
    /// Vollständig auf MVVM umgestellt mit MobileConnectionViewModel
    /// </summary>
    public partial class MobileConnectionWindow : Window
    {
        private MobileConnectionViewModel? _viewModel;

        public MobileConnectionWindow()
        {
            InitializeComponent();
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
                
                LoggingService.Instance.LogInfo("MobileConnectionWindow initialized with MVVM");
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
                // MobileService initialisieren falls nötig
                MobileService.Instance.Connect();
                
                var mobileIntegrationService = MobileService.Instance.GetMobileIntegrationService();
                if (mobileIntegrationService != null)
                {
                    // Setup Delegates für Daten-Zugriff
                    mobileIntegrationService.GetCurrentTeams = () => 
                    {
                        try
                        {
                            return mainViewModel.Teams?.ToList() ?? new List<Team>();
                        }
                        catch (Exception ex)
                        {
                            LoggingService.Instance.LogError("Error getting teams for mobile service", ex);
                            return new List<Team>();
                        }
                    };
                    
                    mobileIntegrationService.GetEinsatzData = () =>
                    {
                        try
                        {
                            // Erstelle EinsatzData aus MainViewModel
                            return new EinsatzData
                            {
                                Einsatzort = ExtractEinsatzortFromViewModel(mainViewModel) ?? "Unbekannt",
                                Einsatzleiter = ExtractEinsatzleiterFromViewModel(mainViewModel) ?? "Unbekannt",
                                EinsatzDatum = DateTime.Now,
                                IstEinsatz = true
                            };
                        }
                        catch (Exception ex)
                        {
                            LoggingService.Instance.LogError("Error getting einsatz data for mobile service", ex);
                            return null;
                        }
                    };
                    
                    mobileIntegrationService.GetGlobalNotes = () =>
                    {
                        try
                        {
                            return mainViewModel.FilteredNotesCollection?.ToList() ?? new List<GlobalNotesEntry>();
                        }
                        catch (Exception ex)
                        {
                            LoggingService.Instance.LogError("Error getting global notes for mobile service", ex);
                            return new List<GlobalNotesEntry>();
                        }
                    };
                    
                    // ViewModel mit MobileService verbinden
                    _viewModel?.SetMobileService(mobileIntegrationService);
                    
                    LoggingService.Instance.LogInfo("MobileConnectionWindow data integration setup completed");
                }
                else
                {
                    LoggingService.Instance.LogWarning("MobileIntegrationService not available for data setup");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error setting up mobile data integration", ex);
                MessageBox.Show($"Warnung: Mobile-Datenintegration konnte nicht vollständig eingerichtet werden.\n{ex.Message}", 
                    "Warnung", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                // Betriebssystem
                diagnose.AppendLine($"💻 Betriebssystem: {Environment.OSVersion}");
                diagnose.AppendLine($"🔧 .NET Version: {Environment.Version}");
                diagnose.AppendLine($"💾 Arbeitsspeicher: {GC.GetTotalMemory(false) / (1024 * 1024):N0} MB");
                diagnose.AppendLine();

                // MainViewModel Verfügbarkeit
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
                }
                diagnose.AppendLine();

                // Mobile Service Status
                var mobileService = MobileService.Instance.GetMobileIntegrationService();
                diagnose.AppendLine($"📱 Mobile Service: {(mobileService != null ? "✅ Verfügbar" : "❌ Nicht verfügbar")}");
                
                if (mobileService != null)
                {
                    diagnose.AppendLine($"🌐 Server Status: {(mobileService.IsRunning ? "✅ Läuft" : "⏸️ Gestoppt")}");
                    diagnose.AppendLine($"🔗 Lokale IP: {mobileService.LocalIPAddress}");
                    diagnose.AppendLine($"📱 QR-Code URL: {mobileService.QRCodeUrl}");
                    
                    // Test der Delegates
                    if (mainViewModel != null)
                    {
                        try
                        {
                            var testTeams = mobileService.GetCurrentTeams?.Invoke();
                            var testEinsatz = mobileService.GetEinsatzData?.Invoke();
                            var testNotes = mobileService.GetGlobalNotes?.Invoke();
                            
                            diagnose.AppendLine($"🔗 Daten-Delegates:");
                            diagnose.AppendLine($"   • GetCurrentTeams: {(testTeams != null ? $"✅ OK ({testTeams.Count} Teams)" : "❌ Fehler")}");
                            diagnose.AppendLine($"   • GetEinsatzData: {(testEinsatz != null ? "✅ OK" : "❌ Fehler")}");
                            diagnose.AppendLine($"   • GetGlobalNotes: {(testNotes != null ? $"✅ OK ({testNotes.Count} Notizen)" : "❌ Fehler")}");
                        }
                        catch (Exception ex)
                        {
                            diagnose.AppendLine($"❌ Fehler bei Delegate-Test: {ex.Message}");
                        }
                    }
                }
                else
                {
                    // Versuche MobileService zu initialisieren für bessere Diagnose
                    try
                    {
                        MobileService.Instance.Connect();
                        mobileService = MobileService.Instance.GetMobileIntegrationService();
                        diagnose.AppendLine($"🔄 Service-Neuinitialisierung: {(mobileService != null ? "✅ Erfolgreich" : "❌ Fehlgeschlagen")}");
                    }
                    catch (Exception ex)
                    {
                        diagnose.AppendLine($"❌ Service-Initialisierung fehlgeschlagen: {ex.Message}");
                    }
                }
                diagnose.AppendLine();

                // Administrator-Rechte
                var isAdmin = IsRunningAsAdministrator();
                diagnose.AppendLine($"🔐 Administrator-Rechte: {(isAdmin ? "✅ Verfügbar" : "❌ Nicht verfügbar")}");
                
                if (!isAdmin)
                {
                    diagnose.AppendLine("💡 Für optimale Netzwerk-Features als Administrator starten");
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

                // Empfehlungen
                diagnose.AppendLine("💡 EMPFEHLUNGEN:");
                
                if (mainViewModel == null)
                {
                    diagnose.AppendLine("   🚨 KRITISCH: MainViewModel nicht verfügbar - App-Neustart empfohlen");
                }
                
                if (mobileService == null)
                {
                    diagnose.AppendLine("   • Mobile Service initialisieren");
                    diagnose.AppendLine("   • 'Server starten' Button verwenden");
                }
                
                if (!isAdmin)
                {
                    diagnose.AppendLine("   • Als Administrator starten für iPhone/Android-Zugriff");
                }
                
                diagnose.AppendLine("   • Firewall-Port 8080 freigeben");
                diagnose.AppendLine("   • Desktop und Mobile im gleichen WLAN");
                diagnose.AppendLine("   • QR-Code mit iPhone-Kamera scannen");
                
                diagnose.AppendLine();
                diagnose.AppendLine("🔧 SCHNELLHILFE:");
                diagnose.AppendLine("   1. 'Server starten' klicken");
                diagnose.AppendLine("   2. QR-Code scannen");
                diagnose.AppendLine("   3. Bei Problemen: Als Administrator starten");
                diagnose.AppendLine("   4. 'API Test' für Funktionsprüfung verwenden");

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
                LoggingService.Instance.LogInfo("MobileConnectionWindow closed and cleaned up");
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
