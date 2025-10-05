; Einsatzüberwachung Professional v1.7 - Inno Setup Script mit GitHub Updates
; Professionelle Installation mit automatischer Mobile Server Konfiguration und Update-Support
; Erstellt: 2024 - RescueDog_SW

#define MyAppName "Einsatzüberwachung Professional"
#define MyAppVersion "1.7.1"
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
OutputDir=Output
OutputBaseFilename=Einsatzueberwachung_Professional_v{#MyAppVersion}_Setup
Compression=lzma
SolidCompression=yes
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
ShowLanguageDialog=no

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "firewall"; Description: "Firewall-Regeln automatisch erstellen"; GroupDescription: "Netzwerk-Konfiguration"
Name: "urlreservation"; Description: "URL-Reservierungen für Mobile Server"; GroupDescription: "Netzwerk-Konfiguration"
Name: "autoupdates"; Description: "Automatische Update-Prüfung aktivieren (empfohlen)"; GroupDescription: "Update-Konfiguration"

[Files]
; Hauptanwendung - Pfad relativ zum Setup-Verzeichnis (ein Verzeichnis höher)
Source: "..\bin\Release\net8.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Comment: "{#MyAppDescription}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; WorkingDir: "{app}"; Comment: "{#MyAppDescription}"

[Run]
; Mobile Server Konfiguration
Filename: "netsh"; Parameters: "http add urlacl url=http://+:8080/ user=Everyone"; StatusMsg: "Konfiguriere Mobile Server URL-Reservierung..."; Flags: runhidden; Tasks: urlreservation
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""Einsatzueberwachung_Mobile"" dir=in action=allow protocol=TCP localport=8080"; StatusMsg: "Konfiguriere Firewall-Regel für Mobile Server..."; Flags: runhidden; Tasks: firewall

; PowerShell-Ausführungsrichtlinie setzen
Filename: "powershell"; Parameters: "-Command ""Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force"""; StatusMsg: "Konfiguriere PowerShell..."; Flags: runhidden

; Optional: Anwendung nach Installation starten
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Cleanup bei Deinstallation
Filename: "netsh"; Parameters: "http delete urlacl url=http://+:8080/"; Flags: runhidden; RunOnceId: "RemoveUrlReservation"
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""Einsatzueberwachung_Mobile"""; Flags: runhidden; RunOnceId: "RemoveFirewallRule"

[Registry]
; Auto-Update Konfiguration
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"; Flags: uninsdeletekey

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
    '• Automatische GitHub Update-Prüfung' + #13#10 + #13#10 +
    'Administrator-Rechte sind für die vollständige ' + #13#10 +
    'Mobile Server-Funktionalität erforderlich.';
end;
