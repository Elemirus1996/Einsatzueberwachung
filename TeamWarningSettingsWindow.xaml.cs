using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;
using FontAwesome.WPF;

namespace Einsatzueberwachung
{
    public partial class TeamWarningSettingsWindow : Window
    {
        private readonly List<Team> _teams;
        private readonly int _globalFirstWarning;
        private readonly int _globalSecondWarning;
        private readonly Dictionary<int, TeamWarningSettings> _teamSettings;

        public bool SettingsChanged { get; private set; } = false;

        public TeamWarningSettingsWindow(List<Team> teams, int globalFirstWarning, int globalSecondWarning)
        {
            InitializeComponent();
            
            _teams = teams ?? new List<Team>();
            _globalFirstWarning = globalFirstWarning;
            _globalSecondWarning = globalSecondWarning;
            _teamSettings = new Dictionary<int, TeamWarningSettings>();

            InitializeSettings();
            ApplyCurrentTheme();
            
            LoggingService.Instance.LogInfo($"Team warning settings window opened for {_teams.Count} teams");
        }

        private void InitializeSettings()
        {
            try
            {
                // Update global settings display
                TxtGlobalWarning1.Text = $"{_globalFirstWarning} Min";
                TxtGlobalWarning2.Text = $"{_globalSecondWarning} Min";
                TxtSubtitle.Text = $"Individuelle Zeiten für {_teams.Count} Teams festlegen";

                // Create settings for each team
                foreach (var team in _teams)
                {
                    _teamSettings[team.TeamId] = new TeamWarningSettings
                    {
                        TeamId = team.TeamId,
                        TeamName = team.TeamName,
                        FirstWarningMinutes = team.FirstWarningMinutes,
                        SecondWarningMinutes = team.SecondWarningMinutes
                    };

                    CreateTeamSettingsCard(team);
                }

                LoggingService.Instance.LogInfo($"Initialized warning settings for {_teamSettings.Count} teams");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing team warning settings", ex);
                MessageBox.Show($"Fehler beim Laden der Einstellungen: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateTeamSettingsCard(Team team)
        {
            try
            {
                var card = new Border
                {
                    Style = (Style)FindResource("TeamWarningCard")
                };

                var mainGrid = new Grid();
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Team Header
                var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
                
                // Team Type Badge
                var teamTypeBadge = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(team.TeamTypeColorHex)),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(0, 0, 12, 0)
                };
                
                var badgeText = new TextBlock
                {
                    Text = team.TeamTypeShortName,
                    Foreground = Brushes.White,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold
                };
                teamTypeBadge.Child = badgeText;
                headerPanel.Children.Add(teamTypeBadge);

                // Team Info
                var teamInfoPanel = new StackPanel();
                
                var teamNameText = new TextBlock
                {
                    Text = team.TeamName,
                    Style = (Style)FindResource("TitleMedium"),
                    Foreground = (Brush)FindResource("OnSurface")
                };
                teamInfoPanel.Children.Add(teamNameText);

                var teamDetailsText = new TextBlock
                {
                    Text = $"{team.HundName} • {team.TeamTypeDisplayName}",
                    Style = (Style)FindResource("BodySmall"),
                    Foreground = (Brush)FindResource("OnSurfaceVariant"),
                    Margin = new Thickness(0, 2, 0, 0)
                };
                teamInfoPanel.Children.Add(teamDetailsText);

                headerPanel.Children.Add(teamInfoPanel);
                Grid.SetRow(headerPanel, 0);
                mainGrid.Children.Add(headerPanel);

                // Warning Settings
                var settingsPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

                // First Warning
                var warning1Panel = CreateWarningSettingPanel(
                    "⚠️ Erste Warnung", 
                    team.FirstWarningMinutes, 
                    1, 
                    59,
                    (value) => _teamSettings[team.TeamId].FirstWarningMinutes = value
                );
                settingsPanel.Children.Add(warning1Panel);

                // Second Warning
                var warning2Panel = CreateWarningSettingPanel(
                    "🚨 Kritische Warnung", 
                    team.SecondWarningMinutes, 
                    team.FirstWarningMinutes + 1, 
                    120,
                    (value) => _teamSettings[team.TeamId].SecondWarningMinutes = value
                );
                settingsPanel.Children.Add(warning2Panel);

                Grid.SetRow(settingsPanel, 1);
                mainGrid.Children.Add(settingsPanel);

                // Quick Presets
                var presetsPanel = CreatePresetsPanel(team.TeamId);
                Grid.SetRow(presetsPanel, 2);
                mainGrid.Children.Add(presetsPanel);

                card.Child = mainGrid;
                TeamsSettingsPanel.Children.Add(card);

                LoggingService.Instance.LogInfo($"Created settings card for team: {team.TeamName}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error creating settings card for team {team.TeamName}", ex);
            }
        }

        private StackPanel CreateWarningSettingPanel(string title, int currentValue, int minValue, int maxValue, Action<int> onValueChanged)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };

            // Title
            var titleText = new TextBlock
            {
                Text = title,
                Style = (Style)FindResource("LabelMedium"),
                Foreground = (Brush)FindResource("OnSurface"),
                Margin = new Thickness(0, 0, 0, 8)
            };
            panel.Children.Add(titleText);

            // Value Controls
            var controlsGrid = new Grid();
            controlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            controlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            controlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });

            // Numeric Input
            var numericInput = new TextBox
            {
                Style = (Style)FindResource("TimeInput"),
                Text = currentValue.ToString(),
                Tag = onValueChanged
            };
            numericInput.TextChanged += NumericInput_TextChanged;
            Grid.SetColumn(numericInput, 0);
            controlsGrid.Children.Add(numericInput);

            // Slider
            var slider = new Slider
            {
                Style = (Style)FindResource("WarningSlider"),
                Minimum = minValue,
                Maximum = maxValue,
                Value = currentValue,
                TickFrequency = 5,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(16, 0, 16, 0),
                Tag = new { Input = numericInput, OnChange = onValueChanged }
            };
            slider.ValueChanged += Slider_ValueChanged;
            Grid.SetColumn(slider, 1);
            controlsGrid.Children.Add(slider);

            // Unit Label
            var unitLabel = new TextBlock
            {
                Text = "Min",
                Style = (Style)FindResource("LabelMedium"),
                Foreground = (Brush)FindResource("OnSurfaceVariant"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(unitLabel, 2);
            controlsGrid.Children.Add(unitLabel);

            panel.Children.Add(controlsGrid);
            return panel;
        }

        private StackPanel CreatePresetsPanel(int teamId)
        {
            var panel = new StackPanel();

            var titleText = new TextBlock
            {
                Text = "🎯 Schnellauswahl",
                Style = (Style)FindResource("LabelSmall"),
                Foreground = (Brush)FindResource("OnSurfaceVariant"),
                Margin = new Thickness(0, 0, 0, 8)
            };
            panel.Children.Add(titleText);

            var presetsGrid = new Grid();
            presetsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            presetsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            presetsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            presetsGrid.ColumnDefinitions.Add(new ColumnDefinition());

            // Preset buttons
            var presets = new[]
            {
                new { Name = "Schnell", Warning1 = 5, Warning2 = 10 },
                new { Name = "Normal", Warning1 = 10, Warning2 = 20 },
                new { Name = "Erweitert", Warning1 = 15, Warning2 = 30 },
                new { Name = "Lang", Warning1 = 20, Warning2 = 40 }
            };

            for (int i = 0; i < presets.Length; i++)
            {
                var preset = presets[i];
                var presetButton = new Button
                {
                    Content = $"{preset.Name}\n{preset.Warning1}/{preset.Warning2}",
                    Style = (Style)FindResource("TextButton"),
                    Margin = new Thickness(2, 2, 2, 2),
                    Padding = new Thickness(8, 4, 8, 4),
                    FontSize = 10,
                    Tag = new { TeamId = teamId, Preset = preset }
                };
                presetButton.Click += PresetButton_Click;
                Grid.SetColumn(presetButton, i);
                presetsGrid.Children.Add(presetButton);
            }

            panel.Children.Add(presetsGrid);
            return panel;
        }

        private void NumericInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (sender is TextBox textBox && textBox.Tag is Action<int> onValueChanged)
                {
                    if (int.TryParse(textBox.Text, out int value) && value >= 1 && value <= 120)
                    {
                        onValueChanged(value);
                        textBox.Foreground = (Brush)FindResource("OnSurface");
                    }
                    else
                    {
                        textBox.Foreground = (Brush)FindResource("Error");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling numeric input change", ex);
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (sender is Slider slider && slider.Tag is object tagObj)
                {
                    var tag = tagObj.GetType();
                    var input = tag.GetProperty("Input")?.GetValue(tagObj) as TextBox;
                    var onChange = tag.GetProperty("OnChange")?.GetValue(tagObj) as Action<int>;

                    if (input != null && onChange != null)
                    {
                        int value = (int)slider.Value;
                        input.Text = value.ToString();
                        onChange(value);
                        input.Foreground = (Brush)FindResource("OnSurface");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling slider value change", ex);
            }
        }

        private void PresetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is object tagObj)
                {
                    var tag = tagObj.GetType();
                    var teamId = (int)tag.GetProperty("TeamId")?.GetValue(tagObj);
                    var preset = tag.GetProperty("Preset")?.GetValue(tagObj);

                    if (preset != null && _teamSettings.ContainsKey(teamId))
                    {
                        var presetType = preset.GetType();
                        var warning1 = (int)presetType.GetProperty("Warning1")?.GetValue(preset);
                        var warning2 = (int)presetType.GetProperty("Warning2")?.GetValue(preset);

                        _teamSettings[teamId].FirstWarningMinutes = warning1;
                        _teamSettings[teamId].SecondWarningMinutes = warning2;

                        // Refresh the UI by recreating the settings panel
                        RefreshTeamSettings();

                        LoggingService.Instance.LogInfo($"Applied preset to team {teamId}: {warning1}/{warning2}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying preset", ex);
            }
        }

        private void BtnApplyGlobal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    $"Globale Warnschwellen ({_globalFirstWarning}/{_globalSecondWarning} Min) auf alle Teams anwenden?\n\nDies überschreibt alle individuellen Einstellungen.",
                    "Globale Einstellungen anwenden",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    foreach (var setting in _teamSettings.Values)
                    {
                        setting.FirstWarningMinutes = _globalFirstWarning;
                        setting.SecondWarningMinutes = _globalSecondWarning;
                    }

                    RefreshTeamSettings();
                    LoggingService.Instance.LogInfo("Applied global warning settings to all teams");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying global settings", ex);
                MessageBox.Show($"Fehler beim Anwenden der globalen Einstellungen: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshTeamSettings()
        {
            try
            {
                TeamsSettingsPanel.Children.Clear();
                
                foreach (var team in _teams)
                {
                    CreateTeamSettingsCard(team);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error refreshing team settings", ex);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate all settings
                var validationErrors = new List<string>();

                foreach (var setting in _teamSettings.Values)
                {
                    if (setting.FirstWarningMinutes >= setting.SecondWarningMinutes)
                    {
                        validationErrors.Add($"{setting.TeamName}: Erste Warnung muss kleiner als zweite Warnung sein");
                    }

                    if (setting.FirstWarningMinutes < 1 || setting.FirstWarningMinutes > 120)
                    {
                        validationErrors.Add($"{setting.TeamName}: Erste Warnung muss zwischen 1 und 120 Minuten liegen");
                    }

                    if (setting.SecondWarningMinutes < 2 || setting.SecondWarningMinutes > 120)
                    {
                        validationErrors.Add($"{setting.TeamName}: Zweite Warnung muss zwischen 2 und 120 Minuten liegen");
                    }
                }

                if (validationErrors.Any())
                {
                    MessageBox.Show($"Bitte korrigieren Sie folgende Fehler:\n\n{string.Join("\n", validationErrors)}",
                        "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Apply settings to teams
                foreach (var team in _teams)
                {
                    if (_teamSettings.ContainsKey(team.TeamId))
                    {
                        var setting = _teamSettings[team.TeamId];
                        team.FirstWarningMinutes = setting.FirstWarningMinutes;
                        team.SecondWarningMinutes = setting.SecondWarningMinutes;
                    }
                }

                SettingsChanged = true;
                DialogResult = true;

                var summary = string.Join(", ", _teamSettings.Values.Select(s => $"{s.TeamName}: {s.FirstWarningMinutes}/{s.SecondWarningMinutes}"));
                LoggingService.Instance.LogInfo($"Team warning settings saved: {summary}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error saving team warning settings", ex);
                MessageBox.Show($"Fehler beim Speichern der Einstellungen: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ApplyCurrentTheme()
        {
            try
            {
                // Apply current theme based on ThemeService
                var isDarkMode = Services.ThemeService.Instance.IsDarkMode;
                
                if (isDarkMode)
                {
                    Background = (Brush)FindResource("Surface");
                }
                else
                {
                    Background = (Brush)FindResource("SurfaceContainer");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to warning settings window", ex);
            }
        }
    }

    // Helper class for team warning settings
    public class TeamWarningSettings
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int FirstWarningMinutes { get; set; }
        public int SecondWarningMinutes { get; set; }
    }
}
