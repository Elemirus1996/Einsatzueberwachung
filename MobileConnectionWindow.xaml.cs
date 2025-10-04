using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Controls;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading.Tasks;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung
{
    public partial class MobileConnectionWindow : Window
    {
        private MobileIntegrationService? _mobileService;
        private int _totalViews = 0;
        private int _activeViewers = 0;
        private bool _allowClose = false;

        public MobileConnectionWindow()
        {
            InitializeComponent();
            UpdateUI();
        }

        public void SetMobileService(MobileIntegrationService mobileService)
        {
            _mobileService = mobileService;
            
            // Event-Handler registrieren
            _mobileService.StatusChanged += OnStatusChanged;
            // CommandReceived Event entfernt - Read-only Interface
            
            UpdateUI();
        }

        private void UpdateQRCode()
        {
            if (_mobileService?.IsRunning == true)
            {
                try
                {
                    var qrCodeBytes = _mobileService.GenerateQRCode();
                    if (qrCodeBytes.Length > 0)
                    {
                        using var stream = new MemoryStream(qrCodeBytes);
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        
                        QRCodeImage.Source = bitmap;
                    }
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"QR-Code Fehler: {ex.Message}";
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Wenn Server läuft, frage den Benutzer was passieren soll
            if (_mobileService?.IsRunning == true && !_allowClose)
            {
                var result = MessageBox.Show(
                    "Der Mobile Server ist noch aktiv.\n\n" +
                    "Möchten Sie:\n" +
                    "• JA: Server weiterlaufen lassen (Fenster minimieren)\n" +
                    "• NEIN: Server stoppen und Fenster schließen\n" +
                    "• ABBRECHEN: Fenster offen lassen",
                    "Mobile Server aktiv", 
                    MessageBoxButton.YesNoCancel, 
                    MessageBoxImage.Question);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        // Fenster minimieren statt schließen
                        this.WindowState = WindowState.Minimized;
                        e.Cancel = true; // Schließen verhindern
                        
                        // Tray-Benachrichtigung simulieren
                        ShowTrayNotification();
                        return;
                        
                    case MessageBoxResult.No:
                        // Server stoppen und normal schließen
                        _mobileService?.StopServer();
                        _allowClose = true;
                        break;
                        
                    case MessageBoxResult.Cancel:
                        // Schließen abbrechen
                        e.Cancel = true;
                        return;
                }
            }
            
            base.OnClosing(e);
        }

        private void ShowTrayNotification()
        {
            // Temporäre Benachrichtigung für 3 Sekunden
            var originalTitle = this.Title;
            this.Title = "📱 Mobile Server läuft im Hintergrund - Klicken zum Öffnen";
            
            // Nach 3 Sekunden Titel zurücksetzen
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, e) =>
            {
                this.Title = originalTitle;
                timer.Stop();
            };
            timer.Start();
        }

        private void OnStatusChanged(string status)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = status;
                
                var networkInfo = _mobileService?.LocalIPAddress != "localhost" 
                    ? $"Netzwerk-IP: {_mobileService?.LocalIPAddress}\n" 
                    : "Nur localhost (Admin-Rechte für Netzwerk erforderlich)\n";
                
                ServerInfo.Text = $"Status: {status}\n{networkInfo}Port: 8080 (Read-Only)\nZeit: {DateTime.Now:HH:mm:ss}";
                
                if (status.Contains("gestartet"))
                {
                    // UPDATED: Use design system color
                    StatusIndicator.Background = (Brush)FindResource("Success");
                    StartServerButton.IsEnabled = false;
                    StopServerButton.IsEnabled = true;
                    QRCodeContainer.Visibility = Visibility.Visible;
                    
                    var url = _mobileService?.QRCodeUrl ?? "";
                    QRCodeUrl.Text = url;
                    
                    // QR-Code generieren und anzeigen
                    UpdateQRCode();
                    
                    ConnectedDevicesText.Text = "Warte auf Mobile Geräte...";
                    _activeViewers = 1; // Simuliert
                    
                    // Title-Update für bessere Sichtbarkeit
                    this.Title = "📱 Mobile Verbindung - READ-ONLY SERVER AKTIV";
                }
                else if (status.Contains("gestoppt"))
                {
                    // UPDATED: Use design system color
                    StatusIndicator.Background = (Brush)FindResource("Error");
                    StartServerButton.IsEnabled = true;
                    StopServerButton.IsEnabled = false;
                    QRCodeContainer.Visibility = Visibility.Collapsed;
                    ConnectedDevicesText.Text = "Keine Geräte verbunden";
                    _activeViewers = 0;
                    
                    this.Title = "📱 Mobile Verbindung - Getrennt";
                }
                else if (status.Contains("Fehler") || status.Contains("fehlgeschlagen"))
                {
                    // UPDATED: Use design system color
                    StatusIndicator.Background = (Brush)FindResource("Warning");
                    
                    // Zeige Hilfe-Informationen bei Fehlern
                    if (status.Contains("Administrator"))
                    {
                        MessageBox.Show(
                            "💡 Tipp für bessere Netzwerk-Kompatibilität:\n\n" +
                            "Starten Sie die Anwendung als Administrator um:\n" +
                            "• iPhone/Android-Zugriff über WLAN zu ermöglichen\n" +
                            "• Alle Netzwerk-Interfaces zu nutzen\n\n" +
                            "Aktuell: Nur localhost-Zugriff möglich",
                            "Netzwerk-Information", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Information);
                    }
                }
                
                UpdateStatistics();
            });
        }

        private void FlashTaskbar()
        {
            // Taskbar-Icon blinken lassen bei neuen Commands
            var originalTitle = this.Title;
            this.Title = $"📱 Neuer Befehl empfangen! - {originalTitle}";
            
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += (s, e) =>
            {
                this.Title = originalTitle;
                timer.Stop();
            };
            timer.Start();
        }

        private void UpdateUI()
        {
            if (_mobileService?.IsRunning == true)
            {
                StatusText.Text = "Verbunden (Read-Only)";
                // UPDATED: Use design system color
                StatusIndicator.Background = (Brush)FindResource("Success");
                StartServerButton.IsEnabled = false;
                StopServerButton.IsEnabled = true;
                QRCodeContainer.Visibility = Visibility.Visible;
                QRCodeUrl.Text = _mobileService.QRCodeUrl;
                UpdateQRCode();
                this.Title = "📱 Mobile Verbindung - READ-ONLY SERVER AKTIV";
            }
            else
            {
                StatusText.Text = "Getrennt";
                // UPDATED: Use design system color
                StatusIndicator.Background = (Brush)FindResource("Error");
                StartServerButton.IsEnabled = true;
                StopServerButton.IsEnabled = false;
                QRCodeContainer.Visibility = Visibility.Collapsed;
                this.Title = "📱 Mobile Verbindung - Getrennt";
            }
            
            UpdateStatistics();
        }

        private void UpdateStatistics()
        {
            TotalViewsValue.Text = _totalViews.ToString();
            ActiveViewersValue.Text = _activeViewers.ToString();
        }

        private async void StartServer_Click(object sender, RoutedEventArgs e)
        {
            if (_mobileService == null)
            {
                MessageBox.Show("Mobile Service nicht verfügbar!", "Fehler", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Disable button während Start-Prozess
            StartServerButton.IsEnabled = false;
            StartServerButton.Content = "🔄 Startet...";

            try
            {
                // Zeige erweiterte Start-Optionen
                var startOptions = MessageBox.Show(
                    "🚀 MOBILE SERVER START\n\n" +
                    "Wählen Sie den Start-Modus:\n\n" +
                    "• JA: Normal-Start mit automatischer Diagnose\n" +
                    "• NEIN: Erweiterte Diagnose vor Start\n" +
                    "• ABBRECHEN: Abbruch\n\n" +
                    "💡 Bei ersten Problemen wählen Sie NEIN für detaillierte Analyse.",
                    "Server-Start-Modus",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                switch (startOptions)
                {
                    case MessageBoxResult.Cancel:
                        return;
                        
                    case MessageBoxResult.No:
                        // Erweiterte Diagnose
                        OnStatusChanged("🔍 Führe erweiterte Vor-Start-Diagnose durch...");
                        await PerformPreStartDiagnosis();
                        break;
                        
                    case MessageBoxResult.Yes:
                        // Normaler Start
                        OnStatusChanged("🚀 Starte im Normal-Modus...");
                        break;
                }

                // Server starten
                var success = await _mobileService.StartServerAsync();
                
                if (success)
                {
                    ShowSuccessMessage();
                }
                else
                {
                    ShowFailureHelp();
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged($"🚨 Kritischer Start-Fehler: {ex.Message}");
                ShowCriticalErrorHelp(ex);
            }
            finally
            {
                // Button wieder aktivieren
                StartServerButton.IsEnabled = true;
                StartServerButton.Content = "Server starten";
            }
        }
        
        private async Task PerformPreStartDiagnosis()
        {
            try
            {
                OnStatusChanged("🔍 ERWEITERTE VOR-START-DIAGNOSE");
                OnStatusChanged("================================");
                
                // Administrator-Check
                var isAdmin = IsRunningAsAdministrator();
                OnStatusChanged($"🔐 Administrator-Rechte: {(isAdmin ? "✅ Verfügbar" : "❌ Nicht verfügbar")}");
                
                if (!isAdmin)
                {
                    OnStatusChanged("💡 Empfehlung: Als Administrator starten für optimale Ergebnisse");
                }
                
                // Port-Check
                OnStatusChanged("🔌 Prüfe Port-Verfügbarkeit...");
                await CheckPortAvailability();
                
                // Firewall-Check
                OnStatusChanged("🛡️ Prüfe Firewall-Status...");
                await CheckFirewallStatus();
                
                // Netzwerk-Check
                OnStatusChanged("🌐 Prüfe Netzwerk-Interfaces...");
                CheckNetworkInterfaces();
                
                OnStatusChanged("✅ Vor-Start-Diagnose abgeschlossen");
                OnStatusChanged("🚀 Bereit für Server-Start...");
                
                await Task.Delay(1000); // Kurze Pause für Benutzer zum Lesen
            }
            catch (Exception ex)
            {
                OnStatusChanged($"⚠️ Diagnose-Fehler: {ex.Message}");
            }
        }
        
        private async Task CheckPortAvailability()
        {
            try
            {
                var tcpConnections = System.Net.NetworkInformation.IPGlobalProperties
                    .GetIPGlobalProperties()
                    .GetActiveTcpListeners();
                
                var port8080InUse = tcpConnections.Any(endpoint => endpoint.Port == 8080);
                
                if (port8080InUse)
                {
                    OnStatusChanged("⚠️ Port 8080 ist bereits belegt");
                    
                    // Versuche den Prozess zu identifizieren
                    try
                    {
                        var processInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "netstat",
                            Arguments = "-ano | findstr :8080",
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
                                OnStatusChanged($"📋 Port-Details: {output.Trim()}");
                            }
                        }
                    }
                    catch
                    {
                        OnStatusChanged("⚠️ Port-Details konnten nicht ermittelt werden");
                    }
                    
                    OnStatusChanged("💡 Alternative Ports (8081-8083) werden automatisch getestet");
                }
                else
                {
                    OnStatusChanged("✅ Port 8080 ist verfügbar");
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged($"⚠️ Port-Check Fehler: {ex.Message}");
            }
        }
        
        private async Task CheckFirewallStatus()
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
                        OnStatusChanged("✅ Windows Firewall ist deaktiviert (keine Blockierung)");
                    }
                    else if (output.Contains("State                                 ON"))
                    {
                        OnStatusChanged("⚠️ Windows Firewall ist aktiv");
                        OnStatusChanged("💡 Port-Freigabe eventuell erforderlich - verwenden Sie 'Netzwerk konfigurieren'");
                    }
                    else
                    {
                        OnStatusChanged("❓ Firewall-Status unbekannt");
                    }
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged($"⚠️ Firewall-Check Fehler: {ex.Message}");
            }
        }
        
        private void CheckNetworkInterfaces()
        {
            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                    .ToList();
                
                OnStatusChanged($"🌐 Aktive Netzwerk-Interfaces: {networkInterfaces.Count}");
                
                foreach (var ni in networkInterfaces.Take(3)) // Nur erste 3 anzeigen
                {
                    var ipProps = ni.GetIPProperties();
                    var ipv4 = ipProps.UnicastAddresses
                        .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
                        
                    if (ipv4 != null)
                    {
                        OnStatusChanged($"   • {ni.Name}: {ipv4.Address} ({ni.NetworkInterfaceType})");
                    }
                }
                
                if (networkInterfaces.Count == 0)
                {
                    OnStatusChanged("⚠️ Keine aktiven Netzwerk-Interfaces - Netzwerk-Verbindung prüfen");
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged($"⚠️ Netzwerk-Check Fehler: {ex.Message}");
            }
        }
        
        private void ShowSuccessMessage()
        {
            var successMessage = @"🎉 MOBILE SERVER ERFOLGREICH GESTARTET!

✅ STATUS: Server läuft und ist bereit
📱 NÄCHSTE SCHRITTE:

1️⃣ QR-CODE SCANNEN:
   • Öffnen Sie die Kamera-App auf Ihrem iPhone
   • Scannen Sie den QR-Code in diesem Fenster
   • Tippen Sie auf den Link

2️⃣ ODER MANUELLE URL-EINGABE:
   • Öffnen Sie Safari auf dem iPhone
   • Geben Sie die angezeigte URL ein
   • Achten Sie auf http:// (nicht https://)

3️⃣ TESTEN:
   • Verwenden Sie 'API Test' für Funktionsprüfung
   • 'Debug-Seite' für detaillierte Informationen

💡 TIPPS:
• Beide Geräte müssen im gleichen WLAN sein
• Bei Problemen: Browser-Cache löschen
• Funktioniert nur mit http:// (nicht https://)

🎯 Der Mobile Server ist jetzt einsatzbereit!";

            MessageBox.Show(successMessage, "Server erfolgreich gestartet", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void ShowFailureHelp()
        {
            var failureMessage = @"❌ SERVER-START FEHLGESCHLAGEN

🔧 SOFORTMASSNAHMEN:

1️⃣ ALS ADMINISTRATOR STARTEN:
   • Rechtsklick auf Einsatzüberwachung.exe
   • 'Als Administrator ausführen' wählen
   • UAC-Dialog mit 'Ja' bestätigen

2️⃣ PORT-PROBLEME LÖSEN:
   • Andere Apps schließen die Port 8080 verwenden
   • Besonders: Skype, IIS, Apache, Jenkins
   • Computer neu starten hilft oft

3️⃣ FIREWALL KONFIGURIEREN:
   • Verwenden Sie 'Netzwerk konfigurieren' Button
   • Oder: Windows-Einstellungen → Firewall
   • App durch Firewall zulassen

4️⃣ ALTERNATIVE METHODEN:
   • 'Ohne Admin-Rechte' Button verwenden
   • Windows Mobile Hotspot aktivieren
   • iPhone als Hotspot nutzen

5️⃣ ERWEITERTE DIAGNOSE:
   • 'System-Diagnose' für detaillierte Analyse
   • Prüfen Sie die Logs im rechten Bereich

🆘 NOTFALL-LÖSUNG:
Computer neu starten und als Administrator erneut versuchen.

💡 Die meisten Probleme lösen sich durch Admin-Rechte oder Neustart!";

            MessageBox.Show(failureMessage, "Server-Start fehlgeschlagen - Hilfe", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        
        private void ShowCriticalErrorHelp(Exception ex)
        {
            var criticalMessage = $@"🚨 KRITISCHER SERVER-FEHLER

❌ FEHLER-TYP: {ex.GetType().Name}
❌ NACHRICHT: {ex.Message}

🔧 SOFORT-LÖSUNGEN:

1️⃣ SYSTEM-NEUSTART:
   • Computer vollständig neu starten
   • Oft löst dies grundlegende Probleme

2️⃣ ADMINISTRATOR-MODUS:
   • Rechtsklick auf .exe → 'Als Administrator ausführen'
   • Dies löst die meisten Berechtigungsprobleme

3️⃣ SICHERHEITS-SOFTWARE:
   • Antivirus temporär deaktivieren (Test)
   • Windows Defender Echtzeit-Schutz pausieren
   • Firewall temporär deaktivieren (Test)

4️⃣ WINDOWS-REPARATUR:
   • Windows-Einstellungen → Update & Sicherheit
   • Problembehandlung → Windows Update
   • System-Datei-Überprüfung: sfc /scannow

5️⃣ .NET RUNTIME:
   • .NET 8 Runtime neu installieren
   • Von Microsoft.com herunterladen

📞 SUPPORT-SAMMLUNG:
• Screenshot dieser Meldung
• Windows-Version notieren
• Netzwerk-Konfiguration dokumentieren

🆘 Bei anhaltenden Problemen:
Verwenden Sie 'System-Diagnose' für vollständige Analyse.";

            MessageBox.Show(criticalMessage, "Kritischer Fehler - Erweiterte Hilfe", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void StopServer_Click(object sender, RoutedEventArgs e)
        {
            if (_mobileService?.IsRunning == true)
            {
                var result = MessageBox.Show(
                    "Mobile Server wirklich stoppen?\n\n" +
                    "Alle verbundenen Smartphones verlieren die Verbindung.",
                    "Server stoppen", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _mobileService.StopServer();
                }
            }
        }

        private void CopyUrl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var url = _mobileService?.QRCodeUrl ?? "";
                Clipboard.SetText(url);
                
                var networkTip = _mobileService?.LocalIPAddress != "localhost" 
                    ? "\n\n✅ Diese URL funktioniert auch auf anderen Geräten im gleichen WLAN!" 
                    : "\n\n💡 Starten Sie als Administrator für Netzwerk-Zugriff auf andere Geräte.";
                
                MessageBox.Show($"URL in die Zwischenablage kopiert:\n{url}{networkTip}",
                               "URL kopiert", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Kopieren: {ex.Message}", "Fehler", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            if (_mobileService?.IsRunning == true)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _mobileService.QRCodeUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Öffnen der URL: {ex.Message}\n\n" +
                                   $"Versuchen Sie, die URL manuell zu öffnen:\n{_mobileService.QRCodeUrl}", 
                                   "Browser-Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Server muss zuerst gestartet werden!", "Server nicht aktiv", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void TestAPI_Click(object sender, RoutedEventArgs e)
        {
            if (_mobileService?.IsRunning != true)
            {
                MessageBox.Show("Server muss zuerst gestartet werden!", "Server nicht aktiv", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var testUrl = $"http://localhost:{8080}/test";
            
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                
                var response = await client.GetAsync(testUrl);
                var content = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"✅ API Test erfolgreich!\n\n" +
                                   $"Status: {response.StatusCode}\n" +
                                   $"URL: {testUrl}\n" +
                                   $"Response: {content.Substring(0, Math.Min(200, content.Length))}...",
                                   "API Test OK", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"❌ API Test fehlgeschlagen!\n\n" +
                                   $"Status: {response.StatusCode}\n" +
                                   $"URL: {testUrl}\n" +
                                   $"Response: {content}",
                                   "API Test Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                MessageBox.Show($"🌐 Netzwerk-Fehler beim API Test:\n\n" +
                               $"URL: {testUrl}\n" +
                               $"Fehler: {ex.Message}\n\n" +
                               $"Mögliche Ursachen:\n" +
                               $"• Server nicht erreichbar\n" +
                               $"• Firewall blockiert Port 8080\n" +
                               $"• Falsche URL-Konfiguration",
                               "API Test - Netzwerk-Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (TaskCanceledException)
            {
                MessageBox.Show($"⏱️ API Test Timeout:\n\n" +
                               $"URL: {testUrl}\n" +
                               $"Der Server antwortet nicht innerhalb von 10 Sekunden.\n\n" +
                               $"Prüfen Sie die Server-Logs im rechten Bereich.",
                               "API Test - Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"🚨 Unerwarteter Fehler beim API Test:\n\n" +
                               $"URL: {testUrl}\n" +
                               $"Fehler: {ex.GetType().Name}\n" +
                               $"Message: {ex.Message}",
                               "API Test - Unbekannter Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenInBrowser_Click(object sender, RoutedEventArgs e)
        {
            TestConnection_Click(sender, e);
        }

        private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
            ShowTrayNotification();
        }

        // Neue Methode zum programmatischen Schließen (z.B. beim App-Shutdown)
        public void ForceClose()
        {
            _allowClose = true;
            _mobileService?.StopServer();
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Nur stoppen wenn explizit gewünscht
            if (_allowClose)
            {
                _mobileService?.StopServer();
            }
            base.OnClosed(e);
        }

        private void AddNetshButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mobileService?.IsRunning != true)
            {
                MessageBox.Show("Server muss zuerst gestartet werden!", "Server nicht aktiv", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ip = _mobileService.LocalIPAddress;
            if (ip == "localhost")
            {
                MessageBox.Show("Keine Netzwerk-IP verfügbar.\n\nStarten Sie die App als Administrator für Netzwerk-Zugriff.", 
                               "Netzwerk-Konfiguration", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var netshCommand = $"netsh http add urlacl url=http://{ip}:8080/ user=Everyone";
            var firewallCommand = $"netsh advfirewall firewall add rule name=\"Einsatzüberwachung Mobile\" dir=in action=allow protocol=TCP localport=8080";

            var instructions = $@"🔧 Windows Netzwerk-Konfiguration

Führen Sie diese Befehle in einer Administrator-Eingabeaufforderung aus:

1️⃣ URL-Berechtigung hinzufügen:
{netshCommand}

2️⃣ Firewall-Regel hinzufügen:
{firewallCommand}

💡 Anleitung:
• Windows-Taste + X → 'Windows PowerShell (Administrator)'
• Befehle einzeln eingeben und Enter drücken
• App neu starten

Diese Befehle wurden in die Zwischenablage kopiert.";

            try
            {
                Clipboard.SetText($"{netshCommand}\n{firewallCommand}");
                MessageBox.Show(instructions, "Netzwerk-Konfiguration", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Kopieren: {ex.Message}\n\n{instructions}", 
                               "Netzwerk-Konfiguration", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void CleanupNetwork_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "🧹 Netzwerk-Konfiguration bereinigen?\n\n" +
                "Dies entfernt:\n" +
                "• HTTP URL Reservierungen\n" +
                "• Firewall-Regeln für Port 8080\n\n" +
                "⚠️ Administrator-Rechte erforderlich!\n\n" +
                "Fortfahren?",
                "Netzwerk-Cleanup", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            if (_mobileService?.IsRunning == true)
            {
                MessageBox.Show("⚠️ Stoppen Sie zuerst den Mobile Server!", 
                               "Server aktiv", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await _mobileService!.CleanupNetworkConfiguration();
                
                MessageBox.Show(
                    "✅ Netzwerk-Konfiguration erfolgreich bereinigt!\n\n" +
                    "🔄 Starten Sie die App neu für eine saubere Konfiguration.",
                    "Cleanup erfolgreich", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Cleanup-Fehler:\n\n{ex.Message}\n\n" +
                    "Stellen Sie sicher, dass die App als Administrator gestartet wurde.",
                    "Cleanup Fehler", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private void NonAdminHelp_Click(object sender, RoutedEventArgs e)
        {
            var helpText = @"🚀 iPhone-Zugriff OHNE Administrator-Rechte

📱 EINFACHSTE LÖSUNG - Windows Mobile Hotspot:

1️⃣ Windows Hotspot aktivieren:
   • Windows-Einstellungen (Win+I)
   • Netzwerk und Internet → Mobiler Hotspot
   • 'Meinen Internetanschluss freigeben' aktivieren
   • WLAN-Name und Passwort notieren

2️⃣ iPhone verbinden:
   • iPhone WLAN-Einstellungen
   • Windows Hotspot-Name wählen
   • Passwort eingeben
   
3️⃣ Mobile App öffnen:
   • URL: http://192.168.137.1:8080/mobile
   • Oder QR-Code scannen

🔧 ALTERNATIVE LÖSUNGEN:

📛 Windows Firewall (über GUI):
   • Systemsteuerung → Windows Defender Firewall
   • 'Eine App durch die Firewall zulassen'
   • Einsatzüberwachung hinzufügen ✅

🌐 Netzwerk-Profil ändern:
   • Windows-Einstellungen → Netzwerk
   • Aktuelles WLAN → Eigenschaften
   • Netzwerkprofil: 'Privat' auswählen

📱 iPhone als Hotspot:
   • iPhone: Einstellungen → Persönlicher Hotspot
   • Desktop mit iPhone-Hotspot verbinden
   • URL: http://172.20.10.1:8080/mobile

🔧 Router-Einstellungen:
   • Router-Webinterface öffnen
   • WLAN → Erweiterte Einstellungen
   • Client-Isolation deaktivieren

💡 ERFOLGS-CHECKS:
   ✅ Desktop-Test: http://localhost:8080/mobile
   ✅ Debug-Seite: http://localhost:8080/debug
   ✅ 'API Test' Button verwenden
   ✅ Beide Geräte im gleichen Netzwerk

⚠️ WICHTIG:
Starten Sie als Administrator für automatische Konfiguration!
Dies ist nur für Fälle gedacht, wo Admin-Rechte nicht verfügbar sind.";

            MessageBox.Show(helpText, "iPhone-Zugriff ohne Admin-Rechte", 
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void SystemDiagnose_Click(object sender, RoutedEventArgs e)
        {
            var diagnoseWindow = new Window
            {
                Title = "🔧 System-Diagnose - Einsatzüberwachung Mobile",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(10)
            };

            var textBlock = new TextBlock
            {
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Background = Brushes.Black,
                Foreground = Brushes.LightGreen,
                Padding = new Thickness(10),
                Text = "🔍 Starte System-Diagnose...\n\n"
            };

            scrollViewer.Content = textBlock;
            diagnoseWindow.Content = scrollViewer;

            diagnoseWindow.Show();

            // Diagnose-Schritte
            var diagnostics = new List<string>();

            try
            {
                AppendDiagnostic(textBlock, "🖥️ SYSTEM-INFORMATIONEN:");
                AppendDiagnostic(textBlock, $"   • Windows: {Environment.OSVersion.VersionString}");
                AppendDiagnostic(textBlock, $"   • .NET Runtime: {Environment.Version}");
                AppendDiagnostic(textBlock, $"   • Prozessor-Architektur: {Environment.OSVersion.Platform}");
                AppendDiagnostic(textBlock, $"   • 64-Bit System: {Environment.Is64BitOperatingSystem}");
                AppendDiagnostic(textBlock, $"   • Administrator: {IsRunningAsAdministrator()}");
                AppendDiagnostic(textBlock, "");

                AppendDiagnostic(textBlock, "🌐 NETZWERK-INTERFACES:");
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var ni in networkInterfaces.Where(n => n.OperationalStatus == OperationalStatus.Up))
                {
                    AppendDiagnostic(textBlock, $"   • {ni.Name}: {ni.OperationalStatus} ({ni.NetworkInterfaceType})");
                    var ipProps = ni.GetIPProperties();
                    foreach (var ip in ipProps.UnicastAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork))
                    {
                        AppendDiagnostic(textBlock, $"     └─ IP: {ip.Address}");
                    }
                }
                AppendDiagnostic(textBlock, "");

                AppendDiagnostic(textBlock, "🔌 PORT-STATUS:");
                var tcpConnections = System.Net.NetworkInformation.IPGlobalProperties
                    .GetIPGlobalProperties()
                    .GetActiveTcpListeners();

                var port8080InUse = tcpConnections.Any(endpoint => endpoint.Port == 8080);
                AppendDiagnostic(textBlock, $"   • Port 8080: {(port8080InUse ? "❌ BELEGT" : "✅ VERFÜGBAR")}");

                if (port8080InUse)
                {
                    try
                    {
                        var processInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "netstat",
                            Arguments = "-ano | findstr :8080",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        };

                        using var process = System.Diagnostics.Process.Start(processInfo);
                        if (process != null)
                        {
                            await process.WaitForExitAsync();
                            var output = await process.StandardOutput.ReadToEndAsync();
                            AppendDiagnostic(textBlock, $"     └─ Netstat Output: {output.Trim()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendDiagnostic(textBlock, $"     └─ Fehler bei Port-Check: {ex.Message}");
                    }
                }

                var alternativePorts = new[] { 8081, 8082, 8083, 9000, 9001 };
                foreach (var port in alternativePorts)
                {
                    var portInUse = tcpConnections.Any(endpoint => endpoint.Port == port);
                    AppendDiagnostic(textBlock, $"   • Port {port}: {(portInUse ? "❌ BELEGT" : "✅ VERFÜGBAR")}");
                }
                AppendDiagnostic(textBlock, "");

                AppendDiagnostic(textBlock, "🛡️ FIREWALL-STATUS:");
                try
                {
                    var firewallInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "advfirewall show allprofiles state",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using var process = System.Diagnostics.Process.Start(firewallInfo);
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        var output = await process.StandardOutput.ReadToEndAsync();
                        
                        if (output.Contains("State                                 OFF"))
                        {
                            AppendDiagnostic(textBlock, "   • Status: ✅ DEAKTIVIERT (keine Blockierung)");
                        }
                        else if (output.Contains("State                                 ON"))
                        {
                            AppendDiagnostic(textBlock, "   • Status: ⚠️ AKTIVIERT (Port-Freigabe erforderlich)");
                        }
                        else
                        {
                            AppendDiagnostic(textBlock, "   • Status: ❓ UNBEKANNT");
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendDiagnostic(textBlock, $"   • Fehler: {ex.Message}");
                }
                AppendDiagnostic(textBlock, "");

                AppendDiagnostic(textBlock, "🔍 HTTP-LISTENER-SUPPORT:");
                AppendDiagnostic(textBlock, $"   • HttpListener unterstützt: {(HttpListener.IsSupported ? "✅ JA" : "❌ NEIN")}");
                AppendDiagnostic(textBlock, "");

                AppendDiagnostic(textBlock, "🔧 URL-RESERVIERUNGEN:");
                try
                {
                    var urlReservationInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "http show urlacl",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using var process = System.Diagnostics.Process.Start(urlReservationInfo);
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        var output = await process.StandardOutput.ReadToEndAsync();
                        
                        var hasPort8080 = output.Contains(":8080/");
                        AppendDiagnostic(textBlock, $"   • Port 8080 Reservierung: {(hasPort8080 ? "✅ VORHANDEN" : "❌ FEHLT")}");
                        
                        if (hasPort8080)
                        {
                            var lines = output.Split('\n');
                            foreach (var line in lines.Where(l => l.Contains(":8080/")))
                            {
                                AppendDiagnostic(textBlock, $"     └─ {line.Trim()}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendDiagnostic(textBlock, $"   • Fehler: {ex.Message}");
                }
                AppendDiagnostic(textBlock, "");

                AppendDiagnostic(textBlock, "📡 MOBILE-SERVICE-STATUS:");
                if (_mobileService != null)
                {
                    AppendDiagnostic(textBlock, $"   • Service initialisiert: ✅ JA");
                    AppendDiagnostic(textBlock, $"   • Server läuft: {(_mobileService.IsRunning ? "✅ JA" : "❌ NEIN")}");
                    AppendDiagnostic(textBlock, $"   • Lokale IP: {_mobileService.LocalIPAddress}");
                    AppendDiagnostic(textBlock, $"   • QR-Code URL: {_mobileService.QRCodeUrl}");
                }
                else
                {
                    AppendDiagnostic(textBlock, "   • Service initialisiert: ❌ NEIN");
                }
                AppendDiagnostic(textBlock, "");

                AppendDiagnostic(textBlock, "💡 EMPFEHLUNGEN:");
                var recommendations = new List<string>();

                if (!IsRunningAsAdministrator())
                {
                    recommendations.Add("• Als Administrator starten für vollständigen Netzwerk-Zugriff");
                }

                if (port8080InUse)
                {
                    recommendations.Add("• Port 8080 wird verwendet - andere Apps schließen oder alternativen Port nutzen");
                }

                if (!HttpListener.IsSupported)
                {
                    recommendations.Add("• HttpListener nicht unterstützt - .NET Runtime überprüfen");
                }

                if (recommendations.Count == 0)
                {
                    AppendDiagnostic(textBlock, "   ✅ System scheint korrekt konfiguriert zu sein!");
                }
                else
                {
                    foreach (var rec in recommendations)
                    {
                        AppendDiagnostic(textBlock, $"   {rec}");
                    }
                }

                AppendDiagnostic(textBlock, "");
                AppendDiagnostic(textBlock, "✅ DIAGNOSE ABGESCHLOSSEN");
                AppendDiagnostic(textBlock, "");
                AppendDiagnostic(textBlock, "🔧 Bei anhaltenden Problemen:");
                AppendDiagnostic(textBlock, "   • Verwenden Sie 'Netzwerk konfigurieren' für automatische Setup");
                AppendDiagnostic(textBlock, "   • Versuchen Sie 'Ohne Admin-Rechte' für alternative Methoden");
                AppendDiagnostic(textBlock, "   • Nutzen Sie das PowerShell-Script Setup-MobileNetwork.ps1");

            }
            catch (Exception ex)
            {
                AppendDiagnostic(textBlock, $"❌ DIAGNOSE-FEHLER: {ex.Message}");
            }

            // Auto-Scroll to bottom
            scrollViewer.ScrollToEnd();
        }

        private void AppendDiagnostic(TextBlock textBlock, string text)
        {
            textBlock.Text += text + "\n";
            Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
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
    }

    // Helper class für Mobile Commands Display
    public class MobileCommandItem
    {
        public string Description { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Icon { get; set; } = "Cog";
        // UPDATED: Default to design system color
        public Brush ActionColor { get; set; } = (Brush)Application.Current.FindResource("Primary");
    }
}
