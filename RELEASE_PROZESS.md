# 🚀 Release-Prozess für Einsatzüberwachung Professional

## 📋 Übersicht

Dieser Prozess stellt sicher, dass alle Versionen konsistent sind und das automatische Update-System funktioniert.

---

## ✅ Vorbereitung für neues Release

### Schritt 1: Version in `.csproj` erhöhen

**Datei:** `Einsatzueberwachung.csproj`

Ändere **nur** diese Zeilen:
```xml
<AssemblyVersion>1.7.2.0</AssemblyVersion>
<FileVersion>1.7.2.0</FileVersion>
<Version>1.7.2</Version>
```

**WICHTIG:** 
- Das sind die **einzigen** Stellen, wo die Version geändert werden muss!
- Das Inno Setup Script liest die Version **automatisch** aus der kompilierten `.exe`
- Die `release.yml` validiert, dass Git-Tag und `.csproj` übereinstimmen

### Schritt 2: Optional - AssemblyTitle und AssemblyProduct aktualisieren

Wenn du möchtest, kannst du auch diese Zeilen anpassen (optional):
```xml
<AssemblyTitle>Einsatzüberwachung Professional v1.7.2</AssemblyTitle>
<AssemblyProduct>Einsatzüberwachung Professional v1.7.2</AssemblyProduct>
```

---

## 🔨 Build-Prozess

### Schritt 1: Clean Build durchführen

```powershell
dotnet clean "Einsatzueberwachung.csproj"
dotnet build "Einsatzueberwachung.csproj" --configuration Release
```

### Schritt 2: Version der .exe überprüfen

```powershell
Get-Item "bin\Release\net8.0-windows\Einsatzueberwachung.exe" | Select-Object -ExpandProperty VersionInfo | Select-Object FileVersion
```

**Erwartete Ausgabe:** `1.7.2.0` (oder deine neue Version)

### Schritt 3: Setup erstellen

```powershell
& "Y:\Inno Setup 6\ISCC.exe" "Setup\Einsatzueberwachung_Setup.iss"
```

**Das Setup wird automatisch benannt:** `Einsatzueberwachung_Professional_v1.7.2.0_Setup.exe`

### Schritt 4: Setup überprüfen

```powershell
Get-ChildItem "Setup\Output" -Filter "*.exe" | Select-Object Name, @{Name="Size (MB)";Expression={[math]::Round($_.Length/1MB, 2)}}, LastWriteTime
```

---

## 🏷️ Git Tag erstellen und Release veröffentlichen

### **WICHTIG:** Repository Rules Check

Bevor du Tags pushst, überprüfe ob Tag-Protection aktiv ist:
- Gehe zu: https://github.com/Elemirus1996/Einsatzueberwachung/settings/rules
- Falls eine Rule für Tags (`v*`) existiert, **deaktiviere** sie temporär

### Option A: Automatischer Release via GitHub Actions (EMPFOHLEN)

**Voraussetzung:** Tag-Protection muss deaktiviert sein

**In Git Bash:**
```sh
# 1. Alle Änderungen committen
git add .
git commit -m "Release v1.7.2 - [Beschreibung der Änderungen]"

# 2. Push zu master
git push origin master

# 3. Tag erstellen
git tag v1.7.2

# 4. Tag pushen (startet automatisch GitHub Actions)
git push origin v1.7.2
```

**Was passiert dann:**
1. GitHub Actions startet automatisch
2. Validiert dass Git-Tag (v1.7.2) mit `.csproj` Version (1.7.2) übereinstimmt
3. Baut die Anwendung
4. Erstellt das Setup
5. Generiert `update-info.json`
6. Erstellt GitHub Release mit beiden Dateien

**Überwache den Workflow:** https://github.com/Elemirus1996/Einsatzueberwachung/actions

### Option B: Manueller Release (wenn Tag-Protection nicht deaktiviert werden kann)

**In Git Bash:**
```sh
# 1. Alle Änderungen committen und pushen
git add .
git commit -m "Release v1.7.2 - [Beschreibung der Änderungen]"
git push origin master
```

**Dann auf GitHub:**

1. **Gehe zu:** https://github.com/Elemirus1996/Einsatzueberwachung/releases/new

2. **Choose a tag:** Gib `v1.7.2` ein und klicke "Create new tag: v1.7.2 on publish"

3. **Release title:** `Release v1.7.2 - [Kurzbeschreibung]`

4. **Description:**
```markdown
# 🚀 Einsatzüberwachung Professional v1.7.2

## ✨ Neue Features:
- [Feature 1]
- [Feature 2]

## 🔧 Verbesserungen:
- [Verbesserung 1]
- [Verbesserung 2]

## 🐛 Bug Fixes:
- [Fix 1]
- [Fix 2]

## 📥 Installation:
1. Laden Sie `Einsatzueberwachung_Professional_v1.7.2.0_Setup.exe` herunter
2. Als Administrator ausführen
3. Setup-Assistenten folgen

## 🔄 Automatisches Update:
Benutzer mit Version 1.7.1+ erhalten automatisch eine Update-Benachrichtigung!
```

5. **Dateien hochladen:**
   - `Setup\Output\Einsatzueberwachung_Professional_v1.7.2.0_Setup.exe`
   - Erstelle `update-info.json` (siehe unten)

6. **Publish release** klicken

---

## 📄 update-info.json manuell erstellen (nur bei Option B nötig)

**Datei erstellen:** `update-info.json`

```json
{
  "version": "1.7.2",
  "releaseDate": "2025-01-05",
  "downloadUrl": "https://github.com/Elemirus1996/Einsatzueberwachung/releases/download/v1.7.2/Einsatzueberwachung_Professional_v1.7.2.0_Setup.exe",
  "releaseNotesUrl": "https://github.com/Elemirus1996/Einsatzueberwachung/releases/tag/v1.7.2",
  "mandatory": false,
  "minimumVersion": "1.7.0",
  "fileSize": 36700160,
  "releaseNotes": [
    "🚀 Neue Features - [Beschreibung]",
    "🔧 Verbesserungen - [Beschreibung]",
    "🐛 Bug Fixes - [Beschreibung]"
  ]
}
```

**WICHTIG:** Passe diese Werte an:
- `version`: Neue Versionsnummer ohne "v"
- `releaseDate`: Aktuelles Datum (YYYY-MM-DD)
- `downloadUrl`: Ersetze `v1.7.2` mit deiner neuen Version (2x)
- `fileSize`: Größe der Setup-Datei in Bytes (siehe unten)
- `releaseNotes`: Deine Release-Notes

**Dateigröße in Bytes ermitteln:**
```powershell
(Get-Item "Setup\Output\Einsatzueberwachung_Professional_v1.7.2.0_Setup.exe").Length
```

---

## ✅ Checkliste vor Release

- [ ] Version in `.csproj` erhöht
- [ ] Clean Build durchgeführt
- [ ] Setup erstellt und getestet
- [ ] Setup-Dateiname enthält korrekte Version
- [ ] Alle Änderungen committed und gepusht
- [ ] Git Tag erstellt
- [ ] Release auf GitHub veröffentlicht
- [ ] `update-info.json` hochgeladen
- [ ] Release als "latest" markiert

---

## 🔍 Versionskonsistenz überprüfen

**Nach dem Release überprüfen:**

1. **GitHub Release:** https://github.com/Elemirus1996/Einsatzueberwachung/releases/latest
   - ✅ Setup-Dateiname: `Einsatzueberwachung_Professional_v1.7.2.0_Setup.exe`
   - ✅ `update-info.json` vorhanden

2. **update-info.json downloaden und prüfen:**
```powershell
Invoke-WebRequest "https://github.com/Elemirus1996/Einsatzueberwachung/releases/latest/download/update-info.json" -OutFile "test-update-info.json"
Get-Content "test-update-info.json"
```

3. **Automatisches Update testen:**
   - Installiere eine ältere Version (z.B. v1.7.1)
   - Starte die Anwendung
   - Warte auf Update-Benachrichtigung (ca. 5-10 Sekunden)
   - Update sollte v1.7.2 anzeigen

---

## 🐛 Troubleshooting

### Problem: "Version mismatch" Fehler in GitHub Actions

**Ursache:** Git-Tag und `.csproj` Version stimmen nicht überein

**Lösung:**
```sh
# Tag löschen
git tag -d v1.7.2
git push origin --delete v1.7.2

# .csproj korrigieren, dann neu:
git add Einsatzueberwachung.csproj
git commit -m "Fix: Version corrected"
git push origin master
git tag v1.7.2
git push origin v1.7.2
```

### Problem: Setup hat falsche Versionsnummer im Dateinamen

**Ursache:** Build wurde nicht neu durchgeführt nach Version-Änderung

**Lösung:**
```powershell
dotnet clean "Einsatzueberwachung.csproj"
dotnet build "Einsatzueberwachung.csproj" --configuration Release
& "Y:\Inno Setup 6\ISCC.exe" "Setup\Einsatzueberwachung_Setup.iss"
```

### Problem: Tag kann nicht gepusht werden (Repository Rules)

**Ursache:** Tag-Protection ist aktiv

**Lösung:**
1. Gehe zu: https://github.com/Elemirus1996/Einsatzueberwachung/settings/rules
2. Deaktiviere die Tag-Protection temporär
3. Oder nutze Option B (Manueller Release)

### Problem: Automatisches Update funktioniert nicht

**Überprüfe:**
1. Ist `update-info.json` im Release vorhanden?
2. Ist der Release als "latest" markiert?
3. Ist die Version in `update-info.json` höher als die installierte Version?
4. Hat der Benutzer Internet-Zugang?

---

## 📚 Wichtige Dateien

### Dateien, die du ÄNDERN musst:
- ✅ `Einsatzueberwachung.csproj` - Version erhöhen

### Dateien, die du NICHT ändern musst:
- ❌ `Setup/Einsatzueberwachung_Setup.iss` - Liest Version automatisch aus
- ❌ `.github/workflows/release.yml` - Validiert automatisch

### Generierte Dateien (nicht committen):
- `Setup/Output/*.exe` - Wird bei jedem Build neu erstellt
- `update-info.json` - Wird von GitHub Actions erstellt (oder manuell)

---

## 🎯 Quick Reference

### Schneller Release-Prozess (wenn alles funktioniert):

```powershell
# 1. Version in Einsatzueberwachung.csproj ändern (z.B. 1.7.2)

# 2. Build
dotnet clean "Einsatzueberwachung.csproj"
dotnet build "Einsatzueberwachung.csproj" --configuration Release

# 3. Setup erstellen
& "Y:\Inno Setup 6\ISCC.exe" "Setup\Einsatzueberwachung_Setup.iss"

# 4. Git (in Git Bash)
git add .
git commit -m "Release v1.7.2 - [Beschreibung]"
git push origin master
git tag v1.7.2
git push origin v1.7.2

# 5. Warten auf GitHub Actions oder manueller Release
```

---

**Erstellt:** 2025-01-05  
**Version:** 1.0  
**Für:** Einsatzüberwachung Professional v1.7.1+
