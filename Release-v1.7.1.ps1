# Quick Release v1.7.1 - Automatisches GitHub Release
# Fuehrt alle Schritte aus: Build -> Setup -> Git Commit -> Push -> Tag

Write-Host ""
Write-Host "EINSATZUEBERWACHUNG PROFESSIONAL v1.7.1 - QUICK RELEASE" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"
$version = "1.7.1"
$tagName = "v$version"

try {
    # 1. Build pruefen/erstellen
    Write-Host "1/6 - Build pruefen..." -ForegroundColor Yellow
    
    if (-not (Test-Path "bin\Release\net8.0-windows\Einsatzueberwachung.exe")) {
        Write-Host "   Build wird erstellt..." -ForegroundColor Blue
        dotnet build "Einsatzüberwachung.csproj" --configuration Release
        Write-Host "   Build erfolgreich" -ForegroundColor Green
    } else {
        Write-Host "   Build bereits vorhanden" -ForegroundColor Green
    }
    
    # 2. Setup erstellen (falls Inno Setup verfuegbar)
    Write-Host ""
    Write-Host "2/6 - Setup.exe erstellen..." -ForegroundColor Yellow
    
    $innoCompiler = "Y:\Inno Setup 6\ISCC.exe"
    if (Test-Path $innoCompiler) {
        Write-Host "   Kompiliere Setup mit Inno Setup..." -ForegroundColor Blue
        & $innoCompiler "Setup\Einsatzueberwachung_Setup_Simple.iss" | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            $setupFile = Get-Item "Setup\Output\Einsatzueberwachung_Professional_v${version}_Setup.exe"
            $setupSize = [math]::Round($setupFile.Length / 1MB, 2)
            Write-Host "   Setup.exe erstellt: $setupSize MB" -ForegroundColor Green
        } else {
            Write-Host "   Setup-Kompilierung fehlgeschlagen" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   Inno Setup nicht gefunden - wird von GitHub Actions erstellt" -ForegroundColor Yellow
    }
    
    # 3. Git Status
    Write-Host ""
    Write-Host "3/6 - Git Status pruefen..." -ForegroundColor Yellow
    
    git status --porcelain 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   Git Repository nicht verfuegbar!" -ForegroundColor Red
        exit 1
    }
    Write-Host "   Git Repository OK" -ForegroundColor Green
    
    # 4. Git Commit
    Write-Host ""
    Write-Host "4/6 - Git Commit erstellen..." -ForegroundColor Yellow
    
    git add .
    
    $commitMessage = "Release v$version - Dashboard & Stammdatenverwaltung

Hauptfeatures v1.7.1:
- Dashboard-Uebersicht mit modernen Team-Cards
- Erweiterter PDF-Export mit professionellen Templates
- Individuelle Team-Warnschwellen pro Team
- Verbessertes Notizen-System im Hauptfenster
- Stammdatenverwaltung fuer Personal und Hunde

Technische Verbesserungen:
- Performance-Optimierungen fuer grosse Team-Listen
- Responsive Layout fuer alle Bildschirmgroessen
- Memory-Management verbessert
- Threading-Issues behoben
- Version-Update von 1.6.0 zu 1.7.1 behoben"
    
    git commit -m $commitMessage
    Write-Host "   Commit erstellt" -ForegroundColor Green
    
    # 5. Git Tag erstellen
    Write-Host ""
    Write-Host "5/6 - Git Tag erstellen..." -ForegroundColor Yellow
    
    $tagMessage = "Einsatzueberwachung Professional v$version - Dashboard & Stammdatenverwaltung Edition"
    
    # Loesche existierenden Tag falls vorhanden
    git tag -d $tagName 2>$null | Out-Null
    git push origin --delete $tagName 2>$null | Out-Null
    
    git tag -a $tagName -m $tagMessage
    Write-Host "   Tag $tagName erstellt" -ForegroundColor Green
    
    # 6. Push zu GitHub
    Write-Host ""
    Write-Host "6/6 - Push zu GitHub..." -ForegroundColor Yellow
    Write-Host "   Pushe Branch und Tag..." -ForegroundColor Blue
    
    git push origin 1.7.1
    git push origin $tagName
    
    Write-Host "   Push erfolgreich!" -ForegroundColor Green
    
    # Erfolgsmeldung
    Write-Host ""
    Write-Host "RELEASE v$version ERFOLGREICH VORBEREITET!" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "WAS PASSIERT JETZT:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. GitHub Actions wird automatisch:" -ForegroundColor Yellow
    Write-Host "   - .NET 8 Build erstellen" -ForegroundColor White
    Write-Host "   - Setup.exe kompilieren" -ForegroundColor White
    Write-Host "   - GitHub Release veroeffentlichen" -ForegroundColor White
    Write-Host ""
    
    Write-Host "2. Nach ca. 5-10 Minuten:" -ForegroundColor Yellow
    Write-Host "   - Release verfuegbar unter:" -ForegroundColor White
    Write-Host "     https://github.com/Elemirus1996/Einsatzueberwachung/releases/tag/$tagName" -ForegroundColor Blue
    Write-Host ""
    
    Write-Host "3. Benutzer mit v1.6.x/v1.7.0 erhalten:" -ForegroundColor Yellow
    Write-Host "   - Automatische Update-Benachrichtigung" -ForegroundColor White
    Write-Host "   - Ein-Klick-Update aus der Anwendung" -ForegroundColor White
    Write-Host ""
    
    $openGitHub = Read-Host "Moechten Sie GitHub Actions jetzt oeffnen? (j/n)"
    if ($openGitHub -eq 'j' -or $openGitHub -eq 'J') {
        Start-Process "https://github.com/Elemirus1996/Einsatzueberwachung/actions"
    }
    
    $openReleases = Read-Host "Moechten Sie GitHub Releases oeffnen? (j/n)"
    if ($openReleases -eq 'j' -or $openReleases -eq 'J') {
        Start-Process "https://github.com/Elemirus1996/Einsatzueberwachung/releases"
    }
    
} catch {
    Write-Host ""
    Write-Host "FEHLER BEIM RELEASE-PROZESS!" -ForegroundColor Red
    Write-Host "Fehler: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "LOESUNGSVORSCHLAEGE:" -ForegroundColor Yellow
    Write-Host "1. Git konfigurieren:" -ForegroundColor White
    Write-Host "   git config user.name 'Ihr Name'" -ForegroundColor Gray
    Write-Host "   git config user.email 'ihre.email@example.com'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Remote Repository pruefen:" -ForegroundColor White
    Write-Host "   git remote -v" -ForegroundColor Gray
    Write-Host ""
    exit 1
}

Write-Host ""
Write-Host "Quick Release Script abgeschlossen!" -ForegroundColor Green
Write-Host ""
