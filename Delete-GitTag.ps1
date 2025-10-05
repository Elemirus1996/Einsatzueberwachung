# Script zum Loeschen eines Git-Tags (lokal und remote)

param(
    [string]$TagName = "v1.7.1"
)

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Git Tag Loeschen" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Pruefe vorhandene Tags
Write-Host "Pruefe vorhandene Tags..." -ForegroundColor Cyan
$allTags = git tag -l
Write-Host "Lokale Tags:" -ForegroundColor Yellow
if ($allTags) {
    $allTags | ForEach-Object { Write-Host "  - $_" -ForegroundColor White }
} else {
    Write-Host "  (keine lokalen Tags gefunden)" -ForegroundColor Gray
}
Write-Host ""

# Pruefe ob Tag lokal existiert
$localTagExists = git tag -l $TagName
if ($localTagExists) {
    Write-Host "Loesche lokalen Tag '$TagName'..." -ForegroundColor Yellow
    git tag -d $TagName
    if ($LASTEXITCODE -eq 0) {
        Write-Host "OK Lokaler Tag '$TagName' geloescht" -ForegroundColor Green
    } else {
        Write-Host "FEHLER beim Loeschen des lokalen Tags" -ForegroundColor Red
    }
} else {
    Write-Host "INFO Lokaler Tag '$TagName' existiert nicht" -ForegroundColor Gray
}

Write-Host ""

# Loesche Remote Tag
Write-Host "Loesche Remote Tag '$TagName' auf GitHub..." -ForegroundColor Yellow
git push origin --delete $TagName 2>&1 | Out-String | Write-Host

if ($LASTEXITCODE -eq 0) {
    Write-Host "OK Remote Tag '$TagName' geloescht" -ForegroundColor Green
} else {
    Write-Host "INFO Remote Tag existiert moeglicherweise nicht oder wurde bereits geloescht" -ForegroundColor Gray
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "Fertig!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Der Tag '$TagName' wurde geloescht (falls vorhanden)" -ForegroundColor White
Write-Host ""
