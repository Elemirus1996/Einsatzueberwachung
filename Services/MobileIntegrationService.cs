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
    public class MobileIntegrationService
    {
        private HttpListener? _httpListener;
        private readonly int _port = 8080;
        private string _baseUrl;
        private bool _isRunning = false;
        private string _localIPAddress;
        
        // Delegate to get current teams from MainWindow
        public Func<List<Team>>? GetCurrentTeams { get; set; }
        public Func<EinsatzData?>? GetEinsatzData { get; set; }
        
        public event Action<string>? StatusChanged;
        
        public string QRCodeUrl => $"http://{_localIPAddress}:{_port}/mobile";
        public bool IsRunning => _isRunning;
        public string LocalIPAddress => _localIPAddress;

        public MobileIntegrationService()
        {
            _localIPAddress = GetLocalIPAddress();
            _baseUrl = $"http://{_localIPAddress}:{_port}";
            
            // Führe Netzwerk- und Firewall-Checks durch
            CheckFirewallAndNetwork();
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
                return "localhost";
            }
        }

        public byte[] GenerateQRCode()
        {
            try
            {
                var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(QRCodeUrl, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new QRCode(qrCodeData);
                
                using var qrCodeImage = qrCode.GetGraphic(20, Color.Black, Color.White, true);
                using var stream = new MemoryStream();
                qrCodeImage.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"QR-Code Fehler: {ex.Message}");
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
                
                // Phase 1: Kritische Checks
                if (!await PerformCriticalChecks())
                {
                    return false;
                }
                
                bool isAdmin = IsRunningAsAdministrator();
                StatusChanged?.Invoke($"🔐 Administrator-Rechte: {(isAdmin ? "✅ Verfügbar" : "❌ Nicht verfügbar")}");
                
                // Phase 2: Netzwerk-Vorbereitung
                if (isAdmin)
                {
                    StatusChanged?.Invoke("🔧 Führe automatische Admin-Konfiguration durch...");
                    await ConfigureNetworkAccess();
                }
                else
                {
                    StatusChanged?.Invoke("🔄 Verwende Nicht-Admin-Strategien für Netzwerk-Zugriff...");
                    await ConfigureNetworkAccessWithoutAdmin();
                }
                
                // Phase 3: HttpListener Setup
                return await SetupHttpListener();
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Schwerwiegender Server-Start-Fehler: {ex.GetType().Name}");
                StatusChanged?.Invoke($"❌ Fehlermeldung: {ex.Message}");
                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    var stackTrace = ex.StackTrace.Length > 200 ? ex.StackTrace.Substring(0, 200) + "..." : ex.StackTrace;
                    StatusChanged?.Invoke($"❌ Stack Trace: {stackTrace}");
                }
                
                // Versuche Fallback-Methoden
                StatusChanged?.Invoke("🔄 Versuche Notfall-Server-Methoden...");
                return await TryEmergencyServerStart();
            }
        }
        
        private async Task<bool> PerformCriticalChecks()
        {
            try
            {
                StatusChanged?.Invoke("🔍 Führe kritische System-Checks durch...");
                
                // 1. HttpListener Support
                if (!HttpListener.IsSupported)
                {
                    StatusChanged?.Invoke("❌ KRITISCH: HttpListener wird nicht unterstützt");
                    StatusChanged?.Invoke("💡 Lösung: .NET Runtime neu installieren oder System-Update");
                    return false;
                }
                StatusChanged?.Invoke("✅ HttpListener wird unterstützt");
                
                // 2. Netzwerk-Interfaces
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                    .ToList();
                    
                if (networkInterfaces.Count == 0)
                {
                    StatusChanged?.Invoke("❌ WARNUNG: Keine aktiven Netzwerk-Interfaces gefunden");
                    StatusChanged?.Invoke("💡 Netzwerk-Verbindung prüfen");
                }
                else
                {
                    StatusChanged?.Invoke($"✅ {networkInterfaces.Count} aktive Netzwerk-Interfaces gefunden");
                }
                
                // 3. Port-Verfügbarkeit prüfen
                await CheckAndResolvePortConflicts();
                
                StatusChanged?.Invoke("✅ Kritische Checks abgeschlossen");
                return true;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ Fehler bei kritischen Checks: {ex.Message}");
                return true; // Weitermachen trotz Fehlern
            }
        }
        
        private async Task CheckAndResolvePortConflicts()
        {
            try
            {
                var tcpConnections = System.Net.NetworkInformation.IPGlobalProperties
                    .GetIPGlobalProperties()
                    .GetActiveTcpListeners();
                
                var portInUse = tcpConnections.Any(endpoint => endpoint.Port == _port);
                
                if (portInUse)
                {
                    StatusChanged?.Invoke($"⚠️ Port {_port} wird bereits verwendet");
                    
                    // Versuche herauszufinden welcher Prozess den Port verwendet
                    try
                    {
                        var processInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "netstat",
                            Arguments = $"-ano | findstr :{_port}",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        };
                        
                        using var process = System.Diagnostics.Process.Start(processInfo);
                        if (process != null)
                        {
                            await process.WaitForExitAsync();
                            var output = await process.StandardOutput.ReadToEndAsync();
                            
                            if (!string.IsNullOrEmpty(output))
                            {
                                StatusChanged?.Invoke($"🔍 Port {_port} Details: {output.Trim()}");
                                
                                // Versuche den Prozess zu identifizieren
                                var lines = output.Split('\n');
                                foreach (var line in lines)
                                {
                                    if (line.Contains($":{_port}"))
                                    {
                                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                        if (parts.Length > 0 && int.TryParse(parts.Last(), out int pid))
                                        {
                                            try
                                            {
                                                var proc = System.Diagnostics.Process.GetProcessById(pid);
                                                StatusChanged?.Invoke($"📋 Port {_port} verwendet von: {proc.ProcessName} (PID: {pid})");
                                                
                                                // Wenn es unser eigener Prozess ist, versuche ihn zu beenden
                                                if (proc.ProcessName.Contains("Einsatzueberwachung") || 
                                                    proc.ProcessName.Contains("Einsatzüberwachung"))
                                                {
                                                    StatusChanged?.Invoke("🔄 Beende vorherige Instanz...");
                                                    proc.Kill();
                                                    await Task.Delay(2000); // Warte 2 Sekunden
                                                    StatusChanged?.Invoke("✅ Vorherige Instanz beendet");
                                                }
                                            }
                                            catch
                                            {
                                                StatusChanged?.Invoke($"⚠️ Prozess PID {pid} konnte nicht identifiziert werden");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusChanged?.Invoke($"⚠️ Port-Analyse fehlgeschlagen: {ex.Message}");
                    }
                    
                    StatusChanged?.Invoke("💡 Alternative Ports werden automatisch getestet");
                }
                else
                {
                    StatusChanged?.Invoke($"✅ Port {_port} ist verfügbar");
                }
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ Port-Check Fehler: {ex.Message}");
            }
        }
        
        private async Task<bool> SetupHttpListener()
        {
            const int maxRetries = 3;
            int currentRetry = 0;
            
            while (currentRetry < maxRetries)
            {
                try
                {
                    currentRetry++;
                    StatusChanged?.Invoke($"🔧 HttpListener Setup (Versuch {currentRetry}/{maxRetries})...");
                    
                    // Alten Listener schließen
                    _httpListener?.Close();
                    _httpListener = new HttpListener();
                    
                    // Prefixes konfigurieren
                    bool networkAccessConfigured = await ConfigurePrefixes();
                    StatusChanged?.Invoke($"📋 Konfigurierte Prefixes: {_httpListener.Prefixes.Count}");
                    
                    if (_httpListener.Prefixes.Count == 0)
                    {
                        StatusChanged?.Invoke("❌ Keine gültigen Prefixes konfiguriert");
                        if (currentRetry < maxRetries)
                        {
                            StatusChanged?.Invoke($"🔄 Wiederhole in 2 Sekunden... (Versuch {currentRetry + 1}/{maxRetries})");
                            await Task.Delay(2000);
                            continue;
                        }
                        return false;
                    }
                    
                    // HttpListener starten
                    StatusChanged?.Invoke("🚀 Starte HttpListener...");
                    _httpListener.Start();
                    _isRunning = true;
                    
                    StatusChanged?.Invoke("✅ HttpListener erfolgreich gestartet");
                    
                    // Erfolgreiche Konfiguration melden
                    ReportSuccessfulStart(networkAccessConfigured);
                    
                    // Request Handler starten
                    StatusChanged?.Invoke("🔄 Starte Request-Handler...");
                    _ = Task.Run(HandleRequestsAsync);
                    
                    // Netzwerk-Tests durchführen
                    await TestNetworkReachability();
                    
                    StatusChanged?.Invoke("🎉 Mobile Server vollständig einsatzbereit!");
                    return true;
                }
                catch (HttpListenerException ex)
                {
                    StatusChanged?.Invoke($"🚨 HttpListener Fehler (Versuch {currentRetry}): {ex.ErrorCode} - {ex.Message}");
                    
                    switch (ex.ErrorCode)
                    {
                        case 5: // Access Denied
                            StatusChanged?.Invoke("🔑 Zugriff verweigert - Admin-Rechte erforderlich");
                            if (currentRetry >= maxRetries)
                            {
                                return await TryAlternativeServerMethods();
                            }
                            break;
                            
                        case 183: // Already exists
                            StatusChanged?.Invoke("⚠️ Port bereits belegt - warte und versuche erneut");
                            await Task.Delay(3000);
                            break;
                            
                        case 87: // Invalid parameter
                            StatusChanged?.Invoke("❌ Ungültige Prefix-Konfiguration");
                            return await TryMinimalConfiguration();
                            
                        default:
                            StatusChanged?.Invoke($"❌ Unbekannter HttpListener-Fehler: {ex.ErrorCode}");
                            break;
                    }
                    
                    if (currentRetry >= maxRetries)
                    {
                        StatusChanged?.Invoke("🚨 Maximale Wiederholungen erreicht - versuche alternative Methoden");
                        return await TryAlternativeServerMethods();
                    }
                }
                catch (Exception ex)
                {
                    StatusChanged?.Invoke($"❌ Unerwarteter Fehler (Versuch {currentRetry}): {ex.Message}");
                    
                    if (currentRetry >= maxRetries)
                    {
                        return await TryEmergencyServerStart();
                    }
                    
                    await Task.Delay(1000 * currentRetry); // Exponential backoff
                }
            }
            
            return false;
        }
        
        private void ReportSuccessfulStart(bool networkAccessConfigured)
        {
            var isAdmin = IsRunningAsAdministrator();
            
            if (networkAccessConfigured && _localIPAddress != "localhost")
            {
                var accessType = isAdmin ? "VOLLSTÄNDIGEM Admin" : "ERWEITERTEN";
                StatusChanged?.Invoke($"🌐 Mobile Server gestartet mit {accessType} Netzwerk-Zugriff!");
                StatusChanged?.Invoke($"📱 iPhone URL: http://{_localIPAddress}:{_port}/mobile");
                StatusChanged?.Invoke($"💻 Desktop URL: http://localhost:{_port}/mobile");
                StatusChanged?.Invoke($"🔧 Debug URL: http://localhost:{_port}/debug");
                StatusChanged?.Invoke($"🌍 QR-Code URL: {QRCodeUrl}");
            }
            else
            {
                StatusChanged?.Invoke($"⚠️ Mobile Server gestartet (EINGESCHRÄNKT - NUR LOCALHOST)");
                StatusChanged?.Invoke($"💻 Desktop URL: http://localhost:{_port}/mobile");
                StatusChanged?.Invoke($"🔧 Debug URL: http://localhost:{_port}/debug");
                StatusChanged?.Invoke("");
                StatusChanged?.Invoke("💡 FÜR IPHONE-ZUGRIFF:");
                StatusChanged?.Invoke("   • Als Administrator starten (empfohlen)");
                StatusChanged?.Invoke("   • 'Netzwerk konfigurieren' verwenden");
                StatusChanged?.Invoke("   • 'Ohne Admin-Rechte' für Alternativen");
                StatusChanged?.Invoke("   • Windows Mobile Hotspot aktivieren");
            }
        }
        
        private async Task<bool> TryMinimalConfiguration()
        {
            try
            {
                StatusChanged?.Invoke("🔄 Versuche minimale Konfiguration...");
                
                _httpListener?.Close();
                _httpListener = new HttpListener();
                
                // Nur localhost hinzufügen
                _httpListener.Prefixes.Add($"http://localhost:{_port}/");
                _httpListener.Prefixes.Add($"http://127.0.0.1:{_port}/");
                
                _httpListener.Start();
                _isRunning = true;
                
                StatusChanged?.Invoke("✅ Minimaler Server gestartet (nur localhost)");
                StatusChanged?.Invoke($"💻 URL: http://localhost:{_port}/mobile");
                
                _ = Task.Run(HandleRequestsAsync);
                return true;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Minimale Konfiguration fehlgeschlagen: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> TryEmergencyServerStart()
        {
            try
            {
                StatusChanged?.Invoke("🚨 NOTFALL-SERVER-START...");
                StatusChanged?.Invoke("🔄 Versuche alle verfügbaren Fallback-Methoden...");
                
                // Versuche alternative Ports
                var emergencyPorts = new[] { 8081, 8082, 8083, 9000, 9001, 3000, 5000, 7000 };
                
                foreach (var port in emergencyPorts)
                {
                    try
                    {
                        StatusChanged?.Invoke($"🔄 Teste Port {port}...");
                        
                        _httpListener?.Close();
                        _httpListener = new HttpListener();
                        _httpListener.Prefixes.Add($"http://localhost:{port}/");
                        
                        _httpListener.Start();
                        _isRunning = true;
                        
                        StatusChanged?.Invoke($"✅ NOTFALL-SERVER ERFOLGREICH auf Port {port}!");
                        StatusChanged?.Invoke($"💻 Notfall-URL: http://localhost:{port}/mobile");
                        StatusChanged?.Invoke($"📱 Für iPhone: Manuelle Netzwerk-Konfiguration erforderlich");
                        
                        _ = Task.Run(HandleRequestsAsync);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        StatusChanged?.Invoke($"⚠️ Port {port} fehlgeschlagen: {ex.Message}");
                    }
                }
                
                StatusChanged?.Invoke("❌ ALLE NOTFALL-METHODEN FEHLGESCHLAGEN");
                StatusChanged?.Invoke("💡 LETZTE LÖSUNGSVERSUCHE:");
                StatusChanged?.Invoke("   1. Computer neu starten");
                StatusChanged?.Invoke("   2. Als Administrator erneut versuchen");
                StatusChanged?.Invoke("   3. Windows Firewall temporär deaktivieren");
                StatusChanged?.Invoke("   4. Antivirus-Software prüfen");
                
                return false;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"🚨 KRITISCHER NOTFALL-FEHLER: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> CheckSystemRequirements()
        {
            try
            {
                StatusChanged?.Invoke("🔍 Prüfe System-Anforderungen...");
                
                // 1. .NET Version Check
                var dotnetVersion = Environment.Version;
                StatusChanged?.Invoke($"🔍 .NET Runtime: {dotnetVersion}");
                
                // 2. Windows Version Check
                var osVersion = Environment.OSVersion;
                StatusChanged?.Invoke($"🔍 Betriebssystem: {osVersion.VersionString}");
                
                if (osVersion.Platform != PlatformID.Win32NT)
                {
                    StatusChanged?.Invoke("❌ Nicht-Windows-System erkannt - HttpListener möglicherweise nicht verfügbar");
                    return false;
                }
                
                // 3. HttpListener Support Check
                if (!HttpListener.IsSupported)
                {
                    StatusChanged?.Invoke("❌ HttpListener wird auf diesem System nicht unterstützt");
                    return false;
                }
                else
                {
                    StatusChanged?.Invoke("✅ HttpListener wird unterstützt");
                }
                
                // 4. Network Interfaces Check
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                    .ToList();
                    
                StatusChanged?.Invoke($"🌐 Aktive Netzwerk-Interfaces: {networkInterfaces.Count}");
                
                if (networkInterfaces.Count == 0)
                {
                    StatusChanged?.Invoke("⚠️ Keine aktiven Netzwerk-Interfaces gefunden");
                }
                
                // 5. Port Availability Check
                var portCheck = await CheckPortAvailability(_port);
                if (!portCheck.IsAvailable)
                {
                    StatusChanged?.Invoke($"⚠️ Port {_port} wird verwendet von: {portCheck.ProcessName} (PID: {portCheck.ProcessId})");
                    StatusChanged?.Invoke("💡 Alternative Ports werden automatisch getestet");
                }
                else
                {
                    StatusChanged?.Invoke($"✅ Port {_port} ist verfügbar");
                }
                
                // 6. Firewall Check (ohne Admin-Rechte)
                await CheckFirewallStatusBasic();
                
                StatusChanged?.Invoke("✅ System-Anforderungen-Check abgeschlossen");
                return true;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ System-Check Fehler: {ex.Message}");
                // Auch bei Fehlern weitermachen - nicht kritisch
                return true;
            }
        }
        
        private async Task<PortStatus> CheckPortAvailability(int port)
        {
            try
            {
                var tcpConnections = System.Net.NetworkInformation.IPGlobalProperties
                    .GetIPGlobalProperties()
                    .GetActiveTcpListeners();
                
                var portInUse = tcpConnections.Any(endpoint => endpoint.Port == port);
                
                if (portInUse)
                {
                    // Versuche Process zu finden der den Port verwendet
                    try
                    {
                        var processInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "netstat",
                            Arguments = $"-ano | findstr :{port}",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        };
                        
                        using var process = System.Diagnostics.Process.Start(processInfo);
                        if (process != null)
                        {
                            await process.WaitForExitAsync();
                            var output = await process.StandardOutput.ReadToEndAsync();
                            
                            if (!string.IsNullOrEmpty(output))
                            {
                                var lines = output.Split('\n');
                                var portLine = lines.FirstOrDefault(l => l.Contains($":{port}"));
                                if (portLine != null)
                                {
                                    var parts = portLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Length > 0 && int.TryParse(parts.Last(), out int pid))
                                    {
                                        try
                                        {
                                            var proc = System.Diagnostics.Process.GetProcessById(pid);
                                            return new PortStatus 
                                            { 
                                                IsAvailable = false, 
                                                ProcessId = pid, 
                                                ProcessName = proc.ProcessName 
                                            };
                                        }
                                        catch
                                        {
                                            return new PortStatus { IsAvailable = false, ProcessId = pid, ProcessName = "Unknown" };
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // netstat-Fehler ignorieren
                    }
                    
                    return new PortStatus { IsAvailable = false, ProcessName = "Unknown Process" };
                }
                
                return new PortStatus { IsAvailable = true };
            }
            catch
            {
                return new PortStatus { IsAvailable = true }; // Bei Fehlern annehmen dass verfügbar
            }
        }
        
        private async Task CheckFirewallStatusBasic()
        {
            try
            {
                StatusChanged?.Invoke("🛡️ Prüfe grundlegende Firewall-Einstellungen...");
                
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "advfirewall show allprofiles state",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                
                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    var output = await process.StandardOutput.ReadToEndAsync();
                    
                    if (output.Contains("State                                 OFF"))
                    {
                        StatusChanged?.Invoke("🛡️ Windows Firewall ist deaktiviert");
                    }
                    else if (output.Contains("State                                 ON"))
                    {
                        StatusChanged?.Invoke("🛡️ Windows Firewall ist aktiv - Port-Freigabe eventuell erforderlich");
                        StatusChanged?.Invoke("💡 Verwenden Sie 'Netzwerk konfigurieren' für automatische Setup");
                    }
                }
            }
            catch
            {
                StatusChanged?.Invoke("🛡️ Firewall-Status konnte nicht ermittelt werden");
            }
        }
        
        private class PortStatus
        {
            public bool IsAvailable { get; set; }
            public int ProcessId { get; set; }
            public string ProcessName { get; set; } = "";
        }

        private async Task ConfigureNetworkAccessWithoutAdmin()
        {
            try
            {
                StatusChanged?.Invoke("🔧 Nicht-Admin Netzwerk-Konfiguration...");
                
                // 1. Prüfe ob URL Reservations bereits existieren
                StatusChanged?.Invoke("🔍 Prüfe bestehende URL-Reservierungen...");
                var hasUrlReservation = await CheckExistingUrlReservation();
                if (hasUrlReservation)
                {
                    StatusChanged?.Invoke("✅ URL-Reservierung bereits vorhanden (von vorheriger Admin-Session)");
                }
                
                // 2. Prüfe Windows Firewall Status
                StatusChanged?.Invoke("🛡️ Prüfe Windows Firewall...");
                var firewallStatus = await CheckFirewallStatus();
                StatusChanged?.Invoke($"🛡️ Firewall Status: {firewallStatus}");
                
                // 3. Versuche Windows Mobile Hotspot Integration
                StatusChanged?.Invoke("📡 Versuche Windows Mobile Hotspot Integration...");
                var hotspotSuccess = await TryWindowsHotspotIntegration();
                
                // 4. Versuche User-Level Konfiguration
                await TryUserLevelConfiguration();
                
                // 5. Gebe erweiterte Benutzer-Anweisungen
                await ProvideEnhancedUserInstructions(hotspotSuccess);
                
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ Nicht-Admin-Konfiguration Fehler: {ex.Message}");
            }
        }
        
        private async Task<bool> CheckExistingUrlReservation()
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "http show urlacl",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                
                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    var output = await process.StandardOutput.ReadToEndAsync();
                    
                    return output.Contains($":{_port}/") || output.Contains($"+:{_port}");
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        private async Task<string> CheckFirewallStatus()
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "advfirewall show allprofiles state",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                
                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    var output = await process.StandardOutput.ReadToEndAsync();
                    
                    if (output.Contains("State                                 OFF"))
                    {
                        return "Deaktiviert (alle Profile)";
                    }
                    else if (output.Contains("State                                 ON"))
                    {
                        return "Aktiviert - Port-Freigabe eventuell erforderlich";
                    }
                    else
                    {
                        return "Status unbekannt";
                    }
                }
                return "Nicht ermittelbar";
            }
            catch
            {
                return "Fehler bei Abfrage";
            }
        }
        
        private async Task TryUserLevelConfiguration()
        {
            try
            {
                StatusChanged?.Invoke("🔧 Versuche User-Level-Konfiguration...");
                
                // Erstelle User-spezifische Temp-Konfiguration
                var tempConfigPath = Path.Combine(Path.GetTempPath(), "EinsatzueberwachungMobile");
                Directory.CreateDirectory(tempConfigPath);
                
                var configFile = Path.Combine(tempConfigPath, "network_config.txt");
                var configContent = $@"Einsatzüberwachung Mobile - Netzwerk-Konfiguration
Erstellt: {DateTime.Now}
Port: {_port}
IP: {_localIPAddress}
Status: User-Level-Konfiguration aktiv
";
                await File.WriteAllTextAsync(configFile, configContent);
                StatusChanged?.Invoke($"💾 User-Konfiguration gespeichert: {configFile}");
                
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ User-Level-Konfiguration Fehler: {ex.Message}");
            }
        }
        
        private async Task ProvideUserInstructions()
        {
            await Task.Delay(100); // Ensure UI updates
            
            StatusChanged?.Invoke("📋 NICHT-ADMIN ANWEISUNGEN:");
            StatusChanged?.Invoke("┌─────────────────────────────────────────────┐");
            StatusChanged?.Invoke("│ 🔧 Manuelle Schritte für iPhone-Zugriff:   │");
            StatusChanged?.Invoke("│                                             │");
            StatusChanged?.Invoke("│ 1️⃣ Windows Firewall konfigurieren:          │");
            StatusChanged?.Invoke("│    • Windows-Einstellungen → Update & Sicherheit");
            StatusChanged?.Invoke("│    • Windows-Sicherheit → Firewall         │");
            StatusChanged?.Invoke("│    • App durch Firewall zulassen           │");
            StatusChanged?.Invoke("│    • Einsatzüberwachung auswählen          │");
            StatusChanged?.Invoke("│                                             │");
            StatusChanged?.Invoke("│ 2️⃣ Router-Einstellungen prüfen:             │");
            StatusChanged?.Invoke("│    • Client-Isolation deaktivieren         │");
            StatusChanged?.Invoke("│    • AP-Isolation deaktivieren             │");
            StatusChanged?.Invoke("│                                             │");
            StatusChanged?.Invoke("│ 3️⃣ Alternative: Hotspot verwenden:          │");
            StatusChanged?.Invoke("│    • Windows Hotspot aktivieren            │");
            StatusChanged?.Invoke("│    • iPhone mit Hotspot verbinden          │");
            StatusChanged?.Invoke("└─────────────────────────────────────────────┘");
        }
        
        private async Task ProvideEnhancedUserInstructions(bool hotspotAvailable)
        {
            await Task.Delay(100);
            
            StatusChanged?.Invoke("📋 ERWEITERTE NICHT-ADMIN LÖSUNGEN:");
            StatusChanged?.Invoke("┌─────────────────────────────────────────────┐");
            StatusChanged?.Invoke("│ 🎯 EMPFOHLENE LÖSUNG (einfachste):          │");
            
            if (hotspotAvailable)
            {
                StatusChanged?.Invoke("│ ✅ Windows Mobile Hotspot (siehe oben)      │");
                StatusChanged?.Invoke("│    → Funktioniert OHNE Administrator!      │");
            }
            else
            {
                StatusChanged?.Invoke("│ 📱 Smartphone als Hotspot verwenden:       │");
                StatusChanged?.Invoke("│    → iPhone Hotspot aktivieren             │");
                StatusChanged?.Invoke("│    → Desktop mit iPhone-Hotspot verbinden  │");
                StatusChanged?.Invoke("│    → URL: http://172.20.10.1:8080/mobile   │");
            }
            
            StatusChanged?.Invoke("│                                             │");
            StatusChanged?.Invoke("│ 🔧 ALTERNATIVE LÖSUNGEN:                    │");
            StatusChanged?.Invoke("│                                             │");
            StatusChanged?.Invoke("│ 1️⃣ Windows Firewall über GUI:               │");
            StatusChanged?.Invoke("│    • Systemsteuerung → System & Sicherheit │");
            StatusChanged?.Invoke("│    • Windows Defender Firewall             │");
            StatusChanged?.Invoke("│    • Eine App durch die Firewall zulassen  │");
            StatusChanged?.Invoke("│    • Einsatzüberwachung hinzufügen         │");
            StatusChanged?.Invoke("│                                             │");
            StatusChanged?.Invoke("│ 2️⃣ Router-basierte Lösung:                  │");
            StatusChanged?.Invoke("│    • Router-Webinterface öffnen            │");
            StatusChanged?.Invoke("│    • WLAN-Einstellungen → Erweitert        │");
            StatusChanged?.Invoke("│    • Client-Isolation deaktivieren         │");
            StatusChanged?.Invoke("│    • AP-Isolation deaktivieren             │");
            StatusChanged?.Invoke("│                                             │");
            StatusChanged?.Invoke("│ 3️⃣ Netzwerk-Profil ändern:                  │");
            StatusChanged?.Invoke("│    • Windows-Einstellungen → Netzwerk      │");
            StatusChanged?.Invoke("│    • Aktuelles Netzwerk → Eigenschaften    │");
            StatusChanged?.Invoke("│    • Netzwerkprofil: Privat auswählen      │");
            StatusChanged?.Invoke("│                                             │");
            StatusChanged?.Invoke("│ 4️⃣ Temporäre Firewall-Deaktivierung:       │");
            StatusChanged?.Invoke("│    • Nur für Tests empfohlen!              │");
            StatusChanged?.Invoke("│    • Windows-Sicherheit → Firewall         │");
            StatusChanged?.Invoke("│    • Firewall temporär deaktivieren        │");
            StatusChanged?.Invoke("│    • Nach Test wieder aktivieren!          │");
            StatusChanged?.Invoke("└─────────────────────────────────────────────┘");
            
            StatusChanged?.Invoke("💡 ERFOLGS-TIPPS:");
            StatusChanged?.Invoke($"   • Desktop-Test zuerst: http://localhost:{_port}/mobile");
            StatusChanged?.Invoke($"   • Debug-Seite nutzen: http://localhost:{_port}/debug");
            StatusChanged?.Invoke("   • API-Test Button für Diagnose verwenden");
            StatusChanged?.Invoke("   • Bei Erfolg: Als Administrator starten für automatische Konfiguration");
        }
        
        private async Task<bool> ConfigurePrefixes()
        {
            bool networkAccessConfigured = false;
            
            // Strategie 1: Spezifische IP-Adresse (funktioniert oft ohne Admin)
            if (_localIPAddress != "localhost" && !string.IsNullOrEmpty(_localIPAddress))
            {
                try
                {
                    _httpListener.Prefixes.Add($"http://{_localIPAddress}:{_port}/");
                    StatusChanged?.Invoke($"✅ Spezifische IP hinzugefügt: {_localIPAddress}:{_port}");
                    networkAccessConfigured = true;
                }
                catch (Exception ex)
                {
                    StatusChanged?.Invoke($"⚠️ Spezifische IP Fehler: {ex.Message}");
                }
            }
            
            // Strategie 2: Wildcard (nur mit Admin-Rechten)
            if (IsRunningAsAdministrator())
            {
                try
                {
                    _httpListener.Prefixes.Add($"http://+:{_port}/");
                    StatusChanged?.Invoke($"✅ Wildcard-Prefix hinzugefügt: +:{_port}");
                    networkAccessConfigured = true;
                }
                catch (Exception ex)
                {
                    StatusChanged?.Invoke($"⚠️ Wildcard-Prefix Fehler: {ex.Message}");
                }
            }
            
            // Strategie 3: Alle verfügbaren IP-Adressen einzeln (ohne Admin möglich)
            await AddAllAvailableIPs();
            
            // Strategie 4: Localhost-Fallback (immer möglich)
            try
            {
                _httpListener.Prefixes.Add($"http://localhost:{_port}/");
                _httpListener.Prefixes.Add($"http://127.0.0.1:{_port}/");
                StatusChanged?.Invoke($"✅ Localhost-Prefixes hinzugefügt");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Localhost-Prefix Fehler: {ex.Message}");
            }
            
            return networkAccessConfigured;
        }
        
        private async Task AddAllAvailableIPs()
        {
            try
            {
                StatusChanged?.Invoke("🔍 Füge alle verfügbaren IP-Adressen hinzu...");
                
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up && 
                               ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);
                
                foreach (var ni in networkInterfaces)
                {
                    var ipProperties = ni.GetIPProperties();
                    var ipAddresses = ipProperties.UnicastAddresses
                        .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork && 
                                     !IPAddress.IsLoopback(addr.Address))
                        .Select(addr => addr.Address);
                    
                    foreach (var ip in ipAddresses)
                    {
                        try
                        {
                            var prefix = $"http://{ip}:{_port}/";
                            if (!_httpListener.Prefixes.Contains(prefix))
                            {
                                _httpListener.Prefixes.Add(prefix);
                                StatusChanged?.Invoke($"✅ IP hinzugefügt: {ip} ({ni.Name})");
                            }
                        }
                        catch (Exception ex)
                        {
                            StatusChanged?.Invoke($"⚠️ IP {ip} Fehler: {ex.Message}");
                        }
                    }
                }
                
                await Task.Delay(100); // UI update time
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ IP-Enumeration Fehler: {ex.Message}");
            }
        }
        
        private async Task TestNetworkReachability()
        {
            try
            {
                StatusChanged?.Invoke("🔍 Teste Netzwerk-Erreichbarkeit...");
                
                // Teste localhost
                var localSuccess = await TestEndpoint($"http://localhost:{_port}/test");
                StatusChanged?.Invoke($"💻 Localhost-Test: {(localSuccess ? "✅ OK" : "❌ Fehlgeschlagen")}");
                
                // Teste Netzwerk-IP
                if (_localIPAddress != "localhost")
                {
                    var networkSuccess = await TestEndpoint($"http://{_localIPAddress}:{_port}/test");
                    StatusChanged?.Invoke($"📱 Netzwerk-Test: {(networkSuccess ? "✅ OK - iPhone-Zugriff möglich" : "❌ Fehlgeschlagen - nur Localhost")}");
                }
                
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ Netzwerk-Test Fehler: {ex.Message}");
            }
        }
        
        private async Task<bool> TestEndpoint(string url)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(2);
                var response = await client.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        
        private async Task<bool> TryAlternativeServerMethods()
        {
            try
            {
                StatusChanged?.Invoke("🔄 Versuche alternative Server-Methoden...");
                
                // Methode 1: Nur spezifische IP ohne Wildcard
                await TrySpecificIPOnly();
                
                // Methode 2: Alternativer Port
                if (!_isRunning)
                {
                    await TryAlternativePort();
                }
                
                // Methode 3: Minimaler Localhost-Server
                if (!_isRunning)
                {
                    await TryMinimalLocalhost();
                }
                
                return _isRunning;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Alternative Methoden fehlgeschlagen: {ex.Message}");
                return false;
            }
        }
        
        private async Task TrySpecificIPOnly()
        {
            try
            {
                StatusChanged?.Invoke("🔄 Versuche spezifische IP ohne Admin-Rechte...");
                
                _httpListener?.Close();
                _httpListener = new HttpListener();
                
                if (_localIPAddress != "localhost")
                {
                    _httpListener.Prefixes.Add($"http://{_localIPAddress}:{_port}/");
                }
                _httpListener.Prefixes.Add($"http://localhost:{_port}/");
                
                _httpListener.Start();
                _isRunning = true;
                
                StatusChanged?.Invoke("✅ Spezifische IP-Methode erfolgreich!");
                _ = Task.Run(HandleRequestsAsync);
                
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ Spezifische IP-Methode fehlgeschlagen: {ex.Message}");
            }
        }
        
        private async Task TryAlternativePort()
        {
            var alternativePorts = new[] { 8081, 8082, 8083, 9000, 9001 };
            
            foreach (var port in alternativePorts)
            {
                try
                {
                    StatusChanged?.Invoke($"🔄 Versuche alternativen Port: {port}");
                    
                    _httpListener?.Close();
                    _httpListener = new HttpListener();
                    _httpListener.Prefixes.Add($"http://localhost:{port}/");
                    
                    if (_localIPAddress != "localhost")
                    {
                        _httpListener.Prefixes.Add($"http://{_localIPAddress}:{port}/");
                    }
                    
                    _httpListener.Start();
                    _isRunning = true;
                    
                    // Update port for future reference
                    var originalPort = _port;
                    // _port = port; // Uncomment if you want to permanently change port
                    
                    StatusChanged?.Invoke($"✅ Alternativer Port {port} erfolgreich!");
                    StatusChanged?.Invoke($"📱 Neue iPhone URL: http://{_localIPAddress}:{port}/mobile");
                    StatusChanged?.Invoke($"💻 Neue Desktop URL: http://localhost:{port}/mobile");
                    
                    _ = Task.Run(HandleRequestsAsync);
                    return;
                    
                }
                catch (Exception ex)
                {
                    StatusChanged?.Invoke($"⚠️ Port {port} fehlgeschlagen: {ex.Message}");
                }
            }
        }
        
        private async Task TryMinimalLocalhost()
        {
            try
            {
                StatusChanged?.Invoke("🔄 Minimaler Localhost-Server als letzter Ausweg...");
                
                _httpListener?.Close();
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://localhost:{_port}/");
                
                _httpListener.Start();
                _isRunning = true;
                
                StatusChanged?.Invoke("✅ Minimaler Localhost-Server gestartet");
                StatusChanged?.Invoke("⚠️ Nur Desktop-Zugriff möglich");
                StatusChanged?.Invoke("💡 Für iPhone: Manuelle Firewall-Konfiguration erforderlich");
                
                _ = Task.Run(HandleRequestsAsync);
                
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Minimaler Server fehlgeschlagen: {ex.Message}");
            }
        }

        private async Task<bool> TryWindowsHotspotIntegration()
        {
            try
            {
                StatusChanged?.Invoke("📡 Prüfe Windows Mobile Hotspot...");
                
                // Prüfe ob Mobile Hotspot verfügbar ist
                var hotspotStatus = await CheckMobileHotspotStatus();
                
                if (hotspotStatus.IsAvailable)
                {
                    StatusChanged?.Invoke("✅ Windows Mobile Hotspot verfügbar");
                    
                    if (hotspotStatus.IsActive)
                    {
                        StatusChanged?.Invoke("🔥 Mobile Hotspot bereits aktiv");
                        StatusChanged?.Invoke($"📱 Hotspot-Name: {hotspotStatus.NetworkName}");
                        StatusChanged?.Invoke("💡 Verbinden Sie Ihr iPhone mit diesem Hotspot");
                        
                        // Verwende Hotspot-IP
                        var hotspotIP = await GetHotspotIPAddress();
                        if (!string.IsNullOrEmpty(hotspotIP))
                        {
                            try
                            {
                                _httpListener.Prefixes.Add($"http://{hotspotIP}:{_port}/");
                                StatusChanged?.Invoke($"✅ Hotspot-IP hinzugefügt: {hotspotIP}");
                                return true;
                            }
                            catch (Exception ex)
                            {
                                StatusChanged?.Invoke($"⚠️ Hotspot-IP Fehler: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        StatusChanged?.Invoke("💡 Tipp: Aktivieren Sie den Windows Mobile Hotspot für iPhone-Zugriff");
                        StatusChanged?.Invoke("   Settings → Network & Internet → Mobile hotspot");
                        return await ProvideHotspotInstructions();
                    }
                }
                else
                {
                    StatusChanged?.Invoke("⚠️ Windows Mobile Hotspot nicht verfügbar auf diesem System");
                }
                
                return false;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ Hotspot-Integration Fehler: {ex.Message}");
                return false;
            }
        }
        
        private async Task<HotspotStatus> CheckMobileHotspotStatus()
        {
            try
            {
                // Verwende PowerShell um Hotspot-Status zu prüfen
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = "-Command \"Get-NetAdapter | Where-Object {$_.InterfaceDescription -like '*Microsoft Wi-Fi Direct Virtual Adapter*'}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                
                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    var output = await process.StandardOutput.ReadToEndAsync();
                    
                    var isAvailable = !string.IsNullOrEmpty(output.Trim());
                    var isActive = output.Contains("Up") || output.Contains("Connected");
                    
                    return new HotspotStatus
                    {
                        IsAvailable = isAvailable,
                        IsActive = isActive,
                        NetworkName = isActive ? await GetHotspotNetworkName() : ""
                    };
                }
                
                return new HotspotStatus { IsAvailable = false, IsActive = false };
            }
            catch
            {
                return new HotspotStatus { IsAvailable = false, IsActive = false };
            }
        }
        
        private async Task<string> GetHotspotNetworkName()
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan show profiles",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                
                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    var output = await process.StandardOutput.ReadToEndAsync();
                    
                    // Suche nach aktuell aktivem Hotspot
                    var lines = output.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Contains("All User Profile") && line.Contains("*"))
                        {
                            var parts = line.Split(':');
                            if (parts.Length > 1)
                            {
                                return parts[1].Trim();
                            }
                        }
                    }
                }
                
                return "Windows Hotspot";
            }
            catch
            {
                return "Windows Hotspot";
            }
        }
        
        private async Task<string> GetHotspotIPAddress()
        {
            try
            {
                // Suche nach der Mobile Hotspot IP (normalerweise 192.168.137.1)
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                
                foreach (var ni in networkInterfaces)
                {
                    if (ni.Description.Contains("Microsoft Wi-Fi Direct Virtual Adapter") ||
                        ni.Name.Contains("Local Area Connection") && ni.OperationalStatus == OperationalStatus.Up)
                    {
                        var ipProperties = ni.GetIPProperties();
                        var ipAddress = ipProperties.UnicastAddresses
                            .FirstOrDefault(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                                                  addr.Address.ToString().StartsWith("192.168.137."));
                        
                        if (ipAddress != null)
                        {
                            return ipAddress.Address.ToString();
                        }
                    }
                }
                
                // Fallback: Standard Hotspot IP
                return "192.168.137.1";
            }
            catch
            {
                return "192.168.137.1";
            }
        }
        
        private async Task<bool> ProvideHotspotInstructions()
        {
            await Task.Delay(100);
            
            StatusChanged?.Invoke("📲 WINDOWS HOTSPOT ANWEISUNGEN (OHNE ADMIN):");
            StatusChanged?.Invoke("┌─────────────────────────────────────────────┐");
            StatusChanged?.Invoke("│ 🔥 Windows Mobile Hotspot aktivieren:       │");
            StatusChanged?.Invoke("│                                             │");
            StatusChanged?.Invoke("│ 1️⃣ Windows-Einstellungen öffnen (Win+I)     │");
            StatusChanged?.Invoke("│ 2️⃣ Netzwerk und Internet                    │");
            StatusChanged?.Invoke("│ 3️⃣ Mobiler Hotspot                         │");
            StatusChanged?.Invoke("│ 4️⃣ Mobilen Hotspot aktivieren              │");
            StatusChanged?.Invoke("│ 5️⃣ WLAN-Name und Passwort notieren         │");
            StatusChanged?.Invoke("│                                             │");
            StatusChanged?.Invoke("│ 📱 iPhone-Verbindung:                       │");
            StatusChanged?.Invoke("│ 1️⃣ iPhone WLAN-Einstellungen               │");
            StatusChanged?.Invoke("│ 2️⃣ Windows Hotspot-Name wählen             │");
            StatusChanged?.Invoke("│ 3️⃣ Passwort eingeben                       │");
            StatusChanged?.Invoke("│ 4️⃣ Verbinden                               │");
            StatusChanged?.Invoke("│                                             │");
            StatusChanged?.Invoke("│ 🌐 URL: http://192.168.137.1:8080/mobile   │");
            StatusChanged?.Invoke("└─────────────────────────────────────────────┘");
            
            return true;
        }
        
        private class HotspotStatus
        {
            public bool IsAvailable { get; set; }
            public bool IsActive { get; set; }
            public string NetworkName { get; set; } = "";
        }

        private void CheckFirewallAndNetwork()
        {
            try
            {
                StatusChanged?.Invoke("🛡️ Prüfe Firewall und Netzwerk...");
                
                // Prüfe ob Port 8080 bereits verwendet wird
                var tcpConnections = System.Net.NetworkInformation.IPGlobalProperties
                    .GetIPGlobalProperties()
                    .GetActiveTcpListeners();
                
                var portInUse = tcpConnections.Any(endpoint => endpoint.Port == _port);
                if (portInUse)
                {
                    StatusChanged?.Invoke($"⚠️ Port {_port} wird bereits verwendet!");
                }
                else
                {
                    StatusChanged?.Invoke($"✅ Port {_port} ist verfügbar");
                }
                
                // Prüfe Windows-Version und Admin-Rechte
                var isAdmin = IsRunningAsAdministrator();
                StatusChanged?.Invoke($"🔐 Administrator-Rechte: {(isAdmin ? "✅ Ja" : "❌ Nein")}");
                
                if (!isAdmin)
                {
                    StatusChanged?.Invoke("💡 Tipp: Als Administrator starten für iPhone-Zugriff!");
                }
                
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ Firewall-Check Fehler: {ex.Message}");
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
            catch
            {
                return false;
            }
        }

        private async Task<bool> ConfigureNetworkAccess()
        {
            try
            {
                StatusChanged?.Invoke("🔧 Konfiguriere automatischen Netzwerk-Zugriff...");
                
                if (!IsRunningAsAdministrator())
                {
                    StatusChanged?.Invoke("⚠️ Keine Administrator-Rechte - Netzwerk-Konfiguration übersprungen");
                    return false;
                }
                
                bool success = true;
                
                // 1. HTTP URL Reservation hinzufügen
                success &= await AddHttpUrlReservation();
                
                // 2. Firewall-Regel hinzufügen
                success &= await AddFirewallRule();
                
                if (success)
                {
                    StatusChanged?.Invoke("✅ Automatische Netzwerk-Konfiguration erfolgreich!");
                }
                else
                {
                    StatusChanged?.Invoke("⚠️ Netzwerk-Konfiguration teilweise fehlgeschlagen");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Netzwerk-Konfiguration Fehler: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> AddHttpUrlReservation()
        {
            try
            {
                StatusChanged?.Invoke("🔗 Füge HTTP URL Reservation hinzu...");
                
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"http add urlacl url=http://+:{_port}/ user=Everyone",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Verb = "runas" // Stellt sicher, dass es mit Admin-Rechten läuft
                };
                
                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    
                    if (process.ExitCode == 0 || output.Contains("already exists"))
                    {
                        StatusChanged?.Invoke($"✅ URL Reservation: http://+:{_port}/");
                        return true;
                    }
                    else
                    {
                        StatusChanged?.Invoke($"⚠️ URL Reservation Warnung: {error}");
                        return false;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ URL Reservation Fehler: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> AddFirewallRule()
        {
            try
            {
                StatusChanged?.Invoke("🛡️ Füge Firewall-Regel hinzu...");
                
                var ruleName = "Einsatzueberwachung_Mobile";
                
                // Prüfe erst ob Regel bereits existiert
                var checkProcessInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall show rule name=\"{ruleName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using var checkProcess = System.Diagnostics.Process.Start(checkProcessInfo);
                if (checkProcess != null)
                {
                    await checkProcess.WaitForExitAsync();
                    var checkOutput = await checkProcess.StandardOutput.ReadToEndAsync();
                    
                    if (checkOutput.Contains(ruleName))
                    {
                        StatusChanged?.Invoke("✅ Firewall-Regel bereits vorhanden");
                        return true;
                    }
                }
                
                // Füge neue Regel hinzu
                var addProcessInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall add rule name=\"{ruleName}\" dir=in action=allow protocol=TCP localport={_port}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using var addProcess = System.Diagnostics.Process.Start(addProcessInfo);
                if (addProcess != null)
                {
                    await addProcess.WaitForExitAsync();
                    var output = await addProcess.StandardOutput.ReadToEndAsync();
                    var error = await addProcess.StandardError.ReadToEndAsync();
                    
                    if (addProcess.ExitCode == 0)
                    {
                        StatusChanged?.Invoke($"✅ Firewall-Regel hinzugefügt: {ruleName} (Port {_port})");
                        return true;
                    }
                    else
                    {
                        StatusChanged?.Invoke($"⚠️ Firewall-Regel Warnung: {error}");
                        return false;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ Firewall-Regel Fehler: {ex.Message}");
                return false;
            }
        }

        public void StopServer()
        {
            try
            {
                StatusChanged?.Invoke("🛑 Stoppe Mobile Server...");
                
                _isRunning = false;
                _httpListener?.Stop();
                _httpListener?.Close();
                _httpListener = null;
                
                StatusChanged?.Invoke("✅ Mobile Server gestoppt");
                StatusChanged?.Invoke("💡 Netzwerk-Konfiguration bleibt für zukünftige Sessions bestehen");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Fehler beim Stoppen des Servers: {ex.Message}");
            }
        }
        
        public async Task<bool> CleanupNetworkConfiguration()
        {
            try
            {
                if (!IsRunningAsAdministrator())
                {
                    StatusChanged?.Invoke("⚠️ Administrator-Rechte erforderlich für Netzwerk-Cleanup");
                    return false;
                }
                
                StatusChanged?.Invoke("🧹 Bereinige Netzwerk-Konfiguration...");
                
                bool success = true;
                
                // URL Reservation entfernen
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"http delete urlacl url=http://+:{_port}/",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0)
                    {
                        StatusChanged?.Invoke("✅ URL Reservation entfernt");
                    }
                    else
                    {
                        StatusChanged?.Invoke("⚠️ URL Reservation nicht gefunden oder bereits entfernt");
                    }
                }
                
                // Firewall-Regel entfernen (optional)
                var firewallProcessInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "advfirewall firewall delete rule name=\"Einsatzueberwachung_Mobile\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using var firewallProcess = System.Diagnostics.Process.Start(firewallProcessInfo);
                if (firewallProcess != null)
                {
                    await firewallProcess.WaitForExitAsync();
                    if (firewallProcess.ExitCode == 0)
                    {
                        StatusChanged?.Invoke("✅ Firewall-Regel entfernt");
                    }
                    else
                    {
                        StatusChanged?.Invoke("⚠️ Firewall-Regel nicht gefunden oder bereits entfernt");
                    }
                }
                
                return success;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Cleanup Fehler: {ex.Message}");
                return false;
            }
        }

        private async Task HandleRequestsAsync()
        {
            while (_isRunning && _httpListener != null)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();
                    _ = Task.Run(() => ProcessRequest(context));
                }
                catch (HttpListenerException ex) when (!_isRunning)
                {
                    // Expected exception when server is stopping
                    break;
                }
                catch (ObjectDisposedException) when (!_isRunning)
                {
                    // Expected exception when server is disposed
                    break;
                }
                catch (Exception ex)
                {
                    StatusChanged?.Invoke($"Server-Fehler: {ex.Message}");
                    // Continue running unless critical error
                    if (!_isRunning) break;
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            
            try
            {
                // Detailliertes Logging für Debugging
                var url = request.Url?.ToString() ?? "Unknown URL";
                var method = request.HttpMethod;
                var path = request.Url?.AbsolutePath ?? "/";
                var userAgent = request.UserAgent ?? "Unknown";
                
                StatusChanged?.Invoke($"📱 {method} {path} from {userAgent}");
                
                // Setze Standard-Headers für alle Responses
                response.Headers.Add("Server", "Einsatzueberwachung-Mobile/1.6");
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");
                
                // Handle OPTIONS preflight requests (CORS)
                if (request.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    response.StatusCode = 200;
                    response.ContentLength64 = 0;
                    StatusChanged?.Invoke("✅ CORS preflight handled");
                    return;
                }
                
                // Normalisiere den Path
                path = path.TrimEnd('/');
                if (string.IsNullOrEmpty(path))
                    path = "/mobile"; // Default
                
                StatusChanged?.Invoke($"🔄 Processing path: {path}");
                
                switch (path.ToLowerInvariant())
                {
                    case "":
                    case "/":
                        // Redirect to mobile interface
                        response.StatusCode = 302;
                        response.Headers.Add("Location", "/mobile");
                        response.ContentLength64 = 0;
                        StatusChanged?.Invoke("↩️ Redirected to /mobile");
                        return;
                        
                    case "/mobile":
                        ServeMobileInterface(response);
                        StatusChanged?.Invoke("✅ Mobile interface served");
                        break;
                        
                    case "/api/status":
                        ServeApiStatus(response);
                        StatusChanged?.Invoke("✅ API status served");
                        break;
                        
                    case "/api/teams":
                        ServeApiTeams(response);
                        StatusChanged?.Invoke("✅ API teams served");
                        break;
                        
                    case "/api/notes":
                        ServeApiNotes(response);
                        StatusChanged?.Invoke("✅ API notes served");
                        break;
                        
                    case "/test":
                    case "/ping":
                        ServeTestResponse(response);
                        StatusChanged?.Invoke("✅ Test endpoint served");
                        break;

                    case "/debug":
                        ServeDebugPage(response);
                        StatusChanged?.Invoke("✅ Debug page served");
                        break;
                        
                    case "/qr":
                    case "/qr.png":
                        ServeQRCode(response);
                        StatusChanged?.Invoke("✅ QR code served");
                        break;
                        
                    case "/favicon.ico":
                        // Return empty favicon to avoid 404s
                        response.StatusCode = 204;
                        response.ContentLength64 = 0;
                        StatusChanged?.Invoke("📄 Favicon request ignored");
                        return;
                        
                    default:
                        StatusChanged?.Invoke($"❌ Unknown path: {path}");
                        ServeError(response, 404, $"Path not found: {path}");
                        break;
                }
            }
            catch (HttpListenerException ex)
            {
                StatusChanged?.Invoke($"🚨 HttpListener Exception: {ex.ErrorCode} - {ex.Message}");
                try
                {
                    if (ex.ErrorCode == 1229) // Connection was forcibly closed
                    {
                        StatusChanged?.Invoke("⚠️ Client disconnected");
                        return;
                    }
                    ServeError(response, 500, $"Network error: {ex.Message}");
                }
                catch { /* Ignore secondary errors */ }
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"🚨 Server Exception: {ex.GetType().Name} - {ex.Message}");
                try
                {
                    ServeError(response, 500, $"Server error: {ex.Message}");
                }
                catch { /* Ignore secondary errors */ }
            }
            finally
            {
                try
                {
                    response?.Close();
                }
                catch (Exception ex)
                {
                    StatusChanged?.Invoke($"⚠️ Response close error: {ex.Message}");
                }
            }
        }

        private void ServeMobileInterface(HttpListenerResponse response)
        {
            try
            {
                StatusChanged?.Invoke("🔄 Generating mobile HTML...");
                var html = GenerateMobileHTML();
                
                if (string.IsNullOrEmpty(html))
                {
                    StatusChanged?.Invoke("❌ Generated HTML is empty!");
                    ServeError(response, 500, "Failed to generate mobile interface");
                    return;
                }
                
                var buffer = Encoding.UTF8.GetBytes(html);
                StatusChanged?.Invoke($"📄 HTML size: {buffer.Length} bytes");
                
                // Setze Response Headers
                response.StatusCode = 200;
                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength64 = buffer.Length;
                response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
                response.Headers.Add("Pragma", "no-cache");
                response.Headers.Add("Expires", "0");
                
                StatusChanged?.Invoke("📤 Writing HTML response...");
                
                // Schreibe Content in kleinen Chunks für bessere Fehlerbehandlung
                int bytesWritten = 0;
                int chunkSize = 8192; // 8KB chunks
                
                while (bytesWritten < buffer.Length)
                {
                    int remainingBytes = buffer.Length - bytesWritten;
                    int currentChunkSize = Math.Min(chunkSize, remainingBytes);
                    
                    response.OutputStream.Write(buffer, bytesWritten, currentChunkSize);
                    bytesWritten += currentChunkSize;
                }
                
                response.OutputStream.Flush();
                StatusChanged?.Invoke($"✅ Mobile interface delivered successfully ({bytesWritten} bytes)");
            }
            catch (HttpListenerException ex)
            {
                StatusChanged?.Invoke($"🚨 HTTP error serving mobile interface: {ex.ErrorCode} - {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"🚨 Error serving mobile interface: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }

        private void ServeApiStatus(HttpListenerResponse response)
        {
            var einsatzData = GetEinsatzData?.Invoke();
            var teams = GetCurrentTeams?.Invoke() ?? new List<Team>();
            
            var status = new
            {
                server = "Einsatzüberwachung Mobile API",
                version = "1.6",
                timestamp = DateTime.Now,
                active = true,
                serverIP = _localIPAddress,
                mission = new
                {
                    location = einsatzData?.Einsatzort ?? "Unbekannt",
                    leader = einsatzData?.Einsatzleiter ?? "Unbekannt",
                    startTime = einsatzData?.EinsatzDatum ?? DateTime.Now,
                    duration = einsatzData != null ? DateTime.Now.Subtract(einsatzData.EinsatzDatum).ToString(@"hh\:mm\:ss") : "00:00:00"
                },
                teamCount = teams.Count,
                activeTeams = teams.Count(t => t.IsRunning)
            };
            
            ServeJson(response, status);
        }

        private void ServeApiTeams(HttpListenerResponse response)
        {
            try
            {
                var teams = GetCurrentTeams?.Invoke() ?? new List<Team>();
                
                var teamData = teams.Select(team => new
                {
                    id = team.TeamId,
                    name = team.TeamName,
                    dogName = team.HundName,
                    handler = team.Hundefuehrer,
                    helper = team.Helfer,
                    types = team.MultipleTeamTypes?.SelectedTypes?.Select(t => t.ToString()).ToArray() ?? new[] { team.TeamType.ToString() },
                    typeDisplay = team.TeamTypeDisplayName,
                    status = team.IsRunning ? "active" : "ready",
                    time = team.ElapsedTimeString,
                    isFirstWarning = team.IsFirstWarning,
                    isSecondWarning = team.IsSecondWarning,
                    firstWarningMinutes = team.FirstWarningMinutes,
                    secondWarningMinutes = team.SecondWarningMinutes,
                    notes = team.NotesEntries?.Take(10).Select(note => new
                    {
                        time = note.Timestamp.ToString("HH:mm"),
                        content = note.Content,
                        type = note.EntryType.ToString()
                    }).ToArray() ?? Array.Empty<object>(),
                    notesCount = team.NotesEntries?.Count ?? 0
                }).ToArray();
                
                ServeJson(response, teamData);
            }
            catch (Exception ex)
            {
                // Fallback to empty array if teams can't be retrieved
                ServeJson(response, Array.Empty<object>());
                StatusChanged?.Invoke($"Fehler beim Laden der Teams: {ex.Message}");
            }
        }

        private void ServeApiNotes(HttpListenerResponse response)
        {
            try
            {
                var teams = GetCurrentTeams?.Invoke() ?? new List<Team>();
                
                var allNotes = teams.SelectMany(team => 
                    team.NotesEntries?.Select(note => new
                    {
                        teamId = team.TeamId,
                        teamName = team.TeamName,
                        time = note.Timestamp.ToString("HH:mm:ss"),
                        content = note.Content,
                        type = note.EntryType.ToString(),
                        timestamp = note.Timestamp
                    }) ?? Enumerable.Empty<object>()
                ).OrderByDescending(note => ((dynamic)note).timestamp)
                .Take(50) // Letzte 50 Notizen
                .ToArray();
                
                ServeJson(response, allNotes);
            }
            catch (Exception ex)
            {
                ServeJson(response, Array.Empty<object>());
                StatusChanged?.Invoke($"Fehler beim Laden der Notizen: {ex.Message}");
            }
        }

        private void ServeTestResponse(HttpListenerResponse response)
        {
            try
            {
                var testData = new
                {
                    status = "OK",
                    message = "Einsatzueberwachung Mobile Server is running",
                    version = "1.6",
                    timestamp = DateTime.Now,
                    server_ip = _localIPAddress,
                    endpoints = new[]
                    {
                        "/mobile - Mobile Web App",
                        "/api/status - Server Status",
                        "/api/teams - Team Data",
                        "/api/notes - Notes Data",
                        "/test - This test endpoint",
                        "/qr - QR Code image"
                    }
                };
                
                ServeJson(response, testData);
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Error serving test response: {ex.Message}");
                throw;
            }
        }

        private void ServeJson(HttpListenerResponse response, object data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true 
                });
                var buffer = Encoding.UTF8.GetBytes(json);
                
                response.StatusCode = 200;
                response.ContentType = "application/json; charset=utf-8";
                response.ContentLength64 = buffer.Length;
                response.Headers.Add("Cache-Control", "no-cache");
                
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Flush();
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Error serving JSON: {ex.Message}");
                throw;
            }
        }

        private void ServeError(HttpListenerResponse response, int statusCode, string message)
        {
            try
            {
                StatusChanged?.Invoke($"🚨 Serving error {statusCode}: {message}");
                
                response.StatusCode = statusCode;
                
                var errorResponse = new 
                { 
                    error = message, 
                    statusCode = statusCode,
                    timestamp = DateTime.Now,
                    server = "Einsatzueberwachung Mobile v1.6"
                };
                
                var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true 
                });
                
                var buffer = Encoding.UTF8.GetBytes(json);
                
                response.ContentType = "application/json; charset=utf-8";
                response.ContentLength64 = buffer.Length;
                response.Headers.Add("Cache-Control", "no-cache");
                
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Flush();
                
                StatusChanged?.Invoke($"📤 Error response sent: {statusCode}");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"🚨 Failed to send error response: {ex.Message}");
                // Last resort: try to set status code only
                try
                {
                    response.StatusCode = statusCode;
                }
                catch { /* Give up */ }
            }
        }

        private void ServeQRCode(HttpListenerResponse response)
        {
            try
            {
                var qrCodeBytes = GenerateQRCode();
                if (qrCodeBytes.Length > 0)
                {
                    response.ContentType = "image/png";
                    response.ContentLength64 = qrCodeBytes.Length;
                    response.OutputStream.Write(qrCodeBytes, 0, qrCodeBytes.Length);
                }
                else
                {
                    ServeError(response, 500, "QR-Code generation failed");
                }
            }
            catch (Exception ex)
            {
                ServeError(response, 500, $"QR-Code error: {ex.Message}");
            }
        }

        private void ServeDebugPage(HttpListenerResponse response)
        {
            try
            {
                var teams = GetCurrentTeams?.Invoke() ?? new List<Team>();
                var einsatzData = GetEinsatzData?.Invoke();
                
                var debugHtml = $@"<!DOCTYPE html>
<html>
<head>
    <title>🔧 Debug - Einsatzüberwachung Mobile</title>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: monospace; margin: 20px; background: #f0f0f0; }}
        .section {{ background: white; margin: 10px 0; padding: 15px; border-radius: 5px; border: 1px solid #ddd; }}
        .ok {{ color: green; }} .error {{ color: red; }} .warning {{ color: orange; }}
        pre {{ background: #f8f8f8; padding: 10px; overflow-x: auto; }}
        table {{ width: 100%; border-collapse: collapse; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
    </style>
</head>
<body>
    <h1>🔧 Einsatzüberwachung Mobile - Debug Information</h1>
    
    <div class='section'>
        <h2>📊 Server Status</h2>
        <table>
            <tr><th>Property</th><th>Value</th><th>Status</th></tr>
            <tr><td>Server Version</td><td>1.6</td><td class='ok'>✅ OK</td></tr>
            <tr><td>Current Time</td><td>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</td><td class='ok'>✅ OK</td></tr>
            <tr><td>Server IP</td><td>{_localIPAddress}</td><td class='{(_localIPAddress != "localhost" ? "ok" : "warning")}'>
                {(_localIPAddress != "localhost" ? "✅ Network Access" : "⚠️ Localhost Only")}</td></tr>
            <tr><td>Base URL</td><td>{_baseUrl}</td><td class='ok'>✅ OK</td></tr>
            <tr><td>Is Running</td><td>{_isRunning}</td><td class='{(_isRunning ? "ok" : "error")}'>{(_isRunning ? "✅ Running" : "❌ Stopped")}</td></tr>
        </table>
    </div>

    <div class='section'>
        <h2>🐕 Team Data</h2>
        <table>
            <tr><th>Team Count</th><td>{teams.Count}</td><td class='{(teams.Count > 0 ? "ok" : "warning")}'>{(teams.Count > 0 ? "✅ Teams Available" : "⚠️ No Teams")}</td></tr>
            <tr><th>Active Teams</th><td>{teams.Count(t => t.IsRunning)}</td><td class='ok'>✅ OK</td></tr>
        </table>
        
        {(teams.Count > 0 ? $@"<h3>Team Details:</h3>
        <table>
            <tr><th>ID</th><th>Name</th><th>Dog</th><th>Handler</th><th>Status</th><th>Time</th></tr>
            {string.Join("", teams.Take(5).Select(t => $@"<tr>
                <td>{t.TeamId}</td><td>{t.TeamName}</td><td>{t.HundName}</td><td>{t.Hundefuehrer}</td>
                <td class='{(t.IsRunning ? "ok" : "warning")}'>{(t.IsRunning ? "Active" : "Ready")}</td>
                <td>{t.ElapsedTimeString}</td>
            </tr>"))}
        </table>" : "<p class='warning'>No teams created yet. Add teams in the desktop application.</p>")}
    </div>

    <div class='section'>
        <h2>📋 Mission Data</h2>
        <table>
            <tr><th>Mission Location</th><td>{einsatzData?.Einsatzort ?? "Not Set"}</td></tr>
            <tr><th>Mission Leader</th><td>{einsatzData?.Einsatzleiter ?? "Not Set"}</td></tr>
            <tr><th>Mission Start</th><td>{einsatzData?.EinsatzDatum.ToString("yyyy-MM-dd HH:mm:ss") ?? "Not Set"}</td></tr>
            <tr><th>Mission Duration</th><td>{(einsatzData != null ? DateTime.Now.Subtract(einsatzData.EinsatzDatum).ToString(@"hh\:mm\:ss") : "Not Set")}</td></tr>
        </table>
    </div>

    <div class='section'>
        <h2>🌐 API Endpoints</h2>
        <p>Test these URLs in your browser or with curl:</p>
        <ul>
            <li><a href='/mobile'>/mobile</a> - Mobile Web Application</li>
            <li><a href='/api/status'>/api/status</a> - Server Status JSON</li>
            <li><a href='/api/teams'>/api/teams</a> - Teams Data JSON</li>
            <li><a href='/api/notes'>/api/notes</a> - Notes Data JSON</li>
            <li><a href='/test'>/test</a> - Simple Test Endpoint</li>
            <li><a href='/qr'>/qr</a> - QR Code Image</li>
            <li><a href='/debug'>/debug</a> - This Debug Page</li>
        </ul>
    </div>

    <div class='section'>
        <h2>📱 Mobile Access Instructions</h2>
        <ol>
            <li><strong>Same WiFi Network:</strong> Ensure both desktop and mobile device are on the same WiFi</li>
            <li><strong>Correct URL:</strong> Use <code>http://{_localIPAddress}:8080/mobile</code></li>
            <li><strong>No HTTPS:</strong> Use HTTP (not HTTPS) - mobile browsers might auto-correct this</li>
            <li><strong>Clear Cache:</strong> Clear browser cache if you see old content</li>
            <li><strong>Admin Rights:</strong> Run desktop app as Administrator for network access</li>
        </ol>
    </div>

    <div class='section'>
        <h2>🔧 Troubleshooting</h2>
        <h3>Common Issues:</h3>
        <ul>
            <li><strong>HTTP 400:</strong> Check URL format - must include '/mobile' suffix</li>
            <li><strong>Connection Refused:</strong> Check Windows Firewall, port 8080 must be open</li>
            <li><strong>Localhost Only:</strong> Run as Administrator for network access</li>
            <li><strong>Blank Page:</strong> Clear browser cache, disable content blockers</li>
        </ul>
        
        <h3>Quick Tests:</h3>
        <ol>
            <li>Test locally: <a href='http://localhost:8080/test'>http://localhost:8080/test</a></li>
            <li>Test mobile endpoint: <a href='/mobile'>/mobile</a></li>
            <li>Test API: <a href='/api/status'>/api/status</a></li>
        </ol>
    </div>

    <div class='section'>
        <h2>📝 Request Log</h2>
        <p><em>Check the Desktop Application's Mobile Connection Window for real-time request logs.</em></p>
        <p>Each request should show: Method, Path, User-Agent, and Result</p>
    </div>

    <script>
        // Auto-refresh every 30 seconds
        setTimeout(() => location.reload(), 30000);
        
        // Add current URL info
        document.addEventListener('DOMContentLoaded', function() {{
            const urlInfo = document.createElement('div');
            urlInfo.className = 'section';
            urlInfo.innerHTML = `<h2>🌐 Current Access Info</h2>
                <p><strong>Your Browser URL:</strong> ${{window.location.href}}</p>
                <p><strong>User Agent:</strong> ${{navigator.userAgent}}</p>
                <p><strong>Screen Size:</strong> ${{screen.width}}x${{screen.height}}</p>
                <p><strong>Viewport:</strong> ${{window.innerWidth}}x${{window.innerHeight}}</p>`;
            document.body.appendChild(urlInfo);
        }});
    </script>
</body>
</html>";

                var buffer = Encoding.UTF8.GetBytes(debugHtml);
                
                response.StatusCode = 200;
                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength64 = buffer.Length;
                response.Headers.Add("Cache-Control", "no-cache");
                
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Flush();
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Error serving debug page: {ex.Message}");
                throw;
            }
        }

        private string GenerateMobileHTML()
        {
            return @"<!DOCTYPE html>
<html lang='de'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, user-scalable=no'>
    <meta name='apple-mobile-web-app-capable' content='yes'>
    <meta name='apple-mobile-web-app-status-bar-style' content='black-translucent'>
    <meta name='apple-mobile-web-app-title' content='Einsatzüberwachung'>
    <title>🐕 Einsatzüberwachung Mobile</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            color: white;
            -webkit-user-select: none;
            user-select: none;
            -webkit-touch-callout: none;
            -webkit-tap-highlight-color: transparent;
        }
        .container { 
            max-width: 600px; 
            margin: 0 auto; 
            padding: 20px; 
            padding-bottom: 120px; /* Space for FAB */
        }
        .header {
            text-align: center;
            margin-bottom: 30px;
        }
        .header h1 {
            font-size: 2rem;
            margin-bottom: 10px;
        }
        .header p {
            opacity: 0.8;
            font-size: 1rem;
        }
        .status {
            background: rgba(255,255,255,0.1);
            border-radius: 15px;
            padding: 20px;
            margin-bottom: 20px;
            backdrop-filter: blur(10px);
            -webkit-backdrop-filter: blur(10px);
        }
        .team-grid {
            display: grid;
            gap: 15px;
        }
        .team-card {
            background: rgba(255,255,255,0.15);
            border-radius: 15px;
            padding: 20px;
            backdrop-filter: blur(10px);
            -webkit-backdrop-filter: blur(10px);
            border: 1px solid rgba(255,255,255,0.2);
            transition: all 0.3s ease;
            touch-action: manipulation;
        }
        .team-card:active {
            background: rgba(255,255,255,0.2);
            transform: scale(0.98);
        }
        .team-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 15px;
        }
        .team-name {
            font-weight: bold;
            font-size: 1.1rem;
        }
        .team-info {
            font-size: 0.9rem;
            opacity: 0.8;
            margin: 5px 0;
        }
        .team-types {
            display: flex;
            flex-wrap: wrap;
            gap: 5px;
            margin: 8px 0;
        }
        .type-badge {
            background: rgba(255,255,255,0.2);
            padding: 3px 8px;
            border-radius: 12px;
            font-size: 0.7rem;
            font-weight: bold;
        }
        .team-status {
            padding: 5px 12px;
            border-radius: 20px;
            font-size: 0.8rem;
            font-weight: bold;
        }
        .status-active { background: #4CAF50; }
        .status-ready { background: #FF9800; }
        .team-time {
            font-size: 2.5rem;
            font-weight: bold;
            text-align: center;
            margin: 15px 0;
            font-family: 'SF Mono', Monaco, 'Cascadia Code', 'Roboto Mono', Consolas, 'Courier New', monospace;
            text-shadow: 0 2px 4px rgba(0,0,0,0.3);
        }
        .warning-indicator {
            text-align: center;
            font-size: 0.9rem;
            margin: 5px 0;
            font-weight: bold;
        }
        .warning-1 { color: #FF9800; text-shadow: 0 1px 2px rgba(0,0,0,0.5); }
        .warning-2 { color: #f44336; animation: blink 1s infinite; text-shadow: 0 1px 2px rgba(0,0,0,0.5); }
        @keyframes blink { 0%, 50% { opacity: 1; } 51%, 100% { opacity: 0.5; } }
        .team-notes {
            background: rgba(255,255,255,0.1);
            border-radius: 10px;
            padding: 12px;
            margin-top: 15px;
            max-height: 120px;
            overflow-y: auto;
            -webkit-overflow-scrolling: touch;
        }
        .team-notes h4 {
            font-size: 0.8rem;
            margin-bottom: 8px;
            opacity: 0.8;
            display: flex;
            align-items: center;
            gap: 5px;
        }
        .notes-list {
            font-size: 0.75rem;
            line-height: 1.4;
        }
        .note-entry {
            margin-bottom: 4px;
            padding: 4px 0;
            border-bottom: 1px solid rgba(255,255,255,0.1);
        }
        .note-time {
            opacity: 0.6;
            font-weight: bold;
        }
        .note-content {
            margin-left: 8px;
        }
        .connection-status {
            position: fixed;
            top: env(safe-area-inset-top, 10px);
            right: 10px;
            padding: 5px 10px;
            border-radius: 15px;
            font-size: 0.8rem;
            background: #4CAF50;
            z-index: 1000;
        }
        .quick-actions {
            position: fixed;
            bottom: env(safe-area-inset-bottom, 20px);
            right: 20px;
            display: flex;
            flex-direction: column;
            gap: 10px;
        }
        .fab {
            width: 56px;
            height: 56px;
            border-radius: 50%;
            border: none;
            background: #2196F3;
            color: white;
            font-size: 1.5rem;
            cursor: pointer;
            box-shadow: 0 4px 8px rgba(0,0,0,0.3);
            transition: all 0.3s;
            touch-action: manipulation;
        }
        .fab:active { transform: scale(0.9); }
        .no-teams {
            text-align: center;
            padding: 40px 20px;
            opacity: 0.7;
        }
        .readonly-info {
            background: rgba(255,255,255,0.1);
            border-radius: 10px;
            padding: 15px;
            margin-bottom: 20px;
            text-align: center;
            font-size: 0.9rem;
            opacity: 0.8;
        }
        .mission-details {
            background: rgba(255,255,255,0.1);
            border-radius: 10px;
            padding: 15px;
            margin-bottom: 20px;
        }
        .mission-details h3 {
            margin-bottom: 10px;
            font-size: 1.1rem;
        }
        .mission-detail-row {
            display: flex;
            justify-content: space-between;
            margin: 5px 0;
            font-size: 0.9rem;
        }
        
        /* iOS Safari specific fixes */
        @supports (-webkit-appearance: none) {
            .container {
                padding-top: env(safe-area-inset-top, 20px);
            }
        }
        
        /* Loading animation */
        .loading-spinner {
            border: 3px solid rgba(255,255,255,0.3);
            border-radius: 50%;
            border-top: 3px solid white;
            width: 30px;
            height: 30px;
            animation: spin 1s linear infinite;
            margin: 20px auto;
        }
        @keyframes spin {
            0% { transform: rotate(0deg); }
            100% { transform: rotate(360deg); }
        }
    </style>
</head>
<body>
    <div class='connection-status' id='connectionStatus'>🔄 Verbinde...</div>
    
    <div class='container'>
        <div class='header'>
            <h1>🐕‍🦺 Einsatzüberwachung</h1>
            <p>Mobile Übersicht v1.6 (Read-Only)</p>
        </div>
        
        <div class='readonly-info'>
            <strong>📱 Mobile Ansicht</strong><br>
            Diese Ansicht dient nur zur Information. Timer-Steuerung erfolgt über die Desktop-Anwendung.
        </div>
        
        <div class='mission-details' id='missionDetails'>
            <h3>📋 Einsatz-Details</h3>
            <div class='mission-detail-row'>
                <span>📍 Einsatzort:</span>
                <span id='missionLocation'>-</span>
            </div>
            <div class='mission-detail-row'>
                <span>👤 Einsatzleiter:</span>
                <span id='missionLeader'>-</span>
            </div>
            <div class='mission-detail-row'>
                <span>⏱️ Einsatzdauer:</span>
                <span id='missionDuration'>-</span>
            </div>
        </div>
        
        <div class='status' id='missionStatus'>
            <h3>📊 Team-Status</h3>
            <div class='mission-detail-row'>
                <span><strong>Aktive Teams:</strong></span>
                <span id='activeTeams'>-</span>
            </div>
            <div class='mission-detail-row'>
                <span><strong>Gesamte Teams:</strong></span>
                <span id='totalTeams'>-</span>
            </div>
            <div class='mission-detail-row'>
                <span><strong>Letzte Aktualisierung:</strong></span>
                <span id='lastUpdate'>Jetzt</span>
            </div>
        </div>
        
        <div class='team-grid' id='teamGrid'>
            <div class='no-teams'>
                <div class='loading-spinner'></div>
                <h3>🔄 Lade Teams...</h3>
                <p>Verbindung zum Desktop wird hergestellt...</p>
            </div>
        </div>
    </div>
    
    <div class='quick-actions'>
        <button class='fab' onclick='refreshData()' title='Aktualisieren'>🔄</button>
    </div>

    <script>
        let teams = [];
        let missionInfo = {};
        let isOnline = false;
        
        function loadData() {
            // Load mission status
            fetch('/api/status')
                .then(response => response.json())
                .then(data => {
                    missionInfo = data.mission || {};
                    updateMissionDetails(data);
                    updateMissionStatus(data);
                    isOnline = true;
                    updateConnectionStatus();
                })
                .catch(error => {
                    console.error('Error loading status:', error);
                    isOnline = false;
                    updateConnectionStatus();
                });
            
            // Load teams
            fetch('/api/teams')
                .then(response => response.json())
                .then(data => {
                    teams = data;
                    renderTeams();
                    updateStatus();
                    isOnline = true;
                    updateConnectionStatus();
                })
                .catch(error => {
                    console.error('Error loading teams:', error);
                    isOnline = false;
                    updateConnectionStatus();
                    showNoTeamsMessage('Verbindungsfehler - Prüfen Sie die WLAN-Verbindung');
                });
        }
        
        function updateConnectionStatus() {
            const statusEl = document.getElementById('connectionStatus');
            if (isOnline) {
                statusEl.innerHTML = '🟢 Verbunden';
                statusEl.style.background = '#4CAF50';
            } else {
                statusEl.innerHTML = '🔴 Offline';
                statusEl.style.background = '#f44336';
            }
        }
        
        function updateMissionDetails(data) {
            if (data.mission) {
                document.getElementById('missionLocation').textContent = data.mission.location || 'Unbekannt';
                document.getElementById('missionLeader').textContent = data.mission.leader || 'Unbekannt';
                document.getElementById('missionDuration').textContent = data.mission.duration || '00:00:00';
            }
        }
        
        function updateMissionStatus(data) {
            if (data.mission) {
                document.getElementById('missionDuration').textContent = data.mission.duration || '00:00:00';
            }
        }
        
        function renderTeams() {
            const grid = document.getElementById('teamGrid');
            
            if (teams.length === 0) {
                showNoTeamsMessage('Keine Teams erstellt - Teams werden im Desktop hinzugefügt');
                return;
            }
            
            grid.innerHTML = teams.map(team => generateTeamCard(team)).join('');
        }
        
        function showNoTeamsMessage(message) {
            const grid = document.getElementById('teamGrid');
            grid.innerHTML = `<div class='no-teams'><h3>📋 ${message}</h3><p>Verwenden Sie die Desktop-Anwendung um Teams zu verwalten.</p></div>`;
        }
        
        function generateTeamCard(team) {
            const warningClass = team.isSecondWarning ? 'warning-2' : (team.isFirstWarning ? 'warning-1' : '');
            const warningText = team.isSecondWarning ? '🚨 Zweite Warnung erreicht!' : (team.isFirstWarning ? '⚠️ Erste Warnung erreicht' : '');
            
            // Generate notes HTML if available
            const notesHtml = generateNotesHtml(team);
            
            return `<div class='team-card' onclick='teamCardTap(${team.id})'>
                    <div class='team-header'>
                        <div>
                            <div class='team-name'>${team.name}</div>
                            <div class='team-info'>🐕 ${team.dogName || 'Unbekannt'}</div>
                            <div class='team-info'>👤 ${team.handler || 'Unbekannt'}</div>
                            ${team.helper ? `<div class='team-info'>🤝 ${team.helper}</div>` : ''}
                        </div>
                        <div class='team-status status-${team.status}'>
                            ${team.status === 'active' ? '🟢 Aktiv' : '🟡 Bereit'}
                        </div>
                    </div>
                    <div class='team-types'>
                        ${(team.types || [team.type || 'Unbekannt']).map(type => `<span class='type-badge'>${type}</span>`).join('')}
                    </div>
                    <div class='team-time ${warningClass}'>${team.time}</div>
                    ${warningText ? `<div class='warning-indicator ${warningClass}'>${warningText}</div>` : ''}
                    ${notesHtml}
                </div>`;
        }
        
        function generateNotesHtml(team) {
            if (!team.notes || team.notes.length === 0) {
                return `<div class='team-notes'>
                        <h4>📝 Notizen</h4>
                        <div class='notes-list'>
                            <div class='note-entry'>
                                <span class='note-content' style='opacity: 0.6; font-style: italic;'>Keine Notizen vorhanden</span>
                            </div>
                        </div>
                    </div>`;
            }
            
            const notesHtml = team.notes.map(note => 
                `<div class='note-entry'>
                    <span class='note-time'>${note.time}</span>
                    <span class='note-content'>${note.content}</span>
                </div>`
            ).join('');
            
            return `<div class='team-notes'>
                    <h4>📝 Notizen <small>(${team.notesCount || team.notes.length})</small></h4>
                    <div class='notes-list'>
                        ${notesHtml}
                    </div>
                </div>`;
        }
        
        function updateStatus() {
            const activeCount = teams.filter(t => t.status === 'active').length;
            const totalCount = teams.length;
            
            document.getElementById('activeTeams').textContent = activeCount;
            document.getElementById('totalTeams').textContent = totalCount;
            document.getElementById('lastUpdate').textContent = new Date().toLocaleTimeString();
        }
        
        function teamCardTap(teamId) {
            // Haptic feedback für iOS
            if (navigator.vibrate) {
                navigator.vibrate(50);
            }
            
            const team = teams.find(t => t.id === teamId);
            if (team) {
                // Kurze visuelle Bestätigung
                const cards = document.querySelectorAll('.team-card');
                cards[teams.indexOf(team)]?.classList.add('active');
                setTimeout(() => {
                    cards[teams.indexOf(team)]?.classList.remove('active');
                }, 200);
            }
        }
        
        function refreshData() {
            const refreshBtn = document.querySelector('.fab');
            refreshBtn.style.transform = 'rotate(360deg)';
            setTimeout(() => {
                refreshBtn.style.transform = '';
            }, 500);
            
            loadData();
        }
        
        // Auto-refresh every 10 seconds
        setInterval(loadData, 10000);
        
        // Initial load with delay to show loading animation
        setTimeout(loadData, 500);
        
        // App-like behavior
        document.addEventListener('visibilitychange', function() {
            if (!document.hidden) {
                setTimeout(loadData, 500);
            }
        });
        
        // Prevent zoom on double tap (iOS)
        let lastTouchEnd = 0;
        document.addEventListener('touchend', function (event) {
            var now = (new Date()).getTime();
            if (now - lastTouchEnd <= 300) {
                event.preventDefault();
            }
            lastTouchEnd = now;
        }, false);
        
        // Service Worker registration for offline capability (future enhancement)
        if ('serviceWorker' in navigator) {
            console.log('Service Worker support detected');
        }
    </script>
</body>
</html>";
        }
    }
}
