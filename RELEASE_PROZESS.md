# 🚀 Release-Prozess für Einsatzüberwachung Professional

## 📋 Übersicht

Dieser Prozess stellt sicher, dass alle Versionen konsistent sind und das automatische Update-System funktioniert.

**NEU:** Vollautomatische Versionsnummer-Extraktion aus `Services\VersionService.cs`!

---

## ✅ Vorbereitung für neues Release

### Schritt 1: Version in VersionService.cs ändern

**Datei:** `Services\VersionService.cs`

Ändere **nur** diese Konstanten:
```csharp
// ZENTRALE VERSIONSDEFINITION - NUR HIER ÄNDERN!
private const string MAJOR_VERSION = "1";
private const string MINOR_VERSION = "9";  // z.B. von "8" auf "9"
private const string PATCH_VERSION = "1";  // z.B. von "0" auf "1"
private const string BUILD_VERSION = "0";

// Für Release auf false setzen!
private const bool IS_DEVELOPMENT_VERSION = false;
```

### Schritt 2: .csproj Versionen manuell synchronisieren

**Datei:** `Einsatzueberwachung.csproj`

```xml
<AssemblyVersion>1.9.1.0</AssemblyVersion>
<FileVersion>1.9.1.0</FileVersion>
<Version>1.9.1</Version>
<AssemblyTitle>Einsatzüberwachung Professional v1.9.1</AssemblyTitle>
<AssemblyProduct>Einsatzüberwachung Professional v1.9.1</AssemblyProduct>
```

**Tipp:** Die Helper-Methode zeigt die richtigen Werte:
```csharp
var versions = VersionUpdateHelper.GetProjectVersions();
// Dann manuell in .csproj eintragen
```

---

## 🚀 Automatischer Release (NEU!)

### Option A: Batch-Script (Windows CMD)

```cmd
Create-Release-Tag.bat
```

**Was passiert:**
- ✅ Liest Version automatisch aus `Services\VersionService.cs`
- ✅ Erkennt Development vs. Release-Version
- ✅ Warnt bei Development-Versionen
- ✅ Erstellt Git Tag und pusht zu GitHub
- ✅ Startet automatisch GitHub Actions

### Option B: PowerShell-Script (Erweitert)

```powershell
# Basis-Verwendung
.\Create-Release-Tag.ps1

# Mit Force-Flag (keine Bestätigung)
.\Create-Release-Tag.ps1 -Force

# Dry-Run (zeigt nur was passieren würde)
.\Create-Release-Tag.ps1 -DryRun

# Mit custom Commit-Message
.\Create-Release-Tag.ps1 -Message "Release v1.9.1 - Neue Features"
```

**Erweiterte Features:**
- ✅ Detailliertes Logging mit Farben
- ✅ Git-Status-Prüfung
- ✅ Automatische Tag-Konflikte-Auflösung
- ✅ Force-Modus für Automation
- ✅ Dry-Run für Testing

---

## 🔨 Build-Prozess (Optional)

Falls Sie vor dem Release testen möchten:

```powershell
# Clean Build
dotnet clean "Einsatzueberwachung.csproj"
dotnet build "Einsatzueberwachung.csproj" --configuration Release

# Version der .exe überprüfen
Get-Item "bin\Release\net8.0-windows\Einsatzueberwachung.exe" | Select-Object -ExpandProperty VersionInfo | Select-Object FileVersion

# Setup erstellen (optional)
& "Y:\Inno Setup 6\ISCC.exe" "Setup\Einsatzueberwachung_Setup.iss"
```

---

## 🏷️ Manueller Release (Fallback)

Falls die automatischen Scripts nicht funktionieren:

### Git Bash:
```sh
# 1. Version aus VersionService.cs ablesen (z.B. 1.9.1)
VERSION="1.9.1"
TAG="v$VERSION"

# 2. Commit und Tag
git add .
git commit -m "Prepare release $TAG"
git push origin master
git tag -a $TAG -m "Release $TAG - Einsatzueberwachung Professional"
git push origin $TAG
```

### GitHub Web-Interface:
1. Gehe zu: https://github.com/Elemirus1996/Einsatzueberwachung/releases/new
2. Choose a tag: `v1.9.1` (aus VersionService.cs)
3. Create new tag on publish
4. Publish release

---

## 🔍 Automatische Version-Extraktion

Die neuen Scripts lesen die Version automatisch aus `Services\VersionService.cs`:

```cmd
:: Batch-Script Extraktion
powershell -Command "& {$content = Get-Content 'Services\VersionService.cs' -Raw; if ($content -match 'private const string MAJOR_VERSION = \"(\d+)\"') {$major = $matches[1]} else {$major = '1'}; if ($content -match 'private const string MINOR_VERSION = \"(\d+)\"') {$minor = $matches[1]} else {$minor = '0'}; if ($content -match 'private const string PATCH_VERSION = \"(\d+)\"') {$patch = $matches[1]} else {$patch = '0'}; Write-Output \"$major.$minor.$patch\"}"
```

**Vorteile:**
- ✅ **Keine manuellen Versionsnummern** mehr in Scripts
- ✅ **Automatische Synchronisation** mit VersionService
- ✅ **Fehlerreduzierung** durch zentrale Quelle
- ✅ **Development-Erkennung** automatisch

---

## ✅ Checkliste für automatischen Release

- [ ] **Version in VersionService.cs erhöht**
- [ ] **IS_DEVELOPMENT_VERSION = false gesetzt**
- [ ] **Einsatzueberwachung.csproj Versionen aktualisiert**
- [ ] **Build getestet** (optional)
- [ ] **Script ausgeführt**: `Create-Release-Tag.bat` ODER `Create-Release-Tag.ps1`
- [ ] **GitHub Actions Workflow überwacht**
- [ ] **Release getestet**

---

## 🔍 Versionskonsistenz-Prüfung

Das System prüft automatisch die Konsistenz:

```powershell
# In der Anwendung beim Start
if (!VersionService.IsVersionConsistent) {
    LoggingService.Instance.LogWarning($"⚠️ Version-Inkonsistenz: Static={VersionService.Version}, Compiled={VersionService.CompiledVersion}");
}
```

**Nach dem Release überprüfen:**
1. **GitHub Release:** https://github.com/Elemirus1996/Einsatzueberwachung/releases/latest
2. **Setup-Dateiname:** `Einsatzueberwachung_Professional_v1.9.1.0_Setup.exe`
3. **update-info.json:** Automatisch von GitHub Actions erstellt

---

## 🐛 Troubleshooting

### Problem: "Version konnte nicht extrahiert werden"

**Ursache:** `Services\VersionService.cs` nicht gefunden oder Parsing-Fehler

**Lösung:**
```cmd
# Script aus Hauptverzeichnis ausführen
cd C:\path\to\Einsatzueberwachung
Create-Release-Tag.bat
```

### Problem: Development-Version wird nicht erkannt

**Ursache:** `IS_DEVELOPMENT_VERSION` nicht korrekt gesetzt

**Lösung:**
```csharp
// In Services\VersionService.cs
private const bool IS_DEVELOPMENT_VERSION = false;  // Für Release
```

### Problem: Tag kann nicht gepusht werden

**Ursache:** Repository Rules oder Berechtigung

**Lösung:**
1. Repository Rules temporär deaktivieren
2. Manueller Release über GitHub Web-Interface
3. Personal Access Token verwenden

### Problem: .csproj Version stimmt nicht überein

**Ursache:** Manueller Sync vergessen

**Lösung:**
```csharp
// Helper-Methode verwenden
var versions = VersionUpdateHelper.GetProjectVersions();
// Werte manuell in .csproj eintragen
