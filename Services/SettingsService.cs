using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace Einsatzueberwachung.Services
{
    public class SettingsService : INotifyPropertyChanged
    {
        private static SettingsService? _instance;
        public static SettingsService Instance => _instance ??= new SettingsService();

        private readonly string _settingsDirectory;
        private readonly string _settingsFile = "settings.json";
        private AppSettings _settings = new();

        private SettingsService()
        {
            _settingsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Einsatzueberwachung");
            
            Directory.CreateDirectory(_settingsDirectory);
            LoadSettings();
        }

        public AppSettings Settings
        {
            get => _settings;
            private set
            {
                _settings = value;
                OnPropertyChanged();
            }
        }

        public async Task SaveSettingsAsync()
        {
            try
            {
                var filePath = Path.Combine(_settingsDirectory, _settingsFile);
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                await File.WriteAllTextAsync(filePath, json);
                LoggingService.Instance.LogInfo("Settings saved successfully");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Failed to save settings", ex);
            }
        }

        private async void LoadSettings()
        {
            try
            {
                var filePath = Path.Combine(_settingsDirectory, _settingsFile);
                if (!File.Exists(filePath))
                {
                    // Create default settings
                    await SaveSettingsAsync();
                    return;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (settings != null)
                {
                    Settings = settings;
                    LoggingService.Instance.LogInfo("Settings loaded successfully");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Failed to load settings", ex);
                // Use default settings on error
                Settings = new AppSettings();
            }
        }

        public async Task UpdateSettingAsync<T>(string propertyName, T value)
        {
            try
            {
                var property = typeof(AppSettings).GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(_settings, value);
                    await SaveSettingsAsync();
                    OnPropertyChanged(nameof(Settings));
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Failed to update setting {propertyName}", ex);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class AppSettings
    {
        // UI Settings
        public bool AutoThemeEnabled { get; set; } = true;
        public bool IsDarkModeManual { get; set; } = false;
        public int AutoSaveIntervalSeconds { get; set; } = 30;
        public bool ShowPerformanceMetrics { get; set; } = false;
        
        // Default Einsatz Settings
        public int DefaultFirstWarningMinutes { get; set; } = 10;
        public int DefaultSecondWarningMinutes { get; set; } = 20;
        public int DefaultTeamCount { get; set; } = 3;
        public string DefaultExportPath { get; set; } = "";
        
        // Window Settings
        public bool RememberWindowSize { get; set; } = true;
        public double WindowWidth { get; set; } = 1200;
        public double WindowHeight { get; set; } = 800;
        public bool StartMaximized { get; set; } = true;
        
        // Sound Settings
        public bool SoundEnabled { get; set; } = true;
        public double SoundVolume { get; set; } = 0.7;
        
        // Performance Settings
        public bool HighPerformanceMode { get; set; } = false;
        public bool EnableAnimations { get; set; } = true;
        public int MemoryCleanupIntervalMinutes { get; set; } = 5;
        
        // Advanced Settings
        public bool EnableCrashRecovery { get; set; } = true;
        public bool EnableDetailedLogging { get; set; } = false;
        public int MaxLogEntries { get; set; } = 1000;
        
        public AppSettings()
        {
            // Set default export path
            DefaultExportPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Einsatzueberwachung");
        }
    }
}