using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung
{
    public partial class PdfExportWindow : Window
    {
        private readonly EinsatzData _einsatzData;
        private readonly System.Collections.Generic.List<Team> _teams;
        private string? _selectedLogoPath;

        public PdfExportWindow(EinsatzData einsatzData, System.Collections.Generic.List<Team> teams)
        {
            InitializeComponent();
            
            _einsatzData = einsatzData ?? throw new ArgumentNullException(nameof(einsatzData));
            _teams = teams ?? throw new ArgumentNullException(nameof(teams));

            // Initialisiere Felder mit vorhandenen Daten
            InitializeFields();
        }

        private void InitializeFields()
        {
            try
            {
                // Einsatznummer aus EinsatzData
                if (!string.IsNullOrEmpty(_einsatzData.EinsatzNummer))
                {
                    TxtEinsatzNummer.Text = _einsatzData.EinsatzNummer;
                }
                else
                {
                    // Generiere automatische Einsatznummer: JJJJ-MM-TT-NNN
                    TxtEinsatzNummer.Text = $"{DateTime.Now:yyyy-MM-dd}-{_einsatzData.EinsatzDatum.Hour:D2}{_einsatzData.EinsatzDatum.Minute:D2}";
                }

                // Alarmierende Organisation
                if (!string.IsNullOrEmpty(_einsatzData.Alarmiert))
                {
                    TxtAlarmierendeOrg.Text = _einsatzData.Alarmiert;
                }

                // Staffelname
                if (!string.IsNullOrEmpty(_einsatzData.StaffelName))
                {
                    TxtStaffelName.Text = _einsatzData.StaffelName;
                }

                // Logo
                if (!string.IsNullOrEmpty(_einsatzData.StaffelLogoPfad) && File.Exists(_einsatzData.StaffelLogoPfad))
                {
                    _selectedLogoPath = _einsatzData.StaffelLogoPfad;
                    TxtLogoPfad.Text = _selectedLogoPath;
                    ChkLogo.IsChecked = true;
                    LoadLogoPreview(_selectedLogoPath);
                }

                // Alarmierungszeit
                if (_einsatzData.AlarmierungsZeit.HasValue)
                {
                    DpAlarmierungsDatum.SelectedDate = _einsatzData.AlarmierungsZeit.Value.Date;
                    TxtAlarmierungsZeit.Text = _einsatzData.AlarmierungsZeit.Value.ToString("HH:mm");
                }
                else
                {
                    // Default: Einsatzdatum
                    DpAlarmierungsDatum.SelectedDate = _einsatzData.EinsatzDatum.Date;
                    TxtAlarmierungsZeit.Text = _einsatzData.EinsatzDatum.ToString("HH:mm");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing PDF export fields", ex);
            }
        }

        private void BtnSelectLogo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "Bilddateien (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Alle Dateien (*.*)|*.*",
                    Title = "Staffel-Logo auswählen",
                    Multiselect = false
                };

                if (openDialog.ShowDialog() == true)
                {
                    _selectedLogoPath = openDialog.FileName;
                    TxtLogoPfad.Text = _selectedLogoPath;
                    
                    // Zeige Vorschau
                    LoadLogoPreview(_selectedLogoPath);
                    
                    LoggingService.Instance.LogInfo($"Logo selected: {_selectedLogoPath}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error selecting logo", ex);
                MessageBox.Show($"Fehler beim Laden des Logos:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    
                    ImgLogoPreview.Source = bitmap;
                    ImgLogoPreview.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"Error loading logo preview: {ex.Message}");
                ImgLogoPreview.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validierung
                if (ChkEinsatzNummer.IsChecked == true && string.IsNullOrWhiteSpace(TxtEinsatzNummer.Text))
                {
                    MessageBox.Show("Bitte geben Sie eine Einsatznummer ein oder deaktivieren Sie die Option.", 
                        "Einsatznummer fehlt", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtEinsatzNummer.Focus();
                    return;
                }

                if (ChkLogo.IsChecked == true && string.IsNullOrWhiteSpace(_selectedLogoPath))
                {
                    MessageBox.Show("Bitte wählen Sie ein Logo aus oder deaktivieren Sie die Option.", 
                        "Logo fehlt", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Speicher-Dialog
                var saveDialog = new SaveFileDialog
                {
                    Filter = "PDF-Dateien (*.pdf)|*.pdf",
                    FileName = $"Einsatzbericht_{TxtEinsatzNummer.Text?.Replace("/", "-").Replace(":", "-")}.pdf",
                    Title = "PDF-Export speichern"
                };

                if (saveDialog.ShowDialog() != true)
                {
                    return;
                }

                // Erstelle Export-Optionen
                var options = new PdfExportOptions
                {
                    IncludeEinsatzNummer = ChkEinsatzNummer.IsChecked == true,
                    EinsatzNummer = TxtEinsatzNummer.Text?.Trim() ?? string.Empty,
                    
                    IncludeAlarmierendeOrg = ChkAlarmierendeOrg.IsChecked == true,
                    AlarmierendeOrganisation = TxtAlarmierendeOrg.Text?.Trim() ?? string.Empty,
                    
                    IncludeStaffelName = ChkStaffelName.IsChecked == true,
                    StaffelName = TxtStaffelName.Text?.Trim() ?? string.Empty,
                    
                    IncludeLogo = ChkLogo.IsChecked == true,
                    LogoPath = _selectedLogoPath ?? string.Empty,
                    
                    IncludeAlarmierungsZeit = ChkAlarmierungsZeit.IsChecked == true,
                    AlarmierungsZeit = ParseAlarmierungsZeit(),
                    
                    IncludeTimeline = ChkTimeline.IsChecked == true
                };

                // Speichere Einstellungen für zukünftige Verwendung
                SaveSettingsToEinsatzData(options);

                // Exportiere PDF
                Cursor = System.Windows.Input.Cursors.Wait;
                
                PdfExportService.Instance.ExportToPdf(saveDialog.FileName, _einsatzData, _teams, options);
                
                Cursor = System.Windows.Input.Cursors.Arrow;

                // Erfolgs-Dialog
                var result = MessageBox.Show(
                    $"PDF-Export erfolgreich!\n\nDatei: {saveDialog.FileName}\n\nMöchten Sie die PDF-Datei jetzt öffnen?",
                    "Export erfolgreich",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveDialog.FileName,
                        UseShellExecute = true
                    });
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Cursor = System.Windows.Input.Cursors.Arrow;
                LoggingService.Instance.LogError("Error during PDF export", ex);
                MessageBox.Show($"Fehler beim PDF-Export:\n\n{ex.Message}", 
                    "Export-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DateTime? ParseAlarmierungsZeit()
        {
            try
            {
                if (DpAlarmierungsDatum.SelectedDate.HasValue)
                {
                    var date = DpAlarmierungsDatum.SelectedDate.Value;
                    var timeString = TxtAlarmierungsZeit.Text?.Trim() ?? "00:00";
                    
                    if (TimeSpan.TryParse(timeString, out var time))
                    {
                        return date.Date.Add(time);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"Error parsing Alarmierungszeit: {ex.Message}");
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
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"Error saving settings to EinsatzData: {ex.Message}");
            }
        }

        private void BtnAbbrechen_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
