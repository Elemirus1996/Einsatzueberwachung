using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace Einsatzueberwachung.Services
{
    public class LoggingService : INotifyPropertyChanged
    {
        private static LoggingService? _instance;
        private readonly string _logFilePath;
        private string _lastLogEntry = string.Empty;
        private bool _verboseLogging = false;

        public static LoggingService Instance => _instance ??= new LoggingService();

        private LoggingService()
        {
            var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Einsatzueberwachung", "Logs");
            Directory.CreateDirectory(logDirectory);
            _logFilePath = Path.Combine(logDirectory, $"Log_{DateTime.Now:yyyy-MM-dd}.txt");
        }

        public string LastLogEntry
        {
            get => _lastLogEntry;
            private set { _lastLogEntry = value; OnPropertyChanged(); }
        }

        public void Initialize(string logFileName, LogLevel logLevel)
        {
            LogInfo($"LoggingService initialized with {logLevel} level");
        }

        public void SetVerboseLogging(bool enabled)
        {
            _verboseLogging = enabled;
            LogInfo($"Verbose logging {(_verboseLogging ? "enabled" : "disabled")}");
        }

        public void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public void LogWarning(string message)
        {
            Log("WARNING", message);
        }

        public void LogError(string message)
        {
            Log("ERROR", message);
        }

        public void LogError(string message, Exception ex)
        {
            Log("ERROR", $"{message}: {ex.Message}");
        }

        private void Log(string level, string message)
        {
            try
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                LastLogEntry = logEntry;

                // IMMER in Debug-Konsole schreiben (für Visual Studio)
                System.Diagnostics.Debug.WriteLine(logEntry);
                Console.WriteLine(logEntry); // Auch in Console schreiben

                // Write to file
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Fallback - log to system event log or ignore
                System.Diagnostics.Debug.WriteLine($"Logging failed: {ex.Message}");
                Console.WriteLine($"Logging failed: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
