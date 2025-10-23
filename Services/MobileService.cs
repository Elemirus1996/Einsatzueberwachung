using System;
using System.Threading.Tasks;

namespace Einsatzueberwachung.Services
{
    public class MobileService
    {
        private static MobileService? _instance;
        private static readonly object _lock = new object();
        private MobileIntegrationService? _mobileIntegrationService;
        private bool _isConnected = false;
        private bool _isInitializing = false;

        public static MobileService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new MobileService();
                        }
                    }
                }
                return _instance;
            }
        }

        private MobileService()
        {
            LoggingService.Instance.LogInfo("MobileService singleton created");
        }

        public void Connect()
        {
            lock (_lock)
            {
                if (_isInitializing)
                {
                    LoggingService.Instance.LogWarning("MobileService connection already in progress");
                    return;
                }

                if (_isConnected && _mobileIntegrationService != null)
                {
                    LoggingService.Instance.LogInfo("MobileService already connected - verifying service health");
                    
                    // Zusätzliche Verifikation
                    if (IsServiceHealthy())
                    {
                        LoggingService.Instance.LogInfo("MobileService connection verified as healthy");
                        return;
                    }
                    else
                    {
                        LoggingService.Instance.LogWarning("MobileService was connected but not healthy - reinitializing");
                        ForceDisconnect(); // Cleanup before new attempt
                    }
                }

                try
                {
                    _isInitializing = true;
                    LoggingService.Instance.LogInfo("Initializing MobileIntegrationService...");
                    
                    _mobileIntegrationService = new MobileIntegrationService();
                    _isConnected = true;
                    
                    // Verifikation der erfolgreichen Erstellung
                    if (_mobileIntegrationService != null)
                    {
                        LoggingService.Instance.LogInfo($"MobileIntegrationService created successfully - LocalIP: {_mobileIntegrationService.LocalIPAddress}");
                        
                        // Zusätzlicher Health-Check
                        if (IsServiceHealthy())
                        {
                            LoggingService.Instance.LogInfo("MobileService connected and verified healthy");
                        }
                        else
                        {
                            LoggingService.Instance.LogWarning("MobileService connected but health check failed");
                        }
                    }
                    else
                    {
                        LoggingService.Instance.LogError("MobileIntegrationService creation returned null");
                        _isConnected = false;
                        throw new InvalidOperationException("MobileIntegrationService creation failed - returned null");
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("Failed to connect MobileService", ex);
                    
                    // Cleanup bei Fehler
                    _mobileIntegrationService?.Dispose();
                    _mobileIntegrationService = null;
                    _isConnected = false;
                    
                    throw; // Re-throw to allow caller to handle
                }
                finally
                {
                    _isInitializing = false;
                }
            }
        }

        public void Disconnect()
        {
            lock (_lock)
            {
                if (!_isConnected)
                {
                    LoggingService.Instance.LogInfo("MobileService already disconnected");
                    return;
                }

                ForceDisconnect();
            }
        }

        /// <summary>
        /// Forciert eine Trennung ohne Status-Checks
        /// </summary>
        private void ForceDisconnect()
        {
            try
            {
                LoggingService.Instance.LogInfo("Disconnecting MobileService...");
                
                if (_mobileIntegrationService != null)
                {
                    // Stoppe Server falls aktiv
                    if (_mobileIntegrationService.IsRunning)
                    {
                        var stopTask = _mobileIntegrationService.StopServer();
                        // Warte maximal 3 Sekunden
                        if (!stopTask.Wait(TimeSpan.FromSeconds(3)))
                        {
                            LoggingService.Instance.LogWarning("MobileServer stop timeout during disconnect");
                        }
                    }
                    
                    // Dispose Service
                    if (_mobileIntegrationService is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    
                    _mobileIntegrationService = null;
                }
                
                _isConnected = false;
                LoggingService.Instance.LogInfo("MobileService disconnected successfully");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error disconnecting MobileService", ex);
                // Force cleanup even if error occurred
                _mobileIntegrationService = null;
                _isConnected = false;
            }
        }

        public MobileIntegrationService? GetMobileIntegrationService()
        {
            lock (_lock)
            {
                if (!_isConnected)
                {
                    LoggingService.Instance.LogWarning("MobileIntegrationService requested but not connected");
                    return null;
                }

                if (_mobileIntegrationService == null)
                {
                    LoggingService.Instance.LogError("MobileIntegrationService is null despite being marked as connected");
                    _isConnected = false;
                    return null;
                }
                
                // Zusätzlicher Health-Check beim Abrufen
                if (!IsServiceHealthy())
                {
                    LoggingService.Instance.LogWarning("MobileIntegrationService health check failed during get");
                }
                
                return _mobileIntegrationService;
            }
        }

        /// <summary>
        /// Versucht die Verbindung wiederherzustellen falls sie unterbrochen wurde
        /// </summary>
        public bool TryReconnect()
        {
            try
            {
                LoggingService.Instance.LogInfo("Attempting MobileService reconnection...");
                
                // Vollständige Trennung und Neuverbindung
                Disconnect();
                
                // Kurze Wartezeit für Cleanup
                System.Threading.Thread.Sleep(500);
                
                Connect();
                
                var success = IsHealthy();
                LoggingService.Instance.LogInfo($"MobileService reconnection result: {success}");
                
                return success;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Failed to reconnect MobileService", ex);
                return false;
            }
        }

        /// <summary>
        /// Überprüft ob der Service funktionsfähig ist
        /// </summary>
        public bool IsHealthy()
        {
            lock (_lock)
            {
                var basicHealth = _isConnected && 
                                 _mobileIntegrationService != null && 
                                 !_isInitializing;

                if (!basicHealth)
                {
                    return false;
                }

                // Erweiterte Gesundheitsprüfung
                return IsServiceHealthy();
            }
        }

        /// <summary>
        /// Detaillierte Gesundheitsprüfung des Services
        /// </summary>
        private bool IsServiceHealthy()
        {
            try
            {
                if (_mobileIntegrationService == null)
                {
                    return false;
                }

                // Prüfe ob LocalIPAddress verfügbar ist (Indikator für korrekte Initialisierung)
                var localIP = _mobileIntegrationService.LocalIPAddress;
                if (string.IsNullOrEmpty(localIP))
                {
                    LoggingService.Instance.LogWarning("MobileIntegrationService has no LocalIPAddress");
                    return false;
                }

                // Prüfe QR-Code URL
                var qrUrl = _mobileIntegrationService.QRCodeUrl;
                if (string.IsNullOrEmpty(qrUrl))
                {
                    LoggingService.Instance.LogWarning("MobileIntegrationService has no QRCodeUrl");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during service health check", ex);
                return false;
            }
        }

        /// <summary>
        /// Gibt Diagnose-Informationen zurück
        /// </summary>
        public string GetDiagnosticInfo()
        {
            lock (_lock)
            {
                var info = new System.Text.StringBuilder();
                info.AppendLine($"Connected: {_isConnected}");
                info.AppendLine($"Initializing: {_isInitializing}");
                info.AppendLine($"Service Available: {_mobileIntegrationService != null}");
                
                if (_mobileIntegrationService != null)
                {
                    try
                    {
                        info.AppendLine($"Server Running: {_mobileIntegrationService.IsRunning}");
                        info.AppendLine($"Local IP: {_mobileIntegrationService.LocalIPAddress}");
                        info.AppendLine($"QR Code URL: {_mobileIntegrationService.QRCodeUrl}");
                        info.AppendLine($"Service Health Check: {IsServiceHealthy()}");
                    }
                    catch (Exception ex)
                    {
                        info.AppendLine($"Error getting service details: {ex.Message}");
                    }
                }
                
                // Zusätzliche System-Info
                try
                {
                    info.AppendLine($"HttpListener Supported: {System.Net.HttpListener.IsSupported}");
                    info.AppendLine($"Administrator Rights: {IsRunningAsAdministrator()}");
                }
                catch (Exception ex)
                {
                    info.AppendLine($"Error getting system info: {ex.Message}");
                }
                
                return info.ToString();
            }
        }

        /// <summary>
        /// Prüft Administrator-Rechte
        /// </summary>
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

        /// <summary>
        /// Führt eine vollständige Diagnose und Reparatur durch
        /// </summary>
        public bool DiagnoseAndRepair()
        {
            lock (_lock)
            {
                try
                {
                    LoggingService.Instance.LogInfo("Starting MobileService diagnosis and repair...");
                    
                    // 1. Basis-Status prüfen
                    LoggingService.Instance.LogInfo($"Current status - Connected: {_isConnected}, Instance: {_mobileIntegrationService != null}");
                    
                    // 2. Bei Problemen: Vollständige Neuinitialisierung
                    if (!IsHealthy())
                    {
                        LoggingService.Instance.LogInfo("Service not healthy - performing complete reset");
                        
                        ForceDisconnect();
                        System.Threading.Thread.Sleep(1000);
                        Connect();
                    }
                    
                    // 3. Finale Verifikation
                    var finalHealth = IsHealthy();
                    LoggingService.Instance.LogInfo($"Diagnosis and repair completed - Final health: {finalHealth}");
                    
                    return finalHealth;
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("Error during diagnosis and repair", ex);
                    return false;
                }
            }
        }
    }
}
