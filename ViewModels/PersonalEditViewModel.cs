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
            
            LoggingService.Instance.LogInfo($"PersonalEditViewModel initialized - Mode: {(_isEditMode ? "Edit" : "New")}");
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
            set => SetProperty(ref _isHundefuehrer, value);
        }

        public bool IsHelfer
        {
            get => _isHelfer;
            set => SetProperty(ref _isHelfer, value);
        }

        public bool IsFuehrungsassistent
        {
            get => _isFuehrungsassistent;
            set => SetProperty(ref _isFuehrungsassistent, value);
        }

        public bool IsGruppenfuehrer
        {
            get => _isGruppenfuehrer;
            set => SetProperty(ref _isGruppenfuehrer, value);
        }

        public bool IsZugfuehrer
        {
            get => _isZugfuehrer;
            set => SetProperty(ref _isZugfuehrer, value);
        }

        public bool IsVerbandsfuehrer
        {
            get => _isVerbandsfuehrer;
            set => SetProperty(ref _isVerbandsfuehrer, value);
        }

        public bool IsDrohnenpilot
        {
            get => _isDrohnenpilot;
            set => SetProperty(ref _isDrohnenpilot, value);
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
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
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
                // Letzte Validierung
                if (!ValidateForm())
                {
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
                }

                LoggingService.Instance.LogInfo($"Personal saved via MVVM: {_personalEntry.FullName} - Mode: {(_isEditMode ? "Edit" : "New")}");

                // Dialog erfolgreich schließen
                if (window != null)
                {
                    window.DialogResult = true;
                    window.Close();
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

            if (!HasAnySkillSelected())
            {
                errors.Add("Mindestens eine Fähigkeit muss ausgewählt werden");
            }

            ValidationMessage = errors.Count > 0 ? string.Join(", ", errors) : string.Empty;
            IsFormValid = errors.Count == 0;

            return IsFormValid;
        }

        private bool HasAnySkillSelected()
        {
            return IsHundefuehrer || IsHelfer || IsFuehrungsassistent || 
                   IsGruppenfuehrer || IsZugfuehrer || IsVerbandsfuehrer || IsDrohnenpilot;
        }

        #endregion
    }
}
