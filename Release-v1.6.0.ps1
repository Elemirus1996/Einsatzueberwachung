# 🚀 GitHub Release v1.6.0 - Automatisches Release-Script
# PowerShell Script für vollständigen GitHub Release-Prozess

param(
    [switch]$SkipBuild = $false,
    [switch]$DryRun = $false
)

Write-Host "🚀 EINSATZÜBERWACHUNG PROFESSIONAL v1.6.0 - GITHUB RELEASE" -ForegroundColor Cyan
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
        
        if (-not (Test-Path "Setup\Output\Einsatzueberwachung_Professional_v1.6.0_Setup.exe")) {
            Write-Host "⚠️  Setup.exe nicht gefunden - erstelle Setup..." -ForegroundColor Yellow
            & "Y:\Inno Setup 6\ISCC.exe" "Setup\Einsatzueberwachung_Setup_Simple.iss"
        }
        
        Write-Host "✅ Build und Setup verfügbar" -ForegroundColor Green
    }
    
    # 3. Setup-Informationen anzeigen
    if (Test-Path "Setup\Output\Einsatzueberwachung_Professional_v1.6.0_Setup.exe") {
        $setupFile = Get-Item "Setup\Output\Einsatzueberwachung_Professional_v1.6.0_Setup.exe"
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
        Write-Host "git commit -m '🚀 Release v1.6.0 - Professional Edition...'" -ForegroundColor Gray
        Write-Host "git tag -a v1.6.0 -m '🚀 Einsatzüberwachung Professional v1.6.0...'" -ForegroundColor Gray
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
🚀 Release v1.6.0 - Professional Edition mit GitHub Auto-Updates

✨ Neue Features:
- 🔄 Vollständiges GitHub Auto-Update-System
- 📱 Mobile Server mit QR-Code-Zugriff  
- 🛠️ Professional Setup.exe mit Auto-Konfiguration
- 📊 Erweiterte Statistics und Analytics
- 🎨 Dark/Light Mode und modernisierte UI

🔧 Technische Verbesserungen:
- Enterprise-ready Deployment-Optionen
- Automatische Firewall und Netzwerk-Konfiguration
- Umfassende Troubleshooting-Tools
- Performance-Optimierungen

📱 Mobile Integration:
- Progressive Web App (PWA) Support
- Touch-optimierte Bedienung
- Live-Updates alle 10 Sekunden
- Automatische IP-Erkennung

🛡️ Enterprise Features:
- Silent Installation für IT-Administratoren
- Zentrale Update-Verwaltung über GitHub
- Professional Logging und Diagnostics
- Group Policy Integration möglich
"@
        
        git commit -m $commitMessage
        
        # Tag erstellen
        Write-Host "   🏷️ Erstelle Git Tag v1.6.0..." -ForegroundColor Blue
        $tagMessage = @"
🚀 Einsatzüberwachung Professional v1.6.0

Major Release mit GitHub Auto-Updates und erweiterter Mobile Integration

Hauptfeatures:
• GitHub Auto-Update-System für nahtlose Updates
• Mobile Server mit QR-Code-Zugriff für iPhone/Android
• Professional Setup.exe mit automatischer Konfiguration
• Erweiterte Statistics und Analytics-Dashboard
• Dark/Light Mode und modernisierte Benutzeroberfläche
• Enterprise-ready Features für professionellen Einsatz

Diese Version bringt die Einsatzüberwachung auf professionelles Niveau!
"@
        
        git tag -a v1.6.0 -m $tagMessage
        
        # Zu GitHub pushen
        Write-Host "   🌐 Pushe zu GitHub..." -ForegroundColor Blue
        git push origin master
        git push --tags
        
        Write-Host "✅ Git Commit und Tag erfolgreich erstellt" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "🎉 GITHUB RELEASE v1.6.0 VORBEREITET!" -ForegroundColor Green
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
        Write-Host "   - Siehe GITHUB_RELEASE_v1.6.0_ANLEITUNG.md" -ForegroundColor White
        Write-Host "   - Manuelles Release über GitHub Web-Interface" -ForegroundColor White
        Write-Host ""
        Write-Host "4. 📱 Nach Release-Veröffentlichung testen:" -ForegroundColor Yellow
        Write-Host "   - Setup.exe Download" -ForegroundColor White
        Write-Host "   - Automatische Update-Benachrichtigung" -ForegroundColor White
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
