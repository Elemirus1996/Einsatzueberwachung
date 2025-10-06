@echo off
:: Git Tag Creator - Automatische Version aus VersionService.cs
:: Liest die Version dynamisch aus Services\VersionService.cs
:: Erstellt: 2025-01-05 - Zentrales Versionsnummer-System

echo ================================================
echo  Git Tag Creator - AUTO VERSION
echo ================================================
echo.

:: Prüfe ob PowerShell verfügbar ist
powershell -Command "Write-Host 'PowerShell verfügbar'" >nul 2>nul
if errorlevel 1 (
    echo FEHLER: PowerShell ist nicht verfügbar!
    echo Dieses Script benötigt PowerShell zum Lesen der VersionService.cs
    pause
    exit /b 1
)

:: Extrahiere Version aus VersionService.cs
echo Lese Version aus Services\VersionService.cs...
for /f "delims=" %%i in ('powershell -Command "& {$content = Get-Content 'Services\VersionService.cs' -Raw; if ($content -match 'private const string MAJOR_VERSION = \"(\d+)\"') {$major = $matches[1]} else {$major = '1'}; if ($content -match 'private const string MINOR_VERSION = \"(\d+)\"') {$minor = $matches[1]} else {$minor = '0'}; if ($content -match 'private const string PATCH_VERSION = \"(\d+)\"') {$patch = $matches[1]} else {$patch = '0'}; Write-Output \"$major.$minor.$patch\"}"') do set VERSION=%%i

if "%VERSION%"=="" (
    echo FEHLER: Konnte Version nicht aus VersionService.cs extrahieren!
    echo Fallback auf manuelle Version...
    set /p VERSION="Bitte Versionsnummer eingeben (z.B. 1.9.0): "
)

set TAG=v%VERSION%

echo.
echo ================================================
echo  AUTOMATISCH ERKANNTE VERSION
echo ================================================
echo Version: %VERSION%
echo Tag:     %TAG%
echo Quelle:  Services\VersionService.cs
echo.

:: Prüfe ob es eine Development-Version ist
powershell -Command "& {$content = Get-Content 'Services\VersionService.cs' -Raw; if ($content -match 'private const bool IS_DEVELOPMENT_VERSION = true') {Write-Output 'DEVELOPMENT'} else {Write-Output 'RELEASE'}}" > temp_version_type.txt
set /p VERSION_TYPE=<temp_version_type.txt
del temp_version_type.txt

if "%VERSION_TYPE%"=="DEVELOPMENT" (
    echo ⚠️  WARNUNG: Dies ist eine DEVELOPMENT-Version!
    echo.
    echo Vor dem Release sollten Sie:
    echo 1. IS_DEVELOPMENT_VERSION = false setzen in VersionService.cs
    echo 2. Einsatzueberwachung.csproj Versionen aktualisieren
    echo 3. Build und Test durchführen
    echo.
    set /p CONTINUE_DEV="Trotzdem als Development-Release fortfahren? (j/n): "
    if /i not "%CONTINUE_DEV%"=="j" (
        echo Abgebrochen. Bitte VersionService.cs für Release konfigurieren.
        pause
        exit /b
    )
    echo.
    echo Erstelle Development-Release-Tag...
) else (
    echo ✅ Release-Version erkannt
)

echo.
echo Dieser Befehl wird ausgefuehrt:
echo   git tag -a %TAG% -m "Release %TAG% - Einsatzueberwachung Professional"
echo   git push origin %TAG%
echo.
echo Der GitHub Actions Workflow erstellt dann automatisch:
echo   - Build der .NET Anwendung
echo   - Setup.exe mit Inno Setup
echo   - GitHub Release mit allen Dateien
echo.
set /p CONFIRM="Fortfahren? (j/n): "

if /i not "%CONFIRM%"=="j" (
    echo Abgebrochen.
    pause
    exit /b
)

echo.
echo Erstelle Git Tag für Version %VERSION%...

:: Zuerst alle Aenderungen committen
echo Committing changes for v%VERSION%...
git add .
git commit -m "Prepare release v%VERSION%"

if errorlevel 1 (
    echo WARNUNG: Nichts zu committen oder Commit fehlgeschlagen. Fahre fort...
)

echo Pushe Aenderungen zum master branch...
git push origin master

:: Prüfe ob Tag bereits existiert
git tag -l %TAG% | findstr %TAG% >nul
if not errorlevel 1 (
    echo.
    echo WARNUNG: Tag %TAG% existiert bereits lokal!
    set /p DELETE_TAG="Lokalen Tag löschen und neu erstellen? (j/n): "
    if /i "%DELETE_TAG%"=="j" (
        echo Lösche lokalen Tag %TAG%...
        git tag -d %TAG%
        echo Lösche Remote-Tag %TAG%...
        git push origin --delete %TAG% 2>nul
    ) else (
        echo Abgebrochen.
        pause
        exit /b
    )
)

:: Erstelle neuen Tag
git tag -a %TAG% -m "Release %TAG% - Einsatzueberwachung Professional v%VERSION%"

if errorlevel 1 (
    echo.
    echo FEHLER: Tag konnte nicht erstellt werden!
    echo.
    pause
    exit /b 1
)

echo ✅ Tag '%TAG%' erstellt!
echo.
echo Pushe Tag zu GitHub...

git push origin %TAG%

if errorlevel 1 (
    echo.
    echo FEHLER: Tag konnte nicht gepusht werden!
    echo Der Tag wurde lokal erstellt, aber nicht zu GitHub gepusht.
    echo Moegliche Ursache: Repository Rules blockieren den Push.
    echo.
    echo Sie koennen manuell pushen mit:
    echo   git push origin %TAG%
    echo.
    echo Oder versuchen Sie einen manuellen Release über GitHub Web-Interface:
    echo   https://github.com/Elemirus1996/Einsatzueberwachung/releases/new
    echo.
    pause
    exit /b 1
)

echo.
echo ================================================
echo  ERFOLG!
echo ================================================
echo.
echo Der Tag '%TAG%' wurde zu GitHub gepusht.
echo Version: %VERSION% (aus VersionService.cs)
echo.
echo GitHub Actions Workflow gestartet!
echo.
echo Status pruefen:
echo   https://github.com/Elemirus1996/Einsatzueberwachung/actions
echo.
echo Release wird verfuegbar sein unter:
echo   https://github.com/Elemirus1996/Einsatzueberwachung/releases/tag/%TAG%
echo.
echo 🎉 Release-Prozess abgeschlossen!
echo.
pause
