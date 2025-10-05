@echo off
:: Git Tag erstellen mit Git Bash für Version 1.7.1
:: Startet automatisch den GitHub Actions Workflow

echo ================================================
echo  Git Tag Creator v1.7.1 - Git Bash Version
echo ================================================
echo.

set VERSION=1.7.1
set TAG=v%VERSION%

echo Version: %VERSION%
echo Tag:     %TAG%
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
echo Erstelle Git Tag...

:: Verwende git.exe direkt (funktioniert wenn Git installiert ist)
git tag -a %TAG% -m "Release %TAG% - Einsatzueberwachung Professional"

if errorlevel 1 (
    echo.
    echo FEHLER: Tag konnte nicht erstellt werden!
    echo Moeglicherweise existiert der Tag bereits lokal.
    echo.
    echo Zum Loeschen des lokalen Tags:
    echo   git tag -d %TAG%
    echo.
    pause
    exit /b 1
)

echo OK Tag '%TAG%' erstellt!
echo.
echo Pushe Tag zu GitHub...

git push origin %TAG%

if errorlevel 1 (
    echo.
    echo FEHLER: Tag konnte nicht gepusht werden!
    echo Der Tag wurde lokal erstellt, aber nicht zu GitHub gepusht.
    echo.
    echo Sie koennen manuell pushen mit:
    echo   git push origin %TAG%
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
echo.
echo GitHub Actions Workflow gestartet!
echo.
echo Status pruefen:
echo   https://github.com/Elemirus1996/Einsatzueberwachung/actions
echo.
echo Release wird verfuegbar sein unter:
echo   https://github.com/Elemirus1996/Einsatzueberwachung/releases/tag/%TAG%
echo.
pause
