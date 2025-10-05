@echo off
echo.
echo ========================================================
echo EINSATZUEBERWACHUNG v1.7.1 - FORCE RELEASE
echo ========================================================
echo.

echo 1/5 - Alle Dateien zu Git hinzufuegen...
git add .
if %errorlevel% neq 0 (
    echo FEHLER: Git add fehlgeschlagen
    pause
    exit /b 1
)
echo    Erfolgreich!

echo.
echo 2/5 - Commit erstellen...
git commit -m "Fix: Version system - auto-read from assembly, validation in workflow"
if %errorlevel% neq 0 (
    echo WARNUNG: Commit fehlgeschlagen - vermutlich keine Aenderungen
)
echo    Erfolgreich!

echo.
echo 3/5 - Push zu GitHub...
git push origin master
if %errorlevel% neq 0 (
    echo FEHLER: Push fehlgeschlagen
    pause
    exit /b 1
)
echo    Erfolgreich!

echo.
echo 4/5 - Git Tag v1.7.1 erstellen (force)...
git tag -f v1.7.1
if %errorlevel% neq 0 (
    echo FEHLER: Tag-Erstellung fehlgeschlagen
    pause
    exit /b 1
)
echo    Tag v1.7.1 erstellt!

echo.
echo 5/5 - Tag zu GitHub pushen (force)...
git push origin v1.7.1 --force
if %errorlevel% neq 0 (
    echo FEHLER: Tag-Push fehlgeschlagen
    pause
    exit /b 1
)

echo.
echo ========================================================
echo RELEASE v1.7.1 ERFOLGREICH ERSTELLT!
echo ========================================================
echo.
echo GitHub Actions wird jetzt automatisch starten:
echo https://github.com/Elemirus1996/Einsatzueberwachung/actions
echo.
echo Der Workflow wird:
echo - Version validieren (v1.7.1 in .csproj vs Git Tag)
echo - Anwendung bauen
echo - Setup erstellen (v1.7.1.0)
echo - update-info.json generieren
echo - GitHub Release erstellen mit beiden Dateien
echo.
echo Nach ca. 5-10 Minuten ist der Release verfuegbar unter:
echo https://github.com/Elemirus1996/Einsatzueberwachung/releases/tag/v1.7.1
echo.

set /p open="Moechten Sie GitHub Actions jetzt oeffnen? (j/n): "
if /i "%open%"=="j" start https://github.com/Elemirus1996/Einsatzueberwachung/actions

pause
