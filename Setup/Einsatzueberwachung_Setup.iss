; Einsatzüberwachung Professional v1.7 - Inno Setup Script mit GitHub Updates
; Professionelle Installation mit automatischer Mobile Server Konfiguration und Update-Support
; Erstellt: 2024 - RescueDog_SW

#define MyAppName "Einsatzüberwachung Professional"
#define MyAppVersion "1.7.0"
#define MyAppPublisher "RescueDog_SW"
#define MyAppURL "https://github.com/Elemirus1996/Einsatzueberwachung"
#define MyAppExeName "Einsatzueberwachung.exe"
#define MyAppDescription "Professionelle Einsatzüberwachung für Rettungshunde-Teams"
#define GitHubRepo "Elemirus1996/Einsatzueberwachung"

[Setup]
; Basis-Konfiguration
AppId={{A1B2C3D4-E5F6-7890-ABCD-123456789012}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=License.txt
InfoBeforeFile=ReadMe.txt
InfoAfterFile=Installation_Complete.txt
OutputDir=Setup\Output
OutputBaseFilename=Einsatzueberwachung_Professional_v{#MyAppVersion}_Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
MinVersion=0,6.1sp1

; Wichtig: Admin-Rechte für Mobile Server Konfiguration
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

; Update-spezifische Konfiguration
AppMutex=EinsatzueberwachungProfessional_UpdateMutex
CloseApplications=yes
RestartApplications=yes

; Moderne UI-Konfiguration
WizardImageBackColor=$FFFFFF
ShowLanguageDialog=no

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checked
Name: "autostart"; Description: "Mobile Server automatisch konfigurieren (empfohlen)"; GroupDescription: "Mobile Server Konfiguration"; Flags: checked
Name: "firewall"; Description: "Firewall-Regeln automatisch erstellen"; GroupDescription: "Netzwerk-Konfiguration"; Flags: checked
Name: "urlreservation"; Description: "URL-Reservierungen für Mobile Server"; GroupDescription: "Netzwerk-Konfiguration"; Flags: checked
Name: "autoupdates"; Description: "Automatische Update-Prüfung aktivieren (empfohlen)"; GroupDescription: "Update-Konfiguration"; Flags: checked

[Files]
; Hauptanwendung
Source: "bin\Release\net8.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; PowerShell-Scripts
Source: "Fix-MobileServer.ps1"; DestDir: "{app}\Scripts"; Flags: ignoreversion
Source: "Setup-MobileNetwork.ps1"; DestDir: "{app}\Scripts"; Flags: ignoreversion
; Dokumentation
Source: "SERVER_START_LOESUNGSANLEITUNG_FINAL.md"; DestDir: "{app}\Documentation"; Flags: ignoreversion
Source: "SETUP_INSTALLATION_GUIDE.md"; DestDir: "{app}\Documentation"; Flags: ignoreversion
Source: "HTTP_400_TROUBLESHOOTING.md"; DestDir: "{app}\Documentation"; Flags: ignoreversion
Source: "MOBILE_SETUP_GUIDE.md"; DestDir: "{app}\Documentation"; Flags: ignoreversion
Source: "GITHUB_UPDATE_SYSTEM.md"; DestDir: "{app}\Documentation"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: ""; IconFilename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Comment: "{#MyAppDescription}"; Flags: runasoriginaluser
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Comment: "{#MyAppDescription}"; Flags: runasoriginaluser

; Admin-Verknüpfung für Mobile Server
Name: "{autoprograms}\{#MyAppName} (Administrator)"; Filename: "{app}\{#MyAppExeName}"; Parameters: ""; IconFilename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Comment: "{#MyAppDescription} - Als Administrator für Mobile Server"; Flags: runascurrentuser

; Troubleshooting Tools
Name: "{autoprograms}\{#MyAppName}\Mobile Server Reparatur"; Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\Scripts\Fix-MobileServer.ps1"""; WorkingDir: "{app}\Scripts"; Comment: "Mobile Server Probleme automatisch reparieren"
Name: "{autoprograms}\{#MyAppName}\System Diagnose"; Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\Scripts\Fix-MobileServer.ps1"" -DiagnoseOnly"; WorkingDir: "{app}\Scripts"; Comment: "System-Diagnose für Mobile Server"
Name: "{autoprograms}\{#MyAppName}\Nach Updates suchen"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--check-updates"; WorkingDir: "{app}"; Comment: "Manuell nach Updates suchen"
Name: "{autoprograms}\{#MyAppName}\Dokumentation"; Filename: "{app}\Documentation"; Comment: "Hilfe und Troubleshooting Dokumentation"

[Run]
; Mobile Server Konfiguration
Filename: "netsh"; Parameters: "http add urlacl url=http://+:8080/ user=Everyone"; StatusMsg: "Konfiguriere Mobile Server URL-Reservierung..."; Flags: runhidden; Tasks: urlreservation
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""Einsatzueberwachung_Mobile"" dir=in action=allow protocol=TCP localport=8080"; StatusMsg: "Konfiguriere Firewall-Regel für Mobile Server..."; Flags: runhidden; Tasks: firewall

; PowerShell-Ausführungsrichtlinie setzen
Filename: "powershell"; Parameters: "-Command ""Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force"""; StatusMsg: "Konfiguriere PowerShell für Troubleshooting-Scripts..."; Flags: runhidden

; Optional: Anwendung nach Installation starten
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser

[UninstallRun]
; Cleanup bei Deinstallation
Filename: "netsh"; Parameters: "http delete urlacl url=http://+:8080/"; Flags: runhidden; RunOnceId: "RemoveUrlReservation"
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""Einsatzueberwachung_Mobile"""; Flags: runhidden; RunOnceId: "RemoveFirewallRule"

[Registry]
; Auto-Update Konfiguration
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; ValueType: dword; ValueName: "MobileServerConfigured"; ValueData: "1"; Tasks: autostart; Flags: uninsdeletekey

; GitHub Update-System Konfiguration
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; ValueType: string; ValueName: "UpdateCheckURL"; ValueData: "https://api.github.com/repos/{#GitHubRepo}/releases/latest"; Tasks: autoupdates; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; ValueType: string; ValueName: "GitHubRepo"; ValueData: "{#GitHubRepo}"; Tasks: autoupdates; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; ValueType: dword; ValueName: "AutoUpdateEnabled"; ValueData: "1"; Tasks: autoupdates; Flags: uninsdeletekey

[Code]
// Update-Detection beim Start
function IsUpdate: Boolean;
begin
  Result := ExpandConstant('{param:UPDATE}') <> '';
end;

// Custom Welcome Message
procedure InitializeWizard;
begin
  WizardForm.WelcomeLabel2.Caption := 
    'Diese Installation konfiguriert automatisch:' + #13#10 + #13#10 +
    '• Mobile Server Netzwerk-Konfiguration' + #13#10 +
    '• Firewall-Regeln für iPhone-Zugriff' + #13#10 +
    '• PowerShell-Scripts für Troubleshooting' + #13#10 +
    '• Automatische GitHub Update-Prüfung' + #13#10 +
    '• Automatische System-Diagnose' + #13#10 + #13#10 +
    'Administrator-Rechte sind für die vollständige ' + #13#10 +
    'Mobile Server-Funktionalität erforderlich.';
end;
