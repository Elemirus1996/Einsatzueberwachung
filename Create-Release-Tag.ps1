# Git Release Creator - Automatische Version aus VersionService.cs
# PowerShell Version mit erweiterten Features
# Erstellt: 2025-01-05 - Zentrales Versionsnummer-System

param(
    [switch]$Force = $false,
    [switch]$DryRun = $false,
    [string]$Message = ""
)

Write-Host "================================================" -ForegroundColor Cyan
Write-Host " Git Release Creator - AUTO VERSION (PowerShell)" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# PrÃ¼fe ob VersionService.cs existiert
if (-not (Test-Path "Services\VersionService.cs")) {
    Write-Host "âŒ FEHLER: Services\VersionService.cs nicht gefunden!" -ForegroundColor Red
    Write-Host "   Bitte fÃ¼hren Sie das Script aus dem Hauptverzeichnis aus." -ForegroundColor Red
    Read-Host "DrÃ¼cken Sie Enter zum Beenden"
    exit 1
}

# Lese VersionService.cs
try {
    $versionServiceContent = Get-Content "Services\VersionService.cs" -Raw
    
    # Extrahiere Versionskomponenten
    if ($versionServiceContent -match 'private const string MAJOR_VERSION = "(\d+)"') {
        $major = $matches[1]
    } else {
        throw "MAJOR_VERSION nicht gefunden"
    }
    
    if ($versionServiceContent -match 'private const string MINOR_VERSION = "(\d+)"') {
        $minor = $matches[1]
    } else {
        throw "MINOR_VERSION nicht gefunden"
    }
    
    if ($versionServiceContent -match 'private const string PATCH_VERSION = "(\d+)"') {
        $patch = $matches[1]
    } else {
        throw "PATCH_VERSION nicht gefunden"
    }
    
    # PrÃ¼fe Development-Flag
    $isDevelopment = $versionServiceContent -match 'private const bool IS_DEVELOPMENT_VERSION = true'
    
    $version = "2.0.0"
    $tag = "v$version"
    
    Write-Host "================================================" -ForegroundColor Green
    Write-Host " AUTOMATISCH ERKANNTE VERSION" -ForegroundColor Green
    Write-Host "================================================" -ForegroundColor Green
    Write-Host "Version: $version" -ForegroundColor White
    Write-Host "Tag:     $tag" -ForegroundColor White
    Write-Host "Quelle:  Services\VersionService.cs" -ForegroundColor Gray
    
    if ($isDevelopment) {
        Write-Host "Typ:     ðŸš§ DEVELOPMENT VERSION" -ForegroundColor Yellow
    } else {
        Write-Host "Typ:     âœ… RELEASE VERSION" -ForegroundColor Green
    }
    Write-Host ""
    
} catch {
    Write-Host "âŒ FEHLER: Konnte Version nicht aus VersionService.cs extrahieren!" -ForegroundColor Red
    Write-Host "   Fehler: $($_.Exception.Message)" -ForegroundColor Red
    
    $version = Read-Host "Bitte Versionsnummer manuell eingeben (z.B. 1.9.0)"
    if ([string]::IsNullOrWhiteSpace($version)) {
        Write-Host "Abgebrochen." -ForegroundColor Red
        exit 1
    }
    $tag = "v$version"
    $isDevelopment = $false
}

# Development-Version Warnung
if ($isDevelopment -and -not $Force) {
    Write-Host "âš ï¸  WARNUNG: Dies ist eine DEVELOPMENT-Version!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Vor dem Release sollten Sie:" -ForegroundColor Yellow
    Write-Host "1. IS_DEVELOPMENT_VERSION = false setzen in Services\VersionService.cs" -ForegroundColor Yellow
    Write-Host "2. Einsatzueberwachung.csproj Versionen aktualisieren" -ForegroundColor Yellow
    Write-Host "3. Build und Test durchfÃ¼hren" -ForegroundColor Yellow
    Write-Host ""
    
    $continue = Read-Host "Trotzdem als Development-Release fortfahren? (j/n)"
    if ($continue -ne 'j') {
        Write-Host "Abgebrochen. Bitte VersionService.cs fÃ¼r Release konfigurieren." -ForegroundColor Red
        exit 1
    }
    Write-Host ""
    Write-Host "ðŸš§ Erstelle Development-Release-Tag..." -ForegroundColor Yellow
} elseif (-not $isDevelopment) {
    Write-Host "âœ… Release-Version erkannt" -ForegroundColor Green
}

# PrÃ¼fe Git Status
try {
    $gitStatus = git status --porcelain 2>$null
    if ($gitStatus) {
        Write-Host "â„¹ï¸  Uncommitted changes gefunden:" -ForegroundColor Blue
        git status --short
        Write-Host ""
    }
} catch {
    Write-Host "âš ï¸  Git-Status konnte nicht geprÃ¼ft werden" -ForegroundColor Yellow
}

# Zeige geplante Aktionen
Write-Host "ðŸŽ¯ GEPLANTE AKTIONEN:" -ForegroundColor Cyan
Write-Host "1. git add ." -ForegroundColor Gray
Write-Host "2. git commit -m `"Prepare release $tag`"" -ForegroundColor Gray
Write-Host "3. git push origin master" -ForegroundColor Gray
Write-Host "4. git tag -a $tag -m `"Release $tag - Einsatzueberwachung Professional v$version`"" -ForegroundColor Gray
Write-Host "5. git push origin $tag" -ForegroundColor Gray
Write-Host ""

Write-Host "Der GitHub Actions Workflow erstellt dann automatisch:" -ForegroundColor Cyan
Write-Host "â€¢ Build der .NET Anwendung" -ForegroundColor Gray
Write-Host "â€¢ Setup.exe mit Inno Setup" -ForegroundColor Gray
Write-Host "â€¢ GitHub Release mit allen Dateien" -ForegroundColor Gray
Write-Host ""

if ($DryRun) {
    Write-Host "ðŸ” DRY RUN - Keine Ã„nderungen werden vorgenommen" -ForegroundColor Blue
    exit 0
}

if (-not $Force) {
    $confirm = Read-Host "ðŸš€ Fortfahren? (j/n)"
    if ($confirm -ne 'j') {
        Write-Host "Abgebrochen." -ForegroundColor Red
        exit 0
    }
}

Write-Host ""
Write-Host "ðŸš€ Starte Release-Prozess fÃ¼r Version $version..." -ForegroundColor Green

# Schritt 1: Add und Commit
try {
    Write-Host "ðŸ“ Committing changes for $tag..." -ForegroundColor Blue
    git add .
    
    if ([string]::IsNullOrWhiteSpace($Message)) {
        $commitMessage = "Prepare release $tag"
    } else {
        $commitMessage = $Message
    }
    
    git commit -m $commitMessage 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "â„¹ï¸  Nichts zu committen oder bereits committed" -ForegroundColor Blue
    } else {
        Write-Host "âœ… Changes committed" -ForegroundColor Green
    }
} catch {
    Write-Host "âš ï¸  Commit-Fehler: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Schritt 2: Push zum Master
try {
    Write-Host "ðŸ“¤ Pushe Ã„nderungen zum master branch..." -ForegroundColor Blue
    git push origin master
    if ($LASTEXITCODE -eq 0) {
        Write-Host "âœ… Master branch gepusht" -ForegroundColor Green
    } else {
        Write-Host "âš ï¸  Push-Warnung (mÃ¶glicherweise nichts zu pushen)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "âš ï¸  Push-Fehler: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Schritt 3: PrÃ¼fe ob Tag bereits existiert
try {
    $existingTag = git tag -l $tag 2>$null
    if ($existingTag) {
        Write-Host "âš ï¸  Tag $tag existiert bereits lokal!" -ForegroundColor Yellow
        
        if ($Force) {
            Write-Host "ðŸ”„ Force-Modus: LÃ¶sche bestehenden Tag..." -ForegroundColor Yellow
            git tag -d $tag 2>$null
            git push origin --delete $tag 2>$null
        } else {
            $deleteTag = Read-Host "Lokalen Tag lÃ¶schen und neu erstellen? (j/n)"
            if ($deleteTag -eq 'j') {
                Write-Host "ðŸ”„ LÃ¶sche lokalen Tag $tag..." -ForegroundColor Blue
                git tag -d $tag 2>$null
                Write-Host "ðŸ”„ LÃ¶sche Remote-Tag $tag..." -ForegroundColor Blue
                git push origin --delete $tag 2>$null
            } else {
                Write-Host "Abgebrochen." -ForegroundColor Red
                exit 1
            }
        }
    }
} catch {
    Write-Host "â„¹ï¸  Tag-PrÃ¼fung Ã¼bersprungen" -ForegroundColor Blue
}

# Schritt 4: Erstelle Tag
try {
    Write-Host "ðŸ·ï¸  Erstelle Git Tag $tag..." -ForegroundColor Blue
    git tag -a $tag -m "Release $tag - Einsatzueberwachung Professional v$version"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "âœ… Tag '$tag' erstellt!" -ForegroundColor Green
    } else {
        throw "Tag-Erstellung fehlgeschlagen"
    }
} catch {
    Write-Host "âŒ FEHLER: Tag konnte nicht erstellt werden!" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Schritt 5: Push Tag
try {
    Write-Host "ðŸ“¤ Pushe Tag zu GitHub..." -ForegroundColor Blue
    git push origin $tag
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "âœ… Tag erfolgreich gepusht!" -ForegroundColor Green
    } else {
        throw "Tag-Push fehlgeschlagen"
    }
} catch {
    Write-Host "âŒ FEHLER: Tag konnte nicht gepusht werden!" -ForegroundColor Red
    Write-Host "   Der Tag wurde lokal erstellt, aber nicht zu GitHub gepusht." -ForegroundColor Red
    Write-Host "   MÃ¶gliche Ursache: Repository Rules blockieren den Push." -ForegroundColor Red
    Write-Host ""
    Write-Host "ðŸ’¡ LÃ¶sungsvorschlÃ¤ge:" -ForegroundColor Yellow
    Write-Host "   1. Manuell pushen: git push origin $tag" -ForegroundColor Yellow
    Write-Host "   2. GitHub Web-Interface: https://github.com/Elemirus1996/Einsatzueberwachung/releases/new" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# Erfolg!
Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host " ðŸŽ‰ ERFOLG!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Der Tag '$tag' wurde erfolgreich zu GitHub gepusht." -ForegroundColor Green
Write-Host "Version: $version (aus Services\VersionService.cs)" -ForegroundColor White
Write-Host ""
Write-Host "ðŸ”„ GitHub Actions Workflow gestartet!" -ForegroundColor Cyan
Write-Host ""
Write-Host "ðŸ“Š Status prÃ¼fen:" -ForegroundColor Blue
Write-Host "   https://github.com/Elemirus1996/Einsatzueberwachung/actions" -ForegroundColor Blue
Write-Host ""
Write-Host "ðŸ“¦ Release wird verfÃ¼gbar sein unter:" -ForegroundColor Blue
Write-Host "   https://github.com/Elemirus1996/Einsatzueberwachung/releases/tag/$tag" -ForegroundColor Blue
Write-Host ""
Write-Host "ðŸŽ¯ Release-Prozess erfolgreich abgeschlossen!" -ForegroundColor Green
Write-Host ""

Read-Host "DrÃ¼cken Sie Enter zum Beenden"





