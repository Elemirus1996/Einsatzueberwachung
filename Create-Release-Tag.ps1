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

# Prüfe ob VersionService.cs existiert
if (-not (Test-Path "Services\VersionService.cs")) {
    Write-Host "❌ FEHLER: Services\VersionService.cs nicht gefunden!" -ForegroundColor Red
    Write-Host "   Bitte führen Sie das Script aus dem Hauptverzeichnis aus." -ForegroundColor Red
    Read-Host "Drücken Sie Enter zum Beenden"
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
    
    # Prüfe Development-Flag
    $isDevelopment = $versionServiceContent -match 'private const bool IS_DEVELOPMENT_VERSION = true'
    
    $version = "$major.$minor.$patch"
    $tag = "v$version"
    
    Write-Host "================================================" -ForegroundColor Green
    Write-Host " AUTOMATISCH ERKANNTE VERSION" -ForegroundColor Green
    Write-Host "================================================" -ForegroundColor Green
    Write-Host "Version: $version" -ForegroundColor White
    Write-Host "Tag:     $tag" -ForegroundColor White
    Write-Host "Quelle:  Services\VersionService.cs" -ForegroundColor Gray
    
    if ($isDevelopment) {
        Write-Host "Typ:     🚧 DEVELOPMENT VERSION" -ForegroundColor Yellow
    } else {
        Write-Host "Typ:     ✅ RELEASE VERSION" -ForegroundColor Green
    }
    Write-Host ""
    
} catch {
    Write-Host "❌ FEHLER: Konnte Version nicht aus VersionService.cs extrahieren!" -ForegroundColor Red
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
    Write-Host "⚠️  WARNUNG: Dies ist eine DEVELOPMENT-Version!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Vor dem Release sollten Sie:" -ForegroundColor Yellow
    Write-Host "1. IS_DEVELOPMENT_VERSION = false setzen in Services\VersionService.cs" -ForegroundColor Yellow
    Write-Host "2. Einsatzueberwachung.csproj Versionen aktualisieren" -ForegroundColor Yellow
    Write-Host "3. Build und Test durchführen" -ForegroundColor Yellow
    Write-Host ""
    
    $continue = Read-Host "Trotzdem als Development-Release fortfahren? (j/n)"
    if ($continue -ne 'j') {
        Write-Host "Abgebrochen. Bitte VersionService.cs für Release konfigurieren." -ForegroundColor Red
        exit 1
    }
    Write-Host ""
    Write-Host "🚧 Erstelle Development-Release-Tag..." -ForegroundColor Yellow
} elseif (-not $isDevelopment) {
    Write-Host "✅ Release-Version erkannt" -ForegroundColor Green
}

# Prüfe Git Status
try {
    $gitStatus = git status --porcelain 2>$null
    if ($gitStatus) {
        Write-Host "ℹ️  Uncommitted changes gefunden:" -ForegroundColor Blue
        git status --short
        Write-Host ""
    }
} catch {
    Write-Host "⚠️  Git-Status konnte nicht geprüft werden" -ForegroundColor Yellow
}

# Zeige geplante Aktionen
Write-Host "🎯 GEPLANTE AKTIONEN:" -ForegroundColor Cyan
Write-Host "1. git add ." -ForegroundColor Gray
Write-Host "2. git commit -m `"Prepare release $tag`"" -ForegroundColor Gray
Write-Host "3. git push origin master" -ForegroundColor Gray
Write-Host "4. git tag -a $tag -m `"Release $tag - Einsatzueberwachung Professional v$version`"" -ForegroundColor Gray
Write-Host "5. git push origin $tag" -ForegroundColor Gray
Write-Host ""

Write-Host "Der GitHub Actions Workflow erstellt dann automatisch:" -ForegroundColor Cyan
Write-Host "• Build der .NET Anwendung" -ForegroundColor Gray
Write-Host "• Setup.exe mit Inno Setup" -ForegroundColor Gray
Write-Host "• GitHub Release mit allen Dateien" -ForegroundColor Gray
Write-Host ""

if ($DryRun) {
    Write-Host "🔍 DRY RUN - Keine Änderungen werden vorgenommen" -ForegroundColor Blue
    exit 0
}

if (-not $Force) {
    $confirm = Read-Host "🚀 Fortfahren? (j/n)"
    if ($confirm -ne 'j') {
        Write-Host "Abgebrochen." -ForegroundColor Red
        exit 0
    }
}

Write-Host ""
Write-Host "🚀 Starte Release-Prozess für Version $version..." -ForegroundColor Green

# Schritt 1: Add und Commit
try {
    Write-Host "📝 Committing changes for $tag..." -ForegroundColor Blue
    git add .
    
    if ([string]::IsNullOrWhiteSpace($Message)) {
        $commitMessage = "Prepare release $tag"
    } else {
        $commitMessage = $Message
    }
    
    git commit -m $commitMessage 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ℹ️  Nichts zu committen oder bereits committed" -ForegroundColor Blue
    } else {
        Write-Host "✅ Changes committed" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️  Commit-Fehler: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Schritt 2: Push zum Master
try {
    Write-Host "📤 Pushe Änderungen zum master branch..." -ForegroundColor Blue
    git push origin master
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Master branch gepusht" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Push-Warnung (möglicherweise nichts zu pushen)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️  Push-Fehler: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Schritt 3: Prüfe ob Tag bereits existiert
try {
    $existingTag = git tag -l $tag 2>$null
    if ($existingTag) {
        Write-Host "⚠️  Tag $tag existiert bereits lokal!" -ForegroundColor Yellow
        
        if ($Force) {
            Write-Host "🔄 Force-Modus: Lösche bestehenden Tag..." -ForegroundColor Yellow
            git tag -d $tag 2>$null
            git push origin --delete $tag 2>$null
        } else {
            $deleteTag = Read-Host "Lokalen Tag löschen und neu erstellen? (j/n)"
            if ($deleteTag -eq 'j') {
                Write-Host "🔄 Lösche lokalen Tag $tag..." -ForegroundColor Blue
                git tag -d $tag 2>$null
                Write-Host "🔄 Lösche Remote-Tag $tag..." -ForegroundColor Blue
                git push origin --delete $tag 2>$null
            } else {
                Write-Host "Abgebrochen." -ForegroundColor Red
                exit 1
            }
        }
    }
} catch {
    Write-Host "ℹ️  Tag-Prüfung übersprungen" -ForegroundColor Blue
}

# Schritt 4: Erstelle Tag
try {
    Write-Host "🏷️  Erstelle Git Tag $tag..." -ForegroundColor Blue
    git tag -a $tag -m "Release $tag - Einsatzueberwachung Professional v$version"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Tag '$tag' erstellt!" -ForegroundColor Green
    } else {
        throw "Tag-Erstellung fehlgeschlagen"
    }
} catch {
    Write-Host "❌ FEHLER: Tag konnte nicht erstellt werden!" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Schritt 5: Push Tag
try {
    Write-Host "📤 Pushe Tag zu GitHub..." -ForegroundColor Blue
    git push origin $tag
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Tag erfolgreich gepusht!" -ForegroundColor Green
    } else {
        throw "Tag-Push fehlgeschlagen"
    }
} catch {
    Write-Host "❌ FEHLER: Tag konnte nicht gepusht werden!" -ForegroundColor Red
    Write-Host "   Der Tag wurde lokal erstellt, aber nicht zu GitHub gepusht." -ForegroundColor Red
    Write-Host "   Mögliche Ursache: Repository Rules blockieren den Push." -ForegroundColor Red
    Write-Host ""
    Write-Host "💡 Lösungsvorschläge:" -ForegroundColor Yellow
    Write-Host "   1. Manuell pushen: git push origin $tag" -ForegroundColor Yellow
    Write-Host "   2. GitHub Web-Interface: https://github.com/Elemirus1996/Einsatzueberwachung/releases/new" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# Erfolg!
Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host " 🎉 ERFOLG!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Der Tag '$tag' wurde erfolgreich zu GitHub gepusht." -ForegroundColor Green
Write-Host "Version: $version (aus Services\VersionService.cs)" -ForegroundColor White
Write-Host ""
Write-Host "🔄 GitHub Actions Workflow gestartet!" -ForegroundColor Cyan
Write-Host ""
Write-Host "📊 Status prüfen:" -ForegroundColor Blue
Write-Host "   https://github.com/Elemirus1996/Einsatzueberwachung/actions" -ForegroundColor Blue
Write-Host ""
Write-Host "📦 Release wird verfügbar sein unter:" -ForegroundColor Blue
Write-Host "   https://github.com/Elemirus1996/Einsatzueberwachung/releases/tag/$tag" -ForegroundColor Blue
Write-Host ""
Write-Host "🎯 Release-Prozess erfolgreich abgeschlossen!" -ForegroundColor Green
Write-Host ""

Read-Host "Drücken Sie Enter zum Beenden"
