using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Threading;
using Einsatzueberwachung.Models;

namespace Einsatzueberwachung.Services
{
    public class PersistenceService
    {
        private static PersistenceService? _instance;
        public static PersistenceService Instance => _instance ??= new PersistenceService();

        private readonly string _autoSaveDirectory;
        private readonly string _autoSaveFileName = "autosave.json";
        private readonly string _crashRecoveryFileName = "crashrecovery.json";
        private DispatcherTimer? _autoSaveTimer;
        private readonly SemaphoreSlim _saveSemaphore = new(1, 1);
        private volatile bool _isDirty = false;
        private EinsatzSessionData? _lastSavedData;

        private PersistenceService()
        {
            _autoSaveDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Einsatzueberwachung", "AutoSave");
            
            Directory.CreateDirectory(_autoSaveDirectory);
        }

        public void StartAutoSave(Func<EinsatzSessionData> getSessionData)
        {
            if (_autoSaveTimer != null) return;

            // Use lower priority for auto-save to not interfere with timers
            _autoSaveTimer = new DispatcherTimer(DispatcherPriority.Background);
            _autoSaveTimer.Interval = TimeSpan.FromSeconds(30); // Auto-save every 30 seconds
            _autoSaveTimer.Tick += async (s, e) =>
            {
                if (!_isDirty) return; // Only save if data has changed

                try
                {
                    var sessionData = getSessionData();
                    if (sessionData != null && HasDataChanged(sessionData))
                    {
                        await SaveSessionAsync(sessionData, _autoSaveFileName);
                        _lastSavedData = sessionData;
                        _isDirty = false;
                        LoggingService.Instance.LogInfo("Auto-save completed");
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("Auto-save failed", ex);
                }
            };
            _autoSaveTimer.Start();
            LoggingService.Instance.LogInfo("Auto-save started (30s interval)");
        }

        public void MarkDirty()
        {
            _isDirty = true;
        }

        private bool HasDataChanged(EinsatzSessionData newData)
        {
            if (_lastSavedData == null) return true;
            
            // Quick check for basic changes
            if (_lastSavedData.Teams.Length != newData.Teams.Length) return true;
            if (_lastSavedData.NextTeamId != newData.NextTeamId) return true;
            
            // Check team changes (simplified check)
            for (int i = 0; i < Math.Min(_lastSavedData.Teams.Length, newData.Teams.Length); i++)
            {
                var oldTeam = _lastSavedData.Teams[i];
                var newTeam = newData.Teams[i];
                
                if (oldTeam.IsRunning != newTeam.IsRunning ||
                    Math.Abs((oldTeam.ElapsedTime - newTeam.ElapsedTime).TotalSeconds) > 1 ||
                    oldTeam.HundName != newTeam.HundName ||
                    oldTeam.Hundefuehrer != newTeam.Hundefuehrer ||
                    oldTeam.Notizen != newTeam.Notizen)
                {
                    return true;
                }
            }
            
            return false;
        }

        public void StopAutoSave()
        {
            _autoSaveTimer?.Stop();
            _autoSaveTimer = null;
            LoggingService.Instance.LogInfo("Auto-save stopped");
        }

        public async Task SaveSessionAsync(EinsatzSessionData sessionData, string fileName)
        {
            await _saveSemaphore.WaitAsync();
            try
            {
                var filePath = Path.Combine(_autoSaveDirectory, fileName);
                
                // Use memory stream for better performance
                using var memoryStream = new MemoryStream();
                await JsonSerializer.SerializeAsync(memoryStream, sessionData, new JsonSerializerOptions 
                { 
                    WriteIndented = false, // Compact JSON for better performance
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                // Write to file atomically
                var tempFilePath = filePath + ".tmp";
                await File.WriteAllBytesAsync(tempFilePath, memoryStream.ToArray());
                
                if (File.Exists(filePath))
                    File.Delete(filePath);
                File.Move(tempFilePath, filePath);
            }
            finally
            {
                _saveSemaphore.Release();
            }
        }

        public async Task<EinsatzSessionData?> LoadSessionAsync(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_autoSaveDirectory, fileName);
                if (!File.Exists(filePath)) return null;

                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
                var sessionData = await JsonSerializer.DeserializeAsync<EinsatzSessionData>(fileStream, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                LoggingService.Instance.LogInfo($"Session loaded from {fileName}");
                return sessionData;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Failed to load session from {fileName}", ex);
                return null;
            }
        }

        public async Task SaveCrashRecoveryAsync(EinsatzSessionData sessionData)
        {
            await SaveSessionAsync(sessionData, _crashRecoveryFileName);
        }

        public async Task<EinsatzSessionData?> LoadCrashRecoveryAsync()
        {
            return await LoadSessionAsync(_crashRecoveryFileName);
        }

        public async Task<EinsatzSessionData?> LoadAutoSaveAsync()
        {
            return await LoadSessionAsync(_autoSaveFileName);
        }

        public bool HasCrashRecovery()
        {
            var filePath = Path.Combine(_autoSaveDirectory, _crashRecoveryFileName);
            return File.Exists(filePath);
        }

        public bool HasAutoSave()
        {
            var filePath = Path.Combine(_autoSaveDirectory, _autoSaveFileName);
            return File.Exists(filePath);
        }

        public void ClearCrashRecovery()
        {
            try
            {
                var filePath = Path.Combine(_autoSaveDirectory, _crashRecoveryFileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    LoggingService.Instance.LogInfo("Crash recovery file cleared");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Failed to clear crash recovery file", ex);
            }
        }

        public void ClearAutoSave()
        {
            try
            {
                var filePath = Path.Combine(_autoSaveDirectory, _autoSaveFileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    LoggingService.Instance.LogInfo("Auto-save file cleared");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Failed to clear auto-save file", ex);
            }
        }
    }

    // Data structure for session persistence
    public class EinsatzSessionData
    {
        public EinsatzData? EinsatzData { get; set; }
        public TeamSessionData[] Teams { get; set; } = Array.Empty<TeamSessionData>();
        public int NextTeamId { get; set; } = 1;
        public int FirstWarningMinutes { get; set; } = 10;
        public int SecondWarningMinutes { get; set; } = 20;
        public DateTime SavedAt { get; set; } = DateTime.Now;
    }

    public class TeamSessionData
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? TeamType { get; set; }
        public string HundName { get; set; } = string.Empty;
        public string Hundefuehrer { get; set; } = string.Empty;
        public string Helfer { get; set; } = string.Empty;
        public string Notizen { get; set; } = string.Empty;
        public TimeSpan ElapsedTime { get; set; }
        public bool IsRunning { get; set; }
        public bool IsFirstWarning { get; set; }
        public bool IsSecondWarning { get; set; }
        public int FirstWarningMinutes { get; set; }
        public int SecondWarningMinutes { get; set; }
        public DateTime? StartTime { get; set; }
    }
}