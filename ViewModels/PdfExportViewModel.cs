using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    /// <summary>
    /// ViewModel für das PdfExportWindow - MVVM-Implementation v1.9.0
    /// PDF-Export mit vollständiger Orange-Design-Integration
    /// </summary>
    public class PdfExportViewModel : BaseViewModel, IDisposable
    {
        private readonly EinsatzData _einsatzData;
        private readonly List<Team> _teams;

        // Export Options Properties
        private bool _includeEinsatzNummer = true;
        private string _einsatzNummer = string.Empty;
        private bool _includeAlarmierendeOrg = true;
        private string _alarmierendeOrganisation = string.Empty;
        private bool _includeStaffelName = true;
        private string _staffelName = string.Empty;
        private bool _includeLogo = false;
        private string _logoPath = string.Empty;
        private bool _includeAlarmierungsZeit = true;
        private DateTime? _alarmierungsDatum;
        private string _alarmierungsZeit = "00:00";
        private bool _includeTimeline = true;

        // UI State Properties
        private BitmapImage? _logoPreview;
        private Visibility _logoPreviewVisibility = Visibility.Collapsed;
        private bool _isExporting = false;
        private string _windowTitle = "📄 PDF-Export - Einsatzdokumentation";

        public PdfExportViewModel(EinsatzData einsatzData, List<Team> teams)
        {
            _einsatzData = einsatzData ?? throw new ArgumentNullException(nameof(einsatzData));
            _teams = teams ?? throw new ArgumentNullException(nameof(teams));

            InitializeCommands();
            InitializeFields();

            LoggingService.Instance.LogInfo("PdfExportViewModel initialized with MVVM pattern v1.9.0");
        }

        #region Properties

        #region Export Options

        /// <summary>
        /// Soll die Einsatznummer im PDF enthalten sein?
        /// </summary>
        public bool IncludeEinsatzNummer
        {
            get => _includeEinsatzNummer;
            set => SetProperty(ref _includeEinsatzNummer, value);
        }

        /// <summary>
        /// Einsatznummer/Aktenzeichen
        /// </summary>
        public string EinsatzNummer
        {
            get => _einsatzNummer;
            set => SetProperty(ref _einsatzNummer, value);
        }

        /// <summary>
        /// Soll die alarmierende Organisation im PDF enthalten sein?
        /// </summary>
        public bool IncludeAlarmierendeOrg
        {
            get => _includeAlarmierendeOrg;
            set => SetProperty(ref _includeAlarmierendeOrg, value);
        }

        /// <summary>
        /// Alarmierende Organisation (z.B. Polizei, Feuerwehr)
        /// </summary>
        public string AlarmierendeOrganisation
        {
            get => _alarmierendeOrganisation;
            set => SetProperty(ref _alarmierendeOrganisation, value);
        }

        /// <summary>
        /// Soll der Staffelname im PDF enthalten sein?
        /// </summary>
        public bool IncludeStaffelName
        {
            get => _includeStaffelName;
            set => SetProperty(ref _includeStaffelName, value);
        }

        /// <summary>
        /// Name der Rettungshunde-Staffel
        /// </summary>
        public string StaffelName
        {
            get => _staffelName;
            set => SetProperty(ref _staffelName, value);
        }

        /// <summary>
        /// Soll das Staffel-Logo im PDF enthalten sein?
        /// </summary>
        public bool IncludeLogo
        {
            get => _includeLogo;
            set
            {
                if (SetProperty(ref _includeLogo, value))
                {
                    if (!value)
                    {
                        LogoPath = string.Empty;
                        LogoPreview = null;
                        LogoPreviewVisibility = Visibility.Collapsed;
                    }
                }
            }
        }

        /// <summary>
        /// Pfad zur Logo-Datei
        /// </summary>
        public string LogoPath
        {
            get => _logoPath;
            set => SetProperty(ref _logoPath, value);
        }

        /// <summary>
        /// Soll die Alarmierungszeit im PDF enthalten sein?
        /// </summary>
        public bool IncludeAlarmierungsZeit
        {
            get => _includeAlarmierungsZeit;
            set => SetProperty(ref _includeAlarmierungsZeit, value);
        }

        /// <summary>
        /// Datum der Alarmierung
        /// </summary>
        public DateTime? AlarmierungsDatum
        {
            get => _alarmierungsDatum;
            set => SetProperty(ref _alarmierungsDatum, value);
        }

        /// <summary>
        /// Uhrzeit der Alarmierung (HH:mm Format)
        /// </summary>
        public string AlarmierungsZeit
        {
            get => _alarmierungsZeit;
            set => SetProperty(ref _alarmierungsZeit, value);
        }

        /// <summary>
        /// Soll die Einsatz-Chronologie im PDF enthalten sein?
        /// </summary>
        public bool IncludeTimeline
        {
            get => _includeTimeline;
            set => SetProperty(ref _includeTimeline, value);
        }

        #endregion

        #region UI State Properties

        /// <summary>
        /// Logo-Vorschau-Bild
        /// </summary>
        public BitmapImage? LogoPreview
        {
            get => _logoPreview;
            set => SetProperty(ref _logoPreview, value);
        }

        /// <summary>
        /// Sichtbarkeit der Logo-Vorschau
        /// </summary>
        public Visibility LogoPreviewVisibility
        {
            get => _logoPreviewVisibility;
            set => SetProperty(ref _logoPreviewVisibility, value);
        }

        /// <summary>
        /// Ist der Export-Vorgang aktiv?
        /// </summary>
        public bool IsExporting
        {
            get => _isExporting;
            set => SetProperty(ref _isExporting, value);
        }

        /// <summary>
        /// Fenstertitel
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        #endregion

        #endregion

        #region Commands

        public ICommand SelectLogoCommand { get; private set; } = null!;
        public ICommand ExportPdfCommand { get; private set; } = null!;
        public ICommand CancelCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            SelectLogoCommand = new RelayCommand(ExecuteSelectLogo, CanExecuteSelectLogo);
            ExportPdfCommand = new RelayCommand(ExecuteExportPdf, CanExecuteExportPdf);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        #endregion

        #region Command Implementations

        private bool CanExecuteSelectLogo() => IncludeLogo && !IsExporting;

        private void ExecuteSelectLogo()
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "Bilddateien (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Alle Dateien (*.*)|*.*",
                    Title = "Staffel-Logo auswählen",
                    Multiselect = false
                };

                var result = Application.Current.Dispatcher.Invoke(() => openDialog.ShowDialog());
                
                if (result == true)
                {
                    LogoPath = openDialog.FileName;
                    LoadLogoPreview(LogoPath);
                    
                    LoggingService.Instance.LogInfo($"Logo selected via MVVM: {LogoPath}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error selecting logo via MVVM", ex);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Fehler beim Laden des Logos:\n{ex.Message}", 
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private bool CanExecuteExportPdf() => !IsExporting;

        private async void ExecuteExportPdf()
        {
            try
            {
                // Validierung
                if (!ValidateInput()) return;

                // Speicher-Dialog
                var saveDialog = new SaveFileDialog
                {
                    Filter = "PDF-Dateien (*.pdf)|*.pdf",
                    FileName = $"Einsatzbericht_{EinsatzNummer?.Replace("/", "-").Replace(":", "-")}.pdf",
                    Title = "PDF-Export speichern"
                };

                var dialogResult = Application.Current.Dispatcher.Invoke(() => saveDialog.ShowDialog());
                
                if (dialogResult != true) return;

                // Export starten
                IsExporting = true;
                WindowTitle = "📄 PDF wird erstellt...";
                
                // Erstelle Export-Optionen
                var options = CreateExportOptions();
                
                // Speichere Einstellungen für zukünftige Verwendung
                SaveSettingsToEinsatzData(options);

                // Exportiere PDF (async um UI nicht zu blockieren)
                await System.Threading.Tasks.Task.Run(() =>
                {
                    PdfExportService.Instance.ExportToPdf(saveDialog.FileName, _einsatzData, _teams, options);
                });
                
                // Export erfolgreich
                IsExporting = false;
                WindowTitle = "📄 PDF-Export - Einsatzdokumentation";

                // Erfolgs-Dialog
                var openResult = Application.Current.Dispatcher.Invoke(() =>
                {
                    return MessageBox.Show(
                        $"PDF-Export erfolgreich!\n\nDatei: {saveDialog.FileName}\n\nMöchten Sie die PDF-Datei jetzt öffnen?",
                        "Export erfolgreich",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);
                });

                if (openResult == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveDialog.FileName,
                        UseShellExecute = true
                    });
                }

                // Fenster schließen
                ExportSuccessful?.Invoke();
                
                LoggingService.Instance.LogInfo($"PDF export completed successfully via MVVM: {saveDialog.FileName}");
            }
            catch (Exception ex)
            {
                IsExporting = false;
                WindowTitle = "📄 PDF-Export - Einsatzdokumentation";
                
                LoggingService.Instance.LogError("Error during PDF export via MVVM", ex);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Fehler beim PDF-Export:\n\n{ex.Message}", 
                        "Export-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                UpdateCommandStates();
            }
        }

        private void ExecuteCancel()
        {
            try
            {
                ExportCancelled?.Invoke();
                LoggingService.Instance.LogInfo("PDF export cancelled via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling cancel via MVVM", ex);
            }
        }

        #endregion

        #region Private Methods

        private void InitializeFields()
        {
            try
            {
                // Einsatznummer aus EinsatzData
                if (!string.IsNullOrEmpty(_einsatzData.EinsatzNummer))
                {
                    EinsatzNummer = _einsatzData.EinsatzNummer;
                }
                else
                {
                    // Generiere automatische Einsatznummer: JJJJ-MM-TT-NNN
                    EinsatzNummer = $"{DateTime.Now:yyyy-MM-dd}-{_einsatzData.EinsatzDatum.Hour:D2}{_einsatzData.EinsatzDatum.Minute:D2}";
                }

                // Alarmierende Organisation
                if (!string.IsNullOrEmpty(_einsatzData.Alarmiert))
                {
                    AlarmierendeOrganisation = _einsatzData.Alarmiert;
                }

                // Staffelname
                if (!string.IsNullOrEmpty(_einsatzData.StaffelName))
                {
                    StaffelName = _einsatzData.StaffelName;
                }

                // Logo
                if (!string.IsNullOrEmpty(_einsatzData.StaffelLogoPfad) && File.Exists(_einsatzData.StaffelLogoPfad))
                {
                    LogoPath = _einsatzData.StaffelLogoPfad;
                    IncludeLogo = true;
                    LoadLogoPreview(LogoPath);
                }

                // Alarmierungszeit
                if (_einsatzData.AlarmierungsZeit.HasValue)
                {
                    AlarmierungsDatum = _einsatzData.AlarmierungsZeit.Value.Date;
                    AlarmierungsZeit = _einsatzData.AlarmierungsZeit.Value.ToString("HH:mm");
                }
                else
                {
                    // Default: Einsatzdatum
                    AlarmierungsDatum = _einsatzData.EinsatzDatum.Date;
                    AlarmierungsZeit = _einsatzData.EinsatzDatum.ToString("HH:mm");
                }
                
                LoggingService.Instance.LogInfo("PDF export fields initialized via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing PDF export fields via MVVM", ex);
            }
        }

        private void LoadLogoPreview(string logoPath)
        {
            try
            {
                if (File.Exists(logoPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(logoPath, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze(); // Für Thread-Safety
                    
                    LogoPreview = bitmap;
                    LogoPreviewVisibility = Visibility.Visible;
                    
                    LoggingService.Instance.LogInfo($"Logo preview loaded via MVVM: {logoPath}");
                }
                else
                {
                    LogoPreview = null;
                    LogoPreviewVisibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"Error loading logo preview via MVVM: {ex.Message}");
                LogoPreview = null;
                LogoPreviewVisibility = Visibility.Collapsed;
            }
        }

        private bool ValidateInput()
        {
            try
            {
                if (IncludeEinsatzNummer && string.IsNullOrWhiteSpace(EinsatzNummer))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Bitte geben Sie eine Einsatznummer ein oder deaktivieren Sie die Option.", 
                            "Einsatznummer fehlt", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                    return false;
                }

                if (IncludeLogo && string.IsNullOrWhiteSpace(LogoPath))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Bitte wählen Sie ein Logo aus oder deaktivieren Sie die Option.", 
                            "Logo fehlt", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                    return false;
                }

                if (IncludeAlarmierungsZeit && !AlarmierungsDatum.HasValue)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Bitte wählen Sie ein Alarmierungsdatum aus oder deaktivieren Sie die Option.", 
                            "Alarmierungsdatum fehlt", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                    return false;
                }

                if (IncludeAlarmierungsZeit && !TimeSpan.TryParse(AlarmierungsZeit, out _))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Bitte geben Sie eine gültige Uhrzeit im Format HH:mm ein.", 
                            "Ungültige Uhrzeit", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error validating input via MVVM", ex);
                return false;
            }
        }

        private PdfExportOptions CreateExportOptions()
        {
            return new PdfExportOptions
            {
                IncludeEinsatzNummer = IncludeEinsatzNummer,
                EinsatzNummer = EinsatzNummer?.Trim() ?? string.Empty,
                
                IncludeAlarmierendeOrg = IncludeAlarmierendeOrg,
                AlarmierendeOrganisation = AlarmierendeOrganisation?.Trim() ?? string.Empty,
                
                IncludeStaffelName = IncludeStaffelName,
                StaffelName = StaffelName?.Trim() ?? string.Empty,
                
                IncludeLogo = IncludeLogo,
                LogoPath = LogoPath ?? string.Empty,
                
                IncludeAlarmierungsZeit = IncludeAlarmierungsZeit,
                AlarmierungsZeit = ParseAlarmierungsZeit(),
                
                IncludeTimeline = IncludeTimeline
            };
        }

        private DateTime? ParseAlarmierungsZeit()
        {
            try
            {
                if (AlarmierungsDatum.HasValue && TimeSpan.TryParse(AlarmierungsZeit, out var time))
                {
                    return AlarmierungsDatum.Value.Date.Add(time);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"Error parsing Alarmierungszeit via MVVM: {ex.Message}");
            }
            
            return null;
        }

        private void SaveSettingsToEinsatzData(PdfExportOptions options)
        {
            try
            {
                if (options.IncludeEinsatzNummer)
                {
                    _einsatzData.EinsatzNummer = options.EinsatzNummer;
                }
                
                if (options.IncludeAlarmierendeOrg)
                {
                    _einsatzData.Alarmiert = options.AlarmierendeOrganisation;
                }
                
                if (options.IncludeStaffelName)
                {
                    _einsatzData.StaffelName = options.StaffelName;
                }
                
                if (options.IncludeLogo)
                {
                    _einsatzData.StaffelLogoPfad = options.LogoPath;
                }
                
                if (options.IncludeAlarmierungsZeit && options.AlarmierungsZeit.HasValue)
                {
                    _einsatzData.AlarmierungsZeit = options.AlarmierungsZeit;
                }
                
                LoggingService.Instance.LogInfo("Settings saved to EinsatzData via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"Error saving settings to EinsatzData via MVVM: {ex.Message}");
            }
        }

        private void UpdateCommandStates()
        {
            ((RelayCommand)SelectLogoCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ExportPdfCommand).RaiseCanExecuteChanged();
        }

        #endregion

        #region Events

        /// <summary>
        /// Event wird ausgelöst, wenn der Export erfolgreich abgeschlossen wurde
        /// </summary>
        public event Action? ExportSuccessful;

        /// <summary>
        /// Event wird ausgelöst, wenn der Export abgebrochen wurde
        /// </summary>
        public event Action? ExportCancelled;

        #endregion

        #region IDisposable Implementation

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Clean up managed resources
                    LogoPreview = null;
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
