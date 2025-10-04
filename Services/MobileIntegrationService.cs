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
            _localIPAddress = GetLocalIPAddress();
            _baseUrl = $"http://{_localIPAddress}:{_port}";
            
            // Führe Netzwerk- und Firewall-Checks durch
            CheckFirewallAndNetwork();
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
                }
                
                StatusChanged?.Invoke("✅ Initiale System-Checks abgeschlossen");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ Netzwerk-Check Fehler: {ex.Message}");
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
                
                bool isAdmin = IsRunningAsAdministrator();
                StatusChanged?.Invoke($"🔐 Administrator-Rechte: {(isAdmin ? "✅ Verfügbar" : "❌ Nicht verfügbar")}");
                
                // Netzwerk-Konfiguration
                if (isAdmin)
                {
                    StatusChanged?.Invoke("🔧 Führe automatische Admin-Konfiguration durch...");
                    await ConfigureNetworkAccess();
                }
                else
                {
                    StatusChanged?.Invoke("🔄 Verwende Nicht-Admin-Strategien für Netzwerk-Zugriff...");
                }
                
                // HttpListener Setup
                _httpListener?.Close();
                _httpListener = new HttpListener();
                
                // Prefixes konfigurieren
                bool networkAccessConfigured = await ConfigurePrefixes();
                StatusChanged?.Invoke($"📋 Konfigurierte Prefixes: {_httpListener.Prefixes.Count}");
                
                if (_httpListener.Prefixes.Count == 0)
                {
                    StatusChanged?.Invoke("❌ Keine gültigen Prefixes konfiguriert");
                    return false;
                }
                
                // HttpListener starten
                StatusChanged?.Invoke("🚀 Starte HttpListener...");
                _httpListener.Start();
                _isRunning = true;
                
                StatusChanged?.Invoke("✅ HttpListener erfolgreich gestartet");
                
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
                    StatusChanged?.Invoke($"⚠️ Mobile Server gestartet (EINGESCHRÄNKT - NUR LOCALHOST)");
                    StatusChanged?.Invoke($"💻 Desktop URL: http://localhost:{_port}/mobile");
                    StatusChanged?.Invoke("💡 FÜR IPHONE-ZUGRIFF: Als Administrator starten");
                }
                
                // Request Handler starten
                StatusChanged?.Invoke("🔄 Starte Request-Handler...");
                _ = Task.Run(HandleRequestsAsync);
                
                StatusChanged?.Invoke("🎉 Mobile Server vollständig einsatzbereit!");
                return true;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Server-Start-Fehler: {ex.Message}");
                return false;
            }
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
                    }
                }
                
                // Gib dem Request-Handler Zeit zum Beenden
                await Task.Delay(500);
                
                StatusChanged?.Invoke("✅ Mobile Server gestoppt");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ Server-Stop Fehler: {ex.Message}");
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
                
                var userName = Environment.UserName;
                var addCmd = $"netsh http add urlacl url=http://+:{_port}/ user={userName}";
                var addResult = await RunCommand(addCmd);
                
                if (addResult.Contains("erfolgreich") || addResult.Contains("successfully"))
                {
                    StatusChanged?.Invoke("✅ URL-Reservierung erfolgreich hinzugefügt");
                }
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ URL-Reservierung Fehler: {ex.Message}");
            }
        }
        
        private async Task ConfigureFirewall()
        {
            try
            {
                StatusChanged?.Invoke("🛡️ Konfiguriere Windows Firewall...");
                
                var checkCmd = "netsh advfirewall firewall show rule name=\"Einsatzüberwachung Mobile\"";
                var checkResult = await RunCommand(checkCmd);
                
                if (checkResult.Contains("Einsatzüberwachung Mobile"))
                {
                    StatusChanged?.Invoke("✅ Firewall-Regel bereits vorhanden");
                    return;
                }
                
                var addCmd = $"netsh advfirewall firewall add rule name=\"Einsatzüberwachung Mobile\" dir=in action=allow protocol=TCP localport={_port}";
                var addResult = await RunCommand(addCmd);
                
                if (addResult.Contains("Ok") || addResult.Contains("erfolgreich"))
                {
                    StatusChanged?.Invoke("✅ Firewall-Regel erfolgreich hinzugefügt");
                }
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ Firewall-Konfiguration Fehler: {ex.Message}");
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
                    CreateNoWindow = true
                };
                
                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    
                    return !string.IsNullOrEmpty(output) ? output : error;
                }
                
                return "Prozess konnte nicht gestartet werden";
            }
            catch (Exception ex)
            {
                return $"Fehler: {ex.Message}";
            }
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
                
                var urlCmd = $"netsh http delete urlacl url=http://+:{_port}/";
                await RunCommand(urlCmd);
                
                StatusChanged?.Invoke("✅ Cleanup abgeschlossen");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"⚠️ Cleanup Fehler: {ex.Message}");
            }
        }
        
        private async Task<bool> ConfigurePrefixes()
        {
            bool networkAccessConfigured = false;
            
            // Strategie 1: Spezifische IP-Adresse
            if (_localIPAddress != "localhost" && !string.IsNullOrEmpty(_localIPAddress))
            {
                try
                {
                    _httpListener!.Prefixes.Add($"http://{_localIPAddress}:{_port}/");
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
                    _httpListener!.Prefixes.Add($"http://+:{_port}/");
                    StatusChanged?.Invoke($"✅ Wildcard-Prefix hinzugefügt: +:{_port}");
                    networkAccessConfigured = true;
                }
                catch (Exception ex)
                {
                    StatusChanged?.Invoke($"⚠️ Wildcard-Prefix Fehler: {ex.Message}");
                }
            }
            
            // Strategie 3: Localhost-Fallback (immer möglich)
            try
            {
                _httpListener!.Prefixes.Add($"http://localhost:{_port}/");
                _httpListener.Prefixes.Add($"http://127.0.0.1:{_port}/");
                StatusChanged?.Invoke($"✅ Localhost-Prefixes hinzugefügt");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"❌ Localhost-Prefix Fehler: {ex.Message}");
            }
            
            return networkAccessConfigured;
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
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
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
                    
                    // NEU: Endpoint für globale Notizen
                    case "/api/globalnotes":
                        await ServeJSON(response, GetGlobalNotesData());
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
                timestamp = n.FormattedTimestamp,
                content = n.Content,
                teamName = n.TeamName ?? "",
                icon = n.EntryTypeIcon,
                type = n.EntryType.ToString()
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
            background: linear-gradient(135deg, #FF8A65 0%, #FF5722 100%);
            min-height: 100vh;
            color: white;
        }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { text-align: center; margin-bottom: 30px; }
        .team-card {
            background: rgba(255,255,255,0.15);
            border-radius: 15px;
            padding: 20px;
            margin-bottom: 15px;
        }
        .team-time { font-size: 2.5rem; font-weight: bold; text-align: center; margin: 15px 0; }
        .status { background: rgba(255,255,255,0.1); border-radius: 15px; padding: 20px; margin-bottom: 20px; }
        
        /* NEU: Notizen-Styling */
        .notes-section { 
            background: rgba(255,255,255,0.1); 
            border-radius: 15px; 
            padding: 20px; 
            margin-top: 20px;
            max-height: 400px;
            overflow-y: auto;
        }
        .notes-header {
            font-size: 1.2rem;
            font-weight: bold;
            margin-bottom: 15px;
            padding-bottom: 10px;
            border-bottom: 2px solid rgba(255,255,255,0.3);
        }
        .note-entry {
            background: rgba(255,255,255,0.1);
            border-left: 3px solid rgba(255,255,255,0.5);
            padding: 10px;
            margin-bottom: 10px;
            border-radius: 5px;
        }
        .note-timestamp {
            font-weight: bold;
            opacity: 0.8;
            font-size: 0.9rem;
        }
        .note-content {
            margin-top: 5px;
            line-height: 1.4;
        }
        .note-team {
            font-size: 0.85rem;
            opacity: 0.7;
            margin-top: 3px;
        }
        .no-notes {
            text-align: center;
            opacity: 0.7;
            font-style: italic;
            padding: 20px;
        }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🐕‍🦺 Einsatzüberwachung</h1>
            <p>Mobile Übersicht v1.7</p>
        </div>
        <div class='status'>
            <h3>📊 Status</h3>
            <p id='status'>Lade...</p>
        </div>
        <div id='teamGrid'></div>
        
        <!-- NEU: Notizen-Bereich -->
        <div class='notes-section'>
            <div class='notes-header'>📝 Notizen & Ereignisse</div>
            <div id='notesContainer'></div>
        </div>
    </div>
    <script>
        function loadData() {
            // Teams laden
            fetch('/api/teams')
                .then(r => r.json())
                .then(teams => {
                    const grid = document.getElementById('teamGrid');
                    if (teams.length === 0) {
                        grid.innerHTML = '<p style=""text-align:center"">Keine Teams vorhanden</p>';
                        return;
                    }
                    grid.innerHTML = teams.map(t => `
                        <div class='team-card'>
                            <h3>${t.name}</h3>
                            <div class='team-time'>${t.time}</div>
                            <p>🐕 ${t.dogName || '-'}</p>
                            <p>👤 ${t.handler || '-'}</p>
                            ${t.suchgebiet ? `<p>🗺️ ${t.suchgebiet}</p>` : ''}
                        </div>
                    `).join('');
                });
            
            // Status laden
            fetch('/api/status')
                .then(r => r.json())
                .then(data => {
                    document.getElementById('status').innerHTML = 
                        `Aktive Teams: ${data.teams.active} / ${data.teams.total}`;
                });
            
            // NEU: Notizen laden
            fetch('/api/globalnotes')
                .then(r => r.json())
                .then(notes => {
                    const container = document.getElementById('notesContainer');
                    if (notes.length === 0) {
                        container.innerHTML = '<div class=""no-notes"">Noch keine Notizen vorhanden</div>';
                        return;
                    }
                    // Zeige die letzten 20 Notizen (neueste zuerst)
                    const recentNotes = notes.slice(-20).reverse();
                    container.innerHTML = recentNotes.map(note => `
                        <div class='note-entry'>
                            <div class='note-timestamp'>${note.icon} ${note.timestamp}</div>
                            <div class='note-content'>${note.content}</div>
                            ${note.teamName ? `<div class='note-team'>Team: ${note.teamName}</div>` : ''}
                        </div>
                    `).join('');
                })
                .catch(err => {
                    console.error('Fehler beim Laden der Notizen:', err);
                    document.getElementById('notesContainer').innerHTML = 
                        '<div class=""no-notes"">Fehler beim Laden der Notizen</div>';
                });
        }
        
        // Initial laden
        loadData();
        
        // Alle 5 Sekunden aktualisieren
        setInterval(loadData, 10000);
    </script>
</body>
</html>";
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
