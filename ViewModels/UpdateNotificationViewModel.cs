using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    /// <summary>
    /// ViewModel für UpdateNotificationWindow - MVVM-Implementation v1.9.0
    /// GitHub-Update-Integration mit Orange-Design und Progress-Tracking
    /// </summary>
    public class UpdateNotificationViewModel : BaseViewModel, IDisposable
    {
        private readonly UpdateInfo _updateInfo;
        private readonly GitHubUpdateService _updateService;
        private bool _isDownloading = false;
        private bool _disposed = false;

        // UI State Properties
        private string _windowTitle = "Update verfügbar - Einsatzüberwachung Professional";
        private string _currentVersion = "1.0.0.0";
        private string _newVersion = "Unbekannt";
        private string _releaseDate = "Unbekannt";
        private string _fileSize = "Unbekannt";
        private string _releaseNotes = "Keine Release-Notes verfügbar.";
        private string _progressText = "";
        private int _progressValue = 0;
        private Visibility _progressVisibility = Visibility.Collapsed;
        private bool _isDownloadEnabled = true;
        private bool _isSkipEnabled = true;
        private bool _isRemindEnabled = true;
        private bool _isReleaseNotesEnabled = true;

        public UpdateNotificationViewModel(UpdateInfo updateInfo)
        {
            _updateInfo = updateInfo ?? throw new ArgumentNullException(nameof(updateInfo));
            _updateService = new GitHubUpdateService();

            // Initialize commands
            InitializeCommands();

            // Initialize update info
            InitializeUpdateInfo();

            LoggingService.Instance.LogInfo("UpdateNotificationViewModel initialized with MVVM pattern v1.9.0");
        }

        #region Properties

        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        public string CurrentVersion
        {
            get => _currentVersion;
            set => SetProperty(ref _currentVersion, value);
        }

        public string NewVersion
        {
            get => _newVersion;
            set => SetProperty(ref _newVersion, value);
        }

        public string ReleaseDate
        {
            get => _releaseDate;
            set => SetProperty(ref _releaseDate, value);
        }

        public string FileSize
        {
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
        }

        public string ReleaseNotes
        {
            get => _releaseNotes;
            set => SetProperty(ref _releaseNotes, value);
        }

        public string ProgressText
        {
            get => _progressText;
            set => SetProperty(ref _progressText, value);
        }

        public int ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        public Visibility ProgressVisibility
        {
            get => _progressVisibility;
            set => SetProperty(ref _progressVisibility, value);
        }

        public bool IsDownloadEnabled
        {
            get => _isDownloadEnabled;
            set => SetProperty(ref _isDownloadEnabled, value);
        }

        public bool IsSkipEnabled
        {
            get => _isSkipEnabled;
            set => SetProperty(ref _isSkipEnabled, value);
        }

        public bool IsRemindEnabled
        {
            get => _isRemindEnabled;
            set => SetProperty(ref _isRemindEnabled, value);
        }

        public bool IsReleaseNotesEnabled
        {
            get => _isReleaseNotesEnabled;
            set => SetProperty(ref _isReleaseNotesEnabled, value);
        }

        public bool IsMandatoryUpdate => _updateInfo.Mandatory;

        #endregion

        #region Commands

        public ICommand DownloadUpdateCommand { get; private set; } = null!;
        public ICommand ShowReleaseNotesCommand { get; private set; } = null!;
        public ICommand RemindLaterCommand { get; private set; } = null!;
        public ICommand SkipUpdateCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            DownloadUpdateCommand = new RelayCommand(async () => await ExecuteDownloadUpdateAsync(), () => !_isDownloading);
            ShowReleaseNotesCommand = new RelayCommand(ExecuteShowReleaseNotes);
            RemindLaterCommand = new RelayCommand(ExecuteRemindLater, () => !IsMandatoryUpdate);
            SkipUpdateCommand = new RelayCommand(ExecuteSkipUpdate, () => !IsMandatoryUpdate);
        }

        #endregion

        #region Command Implementations

        private async Task ExecuteDownloadUpdateAsync()
        {
            if (_isDownloading) return;

            try
            {
                _isDownloading = true;
                SetDownloadingState(true);

                LoggingService.Instance.LogInfo("🔄 User started update download via MVVM");

                var progress = new Progress<int>(value =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProgressValue = value;
                        ProgressText = $"Download: {value}% abgeschlossen";
                    });
                });

                var downloadPath = await _updateService.DownloadUpdateAsync(_updateInfo, progress);

                if (!string.IsNullOrEmpty(downloadPath))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProgressText = "Download abgeschlossen - Installation wird vorbereitet...";
                    });

                    await Task.Delay(1000); // Kurze Pause für UI-Update

                    var result = Application.Current.Dispatcher.Invoke(() =>
                    {
                        return MessageBox.Show(
                            "Update erfolgreich heruntergeladen!\n\n" +
                            "Die Anwendung wird jetzt geschlossen und das Update installiert.\n" +
                            "Nach der Installation startet die Anwendung automatisch neu.\n\n" +
                            "Möchten Sie das Update jetzt installieren?",
                            "Update bereit zur Installation",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);
                    });

                    if (result == MessageBoxResult.Yes)
                    {
                        LoggingService.Instance.LogInfo("🚀 Starting update installation via MVVM");
                        
                        // Update installieren
                        var installSuccess = await _updateService.InstallUpdateAsync(downloadPath, true);
                        
                        if (!installSuccess)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show(
                                    "Die Update-Installation konnte nicht gestartet werden.\n\n" +
                                    "Bitte führen Sie die heruntergeladene Setup-Datei manuell aus:\n" +
                                    downloadPath,
                                    "Update-Installation fehlgeschlagen",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                            });
                        }
                        // Wenn erfolgreich, wird die App automatisch beendet
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(
                                "Das Update wurde heruntergeladen aber nicht installiert.\n\n" +
                                "Sie können es später manuell installieren:\n" +
                                downloadPath,
                                "Update heruntergeladen",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        });
                        
                        RequestClose?.Invoke();
                    }
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            "Der Update-Download ist fehlgeschlagen.\n\n" +
                            "Bitte prüfen Sie Ihre Internetverbindung und versuchen Sie es erneut.\n" +
                            "Alternativ können Sie das Update manuell von GitHub herunterladen.",
                            "Download fehlgeschlagen",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    });
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Update download error via MVVM", ex);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"Ein Fehler ist beim Update-Download aufgetreten:\n\n{ex.Message}\n\n" +
                        "Bitte versuchen Sie es erneut oder laden Sie das Update manuell von GitHub herunter.",
                        "Update-Fehler",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
            finally
            {
                _isDownloading = false;
                SetDownloadingState(false);
            }
        }

        private void ExecuteShowReleaseNotes()
        {
            try
            {
                if (!string.IsNullOrEmpty(_updateInfo.ReleaseNotesUrl))
                {
                    LoggingService.Instance.LogInfo($"Opening release notes via MVVM: {_updateInfo.ReleaseNotesUrl}");
                    
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
                LoggingService.Instance.LogError("Error opening release notes via MVVM", ex);
                MessageBox.Show(
                    "Die Release Notes konnten nicht geöffnet werden.\n\n" +
                    "Besuchen Sie GitHub manuell:\n" +
                    "https://github.com/Elemirus1996/Einsatzueberwachung/releases",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void ExecuteRemindLater()
        {
            try
            {
                LoggingService.Instance.LogInfo("User selected 'Remind Later' via MVVM");
                
                // Setze Reminder für nächsten Start
                SetUpdateReminder(_updateInfo.Version);
                
                MessageBox.Show(
                    "Sie werden beim nächsten Start der Anwendung erneut an das Update erinnert.",
                    "Erinnerung gesetzt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error setting update reminder via MVVM", ex);
                RequestClose?.Invoke();
            }
        }

        private void ExecuteSkipUpdate()
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
                    LoggingService.Instance.LogInfo($"User skipped update {_updateInfo.Version} via MVVM");
                    
                    // Markiere Version als übersprungen
                    SkipUpdateVersion(_updateInfo.Version);
                    
                    RequestClose?.Invoke();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error skipping update via MVVM", ex);
                RequestClose?.Invoke();
            }
        }

        #endregion

        #region Private Methods

        private void InitializeUpdateInfo()
        {
            try
            {
                // Zeige Update-Informationen mit zentraler Versionsverwaltung
                CurrentVersion = VersionService.DisplayVersion;
                NewVersion = _updateInfo.Version;
                ReleaseDate = _updateInfo.ReleaseDate;
                
                // Format file size
                if (_updateInfo.FileSize > 0)
                {
                    var sizeInMB = _updateInfo.FileSize / 1024.0 / 1024.0;
                    FileSize = $"{sizeInMB:F1} MB";
                }
                else
                {
                    FileSize = "Unbekannt";
                }

                // Release Notes anzeigen
                if (_updateInfo.ReleaseNotes?.Length > 0)
                {
                    ReleaseNotes = string.Join("\n", _updateInfo.ReleaseNotes);
                }

                // Mandatory Update prüfen
                if (_updateInfo.Mandatory)
                {
                    IsSkipEnabled = false;
                    IsRemindEnabled = false;
                    WindowTitle = $"Wichtiges Update verfügbar - {VersionService.ProductNameWithVersion}";
                }
                else
                {
                    WindowTitle = $"Update verfügbar - {VersionService.ProductNameWithVersion}";
                }

                LoggingService.Instance.LogInfo($"Update dialog initialized via MVVM: {CurrentVersion} → {NewVersion}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing update info via MVVM", ex);
            }
        }

        private void SetDownloadingState(bool isDownloading)
        {
            IsDownloadEnabled = !isDownloading;
            IsRemindEnabled = !isDownloading && !IsMandatoryUpdate;
            IsSkipEnabled = !isDownloading && !IsMandatoryUpdate;
            IsReleaseNotesEnabled = !isDownloading;
            
            ProgressVisibility = isDownloading ? Visibility.Visible : Visibility.Collapsed;
            
            if (isDownloading)
            {
                ProgressValue = 0;
                ProgressText = "Download wird vorbereitet...";
            }

            // Update command states
            ((RelayCommand)DownloadUpdateCommand).RaiseCanExecuteChanged();
            ((RelayCommand)RemindLaterCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SkipUpdateCommand).RaiseCanExecuteChanged();
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
                LoggingService.Instance.LogWarning($"Could not set update reminder via MVVM: {ex.Message}");
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
                LoggingService.Instance.LogWarning($"Could not save skipped version via MVVM: {ex.Message}");
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Event wird ausgelöst, wenn das Fenster geschlossen werden soll
        /// </summary>
        public event Action? RequestClose;

        #endregion

        #region IDisposable Implementation

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        _updateService?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogError("Error disposing UpdateService via MVVM", ex);
                    }
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
