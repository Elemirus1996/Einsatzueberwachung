@echo off
:: Git Tag Loeschen - Batch Version
:: Loescht Tag lokal und auf GitHub

echo ================================================
echo  Git Tag Loeschen
echo ================================================
echo.

set TAG=v1.7.1

echo Dieser Befehl loescht den Tag '%TAG%':
echo   - Lokal in Ihrem Repository
echo   - Auf GitHub (remote)
echo.
set /p CONFIRM="Moechten Sie den Tag '%TAG%' wirklich loeschen? (j/n): "

if /i not "%CONFIRM%"=="j" (
    echo.
    echo Abgebrochen.
    pause
    exit /b
)

echo.
echo ================================================
echo Schritt 1: Lokalen Tag loeschen
echo ================================================
echo.

git tag -d %TAG%

if errorlevel 1 (
    echo WARNUNG: Lokaler Tag existiert nicht oder konnte nicht geloescht werden.
) else (
    echo OK Lokaler Tag '%TAG%' geloescht!
)

echo.
echo ================================================
echo Schritt 2: Remote Tag auf GitHub loeschen
echo ================================================
echo.

git push origin --delete %TAG%

if errorlevel 1 (
    echo WARNUNG: Remote Tag existiert nicht oder konnte nicht geloescht werden.
    echo Das kann passieren wenn:
    echo   - Der Tag auf GitHub nicht existiert
    echo   - Repository Rules das Loeschen verhindern
    echo   - Sie keine Berechtigung haben
) else (
    echo OK Remote Tag '%TAG%' von GitHub geloescht!
)

echo.
echo ================================================
echo Fertig!
echo ================================================
echo.
echo Der Tag '%TAG%' wurde geloescht (soweit vorhanden).
echo.
echo Sie koennen jetzt einen neuen Tag erstellen mit:
echo   Create-Tag-GitBash.bat
echo.
pause
