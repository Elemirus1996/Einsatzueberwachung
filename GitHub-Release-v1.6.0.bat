@echo off
REM 🚀 GitHub Release v1.6.0 - Automatisches Release Script
REM Einsatzüberwachung Professional v1.6.0 Release

echo.
echo ========================================================
echo GITHUB RELEASE v1.6.0 - EINSATZUEBERWACHUNG PROFESSIONAL
echo ========================================================
echo.

REM Prüfe ob Git verfügbar ist
where git >nul 2>nul
if %errorlevel% neq 0 (
    echo ❌ Git nicht gefunden!
    echo.
    echo 💡 LÖSUNG: Installieren Sie Git für Windows:
    echo    https://git-scm.com/download/win
    echo.
    echo Oder führen Sie die Befehle manuell in Git Bash aus:
    echo    git add .
    echo    git commit -m "Release v1.6.0 - Professional Edition"
    echo    git tag v1.6.0
    echo    git push origin master
    echo    git push --tags
    echo.
    pause
    exit /b 1
)

echo ✅ Git ist verfügbar
echo.

REM Git Status anzeigen
echo 🔍 Prüfe Git Repository Status...
git status --short

echo.
echo 📋 BEREIT FÜR GITHUB RELEASE v1.6.0
echo.
echo Was wird gemacht:
echo   1. Alle Dateien hinzufügen (git add .)
echo   2. Commit erstellen mit Release-Nachricht
echo   3. Git Tag v1.6.0 erstellen
echo   4. Zu GitHub pushen (master branch)
echo   5. Tags zu GitHub pushen (triggert GitHub Actions)
echo.

set /p confirm="Möchten Sie fortfahren? (j/n): "
if /i NOT "%confirm%"=="j" (
    echo Abgebrochen.
    pause
    exit /b 0
)

echo.
echo 🚀 Starte GitHub Release v1.6.0...
echo.

REM 1. Alle Dateien hinzufügen
echo 📁 Füge alle Dateien hinzu...
git add .
if %errorlevel% neq 0 (
    echo ❌ Fehler beim Hinzufügen der Dateien!
    pause
    exit /b 1
)
echo ✅ Dateien hinzugefügt

REM 2. Commit erstellen
echo 💾 Erstelle Commit für v1.6.0...
git commit -m "🚀 Release v1.6.0 - Professional Edition mit GitHub Auto-Updates

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
- Professional Logging und Diagnostics"

if %errorlevel% neq 0 (
    echo ❌ Fehler beim Erstellen des Commits!
    pause
    exit /b 1
)
echo ✅ Commit erstellt

REM 3. Git Tag erstellen
echo 🏷️ Erstelle Git Tag v1.6.0...
git tag -a v1.6.0 -m "🚀 Einsatzüberwachung Professional v1.6.0

Major Release mit GitHub Auto-Updates und erweiterter Mobile Integration

Hauptfeatures:
• GitHub Auto-Update-System für nahtlose Updates
• Mobile Server mit QR-Code-Zugriff für iPhone/Android
• Professional Setup.exe mit automatischer Konfiguration
• Erweiterte Statistics und Analytics-Dashboard
• Dark/Light Mode und modernisierte Benutzeroberfläche
• Enterprise-ready Features für professionellen Einsatz

Diese Version bringt die Einsatzüberwachung auf professionelles Niveau!"

if %errorlevel% neq 0 (
    echo ❌ Fehler beim Erstellen des Tags!
    pause
    exit /b 1
)
echo ✅ Git Tag v1.6.0 erstellt

REM 4. Master Branch pushen
echo 🌐 Pushe Master Branch zu GitHub...
git push origin master
if %errorlevel% neq 0 (
    echo ❌ Fehler beim Pushen des Master Branch!
    echo 💡 Überprüfen Sie Ihre GitHub-Anmeldedaten
    pause
    exit /b 1
)
echo ✅ Master Branch gepusht

REM 5. Tags pushen (triggert GitHub Actions)
echo 🏷️ Pushe Tags zu GitHub (triggert Automatic Release)...
git push --tags
if %errorlevel% neq 0 (
    echo ❌ Fehler beim Pushen der Tags!
    echo 💡 Überprüfen Sie Ihre GitHub-Anmeldedaten
    pause
    exit /b 1
)
echo ✅ Tags gepusht - GitHub Actions sollte automatisch starten

echo.
echo 🎉 GITHUB RELEASE v1.6.0 ERFOLGREICH GESTARTET!
echo ================================================
echo.
echo 📋 Was passiert jetzt:
echo   1. GitHub Actions Workflow startet automatisch
echo   2. Setup.exe wird automatisch kompiliert und hochgeladen
echo   3. GitHub Release wird automatisch erstellt
echo   4. Update-Info wird für automatische Updates bereitgestellt
echo.
echo 🔗 Überwachen Sie den Prozess hier:
echo   GitHub Actions: https://github.com/Elemirus1996/Einsatzueberwachung/actions
echo   Releases:       https://github.com/Elemirus1996/Einsatzueberwachung/releases
echo.
echo ⏰ Der automatische Release-Prozess dauert ca. 5-10 Minuten
echo.

set /p openActions="Möchten Sie GitHub Actions jetzt öffnen? (j/n): "
if /i "%openActions%"=="j" (
    start https://github.com/Elemirus1996/Einsatzueberwachung/actions
)

set /p openReleases="Möchten Sie GitHub Releases öffnen? (j/n): "
if /i "%openReleases%"=="j" (
    start https://github.com/Elemirus1996/Einsatzueberwachung/releases
)

echo.
echo ✅ Release-Prozess gestartet!
echo 📱 Benutzer erhalten automatisch Update-Benachrichtigungen
echo 🎯 Version 1.6.0 wird in wenigen Minuten verfügbar sein!
echo.
pause
