using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using Einsatzueberwachung.Models;

namespace Einsatzueberwachung.Services
{
    public class MobileIntegrationService : IDisposable
    {
        private HttpListener? _httpListener;
        private readonly int _port = 8080;
        private string _baseUrl;
        private bool _isRunning = false;
        private string _localIPAddress;
        private bool _disposed = false;
        
        // Delegate to get current teams from MainWindow
        public Func<List<Team>>? GetCurrentTeams { get; set; }
        public Func<EinsatzData?>? GetEinsatzData { get; set; }
        public Func<List<GlobalNotesEntry>>? GetGlobalNotes { get; set; }  // NEU: Zugriff auf globale Notizen
        
        public event Action<string>? StatusChanged;
        
        public string QRCodeUrl => $"http://{_localIPAddress}:{_port}/mobile";
        public bool IsRunning => _isRunning;
        public string LocalIPAddress => _localIPAddress;

        public MobileIntegrationService()
        {
            try
            {
                StatusChanged?.Invoke("🔧 Initialisiere Mobile Integration Service...");
                
                _localIPAddress = GetLocalIPAddress();
                _baseUrl = $"http://{_localIPAddress}:{_port}";
                
                StatusChanged?.Invoke($"✅ Service initialisiert - IP: {_localIPAddress}");
                
                // Führe Netzwerk- und Firewall-Checks durch
                CheckFirewallAndNetwork();
                
                StatusChanged?.Invoke("🎯 Mobile Integration Service bereit");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Service-Initialisierung fehlgeschlagen: {ex.Message}");
                LoggingService.Instance.LogError("Failed to initialize MobileIntegrationService", ex);
                
                // Fallback-Werte setzen
                _localIPAddress = "localhost";
                _baseUrl = $"http://{_localIPAddress}:{_port}";
            }
        }
        
        private void CheckFirewallAndNetwork()
        {
            try
            {
                StatusChanged?.Invoke("🔍 Führe initiale Netzwerk-Checks durch...");
                
                // Prüfe Netzwerk-Interfaces
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                    .ToList();
                
                StatusChanged?.Invoke($"📡 {networkInterfaces.Count} aktive Netzwerk-Interfaces erkannt");
                
                // Prüfe HttpListener Support
                if (!HttpListener.IsSupported)
                {
                    StatusChanged?.Invoke("⚠️ WARNUNG: HttpListener wird auf diesem System nicht unterstützt!");
                    throw new NotSupportedException("HttpListener ist auf diesem System nicht verfügbar");
                }
                
                // Prüfe Port-Verfügbarkeit
                var tcpConnections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
                var portInUse = tcpConnections.Any(endpoint => endpoint.Port == _port);
                
                if (portInUse)
                {
                    StatusChanged?.Invoke($"⚠️ Port {_port} wird bereits verwendet - Server-Start könnte fehlschlagen");
                }
                else
                {
                    StatusChanged?.Invoke($"✅ Port {_port} ist verfügbar");
                }
                
                StatusChanged?.Invoke("✅ Initiale System-Checks abgeschlossen");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ Netzwerk-Check Fehler: {ex.Message}");
                LoggingService.Instance.LogError("Network check failed", ex);
            }
        }

        private string GetLocalIPAddress()
        {
            try
            {
                StatusChanged?.Invoke("🔍 Suche nach lokaler IP-Adresse...");
                
                // Hole alle Netzwerk-Interfaces
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up && 
                               ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .ToList();
                
                StatusChanged?.Invoke($"📡 Gefundene Netzwerk-Interfaces: {networkInterfaces.Count}");
                
                foreach (var ni in networkInterfaces)
                {
                    var ipProperties = ni.GetIPProperties();
                    var ipAddresses = ipProperties.UnicastAddresses
                        .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork && 
                                     !IPAddress.IsLoopback(addr.Address))
                        .Select(addr => addr.Address)
                        .ToList();
                    
                    foreach (var ip in ipAddresses)
                    {
                        var ipString = ip.ToString();
                        StatusChanged?.Invoke($"🔍 Prüfe IP: {ipString} ({ni.Name})");
                        
                        // Bevorzuge private Netzwerk-Adressen
                        if (ipString.StartsWith("192.168.") || 
                            ipString.StartsWith("10.") ||
                            (ipString.StartsWith("172.") && 
                             int.TryParse(ipString.Split('.')[1], out int second) && 
                             second >= 16 && second <= 31))
                        {
                            StatusChanged?.Invoke($"✅ Lokale IP gefunden: {ipString}");
                            return ipString;
                        }
                    }
                }
                
                // Fallback: Verwende DNS-Lookup
                StatusChanged?.Invoke("🔄 Fallback: DNS-Lookup...");
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var fallbackIp = host.AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && 
                                         !IPAddress.IsLoopback(ip));
                
                if (fallbackIp != null)
                {
                    StatusChanged?.Invoke($"✅ DNS-IP gefunden: {fallbackIp}");
                    return fallbackIp.ToString();
                }
                
                StatusChanged?.Invoke("⚠️ Keine Netzwerk-IP gefunden, verwende localhost");
                return "localhost";
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ IP-Adress-Fehler: {ex.Message}");
                LoggingService.Instance.LogError("Failed to get local IP address", ex);
                return "localhost";
            }
        }

        public byte[] GenerateQRCode()
        {
            try
            {
                StatusChanged?.Invoke("🎯 Generiere QR-Code...");
                
                var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(QRCodeUrl, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new QRCode(qrCodeData);
                
                using var qrCodeImage = qrCode.GetGraphic(20, Color.Black, Color.White, true);
                using var stream = new MemoryStream();
                qrCodeImage.Save(stream, ImageFormat.Png);
                
                StatusChanged?.Invoke("✅ QR-Code erfolgreich generiert");
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ QR-Code Fehler: {ex.Message}");
                LoggingService.Instance.LogError("QR-Code generation failed", ex);
                return Array.Empty<byte>();
            }
        }

        public async Task<bool> StartServerAsync()
        {
            try
            {
                StatusChanged?.Invoke("🚀 Starte Mobile Server...");
                StatusChanged?.Invoke($"🔍 System-Check: Windows {Environment.OSVersion.VersionString}");
                StatusChanged?.Invoke($"💻 .NET Version: {Environment.Version}");
                
                bool isAdmin = IsRunningAsAdministrator();
                StatusChanged?.Invoke($"🔐 Administrator-Rechte: {(isAdmin ? "✅ Verfügbar" : "❌ Nicht verfügbar")}");
                
                if (!isAdmin)
                {
                    StatusChanged?.Invoke("💡 FÜR NETZWERK-ZUGRIFF: Starten Sie die App als Administrator");
                    StatusChanged?.Invoke("🔄 Verwende Localhost-Modus für lokalen Test...");
                }

                // Prüfe HttpListener Support
                if (!HttpListener.IsSupported)
                {
                    StatusChanged?.Invoke("❌ KRITISCH: HttpListener wird auf diesem System nicht unterstützt!");
                    LoggingService.Instance.LogError("HttpListener is not supported on this system");
                    return false;
                }

                // Bereinige vorherige Listener
                await SafeStopExistingListener();
                
                // Neue HttpListener-Instanz
                _httpListener = new HttpListener();
                
                // Netzwerk-Konfiguration
                if (isAdmin)
                {
                    StatusChanged?.Invoke("🔧 Führe automatische Admin-Konfiguration durch...");
                    await ConfigureNetworkAccess();
                }
                
                // Prefixes konfigurieren - mit verbesserter Logik
                bool networkAccessConfigured = await ConfigurePrefixesWithValidation();
                StatusChanged?.Invoke($"📋 Konfigurierte Prefixes: {_httpListener.Prefixes.Count}");
                
                if (_httpListener.Prefixes.Count == 0)
                {
                    StatusChanged?.Invoke("❌ Keine gültigen Prefixes konfiguriert - Server-Start nicht möglich");
                    LoggingService.Instance.LogError("No valid prefixes configured for HttpListener");
                    return false;
                }

                // Teste jeden Prefix vor dem Start
                await ValidateConfiguredPrefixes();
                
                // HttpListener starten mit verbessertem Error-Handling
                StatusChanged?.Invoke("🚀 Starte HttpListener...");
                
                try
                {
                    _httpListener.Start();
                    _isRunning = true;
                    StatusChanged?.Invoke("✅ HttpListener erfolgreich gestartet");
                }
                catch (HttpListenerException httpEx)
                {
                    string detailedError = GetDetailedHttpListenerError(httpEx);
                    StatusChanged?.Invoke($"❌ HttpListener-Start fehlgeschlagen: {detailedError}");
                    LoggingService.Instance.LogError($"HttpListener start failed: {detailedError}", httpEx);
                    
                    // Versuche Fallback-Strategien
                    if (await TryFallbackStrategies())
                    {
                        StatusChanged?.Invoke("✅ Fallback-Strategie erfolgreich");
                        _isRunning = true;
                    }
                    else
                    {
                        return false;
                    }
                }
                
                // Erfolgreiche Konfiguration melden
                if (networkAccessConfigured && _localIPAddress != "localhost")
                {
                    StatusChanged?.Invoke($"🌐 Mobile Server gestartet mit Netzwerk-Zugriff!");
                    StatusChanged?.Invoke($"📱 iPhone URL: http://{_localIPAddress}:{_port}/mobile");
                    StatusChanged?.Invoke($"💻 Desktop URL: http://localhost:{_port}/mobile");
                    StatusChanged?.Invoke($"🔧 Debug URL: http://localhost:{_port}/debug");
                }
                else
                {
                    StatusChanged?.Invoke($"⚠️ Mobile Server gestartet (NUR LOCALHOST)");
                    StatusChanged?.Invoke($"💻 Desktop URL: http://localhost:{_port}/mobile");
                    StatusChanged?.Invoke($"🔧 Debug URL: http://localhost:{_port}/debug");
                    StatusChanged?.Invoke("💡 FÜR IPHONE-ZUGRIFF: Als Administrator starten");
                }
                
                // Request Handler starten
                StatusChanged?.Invoke("🔄 Starte Request-Handler...");
                _ = Task.Run(HandleRequestsAsync);
                
                // Finale Validierung
                await Task.Delay(500); // Kurz warten
                if (_httpListener?.IsListening == true)
                {
                    StatusChanged?.Invoke("🎉 Mobile Server vollständig einsatzbereit!");
                    LoggingService.Instance.LogInfo("Mobile server started successfully");
                    return true;
                }
                else
                {
                    StatusChanged?.Invoke("❌ Server-Validierung fehlgeschlagen");
                    return false;
                }
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Kritischer Server-Start-Fehler: {ex.Message}");
                LoggingService.Instance.LogError("Critical server start error", ex);
                return false;
            }
        }

        /// <summary>
        /// Stoppt sicher einen existierenden Listener
        /// </summary>
        private async Task SafeStopExistingListener()
        {
            if (_httpListener != null)
            {
                try
                {
                    StatusChanged?.Invoke("🔄 Bereinige vorherigen Listener...");
                    
                    if (_httpListener.IsListening)
                    {
                        _httpListener.Stop();
                    }
                    _httpListener.Close();
                    
                    // Kurz warten für cleanup
                    await Task.Delay(200);
                    
                    StatusChanged?.Invoke("✅ Vorheriger Listener bereinigt");
                }
                catch (Exception ex)
                {
                    StatusChanged?.Invoke($"⚠️ Listener-Cleanup Warning: {ex.Message}");
                    LoggingService.Instance.LogWarning($"Listener cleanup warning: {ex.Message}");
                }
                finally
                {
                    _httpListener = null;
                }
            }
        }

        /// <summary>
        /// Konfiguriert Prefixes mit Validierung
        /// </summary>
        private async Task<bool> ConfigurePrefixesWithValidation()
        {
            bool networkAccessConfigured = false;
            var configuredPrefixes = new List<string>();
            
            StatusChanged?.Invoke("🔧 Konfiguriere HTTP-Prefixes...");
            
            // Strategie 1: Localhost (immer möglich)
            var localhostPrefixes = new[] 
            {
                $"http://localhost:{_port}/",
                $"http://127.0.0.1:{_port}/"
            };
            
            foreach (var prefix in localhostPrefixes)
            {
                if (await TryAddPrefix(prefix))
                {
                    configuredPrefixes.Add(prefix);
                    StatusChanged?.Invoke($"✅ Localhost-Prefix hinzugefügt: {prefix}");
                }
            }
            
            // Strategie 2: Spezifische IP-Adresse (wenn verfügbar und Admin-Rechte)
            if (_localIPAddress != "localhost" && !string.IsNullOrEmpty(_localIPAddress) && IsRunningAsAdministrator())
            {
                var ipPrefix = $"http://{_localIPAddress}:{_port}/";
                if (await TryAddPrefix(ipPrefix))
                {
                    configuredPrefixes.Add(ipPrefix);
                    networkAccessConfigured = true;
                    StatusChanged?.Invoke($"✅ IP-Prefix hinzugefügt: {ipPrefix}");
                }
            }
            
            // Strategie 3: Wildcard (nur mit Admin-Rechten)
            if (IsRunningAsAdministrator())
            {
                var wildcardPrefix = $"http://+:{_port}/";
                if (await TryAddPrefix(wildcardPrefix))
                {
                    configuredPrefixes.Add(wildcardPrefix);
                    networkAccessConfigured = true;
                    StatusChanged?.Invoke($"✅ Wildcard-Prefix hinzugefügt: {wildcardPrefix}");
                }
            }
            
            // Ergebnis-Zusammenfassung
            StatusChanged?.Invoke($"📋 Prefix-Konfiguration abgeschlossen:");
            StatusChanged?.Invoke($"   • Konfigurierte Prefixes: {_httpListener!.Prefixes.Count}");
            StatusChanged?.Invoke($"   • Netzwerk-Zugriff: {(networkAccessConfigured ? "✅ Aktiviert" : "❌ Nur Localhost")}");
            
            return networkAccessConfigured;
        }

        /// <summary>
        /// Versucht einen Prefix hinzuzufügen mit Fehlerbehandlung
        /// </summary>
        private async Task<bool> TryAddPrefix(string prefix)
        {
            try
            {
                _httpListener!.Prefixes.Add(prefix);
                return true;
            }
            catch (HttpListenerException httpEx)
            {
                StatusChanged?.Invoke($"⚠️ Prefix-Fehler {prefix}: {GetDetailedHttpListenerError(httpEx)}");
                return false;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ Prefix-Fehler {prefix}: {ex.Message}");
                LoggingService.Instance.LogError($"Error adding prefix {prefix}", ex);
                return false;
            }
        }

        /// <summary>
        /// Validiert konfigurierte Prefixes
        /// </summary>
        private async Task ValidateConfiguredPrefixes()
        {
            StatusChanged?.Invoke("🔍 Validiere konfigurierte Prefixes...");
            
            foreach (var prefix in _httpListener!.Prefixes)
            {
                StatusChanged?.Invoke($"   • {prefix} - bereit");
            }
            
            StatusChanged?.Invoke("✅ Prefix-Validierung abgeschlossen");
        }

        /// <summary>
        /// Liefert detaillierte HttpListener-Fehlermeldungen
        /// </summary>
        private string GetDetailedHttpListenerError(HttpListenerException httpEx)
        {
            return httpEx.ErrorCode switch
            {
                5 => "Zugriff verweigert - Administrator-Rechte erforderlich oder Port bereits belegt",
                183 => "Port bereits in Verwendung - andere Anwendung beenden oder alternativen Port verwenden",
                87 => "Ungültiger Parameter - Netzwerk-Konfiguration prüfen oder URL-Format korrigieren",
                995 => "Operation abgebrochen - Server wird gestoppt",
                10048 => "Adresse bereits in Verwendung - Port-Konflikt mit anderer Anwendung",
                10013 => "Berechtigung verweigert - Firewall oder Sicherheitsrichtlinie blockiert Zugriff",
                _ => $"HTTP-Listener Fehler (Code {httpEx.ErrorCode}): {httpEx.Message}"
            };
        }

        /// <summary>
        /// Versucht Fallback-Strategien wenn der normale Start fehlschlägt
        /// </summary>
        private async Task<bool> TryFallbackStrategies()
        {
            StatusChanged?.Invoke("🔄 Versuche Fallback-Strategien...");
            
            // Strategie 1: Alternative Ports
            var alternativePorts = new[] { 8081, 8082, 8083, 9080, 9081 };
            
            foreach (var altPort in alternativePorts)
            {
                try
                {
                    StatusChanged?.Invoke($"🔄 Teste alternativen Port {altPort}...");
                    
                    // Neuen Listener für alternativen Port erstellen
                    var testListener = new HttpListener();
                    testListener.Prefixes.Add($"http://localhost:{altPort}/");
                    
                    testListener.Start();
                    
                    // Wenn erfolgreich, übernehmen
                    _httpListener?.Close();
                    _httpListener = testListener;
                    
                    StatusChanged?.Invoke($"✅ Alternative Port {altPort} erfolgreich konfiguriert");
                    LoggingService.Instance.LogInfo($"Successfully configured alternative port {altPort}");
                    return true;
                }
                catch (Exception ex)
                {
                    StatusChanged?.Invoke($"⚠️ Alternative Port {altPort} fehlgeschlagen: {ex.Message}");
                }
            }
            
            StatusChanged?.Invoke("❌ Alle Fallback-Strategien fehlgeschlagen");
            return false;
        }
        
        public async Task StopServer()
        {
            try
            {
                StatusChanged?.Invoke("🛑 Stoppe Mobile Server...");
                
                _isRunning = false;
                
                if (_httpListener != null)
                {
                    try
                    {
                        if (_httpListener.IsListening)
                        {
                            _httpListener.Stop();
                        }
                        _httpListener.Close();
                        _httpListener = null;
                    }
                    catch (Exception ex)
                    {
                        StatusChanged?.Invoke($"⚠️ HttpListener-Stop Warnung: {ex.Message}");
                        LoggingService.Instance.LogWarning($"HttpListener stop warning: {ex.Message}");
                    }
                }
                
                // Gib dem Request-Handler Zeit zum Beenden
                await Task.Delay(500);
                
                StatusChanged?.Invoke("✅ Mobile Server gestoppt");
                LoggingService.Instance.LogInfo("Mobile server stopped successfully");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ Server-Stop Fehler: {ex.Message}");
                LoggingService.Instance.LogError("Error stopping server", ex);
            }
        }
        
        private bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ Administrator-Check Fehler: {ex.Message}");
                LoggingService.Instance.LogError("Administrator check failed", ex);
                return false;
            }
        }
        
        private async Task ConfigureNetworkAccess()
        {
            try
            {
                StatusChanged?.Invoke("🔧 Konfiguriere Netzwerk-Zugriff (Admin-Modus)...");
                
                await ConfigureUrlReservation();
                await ConfigureFirewall();
                
                StatusChanged?.Invoke("✅ Admin-Netzwerk-Konfiguration abgeschlossen");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ Netzwerk-Konfiguration Fehler: {ex.Message}");
                LoggingService.Instance.LogError("Network access configuration failed", ex);
            }
        }
        
        private async Task ConfigureUrlReservation()
        {
            try
            {
                StatusChanged?.Invoke("🔧 Konfiguriere URL-Reservierung...");
                
                var checkCmd = $"netsh http show urlacl url=http://+:{_port}/";
                var checkResult = await RunCommand(checkCmd);
                
                if (checkResult.Contains($"http://+:{_port}/"))
                {
                    StatusChanged?.Invoke("✅ URL-Reservierung bereits vorhanden");
                    return;
                }
                
                // Versuche verschiedene Benutzer-Konfigurationen
                var userConfigs = new[]
                {
                    "Everyone",
                    Environment.UserName,
                    $"{Environment.UserDomainName}\\{Environment.UserName}",
                    "Users"
                };

                foreach (var user in userConfigs)
                {
                    try
                    {
                        var addCmd = $"netsh http add urlacl url=http://+:{_port}/ user={user}";
                        var addResult = await RunCommand(addCmd);
                        
                        if (addResult.Contains("erfolgreich") || addResult.Contains("successfully") || addResult.Contains("OK"))
                        {
                            StatusChanged?.Invoke($"✅ URL-Reservierung erfolgreich für Benutzer: {user}");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusChanged?.Invoke($"⚠️ URL-Reservierung für {user} fehlgeschlagen: {ex.Message}");
                    }
                }
                
                StatusChanged?.Invoke("⚠️ URL-Reservierung konnte nicht erstellt werden");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ URL-Reservierung Fehler: {ex.Message}");
                LoggingService.Instance.LogError("URL reservation failed", ex);
            }
        }
        
        private async Task ConfigureFirewall()
        {
            try
            {
                StatusChanged?.Invoke("🛡️ Konfiguriere Windows Firewall...");
                
                // Prüfe ob Regel bereits existiert
                var checkCmd = "netsh advfirewall firewall show rule name=\"Einsatzüberwachung Mobile\"";
                var checkResult = await RunCommand(checkCmd);
                
                if (checkResult.Contains("Einsatzüberwachung Mobile"))
                {
                    StatusChanged?.Invoke("✅ Firewall-Regel bereits vorhanden");
                    return;
                }
                
                // Erstelle Firewall-Regel
                var addCmd = $"netsh advfirewall firewall add rule name=\"Einsatzüberwachung Mobile\" dir=in action=allow protocol=TCP localport={_port}";
                var addResult = await RunCommand(addCmd);
                
                if (addResult.Contains("Ok") || addResult.Contains("erfolgreich") || addResult.Contains("OK"))
                {
                    StatusChanged?.Invoke("✅ Firewall-Regel erfolgreich hinzugefügt");
                }
                else
                {
                    StatusChanged?.Invoke($"⚠️ Firewall-Regel Ergebnis: {addResult}");
                    
                    // Fallback: Versuche PowerShell-Befehl
                    StatusChanged?.Invoke("🔄 Versuche PowerShell-Fallback...");
                    var psCmd = $"powershell -Command \"New-NetFirewallRule -DisplayName 'Einsatzüberwachung Mobile' -Direction Inbound -Protocol TCP -LocalPort {_port} -Action Allow\"";
                    var psResult = await RunCommand(psCmd);
                    
                    if (!string.IsNullOrEmpty(psResult) && !psResult.Contains("Fehler") && !psResult.Contains("Error"))
                    {
                        StatusChanged?.Invoke("✅ Firewall-Regel über PowerShell erfolgreich");
                    }
                    else
                    {
                        StatusChanged?.Invoke("⚠️ Firewall-Regel konnte nicht erstellt werden");
                        StatusChanged?.Invoke("💡 Öffnen Sie manuell die Windows Firewall und geben Port 8080 frei");
                    }
                }
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ Firewall-Konfiguration Fehler: {ex.Message}");
                LoggingService.Instance.LogError("Firewall configuration failed", ex);
            }
        }
        
        private async Task<string> RunCommand(string command)
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Verb = "runas" // Run with elevated privileges if available
                };
                
                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    // Set timeout to prevent hanging
                    var timeoutTask = Task.Delay(10000); // 10 seconds timeout
                    var processTask = process.WaitForExitAsync();
                    
                    var completedTask = await Task.WhenAny(processTask, timeoutTask);
                    
                    if (completedTask == timeoutTask)
                    {
                        StatusChanged?.Invoke("⚠️ Befehl-Timeout - Prozess wird beendet");
                        try
                        {
                            process.Kill();
                        }
                        catch { }
                        return "Timeout";
                    }
                    
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    
                    var result = !string.IsNullOrEmpty(output) ? output : error;
                    var logResult = result?.Length > 200 ? result.Substring(0, 200) : result ?? "";
                    LoggingService.Instance.LogInfo($"Command result: {command} -> {logResult}");
                    return result ?? string.Empty;
                }
                
                return "Prozess konnte nicht gestartet werden";
            }
            catch (Exception ex)
            {
                var errorMsg = $"Command execution failed: {ex.Message}";
                StatusChanged?.Invoke($"⚠️ Befehl-Fehler: {ex.Message}");
                LoggingService.Instance.LogError(errorMsg, ex);
                return errorMsg;
            }
        }
        
        private async Task<bool> ConfigurePrefixes()
        {
            bool networkAccessConfigured = false;
            var configuredPrefixes = new List<string>();
            
            StatusChanged?.Invoke("🔧 Konfiguriere HTTP-Prefixes...");
            
            // Strategie 1: Localhost (immer möglich)
            try
            {
                var localhostPrefixes = new[] 
                {
                    $"http://localhost:{_port}/",
                    $"http://127.0.0.1:{_port}/"
                };
                
                foreach (var prefix in localhostPrefixes)
                {
                    try
                    {
                        _httpListener!.Prefixes.Add(prefix);
                        configuredPrefixes.Add(prefix);
                        StatusChanged?.Invoke($"✅ Localhost-Prefix hinzugefügt: {prefix}");
                    }
                    catch (Exception ex)
                    {
                        StatusChanged?.Invoke($"⚠️ Localhost-Prefix Fehler {prefix}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Kritischer Localhost-Prefix Fehler: {ex.Message}");
                LoggingService.Instance.LogError("Critical localhost prefix error", ex);
            }
            
            // Strategie 2: Spezifische IP-Adresse (wenn verfügbar)
            if (_localIPAddress != "localhost" && !string.IsNullOrEmpty(_localIPAddress))
            {
                try
                {
                    var ipPrefix = $"http://{_localIPAddress}:{_port}/";
                    _httpListener!.Prefixes.Add(ipPrefix);
                    configuredPrefixes.Add(ipPrefix);
                    networkAccessConfigured = true;
                    StatusChanged?.Invoke($"✅ IP-Prefix hinzugefügt: {ipPrefix}");
                }
                catch (HttpListenerException httpEx) when (httpEx.ErrorCode == 5)
                {
                    StatusChanged?.Invoke($"⚠️ IP-Prefix benötigt Administrator-Rechte: {_localIPAddress}:{_port}");
                    StatusChanged?.Invoke("💡 Starten Sie als Administrator für Netzwerk-Zugriff");
                }
                catch (Exception ex)
                {
                    StatusChanged?.Invoke($"⚠️ IP-Prefix Fehler: {ex.Message}");
                    LoggingService.Instance.LogError($"IP prefix error for {_localIPAddress}", ex);
                }
            }
            
            // Strategie 3: Wildcard (nur mit Admin-Rechten)
            if (IsRunningAsAdministrator())
            {
                try
                {
                    var wildcardPrefix = $"http://+:{_port}/";
                    _httpListener!.Prefixes.Add(wildcardPrefix);
                    configuredPrefixes.Add(wildcardPrefix);
                    networkAccessConfigured = true;
                    StatusChanged?.Invoke($"✅ Wildcard-Prefix hinzugefügt: {wildcardPrefix}");
                }
                catch (HttpListenerException httpEx) when (httpEx.ErrorCode == 183)
                {
                    StatusChanged?.Invoke($"⚠️ Wildcard-Prefix: URL bereits reserviert");
                }
                catch (Exception ex)
                {
                    StatusChanged?.Invoke($"⚠️ Wildcard-Prefix Fehler: {ex.Message}");
                    LoggingService.Instance.LogError("Wildcard prefix error", ex);
                }
            }
            else
            {
                StatusChanged?.Invoke("💡 Wildcard-Prefix übersprungen - Administrator-Rechte erforderlich");
            }
            
            // Strategie 4: Alternative Ports (Fallback)
            if (_httpListener!.Prefixes.Count == 0)
            {
                StatusChanged?.Invoke("🔄 Versuche alternative Ports...");
                
                var alternativePorts = new[] { 8081, 8082, 8083, 9080, 9081 };
                
                foreach (var altPort in alternativePorts)
                {
                    try
                    {
                        var altPrefix = $"http://localhost:{altPort}/";
                        _httpListener.Prefixes.Add(altPrefix);
                        configuredPrefixes.Add(altPrefix);
                        
                        // Update URLs mit neuem Port
                        var newBaseUrl = $"http://{_localIPAddress}:{altPort}";
                        
                        StatusChanged?.Invoke($"✅ Alternative Port konfiguriert: {altPort}");
                        StatusChanged?.Invoke($"🔧 URLs aktualisiert für Port {altPort}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        StatusChanged?.Invoke($"⚠️ Alternative Port {altPort} fehlgeschlagen: {ex.Message}");
                    }
                }
            }
            
            // Ergebnis-Zusammenfassung
            StatusChanged?.Invoke($"📋 Prefix-Konfiguration abgeschlossen:");
            StatusChanged?.Invoke($"   • Konfigurierte Prefixes: {_httpListener.Prefixes.Count}");
            StatusChanged?.Invoke($"   • Netzwerk-Zugriff: {(networkAccessConfigured ? "✅ Aktiviert" : "❌ Nur Localhost")}")
            ;
            if (configuredPrefixes.Count > 0)
            {
                StatusChanged?.Invoke("📋 Aktive URLs:");
                foreach (var prefix in configuredPrefixes)
                {
                    StatusChanged?.Invoke($"   • {prefix}mobile");
                }
            }
            
            return networkAccessConfigured;
        }
        
        /// <summary>
        /// Behandelt POST-Requests zum Erstellen von Replies
        /// </summary>
        private async Task HandleCreateReply(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                // Extract note ID from URL path
                var pathParts = request.Url?.AbsolutePath.Split('/');
                if (pathParts == null || pathParts.Length < 4)
                {
                    response.StatusCode = 400;
                    await ServeJSON(response, new { error = "Invalid URL format" });
                    return;
                }

                var noteId = pathParts[3]; // /api/notes/{id}/reply

                // Read request body
                var body = "";
                using (var reader = new System.IO.StreamReader(request.InputStream))
                {
                    body = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrEmpty(body))
                {
                    response.StatusCode = 400;
                    await ServeJSON(response, new { error = "Request body is required" });
                    return;
                }

                // Parse JSON
                var replyData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(body);
                if (replyData == null || !replyData.ContainsKey("content"))
                {
                    response.StatusCode = 400;
                    await ServeJSON(response, new { error = "Content field is required" });
                    return;
                }

                var content = replyData["content"].ToString();
                if (string.IsNullOrEmpty(content))
                {
                    response.StatusCode = 400;
                    await ServeJSON(response, new { error = "Content cannot be empty" });
                    return;
                }
                
                var teamName = replyData.ContainsKey("teamName") ? replyData["teamName"]?.ToString() : null;
                var entryTypeStr = replyData.ContainsKey("entryType") ? replyData["entryType"]?.ToString() : "Manual";
                
                if (!Enum.TryParse<GlobalNotesEntryType>(entryTypeStr, out var entryType))
                {
                    entryType = GlobalNotesEntryType.Manual;
                }

                // Create reply via GlobalNotesService
                var reply = GlobalNotesService.Instance.CreateReply(noteId, content, teamName, entryType);
                
                if (reply != null)
                {
                    response.StatusCode = 201;
                    await ServeJSON(response, new
                    {
                        id = reply.Id,
                        content = reply.Content,
                        timestamp = reply.FormattedTimestamp,
                        teamName = reply.TeamName,
                        entryType = reply.EntryType.ToString(),
                        replyToEntryId = reply.ReplyToEntryId,
                        threadId = reply.ThreadId,
                        threadDepth = reply.ThreadDepth
                    });
                }
                else
                {
                    response.StatusCode = 404;
                    await ServeJSON(response, new { error = "Original note not found" });
                }
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Create reply error: {ex.Message}");
                response.StatusCode = 500;
                await ServeJSON(response, new { error = ex.Message });
            }
        }

        private async Task HandleRequestsAsync()
        {
            StatusChanged?.Invoke("🔄 Request-Handler gestartet");
            
            while (_isRunning && _httpListener != null && _httpListener.IsListening)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();
                    _ = Task.Run(() => ProcessRequest(context));
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 995)
                {
                    StatusChanged?.Invoke("🛑 Request-Handler wird beendet...");
                    break;
                }
                catch (ObjectDisposedException)
                {
                    StatusChanged?.Invoke("🛑 Request-Handler beendet (Listener disposed)");
                    break;
                }
                catch (Exception ex)
                {
                    StatusChanged?.Invoke($"⚠️ Request-Handler Fehler: {ex.Message}");
                    
                    if (!_isRunning)
                    {
                        break;
                    }
                    
                    await Task.Delay(100);
                }
            }
            
            StatusChanged?.Invoke("✅ Request-Handler gestoppt");
        }
        
        private async Task ProcessRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;
                
                // CORS Headers
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS, PUT");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                
                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }
                
                var path = request.Url?.AbsolutePath ?? "/";
                StatusChanged?.Invoke($"📨 Request: {request.HttpMethod} {path} von {request.RemoteEndPoint}");
                
                switch (path.ToLower())
                {
                    case "/":
                    case "/mobile":
                    case "/mobile/":
                        await ServeHTML(response, GenerateMobileHTML());
                        break;
                    
                    case "/api/teams":
                        await ServeJSON(response, GetTeamsData());
                        break;
                    
                    case "/api/status":
                        await ServeJSON(response, GetStatusData());
                        break;
                    
                    case "/test":
                        await ServeText(response, "OK");
                        break;
                    
                    case "/debug":
                        await ServeHTML(response, GenerateDebugHTML());
                        break;
                    
                    // Endpoint für globale Notizen - unterstützt beide URLs
                    case "/api/globalnotes":
                    case "/api/notes":
                        await ServeJSON(response, GetGlobalNotesData());
                        break;
                    
                    // Neue Reply-System Endpoints
                    case var p when p.StartsWith("/api/notes/") && p.EndsWith("/reply") && request.HttpMethod == "POST":
                        await HandleCreateReply(request, response);
                        break;
                    
                    case var p when p.StartsWith("/api/threads/"):
                        await HandleThreadRequest(request, response, path);
                        break;
                    
                    case "/api/reply-stats":
                        await ServeJSON(response, GetReplyStatsData());
                        break;
                    
                    default:
                        response.StatusCode = 404;
                        await ServeText(response, "Not Found");
                        break;
                }
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Request-Verarbeitung Fehler: {ex.Message}");
                
                try
                {
                    context.Response.StatusCode = 500;
                    await ServeText(context.Response, $"Internal Server Error: {ex.Message}");
                }
                catch
                {
                    // Fehler beim Senden der Fehlerantwort - ignorieren
                }
            }
        }

        /// <summary>
        /// Behandelt Thread-spezifische Requests
        /// </summary>
        private async Task HandleThreadRequest(HttpListenerRequest request, HttpListenerResponse response, string path)
        {
            try
            {
                var pathParts = path.Split('/');
                if (pathParts.Length < 4)
                {
                    response.StatusCode = 400;
                    await ServeJSON(response, new { error = "Invalid thread URL format" });
                    return;
                }

                var threadId = pathParts[3]; // /api/threads/{id}

                if (pathParts.Length == 4)
                {
                    // GET /api/threads/{id} - Get all messages in thread
                    var threadMessages = GlobalNotesService.Instance.GetThreadMessages(threadId);
                    await ServeJSON(response, threadMessages.Select(m => new
                    {
                        id = m.Id,
                        content = m.Content,
                        timestamp = m.FormattedTimestamp,
                        teamName = m.TeamName,
                        entryType = m.EntryType.ToString(),
                        entryTypeIcon = m.EntryTypeIcon,
                        isReply = m.IsReply,
                        replyToEntryId = m.ReplyToEntryId,
                        threadDepth = m.ThreadDepth,
                        repliesCount = m.RepliesCount
                    }).ToList());
                }
                else if (pathParts.Length == 5 && pathParts[4] == "export")
                {
                    // GET /api/threads/{id}/export - Export thread as text
                    var threadText = GlobalNotesService.Instance.ExportThreadAsText(threadId);
                    response.ContentType = "text/plain; charset=utf-8";
                    await ServeText(response, threadText);
                }
                else
                {
                    response.StatusCode = 404;
                    await ServeText(response, "Thread endpoint not found");
                }
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Thread request error: {ex.Message}");
                response.StatusCode = 500;
                await ServeJSON(response, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Liefert Reply-System Statistiken
        /// </summary>
        private object GetReplyStatsData()
        {
            try
            {
                var stats = GlobalNotesService.Instance.GetReplyStats();
                return new
                {
                    totalNotes = stats.TotalNotes,
                    threadRoots = stats.ThreadRoots,
                    replies = stats.Replies,
                    threadsWithReplies = stats.ThreadsWithReplies,
                    averageRepliesPerThread = Math.Round(stats.AverageRepliesPerThread, 2)
                };
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Reply stats error: {ex.Message}");
                return new { error = ex.Message };
            }
        }
        
        private async Task ServeHTML(HttpListenerResponse response, string html)
        {
            response.ContentType = "text/html; charset=utf-8";
            var buffer = Encoding.UTF8.GetBytes(html);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }
        
        private async Task ServeJSON(HttpListenerResponse response, object data)
        {
            response.ContentType = "application/json; charset=utf-8";
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }
        
        private async Task ServeText(HttpListenerResponse response, string text)
        {
            response.ContentType = "text/plain; charset=utf-8";
            var buffer = Encoding.UTF8.GetBytes(text);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }
        
        private object GetTeamsData()
        {
            var teams = GetCurrentTeams?.Invoke() ?? new List<Team>();
            
            return teams.Select(t => new
            {
                id = t.TeamId,
                name = t.TeamName,
                dogName = t.HundName,
                handler = t.Hundefuehrer,
                helper = t.Helfer,
                type = t.TeamTypeDisplayName,
                types = t.MultipleTeamTypes?.SelectedTypes.Select(type => TeamTypeInfo.GetTypeInfo(type).DisplayName).ToList() ?? new List<string>(),
                status = t.IsRunning ? "active" : "ready",
                time = t.ElapsedTimeString,
                isFirstWarning = t.IsFirstWarning,
                isSecondWarning = t.IsSecondWarning,
                firstWarningMinutes = t.FirstWarningMinutes,
                secondWarningMinutes = t.SecondWarningMinutes,
                suchgebiet = t.Suchgebiet ?? ""
            }).ToList();
        }
        
        private object GetStatusData()
        {
            var teams = GetCurrentTeams?.Invoke() ?? new List<Team>();
            var einsatzData = GetEinsatzData?.Invoke();
            
            // Berechne Einsatzdauer basierend auf allen aktiven Timern
            var maxRunningTime = teams.Where(t => t.IsRunning)
                                     .Select(t => t.ElapsedTime)
                                     .OrderByDescending(t => t)
                                     .FirstOrDefault();
            
            var durationString = maxRunningTime != TimeSpan.Zero 
                ? $"{(int)maxRunningTime.TotalHours:00}:{maxRunningTime.Minutes:00}:{maxRunningTime.Seconds:00}"
                : "00:00:00";
            
            return new
            {
                mission = new
                {
                    location = einsatzData?.Einsatzort ?? "Unbekannt",
                    leader = einsatzData?.Einsatzleiter ?? "Unbekannt",
                    duration = durationString
                },
                teams = new
                {
                    total = teams.Count,
                    active = teams.Count(t => t.IsRunning)
                },
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
        
        private object GetGlobalNotesData()
        {
            var notes = GetGlobalNotes?.Invoke() ?? new List<GlobalNotesEntry>();
            
            return notes.Select(n => new
            {
                id = n.Id,
                timestamp = n.FormattedTimestamp,
                content = n.Content,
                teamName = n.TeamName ?? "",
                icon = n.EntryTypeIcon,
                type = n.EntryType.ToString(),
                
                // Reply-System Daten
                isReply = n.IsReply,
                replyToEntryId = n.ReplyToEntryId,
                replyPreview = n.ReplyPreview,
                threadId = n.ThreadId,
                threadDepth = n.ThreadDepth,
                repliesCount = n.RepliesCount,
                hasReplies = n.HasReplies,
                threadMarginLeft = n.ThreadMarginLeft,
                replyIcon = n.ReplyIcon
            }).ToList();
        }
        
        private string GenerateDebugHTML()
        {
            var teams = GetCurrentTeams?.Invoke() ?? new List<Team>();
            var einsatzData = GetEinsatzData?.Invoke();
            
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Debug - Einsatzüberwachung Mobile</title>
    <style>
        body {{ font-family: monospace; padding: 20px; background: #1e1e1e; color: #d4d4d4; }}
        h1 {{ color: #4ec9b0; }}
        .info {{ background: #2d2d2d; padding: 15px; border-radius: 5px; margin: 10px 0; }}
    </style>
</head>
<body>
    <h1>🔧 Einsatzüberwachung Mobile - Debug</h1>
    <div class='info'>
        <p><strong>Server-Status:</strong> 🟢 Online</p>
        <p><strong>Lokale IP:</strong> {_localIPAddress}</p>
        <p><strong>Port:</strong> {_port}</p>
        <p><strong>Teams:</strong> {teams.Count}</p>
        <p><strong>Aktiv:</strong> {teams.Count(t => t.IsRunning)}</p>
    </div>
    <p><a href='/mobile'>Zur Mobile-Ansicht</a></p>
</body>
</html>";
        }
        
        private string GenerateMobileHTML()
        {
            return @"<!DOCTYPE html>
<html lang='de'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>🐕 Einsatzüberwachung Mobile</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        
        body { 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
            background: linear-gradient(135deg, #FFB74D 0%, #FF9800 100%);
            min-height: 100vh;
            color: #2c3e50;
            padding: 20px;
        }
        
        .container { 
            max-width: 600px; 
            margin: 0 auto; 
        }
        
        .header { 
            text-align: center; 
            margin-bottom: 30px;
            background: rgba(255,255,255,0.95);
            padding: 25px;
            border-radius: 15px;
            box-shadow: 0 8px 32px rgba(0,0,0,0.1);
            backdrop-filter: blur(10px);
        }
        
        .header h1 {
            font-size: 28px;
            font-weight: bold;
            color: #2c3e50;
            margin-bottom: 8px;
        }
        
        .header p {
            color: #7f8c8d;
            font-size: 16px;
        }
        
        .status-card {
            background: rgba(255,255,255,0.95);
            border-radius: 15px;
            padding: 20px;
            margin-bottom: 25px;
            box-shadow: 0 4px 20px rgba(0,0,0,0.1);
            backdrop-filter: blur(10px);
        }
        
        .status-title {
            font-size: 18px;
            font-weight: bold;
            color: #2c3e50;
            margin-bottom: 15px;
            display: flex;
            align-items: center;
            gap: 8px;
        }
        
        .status-content {
            font-size: 16px;
            color: #34495e;
            line-height: 1.6;
        }
        
        .section-title {
            color: #ffffff;
            font-size: 20px;
            font-weight: bold;
            margin: 30px 0 15px 0;
            text-shadow: 0 2px 4px rgba(0,0,0,0.3);
            display: flex;
            align-items: center;
            gap: 10px;
        }
        
        .team-card {
            background: rgba(255,255,255,0.95);
            padding: 25px;
            border-radius: 15px;
            margin-bottom: 20px;
            box-shadow: 0 4px 20px rgba(0,0,0,0.1);
            backdrop-filter: blur(10px);
            border-left: 5px solid #FF9800;
        }
        
        .team-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 20px;
            padding-bottom: 15px;
            border-bottom: 2px solid #ecf0f1;
        }
        
        .team-name {
            font-size: 20px;
            font-weight: bold;
            color: #2c3e50;
        }
        
        .team-status {
            padding: 6px 14px;
            border-radius: 20px;
            font-size: 13px;
            font-weight: bold;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }
        
        .status-active {
            background: #27ae60;
            color: white;
        }
        
        .status-ready {
            background: #95a5a6;
            color: white;
        }
        
        .team-time {
            font-size: 42px;
            font-weight: bold;
            font-family: 'SF Mono', 'Monaco', 'Cascadia Code', 'Courier New', monospace;
            color: #2c3e50;
            text-align: center;
            margin: 20px 0;
            letter-spacing: 2px;
        }
        
        .team-time.warning {
            color: #f39c12;
            animation: pulse 2s ease-in-out infinite;
        }
        
        .team-time.critical {
            color: #e74c3c;
            animation: pulse 1s ease-in-out infinite;
        }
        
        @keyframes pulse {
            0%, 100% { opacity: 1; }
            50% { opacity: 0.7; }
        }
        
        .team-info {
            display: grid;
            gap: 12px;
        }
        
        .info-row {
            display: flex;
            align-items: center;
            gap: 12px;
            padding: 12px;
            background: #f8f9fa;
            border-radius: 10px;
            border-left: 3px solid #FF9800;
        }
        
        .info-icon {
            font-size: 18px;
            width: 24px;
            text-align: center;
        }
        
        .info-label {
            font-size: 13px;
            color: #7f8c8d;
            text-transform: uppercase;
            font-weight: bold;
            letter-spacing: 0.5px;
            min-width: 80px;
        }
        
        .info-value {
            font-size: 16px;
            font-weight: 500;
            color: #2c3e50;
            flex: 1;
        }
        
        .notes-section {
            background: rgba(255,255,255,0.95);
            border-radius: 15px;
            padding: 25px;
            margin-top: 25px;
            box-shadow: 0 4px 20px rgba(0,0,0,0.1);
            backdrop-filter: blur(10px);
            max-height: 400px;
            overflow-y: auto;
        }
        
        .notes-header {
            font-size: 18px;
            font-weight: bold;
            margin-bottom: 20px;
            color: #2c3e50;
            display: flex;
            align-items: center;
            gap: 10px;
            border-bottom: 2px solid #ecf0f1;
            padding-bottom: 15px;
        }
        
        .note-entry {
            background: #f8f9fa;
            border-left: 4px solid #FF9800;
            padding: 15px;
            margin-bottom: 15px;
            border-radius: 8px;
        }
        
        .note-timestamp {
            font-weight: bold;
            font-size: 14px;
            color: #7f8c8d;
            margin-bottom: 8px;
        }
        
        .note-content {
            font-size: 15px;
            line-height: 1.5;
            color: #2c3e50;
        }
        
        .note-team {
            font-size: 12px;
            color: #95a5a6;
            margin-top: 8px;
            font-style: italic;
        }
        
        .loading, .no-teams, .no-notes {
            text-align: center;
            padding: 40px 20px;
            color: #7f8c8d;
            font-size: 16px;
            background: rgba(255,255,255,0.9);
            border-radius: 15px;
            margin: 20px 0;
        }
        
        .error {
            background: #e74c3c;
            color: white;
            padding: 20px;
            border-radius: 15px;
            margin: 20px 0;
            text-align: center;
            font-weight: bold;
        }
        
        .refresh-indicator {
            position: fixed;
            bottom: 20px;
            right: 20px;
            background: rgba(255,255,255,0.95);
            padding: 12px 18px;
            border-radius: 25px;
            box-shadow: 0 4px 20px rgba(0,0,0,0.2);
            font-size: 13px;
            color: #7f8c8d;
            display: flex;
            align-items: center;
            gap: 8px;
            z-index: 1000;
        }
        
        .refresh-spinner {
            width: 14px;
            height: 14px;
            border: 2px solid #ecf0f1;
            border-top-color: #FF9800;
            border-radius: 50%;
            animation: spin 1s linear infinite;
        }
        
        @keyframes spin {
            to { transform: rotate(360deg); }
        }
        
        @media (max-width: 480px) {
            body { padding: 15px; }
            .header h1 { font-size: 24px; }
            .team-time { font-size: 36px; }
            .team-card { padding: 20px; }
        }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🐕‍🦺 Einsatzüberwachung</h1>
            <p>Mobile Übersicht v1.9 - Professional Edition</p>
        </div>
        
        <div class='status-card'>
            <div class='status-title'>📊 Status</div>
            <div class='status-content' id='status'>Lade...</div>
        </div>
        
        <div class='section-title'>🎯 Teams</div>
        <div id='teamsContainer'>
            <div class='loading'>Lade Team-Daten...</div>
        </div>
        
        <div class='section-title'>📝 Notizen & Ereignisse</div>
        <div class='notes-section'>
            <div class='notes-header'>Ereignis-Timeline</div>
            <div id='notesContainer'>
                <div class='loading'>Lade Notizen...</div>
            </div>
        </div>
    </div>

    <div class='refresh-indicator' id='refreshIndicator'>
        <div class='refresh-spinner'></div>
        <span>Aktualisiere...</span>
    </div>

    <script>
        let updateInterval;
        let isUpdating = false;

        async function loadData() {
            if (isUpdating) return;
            isUpdating = true;
            showRefreshIndicator();

            try {
                // Status laden
                const statusResponse = await fetch('/api/status');
                const status = await statusResponse.json();
                updateStats(status);

                // Teams laden
                const teamsResponse = await fetch('/api/teams');
                const teams = await teamsResponse.json();
                displayTeams(teams);

                // Notizen laden
                const notesResponse = await fetch('/api/notes');
                const notes = await notesResponse.json();
                displayNotes(notes);

                hideRefreshIndicator();
            } catch (error) {
                console.error('Fehler beim Laden der Daten:', error);
                showError('Verbindungsfehler. Überprüfe die Netzwerkverbindung.');
                hideRefreshIndicator();
            }

            isUpdating = false;
        }

        function updateStats(status) {
            const statusElement = document.getElementById('status');
            statusElement.innerHTML = `
                <strong>Aktive Teams:</strong> ${status.teams?.active || 0} von ${status.teams?.total || 0}<br>
                <strong>Einsatzdauer:</strong> ${status.mission?.duration || '00:00:00'}<br>
                <strong>Einsatzort:</strong> ${status.mission?.location || 'Unbekannt'}
            `;
        }

        function displayTeams(teams) {
            const container = document.getElementById('teamsContainer');
            
            if (!teams || teams.length === 0) {
                container.innerHTML = `
                    <div class='no-teams'>
                        <strong>Keine Teams verfügbar</strong><br>
                        Erstelle Teams in der Desktop-Anwendung
                    </div>`;
                return;
            }

            container.innerHTML = teams.map(team => {
                const statusClass = team.status === 'active' ? 'status-active' : 'status-ready';
                const statusText = team.status === 'active' ? 'Aktiv' : 'Bereit';
                
                let timeClass = '';
                if (team.isSecondWarning) {
                    timeClass = 'critical';
                } else if (team.isFirstWarning) {
                    timeClass = 'warning';
                }

                return `
                    <div class='team-card'>
                        <div class='team-header'>
                            <div class='team-name'>${escapeHtml(team.name || 'Unbenannt')}</div>
                            <div class='team-status ${statusClass}'>${statusText}</div>
                        </div>

                        <div class='team-time ${timeClass}'>${escapeHtml(team.time || '00:00:00')}</div>

                        <div class='team-info'>
                            <div class='info-row'>
                                <div class='info-icon'>🐕</div>
                                <div class='info-label'>Hund</div>
                                <div class='info-value'>${escapeHtml(team.dogName || '-')}</div>
                            </div>

                            <div class='info-row'>
                                <div class='info-icon'>👤</div>
                                <div class='info-label'>Hundeführer</div>
                                <div class='info-value'>${escapeHtml(team.handler || '-')}</div>
                            </div>

                            ${team.helper ? `
                            <div class='info-row'>
                                <div class='info-icon'>👥</div>
                                <div class='info-label'>Helfer</div>
                                <div class='info-value'>${escapeHtml(team.helper)}</div>
                            </div>
                            ` : ''}

                            ${team.suchgebiet ? `
                            <div class='info-row'>
                                <div class='info-icon'>📍</div>
                                <div class='info-label'>Suchgebiet</div>
                                <div class='info-value'>${escapeHtml(team.suchgebiet)}</div>
                            </div>
                            ` : ''}
                        </div>
                    </div>
                `;
            }).join('');
        }

        function displayNotes(notes) {
            const container = document.getElementById('notesContainer');
            
            if (!notes || !Array.isArray(notes) || notes.length === 0) {
                container.innerHTML = `
                    <div class='no-notes'>
                        Noch keine Notizen vorhanden
                    </div>`;
                return;
            }

            // Zeige die letzten 20 Notizen (neueste zuerst)
            const recentNotes = notes.slice(-20).reverse();
            container.innerHTML = recentNotes.map(note => `
                <div class='note-entry'>
                    <div class='note-timestamp'>${note.icon || '📝'} ${note.timestamp || note.formattedTimestamp || 'N/A'}</div>
                    <div class='note-content'>${escapeHtml(note.content || 'Kein Inhalt')}</div>
                    ${note.teamName ? `<div class='note-team'>Team: ${escapeHtml(note.teamName)}</div>` : ''}
                </div>
            `).join('');
        }

        function showError(message) {
            const container = document.getElementById('teamsContainer');
            container.innerHTML = `<div class='error'>${escapeHtml(message)}</div>`;
        }

        function showRefreshIndicator() {
            document.getElementById('refreshIndicator').style.display = 'flex';
        }

        function hideRefreshIndicator() {
            setTimeout(() => {
                document.getElementById('refreshIndicator').style.display = 'none';
            }, 500);
        }

        function escapeHtml(text) {
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        }

        // Initial load
        loadData();

        // Auto-refresh every 10 seconds
        updateInterval = setInterval(loadData, 10000);

        // Reload when coming back to the page
        document.addEventListener('visibilitychange', () => {
            if (!document.hidden) {
                loadData();
            }
        });

        console.log('Einsatzüberwachung Mobile v1.9 loaded - Professional Edition');
    </script>
</body>
</html>";
        }

        public async Task CleanupNetworkConfiguration()
        {
            try
            {
                if (!IsRunningAsAdministrator())
                {
                    StatusChanged?.Invoke("⚠️ Cleanup erfordert Admin-Rechte - überspringe");
                    return;
                }
                
                StatusChanged?.Invoke("🧹 Räume Netzwerk-Konfiguration auf...");
                
                // Remove URL reservations
                var ports = new[] { 8080, 8081, 8082, 8083 };
                foreach (var port in ports)
                {
                    try
                    {
                        var urlCmd = $"netsh http delete urlacl url=http://+:{port}/";
                        var result = await RunCommand(urlCmd);
                        if (!result.Contains("Fehler") && !result.Contains("Error"))
                        {
                            StatusChanged?.Invoke($"✅ URL-Reservierung für Port {port} entfernt");
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusChanged?.Invoke($"⚠️ URL-Cleanup für Port {port} fehlgeschlagen: {ex.Message}");
                    }
                }
                
                // Remove firewall rules
                try
                {
                    var firewallCmd = "netsh advfirewall firewall delete rule name=\"Einsatzüberwachung Mobile\"";
                    var result = await RunCommand(firewallCmd);
                    StatusChanged?.Invoke("✅ Firewall-Regeln entfernt");
                }
                catch (Exception ex)
                {
                    StatusChanged?.Invoke($"⚠️ Firewall-Cleanup fehlgeschlagen: {ex.Message}");
                }
                
                StatusChanged?.Invoke("✅ Netzwerk-Cleanup abgeschlossen");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ Cleanup Fehler: {ex.Message}");
                LoggingService.Instance.LogError("Network cleanup failed", ex);
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        _isRunning = false;
                        
                        if (_httpListener != null)
                        {
                            try
                            {
                                if (_httpListener.IsListening)
                                {
                                    _httpListener.Stop();
                                }
                                _httpListener.Close();
                            }
                            catch { /* Ignoriere Fehler beim Dispose */ }
                            finally
                            {
                                _httpListener = null;
                            }
                        }
                    }
                    catch { /* Ignoriere alle Fehler beim Dispose */ }
                }
                
                _disposed = true;
            }
        }

        ~MobileIntegrationService()
        {
            Dispose(false);
        }
    }
}
