# Build-Setup.ps1
# Dieses Skript automatisiert die Erstellung der setup.exe für die Einsatzüberwachung Professional.
# Es führt die folgenden Schritte aus:
# 1. Baut die .NET-Anwendung im Release-Modus.
# 2. Kompiliert das Inno Setup-Skript, um die setup.exe zu erstellen.

# --- Konfiguration ---
$ProjectFile = "Einsatzueberwachung.csproj"
$InnoSetupScript = "Setup\Einsatzueberwachung_Setup.iss"
# Standard-Installationspfad für Inno Setup. Passen Sie diesen Pfad an, falls Inno Setup an einem anderen Ort installiert ist.
$InnoSetupCompiler = "C:\Program Files (x86)\Inno Setup 6\iscc.exe"

# --- Skriptablauf ---

# 1. Überprüfen, ob der Inno Setup Compiler existiert
if (-not (Test-Path $InnoSetupCompiler)) {
    Write-Host "Fehler: Inno Setup Compiler (iscc.exe) nicht gefunden unter: $InnoSetupCompiler"
    Write-Host "Bitte installieren Sie Inno Setup 6 (https://jrsoftware.org/isinfo.php) oder passen Sie den Pfad im Skript an."
    exit 1
}

# 2. .NET-Anwendung bauen
Write-Host "Baue die .NET-Anwendung im Release-Modus..."
dotnet build $ProjectFile -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Fehler beim Bauen der .NET-Anwendung. Skript wird abgebrochen."
    exit 1
}
Write-Host ".NET-Anwendung erfolgreich gebaut."

# 3. Inno Setup-Skript kompilieren
Write-Host "Kompiliere das Inno Setup-Skript, um die setup.exe zu erstellen..."
& $InnoSetupCompiler $InnoSetupScript
if ($LASTEXITCODE -ne 0) {
    Write-Host "Fehler beim Kompilieren des Inno Setup-Skripts. Skript wird abgebrochen."
    exit 1
}

Write-Host "Setup erfolgreich erstellt!"
Write-Host "Die erstellte setup.exe befindet sich im 'Setup\Output' Verzeichnis."
