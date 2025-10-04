# 🚀 GitHub Release v1.7.0 - Automatisches Release-Script
# PowerShell Script für vollständigen GitHub Release-Prozess

param(
    [switch]$SkipBuild = $false,
    [switch]$DryRun = $false
)

Write-Host "🚀 EINSATZÜBERWACHUNG PROFESSIONAL v1.7.0 - GITHUB RELEASE" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Continue"

try {
    # 1. Git Status prüfen
    Write-Host "🔍 Prüfe Git Repository..." -ForegroundColor Yellow
    
    $gitStatus = git status --porcelain 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Git Repository nicht initialisiert oder nicht verfügbar" -ForegroundColor Red
        Write-Host "💡 Führen Sie folgende Befehle manuell aus:" -ForegroundColor Yellow
        Write-Host "   git init" -ForegroundColor White
        Write-Host "   git remote add origin https://github.com/Elemirus1996/Einsatzueberwachung.git" -ForegroundColor White
        Write-Host "   git branch -M master" -ForegroundColor White
        exit 1
    }
    
    Write-Host "✅ Git Repository verfügbar" -ForegroundColor Green
    
    # 2. Build und Setup prüfen
    if (-not $SkipBuild) {
        Write-Host "🔨 Prüfe Build und Setup..." -ForegroundColor Yellow
        
        if (-not (Test-Path "bin\Release\net8.0-windows\Einsatzueberwachung.exe")) {
            Write-Host "⚠️  Build nicht gefunden - erstelle Build..." -ForegroundColor Yellow
            dotnet build "Einsatzüberwachung.csproj" --configuration Release
        }
        
        if (-not (Test-Path "Setup\Output\Einsatzueberwachung_Professional_v1.7.0_Setup.exe")) {
            Write-Host "⚠️  Setup.exe nicht gefunden - erstelle Setup..." -ForegroundColor Yellow
            & "Y:\Inno Setup 6\ISCC.exe" "Setup\Einsatzueberwachung_Setup.iss"
        }
        
        Write-Host "✅ Build und Setup verfügbar" -ForegroundColor Green
    }
    
    # 3. Setup-Informationen anzeigen
    if (Test-Path "Setup\Output\Einsatzueberwachung_Professional_v1.7.0_Setup.exe") {
        $setupFile = Get-Item "Setup\Output\Einsatzueberwachung_Professional_v1.7.0_Setup.exe"
        $setupSize = [math]::Round($setupFile.Length / 1MB, 2)
        
        Write-Host ""
        Write-Host "📦 SETUP-INFORMATIONEN:" -ForegroundColor Cyan
        Write-Host "   Datei: $($setupFile.Name)" -ForegroundColor White
        Write-Host "   Größe: $setupSize MB" -ForegroundColor White
        Write-Host "   Pfad: $($setupFile.FullName)" -ForegroundColor Gray
        Write-Host ""
    }
    
    # 4. Git Commit vorbereiten
    Write-Host "📝 Bereite Git Commit vor..." -ForegroundColor Yellow
    
    if ($DryRun) {
        Write-Host "🔍 DRY RUN - Zeige was gemacht würde:" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "git add ." -ForegroundColor Gray
        Write-Host "git commit -m '🚀 Release v1.7.0 - GitHub Auto-Updates Edition...'" -ForegroundColor Gray
        Write-Host "git tag -a v1.7.0 -m '🚀 Einsatzüberwachung Professional v1.7.0...'" -ForegroundColor Gray
        Write-Host "git push origin master" -ForegroundColor Gray
        Write-Host "git push --tags" -ForegroundColor Gray
        Write-Host ""
    } else {
        # Alle Dateien hinzufügen
        Write-Host "   📁 Füge alle Dateien hinzu..." -ForegroundColor Blue
        git add .
        
        # Commit erstellen
        Write-Host "   💾 Erstelle Commit..." -ForegroundColor Blue
        $commitMessage = @"
🚀 Release v1.7.0 - GitHub Auto-Updates & Professional Setup Edition

✨ Neue Features v1.7:
- 🔄 Vollständiges GitHub Auto-Update-System
- 🛠️ Professional Setup.exe mit Auto-Konfiguration
- 📱 Enhanced Mobile Server Setup
- 🔧 Erweiterte Troubleshooting-Tools
- 📊 Update-Benachrichtigungen mit Changelog

🔧 Technische Verbesserungen v1.7:
- GitHub Releases API Integration
- Automatische Update-Prüfung beim Start
- Ein-Klick Update-Installation
- Silent Update-Modus für Enterprise
- Intelligente Version-Verwaltung

📱 Mobile Integration (v1.6):
- Progressive Web App (PWA) Support
- Touch-optimierte Bedienung
- Live-Updates alle 10 Sekunden
- Automatische IP-Erkennung

📊 Analytics & Statistics (v1.6):
- Real-time Dashboard
- Team-Rankings
- Performance-Metriken
- Intelligente Empfehlungen

🛡️ Enterprise Features:
- Silent Installation für IT-Administratoren
- Zentrale Update-Verwaltung über GitHub
- Professional Logging und Diagnostics
- Automatische Netzwerk-Konfiguration
"@
        
        git commit -m $commitMessage
        
        # Tag erstellen
        Write-Host "   🏷️ Erstelle Git Tag v1.7.0..." -ForegroundColor Blue
        $tagMessage = @"
🚀 Einsatzüberwachung Professional v1.7.0

GitHub Auto-Updates & Professional Setup Edition

Hauptfeatures v1.7:
• GitHub Auto-Update-System für nahtlose Updates
• Professional Setup.exe mit vollautomatischer Konfiguration
• Enhanced Mobile Server Integration
• Intelligente Update-Benachrichtigungen
• Silent Update-Modus für IT-Administratoren

Technische Highlights:
• GitHub Releases API Integration
• Automatische Update-Prüfung im Hintergrund
• Ein-Klick Installation von Updates
• Version-Management mit Skip-Funktion
• Release Notes direkt im Update-Dialog

Diese Version bringt Enterprise-ready Update-Management!
"@
        
        git tag -a v1.7.0 -m $tagMessage
        
        # Zu GitHub pushen
        Write-Host "   🌐 Pushe zu GitHub..." -ForegroundColor Blue
        git push origin master
        git push --tags
        
        Write-Host "✅ Git Commit und Tag erfolgreich erstellt" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "🎉 GITHUB RELEASE v1.7.0 VORBEREITET!" -ForegroundColor Green
    Write-Host "=======================================" -ForegroundColor Green
    Write-Host ""
    
    if (-not $DryRun) {
        Write-Host "📋 NÄCHSTE SCHRITTE:" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "1. 🔍 GitHub Actions prüfen:" -ForegroundColor Yellow
        Write-Host "   https://github.com/Elemirus1996/Einsatzueberwachung/actions" -ForegroundColor Blue
        Write-Host ""
        Write-Host "2. 📦 Release wird automatisch erstellt unter:" -ForegroundColor Yellow
        Write-Host "   https://github.com/Elemirus1996/Einsatzueberwachung/releases" -ForegroundColor Blue
        Write-Host ""
        Write-Host "3. ⏰ Falls GitHub Actions NICHT automatisch läuft:" -ForegroundColor Yellow
        Write-Host "   - Siehe GITHUB_RELEASE_v1.7.0_ANLEITUNG.md" -ForegroundColor White
        Write-Host "   - Manuelles Release über GitHub Web-Interface" -ForegroundColor White
        Write-Host ""
        Write-Host "4. 📱 Nach Release-Veröffentlichung testen:" -ForegroundColor Yellow
        Write-Host "   - Setup.exe Download" -ForegroundColor White
        Write-Host "   - Automatische Update-Benachrichtigung" -ForegroundColor White
        Write-Host "   - Ein-Klick Update-Installation" -ForegroundColor White
        Write-Host "   - Mobile Server mit QR-Code" -ForegroundColor White
        Write-Host ""
        
        $openGitHub = Read-Host "Möchten Sie GitHub Actions jetzt öffnen? (j/n)"
        if ($openGitHub -eq 'j' -or $openGitHub -eq 'J') {
            Start-Process "https://github.com/Elemirus1996/Einsatzueberwachung/actions"
        }
        
        $openReleases = Read-Host "Möchten Sie GitHub Releases öffnen? (j/n)"
        if ($openReleases -eq 'j' -or $openReleases -eq 'J') {
            Start-Process "https://github.com/Elemirus1996/Einsatzueberwachung/releases"
        }
    }
    
} catch {
    Write-Host ""
    Write-Host "❌ FEHLER BEIM RELEASE-PROZESS!" -ForegroundColor Red
    Write-Host "Fehler: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "💡 LÖSUNGSVORSCHLÄGE:" -ForegroundColor Yellow
    Write-Host "1. Git konfigurieren:" -ForegroundColor White
    Write-Host "   git config user.name 'Ihr Name'" -ForegroundColor Gray
    Write-Host "   git config user.email 'ihre.email@example.com'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Remote Repository prüfen:" -ForegroundColor White
    Write-Host "   git remote -v" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. Manuelles Release über:" -ForegroundColor White
    Write-Host "   https://github.com/Elemirus1996/Einsatzueberwachung/releases" -ForegroundColor Blue
    Write-Host ""
}

Write-Host "✅ Release-Script abgeschlossen!" -ForegroundColor Green
