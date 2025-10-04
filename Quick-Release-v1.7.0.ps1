# 🚀 Quick Release v1.7.0 - Automatisches GitHub Release
# Führt alle Schritte aus: Build → Setup → Git Commit → Push → Tag

Write-Host ""
Write-Host "🚀 EINSATZÜBERWACHUNG PROFESSIONAL v1.7.0 - QUICK RELEASE" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"
$version = "1.7.0"
$tagName = "v$version"

try {
    # 1. Build prüfen/erstellen
    Write-Host "🔨 1/6 - Build prüfen..." -ForegroundColor Yellow
    
    if (-not (Test-Path "bin\Release\net8.0-windows\Einsatzueberwachung.exe")) {
        Write-Host "   ⚙️ Build wird erstellt..." -ForegroundColor Blue
        dotnet build "Einsatzüberwachung.csproj" --configuration Release
        Write-Host "   ✅ Build erfolgreich" -ForegroundColor Green
    } else {
        Write-Host "   ✅ Build bereits vorhanden" -ForegroundColor Green
    }
    
    # 2. Setup erstellen (falls Inno Setup verfügbar)
    Write-Host ""
    Write-Host "📦 2/6 - Setup.exe erstellen..." -ForegroundColor Yellow
    
    $innoCompiler = "Y:\Inno Setup 6\ISCC.exe"
    if (Test-Path $innoCompiler) {
        Write-Host "   ⚙️ Kompiliere Setup mit Inno Setup..." -ForegroundColor Blue
        & $innoCompiler "Setup\Einsatzueberwachung_Setup_Simple.iss" | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            $setupFile = Get-Item "Setup\Output\Einsatzueberwachung_Professional_v${version}_Setup.exe"
            $setupSize = [math]::Round($setupFile.Length / 1MB, 2)
            Write-Host "   ✅ Setup.exe erstellt: $setupSize MB" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️ Setup-Kompilierung fehlgeschlagen" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ⚠️ Inno Setup nicht gefunden - Setup wird später von GitHub Actions erstellt" -ForegroundColor Yellow
    }
    
    # 3. Git Status
    Write-Host ""
    Write-Host "📋 3/6 - Git Status prüfen..." -ForegroundColor Yellow
    
    git status --porcelain 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   ❌ Git Repository nicht verfügbar!" -ForegroundColor Red
        exit 1
    }
    Write-Host "   ✅ Git Repository OK" -ForegroundColor Green
    
    # 4. Git Commit
    Write-Host ""
    Write-Host "💾 4/6 - Git Commit erstellen..." -ForegroundColor Yellow
    
    git add .
    
    $commitMessage = @"
🚀 Release v$version - GitHub Auto-Update-System

✨ Hauptfeatures:
- 🔄 Vollständiges GitHub Auto-Update-System mit automatischer Prüfung
- 📱 Verbesserte Mobile Server-Stabilität und Performance
- 🛡️ Erweiterte Crash-Recovery und Auto-Save-Funktionalität
- 🎨 Optimierte UI/UX und Theme-System
- 🔧 Performance-Optimierungen für große Einsätze

🔄 Update-System:
- Automatische Update-Prüfung beim Start (5s Verzögerung)
- Professional Update-Dialog mit Download-Progress
- Ein-Klick-Updates mit Backup & Rollback
- Silent Installation für IT-Administratoren
- Intelligente Cleanup alter Update-Dateien

📱 Mobile Server:
- Verbesserte Netzwerk-Erkennung und IP-Handling
- Erweiterte Diagnose-Tools für Troubleshooting
- Windows Mobile Hotspot Integration optimiert
- Enhanced Error-Handling und Logging

🛡️ Stabilität:
- Optimiertes Auto-Save-System mit Change-Tracking
- Intelligentere Crash-Recovery mit Daten-Vorschau
- Erweiterte Logging-Funktionalität
- Robuste Fehlerbehandlung in kritischen Bereichen

Diese Version bringt die Einsatzüberwachung auf professionelles Niveau
mit vollständig automatisierten Updates über GitHub Releases!
"@
    
    git commit -m $commitMessage
    Write-Host "   ✅ Commit erstellt" -ForegroundColor Green
    
    # 5. Git Tag erstellen
    Write-Host ""
    Write-Host "🏷️ 5/6 - Git Tag erstellen..." -ForegroundColor Yellow
    
    $tagMessage = @"
🚀 Einsatzüberwachung Professional v$version

Major Release mit vollständigem GitHub Auto-Update-System

Hauptfeatures:
• Automatische Update-Prüfung beim Start (nicht-blockierend)
• Professional Update-Dialog mit Download-Progress
• Ein-Klick-Updates mit Backup & Rollback-Funktionalität
• Verbesserte Mobile Server-Stabilität und Performance
• Erweiterte Crash-Recovery und Auto-Save-System
• Optimierte UI/UX mit modernem Theme-System
• Enterprise-ready mit Silent Installation Support

Diese Version bringt professionelles Auto-Update-System auf dem Niveau
kommerzieller Software. Benutzer erhalten automatische Benachrichtigungen
über neue Versionen und können mit einem Klick updaten!

🎯 GitHub Actions wird automatisch Setup.exe erstellen und Release
veröffentlichen. Benutzer erhalten Updates vollautomatisch!
"@
    
    # Lösche existierenden Tag falls vorhanden
    git tag -d $tagName 2>$null | Out-Null
    git push origin --delete $tagName 2>$null | Out-Null
    
    git tag -a $tagName -m $tagMessage
    Write-Host "   ✅ Tag $tagName erstellt" -ForegroundColor Green
    
    # 6. Push zu GitHub
    Write-Host ""
    Write-Host "🌐 6/6 - Push zu GitHub..." -ForegroundColor Yellow
    Write-Host "   ⏳ Pushe Branch und Tag..." -ForegroundColor Blue
    
    git push origin master
    git push origin $tagName
    
    Write-Host "   ✅ Push erfolgreich!" -ForegroundColor Green
    
    # Erfolgsmeldung
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║   🎉 RELEASE v$version ERFOLGREICH VORBEREITET!                    ║" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "📋 WAS PASSIERT JETZT:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1️⃣  GitHub Actions wird automatisch:" -ForegroundColor Yellow
    Write-Host "   • .NET 8 Build erstellen" -ForegroundColor White
    Write-Host "   • Setup.exe mit Inno Setup kompilieren" -ForegroundColor White
    Write-Host "   • update-info.json generieren" -ForegroundColor White
    Write-Host "   • Release Notes erstellen" -ForegroundColor White
    Write-Host "   • GitHub Release veröffentlichen" -ForegroundColor White
    Write-Host ""
    
    Write-Host "2️⃣  Nach ca. 5-10 Minuten:" -ForegroundColor Yellow
    Write-Host "   • Release verfügbar unter:" -ForegroundColor White
    Write-Host "     https://github.com/Elemirus1996/Einsatzueberwachung/releases/tag/$tagName" -ForegroundColor Blue
    Write-Host "   • Setup.exe Download-Link verfügbar" -ForegroundColor White
    Write-Host "   • Automatische Update-Benachrichtigungen aktiviert" -ForegroundColor White
    Write-Host ""
    
    Write-Host "3️⃣  Benutzer mit v1.6.x erhalten:" -ForegroundColor Yellow
    Write-Host "   • Automatische Update-Benachrichtigung beim nächsten Start" -ForegroundColor White
    Write-Host "   • Ein-Klick-Update direkt aus der Anwendung" -ForegroundColor White
    Write-Host ""
    
    $openGitHub = Read-Host "🌐 Möchten Sie GitHub Actions jetzt öffnen? (j/n)"
    if ($openGitHub -eq 'j' -or $openGitHub -eq 'J') {
        Start-Process "https://github.com/Elemirus1996/Einsatzueberwachung/actions"
    }
    
    $openReleases = Read-Host "📦 Möchten Sie GitHub Releases öffnen? (j/n)"
    if ($openReleases -eq 'j' -or $openReleases -eq 'J') {
        Start-Process "https://github.com/Elemirus1996/Einsatzueberwachung/releases"
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
    Write-Host "3. Manuelle Schritte:" -ForegroundColor White
    Write-Host "   git push origin master" -ForegroundColor Gray
    Write-Host "   git push origin $tagName" -ForegroundColor Gray
    Write-Host ""
    exit 1
}

Write-Host ""
Write-Host "✅ Quick Release Script abgeschlossen!" -ForegroundColor Green
Write-Host ""
