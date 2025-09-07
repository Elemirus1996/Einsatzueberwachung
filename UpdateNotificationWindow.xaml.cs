using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung
{
    /// <summary>
    /// Update-Benachrichtigungs-Fenster für GitHub-Updates
    /// </summary>
    public partial class UpdateNotificationWindow : Window
    {
        private readonly UpdateInfo _updateInfo;
        private readonly GitHubUpdateService _updateService;
        private bool _isDownloading = false;

        public UpdateNotificationWindow(UpdateInfo updateInfo)
        {
            InitializeComponent();
            _updateInfo = updateInfo;
            _updateService = new GitHubUpdateService();
            
            InitializeUpdateInfo();
            ApplyTheme(ThemeService.Instance.IsDarkMode);
        }

        private void InitializeUpdateInfo()
        {
            try
            {
                // Zeige Update-Informationen
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
                TxtCurrentVersion.Text = currentVersion;
                TxtNewVersion.Text = _updateInfo.Version;
                TxtReleaseDate.Text = _updateInfo.ReleaseDate;
                
                // Format file size
                if (_updateInfo.FileSize > 0)
                {
                    var sizeInMB = _updateInfo.FileSize / 1024.0 / 1024.0;
                    TxtFileSize.Text = $"{sizeInMB:F1} MB";
                }
                else
                {
                    TxtFileSize.Text = "Unbekannt";
                }

                // Release Notes anzeigen
                if (_updateInfo.ReleaseNotes?.Length > 0)
                {
                    TxtReleaseNotes.Text = string.Join("\n", _updateInfo.ReleaseNotes);
                }

                // Mandatory Update prüfen
                if (_updateInfo.Mandatory)
                {
                    BtnSkipUpdate.Visibility = Visibility.Collapsed;
                    BtnRemindLater.Visibility = Visibility.Collapsed;
                    Title = "Wichtiges Update verfügbar - Einsatzüberwachung Professional";
                }

                LoggingService.Instance.LogInfo($"Update-Dialog angezeigt: {currentVersion} → {_updateInfo.Version}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Fehler beim Initialisieren der Update-Informationen", ex);
            }
        }

        private void ApplyTheme(bool isDarkMode)
        {
            try
            {
                // Theme wird über DynamicResource automatisch angewendet
                // Zusätzliche theme-spezifische Anpassungen hier
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Fehler beim Anwenden des Themes", ex);
            }
        }

        private async void BtnDownloadUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_isDownloading) return;

            try
            {
                _isDownloading = true;
                SetDownloadingState(true);

                LoggingService.Instance.LogInfo("🔄 Benutzer hat Update-Download gestartet");

                var progress = new Progress<int>(value =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ProgressUpdate.Value = value;
                        TxtProgress.Text = $"Download: {value}% abgeschlossen";
                    });
                });

                var downloadPath = await _updateService.DownloadUpdateAsync(_updateInfo, progress);

                if (!string.IsNullOrEmpty(downloadPath))
                {
                    Dispatcher.Invoke(() =>
                    {
                        TxtProgress.Text = "Download abgeschlossen - Installation wird vorbereitet...";
                    });

                    await Task.Delay(1000); // Kurze Pause für UI-Update

                    var result = MessageBox.Show(
                        "Update erfolgreich heruntergeladen!\n\n" +
                        "Die Anwendung wird jetzt geschlossen und das Update installiert.\n" +
                        "Nach der Installation startet die Anwendung automatisch neu.\n\n" +
                        "Möchten Sie das Update jetzt installieren?",
                        "Update bereit zur Installation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        LoggingService.Instance.LogInfo("🚀 Starte Update-Installation");
                        
                        // Update installieren
                        var installSuccess = await _updateService.InstallUpdateAsync(downloadPath, true);
                        
                        if (!installSuccess)
                        {
                            MessageBox.Show(
                                "Die Update-Installation konnte nicht gestartet werden.\n\n" +
                                "Bitte führen Sie die heruntergeladene Setup-Datei manuell aus:\n" +
                                downloadPath,
                                "Update-Installation fehlgeschlagen",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                        // Wenn erfolgreich, wird die App automatisch beendet
                    }
                    else
                    {
                        MessageBox.Show(
                            "Das Update wurde heruntergeladen aber nicht installiert.\n\n" +
                            "Sie können es später manuell installieren:\n" +
                            downloadPath,
                            "Update heruntergeladen",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        
                        Close();
                    }
                }
                else
                {
                    MessageBox.Show(
                        "Der Update-Download ist fehlgeschlagen.\n\n" +
                        "Bitte prüfen Sie Ihre Internetverbindung und versuchen Sie es erneut.\n" +
                        "Alternativ können Sie das Update manuell von GitHub herunterladen.",
                        "Download fehlgeschlagen",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Update-Download Fehler", ex);
                MessageBox.Show(
                    $"Ein Fehler ist beim Update-Download aufgetreten:\n\n{ex.Message}\n\n" +
                    "Bitte versuchen Sie es erneut oder laden Sie das Update manuell von GitHub herunter.",
                    "Update-Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isDownloading = false;
                SetDownloadingState(false);
            }
        }

        private void BtnShowReleaseNotes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_updateInfo.ReleaseNotesUrl))
                {
                    LoggingService.Instance.LogInfo($"Öffne Release Notes: {_updateInfo.ReleaseNotesUrl}");
                    
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = _updateInfo.ReleaseNotesUrl,
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                }
                else
                {
                    MessageBox.Show(
                        "Release Notes sind nicht verfügbar.\n\n" +
                        "Besuchen Sie GitHub für weitere Informationen:\n" +
                        "https://github.com/Elemirus1996/Einsatzueberwachung/releases",
                        "Release Notes",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Fehler beim Öffnen der Release Notes", ex);
                MessageBox.Show(
                    "Die Release Notes konnten nicht geöffnet werden.\n\n" +
                    "Besuchen Sie GitHub manuell:\n" +
                    "https://github.com/Elemirus1996/Einsatzueberwachung/releases",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void BtnRemindLater_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoggingService.Instance.LogInfo("Benutzer hat 'Später erinnern' gewählt");
                
                // Setze Reminder für nächsten Start (kann erweitert werden um specific time)
                SetUpdateReminder(_updateInfo.Version);
                
                MessageBox.Show(
                    "Sie werden beim nächsten Start der Anwendung erneut an das Update erinnert.",
                    "Erinnerung gesetzt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                Close();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Fehler beim Setzen der Update-Erinnerung", ex);
                Close();
            }
        }

        private void BtnSkipUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Möchten Sie dieses Update wirklich überspringen?\n\n" +
                    "Sie werden nicht erneut über diese Version benachrichtigt.\n" +
                    "Updates können wichtige Sicherheits- und Funktionsverbesserungen enthalten.",
                    "Update überspringen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    LoggingService.Instance.LogInfo($"Benutzer hat Update {_updateInfo.Version} übersprungen");
                    
                    // Markiere Version als übersprungen
                    SkipUpdateVersion(_updateInfo.Version);
                    
                    Close();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Fehler beim Überspringen des Updates", ex);
                Close();
            }
        }

        private void SetDownloadingState(bool isDownloading)
        {
            BtnDownloadUpdate.IsEnabled = !isDownloading;
            BtnDownloadUpdate.Content = isDownloading ? "📦 Download läuft..." : "⬇️ Update herunterladen";
            
            BtnRemindLater.IsEnabled = !isDownloading;
            BtnSkipUpdate.IsEnabled = !isDownloading;
            BtnShowReleaseNotes.IsEnabled = !isDownloading;
            
            ProgressPanel.Visibility = isDownloading ? Visibility.Visible : Visibility.Collapsed;
            
            if (isDownloading)
            {
                ProgressUpdate.Value = 0;
                TxtProgress.Text = "Download wird vorbereitet...";
            }
        }

        private void SetUpdateReminder(string version)
        {
            try
            {
                // Speichere in Registry oder App-Einstellungen für Reminder
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\RescueDog_SW\Einsatzüberwachung Professional",
                    "UpdateReminder",
                    version);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"Konnte Update-Reminder nicht setzen: {ex.Message}");
            }
        }

        private void SkipUpdateVersion(string version)
        {
            try
            {
                // Speichere übersprungene Version
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\RescueDog_SW\Einsatzüberwachung Professional",
                    "SkippedVersion",
                    version);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"Konnte übersprungene Version nicht speichern: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                _updateService?.Dispose();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Fehler beim Schließen des Update-Services", ex);
            }
            
            base.OnClosed(e);
        }
    }
}
