using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private const string USER_AGENT = "Einsatzueberwachung-Professional-v1.9";

        private readonly HttpClient _httpClient;
        private bool _disposed = false;

        public GitHubUpdateService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", VersionService.UserAgent);
            
            // ✅ FIXED: Korrekte Cache-Bypass Headers
            _httpClient.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true,
                MustRevalidate = true
            };
            _httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
            
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
                LoggingService.Instance.LogInfo($"🔍 Lokale Version: {GetCurrentVersion()} (Development: {VersionService.IsDevelopmentVersion})");
                LoggingService.Instance.LogInfo($"📡 GitHub Repository: {GITHUB_REPO}");

                // ✅ DEBUG: Teste GitHub API Zugriff ausführlich
                var apiUrl = string.Format(GITHUB_API_URL, GITHUB_REPO);
                LoggingService.Instance.LogInfo($"🌐 API URL: {apiUrl}");
                
                // ✅ FIXED: Korrekte Cache-Bypass Headers für Request
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                requestMessage.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true,
                    MustRevalidate = true
                };
                requestMessage.Headers.Add("Pragma", "no-cache");
                
                var response = await _httpClient.SendAsync(requestMessage);
                LoggingService.Instance.LogInfo($"📊 API Response Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    LoggingService.Instance.LogWarning($"GitHub API nicht erreichbar: {response.StatusCode}");
                    LoggingService.Instance.LogWarning($"Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");
                    return null;
                }

                var apiContent = await response.Content.ReadAsStringAsync();
                LoggingService.Instance.LogInfo($"📄 API Response Length: {apiContent.Length} characters");
                
                // ✅ DEBUG: Logge ersten Teil der Antwort
                var preview = apiContent.Length > 500 ? apiContent.Substring(0, 500) + "..." : apiContent;
                LoggingService.Instance.LogInfo($"📋 API Response Preview: {preview}");

                var releaseInfo = JsonSerializer.Deserialize<GitHubReleaseInfo>(apiContent, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });

                if (releaseInfo?.TagName == null)
                {
                    LoggingService.Instance.LogWarning("Keine gültige Release-Information gefunden");
                    LoggingService.Instance.LogWarning($"Parsed Release Info: TagName={releaseInfo?.TagName}, Name={releaseInfo?.Name}, Draft={releaseInfo?.Draft}, Prerelease={releaseInfo?.Prerelease}");
                    return null;
                }

                // ✅ DEBUG: Ausführliche Release-Info
                LoggingService.Instance.LogInfo($"🌐 GitHub Latest Release:");
                LoggingService.Instance.LogInfo($"   Tag: {releaseInfo.TagName}");
                LoggingService.Instance.LogInfo($"   Name: {releaseInfo.Name}");
                LoggingService.Instance.LogInfo($"   Published: {releaseInfo.PublishedAt}");
                LoggingService.Instance.LogInfo($"   Draft: {releaseInfo.Draft}");
                LoggingService.Instance.LogInfo($"   Prerelease: {releaseInfo.Prerelease}");
                LoggingService.Instance.LogInfo($"   Assets Count: {releaseInfo.Assets?.Length ?? 0}");

                // ✅ DEBUG: Prüfe ob es ein Draft oder Prerelease ist
                if (releaseInfo.Draft)
                {
                    LoggingService.Instance.LogWarning($"⚠️ PROBLEM GEFUNDEN: Latest Release {releaseInfo.TagName} ist als DRAFT markiert!");
                    LoggingService.Instance.LogWarning($"   Lösung: Gehen Sie zu GitHub und veröffentlichen Sie das Release als finale Version.");
                }
                
                if (releaseInfo.Prerelease)
                {
                    LoggingService.Instance.LogWarning($"⚠️ PROBLEM GEFUNDEN: Latest Release {releaseInfo.TagName} ist als PRE-RELEASE markiert!");
                    LoggingService.Instance.LogWarning($"   Lösung: Gehen Sie zu GitHub und entfernen Sie das Pre-release Häkchen.");
                }

                // ✅ DEBUG: Teste auch alle Releases API
                try
                {
                    var allReleasesUrl = $"https://api.github.com/repos/{GITHUB_REPO}/releases";
                    LoggingService.Instance.LogInfo($"🔍 Prüfe alle Releases: {allReleasesUrl}");
                    
                    var allReleasesResponse = await _httpClient.GetAsync(allReleasesUrl);
                    if (allReleasesResponse.IsSuccessStatusCode)
                    {
                        var allReleasesContent = await allReleasesResponse.Content.ReadAsStringAsync();
                        var allReleases = JsonSerializer.Deserialize<GitHubReleaseInfo[]>(allReleasesContent, new JsonSerializerOptions 
                        { 
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                        });

                        LoggingService.Instance.LogInfo($"📊 Alle Releases gefunden: {allReleases?.Length ?? 0}");
                        
                        if (allReleases != null && allReleases.Length > 0)
                        {
                            LoggingService.Instance.LogInfo($"📋 Release-Übersicht:");
                            foreach (var release in allReleases.Take(10)) // Nur erste 10 zeigen
                            {
                                var status = "";
                                if (release.Draft) status += " [DRAFT]";
                                if (release.Prerelease) status += " [PRE-RELEASE]";
                                
                                LoggingService.Instance.LogInfo($"   • {release.TagName} - {release.Name}{status}");
                            }
                            
                            // Prüfe ob es neuere non-draft Releases gibt
                            var finalReleases = allReleases.Where(r => !r.Draft && !r.Prerelease).ToArray();
                            if (finalReleases.Length > 0)
                            {
                                var newestFinalRelease = finalReleases.First();
                                if (newestFinalRelease.TagName != releaseInfo.TagName)
                                {
                                    LoggingService.Instance.LogWarning($"🚨 PROBLEM IDENTIFIZIERT:");
                                    LoggingService.Instance.LogWarning($"   Latest Release API gibt: {releaseInfo.TagName} (Draft/Pre-Release)");
                                    LoggingService.Instance.LogWarning($"   Neuestes finales Release: {newestFinalRelease.TagName}");
                                    LoggingService.Instance.LogWarning($"   Das erklärt warum ältere Versionen angezeigt werden!");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogWarning($"Fehler beim Abrufen aller Releases: {ex.Message}");
                }

                // ✅ IMPROVED: Frühe Prüfung für Development-Versionen
                var currentVersion = GetCurrentVersion();
                var availableVersion = releaseInfo.TagName.TrimStart('v');
                
                LoggingService.Instance.LogInfo($"🔢 Version-Vergleich:");
                LoggingService.Instance.LogInfo($"   Lokal: {currentVersion}");
                LoggingService.Instance.LogInfo($"   GitHub: {availableVersion}");
                
                // Für Development-Versionen: Nur Updates anzeigen, wenn eine NEUERE Version verfügbar ist
                if (VersionService.IsDevelopmentVersion)
                {
                    var currentVersionNumber = ParseVersionSafely(currentVersion);
                    var availableVersionNumber = ParseVersionSafely(availableVersion);
                    
                    LoggingService.Instance.LogInfo($"📊 Parsed Versions:");
                    LoggingService.Instance.LogInfo($"   Lokal Parsed: {currentVersionNumber}");
                    LoggingService.Instance.LogInfo($"   GitHub Parsed: {availableVersionNumber}");
                    
                    if (currentVersionNumber != null && availableVersionNumber != null)
                    {
                        // Development-Version ist gleich oder neuer als verfügbare Release-Version
                        if (currentVersionNumber >= availableVersionNumber)
                        {
                            LoggingService.Instance.LogInfo($"🚧 Development-Version {currentVersion} ist aktuell oder neuer als verfügbare Release-Version {availableVersion} - kein Update erforderlich");
                            return null;
                        }
                    }
                }

                // ✅ TEMPORÄR DEAKTIVIERT: Update-Info JSON laden falls verfügbar
                // Problem-Debugging: Verwende nur GitHub API Daten
                var updateInfoUrl = string.Format(UPDATE_INFO_URL, GITHUB_REPO);
                UpdateInfo? updateInfo = null;

                // ❌ DISABLED FOR DEBUGGING - Skip update-info.json completely
                LoggingService.Instance.LogInfo($"🚧 DEBUG MODE: Überspringe update-info.json, verwende nur GitHub API Daten");
                /*
                try
                {
                    LoggingService.Instance.LogInfo($"📄 Lade Update-Info JSON: {updateInfoUrl}");
                    var updateResponse = await _httpClient.GetAsync(updateInfoUrl);
                    LoggingService.Instance.LogInfo($"📄 Update-Info Response: {updateResponse.StatusCode}");
                    
                    if (updateResponse.IsSuccessStatusCode)
                    {
                        var updateContent = await updateResponse.Content.ReadAsStringAsync();
                        LoggingService.Instance.LogInfo($"📄 Update-Info Content Length: {updateContent.Length}");
                        
                        updateInfo = JsonSerializer.Deserialize<UpdateInfo>(updateContent, new JsonSerializerOptions 
                        { 
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                        });
                        
                        LoggingService.Instance.LogInfo($"✅ Update-Info JSON erfolgreich geladen");
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogWarning($"Update-Info JSON nicht verfügbar: {ex.Message}");
                }
                */

                // Fallback: Basis-Info aus GitHub Release
                if (updateInfo == null)
                {
                    LoggingService.Instance.LogInfo($"📋 Erstelle Fallback Update-Info aus GitHub Release");
                    
                    updateInfo = new UpdateInfo
                    {
                        Version = availableVersion,
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
                        LoggingService.Instance.LogInfo($"📦 Setup Asset gefunden: {setupAsset.Name} ({setupAsset.Size} bytes)");
                    }
                    else
                    {
                        LoggingService.Instance.LogWarning($"⚠️ Kein Setup.exe Asset im Release gefunden!");
                        if (releaseInfo.Assets?.Length > 0)
                        {
                            LoggingService.Instance.LogInfo($"📦 Verfügbare Assets:");
                            foreach (var asset in releaseInfo.Assets)
                            {
                                LoggingService.Instance.LogInfo($"   • {asset.Name} ({asset.Size} bytes)");
                            }
                        }
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

                var isNewerVersion = IsNewerVersion(updateInfo.Version, currentVersion);
                
                LoggingService.Instance.LogInfo($"📋 FINALER Update-Check:");
                LoggingService.Instance.LogInfo($"   Current: {currentVersion}");
                LoggingService.Instance.LogInfo($"   Available: {updateInfo.Version}");
                LoggingService.Instance.LogInfo($"   IsNewer: {isNewerVersion}");
                
                if (isNewerVersion)
                {
                    LoggingService.Instance.LogInfo($"✅ Update verfügbar: {currentVersion} → {updateInfo.Version}");
                    return updateInfo;
                }
                else
                {
                    // ✅ IMPROVED: Bessere Behandlung für verschiedene Szenarien
                    var currentVersionObj = ParseVersionSafely(currentVersion);
                    var availableVersionObj = ParseVersionSafely(updateInfo.Version);
                    
                    if (currentVersionObj != null && availableVersionObj != null)
                    {
                        if (currentVersionObj > availableVersionObj)
                        {
                            if (VersionService.IsDevelopmentVersion)
                            {
                                LoggingService.Instance.LogInfo($"🚧 Development-Version {currentVersion} ist neuer als verfügbare Release-Version {updateInfo.Version} - normaler Zustand während der Entwicklung");
                            }
                            else
                            {
                                LoggingService.Instance.LogWarning($"⚠️ DOWNGRADE SITUATION: Release-Version {currentVersion} ist neuer als verfügbare Version {updateInfo.Version}");
                                LoggingService.Instance.LogWarning($"   Das sollte nicht passieren! Prüfen Sie die GitHub Releases.");
                            }
                        }
                        else if (currentVersionObj == availableVersionObj)
                        {
                            LoggingService.Instance.LogInfo($"✅ Anwendung ist aktuell: {currentVersion}");
                        }
                    }
                    
                    return null;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Update-Check fehlgeschlagen", ex);
                LoggingService.Instance.LogError($"Exception Details: {ex}");
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
                LoggingService.Instance.LogInfo($"🔍 Version-Vergleich: Remote={newVersionString}, Local={currentVersionString}");

                var isNewer = VersionService.IsNewerVersion(newVersionString, currentVersionString);
                
                LoggingService.Instance.LogInfo($"📊 Vergleichsergebnis: {newVersionString} > {currentVersionString} = {isNewer}");
                
                // Verhindere Downgrades für Entwicklungsversionen
                if (!isNewer && VersionService.IsNewerVersion(currentVersionString, newVersionString))
                {
                    LoggingService.Instance.LogInfo($"⚠️ Downgrade erkannt: Local {currentVersionString} ist neuer als Remote {newVersionString} - Update wird übersprungen");
                }

                return isNewer;
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
                // Verwende zentrale Versionsverwaltung
                return VersionService.Version;
            }
            catch
            {
                return "1.0.0";
            }
        }

        /// <summary>
        /// ✅ Sichere Version-Parsing mit besserer Fehlerbehandlung
        /// </summary>
        private Version? ParseVersionSafely(string versionString)
        {
            try
            {
                if (string.IsNullOrEmpty(versionString))
                    return null;

                // Entferne 'v' Prefix und Development-Suffix falls vorhanden
                var cleanVersion = versionString.TrimStart('v').Split('-')[0];
                
                return new Version(cleanVersion);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"Fehler beim Parsen der Version '{versionString}': {ex.Message}");
                return null;
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
