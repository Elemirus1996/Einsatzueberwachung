@echo off
:: Git Tag Creator - Automatische Version aus VersionService.cs
:: Liest die Version dynamisch aus Services\VersionService.cs
:: Erstellt: 2025-01-05 - Zentrales Versionsnummer-System
:: VERBESSERT: 2025-01-10 - Robuste Fehlerbehandlung

echo ================================================
echo  Git Tag Creator - AUTO VERSION
echo ================================================
echo.

:: Debug-Modus aktivieren
set DEBUG=1

:: Pr??fe ob wir im richtigen Verzeichnis sind
if not exist "Services\VersionService.cs" (
    echo ??? FEHLER: Services\VersionService.cs nicht gefunden!
    echo.
    echo Aktuelles Verzeichnis: %CD%
    echo.
    echo Bitte stellen Sie sicher, dass Sie das Script aus dem
    echo Hauptverzeichnis des Projekts ausf??hren.
    echo.
    echo Erwartete Struktur:
    echo   ????????? Services\
    echo       ????????? VersionService.cs
    echo.
    pause
    exit /b 1
)

:: Pr??fe ob PowerShell verf??gbar ist
echo Teste PowerShell-Verf??gbarkeit...
powershell -Command "Write-Host 'PowerShell Test erfolgreich'" >nul 2>nul
if errorlevel 1 (
    echo ??? FEHLER: PowerShell ist nicht verf??gbar!
    echo Dieses Script ben??tigt PowerShell zum Lesen der VersionService.cs
    echo.
    pause
    exit /b 1
)

echo ??? PowerShell verf??gbar

:: Extrahiere Version aus VersionService.cs mit verbesserter Fehlerbehandlung
echo.
echo Lese Version aus Services\VersionService.cs...

:: Erstelle tempor??re PowerShell-Script-Datei f??r bessere Debugging
echo try { > temp_version_reader.ps1
echo   $content = Get-Content 'Services\VersionService.cs' -Raw -ErrorAction Stop >> temp_version_reader.ps1
echo   if ($content -match 'private const string MAJOR_VERSION = "(\d+)"') { >> temp_version_reader.ps1
echo     $major = $matches[1] >> temp_version_reader.ps1
echo   } else { >> temp_version_reader.ps1
echo     Write-Host "MAJOR_VERSION nicht gefunden, verwende 1" >> temp_version_reader.ps1
echo     $major = '1' >> temp_version_reader.ps1
echo   } >> temp_version_reader.ps1
echo   if ($content -match 'private const string MINOR_VERSION = "(\d+)"') { >> temp_version_reader.ps1
echo     $minor = $matches[1] >> temp_version_reader.ps1
echo   } else { >> temp_version_reader.ps1
echo     Write-Host "MINOR_VERSION nicht gefunden, verwende 0" >> temp_version_reader.ps1
echo     $minor = '0' >> temp_version_reader.ps1
echo   } >> temp_version_reader.ps1
echo   if ($content -match 'private const string PATCH_VERSION = "(\d+)"') { >> temp_version_reader.ps1
echo     $patch = $matches[1] >> temp_version_reader.ps1
echo   } else { >> temp_version_reader.ps1
echo     Write-Host "PATCH_VERSION nicht gefunden, verwende 0" >> temp_version_reader.ps1
echo     $patch = '0' >> temp_version_reader.ps1
echo   } >> temp_version_reader.ps1
echo   Write-Output "$major.$minor.$patch" >> temp_version_reader.ps1
echo } catch { >> temp_version_reader.ps1
echo   Write-Host "FEHLER beim Lesen der VersionService.cs: $_" >> temp_version_reader.ps1
echo   Write-Output "ERROR" >> temp_version_reader.ps1
echo } >> temp_version_reader.ps1

:: F??hre PowerShell-Script aus
for /f "delims=" %%i in ('powershell -ExecutionPolicy Bypass -File temp_version_reader.ps1') do set VERSION=1.9.7

:: R??ume tempor??re Datei auf
del temp_version_reader.ps1 >nul 2>nul

:: Pr??fe das Ergebnis
if "%VERSION%"=="ERROR" (
    echo ??? FEHLER: Konnte Version nicht aus VersionService.cs extrahieren!
    echo.
    echo PowerShell-Fehler beim Lesen der Datei.
    echo Bitte pr??fen Sie:
    echo 1. Existiert Services\VersionService.cs?
    echo 2. Ist die Datei korrekt formatiert?
    echo 3. Haben Sie Lese-Berechtigung?
    echo.
    pause
    exit /b 1
)

if "%VERSION%"=="" (
    echo ??? FEHLER: Keine Version extrahiert!
    echo.
    echo Fallback auf manuelle Eingabe...
    set /p VERSION="Bitte Versionsnummer eingeben (z.B. 1.9.0): "
    if "%VERSION%"=="" (
        echo Keine Version eingegeben. Abgebrochen.
        pause
        exit /b 1
    )
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

:: Pr??fe ob es eine Development-Version ist mit robusterem Handling
echo Pr??fe Development-Status...

:: Erstelle tempor??res PowerShell-Script f??r Development-Check
echo try { > temp_dev_check.ps1
echo   $content = Get-Content 'Services\VersionService.cs' -Raw -ErrorAction Stop >> temp_dev_check.ps1
echo   if ($content -match 'private const bool IS_DEVELOPMENT_VERSION = true') { >> temp_dev_check.ps1
echo     Write-Output 'DEVELOPMENT' >> temp_dev_check.ps1
echo   } else { >> temp_dev_check.ps1
echo     Write-Output 'RELEASE' >> temp_dev_check.ps1
echo   } >> temp_dev_check.ps1
echo } catch { >> temp_dev_check.ps1
echo   Write-Host "FEHLER beim Development-Check: $_" >> temp_dev_check.ps1
echo   Write-Output 'UNKNOWN' >> temp_dev_check.ps1
echo } >> temp_dev_check.ps1

for /f "delims=" %%i in ('powershell -ExecutionPolicy Bypass -File temp_dev_check.ps1') do set VERSION_TYPE=%%i

:: R??ume tempor??re Datei auf
del temp_dev_check.ps1 >nul 2>nul

if "%VERSION_TYPE%"=="DEVELOPMENT" (
    echo ??????  WARNUNG: Dies ist eine DEVELOPMENT-Version!
    echo.
    echo Vor dem Release sollten Sie:
    echo 1. IS_DEVELOPMENT_VERSION = false setzen in VersionService.cs
    echo 2. Einsatzueberwachung.csproj Versionen aktualisieren
    echo 3. Build und Test durchf??hren
    echo.
    set /p CONTINUE_DEV="Trotzdem als Development-Release fortfahren? (j/n): "
    if /i not "%CONTINUE_DEV%"=="j" (
        echo Abgebrochen. Bitte VersionService.cs f??r Release konfigurieren.
        echo.
        pause
        exit /b 0
    )
    echo.
    echo Erstelle Development-Release-Tag...
) else if "%VERSION_TYPE%"=="RELEASE" (
    echo ??? Release-Version erkannt
) else (
    echo ??????  WARNUNG: Development-Status unbekannt
    echo Fahre als Release-Version fort...
)

echo.
echo ================================================
echo  GIT-OPERATIONEN
echo ================================================
echo.
echo Dieser Befehl wird ausgefuehrt:
echo   1. git add . (alle ??nderungen hinzuf??gen)
echo   2. git commit -m "Prepare release v%VERSION%"
echo   3. git push origin master
echo   4. git tag -a %TAG% -m "Release %TAG% - Einsatzueberwachung Professional"
echo   5. git push origin %TAG%
echo.
echo Der GitHub Actions Workflow erstellt dann automatisch:
echo   - Build der .NET Anwendung
echo   - Setup.exe mit Inno Setup
echo   - GitHub Release mit allen Dateien
echo.
set /p CONFIRM="Fortfahren? (j/n): "

if /i not "%CONFIRM%"=="j" (
    echo Abgebrochen.
    echo.
    pause
    exit /b 0
)

echo.
echo Erstelle Git Tag f??r Version %VERSION%...
echo.

:: Pr??fe Git-Status
echo ???? Aktueller Git-Status:
git status --porcelain
echo.

:: Zuerst alle Aenderungen committen
echo ???? Committing changes for v%VERSION%...
git add .

if errorlevel 1 (
    echo ??? FEHLER: 'git add .' fehlgeschlagen!
    echo.
    pause
    exit /b 1
)

git commit -m "Prepare release v%VERSION%"

if errorlevel 1 (
    echo ??????  WARNUNG: Nichts zu committen oder Commit fehlgeschlagen.
    echo Das kann normal sein, wenn bereits alle ??nderungen committed sind.
    echo Fahre fort...
    echo.
) else (
    echo ??? Commit erfolgreich
)

echo ???? Pushe Aenderungen zum master branch...
git push origin master

if errorlevel 1 (
    echo ??? FEHLER: Push zum master branch fehlgeschlagen!
    echo.
    echo M??gliche Ursachen:
    echo 1. Keine Internetverbindung
    echo 2. Repository Rules blockieren Push
    echo 3. Authentifizierung fehlgeschlagen
    echo.
    pause
    exit /b 1
)

echo ??? Push zum master erfolgreich

:: Pr??fe ob Tag bereits existiert
echo ???????  Pr??fe existierende Tags...
git tag -l %TAG% | findstr %TAG% >nul
if not errorlevel 1 (
    echo.
    echo ??????  WARNUNG: Tag %TAG% existiert bereits lokal!
    set /p DELETE_TAG="Lokalen Tag l??schen und neu erstellen? (j/n): "
    if /i "%DELETE_TAG%"=="j" (
        echo L??sche lokalen Tag %TAG%...
        git tag -d %TAG%
        echo L??sche Remote-Tag %TAG%...
        git push origin --delete %TAG% >nul 2>nul
        echo ??? Tag gel??scht
    ) else (
        echo Abgebrochen.
        echo.
        pause
        exit /b 0
    )
    echo.
)

:: Erstelle neuen Tag
echo ???????  Erstelle neuen Tag %TAG%...
git tag -a %TAG% -m "Release %TAG% - Einsatzueberwachung Professional v%VERSION%"

if errorlevel 1 (
    echo.
    echo ??? FEHLER: Tag konnte nicht erstellt werden!
    echo.
    pause
    exit /b 1
)

echo ??? Tag '%TAG%' erfolgreich erstellt!
echo.

echo ???? Pushe Tag zu GitHub...
git push origin %TAG%

if errorlevel 1 (
    echo.
    echo ??? FEHLER: Tag konnte nicht gepusht werden!
    echo Der Tag wurde lokal erstellt, aber nicht zu GitHub gepusht.
    echo.
    echo M??gliche Ursachen:
    echo 1. Repository Rules blockieren den Tag-Push
    echo 2. Netzwerk-Problem
    echo 3. Authentifizierung fehlgeschlagen
    echo.
    echo Sie k??nnen manuell pushen mit:
    echo   git push origin %TAG%
    echo.
    echo Oder versuchen Sie einen manuellen Release ??ber GitHub Web-Interface:
    echo   https://github.com/Elemirus1996/Einsatzueberwachung/releases/new
    echo.
    pause
    exit /b 1
)

echo ??? Tag erfolgreich zu GitHub gepusht!

echo.
echo ================================================
echo  ???? ERFOLG!
echo ================================================
echo.
echo Der Tag '%TAG%' wurde erfolgreich zu GitHub gepusht.
echo Version: %VERSION% (aus VersionService.cs)
echo.
echo ???? GitHub Actions Workflow wurde gestartet!
echo.
echo Status pruefen:
echo   https://github.com/Elemirus1996/Einsatzueberwachung/actions
echo.
echo Release wird verfuegbar sein unter:
echo   https://github.com/Elemirus1996/Einsatzueberwachung/releases/tag/%TAG%
echo.
echo ???? Release-Prozess erfolgreich abgeschlossen!
echo.
echo Dr??cken Sie eine beliebige Taste zum Beenden...
pause >nul




