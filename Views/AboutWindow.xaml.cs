using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// AboutWindow v2.0 - Enhanced with Modern UI and Extended Features
    /// Now inherits from BaseThemeWindow for automatic theme support
    /// Fully integrated with design system and theme service
    /// </summary>
    public partial class AboutWindow : BaseThemeWindow
    {
        public AboutWindow()
        {
            InitializeComponent();
            InitializeThemeSupport(); // Initialize theme after component initialization
            InitializeContent();
            
            LoggingService.Instance.LogInfo("AboutWindow v2.0 initialized with enhanced UI and theme integration");
        }

        private void InitializeContent()
        {
            try
            {
                // Set dynamic version information
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                var versionText = $"Version {version?.Major}.{version?.Minor}.{version?.Build ?? 0}";
                
                // Update version display if TextBlock exists
                if (FindName("VersionTextBlock") is System.Windows.Controls.TextBlock versionBlock)
                {
                    versionBlock.Text = versionText;
                }
                
                LoggingService.Instance.LogInfo($"AboutWindow content initialized - {versionText}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing AboutWindow content", ex);
            }
        }

        protected override void ApplyThemeToWindow(bool isDarkMode)
        {
            try
            {
                // Apply theme-specific styling
                base.ApplyThemeToWindow(isDarkMode);
                
                LoggingService.Instance.LogInfo($"Theme applied to AboutWindow v2.0: {(isDarkMode ? "Dark" : "Light")} mode with Orange design");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to AboutWindow", ex);
            }
        }

        #region Event Handlers

        /// <summary>
        /// Handles hyperlink navigation
        /// </summary>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
                
                LoggingService.Instance.LogInfo($"Opened external link: {e.Uri.AbsoluteUri}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error opening link {e.Uri?.AbsoluteUri}", ex);
                MessageBox.Show($"Fehler beim Öffnen des Links:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Close button click handler
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = true;
                Close();
                LoggingService.Instance.LogInfo("AboutWindow closed by user");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error closing AboutWindow", ex);
            }
        }

        /// <summary>
        /// Enhanced system info button click handler
        /// </summary>
        private void ShowSystemInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var systemInfo = GenerateEnhancedSystemInfo();
                var infoWindow = new SimpleSystemInfoWindow(systemInfo);
                infoWindow.Owner = this;
                infoWindow.ShowDialog();
                
                LoggingService.Instance.LogInfo("Enhanced system information displayed");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing enhanced system info", ex);
                
                // Fallback to simple message box
                try
                {
                    var fallbackInfo = GenerateSystemInfo();
                    MessageBox.Show(fallbackInfo, "System-Information (Fallback)", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception fallbackEx)
                {
                    MessageBox.Show($"Fehler beim Anzeigen der System-Informationen:\n{ex.Message}\n\nFallback-Fehler: {fallbackEx.Message}", 
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Open theme settings button click handler
        /// </summary>
        private void OpenThemeSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new SettingsWindow();
                settingsWindow.Owner = this.Owner ?? this; // Use parent window if available
                settingsWindow.ShowCategory("appearance"); // Direct to theme settings
                settingsWindow.ShowDialog();
                
                LoggingService.Instance.LogInfo("Theme settings opened from AboutWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening theme settings from AboutWindow", ex);
                MessageBox.Show($"Fehler beim Öffnen der Design-Einstellungen:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Test mobile connection button click handler
        /// </summary>
        private void TestMobileConnection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Quick mobile connection test
                var mobileService = MobileService.Instance.GetMobileIntegrationService();
                
                if (mobileService != null && mobileService.IsRunning)
                {
                    MessageBox.Show($"✅ Mobile-Verbindung ist aktiv!\n\n" +
                                   $"Server läuft auf: {mobileService.LocalIPAddress}:8080\n" +
                                   $"QR-Code URL: {mobileService.QRCodeUrl}\n\n" +
                                   $"💡 Scannen Sie den QR-Code mit Ihrem Smartphone für Zugriff auf die Mobile-Ansicht.",
                                   "Mobile-Verbindung Test", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var result = MessageBox.Show($"⚠️ Mobile-Server ist nicht aktiv.\n\n" +
                                               $"Möchten Sie das Mobile-Verbindungs-Fenster öffnen um den Server zu starten?",
                                               "Mobile-Verbindung Test", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        var mobileWindow = new MobileConnectionWindow();
                        mobileWindow.Owner = this.Owner ?? this;
                        mobileWindow.Show();
                    }
                }
                
                LoggingService.Instance.LogInfo("Mobile connection test performed from AboutWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error testing mobile connection from AboutWindow", ex);
                MessageBox.Show($"Fehler beim Testen der Mobile-Verbindung:\n{ex.Message}\n\n" +
                               $"💡 Tipp: Starten Sie die Anwendung als Administrator für optimale Mobile-Features.",
                               "Mobile-Test Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Generate basic system info (fallback)
        /// </summary>
        private string GenerateSystemInfo()
        {
            try
            {
                var info = new System.Text.StringBuilder();
                info.AppendLine("🖥️ SYSTEM-INFORMATION");
                info.AppendLine("=" + new string('=', 30));
                info.AppendLine();
                
                // Application Info
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                info.AppendLine($"📱 Anwendung: Einsatzüberwachung Professional");
                info.AppendLine($"🔢 Version: {version?.Major}.{version?.Minor}.{version?.Build}");
                info.AppendLine($"📅 Build-Datum: {System.IO.File.GetLastWriteTime(System.Reflection.Assembly.GetExecutingAssembly().Location):yyyy-MM-dd HH:mm}");
                info.AppendLine();
                
                // Theme Info
                var themeManager = UnifiedThemeManager.Instance;
                info.AppendLine($"🎨 Theme: {themeManager.CurrentThemeStatus}");
                info.AppendLine($"🌓 Modus: {(themeManager.IsAutoMode ? "Automatisch" : "Manuell")}");
                info.AppendLine($"🎭 Aktuell: {(themeManager.IsDarkMode ? "Dunkel" : "Hell")}");
                info.AppendLine();
                
                // System Info
                info.AppendLine($"💻 Betriebssystem: {Environment.OSVersion}");
                info.AppendLine($"🔧 .NET Version: {Environment.Version}");
                info.AppendLine($"🏗️ Architektur: {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");
                info.AppendLine($"💾 Arbeitsspeicher: {GC.GetTotalMemory(false) / (1024 * 1024):N0} MB");
                info.AppendLine($"👤 Benutzer: {Environment.UserName}");
                info.AppendLine($"🖥️ Computer: {Environment.MachineName}");
                
                return info.ToString();
            }
            catch (Exception ex)
            {
                return $"Fehler beim Generieren der System-Informationen: {ex.Message}";
            }
        }

        /// <summary>
        /// Generate enhanced system info for dedicated window
        /// </summary>
        private SystemInfoData GenerateEnhancedSystemInfo()
        {
            try
            {
                var data = new SystemInfoData();
                
                // Application Info
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                data.ApplicationName = "Einsatzüberwachung Professional";
                data.Version = $"{version?.Major}.{version?.Minor}.{version?.Build ?? 0}";
                data.BuildDate = System.IO.File.GetLastWriteTime(System.Reflection.Assembly.GetExecutingAssembly().Location);
                
                // Theme Info
                var themeManager = UnifiedThemeManager.Instance;
                data.CurrentTheme = themeManager.CurrentThemeStatus;
                data.ThemeMode = themeManager.IsAutoMode ? "Automatisch" : "Manuell";
                data.CurrentAppearance = themeManager.IsDarkMode ? "Dunkel" : "Hell";
                
                // System Info
                data.OperatingSystem = Environment.OSVersion.ToString();
                data.DotNetVersion = Environment.Version.ToString();
                data.Architecture = Environment.Is64BitProcess ? "64-bit" : "32-bit";
                data.WorkingMemory = GC.GetTotalMemory(false) / (1024 * 1024);
                data.UserName = Environment.UserName;
                data.MachineName = Environment.MachineName;
                data.ProcessorCount = Environment.ProcessorCount;
                
                // Performance Info
                var sw = Stopwatch.StartNew();
                GC.Collect();
                sw.Stop();
                data.GCLatency = sw.ElapsedMilliseconds;
                data.GCGen0 = GC.CollectionCount(0);
                data.GCGen1 = GC.CollectionCount(1);
                data.GCGen2 = GC.CollectionCount(2);
                
                // Mobile Service Info
                try
                {
                    var mobileService = MobileService.Instance.GetMobileIntegrationService();
                    data.MobileServiceRunning = mobileService?.IsRunning ?? false;
                    data.MobileServiceUrl = mobileService?.QRCodeUrl ?? "Nicht verfügbar";
                    data.MobileServiceIP = mobileService?.LocalIPAddress ?? "Nicht verfügbar";
                }
                catch
                {
                    data.MobileServiceRunning = false;
                    data.MobileServiceUrl = "Fehler beim Abrufen";
                    data.MobileServiceIP = "Fehler beim Abrufen";
                }
                
                return data;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error generating enhanced system info", ex);
                throw;
            }
        }

        #endregion

        #region Window Events

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                LoggingService.Instance.LogInfo("AboutWindow v2.0 closed");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during AboutWindow cleanup", ex);
            }
            finally
            {
                base.OnClosed(e);
            }
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Data structure for enhanced system information
        /// </summary>
        public class SystemInfoData
        {
            public string ApplicationName { get; set; } = "";
            public string Version { get; set; } = "";
            public DateTime BuildDate { get; set; }
            public string CurrentTheme { get; set; } = "";
            public string ThemeMode { get; set; } = "";
            public string CurrentAppearance { get; set; } = "";
            public string OperatingSystem { get; set; } = "";
            public string DotNetVersion { get; set; } = "";
            public string Architecture { get; set; } = "";
            public long WorkingMemory { get; set; }
            public string UserName { get; set; } = "";
            public string MachineName { get; set; } = "";
            public int ProcessorCount { get; set; }
            public long GCLatency { get; set; }
            public int GCGen0 { get; set; }
            public int GCGen1 { get; set; }
            public int GCGen2 { get; set; }
            public bool MobileServiceRunning { get; set; }
            public string MobileServiceUrl { get; set; } = "";
            public string MobileServiceIP { get; set; } = "";
        }

        /// <summary>
        /// Simple system info window for fallback
        /// </summary>
        private class SimpleSystemInfoWindow : BaseThemeWindow
        {
            public SimpleSystemInfoWindow(SystemInfoData data)
            {
                Title = "Detaillierte System-Information";
                Width = 600;
                Height = 500;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                
                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Padding = new Thickness(20)
                };
                
                var textBlock = new TextBlock
                {
                    Text = FormatSystemInfo(data),
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap
                };
                
                scrollViewer.Content = textBlock;
                Content = scrollViewer;
                
                InitializeThemeSupport();
            }
            
            private static string FormatSystemInfo(SystemInfoData data)
            {
                var info = new System.Text.StringBuilder();
                info.AppendLine("🖥️ DETAILLIERTE SYSTEM-INFORMATION");
                info.AppendLine("=" + new string('=', 50));
                info.AppendLine();
                
                info.AppendLine("📱 ANWENDUNG:");
                info.AppendLine($"   Name: {data.ApplicationName}");
                info.AppendLine($"   Version: {data.Version}");
                info.AppendLine($"   Build-Datum: {data.BuildDate:yyyy-MM-dd HH:mm:ss}");
                info.AppendLine();
                
                info.AppendLine("🎨 DESIGN-SYSTEM:");
                info.AppendLine($"   Status: {data.CurrentTheme}");
                info.AppendLine($"   Modus: {data.ThemeMode}");
                info.AppendLine($"   Darstellung: {data.CurrentAppearance}");
                info.AppendLine();
                
                info.AppendLine("💻 SYSTEM:");
                info.AppendLine($"   Betriebssystem: {data.OperatingSystem}");
                info.AppendLine($"   .NET Version: {data.DotNetVersion}");
                info.AppendLine($"   Architektur: {data.Architecture}");
                info.AppendLine($"   Prozessoren: {data.ProcessorCount}");
                info.AppendLine($"   Arbeitsspeicher: {data.WorkingMemory:N0} MB");
                info.AppendLine($"   Benutzer: {data.UserName}");
                info.AppendLine($"   Computer: {data.MachineName}");
                info.AppendLine();
                
                info.AppendLine("⚡ PERFORMANCE:");
                info.AppendLine($"   GC-Latenz: {data.GCLatency} ms");
                info.AppendLine($"   GC-Generationen: Gen0={data.GCGen0}, Gen1={data.GCGen1}, Gen2={data.GCGen2}");
                info.AppendLine();
                
                info.AppendLine("📱 MOBILE-SERVICE:");
                info.AppendLine($"   Status: {(data.MobileServiceRunning ? "✅ Aktiv" : "❌ Gestoppt")}");
                info.AppendLine($"   URL: {data.MobileServiceUrl}");
                info.AppendLine($"   IP-Adresse: {data.MobileServiceIP}");
                
                return info.ToString();
            }
        }

        #endregion
    }
}
