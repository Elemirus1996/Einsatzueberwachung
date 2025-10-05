@echo off
echo.
echo ========================================================
echo EINSATZUEBERWACHUNG v1.7.1 - QUICK RELEASE
echo ========================================================
echo.

echo 1/4 - Dateien zu Git hinzufuegen...
git add .
if %errorlevel% neq 0 (
    echo FEHLER: Git add fehlgeschlagen
    pause
    exit /b 1
)
echo    Erfolgreich!

echo.
echo 2/4 - Commit erstellen...
git commit -m "Release v1.7.1 - Dashboard und Stammdatenverwaltung - Version-Update von 1.6.0 zu 1.7.1 behoben - Performance-Optimierungen - Memory-Management verbessert"
if %errorlevel% neq 0 (
    echo FEHLER: Commit fehlgeschlagen
    pause
    exit /b 1
)
echo    Erfolgreich!

echo.
echo 3/4 - Git Tag erstellen...
git tag -d v1.7.1 2>nul
git push origin --delete v1.7.1 2>nul
git tag -a v1.7.1 -m "Einsatzueberwachung Professional v1.7.1 - Dashboard und Stammdatenverwaltung Edition"
if %errorlevel% neq 0 (
    echo FEHLER: Tag-Erstellung fehlgeschlagen
    pause
    exit /b 1
)
echo    Tag v1.7.1 erstellt!

echo.
echo 4/4 - Push zu GitHub...
git push origin 1.7.1
git push origin v1.7.1
if %errorlevel% neq 0 (
    echo FEHLER: Push fehlgeschlagen
    pause
    exit /b 1
)

echo.
echo ========================================================
echo RELEASE v1.7.1 ERFOLGREICH!
echo ========================================================
echo.
echo GitHub Release wird automatisch erstellt unter:
echo https://github.com/Elemirus1996/Einsatzueberwachung/releases/tag/v1.7.1
echo.
pause
