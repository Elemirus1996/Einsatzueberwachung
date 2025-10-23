using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    /// <summary>
    /// ViewModel für TeamTypeSelectionWindow - MVVM-Implementation v1.9.0
    /// Multi-Select Team Type Selection mit Orange-Design-Integration
    /// </summary>
    public class TeamTypeSelectionViewModel : BaseViewModel
    {
        private MultipleTeamTypes _selectedMultipleTeamTypes;
        private string _selectedTypesDisplayText = "Keine Auswahl";
        private bool _isOkButtonEnabled = false;
        private string _windowTitle = "Team-Spezialisierungen auswählen";
        private bool? _dialogResult;

        // Collections
        public ObservableCollection<TeamTypeItem> TeamTypeItems { get; } = new ObservableCollection<TeamTypeItem>();

        // Commands
        public ICommand ClearAllCommand { get; }
        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        // Events
        public event Action? RequestClose;

        // Properties
        public MultipleTeamTypes SelectedMultipleTeamTypes
        {
            get => _selectedMultipleTeamTypes;
            set
            {
                SetProperty(ref _selectedMultipleTeamTypes, value);
                UpdateSelectedTypesDisplay();
                UpdateOkButtonState();
            }
        }

        public string SelectedTypesDisplayText
        {
            get => _selectedTypesDisplayText;
            set => SetProperty(ref _selectedTypesDisplayText, value);
        }

        public bool IsOkButtonEnabled
        {
            get => _isOkButtonEnabled;
            set => SetProperty(ref _isOkButtonEnabled, value);
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        // Dialog Result with proper PropertyChanged notification
        public bool? DialogResult
        {
            get => _dialogResult;
            private set => SetProperty(ref _dialogResult, value);
        }

        public TeamTypeSelectionViewModel(MultipleTeamTypes? currentSelection = null)
        {
            _selectedMultipleTeamTypes = currentSelection ?? new MultipleTeamTypes();

            // Initialize commands
            ClearAllCommand = new RelayCommand(ExecuteClearAll);
            OkCommand = new RelayCommand(ExecuteOk, CanExecuteOk);
            CancelCommand = new RelayCommand(ExecuteCancel);

            // Load team types
            LoadTeamTypes();

            LoggingService.Instance.LogInfo("TeamTypeSelectionViewModel initialized with MVVM pattern v1.9.0");
        }

        private void LoadTeamTypes()
        {
            try
            {
                TeamTypeItems.Clear();

                // Get all types EXCEPT Allgemein - exclude it completely
                // So that only specific specializations from master data are shown
                var teamTypes = TeamTypeInfo.GetAllTypes()
                    .Where(t => t.Type != TeamType.Allgemein)  // Filter out Allgemein completely
                    .OrderBy(t => t.DisplayName);  // Sort alphabetically

                foreach (var typeInfo in teamTypes)
                {
                    var teamTypeItem = new TeamTypeItem
                    {
                        TeamType = typeInfo.Type,
                        DisplayName = typeInfo.DisplayName,
                        Description = typeInfo.Description,
                        ColorHex = typeInfo.ColorHex,
                        IsSelected = _selectedMultipleTeamTypes.HasType(typeInfo.Type)
                    };

                    // Subscribe to selection changes
                    teamTypeItem.PropertyChanged += TeamTypeItem_PropertyChanged;
                    TeamTypeItems.Add(teamTypeItem);
                }

                UpdateSelectedTypesDisplay();
                UpdateOkButtonState();

                LoggingService.Instance.LogInfo($"TeamTypeSelection v1.9.0 - Created {TeamTypeItems.Count} type options (excluding Allgemein - only specific specializations shown)");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading team types", ex);
            }
        }

        private void TeamTypeItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TeamTypeItem.IsSelected) && sender is TeamTypeItem item)
            {
                try
                {
                    // Update the selected types based on checkbox changes
                    var selectedTypes = TeamTypeItems
                        .Where(t => t.IsSelected)
                        .Select(t => t.TeamType)
                        .ToHashSet();

                    _selectedMultipleTeamTypes.SelectedTypes = selectedTypes;
                    
                    OnPropertyChanged(nameof(SelectedMultipleTeamTypes));
                    UpdateSelectedTypesDisplay();
                    UpdateOkButtonState();
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("Error handling team type selection change", ex);
                }
            }
        }

        private void UpdateSelectedTypesDisplay()
        {
            try
            {
                if (!_selectedMultipleTeamTypes.SelectedTypes.Any())
                {
                    SelectedTypesDisplayText = "Keine Auswahl";
                }
                else
                {
                    SelectedTypesDisplayText = _selectedMultipleTeamTypes.DisplayName;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating selected types display", ex);
                SelectedTypesDisplayText = "Fehler beim Laden";
            }
        }

        private void UpdateOkButtonState()
        {
            IsOkButtonEnabled = _selectedMultipleTeamTypes.SelectedTypes.Any();
            ((RelayCommand)OkCommand).RaiseCanExecuteChanged();
        }

        #region Command Implementations

        private void ExecuteClearAll()
        {
            try
            {
                foreach (var item in TeamTypeItems)
                {
                    item.IsSelected = false;
                }

                LoggingService.Instance.LogInfo("All team type selections cleared");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error clearing all selections", ex);
            }
        }

        private bool CanExecuteOk()
        {
            return _selectedMultipleTeamTypes.SelectedTypes.Any();
        }

        private void ExecuteOk()
        {
            try
            {
                if (!_selectedMultipleTeamTypes.SelectedTypes.Any())
                {
                    LoggingService.Instance.LogWarning("OK attempted with no team types selected");
                    return;
                }

                DialogResult = true;
                LoggingService.Instance.LogInfo($"Team types selected: {_selectedMultipleTeamTypes.DisplayName}");
                
                // Trigger window close
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error confirming team type selection", ex);
                DialogResult = false;
            }
        }

        private void ExecuteCancel()
        {
            try
            {
                DialogResult = false;
                LoggingService.Instance.LogInfo("Team type selection cancelled");
                
                // Trigger window close
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error cancelling team type selection", ex);
            }
        }

        #endregion
    }

    /// <summary>
    /// Item class for team type selection with binding support
    /// </summary>
    public class TeamTypeItem : BaseViewModel
    {
        private bool _isSelected;

        public TeamType TeamType { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#F57C00";

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public Brush ColorBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(ColorHex));
    }
}
