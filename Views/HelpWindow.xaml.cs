using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// HelpWindow v2.0 - Enhanced with Interactive Navigation and Modern UX
    /// Now inherits from BaseThemeWindow for automatic theme support
    /// Features: Section navigation, search functionality, quick actions
    /// </summary>
    public partial class HelpWindow : BaseThemeWindow
    {
        private readonly Dictionary<string, StackPanel> _contentSections;
        private string _currentSection = "QuickStart";

        public HelpWindow()
        {
            InitializeComponent();
            InitializeThemeSupport(); // Initialize theme after component initialization
            
            // Initialize content sections mapping
            _contentSections = new Dictionary<string, StackPanel>
            {
                ["QuickStart"] = QuickStartContent,
                ["Teams"] = TeamsContent,
                ["Theme"] = ThemeContent,
                ["Mobile"] = MobileContent,
                ["Shortcuts"] = ShortcutsContent,
                ["Tips"] = TipsContent,
                ["Troubleshoot"] = TroubleshootContent
            };

            InitializeContent();
            LoggingService.Instance.LogInfo("HelpWindow v2.0 initialized with enhanced navigation and theme integration");
        }

        private void InitializeContent()
        {
            try
            {
                // Show default section (Quick Start)
                ShowSection("QuickStart");
                SetActiveNavigationButton(QuickStartBtn);
                
                LoggingService.Instance.LogInfo("HelpWindow content initialized with Quick Start section");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing HelpWindow content", ex);
            }
        }

        protected override void ApplyThemeToWindow(bool isDarkMode)
        {
            try
            {
                // Apply theme-specific styling
                base.ApplyThemeToWindow(isDarkMode);
                
                LoggingService.Instance.LogInfo($"Theme applied to HelpWindow v2.0: {(isDarkMode ? "Dark" : "Light")} mode with Orange design");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to HelpWindow", ex);
            }
        }

        #region Navigation Methods

        private void ShowSection(string sectionName)
        {
            try
            {
                // Hide all sections
                foreach (var section in _contentSections.Values)
                {
                    section.Visibility = Visibility.Collapsed;
                }

                // Show requested section
                if (_contentSections.ContainsKey(sectionName))
                {
                    _contentSections[sectionName].Visibility = Visibility.Visible;
                    _currentSection = sectionName;
                    
                    // Scroll to top
                    ContentScrollViewer?.ScrollToTop();
                    
                    LoggingService.Instance.LogInfo($"HelpWindow section changed to: {sectionName}");
                }
                else
                {
                    // Fallback to Quick Start
                    _contentSections["QuickStart"].Visibility = Visibility.Visible;
                    _currentSection = "QuickStart";
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error showing help section: {sectionName}", ex);
            }
        }

        private void SetActiveNavigationButton(Button activeButton)
        {
            try
            {
                // Reset all navigation buttons
                var navButtons = new[] { QuickStartBtn, TeamsBtn, ThemeBtn, MobileBtn, ShortcutsBtn, TipsBtn, TroubleshootBtn };
                
                foreach (var btn in navButtons)
                {
                    btn.Tag = null; // Remove "Active" tag
                }
                
                // Set active button
                activeButton.Tag = "Active";
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error setting active navigation button", ex);
            }
        }

        #endregion

        #region Event Handlers - Navigation

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button)
                {
                    var section = button.Name switch
                    {
                        "QuickStartBtn" => "QuickStart",
                        "TeamsBtn" => "Teams",
                        "ThemeBtn" => "Theme", 
                        "MobileBtn" => "Mobile",
                        "ShortcutsBtn" => "Shortcuts",
                        "TipsBtn" => "Tips",
                        "TroubleshootBtn" => "Troubleshoot",
                        _ => "QuickStart"
                    };

                    ShowSection(section);
                    SetActiveNavigationButton(button);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling navigation button click", ex);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (sender is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    var searchTerm = textBox.Text.ToLower();
                    
                    // Simple search logic - could be enhanced
                    if (searchTerm.Contains("theme") || searchTerm.Contains("design") || searchTerm.Contains("farbe"))
                    {
                        ShowSection("Theme");
                        SetActiveNavigationButton(ThemeBtn);
                    }
                    else if (searchTerm.Contains("mobile") || searchTerm.Contains("handy") || searchTerm.Contains("phone"))
                    {
                        ShowSection("Mobile");
                        SetActiveNavigationButton(MobileBtn);
                    }
                    else if (searchTerm.Contains("team") || searchTerm.Contains("hund"))
                    {
                        ShowSection("Teams");
                        SetActiveNavigationButton(TeamsBtn);
                    }
                    else if (searchTerm.Contains("shortcut") || searchTerm.Contains("taste") || searchTerm.Contains("strg"))
                    {
                        ShowSection("Shortcuts");
                        SetActiveNavigationButton(ShortcutsBtn);
                    }
                    else if (searchTerm.Contains("problem") || searchTerm.Contains("fehler") || searchTerm.Contains("hilfe"))
                    {
                        ShowSection("Troubleshoot");
                        SetActiveNavigationButton(TroubleshootBtn);
                    }
                    
                    LoggingService.Instance.LogInfo($"Help search performed: {searchTerm}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling search", ex);
            }
        }

        #endregion

        #region Event Handlers - Actions

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Close();
                LoggingService.Instance.LogInfo("HelpWindow v2.0 closed by user");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error closing HelpWindow", ex);
            }
        }

        private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open settings window focused on theme category
                var settingsWindow = new SettingsWindow();
                settingsWindow.Owner = Owner ?? Application.Current.MainWindow;
                settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                
                // Show theme settings specifically
                settingsWindow.ShowThemeSettings();
                settingsWindow.ShowDialog();
                
                LoggingService.Instance.LogInfo("Settings window opened from HelpWindow v2.0");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening settings from HelpWindow", ex);
                MessageBox.Show($"Fehler beim Öffnen der Einstellungen:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenMobileConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Try to get MainViewModel for data integration
                var mainViewModel = MainViewModelService.Instance.GetMainViewModel();
                
                MobileConnectionWindow mobileWindow;
                if (mainViewModel != null)
                {
                    // Create with data integration
                    mobileWindow = MobileConnectionWindow.CreateWithDataIntegration(mainViewModel);
                    LoggingService.Instance.LogInfo("MobileConnectionWindow opened with data integration from HelpWindow");
                }
                else
                {
                    // Create without data integration
                    mobileWindow = new MobileConnectionWindow();
                    LoggingService.Instance.LogInfo("MobileConnectionWindow opened without data integration from HelpWindow");
                }
                
                mobileWindow.Owner = Owner ?? Application.Current.MainWindow;
                mobileWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                mobileWindow.Show();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening mobile connection from HelpWindow", ex);
                MessageBox.Show($"Fehler beim Öffnen der Mobile-Verbindung:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenStatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainViewModel = MainViewModelService.Instance.GetMainViewModel();
                
                if (mainViewModel?.Teams != null)
                {
                    var statsWindow = new StatisticsWindow(mainViewModel.Teams.ToList(), new Models.EinsatzData());
                    statsWindow.Owner = Owner ?? Application.Current.MainWindow;
                    statsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    statsWindow.Show();
                    
                    LoggingService.Instance.LogInfo("StatisticsWindow opened from HelpWindow");
                }
                else
                {
                    MessageBox.Show("Keine Einsatzdaten verfügbar.\n\nStarten Sie zuerst einen Einsatz um Statistiken anzuzeigen.", 
                        "Keine Daten", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening statistics from HelpWindow", ex);
                MessageBox.Show($"Fehler beim Öffnen der Statistiken:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenAboutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var aboutWindow = new AboutWindow();
                aboutWindow.Owner = this;
                aboutWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                aboutWindow.ShowDialog();
                
                LoggingService.Instance.LogInfo("AboutWindow opened from HelpWindow v2.0");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening about window from HelpWindow", ex);
                MessageBox.Show($"Fehler beim Öffnen des Info-Fensters:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ContactSupport_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                var supportInfo = GenerateSupportInformation();
                
                MessageBox.Show($"📧 SUPPORT-KONTAKT\n\n" +
                              $"Für technischen Support:\n\n" +
                              $"1. Screenshot des Problems erstellen\n" +
                              $"2. System-Information sammeln (siehe unten)\n" +
                              $"3. Detaillierte Fehlerbeschreibung\n\n" +
                              $"System-Info für Support:\n{supportInfo}",
                              "Support kontaktieren", MessageBoxButton.OK, MessageBoxImage.Information);
                
                LoggingService.Instance.LogInfo("Support contact information displayed");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing support contact", ex);
            }
        }

        private void ShowSystemInfo_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                var systemInfo = GenerateDetailedSystemInfo();
                
                var infoWindow = new SystemInfoDisplayWindow(systemInfo);
                infoWindow.Owner = this;
                infoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                infoWindow.ShowDialog();
                
                LoggingService.Instance.LogInfo("System information displayed from HelpWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing system info from HelpWindow", ex);
                MessageBox.Show($"Fehler bei der System-Diagnose:\n{ex.Message}", 
                    "Diagnose-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helper Methods

        private string GenerateSupportInformation()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                var themeManager = UnifiedThemeManager.Instance;
                
                return $"Version: {version?.Major}.{version?.Minor}.{version?.Build}\n" +
                       $"OS: {Environment.OSVersion}\n" +
                       $"Theme: {(themeManager.IsDarkMode ? "Dark" : "Light")}\n" +
                       $"Zeit: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            }
            catch
            {
                return "Fehler beim Sammeln der System-Informationen";
            }
        }

        private string GenerateDetailedSystemInfo()
        {
            try
            {
                var info = new System.Text.StringBuilder();
                info.AppendLine("🔍 SYSTEM-DIAGNOSE");
                info.AppendLine("=" + new string('=', 40));
                info.AppendLine();
                
                // Application Info
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                info.AppendLine($"📱 Anwendung: Einsatzüberwachung Professional v2.0");
                info.AppendLine($"🔢 Version: {version?.Major}.{version?.Minor}.{version?.Build}");
                info.AppendLine();
                
                // Theme Info
                var themeManager = UnifiedThemeManager.Instance;
                info.AppendLine($"🎨 Design-System: {themeManager.CurrentThemeStatus}");
                info.AppendLine($"🌓 Modus: {(themeManager.IsAutoMode ? "Automatisch" : "Manuell")}");
                info.AppendLine();
                
                // System Info
                info.AppendLine($"💻 System: {Environment.OSVersion}");
                info.AppendLine($"🔧 .NET: {Environment.Version}");
                info.AppendLine($"💾 RAM: {GC.GetTotalMemory(false) / (1024 * 1024):N0} MB");
                info.AppendLine($"🖥️ Auflösung: {SystemParameters.PrimaryScreenWidth}x{SystemParameters.PrimaryScreenHeight}");
                info.AppendLine();
                
                // Current Session
                var mainViewModel = MainViewModelService.Instance.GetMainViewModel();
                info.AppendLine($"👥 Aktive Teams: {mainViewModel?.Teams?.Count ?? 0}");
                info.AppendLine($"⏱️ Laufende Timer: {mainViewModel?.Teams?.Count(t => t.IsRunning) ?? 0}");
                
                return info.ToString();
            }
            catch (Exception ex)
            {
                return $"Fehler bei der Diagnose: {ex.Message}";
            }
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Simple system info display window
        /// </summary>
        private class SystemInfoDisplayWindow : BaseThemeWindow
        {
            public SystemInfoDisplayWindow(string systemInfo)
            {
                Title = "System-Diagnose";
                Width = 600;
                Height = 450;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                
                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Padding = new Thickness(20)
                };
                
                var textBlock = new TextBlock
                {
                    Text = systemInfo,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 18
                };
                
                scrollViewer.Content = textBlock;
                Content = scrollViewer;
                
                InitializeThemeSupport();
            }
        }

        #endregion

        #region Window Events

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                LoggingService.Instance.LogInfo("HelpWindow v2.0 closed and cleaned up");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during HelpWindow cleanup", ex);
            }
            finally
            {
                base.OnClosed(e);
            }
        }

        #endregion
    }
}
