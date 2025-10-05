@echo off
:: Batch-Wrapper für Create-GitTag.ps1
:: Erstellt automatisch Git Tag für Version 1.7.1

echo ================================================
echo  Git Tag Creator - Quick Release
echo ================================================
echo.

powershell -ExecutionPolicy Bypass -File "%~dp0Create-GitTag.ps1" -Version "1.7.1"

pause
