using System;
using System.Reflection;

namespace Einsatzueberwachung.Services
{
    /// <summary>
    /// Zentraler Service für Versionsnummern-Management
    /// Einzige Quelle der Wahrheit für alle Versionsangaben in der Anwendung
    /// </summary>
    public static class VersionService
    {
        // ZENTRALE VERSIONSDEFINITION - NUR HIER ÄNDERN!
        private const string MAJOR_VERSION = "1";
        private const string MINOR_VERSION = "9";
        private const string PATCH_VERSION = "1";
        private const string BUILD_VERSION = "1";  // ✅ UPDATED: Bug-Fix Release 1.9.0.1
        
        // Development/Release Kennzeichnung
        private const bool IS_DEVELOPMENT_VERSION = false;  // ✅ Release-Modus aktiviert
        private const string DEVELOPMENT_SUFFIX = "-dev";
        
        /// <summary>
        /// Vollständige Versionsnummer (z.B. "1.9.0")
        /// </summary>
        public static string Version => $"{MAJOR_VERSION}.{MINOR_VERSION}.{PATCH_VERSION}";
        
        /// <summary>
        /// Vollständige Assembly-Version (z.B. "1.9.0.0")
        /// </summary>
        public static string AssemblyVersion => $"{MAJOR_VERSION}.{MINOR_VERSION}.{PATCH_VERSION}.{BUILD_VERSION}";
        
        /// <summary>
        /// Version mit Development-Suffix (z.B. "1.9.0-dev")
        /// </summary>
        public static string DisplayVersion => IS_DEVELOPMENT_VERSION ? $"{Version}{DEVELOPMENT_SUFFIX}" : Version;
        
        /// <summary>
        /// Produktname mit Version (z.B. "Einsatzüberwachung Professional v1.9.0")
        /// </summary>
        public static string ProductNameWithVersion => $"Einsatzüberwachung Professional v{Version}";
        
        /// <summary>
        /// Vollständiger Produktname mit Development-Kennzeichnung
        /// </summary>
        public static string FullProductName => $"Einsatzüberwachung Professional v{DisplayVersion}";
        
        /// <summary>
        /// User Agent für HTTP-Requests
        /// </summary>
        public static string UserAgent => $"Einsatzueberwachung-Professional-v{Version}";
        
        /// <summary>
        /// Git Tag Format (z.B. "v1.9.0")
        /// </summary>
        public static string GitTag => $"v{Version}";
        
        /// <summary>
        /// Ist dies eine Entwicklungsversion?
        /// </summary>
        public static bool IsDevelopmentVersion => IS_DEVELOPMENT_VERSION;
        
        /// <summary>
        /// Versionsnummer als Version-Objekt
        /// </summary>
        public static Version VersionObject => new Version(
            int.Parse(MAJOR_VERSION), 
            int.Parse(MINOR_VERSION), 
            int.Parse(PATCH_VERSION), 
            int.Parse(BUILD_VERSION)
        );
        
        /// <summary>
        /// Release-Jahr
        /// </summary>
        public static string ReleaseYear => "2024";
        
        /// <summary>
        /// Copyright-String
        /// </summary>
        public static string Copyright => $"Copyright © {ReleaseYear} RescueDog_SW";
        
        /// <summary>
        /// Ermittelt die tatsächlich kompilierte Version aus der Assembly
        /// (als Fallback und Vergleich)
        /// </summary>
        public static string CompiledVersion
        {
            get
            {
                try
                {
                    var version = Assembly.GetExecutingAssembly().GetName().Version;
                    if (version != null)
                    {
                        return $"{version.Major}.{version.Minor}.{version.Build}";
                    }
                    return Version; // Fallback auf statische Version
                }
                catch
                {
                    return Version; // Fallback auf statische Version
                }
            }
        }
        
        /// <summary>
        /// Prüft ob die kompilierte Version mit der statischen Version übereinstimmt
        /// </summary>
        public static bool IsVersionConsistent
        {
            get
            {
                try
                {
                    return CompiledVersion == Version;
                }
                catch
                {
                    return false;
                }
            }
        }
        
        /// <summary>
        /// Vergleicht zwei Versionsnummern
        /// </summary>
        /// <param name="version1">Erste Version (z.B. "1.9.0")</param>
        /// <param name="version2">Zweite Version (z.B. "1.8.0")</param>
        /// <returns>True wenn version1 > version2</returns>
        public static bool IsNewerVersion(string version1, string version2)
        {
            try
            {
                // Entferne 'v' Prefix und Development-Suffix falls vorhanden
                version1 = version1.TrimStart('v').Split('-')[0];
                version2 = version2.TrimStart('v').Split('-')[0];
                
                var v1 = new Version(version1);
                var v2 = new Version(version2);
                
                return v1 > v2;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Formatiert eine Versionsnummer für die Anzeige
        /// </summary>
        /// <param name="version">Version (z.B. "1.9.0")</param>
        /// <param name="includeDevelopment">Development-Suffix anzeigen falls zutreffend</param>
        /// <returns>Formatierte Version</returns>
        public static string FormatVersion(string version, bool includeDevelopment = true)
        {
            if (string.IsNullOrEmpty(version))
                return "Unbekannt";
                
            var cleanVersion = version.TrimStart('v');
            
            if (includeDevelopment && IS_DEVELOPMENT_VERSION && cleanVersion == Version)
            {
                return $"{cleanVersion}{DEVELOPMENT_SUFFIX}";
            }
            
            return cleanVersion;
        }
    }
    
    /// <summary>
    /// Version-Update Helper für Release-Management
    /// </summary>
    public static class VersionUpdateHelper
    {
        /// <summary>
        /// Generiert die Werte für .csproj Update
        /// </summary>
        public static (string assemblyVersion, string fileVersion, string version, string title, string product) GetProjectVersions()
        {
            return (
                assemblyVersion: VersionService.AssemblyVersion,
                fileVersion: VersionService.AssemblyVersion,
                version: VersionService.Version,
                title: VersionService.ProductNameWithVersion,
                product: VersionService.ProductNameWithVersion
            );
        }
        
        /// <summary>
        /// Generiert Inno Setup Script Werte
        /// </summary>
        public static (string version, string appName, string userAgent) GetSetupVersions()
        {
            return (
                version: VersionService.Version,
                appName: VersionService.ProductNameWithVersion,
                userAgent: VersionService.UserAgent
            );
        }
        
        /// <summary>
        /// Generiert Update-Info JSON Werte
        /// </summary>
        public static (string version, string displayName) GetUpdateInfoVersions()
        {
            return (
                version: VersionService.Version,
                displayName: VersionService.FullProductName
            );
        }
    }
}
