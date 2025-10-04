# Build-Script für Einsatzüberwachung Professional v1.7 Setup
# Automatische Erstellung von Release und Setup.exe
# PowerShell Script für kompletten Build-Prozess

param(
    [switch]$CleanBuild = $false,
    [switch]$SkipTests = $false,
    [string]$Configuration = "Release",
    [string]$Version = "1.7.0"
)

Write-Host "Einsatzueberwachung Professional v1.7 - Build Script" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Continue"
$ProjectName = "Einsatzüberwachung.csproj"
$OutputDir = "bin\$Configuration\net8.0-windows"
$SetupDir = "Setup"
$SetupOutput = "$SetupDir\Output"

# Angepasster Inno Setup Pfad
$InnoSetupPath = "Y:\Inno Setup 6\ISCC.exe"

try {
    # 1. Umgebung vorbereiten
    Write-Host "SCHRITT 1: Umgebung vorbereiten..." -ForegroundColor Yellow
    
    if ($CleanBuild) {
        Write-Host "   Clean Build - Loesche vorherige Builds..." -ForegroundColor Blue
        if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
        if (Test-Path $SetupOutput) { Remove-Item $SetupOutput -Recurse -Force }
        
        dotnet clean $ProjectName --configuration $Configuration
        Write-Host "   Clean abgeschlossen" -ForegroundColor Green
    }
    
    # Erstelle Setup-Verzeichnisse
    if (!(Test-Path $SetupDir)) { New-Item -ItemType Directory -Path $SetupDir }
    if (!(Test-Path $SetupOutput)) { New-Item -ItemType Directory -Path $SetupOutput }
    if (!(Test-Path "$SetupDir\Resources")) { New-Item -ItemType Directory -Path "$SetupDir\Resources" }
    
    Write-Host "   Umgebung vorbereitet" -ForegroundColor Green
    Write-Host ""

    # 2. .NET Projekt bauen
    Write-Host "SCHRITT 2: .NET Projekt bauen..." -ForegroundColor Yellow
    
    # Restore NuGet packages
    Write-Host "   Restore NuGet Packages..." -ForegroundColor Blue
    dotnet restore $ProjectName
    
    # Build Projekt
    Write-Host "   Build Anwendung..." -ForegroundColor Blue
    dotnet build $ProjectName --configuration $Configuration --no-restore
    
    # Publish für Setup
    Write-Host "   Publish fuer Setup..." -ForegroundColor Blue
    dotnet publish $ProjectName --configuration $Configuration --no-restore --output $OutputDir
    
    Write-Host "   Build erfolgreich" -ForegroundColor Green
    Write-Host ""

    # 4. Setup-Dateien vorbereiten
    Write-Host "SCHRITT 4: Setup-Dateien vorbereiten..." -ForegroundColor Yellow
    
    # License und ReadMe im Setup-Verzeichnis erstellen
    Write-Host "   Erstelle Setup-Dokumentation..." -ForegroundColor Blue
    
    $licenseText = "MIT License`n`nCopyright (c) 2024-2025 RescueDog_SW`n`nPermission is hereby granted, free of charge, to any person obtaining a copy of this software..."
    
    $readmeText = @"
Einsatzueberwachung Professional v$Version
=========================================

WICHTIGE INFORMATIONEN VOR DER INSTALLATION:

System-Anforderungen:
- Windows 10 oder neuer (Windows 11 empfohlen)
- .NET 8 Runtime (wird automatisch installiert)
- Mindestens 500 MB freier Speicherplatz
- Administrator-Rechte fuer Mobile Server-Funktionalitaet
- Internetverbindung für automatische Updates

Was wird installiert:
- Einsatzueberwachung Professional v$Version Hauptanwendung
- .NET 8 Desktop Runtime (falls nicht vorhanden)
- GitHub Auto-Update-System
- Mobile Server Netzwerk-Konfiguration
- Firewall-Regeln fuer iPhone/Android-Zugriff
- PowerShell-Scripts fuer Troubleshooting
- Automatische GitHub Update-Pruefung
- Vollstaendige Dokumentation und Hilfe-Dateien

Neue Features v1.7:
- Automatische Update-Prüfung über GitHub Releases
- Ein-Klick Update-Installation
- Silent Update-Modus für IT-Administratoren
- Vollautomatische Setup-Konfiguration
- Erweiterte Troubleshooting-Tools

Erste Schritte nach Installation:
1. Anwendung starten (automatische Update-Prüfung)
2. Einsatzleiter und Ort eingeben
3. Teams hinzufügen und Timer starten
4. Optional: Mobile Verbindung für Smartphone-Zugriff

Viel Erfolg mit der Einsatzueberwachung Professional v$Version!
"@
    
    $installCompleteText = @"
Einsatzueberwachung Professional v$Version erfolgreich installiert!

Installation abgeschlossen:
- Hauptanwendung installiert und konfiguriert
- GitHub Auto-Update-System aktiviert
- Mobile Server automatisch eingerichtet
- Firewall-Regeln erstellt
- PowerShell-Scripts installiert
- Automatische Update-Benachrichtigungen aktiviert
- Dokumentation verfuegbar

Die Anwendung ist jetzt vollstaendig einsatzbereit!

Neue Features in v1.7:
- Automatische Updates aus GitHub
- Professional Setup mit Auto-Konfiguration
- Verbesserte Mobile Server Integration
- Erweiterte Troubleshooting-Tools

Starten Sie die Anwendung und genießen Sie die neuen Features!
"@
    
    # Erstelle Dateien im Setup-Verzeichnis
    Set-Content -Path "$SetupDir\License.txt" -Value $licenseText -Encoding UTF8
    Set-Content -Path "$SetupDir\ReadMe.txt" -Value $readmeText -Encoding UTF8
    Set-Content -Path "$SetupDir\Installation_Complete.txt" -Value $installCompleteText -Encoding UTF8
    
    Write-Host "   Setup-Dokumentation erstellt" -ForegroundColor Green

    # 5. Inno Setup ausführen
    Write-Host "SCHRITT 5: Setup.exe erstellen..." -ForegroundColor Yellow
    
    # Prüfe Inno Setup Installation
    if (Test-Path $InnoSetupPath) {
        Write-Host "   Gefunden: $InnoSetupPath" -ForegroundColor Blue
        Write-Host "   Kompiliere Setup..." -ForegroundColor Blue
        
        & $InnoSetupPath "Setup\Einsatzueberwachung_Setup.iss"
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   Setup.exe erfolgreich erstellt!" -ForegroundColor Green
        } else {
            Write-Host "   Setup-Kompilierung fehlgeschlagen (Exit Code: $LASTEXITCODE)" -ForegroundColor Red
            Write-Host "   Pruefen Sie die Inno Setup Ausgabe oben fuer Details" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   Inno Setup nicht gefunden unter: $InnoSetupPath" -ForegroundColor Red
        Write-Host "   Pruefen Sie den Installationspfad" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host ""

    # 6. Ergebnis und Statistiken
    Write-Host "SCHRITT 6: Build-Ergebnis..." -ForegroundColor Yellow
    
    if (Test-Path $OutputDir) {
        $buildSize = (Get-ChildItem $OutputDir -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
        Write-Host "   Build-Groesse: $([math]::Round($buildSize, 2)) MB" -ForegroundColor Blue
    }
    
    $setupFiles = Get-ChildItem "$SetupOutput\*.exe" -ErrorAction SilentlyContinue
    if ($setupFiles) {
        Write-Host ""
        Write-Host "SETUP ERFOLGREICH ERSTELLT!" -ForegroundColor Green
        Write-Host "============================" -ForegroundColor Green
        foreach ($setupFile in $setupFiles) {
            $setupSize = $setupFile.Length / 1MB
            Write-Host "   Setup-Groesse: $([math]::Round($setupSize, 2)) MB" -ForegroundColor Blue
            Write-Host "   Setup-Datei: $($setupFile.Name)" -ForegroundColor Blue
            Write-Host "   Vollstaendiger Pfad: $($setupFile.FullName)" -ForegroundColor Blue
        }
    } else {
        Write-Host "   WARNUNG: Keine Setup-Dateien gefunden in $SetupOutput" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "BUILD ABGESCHLOSSEN!" -ForegroundColor Green
    Write-Host "=====================" -ForegroundColor Green
    Write-Host ""
    Write-Host "VERFUEGBARE DATEIEN:" -ForegroundColor Cyan
    Write-Host "- Anwendung: $OutputDir\" -ForegroundColor White
    Write-Host "- Setup.exe: $SetupOutput\" -ForegroundColor White
    Write-Host "- Dokumentation: .\Documentation\" -ForegroundColor White
    Write-Host "- PowerShell-Scripts: .\Scripts\" -ForegroundColor White
    Write-Host ""
    Write-Host "NAECHSTE SCHRITTE FUER GITHUB RELEASE:" -ForegroundColor Cyan
    Write-Host "1. git add ." -ForegroundColor White
    Write-Host "2. git commit -m 'Release v$Version - GitHub Auto-Updates & Professional Setup'" -ForegroundColor White
    Write-Host "3. git tag v$Version" -ForegroundColor White
    Write-Host "4. git push origin V1.7" -ForegroundColor White
    Write-Host "5. git push --tags" -ForegroundColor White
    Write-Host "6. GitHub Actions erstellt automatisch Release!" -ForegroundColor White
    Write-Host ""

} catch {
    Write-Host ""
    Write-Host "BUILD FEHLGESCHLAGEN!" -ForegroundColor Red
    Write-Host "Fehler: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    exit 1
}

# Optional: Setup testen
if (Test-Path "$SetupOutput\*.exe") {
    $setupFiles = Get-ChildItem "$SetupOutput\*.exe"
    $response = Read-Host "Moechten Sie das Setup jetzt testen? (j/n)"
    if ($response -eq 'j' -or $response -eq 'J') {
        Write-Host "Starte Setup-Test..." -ForegroundColor Yellow
        Start-Process $setupFiles[0].FullName
    }
    
    $response2 = Read-Host "Setup-Ordner oeffnen? (j/n)"
    if ($response2 -eq 'j' -or $response2 -eq 'J') {
        Start-Process "explorer.exe" -ArgumentList $SetupOutput
    }
}

Write-Host "Build-Script abgeschlossen!" -ForegroundColor Green
