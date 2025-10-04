using System;
using System.Linq;
using System.Windows;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung
{
    public partial class TeamInputWindow : Window
    {
        public string HundName { get; private set; } = string.Empty;
        public string Hundefuehrer { get; private set; } = string.Empty;
        public string Helfer { get; private set; } = string.Empty;
        public string Suchgebiet { get; private set; } = string.Empty;
        public string TeamName => string.IsNullOrWhiteSpace(HundName) ? "Team [Hundename]" : $"Team {HundName}";
        
        // NEU: Speichere gewählten Hund und dessen Spezialisierungen
        public DogEntry? SelectedDog { get; private set; }
        public MultipleTeamTypes? PreselectedTeamTypes { get; private set; }

        private readonly MasterDataService _masterDataService;

        public TeamInputWindow()
        {
            InitializeComponent();
            
            _masterDataService = MasterDataService.Instance;
            
            // Apply current theme
            ApplyCurrentTheme();
            
            // Load master data into ComboBoxes
            LoadMasterData();
            
            UpdatePreview();
        }

        private void LoadMasterData()
        {
            try
            {
                // Load dogs into ComboBox
                var dogs = _masterDataService.GetActiveDogs();
                CmbHund.Items.Clear();
                CmbHund.Items.Add(new { Name = "(Manuell eingeben)" }); // Manual entry option
                
                foreach (var dog in dogs)
                {
                    CmbHund.Items.Add(dog);
                }
                
                if (CmbHund.Items.Count > 0)
                    CmbHund.SelectedIndex = 0;

                // Load Hundeführer (personnel with Hundeführer skill)
                var hundefuehrer = _masterDataService.GetPersonalBySkill(PersonalSkills.Hundefuehrer);
                CmbHundefuehrer.Items.Clear();
                CmbHundefuehrer.Items.Add(new { FullName = "(Manuell eingeben)" });
                
                foreach (var person in hundefuehrer)
                {
                    CmbHundefuehrer.Items.Add(person);
                }
                
                if (CmbHundefuehrer.Items.Count > 0)
                    CmbHundefuehrer.SelectedIndex = 0;

                // Load Helfer (personnel with Helfer skill)
                var helfer = _masterDataService.GetPersonalBySkill(PersonalSkills.Helfer);
                CmbHelfer.Items.Clear();
                CmbHelfer.Items.Add(new { FullName = "(Leer lassen / Manuell)" });
                
                foreach (var person in helfer)
                {
                    CmbHelfer.Items.Add(person);
                }
                
                if (CmbHelfer.Items.Count > 0)
                    CmbHelfer.SelectedIndex = 0;

                LoggingService.Instance.LogInfo($"Loaded master data: {dogs.Count} dogs, {hundefuehrer.Count} handlers, {helfer.Count} helpers");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading master data into TeamInputWindow", ex);
                MessageBox.Show("Fehler beim Laden der Stammdaten. Sie können die Daten manuell eingeben.", 
                    "Warnung", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ApplyCurrentTheme()
        {
            try
            {
                // Subscribe to theme changes - The window will automatically use global theme resources
                if (Services.ThemeService.Instance != null)
                {
                    var isDarkMode = Services.ThemeService.Instance.IsDarkMode;
                    // Window automatically inherits theme from Application.Current.Resources
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to TeamInputWindow", ex);
            }
        }

        private void CmbHund_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (CmbHund.SelectedItem is DogEntry selectedDog)
                {
                    // Speichere gewählten Hund
                    SelectedDog = selectedDog;
                    HundName = selectedDog.Name;
                    UpdatePreview();
                    ValidateInput();

                    // Konvertiere Hunde-Spezialisierungen zu TeamTypes
                    PreselectedTeamTypes = ConvertDogSpecializationsToTeamTypes(selectedDog.Specializations);
                    
                    // Zeige ausgewählte Spezialisierungen an
                    if (PreselectedTeamTypes != null && PreselectedTeamTypes.SelectedTypes.Count > 0)
                    {
                        TxtSelectedSpecialization.Text = $"Vorauswahl: {PreselectedTeamTypes.DisplayName}";
                        // UPDATED: Use design system Success color
                        TxtSelectedSpecialization.Foreground = (System.Windows.Media.Brush)FindResource("Success");
                        
                        LoggingService.Instance.LogInfo($"Dog '{selectedDog.Name}' selected with specializations: {PreselectedTeamTypes.DisplayName}");
                    }
                    else
                    {
                        TxtSelectedSpecialization.Text = "Wird im nächsten Schritt ausgewählt";
                        TxtSelectedSpecialization.Foreground = (System.Windows.Media.Brush)FindResource("OnSurfaceVariant");
                    }

                    // Auto-fill Hundeführer if dog has one assigned
                    if (!string.IsNullOrEmpty(selectedDog.HundefuehrerId))
                    {
                        var hundefuehrer = _masterDataService.GetPersonalById(selectedDog.HundefuehrerId);
                        if (hundefuehrer != null)
                        {
                            // Suche den Hundeführer in der ComboBox und wähle ihn aus
                            foreach (var item in CmbHundefuehrer.Items)
                            {
                                if (item is PersonalEntry person && person.Id == hundefuehrer.Id)
                                {
                                    CmbHundefuehrer.SelectedItem = item;
                                    LoggingService.Instance.LogInfo($"Auto-filled handler: {hundefuehrer.FullName}");
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (CmbHund.IsEditable && !string.IsNullOrEmpty(CmbHund.Text))
                {
                    // Manual text entry - keine Spezialisierungen vorauswählen
                    SelectedDog = null;
                    PreselectedTeamTypes = null;
                    HundName = CmbHund.Text.Trim();
                    UpdatePreview();
                    ValidateInput();
                    
                    TxtSelectedSpecialization.Text = "Wird im nächsten Schritt ausgewählt";
                    TxtSelectedSpecialization.Foreground = (System.Windows.Media.Brush)FindResource("OnSurfaceVariant");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling dog selection", ex);
            }
        }

        private MultipleTeamTypes? ConvertDogSpecializationsToTeamTypes(DogSpecialization specializations)
        {
            if (specializations == DogSpecialization.None)
                return null;

            var teamTypes = new MultipleTeamTypes();

            // Konvertiere jede Hunde-Spezialisierung zu einem entsprechenden TeamType
            if (specializations.HasFlag(DogSpecialization.Flaechensuche))
                teamTypes.SelectedTypes.Add(TeamType.Flaechensuchhund);

            if (specializations.HasFlag(DogSpecialization.Truemmersuche))
                teamTypes.SelectedTypes.Add(TeamType.Truemmersuchhund);

            if (specializations.HasFlag(DogSpecialization.Mantrailing))
                teamTypes.SelectedTypes.Add(TeamType.Mantrailer);

            if (specializations.HasFlag(DogSpecialization.Wasserortung))
                teamTypes.SelectedTypes.Add(TeamType.Wasserrettungshund);

            if (specializations.HasFlag(DogSpecialization.Lawinensuche))
                teamTypes.SelectedTypes.Add(TeamType.Lawinensuchhund);

            // Geländesuche und Leichensuche könnten auf Allgemein gemappt werden
            // da es keine spezifischen TeamTypes dafür gibt
            if (specializations.HasFlag(DogSpecialization.Gelaendesuche) || 
                specializations.HasFlag(DogSpecialization.Leichensuche))
            {
                // Füge Allgemein hinzu, falls nicht schon eine andere Spezialisierung vorhanden
                if (teamTypes.SelectedTypes.Count == 0)
                {
                    teamTypes.SelectedTypes.Add(TeamType.Allgemein);
                }
            }

            return teamTypes.SelectedTypes.Count > 0 ? teamTypes : null;
        }

        private void CmbHundefuehrer_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (CmbHundefuehrer.SelectedItem is PersonalEntry selectedPerson)
                {
                    Hundefuehrer = selectedPerson.FullName;
                }
                else if (CmbHundefuehrer.IsEditable && !string.IsNullOrEmpty(CmbHundefuehrer.Text))
                {
                    Hundefuehrer = CmbHundefuehrer.Text.Trim();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling Hundeführer selection", ex);
            }
        }

        private void CmbHelfer_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (CmbHelfer.SelectedItem is PersonalEntry selectedPerson)
                {
                    Helfer = selectedPerson.FullName;
                }
                else if (CmbHelfer.IsEditable && !string.IsNullOrEmpty(CmbHelfer.Text))
                {
                    Helfer = CmbHelfer.Text.Trim();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling Helfer selection", ex);
            }
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
                if (string.IsNullOrWhiteSpace(HundName))
                {
                    TxtTeamNamePreview.Text = "Teamname: Team [Hundename]";
                }
                else
                {
                    TxtTeamNamePreview.Text = $"Teamname: Team {HundName}";
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating team name preview", ex);
            }
        }

        private void ValidateInput()
        {
            try
            {
                BtnNext.IsEnabled = !string.IsNullOrWhiteSpace(HundName);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error validating input", ex);
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
                // Get final values from ComboBoxes (in case manually edited)
                if (CmbHund.IsEditable && !string.IsNullOrEmpty(CmbHund.Text))
                {
                    HundName = CmbHund.Text.Trim();
                }

                if (CmbHundefuehrer.IsEditable && !string.IsNullOrEmpty(CmbHundefuehrer.Text))
                {
                    Hundefuehrer = CmbHundefuehrer.Text.Trim();
                }

                if (CmbHelfer.IsEditable && !string.IsNullOrEmpty(CmbHelfer.Text))
                {
                    Helfer = CmbHelfer.Text.Trim();
                }

                // Get Suchgebiet value
                Suchgebiet = TxtSuchgebiet.Text.Trim();

                // Validate required fields
                if (string.IsNullOrWhiteSpace(HundName))
                {
                    MessageBox.Show("Bitte wählen Sie einen Hund aus oder geben Sie einen Namen ein.", 
                        "Eingabe erforderlich", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CmbHund?.Focus();
                    return;
                }

                // NEU: Team-Typ-Auswahl öffnen
                var teamTypeWindow = new TeamTypeSelectionWindow(PreselectedTeamTypes);
                teamTypeWindow.Owner = this;
                teamTypeWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                
                if (teamTypeWindow.ShowDialog() == true)
                {
                    // Ausgewählte Team-Typen übernehmen
                    PreselectedTeamTypes = teamTypeWindow.SelectedMultipleTeamTypes;
                    
                    LoggingService.Instance.LogInfo($"Team input completed - Team name: {TeamName}, " +
                        $"Suchgebiet: {Suchgebiet}, Selected types: {PreselectedTeamTypes?.DisplayName ?? "None"}");
                    
                    DialogResult = true;
                }
                // Wenn TeamTypeSelectionWindow abgebrochen wird, bleibt TeamInputWindow offen
                
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error completing team input", ex);
                MessageBox.Show($"Fehler beim Speichern der Team-Daten: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
