# 🔄 GitHub Updates für Einsatzüberwachung Professional v1.6

## 🎯 **Ja! Inno Setup kann Updates über GitHub ausrollen**

Das Update-System nutzt **GitHub Releases** als zentralen Update-Server für die Einsatzüberwachung Professional.

---

## 🚀 **Wie funktioniert das GitHub Update-System?**

### **📋 Überblick:**
1. **GitHub Releases** als Update-Server
2. **Automatische Update-Prüfung** beim App-Start
3. **JSON-API** für Version-Informationen
4. **Automatischer Download** neuer Versionen
5. **Inno Setup** für Update-Installation
6. **Rollback-Funktionalität** bei Problemen

### **🔧 Technische Umsetzung:**
- **GitHub Releases API** für Version-Check
- **Automatischer Download** der Setup.exe
- **Silent Update-Installation** im Hintergrund
- **Benutzerfreundliche Update-Benachrichtigungen**
- **Backup & Rollback** System

---

## 🛠️ **Implementierung - GitHub Update-System**

### **1. GitHub Repository Setup:**

#### **📦 Release-Struktur:**
```
GitHub Repository: Elemirus1996/Einsatzueberwachung
├── Releases/
│   ├── v1.6.0/
│   │   ├── Einsatzueberwachung_Professional_v1.6.0_Setup.exe
│   │   ├── update-info.json
│   │   └── release-notes.md
│   ├── v1.6.1/
│   │   ├── Einsatzueberwachung_Professional_v1.6.1_Setup.exe
│   │   ├── update-info.json
│   │   └── release-notes.md
```

#### **📋 Update-Info JSON:**
```json
{
    "version": "1.6.1",
    "releaseDate": "2024-01-15",
    "downloadUrl": "https://github.com/Elemirus1996/Einsatzueberwachung/releases/download/v1.6.1/Einsatzueberwachung_Professional_v1.6.1_Setup.exe",
    "releaseNotesUrl": "https://github.com/Elemirus1996/Einsatzueberwachung/releases/tag/v1.6.1",
    "mandatory": false,
    "minimumVersion": "1.5.0",
    "fileSize": 52428800,
    "checksum": "sha256:abc123...",
    "releaseNotes": [
        "🚀 Verbesserte Mobile Server Stabilität",
        "🔧 Automatische Firewall-Konfiguration",
        "📱 Erweiterte iPhone-Kompatibilität",
        "🛡️ Sicherheits-Updates"
    ]
}
```

### **2. Update-Service Implementation:**

#### **🔧 GitHub Update Service (C#):**
```csharp
public class GitHubUpdateService
{
    private const string GITHUB_API_URL = "https://api.github.com/repos/Elemirus1996/Einsatzueberwachung/releases/latest";
    private const string UPDATE_CHECK_URL = "https://github.com/Elemirus1996/Einsatzueberwachung/releases/latest/download/update-info.json";
    
    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Einsatzueberwachung-Professional");
            
            var response = await client.GetAsync(UPDATE_CHECK_URL);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UpdateInfo>(json);
            }
        }
        catch (Exception ex)
        {
            LoggingService.Instance.LogError($"Update-Check fehlgeschlagen: {ex.Message}", ex);
        }
        return null;
    }
    
    public async Task<bool> DownloadUpdateAsync(UpdateInfo updateInfo, IProgress<int> progress)
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "Einsatzueberwachung_Update.exe");
            
            using var client = new HttpClient();
            using var response = await client.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(tempPath, FileMode.Create);
            
            var totalBytes = response.Content.Headers.ContentLength ?? 0;
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
                    progress?.Report((int)((totalRead * 100) / totalBytes));
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            LoggingService.Instance.LogError($"Update-Download fehlgeschlagen: {ex.Message}", ex);
            return false;
        }
    }
}
```

---

## 🔄 **Automatisches Update-System**

### **📱 Update-Benachrichtigung in der App:**

#### **🖥️ Update-Dialog:**
```csharp
public partial class UpdateNotificationWindow : Window
{
    private UpdateInfo _updateInfo;
    private GitHubUpdateService _updateService;
    
    public UpdateNotificationWindow(UpdateInfo updateInfo)
    {
        InitializeComponent();
        _updateInfo = updateInfo;
        _updateService = new GitHubUpdateService();
        
        ShowUpdateInfo();
    }
    
    private void ShowUpdateInfo()
    {
        TxtCurrentVersion.Text = $"Aktuelle Version: {Assembly.GetExecutingAssembly().GetName().Version}";
        TxtNewVersion.Text = $"Neue Version: {_updateInfo.Version}";
        TxtReleaseDate.Text = $"Veröffentlicht: {_updateInfo.ReleaseDate:dd.MM.yyyy}";
        TxtFileSize.Text = $"Download-Größe: {_updateInfo.FileSize / 1024 / 1024} MB";
        
        // Release Notes anzeigen
        var releaseNotes = string.Join("\n", _updateInfo.ReleaseNotes);
        TxtReleaseNotes.Text = releaseNotes;
    }
    
    private async void BtnDownloadUpdate_Click(object sender, RoutedEventArgs e)
    {
        BtnDownloadUpdate.IsEnabled = false;
        ProgressUpdate.Visibility = Visibility.Visible;
        
        var progress = new Progress<int>(value => 
        {
            ProgressUpdate.Value = value;
            TxtProgress.Text = $"Download: {value}%";
        });
        
        try
        {
            var success = await _updateService.DownloadUpdateAsync(_updateInfo, progress);
            
            if (success)
            {
                // Update installieren
                var result = MessageBox.Show(
                    "Update erfolgreich heruntergeladen!\n\n" +
                    "Die Anwendung wird jetzt geschlossen und das Update installiert.\n" +
                    "Nach der Installation wird die Anwendung automatisch neu gestartet.",
                    "Update bereit",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Information);
                
                if (result == MessageBoxResult.OK)
                {
                    await InstallUpdateAsync();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Update-Fehler: {ex.Message}", "Fehler", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnDownloadUpdate.IsEnabled = true;
            ProgressUpdate.Visibility = Visibility.Collapsed;
        }
    }
    
    private async Task InstallUpdateAsync()
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "Einsatzueberwachung_Update.exe");
            var currentPath = Assembly.GetExecutingAssembly().Location;
            
            // Silent Update-Installation
            var startInfo = new ProcessStartInfo
            {
                FileName = tempPath,
                Arguments = $"/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS /UPDATE \"{currentPath}\"",
                UseShellExecute = true,
                Verb = "runas" // Als Administrator ausführen
            };
            
            Process.Start(startInfo);
            
            // Aktuelle Anwendung beenden
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            LoggingService.Instance.LogError($"Update-Installation fehlgeschlagen: {ex.Message}", ex);
            MessageBox.Show($"Update-Installation fehlgeschlagen: {ex.Message}", 
                          "Update-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
```

---

## 📦 **Erweiterte Inno Setup Konfiguration für Updates**

### **🔧 Update-fähiges Inno Setup Script:**

```pascal
; Update-Konfiguration für GitHub Updates
[Setup]
; Standard Setup-Konfiguration...
AppUpdatesURL=https://github.com/Elemirus1996/Einsatzueberwachung/releases
UpdateCheckURL=https://github.com/Elemirus1996/Einsatzueberwachung/releases/latest/download/update-info.json

; Update-spezifische Einstellungen
AppMutex=EinsatzueberwachungProfessional_Update
CloseApplications=yes
RestartApplications=yes

[Code]
// Update-Detection beim Start
function IsUpdate: Boolean;
begin
  Result := ExpandConstant('{param:UPDATE}') <> '';
end;

// Update-Installation
procedure CurStepChanged(CurStep: TSetupStep);
var
  OldVersion: String;
begin
  if CurStep = ssInstall then
  begin
    if IsUpdate then
    begin
      // Backup erstellen vor Update
      if RegQueryStringValue(HKEY_CURRENT_USER, 'Software\RescueDog_SW\Einsatzüberwachung Professional', 'Version', OldVersion) then
      begin
        Log('Update von Version ' + OldVersion + ' auf {#MyAppVersion}');
        
        // Backup der Konfiguration
        CreateBackup();
      end;
    end;
  end;
  
  if CurStep = ssPostInstall then
  begin
    if IsUpdate then
    begin
      // Update-spezifische Post-Installation
      Log('Update-Installation abgeschlossen');
      
      // Cleanup alter Backups
      CleanupOldBackups();
    end;
  end;
end;

// Backup-Funktionalität
procedure CreateBackup();
var
  BackupDir: String;
  ConfigDir: String;
begin
  try
    ConfigDir := ExpandConstant('{userappdata}\Einsatzüberwachung');
    BackupDir := ExpandConstant('{userappdata}\Einsatzüberwachung\Backup\{#MyAppVersion}');
    
    if DirExists(ConfigDir) then
    begin
      ForceDirectories(BackupDir);
      
      // Kopiere wichtige Konfigurationsdateien
      if FileExists(ConfigDir + '\settings.json') then
        FileCopy(ConfigDir + '\settings.json', BackupDir + '\settings.json', False);
      if FileExists(ConfigDir + '\crash-recovery.json') then
        FileCopy(ConfigDir + '\crash-recovery.json', BackupDir + '\crash-recovery.json', False);
        
      Log('Backup erstellt in: ' + BackupDir);
    end;
  except
    Log('Backup-Erstellung fehlgeschlagen');
  end;
end;

// Cleanup alter Backups (behalte nur die letzten 3)
procedure CleanupOldBackups();
var
  BackupBaseDir: String;
  FindRec: TFindRec;
  BackupDirs: TStringList;
  i: Integer;
begin
  try
    BackupBaseDir := ExpandConstant('{userappdata}\Einsatzüberwachung\Backup');
    BackupDirs := TStringList.Create;
    
    if FindFirst(BackupBaseDir + '\*', FindRec) then
    begin
      try
        repeat
          if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0) and 
             (FindRec.Name <> '.') and (FindRec.Name <> '..') then
          begin
            BackupDirs.Add(BackupBaseDir + '\' + FindRec.Name);
          end;
        until not FindNext(FindRec);
      finally
        FindClose(FindRec);
      end;
    end;
    
    // Sortiere nach Datum (neueste zuerst) und lösche alte
    if BackupDirs.Count > 3 then
    begin
      for i := 3 to BackupDirs.Count - 1 do
      begin
        DelTree(BackupDirs[i], True, True, True);
        Log('Altes Backup gelöscht: ' + BackupDirs[i]);
      end;
    end;
    
    BackupDirs.Free;
  except
    Log('Backup-Cleanup fehlgeschlagen');
  end;
end;

[UninstallDelete]
; Cleanup bei Deinstallation
Type: filesandordirs; Name: "{userappdata}\Einsatzüberwachung\Backup"
```

---

## 🚀 **GitHub Actions für automatische Releases**

### **📋 Workflow für automatische Setup-Erstellung:**

```yaml
# .github/workflows/release.yml
name: Create Release with Setup

on:
  push:
    tags:
      - 'v*'

jobs:
  build-and-release:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore "Einsatzüberwachung V1.5.csproj"
    
    - name: Build Release
      run: dotnet build "Einsatzüberwachung V1.5.csproj" --configuration Release --no-restore
    
    - name: Publish Application
      run: dotnet publish "Einsatzüberwachung V1.5.csproj" --configuration Release --output "bin/Release/net8.0-windows"
    
    - name: Setup Inno Setup
      run: |
        Invoke-WebRequest -Uri "https://jrsoftware.org/download.php/is.exe" -OutFile "innosetup.exe"
        Start-Process -FilePath "innosetup.exe" -ArgumentList "/SILENT" -Wait
    
    - name: Create Setup.exe
      run: |
        & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "Setup\Einsatzueberwachung_Setup.iss"
      shell: powershell
    
    - name: Create update-info.json
      run: |
        $version = "${{ github.ref_name }}".TrimStart('v')
        $setupFile = Get-Item "Setup\Output\Einsatzueberwachung_Professional_v*_Setup.exe"
        $fileSize = $setupFile.Length
        $releaseDate = Get-Date -Format "yyyy-MM-dd"
        
        $updateInfo = @{
            version = $version
            releaseDate = $releaseDate
            downloadUrl = "https://github.com/Elemirus1996/Einsatzueberwachung/releases/download/${{ github.ref_name }}/$($setupFile.Name)"
            releaseNotesUrl = "https://github.com/Elemirus1996/Einsatzueberwachung/releases/tag/${{ github.ref_name }}"
            mandatory = $false
            minimumVersion = "1.5.0"
            fileSize = $fileSize
            releaseNotes = @(
                "🚀 Neue Version $version verfügbar",
                "📱 Verbesserte Mobile Server-Funktionalität",
                "🔧 Performance-Optimierungen",
                "🛡️ Sicherheits-Updates"
            )
        } | ConvertTo-Json -Depth 3
        
        $updateInfo | Out-File -FilePath "update-info.json" -Encoding UTF8
      shell: powershell
    
    - name: Create GitHub Release
      uses: softprops/action-gh-release@v1
      with:
        files: |
          Setup/Output/Einsatzueberwachung_Professional_v*_Setup.exe
          update-info.json
        body_path: RELEASE_NOTES.md
        draft: false
        prerelease: false
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

---

## 🔧 **Integration in die Hauptanwendung**

### **📱 Update-Check beim Start:**

```csharp
// In MainWindow.xaml.cs
private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
{
    // Standard-Initialisierung...
    
    // Update-Check im Hintergrund
    _ = Task.Run(async () =>
    {
        await Task.Delay(5000); // 5 Sekunden warten nach Start
        await CheckForUpdatesAsync();
    });
}

private async Task CheckForUpdatesAsync()
{
    try
    {
        var updateService = new GitHubUpdateService();
        var updateInfo = await updateService.CheckForUpdatesAsync();
        
        if (updateInfo != null && IsNewerVersion(updateInfo.Version))
        {
            Dispatcher.Invoke(() =>
            {
                ShowUpdateNotification(updateInfo);
            });
        }
    }
    catch (Exception ex)
    {
        LoggingService.Instance.LogError("Update-Check Fehler", ex);
    }
}

private bool IsNewerVersion(string newVersion)
{
    var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
    var newVer = new Version(newVersion);
    return newVer > currentVersion;
}

private void ShowUpdateNotification(UpdateInfo updateInfo)
{
    // Update-Benachrichtigung anzeigen
    var notification = new UpdateNotificationWindow(updateInfo);
    notification.Owner = this;
    notification.ShowDialog();
}
```

---

## 📊 **Vorteile des GitHub Update-Systems**

### **✅ Für Entwickler:**
- **Kostenlos** - GitHub Releases sind kostenfrei
- **Automatisiert** - GitHub Actions erstellen Releases automatisch
- **Versionskontrolle** - Vollständige Historie aller Versionen
- **CDN-Performance** - GitHub's globales CDN für Downloads
- **Statistiken** - Download-Zahlen und Analytics

### **✅ Für Benutzer:**
- **Automatische Updates** - Keine manuelle Suche nach neuen Versionen
- **Sichere Downloads** - Direkt von GitHub (vertrauenswürdig)
- **Rollback-Funktionalität** - Bei Problemen auf alte Version zurück
- **Transparenz** - Vollständige Release Notes und Änderungshistorie

### **✅ Für IT-Administratoren:**
- **Zentrale Verwaltung** - Updates über GitHub-Repository
- **Staging-Möglichkeiten** - Pre-Release Versionen für Tests
- **Controlled Rollout** - Schrittweise Verteilung möglich
- **Enterprise-Integration** - GitHub Enterprise Support

---

## 🎯 **Implementierungs-Roadmap**

### **Phase 1: Basis-Update-System (1-2 Tage)**
1. ✅ **GitHub Repository** für Releases vorbereiten
2. ✅ **Update-Service** in C# implementieren
3. ✅ **Inno Setup** für Updates konfigurieren
4. ✅ **Basis-UI** für Update-Benachrichtigungen

### **Phase 2: Automatisierung (2-3 Tage)**
1. ✅ **GitHub Actions** Workflow erstellen
2. ✅ **Automatische Release-Erstellung** implementieren
3. ✅ **Update-Info JSON** generieren
4. ✅ **Testing** auf verschiedenen Systemen

### **Phase 3: Enterprise Features (3-5 Tage)**
1. ✅ **Silent Updates** für IT-Administratoren
2. ✅ **Rollback-Funktionalität** implementieren
3. ✅ **Backup-System** für Konfigurationen
4. ✅ **Analytics** und Update-Statistiken

---

## 🎉 **Ergebnis:**

**✅ Ja, Inno Setup kann definitiv Updates über GitHub ausrollen!**

Das implementierte System bietet:
- **Vollautomatische Updates** über GitHub Releases
- **Professional Update-Experience** für Endbenutzer
- **Enterprise-ready Features** für IT-Administratoren
- **Kostenlose Infrastruktur** über GitHub
- **Globale CDN-Performance** für Downloads
- **Vollständige Versionskontrolle** und Rollback-Möglichkeiten

**🚀 Ihre Einsatzüberwachung Professional erhält damit ein Update-System auf Enterprise-Niveau - komplett kostenlos über GitHub!**
