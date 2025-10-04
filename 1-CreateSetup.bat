@echo off
echo.
echo ========================================================
echo SCHRITT 1: INNO SETUP INSTALLATION PRUEFEN
echo ========================================================
echo.

REM Prüfe ob Inno Setup bereits installiert ist
set INNO_PATH=
if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set INNO_PATH=%ProgramFiles%\Inno Setup 6\ISCC.exe
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set INNO_PATH=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe

if "%INNO_PATH%"=="" (
    echo ❌ Inno Setup 6 nicht gefunden!
    echo.
    echo 📥 INNO SETUP HERUNTERLADEN:
    echo    1. Besuchen Sie: https://jrsoftware.org/isinfo.php
    echo    2. Download "Inno Setup 6.2.2"
    echo    3. Als Administrator installieren
    echo    4. Standard-Installationspfad verwenden
    echo.
    echo 🔄 Nach der Installation führen Sie dieses Script erneut aus.
    echo.
    
    set /p openBrowser="Möchten Sie die Download-Seite jetzt öffnen? (j/n): "
    if /i "%openBrowser%"=="j" start https://jrsoftware.org/isinfo.php
    
    pause
    exit /b 1
) else (
    echo ✅ Inno Setup gefunden: %INNO_PATH%
    echo.
    echo 🚀 BEREIT FÜR SETUP-ERSTELLUNG!
    echo.
    pause
)

echo.
echo ========================================================
echo SCHRITT 2: SETUP-ERSTELLUNG STARTEN
echo ========================================================
echo.

echo 🔨 Starte vollständige Setup-Erstellung...
echo.

REM Führe PowerShell Build-Script aus
powershell.exe -ExecutionPolicy Bypass -File "Build-Setup.ps1" -CleanBuild

if %errorlevel% equ 0 (
    echo.
    echo 🎉 SETUP-ERSTELLUNG ERFOLGREICH!
    echo.
    if exist "Setup\Output\*.exe" (
        echo 📦 Setup-Datei erstellt in: Setup\Output\
        for %%f in (Setup\Output\*.exe) do echo    • %%~nxf
        echo.
        
        set /p openFolder="Möchten Sie den Output-Ordner öffnen? (j/n): "
        if /i "%openFolder%"=="j" explorer "Setup\Output"
        
        echo.
        echo 🚀 NÄCHSTER SCHRITT: GITHUB RELEASE
        echo    1. Git Tag erstellen
        echo    2. GitHub Actions automatisch
        echo    3. Setup.exe wird veröffentlicht
        echo.
    )
) else (
    echo ❌ Setup-Erstellung fehlgeschlagen!
    echo Prüfen Sie die Ausgabe oben für Details.
)

pause
