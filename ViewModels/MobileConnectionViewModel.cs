using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Linq;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    /// <summary>
    /// ViewModel für das MobileConnectionWindow - MVVM-Implementation v1.9.0
    /// Mobile Integration mit vollständiger Orange-Design-Integration
    /// </summary>
    public class MobileConnectionViewModel : BaseViewModel, IDisposable
    {
        private MobileIntegrationService? _mobileService;
        private bool _allowClose = false;
        
        // UI State Properties
        private string _statusText = "Getrennt";
        private string _serverInfo = "Server gestoppt";
        private string _qrCodeUrl = string.Empty;
        private string _connectedDevicesText = "Keine Geräte verbunden";
        private string _totalViewsValue = "0";
        private string _activeViewersValue = "0";
        private string _windowTitle = "📱 Mobile Verbindung - Getrennt";
        
        // Visual State Properties
        private Brush _statusIndicatorBackground = Brushes.Red;
        private bool _isStartButtonEnabled = true;
        private bool _isStopButtonEnabled = false;
        private Visibility _qrCodeVisibility = Visibility.Collapsed;
        private byte[]? _qrCodeImageBytes;
        
        // Statistics
        private int _totalViews = 0;
        private int _activeViewers = 0;

        public MobileConnectionViewModel()
        {
            InitializeCommands();
            
            LoggingService.Instance.LogInfo("MobileConnectionViewModel initialized with MVVM pattern v1.9.0");
        }

        #region Properties

        /// <summary>
        /// Mobile Integration Service Referenz
        /// </summary>
        public MobileIntegrationService? MobileService
        {
            get => _mobileService;
            set
            {
                if (_mobileService != null)
                {
                    _mobileService.StatusChanged -= OnMobileServiceStatusChanged;
                }
                
                SetProperty(ref _mobileService, value);
                
                if (_mobileService != null)
                {
                    _mobileService.StatusChanged += OnMobileServiceStatusChanged;
                    UpdateUI();
                }
            }
        }

        #region UI Properties

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string ServerInfo
        {
            get => _serverInfo;
            set => SetProperty(ref _serverInfo, value);
        }

        public string QRCodeUrl
        {
            get => _qrCodeUrl;
            set => SetProperty(ref _qrCodeUrl, value);
        }

        public string ConnectedDevicesText
        {
            get => _connectedDevicesText;
            set => SetProperty(ref _connectedDevicesText, value);
        }

        public string TotalViewsValue
        {
            get => _totalViewsValue;
            set => SetProperty(ref _totalViewsValue, value);
        }

        public string ActiveViewersValue
        {
            get => _activeViewersValue;
            set => SetProperty(ref _activeViewersValue, value);
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        #endregion

        #region Visual State Properties

        public Brush StatusIndicatorBackground
        {
            get => _statusIndicatorBackground;
            set => SetProperty(ref _statusIndicatorBackground, value);
        }

        public bool IsStartButtonEnabled
        {
            get => _isStartButtonEnabled;
            set => SetProperty(ref _isStartButtonEnabled, value);
        }

        public bool IsStopButtonEnabled
        {
            get => _isStopButtonEnabled;
            set => SetProperty(ref _isStopButtonEnabled, value);
        }

        public Visibility QRCodeVisibility
        {
            get => _qrCodeVisibility;
            set => SetProperty(ref _qrCodeVisibility, value);
        }

        public byte[]? QRCodeImageBytes
        {
            get => _qrCodeImageBytes;
            set => SetProperty(ref _qrCodeImageBytes, value);
        }

        #endregion

        #endregion

        #region Commands

        public ICommand StartServerCommand { get; private set; } = null!;
        public ICommand StopServerCommand { get; private set; } = null!;
        public ICommand CopyUrlCommand { get; private set; } = null!;
        public ICommand OpenInBrowserCommand { get; private set; } = null!;
        public ICommand TestConnectionCommand { get; private set; } = null!;
        public ICommand TestAPICommand { get; private set; } = null!;
        public ICommand SystemDiagnoseCommand { get; private set; } = null!;
        public ICommand ConfigureNetworkCommand { get; private set; } = null!;
        public ICommand NonAdminHelpCommand { get; private set; } = null!;
        public ICommand CleanupNetworkCommand { get; private set; } = null!;
        public ICommand MinimizeWindowCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            StartServerCommand = new RelayCommand(ExecuteStartServer, CanExecuteStartServer);
            StopServerCommand = new RelayCommand(ExecuteStopServer, CanExecuteStopServer);
            CopyUrlCommand = new RelayCommand(ExecuteCopyUrl, CanExecuteCopyUrl);
            OpenInBrowserCommand = new RelayCommand(ExecuteOpenInBrowser, CanExecuteOpenInBrowser);
            TestConnectionCommand = new RelayCommand(ExecuteTestConnection, CanExecuteTestConnection);
            TestAPICommand = new RelayCommand(ExecuteTestAPI, CanExecuteTestAPI);
            SystemDiagnoseCommand = new RelayCommand(ExecuteSystemDiagnose);
            ConfigureNetworkCommand = new RelayCommand(ExecuteConfigureNetwork, CanExecuteConfigureNetwork);
            NonAdminHelpCommand = new RelayCommand(ExecuteNonAdminHelp);
            CleanupNetworkCommand = new RelayCommand(ExecuteCleanupNetwork, CanExecuteCleanupNetwork);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);
        }

        #endregion

        #region Command Implementations

        private bool CanExecuteStartServer() => _mobileService != null && !(_mobileService?.IsRunning ?? false);

        private async void ExecuteStartServer()
        {
            if (_mobileService == null) return;

            try
            {
                IsStartButtonEnabled = false;
                
                // Zeige erweiterte Start-Optionen
                var startOptions = Application.Current.Dispatcher.Invoke(() =>
                {
                    return MessageBox.Show(
                        "🚀 MOBILE SERVER START\n\n" +
                        "Wählen Sie den Start-Modus:\n\n" +
                        "• JA: Normal-Start mit automatischer Diagnose\n" +
                        "• NEIN: Erweiterte Diagnose vor Start\n" +
                        "• ABBRECHEN: Abbruch\n\n" +
                        "💡 Bei ersten Problemen wählen Sie NEIN für detaillierte Analyse.",
                        "Server-Start-Modus",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);
                });

                switch (startOptions)
                {
                    case MessageBoxResult.Cancel:
                        return;
                    case MessageBoxResult.No:
                        // Erweiterte Diagnose
                        OnMobileServiceStatusChanged("🔍 Führe erweiterte Vor-Start-Diagnose durch...");
                        await PerformPreStartDiagnosis();
                        break;
                    case MessageBoxResult.Yes:
                        // Normaler Start
                        OnMobileServiceStatusChanged("🚀 Starte im Normal-Modus...");
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
                OnMobileServiceStatusChanged($"🚨 Kritischer Start-Fehler: {ex.Message}");
                ShowCriticalErrorHelp(ex);
                LoggingService.Instance.LogError("Critical error starting mobile server via MVVM", ex);
            }
            finally
            {
                IsStartButtonEnabled = true;
                UpdateCommandStates();
            }
        }

        private bool CanExecuteStopServer() => _mobileService?.IsRunning ?? false;

        private void ExecuteStopServer()
        {
            if (_mobileService?.IsRunning == true)
            {
                var result = Application.Current.Dispatcher.Invoke(() =>
                {
                    return MessageBox.Show(
                        "Mobile Server wirklich stoppen?\n\n" +
                        "Alle verbundenen Smartphones verlieren die Verbindung.",
                        "Server stoppen", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);
                });
                
                if (result == MessageBoxResult.Yes)
                {
                    _mobileService.StopServer();
                    LoggingService.Instance.LogInfo("Mobile server stopped via MVVM");
                }
            }
        }

        private bool CanExecuteCopyUrl() => _mobileService?.IsRunning ?? false;

        private void ExecuteCopyUrl()
        {
            try
            {
                var url = _mobileService?.QRCodeUrl ?? "";
                Clipboard.SetText(url);
                
                var networkTip = _mobileService?.LocalIPAddress != "localhost" 
                    ? "\n\n✅ Diese URL funktioniert auch auf anderen Geräten im gleichen WLAN!" 
                    : "\n\n💡 Starten Sie als Administrator für Netzwerk-Zugriff auf andere Geräte.";
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"URL in die Zwischenablage kopiert:\n{url}{networkTip}",
                                   "URL kopiert", MessageBoxButton.OK, MessageBoxImage.Information);
                });
                
                LoggingService.Instance.LogInfo($"URL copied to clipboard via MVVM: {url}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error copying URL via MVVM", ex);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Fehler beim Kopieren: {ex.Message}", "Fehler", 
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private bool CanExecuteOpenInBrowser() => _mobileService?.IsRunning ?? false;

        private void ExecuteOpenInBrowser()
        {
            try
            {
                if (_mobileService?.IsRunning == true)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _mobileService.QRCodeUrl,
                        UseShellExecute = true
                    });
                    LoggingService.Instance.LogInfo($"Opened browser via MVVM: {_mobileService.QRCodeUrl}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening browser via MVVM", ex);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Fehler beim Öffnen der URL: {ex.Message}\n\n" +
                                   $"Versuchen Sie, die URL manuell zu öffnen:\n{_mobileService?.QRCodeUrl}", 
                                   "Browser-Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }

        private bool CanExecuteTestConnection() => _mobileService?.IsRunning ?? false;

        private void ExecuteTestConnection()
        {
            ExecuteOpenInBrowser(); // Same functionality
        }

        private bool CanExecuteTestAPI() => _mobileService?.IsRunning ?? false;

        private async void ExecuteTestAPI()
        {
            if (_mobileService?.IsRunning != true) return;

            var testUrl = $"http://localhost:{8080}/test";
            
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                
                var response = await client.GetAsync(testUrl);
                var content = await response.Content.ReadAsStringAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
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
                });
                
                LoggingService.Instance.LogInfo($"API test completed via MVVM: {response.StatusCode}");
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                LoggingService.Instance.LogError("HTTP request error in API test via MVVM", ex);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"🌐 Netzwerk-Fehler beim API Test:\n\n" +
                                   $"URL: {testUrl}\n" +
                                   $"Fehler: {ex.Message}\n\n" +
                                   $"Mögliche Ursachen:\n" +
                                   $"• Server nicht erreichbar\n" +
                                   $"• Firewall blockiert Port 8080\n" +
                                   $"• Falsche URL-Konfiguration",
                                   "API Test - Netzwerk-Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
            catch (TaskCanceledException)
            {
                LoggingService.Instance.LogWarning("API test timeout via MVVM");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"⏱️ API Test Timeout:\n\n" +
                                   $"URL: {testUrl}\n" +
                                   $"Der Server antwortet nicht innerhalb von 10 Sekunden.\n\n" +
                                   $"Prüfen Sie die Server-Logs im rechten Bereich.",
                                   "API Test - Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Unexpected error in API test via MVVM", ex);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"🚨 Unerwarteter Fehler beim API Test:\n\n" +
                                   $"URL: {testUrl}\n" +
                                   $"Fehler: {ex.GetType().Name}\n" +
                                   $"Message: {ex.Message}",
                                   "API Test - Unbekannter Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void ExecuteSystemDiagnose()
        {
            try
            {
                SystemDiagnoseRequested?.Invoke();
                LoggingService.Instance.LogInfo("System diagnose requested via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error requesting system diagnose via MVVM", ex);
            }
        }

        private bool CanExecuteConfigureNetwork() => _mobileService?.IsRunning ?? false;

        private void ExecuteConfigureNetwork()
        {
            if (_mobileService?.IsRunning != true)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Server muss zuerst gestartet werden!", "Server nicht aktiv", 
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }

            var ip = _mobileService.LocalIPAddress;
            if (ip == "localhost")
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Keine Netzwerk-IP verfügbar.\n\nStarten Sie die App als Administrator für Netzwerk-Zugriff.", 
                                   "Netzwerk-Konfiguration", MessageBoxButton.OK, MessageBoxImage.Information);
                });
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
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(instructions, "Netzwerk-Konfiguration", 
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                });
                LoggingService.Instance.LogInfo("Network configuration commands copied to clipboard via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error copying network configuration via MVVM", ex);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Fehler beim Kopieren: {ex.Message}\n\n{instructions}", 
                                   "Netzwerk-Konfiguration", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }

        private void ExecuteNonAdminHelp()
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

            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(helpText, "iPhone-Zugriff ohne Admin-Rechte", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            });
            
            LoggingService.Instance.LogInfo("Non-admin help displayed via MVVM");
        }

        private bool CanExecuteCleanupNetwork() => _mobileService != null;

        private async void ExecuteCleanupNetwork()
        {
            var result = Application.Current.Dispatcher.Invoke(() =>
            {
                return MessageBox.Show(
                    "🧹 Netzwerk-Konfiguration bereinigen?\n\n" +
                    "Dies entfernt:\n" +
                    "• HTTP URL Reservierungen\n" +
                    "• Firewall-Regeln für Port 8080\n\n" +
                    "⚠️ Administrator-Rechte erforderlich!\n\n" +
                    "Fortfahren?",
                    "Netzwerk-Cleanup", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);
            });

            if (result != MessageBoxResult.Yes) return;

            if (_mobileService?.IsRunning == true)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("⚠️ Stoppen Sie zuerst den Mobile Server!", 
                                   "Server aktiv", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }

            try
            {
                await _mobileService!.CleanupNetworkConfiguration();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        "✅ Netzwerk-Konfiguration erfolgreich bereinigt!\n\n" +
                        "🔄 Starten Sie die App neu für eine saubere Konfiguration.",
                        "Cleanup erfolgreich", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                });
                
                LoggingService.Instance.LogInfo("Network cleanup completed via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Network cleanup error via MVVM", ex);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"❌ Cleanup-Fehler:\n\n{ex.Message}\n\n" +
                        "Stellen Sie sicher, dass die App als Administrator gestartet wurde.",
                        "Cleanup Fehler", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error);
                });
            }
        }

        private void ExecuteMinimizeWindow()
        {
            try
            {
                WindowMinimizeRequested?.Invoke();
                LoggingService.Instance.LogInfo("Window minimize requested via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error requesting window minimize via MVVM", ex);
            }
        }

        #endregion

        #region Public Methods

        public void SetMobileService(MobileIntegrationService mobileService)
        {
            MobileService = mobileService;
        }

        public bool RequestWindowClose()
        {
            // Wenn Server läuft, frage den Benutzer was passieren soll
            if (_mobileService?.IsRunning == true && !_allowClose)
            {
                var result = Application.Current.Dispatcher.Invoke(() =>
                {
                    return MessageBox.Show(
                        "Der Mobile Server ist noch aktiv.\n\n" +
                        "Möchten Sie:\n" +
                        "• JA: Server weiterlaufen lassen (Fenster minimieren)\n" +
                        "• NEIN: Server stoppen und Fenster schließen\n" +
                        "• ABBRECHEN: Fenster offen lassen",
                        "Mobile Server aktiv", 
                        MessageBoxButton.YesNoCancel, 
                        MessageBoxImage.Question);
                });

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        // Fenster minimieren statt schließen
                        WindowMinimizeRequested?.Invoke();
                        ShowTrayNotification();
                        return false; // Schließen verhindern
                        
                    case MessageBoxResult.No:
                        // Server stoppen und normal schließen
                        _mobileService?.StopServer();
                        _allowClose = true;
                        return true;
                        
                    case MessageBoxResult.Cancel:
                        // Schließen abbrechen
                        return false;
                }
            }
            
            return true; // Schließen erlauben
        }

        public void ForceClose()
        {
            _allowClose = true;
            _mobileService?.StopServer();
        }

        #endregion

        #region Private Methods

        private void OnMobileServiceStatusChanged(string status)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText = status;
                
                var networkInfo = _mobileService?.LocalIPAddress != "localhost" 
                    ? $"Netzwerk-IP: {_mobileService?.LocalIPAddress}\n" 
                    : "Nur localhost (Admin-Rechte für Netzwerk erforderlich)\n";
                
                ServerInfo = $"Status: {status}\n{networkInfo}Port: 8080 (Read-Only)\nZeit: {DateTime.Now:HH:mm:ss}";
                
                if (status.Contains("gestartet"))
                {
                    StatusIndicatorBackground = GetThemeColor("Success");
                    IsStartButtonEnabled = false;
                    IsStopButtonEnabled = true;
                    QRCodeVisibility = Visibility.Visible;
                    
                    QRCodeUrl = _mobileService?.QRCodeUrl ?? "";
                    
                    // QR-Code generieren und anzeigen
                    UpdateQRCode();
                    
                    ConnectedDevicesText = "Warte auf Mobile Geräte...";
                    _activeViewers = 1; // Simuliert
                    
                    // Title-Update für bessere Sichtbarkeit
                    WindowTitle = "📱 Mobile Verbindung - READ-ONLY SERVER AKTIV";
                }
                else if (status.Contains("gestoppt"))
                {
                    StatusIndicatorBackground = GetThemeColor("Error");
                    IsStartButtonEnabled = true;
                    IsStopButtonEnabled = false;
                    QRCodeVisibility = Visibility.Collapsed;
                    ConnectedDevicesText = "Keine Geräte verbunden";
                    _activeViewers = 0;
                    
                    WindowTitle = "📱 Mobile Verbindung - Getrennt";
                }
                else if (status.Contains("Fehler") || status.Contains("fehlgeschlagen"))
                {
                    StatusIndicatorBackground = GetThemeColor("Warning");
                    
                    // Zeige Hilfe-Informationen bei Fehlern
                    if (status.Contains("Administrator"))
                    {
                        Application.Current.Dispatcher.BeginInvoke(() =>
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
                        });
                    }
                }
                
                UpdateStatistics();
                UpdateCommandStates();
            });
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
                        QRCodeImageBytes = qrCodeBytes;
                    }
                }
                catch (Exception ex)
                {
                    StatusText = $"QR-Code Fehler: {ex.Message}";
                    LoggingService.Instance.LogError("QR-Code generation error via MVVM", ex);
                }
            }
        }

        private void UpdateUI()
        {
            if (_mobileService?.IsRunning == true)
            {
                StatusText = "Verbunden (Read-Only)";
                StatusIndicatorBackground = GetThemeColor("Success");
                IsStartButtonEnabled = false;
                IsStopButtonEnabled = true;
                QRCodeVisibility = Visibility.Visible;
                QRCodeUrl = _mobileService.QRCodeUrl;
                UpdateQRCode();
                WindowTitle = "📱 Mobile Verbindung - READ-ONLY SERVER AKTIV";
            }
            else
            {
                StatusText = "Getrennt";
                StatusIndicatorBackground = GetThemeColor("Error");
                IsStartButtonEnabled = true;
                IsStopButtonEnabled = false;
                QRCodeVisibility = Visibility.Collapsed;
                WindowTitle = "📱 Mobile Verbindung - Getrennt";
            }
            
            UpdateStatistics();
            UpdateCommandStates();
        }

        private void UpdateStatistics()
        {
            TotalViewsValue = _totalViews.ToString();
            ActiveViewersValue = _activeViewers.ToString();
        }

        private void UpdateCommandStates()
        {
            ((RelayCommand)StartServerCommand).RaiseCanExecuteChanged();
            ((RelayCommand)StopServerCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CopyUrlCommand).RaiseCanExecuteChanged();
            ((RelayCommand)OpenInBrowserCommand).RaiseCanExecuteChanged();
            ((RelayCommand)TestConnectionCommand).RaiseCanExecuteChanged();
            ((RelayCommand)TestAPICommand).RaiseCanExecuteChanged();
            ((RelayCommand)ConfigureNetworkCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CleanupNetworkCommand).RaiseCanExecuteChanged();
        }

        private Brush GetThemeColor(string resourceKey)
        {
            try
            {
                if (Application.Current?.FindResource(resourceKey) is Brush brush)
                {
                    return brush;
                }
            }
            catch
            {
                // Ignore and fall back
            }
            
            // Fallback colors
            return resourceKey switch
            {
                "Success" => Brushes.Green,
                "Error" => Brushes.Red,
                "Warning" => Brushes.Orange,
                _ => Brushes.Gray
            };
        }

        private void ShowTrayNotification()
        {
            TrayNotificationRequested?.Invoke();
        }

        private async Task PerformPreStartDiagnosis()
        {
            // Implementation similar to original but via ViewModel
            try
            {
                OnMobileServiceStatusChanged("🔍 ERWEITERTE VOR-START-DIAGNOSE");
                OnMobileServiceStatusChanged("================================");
                
                // Administrator-Check
                var isAdmin = IsRunningAsAdministrator();
                OnMobileServiceStatusChanged($"🔐 Administrator-Rechte: {(isAdmin ? "✅ Verfügbar" : "❌ Nicht verfügbar")}");
                
                if (!isAdmin)
                {
                    OnMobileServiceStatusChanged("💡 Empfehlung: Als Administrator starten für optimale Ergebnisse");
                }
                
                // Port-Check
                OnMobileServiceStatusChanged("🔌 Prüfe Port-Verfügbarkeit...");
                await CheckPortAvailability();
                
                // Firewall-Check
                OnMobileServiceStatusChanged("🛡️ Prüfe Firewall-Status...");
                await CheckFirewallStatus();
                
                // Netzwerk-Check
                OnMobileServiceStatusChanged("🌐 Prüfe Netzwerk-Interfaces...");
                CheckNetworkInterfaces();
                
                OnMobileServiceStatusChanged("✅ Vor-Start-Diagnose abgeschlossen");
                OnMobileServiceStatusChanged("🚀 Bereit für Server-Start...");
                
                await Task.Delay(1000); // Kurze Pause für Benutzer zum Lesen
            }
            catch (Exception ex)
            {
                OnMobileServiceStatusChanged($"⚠️ Diagnose-Fehler: {ex.Message}");
                LoggingService.Instance.LogError("Pre-start diagnosis error via MVVM", ex);
            }
        }

        private async Task CheckPortAvailability()
        {
            try
            {
                var tcpConnections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
                var port8080InUse = tcpConnections.Any(endpoint => endpoint.Port == 8080);
                
                if (port8080InUse)
                {
                    OnMobileServiceStatusChanged("⚠️ Port 8080 ist bereits belegt");
                    OnMobileServiceStatusChanged("💡 Alternative Ports (8081-8083) werden automatisch getestet");
                }
                else
                {
                    OnMobileServiceStatusChanged("✅ Port 8080 ist verfügbar");
                }
            }
            catch (Exception ex)
            {
                OnMobileServiceStatusChanged($"⚠️ Port-Check Fehler: {ex.Message}");
            }
        }

        private async Task CheckFirewallStatus()
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "advfirewall show allprofiles state",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    var output = await process.StandardOutput.ReadToEndAsync();
                    
                    if (output.Contains("State                                 OFF"))
                    {
                        OnMobileServiceStatusChanged("✅ Windows Firewall ist deaktiviert (keine Blockierung)");
                    }
                    else if (output.Contains("State                                 ON"))
                    {
                        OnMobileServiceStatusChanged("⚠️ Windows Firewall ist aktiv");
                        OnMobileServiceStatusChanged("💡 Port-Freigabe eventuell erforderlich - verwenden Sie 'Netzwerk konfigurieren'");
                    }
                    else
                    {
                        OnMobileServiceStatusChanged("❓ Firewall-Status unbekannt");
                    }
                }
            }
            catch (Exception ex)
            {
                OnMobileServiceStatusChanged($"⚠️ Firewall-Check Fehler: {ex.Message}");
            }
        }

        private void CheckNetworkInterfaces()
        {
            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                    .ToList();
                
                OnMobileServiceStatusChanged($"🌐 Aktive Netzwerk-Interfaces: {networkInterfaces.Count}");
                
                foreach (var ni in networkInterfaces.Take(3)) // Nur erste 3 anzeigen
                {
                    var ipProps = ni.GetIPProperties();
                    var ipv4 = ipProps.UnicastAddresses
                        .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
                        
                    if (ipv4 != null)
                    {
                        OnMobileServiceStatusChanged($"   • {ni.Name}: {ipv4.Address} ({ni.NetworkInterfaceType})");
                    }
                }
                
                if (networkInterfaces.Count == 0)
                {
                    OnMobileServiceStatusChanged("⚠️ Keine aktiven Netzwerk-Interfaces - Netzwerk-Verbindung prüfen");
                }
            }
            catch (Exception ex)
            {
                OnMobileServiceStatusChanged($"⚠️ Netzwerk-Check Fehler: {ex.Message}");
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

            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(successMessage, "Server erfolgreich gestartet", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            });
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

            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(failureMessage, "Server-Start fehlgeschlagen - Hilfe", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            });
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

            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(criticalMessage, "Kritischer Fehler - Erweiterte Hilfe", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            });
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

        #endregion

        #region Events

        /// <summary>
        /// Event wird ausgelöst, wenn das Fenster minimiert werden soll
        /// </summary>
        public event Action? WindowMinimizeRequested;

        /// <summary>
        /// Event wird ausgelöst, wenn eine Tray-Notification angezeigt werden soll
        /// </summary>
        public event Action? TrayNotificationRequested;

        /// <summary>
        /// Event wird ausgelöst, wenn System-Diagnose angefordert wird
        /// </summary>
        public event Action? SystemDiagnoseRequested;

        #endregion

        #region IDisposable Implementation

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_mobileService != null)
                    {
                        _mobileService.StatusChanged -= OnMobileServiceStatusChanged;
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
