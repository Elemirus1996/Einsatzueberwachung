using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Einsatzueberwachung.Services
{
    /// <summary>
    /// GitHub-basierter Update-Service für automatische Updates über GitHub Releases
    /// </summary>
    public class GitHubUpdateService : IDisposable
    {
        private const string GITHUB_REPO = "Elemirus1996/Einsatzueberwachung";
        private const string GITHUB_API_URL = "https://api.github.com/repos/{0}/releases/latest";
        private const string UPDATE_INFO_URL = "https://github.com/{0}/releases/latest/download/update-info.json";
        private const string USER_AGENT = "Einsatzueberwachung-Professional-v1.7";

        private readonly HttpClient _httpClient;
        private bool _disposed = false;

        public GitHubUpdateService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
            _httpClient.Timeout = TimeSpan.FromMinutes(10); // Für große Downloads
        }

        /// <summary>
        /// Prüft auf verfügbare Updates über GitHub Releases API
        /// </summary>
        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                LoggingService.Instance.LogInfo("🔄 Prüfe auf Updates über GitHub...");

                // Erst GitHub Releases API prüfen
                var apiUrl = string.Format(GITHUB_API_URL, GITHUB_REPO);
                var response = await _httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    LoggingService.Instance.LogWarning($"GitHub API nicht erreichbar: {response.StatusCode}");
                    return null;
                }

                var apiContent = await response.Content.ReadAsStringAsync();
                var releaseInfo = JsonSerializer.Deserialize<GitHubReleaseInfo>(apiContent, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });

                if (releaseInfo?.TagName == null)
                {
                    LoggingService.Instance.LogWarning("Keine gültige Release-Information gefunden");
                    return null;
                }

                // Update-Info JSON laden falls verfügbar
                var updateInfoUrl = string.Format(UPDATE_INFO_URL, GITHUB_REPO);
                UpdateInfo? updateInfo = null;

                try
                {
                    var updateResponse = await _httpClient.GetAsync(updateInfoUrl);
                    if (updateResponse.IsSuccessStatusCode)
                    {
                        var updateContent = await updateResponse.Content.ReadAsStringAsync();
                        updateInfo = JsonSerializer.Deserialize<UpdateInfo>(updateContent, new JsonSerializerOptions 
                        { 
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                        });
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogWarning($"Update-Info JSON nicht verfügbar: {ex.Message}");
                }

                // Fallback: Basis-Info aus GitHub Release
                if (updateInfo == null)
                {
                    updateInfo = new UpdateInfo
                    {
                        Version = releaseInfo.TagName.TrimStart('v'),
                        ReleaseDate = releaseInfo.PublishedAt?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd"),
                        ReleaseNotesUrl = releaseInfo.HtmlUrl ?? $"https://github.com/{GITHUB_REPO}/releases/tag/{releaseInfo.TagName}",
                        Mandatory = false,
                        MinimumVersion = "1.5.0"
                    };

                    // Suche Setup.exe in Assets
                    var setupAsset = Array.Find(releaseInfo.Assets ?? Array.Empty<GitHubAsset>(), 
                        asset => asset.Name?.Contains("Setup.exe") == true);

                    if (setupAsset != null)
                    {
                        updateInfo.DownloadUrl = setupAsset.BrowserDownloadUrl;
                        updateInfo.FileSize = setupAsset.Size;
                    }

                    // Basis Release Notes
                    updateInfo.ReleaseNotes = new[]
                    {
                        $"🚀 Neue Version {updateInfo.Version} verfügbar",
                        "📱 Verbesserte Mobile Server-Funktionalität",
                        "🔧 Performance-Optimierungen und Bugfixes",
                        "🛡️ Sicherheits-Updates"
                    };
                }

                var currentVersion = GetCurrentVersion();
                if (IsNewerVersion(updateInfo.Version, currentVersion))
                {
                    LoggingService.Instance.LogInfo($"✅ Update verfügbar: {currentVersion} → {updateInfo.Version}");
                    return updateInfo;
                }
                else
                {
                    LoggingService.Instance.LogInfo($"✅ Anwendung ist aktuell: {currentVersion}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Update-Check fehlgeschlagen", ex);
                return null;
            }
        }

        /// <summary>
        /// Lädt das Update herunter und zeigt Fortschritt an
        /// </summary>
        public async Task<string?> DownloadUpdateAsync(UpdateInfo updateInfo, IProgress<int>? progress = null)
        {
            if (string.IsNullOrEmpty(updateInfo.DownloadUrl))
            {
                LoggingService.Instance.LogError("Keine Download-URL für Update verfügbar");
                return null;
            }

            try
            {
                LoggingService.Instance.LogInfo($"📦 Download Update von: {updateInfo.DownloadUrl}");

                var tempPath = Path.Combine(Path.GetTempPath(), $"Einsatzueberwachung_Update_v{updateInfo.Version}.exe");

                // Lösche alte Update-Dateien
                CleanupOldUpdateFiles();

                using var response = await _httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write);

                var totalBytes = response.Content.Headers.ContentLength ?? updateInfo.FileSize;
                var totalRead = 0L;
                var buffer = new byte[8192];

                while (true)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    if (totalBytes > 0)
                    {
                        var progressPercent = (int)((totalRead * 100) / totalBytes);
                        progress?.Report(progressPercent);
                    }
                }

                LoggingService.Instance.LogInfo($"✅ Update erfolgreich heruntergeladen: {tempPath}");
                return tempPath;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Update-Download fehlgeschlagen", ex);
                return null;
            }
        }

        /// <summary>
        /// Installiert das Update im Silent-Modus
        /// </summary>
        public async Task<bool> InstallUpdateAsync(string updatePath, bool restartApp = true)
        {
            try
            {
                LoggingService.Instance.LogInfo($"🔧 Installiere Update: {updatePath}");

                var currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = updatePath,
                    UseShellExecute = true,
                    Verb = "runas" // Als Administrator ausführen
                };

                // Silent Update mit aktueller Installation als Parameter
                if (!string.IsNullOrEmpty(currentExePath))
                {
                    startInfo.Arguments = $"/SILENT /CLOSEAPPLICATIONS /UPDATE \"{currentExePath}\"";
                    
                    if (restartApp)
                    {
                        startInfo.Arguments += " /RESTARTAPPLICATIONS";
                    }
                }
                else
                {
                    startInfo.Arguments = "/SILENT /CLOSEAPPLICATIONS";
                }

                LoggingService.Instance.LogInfo($"Update-Befehl: {startInfo.FileName} {startInfo.Arguments}");

                var process = Process.Start(startInfo);
                
                if (restartApp && process != null)
                {
                    // Warte kurz und beende dann die aktuelle Anwendung
                    await Task.Delay(2000);
                    
                    LoggingService.Instance.LogInfo("🔄 Beende Anwendung für Update-Installation...");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Application.Current.Shutdown();
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Update-Installation fehlgeschlagen", ex);
                return false;
            }
        }

        /// <summary>
        /// Prüft ob eine Version neuer ist als die aktuelle
        /// </summary>
        private bool IsNewerVersion(string newVersionString, string currentVersionString)
        {
            try
            {
                // Entferne 'v' Prefix falls vorhanden
                newVersionString = newVersionString.TrimStart('v');
                currentVersionString = currentVersionString.TrimStart('v');

                var newVersion = new Version(newVersionString);
                var currentVersion = new Version(currentVersionString);

                return newVersion > currentVersion;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"Version-Vergleich fehlgeschlagen: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Ermittelt die aktuelle Anwendungsversion
        /// </summary>
        private string GetCurrentVersion()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                {
                    // Konvertiere zu 3-teiliger Version (X.Y.Z) für Vergleich mit update-info.json
                    return $"{version.Major}.{version.Minor}.{version.Build}";
                }
                return "1.0.0";
            }
            catch
            {
                return "1.0.0";
            }
        }

        /// <summary>
        /// Löscht alte Update-Dateien aus dem Temp-Verzeichnis
        /// </summary>
        private void CleanupOldUpdateFiles()
        {
            try
            {
                var tempDir = Path.GetTempPath();
                var updateFiles = Directory.GetFiles(tempDir, "Einsatzueberwachung_Update_*.exe");

                foreach (var file in updateFiles)
                {
                    try
                    {
                        if (File.GetCreationTime(file) < DateTime.Now.AddDays(-7)) // Älter als 7 Tage
                        {
                            File.Delete(file);
                            LoggingService.Instance.LogInfo($"🧹 Alte Update-Datei gelöscht: {Path.GetFileName(file)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogWarning($"Cleanup-Fehler für {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"Update-Cleanup fehlgeschlagen: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Update-Informationen aus update-info.json
    /// </summary>
    public class UpdateInfo
    {
        public string Version { get; set; } = string.Empty;
        public string ReleaseDate { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotesUrl { get; set; } = string.Empty;
        public bool Mandatory { get; set; }
        public string MinimumVersion { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public string[] ReleaseNotes { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// GitHub Release API Response
    /// </summary>
    public class GitHubReleaseInfo
    {
        public string? TagName { get; set; }
        public string? Name { get; set; }
        public string? Body { get; set; }
        public string? HtmlUrl { get; set; }
        public DateTime? PublishedAt { get; set; }
        public bool Draft { get; set; }
        public bool Prerelease { get; set; }
        public GitHubAsset[]? Assets { get; set; }
    }

    /// <summary>
    /// GitHub Release Asset
    /// </summary>
    public class GitHubAsset
    {
        public string? Name { get; set; }
        public string? BrowserDownloadUrl { get; set; }
        public long Size { get; set; }
        public string? ContentType { get; set; }
    }
}
