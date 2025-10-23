using System;
using System.Windows;
using System.Windows.Input;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    /// <summary>
    /// ViewModel für das PersonalEditWindow - MVVM-Implementation v1.9.0
    /// Verwaltet die Bearbeitung und Erstellung von Personal-Einträgen
    /// </summary>
    public class PersonalEditViewModel : BaseViewModel
    {
        private PersonalEntry _personalEntry;
        private readonly bool _isEditMode;
        private string _windowTitle = "Personal bearbeiten";
        
        // Form Properties
        private string _vorname = string.Empty;
        private string _nachname = string.Empty;
        private string _notizen = string.Empty;
        private bool _isActive = true;
        
        // Skills Properties
        private bool _isHundefuehrer;
        private bool _isHelfer;
        private bool _isFuehrungsassistent;
        private bool _isGruppenfuehrer;
        private bool _isZugfuehrer;
        private bool _isVerbandsfuehrer;
        private bool _isDrohnenpilot;
        
        // Validation Properties
        private bool _isFormValid;
        private string _validationMessage = string.Empty;

        public PersonalEditViewModel(PersonalEntry? existingEntry = null)
        {
            _isEditMode = existingEntry != null;
            _personalEntry = existingEntry ?? new PersonalEntry();
            
            InitializeCommands();
            LoadData();
            ValidateForm();
            
            WindowTitle = _isEditMode ? "Personal bearbeiten" : "Neues Personal";
            
            LoggingService.Instance.LogInfo($"PersonalEditViewModel initialized - Mode: {(_isEditMode ? "Edit" : "New")}, Entry ID: {_personalEntry.Id}");
        }

        #region Properties

        /// <summary>
        /// Das bearbeitete PersonalEntry-Objekt
        /// </summary>
        public PersonalEntry PersonalEntry => _personalEntry;

        /// <summary>
        /// Fenster-Titel basierend auf Edit/New-Modus
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        /// <summary>
        /// Vorname des Personals
        /// </summary>
        public string Vorname
        {
            get => _vorname;
            set
            {
                if (SetProperty(ref _vorname, value))
                {
                    ValidateForm();
                }
            }
        }

        /// <summary>
        /// Nachname des Personals
        /// </summary>
        public string Nachname
        {
            get => _nachname;
            set
            {
                if (SetProperty(ref _nachname, value))
                {
                    ValidateForm();
                }
            }
        }

        /// <summary>
        /// Notizen zum Personal
        /// </summary>
        public string Notizen
        {
            get => _notizen;
            set => SetProperty(ref _notizen, value);
        }

        /// <summary>
        /// Ist das Personal aktiv?
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        #region Skills Properties

        public bool IsHundefuehrer
        {
            get => _isHundefuehrer;
            set 
            { 
                if (SetProperty(ref _isHundefuehrer, value))
                {
                    ValidateForm();
                }
            }
        }

        public bool IsHelfer
        {
            get => _isHelfer;
            set 
            { 
                if (SetProperty(ref _isHelfer, value))
                {
                    ValidateForm();
                }
            }
        }

        public bool IsFuehrungsassistent
        {
            get => _isFuehrungsassistent;
            set 
            { 
                if (SetProperty(ref _isFuehrungsassistent, value))
                {
                    ValidateForm();
                }
            }
        }

        public bool IsGruppenfuehrer
        {
            get => _isGruppenfuehrer;
            set 
            { 
                if (SetProperty(ref _isGruppenfuehrer, value))
                {
                    ValidateForm();
                }
            }
        }

        public bool IsZugfuehrer
        {
            get => _isZugfuehrer;
            set 
            { 
                if (SetProperty(ref _isZugfuehrer, value))
                {
                    ValidateForm();
                }
            }
        }

        public bool IsVerbandsfuehrer
        {
            get => _isVerbandsfuehrer;
            set 
            { 
                if (SetProperty(ref _isVerbandsfuehrer, value))
                {
                    ValidateForm();
                }
            }
        }

        public bool IsDrohnenpilot
        {
            get => _isDrohnenpilot;
            set 
            { 
                if (SetProperty(ref _isDrohnenpilot, value))
                {
                    ValidateForm();
                }
            }
        }

        #endregion

        #region Validation Properties

        /// <summary>
        /// Ist das Formular gültig und kann gespeichert werden?
        /// </summary>
        public bool IsFormValid
        {
            get => _isFormValid;
            set
            {
                if (SetProperty(ref _isFormValid, value))
                {
                    // Fix: Correct cast to RelayCommand<Window>
                    ((RelayCommand<Window>)SaveCommand).RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Aktuelle Validierungsnachricht
        /// </summary>
        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        /// <summary>
        /// Gibt es Validierungsfehler?
        /// </summary>
        public bool HasValidationError => !string.IsNullOrEmpty(ValidationMessage);

        #endregion

        #endregion

        #region Commands

        public ICommand SaveCommand { get; private set; } = null!;
        public ICommand CancelCommand { get; private set; } = null!;
        public ICommand ResetFormCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            SaveCommand = new RelayCommand<Window>(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand<Window>(ExecuteCancel);
            ResetFormCommand = new RelayCommand(ExecuteResetForm, CanExecuteResetForm);
        }

        #endregion

        #region Command Implementations

        private bool CanExecuteSave(Window? window) => IsFormValid;

        private void ExecuteSave(Window? window)
        {
            try
            {
                LoggingService.Instance.LogInfo($"=== EXECUTE SAVE CALLED ===");
                LoggingService.Instance.LogInfo($"Window parameter: {window?.GetType().Name ?? "NULL"}");
                LoggingService.Instance.LogInfo($"Starting save process - IsEditMode: {_isEditMode}, PersonalEntry ID: {_personalEntry.Id}");

                // Letzte Validierung
                if (!ValidateForm())
                {
                    LoggingService.Instance.LogWarning("Save cancelled due to validation errors");
                    return;
                }

                // Daten in PersonalEntry übertragen
                _personalEntry.Vorname = Vorname.Trim();
                _personalEntry.Nachname = Nachname.Trim();
                _personalEntry.Notizen = Notizen.Trim();
                _personalEntry.IsActive = IsActive;

                // Skills zusammenbauen
                PersonalSkills skills = PersonalSkills.None;
                if (IsHundefuehrer) skills |= PersonalSkills.Hundefuehrer;
                if (IsHelfer) skills |= PersonalSkills.Helfer;
                if (IsFuehrungsassistent) skills |= PersonalSkills.Fuehrungsassistent;
                if (IsGruppenfuehrer) skills |= PersonalSkills.Gruppenfuehrer;
                if (IsZugfuehrer) skills |= PersonalSkills.Zugfuehrer;
                if (IsVerbandsfuehrer) skills |= PersonalSkills.Verbandsfuehrer;
                if (IsDrohnenpilot) skills |= PersonalSkills.Drohnenpilot;

                _personalEntry.Skills = skills;

                // Neue IDs für neue Einträge generieren
                if (!_isEditMode && string.IsNullOrEmpty(_personalEntry.Id))
                {
                    _personalEntry.Id = Guid.NewGuid().ToString();
                    LoggingService.Instance.LogInfo($"Generated new ID for PersonalEntry: {_personalEntry.Id}");
                }

                LoggingService.Instance.LogInfo($"PersonalEntry prepared for save: ID={_personalEntry.Id}, Name={_personalEntry.FullName}, Skills={skills}");

                // WICHTIG: Das PersonalEntry-Objekt ist jetzt fertig und wird an das aufrufende Fenster zurückgegeben
                // Die tatsächliche Speicherung in der MasterData erfolgt durch das aufrufende ViewModel
                
                LoggingService.Instance.LogInfo($"Personal data ready for return to caller - Mode: {(_isEditMode ? "Edit" : "New")}");

                // Dialog erfolgreich schließen
                if (window != null)
                {
                    LoggingService.Instance.LogInfo("Setting DialogResult to true and closing window");
                    window.DialogResult = true;
                    window.Close();
                    LoggingService.Instance.LogInfo("Window closed successfully");
                }
                else
                {
                    LoggingService.Instance.LogError("Window parameter is NULL - cannot close window!");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error saving personal via MVVM", ex);
                ValidationMessage = $"Fehler beim Speichern: {ex.Message}";
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Fehler beim Speichern:\n{ex.Message}", 
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void ExecuteCancel(Window? window)
        {
            try
            {
                LoggingService.Instance.LogInfo($"Personal edit cancelled via MVVM - Mode: {(_isEditMode ? "Edit" : "New")}");
                
                if (window != null)
                {
                    window.DialogResult = false;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error cancelling personal edit", ex);
            }
        }

        private bool CanExecuteResetForm()
        {
            return !string.IsNullOrWhiteSpace(Vorname) || 
                   !string.IsNullOrWhiteSpace(Nachname) || 
                   !string.IsNullOrWhiteSpace(Notizen) ||
                   HasAnySkillSelected();
        }

        private void ExecuteResetForm()
        {
            try
            {
                var result = Application.Current.Dispatcher.Invoke(() =>
                {
                    return MessageBox.Show("Möchten Sie alle Eingaben zurücksetzen?", 
                        "Formular zurücksetzen", MessageBoxButton.YesNo, MessageBoxImage.Question);
                });

                if (result == MessageBoxResult.Yes)
                {
                    ResetToDefaults();
                    LoggingService.Instance.LogInfo("Personal edit form reset via MVVM");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error resetting personal edit form", ex);
            }
        }

        #endregion

        #region Private Methods

        private void LoadData()
        {
            if (_isEditMode && _personalEntry != null)
            {
                Vorname = _personalEntry.Vorname;
                Nachname = _personalEntry.Nachname;
                Notizen = _personalEntry.Notizen;
                IsActive = _personalEntry.IsActive;

                // Skills laden
                IsHundefuehrer = _personalEntry.Skills.HasFlag(PersonalSkills.Hundefuehrer);
                IsHelfer = _personalEntry.Skills.HasFlag(PersonalSkills.Helfer);
                IsFuehrungsassistent = _personalEntry.Skills.HasFlag(PersonalSkills.Fuehrungsassistent);
                IsGruppenfuehrer = _personalEntry.Skills.HasFlag(PersonalSkills.Gruppenfuehrer);
                IsZugfuehrer = _personalEntry.Skills.HasFlag(PersonalSkills.Zugfuehrer);
                IsVerbandsfuehrer = _personalEntry.Skills.HasFlag(PersonalSkills.Verbandsfuehrer);
                IsDrohnenpilot = _personalEntry.Skills.HasFlag(PersonalSkills.Drohnenpilot);

                LoggingService.Instance.LogInfo($"Loaded existing PersonalEntry data: {_personalEntry.FullName}, Skills: {_personalEntry.Skills}");
            }
            else
            {
                ResetToDefaults();
            }
        }

        private void ResetToDefaults()
        {
            Vorname = string.Empty;
            Nachname = string.Empty;
            Notizen = string.Empty;
            IsActive = true;
            
            // Alle Skills zurücksetzen
            IsHundefuehrer = false;
            IsHelfer = false;
            IsFuehrungsassistent = false;
            IsGruppenfuehrer = false;
            IsZugfuehrer = false;
            IsVerbandsfuehrer = false;
            IsDrohnenpilot = false;
            
            ValidateForm();
        }

        private bool ValidateForm()
        {
            var errors = new System.Collections.Generic.List<string>();

            if (string.IsNullOrWhiteSpace(Vorname))
            {
                errors.Add("Vorname ist erforderlich");
            }

            if (string.IsNullOrWhiteSpace(Nachname))
            {
                errors.Add("Nachname ist erforderlich");
            }

            var hasSkills = HasAnySkillSelected();
            LoggingService.Instance.LogInfo($"Validation: HasAnySkillSelected = {hasSkills}, Skills: HF={IsHundefuehrer}, H={IsHelfer}, FA={IsFuehrungsassistent}, GF={IsGruppenfuehrer}, ZF={IsZugfuehrer}, VF={IsVerbandsfuehrer}, DP={IsDrohnenpilot}");

            if (!hasSkills)
            {
                errors.Add("Mindestens eine Fähigkeit muss ausgewählt werden");
            }

            ValidationMessage = errors.Count > 0 ? string.Join(", ", errors) : string.Empty;
            IsFormValid = errors.Count == 0;

            LoggingService.Instance.LogInfo($"Validation result: IsFormValid={IsFormValid}, ValidationMessage='{ValidationMessage}'");

            return IsFormValid;
        }

        private bool HasAnySkillSelected()
        {
            var result = IsHundefuehrer || IsHelfer || IsFuehrungsassistent || 
                   IsGruppenfuehrer || IsZugfuehrer || IsVerbandsfuehrer || IsDrohnenpilot;
            
            LoggingService.Instance.LogInfo($"HasAnySkillSelected: result={result}, individual skills: HF={IsHundefuehrer}, H={IsHelfer}, FA={IsFuehrungsassistent}, GF={IsGruppenfuehrer}, ZF={IsZugfuehrer}, VF={IsVerbandsfuehrer}, DP={IsDrohnenpilot}");
            
            return result;
        }

        #endregion
    }
}
