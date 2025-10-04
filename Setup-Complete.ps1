# Automatische Inno Setup Installation und Setup-Erstellung
# PowerShell Script für kompletten Setup-Prozess

param(
    [switch]$AutoInstallInno = $false,
    [switch]$SkipInnoCheck = $false
)

Write-Host "Einsatzueberwachung Professional - Setup Erstellung" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Continue"

# Funktion: Pruefe Administrator-Rechte
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Funktion: Pruefe Inno Setup Installation
function Test-InnoSetup {
    $innoPaths = @(
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    )
    
    foreach ($path in $innoPaths) {
        if (Test-Path $path) {
            return $path
        }
    }
    return $null
}

# Funktion: Installiere Inno Setup automatisch
function Install-InnoSetup {
    param([bool]$Force = $false)
    
    if (-not $Force) {
        $response = Read-Host "Moechten Sie Inno Setup 6 automatisch herunterladen und installieren? (j/n)"
        if ($response -ne 'j' -and $response -ne 'J') {
            return $false
        }
    }
    
    try {
        Write-Host "Lade Inno Setup 6.2.2 herunter..." -ForegroundColor Yellow
        
        $downloadUrl = "https://files.jrsoftware.org/is/6/innosetup-6.2.2.exe"
        $tempPath = "$env:TEMP\innosetup-6.2.2.exe"
        
        # Download mit Progress
        $webClient = New-Object System.Net.WebClient
        $webClient.DownloadFile($downloadUrl, $tempPath)
        
        Write-Host "Download abgeschlossen" -ForegroundColor Green
        Write-Host "Installiere Inno Setup..." -ForegroundColor Yellow
        
        # Silent Installation
        $processArgs = @{
            FilePath = $tempPath
            ArgumentList = "/SILENT", "/SUPPRESSMSGBOXES", "/CLOSEAPPLICATIONS"
            Wait = $true
            NoNewWindow = $true
        }
        
        $process = Start-Process @processArgs
        
        # Warte auf Installation
        Start-Sleep -Seconds 5
        
        # Pruefe Installation
        $innoPath = Test-InnoSetup
        if ($innoPath) {
            Write-Host "Inno Setup erfolgreich installiert: $innoPath" -ForegroundColor Green
            
            # Cleanup
            if (Test-Path $tempPath) {
                Remove-Item $tempPath -Force
            }
            
            return $true
        } else {
            Write-Host "Installation fehlgeschlagen - bitte manuell installieren" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "Fehler beim Download/Installation: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Fallback: Manueller Download von https://jrsoftware.org/isinfo.php" -ForegroundColor Yellow
        return $false
    }
}

# Hauptprogramm
try {
    # Administrator-Check
    if (-not (Test-Administrator)) {
        Write-Host "Fuer optimale Ergebnisse als Administrator ausfuehren" -ForegroundColor Yellow
        Write-Host "Rechtsklick auf PowerShell -> 'Als Administrator ausfuehren'" -ForegroundColor Yellow
        Write-Host ""
    }

    # Inno Setup Check
    if (-not $SkipInnoCheck) {
        Write-Host "Pruefe Inno Setup Installation..." -ForegroundColor Yellow
        
        $innoPath = Test-InnoSetup
        
        if (-not $innoPath) {
            Write-Host "Inno Setup 6 nicht gefunden" -ForegroundColor Red
            Write-Host ""
            
            if ($AutoInstallInno -or (Install-InnoSetup)) {
                $innoPath = Test-InnoSetup
            }
            
            if (-not $innoPath) {
                Write-Host "SETUP-ERSTELLUNG ABGEBROCHEN" -ForegroundColor Red
                Write-Host ""
                Write-Host "MANUELLE INSTALLATION:" -ForegroundColor Cyan
                Write-Host "1. Besuchen Sie: https://jrsoftware.org/isinfo.php" -ForegroundColor White
                Write-Host "2. Download 'Inno Setup 6.2.2'" -ForegroundColor White
                Write-Host "3. Als Administrator installieren" -ForegroundColor White
                Write-Host "4. Script erneut ausfuehren" -ForegroundColor White
                Write-Host ""
                
                $openBrowser = Read-Host "Download-Seite jetzt oeffnen? (j/n)"
                if ($openBrowser -eq 'j' -or $openBrowser -eq 'J') {
                    Start-Process "https://jrsoftware.org/isinfo.php"
                }
                
                exit 1
            }
        } else {
            Write-Host "Inno Setup gefunden: $innoPath" -ForegroundColor Green
        }
        
        Write-Host ""
    }

    # Setup-Erstellung starten
    Write-Host "STARTE SETUP-ERSTELLUNG..." -ForegroundColor Cyan
    Write-Host "==============================" -ForegroundColor Cyan
    Write-Host ""

    # Pruefe ob Build-Setup.ps1 existiert
    if (-not (Test-Path "Build-Setup.ps1")) {
        Write-Host "Build-Setup.ps1 nicht gefunden!" -ForegroundColor Red
        Write-Host "Stelle sicher, dass Sie sich im korrekten Projektverzeichnis befinden" -ForegroundColor Yellow
        exit 1
    }

    # Fuehre Build-Setup aus
    Write-Host "Fuehre Build-Setup.ps1 aus..." -ForegroundColor Yellow
    & .\Build-Setup.ps1 -CleanBuild
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "SETUP-ERSTELLUNG ERFOLGREICH!" -ForegroundColor Green
        Write-Host "=================================" -ForegroundColor Green
        Write-Host ""
        
        # Pruefe Output
        $setupFiles = Get-ChildItem "Setup\Output\*.exe" -ErrorAction SilentlyContinue
        
        if ($setupFiles) {
            Write-Host "Erstellte Setup-Dateien:" -ForegroundColor Cyan
            foreach ($file in $setupFiles) {
                $sizeInMB = [math]::Round($file.Length / 1MB, 2)
                Write-Host "   - $($file.Name) ($sizeInMB MB)" -ForegroundColor White
            }
            Write-Host ""
            
            Write-Host "Setup-Verzeichnis: Setup\Output\" -ForegroundColor Blue
            Write-Host ""
            
            # Angebote fuer naechste Schritte
            Write-Host "NAECHSTE SCHRITTE:" -ForegroundColor Cyan
            Write-Host "1. Setup.exe lokal testen" -ForegroundColor White
            Write-Host "2. GitHub Release erstellen" -ForegroundColor White
            Write-Host "3. Automatische Verteilung" -ForegroundColor White
            Write-Host ""
            
            $choice = Read-Host "Waehlen Sie eine Option (1-3, oder Enter fuer beenden)"
            
            switch ($choice) {
                "1" {
                    Write-Host "Starte Setup-Test..." -ForegroundColor Yellow
                    $setupFile = $setupFiles[0].FullName
                    Start-Process $setupFile
                }
                "2" {
                    Write-Host "Bereite GitHub Release vor..." -ForegroundColor Yellow
                    Write-Host ""
                    Write-Host "GITHUB RELEASE SCHRITTE:" -ForegroundColor Cyan
                    Write-Host "1. git add ." -ForegroundColor White
                    Write-Host "2. git commit -m 'Release v1.6.0 mit Setup.exe'" -ForegroundColor White
                    Write-Host "3. git tag v1.6.0" -ForegroundColor White
                    Write-Host "4. git push --tags" -ForegroundColor White
                    Write-Host ""
                    Write-Host "GitHub Actions erstellt dann automatisch das Release!" -ForegroundColor Yellow
                }
                "3" {
                    Write-Host "Oeffne Setup-Ordner fuer manuelle Verteilung..." -ForegroundColor Yellow
                    Start-Process "explorer.exe" -ArgumentList "Setup\Output"
                }
            }
            
        } else {
            Write-Host "Keine Setup-Dateien im Output-Verzeichnis gefunden" -ForegroundColor Yellow
            Write-Host "Pruefen Sie: Setup\Output\" -ForegroundColor Blue
        }
        
    } else {
        Write-Host ""
        Write-Host "SETUP-ERSTELLUNG FEHLGESCHLAGEN!" -ForegroundColor Red
        Write-Host "====================================" -ForegroundColor Red
        Write-Host ""
        Write-Host "TROUBLESHOOTING:" -ForegroundColor Yellow
        Write-Host "1. Als Administrator ausfuehren" -ForegroundColor White
        Write-Host "2. .NET 8 SDK installiert?" -ForegroundColor White
        Write-Host "3. Inno Setup korrekt installiert?" -ForegroundColor White
        Write-Host "4. Projekt kompiliert ohne Fehler?" -ForegroundColor White
        Write-Host ""
    }

} catch {
    Write-Host ""
    Write-Host "KRITISCHER FEHLER!" -ForegroundColor Red
    Write-Host "Fehler: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "SUPPORT:" -ForegroundColor Yellow
    Write-Host "1. PowerShell als Administrator starten" -ForegroundColor White
    Write-Host "2. Script-Ausfuehrungsrichtlinie pruefen" -ForegroundColor White
    Write-Host "3. .NET 8 SDK vollstaendig installiert?" -ForegroundColor White
    Write-Host ""
}

Write-Host "Setup-Erstellung Script abgeschlossen" -ForegroundColor Green
