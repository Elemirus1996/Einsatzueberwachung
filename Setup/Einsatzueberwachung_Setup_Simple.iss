; Einsatzüberwachung Professional v1.7 - Inno Setup Script
; Einfache Installation für Windows

#define MyAppName "Einsatzüberwachung Professional"
#define MyAppVersion "1.7.0"
#define MyAppPublisher "RescueDog_SW"
#define MyAppURL "https://github.com/Elemirus1996/Einsatzueberwachung"
#define MyAppExeName "Einsatzueberwachung.exe"

[Setup]
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
LicenseFile=..\License.txt
InfoBeforeFile=..\ReadMe.txt
InfoAfterFile=..\Installation_Complete.txt
OutputDir=Output
OutputBaseFilename=Einsatzueberwachung_Professional_v{#MyAppVersion}_Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
MinVersion=0,6.1sp1
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
ShowLanguageDialog=no

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "Desktop-Verknüpfung erstellen"
Name: "mobileconfiguration"; Description: "Mobile Server automatisch konfigurieren (empfohlen)"

[Files]
Source: "..\bin\Release\net8.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Fix-MobileServer.ps1"; DestDir: "{app}\Scripts"; Flags: ignoreversion
Source: "..\Setup-MobileNetwork.ps1"; DestDir: "{app}\Scripts"; Flags: ignoreversion
Source: "..\SERVER_START_LOESUNGSANLEITUNG_FINAL.md"; DestDir: "{app}\Documentation"; Flags: ignoreversion
Source: "..\MOBILE_SETUP_GUIDE.md"; DestDir: "{app}\Documentation"; Flags: ignoreversion
Source: "..\GITHUB_UPDATE_SYSTEM.md"; DestDir: "{app}\Documentation"; Flags: ignoreversion
Source: "..\HTTP_400_TROUBLESHOOTING.md"; DestDir: "{app}\Documentation"; Flags: ignoreversion
Source: "..\NON_ADMIN_COMPLETE_GUIDE.md"; DestDir: "{app}\Documentation"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; WorkingDir: "{app}"
Name: "{autoprograms}\{#MyAppName} (Administrator)"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autoprograms}\{#MyAppName}\Mobile Server Reparatur"; Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\Scripts\Fix-MobileServer.ps1"""; WorkingDir: "{app}\Scripts"
Name: "{autoprograms}\{#MyAppName}\System Diagnose"; Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\Scripts\Fix-MobileServer.ps1"" -DiagnoseOnly"; WorkingDir: "{app}\Scripts"
Name: "{autoprograms}\{#MyAppName}\Dokumentation"; Filename: "{app}\Documentation"

[Run]
Filename: "netsh"; Parameters: "http add urlacl url=http://+:8080/ user=Everyone"; StatusMsg: "Konfiguriere Mobile Server..."; Flags: runhidden; Tasks: mobileconfiguration
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""Einsatzueberwachung_Mobile"" dir=in action=allow protocol=TCP localport=8080"; StatusMsg: "Konfiguriere Firewall..."; Flags: runhidden; Tasks: mobileconfiguration
Filename: "powershell"; Parameters: "-Command ""Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force"""; StatusMsg: "Konfiguriere PowerShell..."; Flags: runhidden
Filename: "{app}\{#MyAppExeName}"; Description: "Einsatzüberwachung Professional starten"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "netsh"; Parameters: "http delete urlacl url=http://+:8080/"; Flags: runhidden
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""Einsatzueberwachung_Mobile"""; Flags: runhidden

[Registry]
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; ValueType: dword; ValueName: "MobileServerConfigured"; ValueData: "1"; Tasks: mobileconfiguration; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; ValueType: string; ValueName: "GitHubRepo"; ValueData: "Elemirus1996/Einsatzueberwachung"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; ValueType: dword; ValueName: "AutoUpdateEnabled"; ValueData: "1"; Flags: uninsdeletekey
