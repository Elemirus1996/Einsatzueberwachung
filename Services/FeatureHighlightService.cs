using System;
using System.IO;
using System.Text.Json;

namespace Einsatzueberwachung.Services
{
    /// <summary>
    /// Service zur Verwaltung der Feature-Highlight-Anzeige
    /// Zeigt neue Features nur bei den ersten 3 Starts einer Version an
    /// </summary>
    public class FeatureHighlightService
    {
        private static FeatureHighlightService? _instance;
        public static FeatureHighlightService Instance => _instance ??= new FeatureHighlightService();

        private readonly string _settingsDirectory;
        private readonly string _settingsFileName = "feature_highlight.json";
        private FeatureHighlightSettings? _settings;

        private FeatureHighlightService()
        {
            _settingsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Einsatzueberwachung", "Settings");

            Directory.CreateDirectory(_settingsDirectory);
            LoadSettings();
        }

        /// <summary>
        /// Prüft ob das Feature-Highlight angezeigt werden soll
        /// </summary>
        public bool ShouldShowFeatureHighlight()
        {
            try
            {
                if (_settings == null)
                {
                    LoggingService.Instance?.LogWarning("FeatureHighlightService: Settings not loaded");
                    return true; // Bei Fehler: Sicherheitshalber anzeigen
                }

                var currentVersion = VersionService.Version;

                // Neue Version? Reset der Zähler
                if (_settings.LastSeenVersion != currentVersion)
                {
                    LoggingService.Instance?.LogInfo($"FeatureHighlightService: New version detected {_settings.LastSeenVersion} -> {currentVersion}");
                    _settings.LastSeenVersion = currentVersion;
                    _settings.ShowCount = 0;
                    SaveSettings();
                }

                // Prüfe ob noch anzeigen soll (max. 3 mal)
                bool shouldShow = _settings.ShowCount < 3;
                
                if (shouldShow)
                {
                    LoggingService.Instance?.LogInfo($"FeatureHighlightService: Showing feature highlight (count: {_settings.ShowCount + 1}/3)");
                }
                else
                {
                    LoggingService.Instance?.LogInfo("FeatureHighlightService: Feature highlight hidden (max count reached)");
                }

                return shouldShow;
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("FeatureHighlightService: Error checking show status", ex);
                return true; // Bei Fehler: Sicherheitshalber anzeigen
            }
        }

        /// <summary>
        /// Markiert dass das Feature-Highlight angezeigt wurde
        /// </summary>
        public void MarkAsShown()
        {
            try
            {
                if (_settings == null)
                {
                    LoggingService.Instance?.LogWarning("FeatureHighlightService: Cannot mark as shown - settings not loaded");
                    return;
                }

                _settings.ShowCount++;
                _settings.LastShownAt = DateTime.Now;
                SaveSettings();

                LoggingService.Instance?.LogInfo($"FeatureHighlightService: Marked as shown (count: {_settings.ShowCount}/3)");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("FeatureHighlightService: Error marking as shown", ex);
            }
        }

        /// <summary>
        /// Setzt die Anzeige zurück (für Tests oder manuelle Reset)
        /// </summary>
        public void ResetShowCount()
        {
            try
            {
                if (_settings == null)
                {
                    _settings = new FeatureHighlightSettings();
                }

                _settings.ShowCount = 0;
                _settings.LastSeenVersion = VersionService.Version;
                SaveSettings();

                LoggingService.Instance?.LogInfo("FeatureHighlightService: Show count reset");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("FeatureHighlightService: Error resetting show count", ex);
            }
        }

        /// <summary>
        /// Gibt aktuelle Statistiken zurück
        /// </summary>
        public (int showCount, string lastVersion, DateTime? lastShown) GetStatistics()
        {
            if (_settings == null)
            {
                return (0, "Unknown", null);
            }

            return (_settings.ShowCount, _settings.LastSeenVersion, _settings.LastShownAt);
        }

        private void LoadSettings()
        {
            try
            {
                var filePath = Path.Combine(_settingsDirectory, _settingsFileName);
                
                if (!File.Exists(filePath))
                {
                    // Erstelle neue Settings für neue Installation
                    _settings = new FeatureHighlightSettings
                    {
                        LastSeenVersion = VersionService.Version,
                        ShowCount = 0,
                        CreatedAt = DateTime.Now
                    };
                    SaveSettings();
                    LoggingService.Instance?.LogInfo("FeatureHighlightService: Created new settings file");
                    return;
                }

                var json = File.ReadAllText(filePath);
                _settings = JsonSerializer.Deserialize<FeatureHighlightSettings>(json);

                if (_settings == null)
                {
                    throw new InvalidOperationException("Deserialized settings is null");
                }

                LoggingService.Instance?.LogInfo($"FeatureHighlightService: Loaded settings - Version: {_settings.LastSeenVersion}, Count: {_settings.ShowCount}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("FeatureHighlightService: Error loading settings", ex);
                
                // Fallback: Neue Settings erstellen
                _settings = new FeatureHighlightSettings
                {
                    LastSeenVersion = VersionService.Version,
                    ShowCount = 0,
                    CreatedAt = DateTime.Now
                };
            }
        }

        private void SaveSettings()
        {
            try
            {
                if (_settings == null)
                {
                    return;
                }

                var filePath = Path.Combine(_settingsDirectory, _settingsFileName);
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(filePath, json);
                LoggingService.Instance?.LogInfo("FeatureHighlightService: Settings saved");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("FeatureHighlightService: Error saving settings", ex);
            }
        }
    }

    /// <summary>
    /// Einstellungen für Feature-Highlight-Anzeige
    /// </summary>
    public class FeatureHighlightSettings
    {
        /// <summary>
        /// Zuletzt gesehene Version
        /// </summary>
        public string LastSeenVersion { get; set; } = string.Empty;

        /// <summary>
        /// Anzahl der Anzeigen für die aktuelle Version
        /// </summary>
        public int ShowCount { get; set; }

        /// <summary>
        /// Zeitpunkt der letzten Anzeige
        /// </summary>
        public DateTime? LastShownAt { get; set; }

        /// <summary>
        /// Zeitpunkt der Erstellung
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
