@echo off
REM Einsatzüberwachung Professional v1.6 - Setup Batch Script
REM Einfache Setup-Erstellung mit angepasstem Inno Setup Pfad

echo.
echo ========================================================
echo EINSATZUEBERWACHUNG PROFESSIONAL v1.6 - SETUP ERSTELLEN
echo ========================================================
echo.

REM Prüfe ob PowerShell verfügbar ist
where powershell >nul 2>nul
if %errorlevel% neq 0 (
    echo ❌ PowerShell nicht gefunden!
    echo    PowerShell wird für die Setup-Erstellung benötigt.
    pause
    exit /b 1
)

REM Prüfe Inno Setup unter Y:\Inno Setup 6
if not exist "Y:\Inno Setup 6\ISCC.exe" (
    echo ❌ Inno Setup nicht gefunden unter Y:\Inno Setup 6\ISCC.exe
    echo.
    echo 💡 LÖSUNGEN:
    echo    1. Prüfen Sie den Installationspfad von Inno Setup
    echo    2. Installieren Sie Inno Setup unter Y:\Inno Setup 6\
    echo    3. Oder verwenden Sie Build-Setup.ps1 mit eigenem Pfad
    echo.
    pause
    exit /b 1
)

echo ✅ Inno Setup gefunden: Y:\Inno Setup 6\ISCC.exe
echo.

REM Optionen anzeigen
echo 💡 SETUP-OPTIONEN:
echo.
echo [1] Vollständiges Setup erstellen (empfohlen)
echo [2] Nur Build ohne Setup
echo [3] Clean Build + Setup
echo [4] Setup-Script manuell ausführen
echo [5] Nur Inno Setup direkt starten
echo.
set /p choice="Wählen Sie eine Option (1-5): "

if "%choice%"=="1" goto :fullsetup
if "%choice%"=="2" goto :buildonly
if "%choice%"=="3" goto :cleanbuild
if "%choice%"=="4" goto :manualscript
if "%choice%"=="5" goto :innosetup
goto :invalid

:fullsetup
echo.
echo 🚀 Erstelle vollständiges Setup mit Y:\Inno Setup 6...
powershell.exe -ExecutionPolicy Bypass -File Build-Setup.ps1
goto :end

:buildonly
echo.
echo 🔨 Nur Build ohne Setup...
dotnet build "Einsatzüberwachung V1.5.csproj" --configuration Release
dotnet publish "Einsatzüberwachung V1.5.csproj" --configuration Release --output "bin\Release\net8.0-windows"
echo ✅ Build abgeschlossen
goto :end

:cleanbuild
echo.
echo 🧹 Clean Build + Setup mit Y:\Inno Setup 6...
powershell.exe -ExecutionPolicy Bypass -File Build-Setup.ps1 -CleanBuild
goto :end

:manualscript
echo.
echo 📋 Öffne PowerShell für manuelles Setup...
start powershell.exe -NoExit -Command "Set-Location '%~dp0'; Write-Host '💡 Führen Sie aus: .\Build-Setup.ps1' -ForegroundColor Yellow"
goto :end

:innosetup
echo.
echo 📦 Erstelle Setup direkt mit Inno Setup...

REM Prüfe ob Build existiert
if not exist "bin\Release\net8.0-windows\Einsatzueberwachung.exe" (
    echo ⚠️ Build nicht gefunden. Erstelle zuerst Build...
    dotnet build "Einsatzüberwachung V1.5.csproj" --configuration Release
    dotnet publish "Einsatzüberwachung V1.5.csproj" --configuration Release --output "bin\Release\net8.0-windows"
)

echo ✅ Build gefunden
echo 📦 Erstelle Setup mit Inno Setup...
"Y:\Inno Setup 6\ISCC.exe" "Setup\Einsatzueberwachung_Setup.iss"

if %errorlevel% equ 0 (
    echo ✅ Setup erfolgreich erstellt!
    if exist "Setup\Output\Einsatzueberwachung_Professional_v*.exe" (
        echo 📂 Setup-Datei: Setup\Output\
        for %%f in (Setup\Output\Einsatzueberwachung_Professional_v*.exe) do echo    • %%~nxf
        set /p opendir="Möchten Sie den Output-Ordner öffnen? (j/n): "
        if /i "%opendir%"=="j" explorer "Setup\Output"
    )
) else (
    echo ❌ Setup-Erstellung fehlgeschlagen!
)
goto :end

:invalid
echo ❌ Ungültige Option!
pause
exit /b 1

:end
echo.
echo 🎉 VORGANG ABGESCHLOSSEN!
echo.
echo 📋 VERFÜGBARE DATEIEN:
if exist "bin\Release\net8.0-windows\Einsatzueberwachung.exe" echo • Anwendung: bin\Release\net8.0-windows\
if exist "Setup\Output\Einsatzueberwachung_Professional_v*.exe" echo • Setup.exe: Setup\Output\
echo • Dokumentation: .\Documentation\
echo • PowerShell-Scripts: .\Scripts\
echo.
echo 💡 NÄCHSTE SCHRITTE FÜR GITHUB RELEASE:
echo.
echo 1. Git-Repository aktualisieren:
echo    git add .
echo    git commit -m "Release v1.6.0 mit Setup.exe"
echo.
echo 2. Git Tag erstellen und pushen:
echo    git tag v1.6.0
echo    git push origin master
echo    git push --tags
echo.
echo 3. GitHub Actions erstellt automatisch Release!
echo    URL: https://github.com/Elemirus1996/Einsatzueberwachung/actions
echo.

set /p test="Möchten Sie das Setup jetzt testen? (j/n): "
if /i "%test%"=="j" (
    for %%f in (Setup\Output\Einsatzueberwachung_Professional_v*.exe) do (
        echo 🚀 Starte Setup: %%f
        start "" "%%f"
        goto :testdone
    )
    echo ❌ Setup-Datei nicht gefunden!
    :testdone
)

set /p github="Möchten Sie das GitHub Repository öffnen? (j/n): "
if /i "%github%"=="j" start https://github.com/Elemirus1996/Einsatzueberwachung

pause
