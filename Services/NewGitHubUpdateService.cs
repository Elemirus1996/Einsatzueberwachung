using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Einsatzueberwachung.Services
{
    /// <summary>
    /// Komplett neuer, sauberer GitHub Update Service
    /// Löst das persistente v1.7.0 Problem definitiv
    /// </summary>
    public class NewGitHubUpdateService : IDisposable
    {
        private const string GITHUB_REPO = "Elemirus1996/Einsatzueberwachung";
        private const string GITHUB_API_URL = "https://api.github.com/repos/{0}/releases/latest";
        
        private readonly HttpClient _httpClient;
        private bool _disposed = false;

        public NewGitHubUpdateService()
        {
            _httpClient = new HttpClient();
            
            // Einfache, saubere Headers ohne komplexe Cache-Logik
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "EinsatzueberwachungNew/1.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Prüft auf Updates - EINFACH und DIREKT
        /// </summary>
        public async Task<SimpleUpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                LoggingService.Instance.LogInfo("🔄 NEW UPDATE SERVICE: Checking for updates...");
                
                var currentVersion = VersionService.Version; // z.B. "1.9.0"
                LoggingService.Instance.LogInfo($"📍 Current Version: {currentVersion}");
                
                // Direkte GitHub API Abfrage
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
                
                // Logge einen Teil der Antwort für Debugging
                var preview = jsonContent.Length > 200 ? jsonContent.Substring(0, 200) + "..." : jsonContent;
                LoggingService.Instance.LogInfo($"📋 Response Preview: {preview}");

                var releaseData = JsonSerializer.Deserialize<GitHubReleaseResponse>(jsonContent, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });

                if (releaseData?.TagName == null)
                {
                    LoggingService.Instance.LogWarning("❌ No valid release data received");
                    return null;
                }

                var githubVersion = releaseData.TagName.TrimStart('v'); // Entferne 'v' prefix
                LoggingService.Instance.LogInfo($"🎯 GitHub Version: {githubVersion}");
                LoggingService.Instance.LogInfo($"🏠 Current Version: {currentVersion}");

                // EINFACHER Versionsvergleich
                var currentVersionObj = new Version(currentVersion);
                var githubVersionObj = new Version(githubVersion);
                
                LoggingService.Instance.LogInfo($"🔢 Parsed Versions: Current={currentVersionObj}, GitHub={githubVersionObj}");
                
                var isNewerAvailable = githubVersionObj > currentVersionObj;
                LoggingService.Instance.LogInfo($"📊 Is GitHub version newer? {isNewerAvailable}");

                if (isNewerAvailable)
                {
                    LoggingService.Instance.LogInfo($"✅ UPDATE AVAILABLE: {currentVersion} → {githubVersion}");
                    
                    return new SimpleUpdateInfo
                    {
                        Version = githubVersion,
                        CurrentVersion = currentVersion,
                        TagName = releaseData.TagName,
                        ReleaseDate = releaseData.PublishedAt?.ToString("yyyy-MM-dd") ?? "Unknown",
                        ReleaseNotesUrl = releaseData.HtmlUrl ?? $"https://github.com/{GITHUB_REPO}/releases/tag/{releaseData.TagName}",
                        DownloadUrl = GetDownloadUrl(releaseData),
                        ReleaseNotes = new[] { $"Update auf Version {githubVersion} verfügbar" }
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
                LoggingService.Instance.LogError($"❌ NEW UPDATE SERVICE ERROR: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Findet Download-URL aus den Release Assets
        /// </summary>
        private string GetDownloadUrl(GitHubReleaseResponse releaseData)
        {
            try
            {
                if (releaseData.Assets != null)
                {
                    foreach (var asset in releaseData.Assets)
                    {
                        if (asset.Name?.Contains("Setup.exe") == true)
                        {
                            LoggingService.Instance.LogInfo($"📦 Found setup asset: {asset.Name}");
                            return asset.BrowserDownloadUrl ?? "";
                        }
                    }
                }
                
                LoggingService.Instance.LogWarning("⚠️ No setup.exe asset found in release");
                return "";
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"⚠️ Error finding download URL: {ex.Message}");
                return "";
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
    /// Einfache Update-Info Klasse
    /// </summary>
    public class SimpleUpdateInfo
    {
        public string Version { get; set; } = "";
        public string CurrentVersion { get; set; } = "";
        public string TagName { get; set; } = "";
        public string ReleaseDate { get; set; } = "";
        public string ReleaseNotesUrl { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string[] ReleaseNotes { get; set; } = Array.Empty<string>();
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
