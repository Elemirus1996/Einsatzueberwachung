using System.Diagnostics;
using System.Windows.Input;
using System.IO;
using System.Reflection;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        private string _applicationVersion = string.Empty;
        private string _buildDate = string.Empty;
        private string _copyrightText = string.Empty;
        private string _developerInfo = string.Empty;

        public string ApplicationVersion
        {
            get => _applicationVersion;
            set
            {
                _applicationVersion = value;
                OnPropertyChanged();
            }
        }

        public string BuildDate
        {
            get => _buildDate;
            set
            {
                _buildDate = value;
                OnPropertyChanged();
            }
        }

        public string CopyrightText
        {
            get => _copyrightText;
            set
            {
                _copyrightText = value;
                OnPropertyChanged();
            }
        }

        public string DeveloperInfo
        {
            get => _developerInfo;
            set
            {
                _developerInfo = value;
                OnPropertyChanged();
            }
        }

        // Enhanced commands with parameter support
        public ICommand CloseCommand { get; private set; } = null!;
        public ICommand OpenUrlCommand { get; private set; } = null!;
        public ICommand CopyToClipboardCommand { get; private set; } = null!;
        public ICommand ShowSystemInfoCommand { get; private set; } = null!;
        public ICommand OpenLogFileCommand { get; private set; } = null!;

        // Events for view communication
        public event EventHandler? RequestClose;
        public event EventHandler<string>? ShowMessage;

        public AboutViewModel()
        {
            InitializeProperties();
            InitializeCommands();
            
            LoggingService.Instance?.LogInfo("AboutViewModel initialized with enhanced command support");
        }

        private void InitializeProperties()
        {
            try
            {
                // Get version information from assembly
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                
                ApplicationVersion = $"v1.9.1 - MVVM Edition ({version?.ToString() ?? "Unknown"})";
                
                // Build date from assembly
                var buildDate = GetBuildDate(assembly);
                BuildDate = buildDate?.ToString("dd.MM.yyyy HH:mm") ?? "Unbekannt";
                
                CopyrightText = "© 2024 Einsatzüberwachung Professional";
                DeveloperInfo = "Entwickelt für Rettungshunde-Staffeln";
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error initializing AboutViewModel properties", ex);
                ApplicationVersion = "v1.9.1 - MVVM Edition";
                BuildDate = DateTime.Now.ToString("dd.MM.yyyy");
                CopyrightText = "© 2024 Einsatzüberwachung Professional";
                DeveloperInfo = "Entwickelt für Rettungshunde-Staffeln";
            }
        }

        private void InitializeCommands()
        {
            CloseCommand = new RelayCommand(ExecuteClose);
            OpenUrlCommand = new RelayCommand<string>(ExecuteOpenUrl, CanExecuteOpenUrl);
            CopyToClipboardCommand = new RelayCommand<string>(ExecuteCopyToClipboard, CanExecuteCopyToClipboard);
            ShowSystemInfoCommand = new RelayCommand(ExecuteShowSystemInfo);
            OpenLogFileCommand = new RelayCommand(ExecuteOpenLogFile, CanExecuteOpenLogFile);
        }

        private DateTime? GetBuildDate(System.Reflection.Assembly assembly)
        {
            try
            {
                var attributes = assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
                var buildDateAttribute = attributes.FirstOrDefault(a => a.Key == "BuildDate");
                if (buildDateAttribute != null && DateTime.TryParse(buildDateAttribute.Value, out var buildDate))
                {
                    return buildDate;
                }
                
                // Fallback: Use file creation time
                var location = assembly.Location;
                if (!string.IsNullOrEmpty(location) && File.Exists(location))
                {
                    return File.GetCreationTime(location);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogWarning($"Could not determine build date: {ex.Message}");
            }
            
            return null;
        }

        // Command implementations
        private void ExecuteClose()
        {
            try
            {
                LoggingService.Instance?.LogInfo("About dialog closed by user");
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error closing About dialog", ex);
            }
        }

        private bool CanExecuteOpenUrl(string? url)
        {
            return !string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out _);
        }

        private void ExecuteOpenUrl(string? url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    LoggingService.Instance?.LogWarning("Cannot open URL: URL is null or empty");
                    return;
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                
                Process.Start(processInfo);
                LoggingService.Instance?.LogInfo($"Opened URL: {url}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError($"Error opening URL {url}", ex);
                ShowMessage?.Invoke(this, $"Fehler beim Öffnen der URL: {ex.Message}");
            }
        }

        private bool CanExecuteCopyToClipboard(string? text)
        {
            return !string.IsNullOrWhiteSpace(text);
        }

        private void ExecuteCopyToClipboard(string? text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return;
                }

                System.Windows.Clipboard.SetText(text);
                LoggingService.Instance?.LogInfo($"Copied to clipboard: {text}");
                ShowMessage?.Invoke(this, "Text in die Zwischenablage kopiert");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError($"Error copying to clipboard: {text}", ex);
                ShowMessage?.Invoke(this, $"Fehler beim Kopieren: {ex.Message}");
            }
        }

        private void ExecuteShowSystemInfo()
        {
            try
            {
                var systemInfo = GetSystemInfo();
                ShowMessage?.Invoke(this, systemInfo);
                LoggingService.Instance?.LogInfo("System information displayed");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error showing system info", ex);
                ShowMessage?.Invoke(this, $"Fehler beim Anzeigen der Systeminformationen: {ex.Message}");
            }
        }

        private bool CanExecuteOpenLogFile()
        {
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "einsatzueberwachung.log");
                return File.Exists(logPath);
            }
            catch
            {
                return false;
            }
        }

        private void ExecuteOpenLogFile()
        {
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "einsatzueberwachung.log");
                
                if (File.Exists(logPath))
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "notepad.exe",
                        Arguments = logPath,
                        UseShellExecute = true
                    };
                    
                    Process.Start(processInfo);
                    LoggingService.Instance?.LogInfo("Log file opened");
                }
                else
                {
                    ShowMessage?.Invoke(this, "Log-Datei wurde nicht gefunden.");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error opening log file", ex);
                ShowMessage?.Invoke(this, $"Fehler beim Öffnen der Log-Datei: {ex.Message}");
            }
        }

        private string GetSystemInfo()
        {
            try
            {
                var info = new System.Text.StringBuilder();
                info.AppendLine("SYSTEM-INFORMATIONEN");
                info.AppendLine("===================");
                info.AppendLine($"Betriebssystem: {Environment.OSVersion}");
                info.AppendLine($".NET Version: {Environment.Version}");
                info.AppendLine($"Prozessor-Architektur: {Environment.ProcessorCount} Kerne");
                info.AppendLine($"Arbeitsspeicher: {GC.GetTotalMemory(false) / 1024 / 1024} MB verwendet");
                info.AppendLine($"Anwendungsordner: {AppDomain.CurrentDomain.BaseDirectory}");
                info.AppendLine($"Startzeit: {Process.GetCurrentProcess().StartTime:dd.MM.yyyy HH:mm:ss}");
                
                return info.ToString();
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error collecting system info", ex);
                return "Fehler beim Sammeln der Systeminformationen.";
            }
        }
    }
}
