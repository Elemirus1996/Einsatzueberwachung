# Script zum Erstellen und Pushen eines Git-Tags fuer automatisches GitHub Release
# Das loest den GitHub Actions Workflow aus, der die Setup.exe erstellt

param(
    [string]$Version = "1.7.1",
    [switch]$Force
)

$tagName = "v$Version"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Git Tag Creator fuer GitHub Release Workflow" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Pruefe ob Git verfuegbar ist
try {
    $gitVersion = git --version
    Write-Host "OK Git gefunden: $gitVersion" -ForegroundColor Green
} catch {
    Write-Host "FEHLER Git ist nicht installiert oder nicht im PATH!" -ForegroundColor Red
    exit 1
}

# Pruefe aktuelle Branch
$currentBranch = git branch --show-current
Write-Host "OK Aktueller Branch: $currentBranch" -ForegroundColor Green

# Pruefe ob es uncommitted changes gibt
$status = git status --porcelain
if ($status) {
    Write-Host "" -ForegroundColor Yellow
    Write-Host "WARNUNG: Es gibt nicht commitete Aenderungen:" -ForegroundColor Yellow
    Write-Host $status -ForegroundColor Yellow
    Write-Host ""
    $continue = Read-Host "Trotzdem fortfahren? (j/n)"
    if ($continue -ne "j") {
        Write-Host "Abgebrochen." -ForegroundColor Red
        exit 1
    }
}

# Pruefe ob Tag bereits existiert
$existingTag = git tag -l $tagName
if ($existingTag -and -not $Force) {
    Write-Host "" -ForegroundColor Yellow
    Write-Host "FEHLER Tag '$tagName' existiert bereits!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Optionen:" -ForegroundColor Cyan
    Write-Host "1. Verwenden Sie -Force um den Tag zu ueberschreiben" -ForegroundColor White
    Write-Host "2. Oder loeschen Sie den Tag manuell:" -ForegroundColor White
    Write-Host "   git tag -d $tagName" -ForegroundColor Gray
    Write-Host "   git push origin --delete $tagName" -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Tag-Informationen:" -ForegroundColor Cyan
Write-Host "  Version: $Version" -ForegroundColor White
Write-Host "  Tag:     $tagName" -ForegroundColor White
Write-Host "  Branch:  $currentBranch" -ForegroundColor White
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Bestaetigung
$confirm = Read-Host "Git Tag '$tagName' erstellen und zu GitHub pushen? (j/n)"
if ($confirm -ne "j") {
    Write-Host "Abgebrochen." -ForegroundColor Red
    exit 0
}

Write-Host ""
Write-Host "Erstelle Git Tag..." -ForegroundColor Cyan

# Loesche alten Tag wenn Force
if ($Force -and $existingTag) {
    Write-Host "  -> Loesche lokalen Tag '$tagName'..." -ForegroundColor Yellow
    git tag -d $tagName
    
    Write-Host "  -> Loesche Remote Tag '$tagName'..." -ForegroundColor Yellow
    git push origin --delete $tagName 2>$null
}

# Erstelle Tag mit Nachricht
$tagMessage = "Release v$Version - Einsatzueberwachung Professional"
Write-Host "  -> Erstelle Tag '$tagName'..." -ForegroundColor Green
git tag -a $tagName -m $tagMessage

if ($LASTEXITCODE -ne 0) {
    Write-Host "FEHLER beim Erstellen des Tags!" -ForegroundColor Red
    exit 1
}

Write-Host "OK Tag '$tagName' erfolgreich erstellt" -ForegroundColor Green

# Push Tag zu GitHub
Write-Host ""
Write-Host "Pushe Tag zu GitHub..." -ForegroundColor Cyan
git push origin $tagName

if ($LASTEXITCODE -ne 0) {
    Write-Host "FEHLER beim Pushen des Tags!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Lokaler Tag wurde erstellt. Sie koennen manuell pushen mit:" -ForegroundColor Yellow
    Write-Host "  git push origin $tagName" -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "ERFOLG!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Der Tag '$tagName' wurde zu GitHub gepusht." -ForegroundColor White
Write-Host ""
Write-Host "Der GitHub Actions Workflow wurde automatisch gestartet und erstellt jetzt:" -ForegroundColor Cyan
Write-Host "  - Die .NET Anwendung (Build)" -ForegroundColor White
Write-Host "  - Die Setup.exe mit Inno Setup" -ForegroundColor White
Write-Host "  - Das GitHub Release mit allen Dateien" -ForegroundColor White
Write-Host ""
Write-Host "Status pruefen:" -ForegroundColor Cyan
Write-Host "  https://github.com/Elemirus1996/Einsatzueberwachung/actions" -ForegroundColor Blue
Write-Host ""
Write-Host "Nach Abschluss verfuegbar unter:" -ForegroundColor Cyan
Write-Host "  https://github.com/Elemirus1996/Einsatzueberwachung/releases/tag/$tagName" -ForegroundColor Blue
Write-Host ""
