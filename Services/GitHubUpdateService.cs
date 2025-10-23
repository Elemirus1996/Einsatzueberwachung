using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.Linq;

namespace Einsatzueberwachung.Services
{
    /// <summary>
    /// 🔄 GitHub Update Service v2.0
    /// Vollständiger Update-Service mit Check, Download und Installation
    /// </summary>
    public class GitHubUpdateService : IDisposable
    {
        private const string GITHUB_REPO = "Elemirus1996/Einsatzueberwachung";
        private const string GITHUB_API_URL = "https://api.github.com/repos/{0}/releases/latest";
        
        private readonly HttpClient _httpClient;
        private bool _disposed = false;

        public GitHubUpdateService()
        {
            _httpClient = new HttpClient();
            
            // Setze User-Agent und Headers
            _httpClient.DefaultRequestHeaders.Add("User-Agent", VersionService.UserAgent);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Prüft auf verfügbare Updates
        /// </summary>
        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                LoggingService.Instance.LogInfo("🔄 UPDATE SERVICE: Checking for updates...");
                
                var currentVersion = VersionService.Version;
                LoggingService.Instance.LogInfo($"📍 Current Version: {currentVersion}");
                
                // GitHub API Abfrage
                var apiUrl = string.Format(GITHUB_API_URL, GITHUB_REPO);
                LoggingService.Instance.LogInfo($"🌐 API URL: {apiUrl}");
                
                var response = await _httpClient.GetAsync(apiUrl);
                LoggingService.Instance.LogInfo($"📊 Response Status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    LoggingService.Instance.LogWarning($"❌ GitHub API failed: {response.StatusCode}");
                    return null;
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                LoggingService.Instance.LogInfo($"📄 Response Length: {jsonContent.Length} chars");

                var releaseData = JsonSerializer.Deserialize<GitHubReleaseResponse>(jsonContent, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });

                if (releaseData?.TagName == null)
                {
                    LoggingService.Instance.LogWarning("❌ No valid release data received");
                    return null;
                }

                var githubVersion = releaseData.TagName.TrimStart('v');
                LoggingService.Instance.LogInfo($"🎯 GitHub Version: {githubVersion}");
                LoggingService.Instance.LogInfo($"🏠 Current Version: {currentVersion}");

                // Versionsvergleich
                if (!Version.TryParse(currentVersion, out var currentVersionObj))
                {
                    LoggingService.Instance.LogWarning($"❌ Could not parse current version: {currentVersion}");
                    return null;
                }

                if (!Version.TryParse(githubVersion, out var githubVersionObj))
                {
                    LoggingService.Instance.LogWarning($"❌ Could not parse GitHub version: {githubVersion}");
                    return null;
                }
                
                LoggingService.Instance.LogInfo($"🔢 Parsed Versions: Current={currentVersionObj}, GitHub={githubVersionObj}");
                
                var isNewerAvailable = githubVersionObj > currentVersionObj;
                LoggingService.Instance.LogInfo($"📊 Is GitHub version newer? {isNewerAvailable}");

                if (isNewerAvailable)
                {
                    LoggingService.Instance.LogInfo($"✅ UPDATE AVAILABLE: {currentVersion} → {githubVersion}");
                    
                    // Hole Download-URL und Größe
                    var (downloadUrl, fileSize) = GetDownloadInfo(releaseData);
                    
                    return new UpdateInfo
                    {
                        Version = githubVersion,
                        CurrentVersion = currentVersion,
                        TagName = releaseData.TagName,
                        ReleaseDate = releaseData.PublishedAt?.ToString("yyyy-MM-dd") ?? "Unbekannt",
                        ReleaseNotesUrl = releaseData.HtmlUrl ?? $"https://github.com/{GITHUB_REPO}/releases/tag/{releaseData.TagName}",
                        DownloadUrl = downloadUrl,
                        FileSize = fileSize,
                        ReleaseNotes = new[] { $"Update auf Version {githubVersion} verfügbar" },
                        Mandatory = false
                    };
                }
                else
                {
                    LoggingService.Instance.LogInfo($"✅ NO UPDATE NEEDED: Current version {currentVersion} is up to date");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"❌ UPDATE SERVICE ERROR: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Lädt ein Update herunter
        /// </summary>
        public async Task<string> DownloadUpdateAsync(UpdateInfo updateInfo, IProgress<int>? progress = null)
        {
            try
            {
                if (string.IsNullOrEmpty(updateInfo.DownloadUrl))
                {
                    LoggingService.Instance.LogWarning("❌ No download URL available");
                    return string.Empty;
                }

                LoggingService.Instance.LogInfo($"📥 Downloading update from: {updateInfo.DownloadUrl}");

                // Download-Verzeichnis erstellen
                var downloadDir = Path.Combine(Path.GetTempPath(), "EinsatzueberwachungUpdates");
                Directory.CreateDirectory(downloadDir);

                var fileName = $"Einsatzueberwachung_Professional_v{updateInfo.Version}_Setup.exe";
                var filePath = Path.Combine(downloadDir, fileName);

                // Lösche alte Datei falls vorhanden
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Download mit Progress
                using (var response = await _httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? 0;
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        long totalRead = 0;
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalRead += bytesRead;

                            if (totalBytes > 0 && progress != null)
                            {
                                var progressPercentage = (int)((totalRead * 100) / totalBytes);
                                progress.Report(progressPercentage);
                            }
                        }
                    }
                }

                LoggingService.Instance.LogInfo($"✅ Update downloaded successfully to: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"❌ Download failed: {ex.Message}", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Installiert ein heruntergeladenes Update
        /// </summary>
        public async Task<bool> InstallUpdateAsync(string installerPath, bool closeApplication = true)
        {
            try
            {
                if (!File.Exists(installerPath))
                {
                    LoggingService.Instance.LogWarning($"❌ Installer not found: {installerPath}");
                    return false;
                }

                LoggingService.Instance.LogInfo($"🚀 Starting update installation: {installerPath}");

                // Starte Installer
                var startInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    UseShellExecute = true,
                    Verb = "runas" // Als Administrator ausführen
                };

                Process.Start(startInfo);

                LoggingService.Instance.LogInfo("✅ Update installer started successfully");

                // Warte kurz damit der Installer sicher startet
                await Task.Delay(1000);

                // Schließe Anwendung wenn gewünscht
                if (closeApplication)
                {
                    LoggingService.Instance.LogInfo("🔄 Closing application for update installation...");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Application.Current.Shutdown();
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"❌ Installation failed: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Holt Download-URL und Dateigröße aus Release-Daten
        /// </summary>
        private (string downloadUrl, long fileSize) GetDownloadInfo(GitHubReleaseResponse releaseData)
        {
            try
            {
                if (releaseData.Assets != null && releaseData.Assets.Length > 0)
                {
                    // Suche nach Setup.exe
                    var setupAsset = releaseData.Assets.FirstOrDefault(a => 
                        a.Name?.Contains("Setup.exe", StringComparison.OrdinalIgnoreCase) == true);

                    if (setupAsset != null)
                    {
                        LoggingService.Instance.LogInfo($"📦 Found setup asset: {setupAsset.Name}, Size: {setupAsset.Size} bytes");
                        return (setupAsset.BrowserDownloadUrl ?? "", setupAsset.Size);
                    }
                }
                
                LoggingService.Instance.LogWarning("⚠️ No setup.exe asset found in release");
                return ("", 0);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"⚠️ Error getting download info: {ex.Message}");
                return ("", 0);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Update-Informationen
    /// </summary>
    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string CurrentVersion { get; set; } = "";
        public string TagName { get; set; } = "";
        public string ReleaseDate { get; set; } = "";
        public string ReleaseNotesUrl { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public long FileSize { get; set; } = 0;
        public string[] ReleaseNotes { get; set; } = Array.Empty<string>();
        public bool Mandatory { get; set; } = false;
    }

    /// <summary>
    /// GitHub API Response Struktur
    /// </summary>
    public class GitHubReleaseResponse
    {
        public string? TagName { get; set; }
        public string? Name { get; set; }
        public string? HtmlUrl { get; set; }
        public DateTime? PublishedAt { get; set; }
        public bool Draft { get; set; }
        public bool Prerelease { get; set; }
        public GitHubAssetResponse[]? Assets { get; set; }
    }

    /// <summary>
    /// GitHub Asset Response
    /// </summary>
    public class GitHubAssetResponse
    {
        public string? Name { get; set; }
        public string? BrowserDownloadUrl { get; set; }
        public long Size { get; set; }
    }
}
