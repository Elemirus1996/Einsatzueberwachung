using System;
using System.Windows;
using Einsatzueberwachung.Models;

namespace Einsatzueberwachung
{
    public partial class TeamInputWindow : Window
    {
        public string HundName { get; private set; } = string.Empty;
        public string Hundefuehrer { get; private set; } = string.Empty;
        public string Helfer { get; private set; } = string.Empty;
        public string TeamName => string.IsNullOrWhiteSpace(HundName) ? "Team [Hundename]" : $"Team {HundName}";

        public TeamInputWindow()
        {
            InitializeComponent();
            UpdatePreview();
        }

        private void TxtHundName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdatePreview();
            ValidateInput();
        }

        private void UpdatePreview()
        {
            try
            {
                var hundName = TxtHundName?.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(hundName))
                {
                    TxtTeamNamePreview.Text = "Teamname: Team [Hundename]";
                }
                else
                {
                    TxtTeamNamePreview.Text = $"Teamname: Team {hundName}";
                }
            }
            catch (Exception ex)
            {
                Services.LoggingService.Instance.LogError("Error updating team name preview", ex);
            }
        }

        private void ValidateInput()
        {
            try
            {
                var hundName = TxtHundName?.Text?.Trim() ?? string.Empty;
                BtnNext.IsEnabled = !string.IsNullOrWhiteSpace(hundName);
            }
            catch (Exception ex)
            {
                Services.LoggingService.Instance.LogError("Error validating input", ex);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate required fields
                var hundName = TxtHundName?.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(hundName))
                {
                    MessageBox.Show("Bitte geben Sie einen Hundenamen ein.", 
                        "Eingabe erforderlich", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtHundName?.Focus();
                    return;
                }

                // Store values
                HundName = hundName;
                Hundefuehrer = TxtHundefuehrer?.Text?.Trim() ?? string.Empty;
                Helfer = TxtHelfer?.Text?.Trim() ?? string.Empty;

                Services.LoggingService.Instance.LogInfo($"Team input completed - Team name will be: {TeamName}");
                DialogResult = true;
            }
            catch (Exception ex)
            {
                Services.LoggingService.Instance.LogError("Error completing team input", ex);
                MessageBox.Show($"Fehler beim Speichern der Team-Daten: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
