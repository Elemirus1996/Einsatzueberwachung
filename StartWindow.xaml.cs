using System;
using System.Windows;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung
{
    public partial class StartWindow : Window
    {
        public EinsatzData? EinsatzData { get; private set; }
        public int FirstWarningMinutes { get; private set; } = 10;
        public int SecondWarningMinutes { get; private set; } = 20;

        public StartWindow()
        {
            InitializeComponent();
            InitializeTheme();
            LoggingService.Instance.LogInfo("StartWindow v1.5 initialized");
        }

        private void InitializeTheme()
        {
            // Apply current theme to start window
            ApplyTheme(ThemeService.Instance.IsDarkMode);
            ThemeService.Instance.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged(bool isDarkMode)
        {
            Dispatcher.Invoke(() => ApplyTheme(isDarkMode));
        }

        private void ApplyTheme(bool isDarkMode)
        {
            try
            {
                if (isDarkMode)
                {
                    // Dark theme colors
                    Resources["BackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(18, 18, 18)); // #121212
                    Resources["CardBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)); // #1E1E1E
                    Resources["TextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224)); // #E0E0E0
                    Resources["BorderBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 51, 51)); // #333333
                    Resources["PrimaryBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(187, 134, 252)); // #BB86FC
                    Resources["AccentBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(207, 102, 121)); // #CF6679
                    
                    // Dark Mode Info Box - bessere Lesbarkeit
                    Resources["InfoBoxBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)); // #2D2D2D
                    Resources["InfoBoxTextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224)); // #E0E0E0
                }
                else
                {
                    // Light theme colors
                    Resources["BackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245)); // #F5F5F5
                    Resources["CardBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                    Resources["TextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 33, 33)); // #212121
                    Resources["BorderBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224)); // #E0E0E0
                    Resources["PrimaryBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243)); // #2196F3
                    Resources["AccentBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 64, 129)); // #FF4081
                    
                    // Light Mode Info Box
                    Resources["InfoBoxBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(227, 242, 253)); // #E3F2FD
                    Resources["InfoBoxTextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(13, 71, 161)); // #0D47A1
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to StartWindow", ex);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            LoggingService.Instance.LogInfo("Start canceled by user");
            DialogResult = false;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            LoggingService.Instance.LogInfo("Start window closed by user");
            DialogResult = false;
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate essential inputs
                if (string.IsNullOrWhiteSpace(TxtEinsatzleiter.Text))
                {
                    MessageBox.Show("Bitte geben Sie einen Einsatzleiter ein.", "Validierungsfehler", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtEinsatzleiter.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtEinsatzort.Text))
                {
                    MessageBox.Show("Bitte geben Sie einen Einsatzort ein.", "Validierungsfehler", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtEinsatzort.Focus();
                    return;
                }

                if (!int.TryParse(TxtWarning1.Text, out int warning1) || warning1 < 1)
                {
                    MessageBox.Show("Bitte geben Sie eine gültige Zeit für die erste Warnung ein (mindestens 1 Minute).", 
                        "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtWarning1.Focus();
                    return;
                }

                if (!int.TryParse(TxtWarning2.Text, out int warning2) || warning2 <= warning1)
                {
                    MessageBox.Show("Die zweite Warnung muss größer als die erste Warnung sein.", 
                        "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtWarning2.Focus();
                    return;
                }

                // Create simplified EinsatzData - teams will be added in MainWindow
                EinsatzData = new EinsatzData
                {
                    Einsatzleiter = TxtEinsatzleiter.Text.Trim(),
                    Alarmiert = TxtAlarmiert.Text.Trim(),
                    Einsatzort = TxtEinsatzort.Text.Trim(),
                    IstEinsatz = RbEinsatz.IsChecked == true,
                    AnzahlTeams = 0, // Teams will be added dynamically in MainWindow
                    EinsatzDatum = DateTime.Now,
                    // Set default export path - can be changed later in MainWindow
                    ExportPfad = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "Einsatzueberwachung"),
                    Fuehrungsassistent = string.Empty // Can be added later if needed
                };

                FirstWarningMinutes = warning1;
                SecondWarningMinutes = warning2;

                LoggingService.Instance.LogInfo($"Einsatz v1.5 started - Type: {EinsatzData.EinsatzTyp}, " +
                    $"Location: {EinsatzData.Einsatzort}, Leader: {EinsatzData.Einsatzleiter}, " +
                    $"Warnings: {warning1}/{warning2} minutes");

                DialogResult = true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error starting mission", ex);
                MessageBox.Show($"Fehler beim Starten des Einsatzes: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from theme changes
            ThemeService.Instance.ThemeChanged -= OnThemeChanged;
            base.OnClosed(e);
        }
    }
}
