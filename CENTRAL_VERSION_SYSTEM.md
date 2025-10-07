# 🚀 Zentrales Versionsnummern-System

## 📋 Übersicht

Das neue `VersionService.cs` ist die **einzige Quelle der Wahrheit** für alle Versionsnummern in der Anwendung.

---

## ✅ Zentrale Konfiguration

### Services/VersionService.cs

```csharp
// ZENTRALE VERSIONSDEFINITION - NUR HIER ÄNDERN!
private const string MAJOR_VERSION = "1";
private const string MINOR_VERSION = "9";
private const string PATCH_VERSION = "1";
private const string BUILD_VERSION = "0";

// Development/Release Kennzeichnung
private const bool IS_DEVELOPMENT_VERSION = false;
```

**Für ein neues Release:**
1. Nur diese 5 Konstanten ändern
2. `IS_DEVELOPMENT_VERSION = false` setzen
3. Alle anderen Stellen werden automatisch aktualisiert

---

## 🔄 Automatisch aktualisierte Stellen

### ✅ Services/VersionService.cs
- `Version` → "1.9.1"
- `AssemblyVersion` → "1.9.1.0"  
- `DisplayVersion` → "1.9.1-dev" (Development) oder "1.9.1" (Release)
- `ProductNameWithVersion` → "Einsatzüberwachung Professional v1.9.1"
- `UserAgent` → "Einsatzueberwachung-Professional-v1.9.1"
- `GitTag` → "v1.9.1"

### ✅ App.xaml.cs
- Startup-Log: `VersionService.FullProductName`
- Update-Check nur für Release-Versionen
- Version-Konsistenz-Prüfung

### ✅ GitHubUpdateService.cs
- User-Agent: `VersionService.UserAgent`
- Version-Vergleich: `VersionService.IsNewerVersion()`
- Aktuelle Version: `VersionService.Version`

### ✅ UpdateNotificationViewModel.cs
- Aktuelle Version: `VersionService.DisplayVersion`
- Window-Titel: `VersionService.ProductNameWithVersion`

---

## 📝 Manuell zu aktualisierende Stellen

### ⚠️ Einsatzueberwachung.csproj
```xml
<!-- Diese Werte müssen manuell synchronisiert werden -->
<AssemblyVersion>1.9.1.0</AssemblyVersion>
<FileVersion>1.9.1.0</FileVersion>
<Version>1.9.1</Version>
<AssemblyTitle>Einsatzüberwachung Professional v1.9.1</AssemblyTitle>
<AssemblyProduct>Einsatzüberwachung Professional v1.9.1</AssemblyProduct>
```

**Warum manuell?** MSBuild kann zur Build-Zeit keine C#-Konstanten lesen.

### 🛠️ Helper-Methode für .csproj Update:
```csharp
var versions = VersionUpdateHelper.GetProjectVersions();
// assemblyVersion: "1.9.1.0"
// fileVersion: "1.9.1.0"  
// version: "1.9.1"
// title: "Einsatzüberwachung Professional v1.9.1"
// product: "Einsatzüberwachung Professional v1.9.1"
```

---

## 🚀 Release-Prozess (vereinfacht)

### Schritt 1: Version in VersionService.cs ändern
```csharp
private const string MINOR_VERSION = "9";  // z.B. von "8" auf "9"
private const string PATCH_VERSION = "1";  // z.B. von "0" auf "1"
private const bool IS_DEVELOPMENT_VERSION = false;  // Für Release
```

### Schritt 2: .csproj manuell synchronisieren
```xml
<AssemblyVersion>1.9.1.0</AssemblyVersion>
<FileVersion>1.9.1.0</FileVersion>
<Version>1.9.1</Version>
<AssemblyTitle>Einsatzüberwachung Professional v1.9.1</AssemblyTitle>
<AssemblyProduct>Einsatzüberwachung Professional v1.9.1</AssemblyProduct>
```

### Schritt 3: Build und Release
- Alle anderen Stellen werden automatisch aktualisiert
- Git Tag: `v1.9.1` (aus `VersionService.GitTag`)
- Setup-Name: `Einsatzueberwachung_Professional_v1.9.1.0_Setup.exe`

---

## 🔍 Version-Konsistenz-Prüfung

Das System prüft automatisch, ob die statische Version (VersionService) mit der kompilierten Version (.csproj) übereinstimmt:

```csharp
if (!VersionService.IsVersionConsistent)
{
    LoggingService.Instance.LogWarning($"⚠️ Version-Inkonsistenz: Static={VersionService.Version}, Compiled={VersionService.CompiledVersion}");
}
```

---

## 🎯 Vorteile

### ✅ Zentrale Verwaltung
- **Eine Stelle** für alle Versionsnummern
- **Keine Inkonsistenzen** mehr zwischen verschiedenen Komponenten
- **Einfache Updates** für neue Releases

### ✅ Automatische Development/Release-Unterscheidung
- **Development-Versionen** haben `-dev` Suffix
- **Update-Check** automatisch deaktiviert für Development
- **Klare Kennzeichnung** in Logs und UI

### ✅ Intelligente Version-Vergleiche
- **Zentrale Logik** für Version-Vergleiche
- **Downgrade-Schutz** integriert
- **Konsistente Behandlung** von Version-Formaten

### ✅ Release-Management
- **Helper-Methoden** für verschiedene Komponenten
- **Git-Tag-Konsistenz** automatisch sichergestellt
- **Setup-Naming** folgt automatisch der Version

---

## 📚 API-Referenz

### Eigenschaften
- `VersionService.Version` - Basis-Version (z.B. "1.9.1")
- `VersionService.DisplayVersion` - Mit Development-Suffix
- `VersionService.AssemblyVersion` - 4-teilige Version
- `VersionService.ProductNameWithVersion` - Vollständiger Produktname
- `VersionService.UserAgent` - HTTP User-Agent
- `VersionService.GitTag` - Git Tag Format
- `VersionService.IsDevelopmentVersion` - Development-Flag

### Methoden
- `VersionService.IsNewerVersion(v1, v2)` - Version-Vergleich
- `VersionService.FormatVersion(version)` - Version formatieren
- `VersionUpdateHelper.GetProjectVersions()` - .csproj Werte
- `VersionUpdateHelper.GetSetupVersions()` - Setup Werte
- `VersionUpdateHelper.GetUpdateInfoVersions()` - Update-Info Werte

---

**Erstellt:** 2025-01-05  
**Version:** 1.1  
**Für:** Einsatzüberwachung Professional v1.9.1+
