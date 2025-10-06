using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    /// <summary>
    /// ViewModel für TeamWarningSettingsWindow - MVVM-Implementation v1.9.0
    /// Komplexe Slider/Input-Logic mit Preset-Buttons und Team-Settings-Management
    /// </summary>
    public class TeamWarningSettingsViewModel : BaseViewModel
    {
        private readonly List<Team> _teams;
        private readonly int _globalFirstWarning;
        private readonly int _globalSecondWarning;
        private readonly Dictionary<int, TeamWarningSettings> _teamSettings;

        // UI State Properties
        private string _windowTitle = "Team-Warnzeiten konfigurieren";
        private string _globalWarning1Text = "10 Min";
        private string _globalWarning2Text = "20 Min";
        private string _subtitleText = "Individuelle Zeiten für Teams festlegen";
        private bool _settingsChanged = false;
        private bool? _dialogResult;

        // Collections
        public ObservableCollection<TeamWarningItemViewModel> TeamWarningItems { get; } = new ObservableCollection<TeamWarningItemViewModel>();

        // Commands
        public ICommand ApplyGlobalCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // Events
        public event Action? RequestClose;

        // Properties
        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        public string GlobalWarning1Text
        {
            get => _globalWarning1Text;
            set => SetProperty(ref _globalWarning1Text, value);
        }

        public string GlobalWarning2Text
        {
            get => _globalWarning2Text;
            set => SetProperty(ref _globalWarning2Text, value);
        }

        public string SubtitleText
        {
            get => _subtitleText;
            set => SetProperty(ref _subtitleText, value);
        }

        public bool SettingsChanged
        {
            get => _settingsChanged;
            set => SetProperty(ref _settingsChanged, value);
        }

        // Dialog Result
        public bool? DialogResult
        {
            get => _dialogResult;
            private set => SetProperty(ref _dialogResult, value);
        }

        public TeamWarningSettingsViewModel(List<Team> teams, int globalFirstWarning, int globalSecondWarning)
        {
            _teams = teams ?? new List<Team>();
            _globalFirstWarning = globalFirstWarning;
            _globalSecondWarning = globalSecondWarning;
            _teamSettings = new Dictionary<int, TeamWarningSettings>();

            // Initialize commands
            ApplyGlobalCommand = new RelayCommand(ExecuteApplyGlobal);
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);

            // Initialize settings
            InitializeSettings();

            LoggingService.Instance.LogInfo($"TeamWarningSettingsViewModel initialized with MVVM pattern v1.9.0 for {_teams.Count} teams");
        }

        private void InitializeSettings()
        {
            try
            {
                // Update global settings display
                GlobalWarning1Text = $"{_globalFirstWarning} Min";
                GlobalWarning2Text = $"{_globalSecondWarning} Min";
                SubtitleText = $"Individuelle Zeiten für {_teams.Count} Teams festlegen";

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

                    CreateTeamWarningItem(team);
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

        private void CreateTeamWarningItem(Team team)
        {
            try
            {
                var teamItem = new TeamWarningItemViewModel
                {
                    TeamId = team.TeamId,
                    TeamName = team.TeamName,
                    HundName = team.HundName,
                    TeamTypeDisplayName = team.TeamTypeDisplayName,
                    TeamTypeShortName = team.TeamTypeShortName,
                    TeamTypeColorHex = team.TeamTypeColorHex,
                    FirstWarningMinutes = team.FirstWarningMinutes,
                    SecondWarningMinutes = team.SecondWarningMinutes
                };

                // Subscribe to property changes
                teamItem.PropertyChanged += TeamItem_PropertyChanged;

                // Subscribe to preset commands
                teamItem.ApplyPresetCommand = new RelayCommand<PresetInfo>(preset => ExecuteApplyPreset(teamItem, preset!));

                TeamWarningItems.Add(teamItem);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error creating team warning item for {team.TeamName}", ex);
            }
        }

        private void TeamItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is TeamWarningItemViewModel item)
            {
                try
                {
                    // Update settings dictionary
                    if (_teamSettings.ContainsKey(item.TeamId))
                    {
                        _teamSettings[item.TeamId].FirstWarningMinutes = item.FirstWarningMinutes;
                        _teamSettings[item.TeamId].SecondWarningMinutes = item.SecondWarningMinutes;
                    }

                    // Mark as changed
                    SettingsChanged = true;

                    // Update save command state
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("Error handling team item property change", ex);
                }
            }
        }

        #region Command Implementations

        private void ExecuteApplyGlobal()
        {
            try
            {
                var result = MessageBox.Show(
                    $"Globale Warnschwellen ({_globalFirstWarning}/{_globalSecondWarning} Min) auf alle Teams anwenden?\n\n" +
                    "Dies überschreibt alle individuellen Einstellungen.",
                    "Globale Einstellungen anwenden",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    foreach (var item in TeamWarningItems)
                    {
                        item.FirstWarningMinutes = _globalFirstWarning;
                        item.SecondWarningMinutes = _globalSecondWarning;
                    }

                    SettingsChanged = true;
                    LoggingService.Instance.LogInfo("Applied global warning settings to all teams via MVVM");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying global settings via MVVM", ex);
                MessageBox.Show($"Fehler beim Anwenden der globalen Einstellungen: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteSave()
        {
            return ValidateAllSettings();
        }

        private void ExecuteSave()
        {
            try
            {
                // Final validation
                var validationErrors = ValidateSettings();
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
                LoggingService.Instance.LogInfo($"Team warning settings saved via MVVM: {summary}");
                
                // Close window
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error saving team warning settings via MVVM", ex);
                MessageBox.Show($"Fehler beim Speichern der Einstellungen: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCancel()
        {
            try
            {
                DialogResult = false;
                LoggingService.Instance.LogInfo("Team warning settings cancelled via MVVM");
                
                // Close window
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error cancelling team warning settings", ex);
            }
        }

        private void ExecuteApplyPreset(TeamWarningItemViewModel item, PresetInfo preset)
        {
            try
            {
                if (preset != null && item != null)
                {
                    item.FirstWarningMinutes = preset.Warning1;
                    item.SecondWarningMinutes = preset.Warning2;
                    
                    LoggingService.Instance.LogInfo($"Applied preset '{preset.Name}' to team {item.TeamName}: {preset.Warning1}/{preset.Warning2}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying preset via MVVM", ex);
            }
        }

        #endregion

        #region Validation

        private bool ValidateAllSettings()
        {
            return !ValidateSettings().Any();
        }

        private List<string> ValidateSettings()
        {
            var errors = new List<string>();

            try
            {
                foreach (var item in TeamWarningItems)
                {
                    if (item.FirstWarningMinutes >= item.SecondWarningMinutes)
                    {
                        errors.Add($"{item.TeamName}: Erste Warnung muss kleiner als zweite Warnung sein");
                    }

                    if (item.FirstWarningMinutes < 1 || item.FirstWarningMinutes > 120)
                    {
                        errors.Add($"{item.TeamName}: Erste Warnung muss zwischen 1 und 120 Minuten liegen");
                    }

                    if (item.SecondWarningMinutes < 2 || item.SecondWarningMinutes > 120)
                    {
                        errors.Add($"{item.TeamName}: Zweite Warnung muss zwischen 2 und 120 Minuten liegen");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error validating settings", ex);
                errors.Add("Fehler bei der Validierung der Einstellungen");
            }

            return errors;
        }

        #endregion
    }

    /// <summary>
    /// ViewModel für ein einzelnes Team-Warning-Item
    /// </summary>
    public class TeamWarningItemViewModel : BaseViewModel
    {
        private int _firstWarningMinutes = 10;
        private int _secondWarningMinutes = 20;

        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string HundName { get; set; } = string.Empty;
        public string TeamTypeDisplayName { get; set; } = string.Empty;
        public string TeamTypeShortName { get; set; } = string.Empty;
        public string TeamTypeColorHex { get; set; } = "#F57C00";

        public int FirstWarningMinutes
        {
            get => _firstWarningMinutes;
            set => SetProperty(ref _firstWarningMinutes, value);
        }

        public int SecondWarningMinutes
        {
            get => _secondWarningMinutes;
            set => SetProperty(ref _secondWarningMinutes, value);
        }

        // Preset Command (will be set by parent ViewModel)
        public ICommand? ApplyPresetCommand { get; set; }

        // Available presets
        public List<PresetInfo> AvailablePresets { get; } = new List<PresetInfo>
        {
            new PresetInfo { Name = "Schnell", Warning1 = 5, Warning2 = 10 },
            new PresetInfo { Name = "Normal", Warning1 = 10, Warning2 = 20 },
            new PresetInfo { Name = "Erweitert", Warning1 = 15, Warning2 = 30 },
            new PresetInfo { Name = "Lang", Warning1 = 20, Warning2 = 40 }
        };
    }

    /// <summary>
    /// Helper class for team warning settings
    /// </summary>
    public class TeamWarningSettings
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int FirstWarningMinutes { get; set; }
        public int SecondWarningMinutes { get; set; }
    }

    /// <summary>
    /// Preset information for quick settings
    /// </summary>
    public class PresetInfo
    {
        public string Name { get; set; } = string.Empty;
        public int Warning1 { get; set; }
        public int Warning2 { get; set; }
        public string DisplayText => $"{Name}\n{Warning1}/{Warning2}";
    }
}
