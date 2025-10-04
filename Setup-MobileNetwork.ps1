# Einsatzüberwachung Mobile - Netzwerk-Konfiguration
# Führen Sie dieses Script als Administrator aus

Write-Host "🔧 Einsatzüberwachung Mobile - Netzwerk-Setup" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

# Prüfe Administrator-Rechte
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
{
    Write-Host "❌ Fehler: Administrator-Rechte erforderlich!" -ForegroundColor Red
    Write-Host "💡 Rechtsklick auf PowerShell → 'Als Administrator ausführen'" -ForegroundColor Yellow
    Read-Host "Drücken Sie Enter zum Beenden"
    exit 1
}

Write-Host "✅ Administrator-Rechte erkannt" -ForegroundColor Green

# Erkenne lokale IP-Adresse
$localIP = Get-NetIPAddress -AddressFamily IPv4 | Where-Object {
    $_.IPAddress -match "^192\.168\." -or 
    $_.IPAddress -match "^10\." -or 
    ($_.IPAddress -match "^172\." -and [int]($_.IPAddress.Split('.')[1]) -ge 16 -and [int]($_.IPAddress.Split('.')[1]) -le 31)
} | Select-Object -First 1 -ExpandProperty IPAddress

if (-not $localIP) {
    Write-Host "⚠️ Keine lokale IP-Adresse gefunden, verwende alle Interfaces (*)" -ForegroundColor Yellow
    $localIP = "*"
}
else {
    Write-Host "🌐 Erkannte lokale IP: $localIP" -ForegroundColor Green
}

$port = 8080

Write-Host "`n🔧 Konfiguriere Windows für Mobile-Zugriff..." -ForegroundColor Cyan

# 1. HTTP URL Reservation hinzufügen
Write-Host "`n1️⃣ Füge HTTP URL Reservation hinzu..." -ForegroundColor Yellow
$urlReservation = "http://+:$port/"
try {
    netsh http add urlacl url=$urlReservation user=Everyone
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ URL Reservation erfolgreich hinzugefügt: $urlReservation" -ForegroundColor Green
    } else {
        Write-Host "⚠️ URL Reservation bereits vorhanden oder Fehler beim Hinzufügen" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Fehler bei URL Reservation: $_" -ForegroundColor Red
}

# 2. Firewall-Regel hinzufügen
Write-Host "`n2️⃣ Füge Windows Firewall-Regel hinzu..." -ForegroundColor Yellow
$ruleName = "Einsatzüberwachung Mobile"
try {
    # Prüfe ob Regel bereits existiert
    $existingRule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
    if ($existingRule) {
        Write-Host "⚠️ Firewall-Regel bereits vorhanden: $ruleName" -ForegroundColor Yellow
    } else {
        New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP -LocalPort $port -Action Allow
        Write-Host "✅ Firewall-Regel erfolgreich hinzugefügt: $ruleName (Port $port)" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Fehler bei Firewall-Regel: $_" -ForegroundColor Red
    # Fallback mit netsh
    Write-Host "🔄 Versuche Fallback mit netsh..." -ForegroundColor Yellow
    netsh advfirewall firewall add rule name=$ruleName dir=in action=allow protocol=TCP localport=$port
}

# 3. Netzwerk-Profil prüfen
Write-Host "`n3️⃣ Prüfe Netzwerk-Profil..." -ForegroundColor Yellow
$networkProfile = Get-NetConnectionProfile | Where-Object { $_.NetworkCategory -eq "Public" }
if ($networkProfile) {
    Write-Host "⚠️ Öffentliches Netzwerk erkannt. Für lokalen Zugriff Private Netzwerk empfohlen." -ForegroundColor Yellow
    Write-Host "💡 Tipp: Windows-Einstellungen → Netzwerk → Eigenschaften → Privat auswählen" -ForegroundColor Cyan
} else {
    Write-Host "✅ Privates Netzwerk erkannt - optimal für lokalen Zugriff" -ForegroundColor Green
}

# 4. Port-Test
Write-Host "`n4️⃣ Teste Port-Verfügbarkeit..." -ForegroundColor Yellow
$portTest = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
if ($portTest) {
    Write-Host "⚠️ Port $port wird bereits verwendet von:" -ForegroundColor Yellow
    $portTest | ForEach-Object {
        $processId = $_.OwningProcess
        $processName = (Get-Process -Id $processId -ErrorAction SilentlyContinue).ProcessName
        Write-Host "   - PID $processId ($processName)" -ForegroundColor Yellow
    }
} else {
    Write-Host "✅ Port $port ist verfügbar" -ForegroundColor Green
}

# Zusammenfassung
Write-Host "`n📋 Konfiguration abgeschlossen!" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "✅ HTTP URL Reservation: http://+:$port/" -ForegroundColor Green
Write-Host "✅ Firewall-Regel: $ruleName (Port $port)" -ForegroundColor Green
if ($localIP -ne "*") {
    Write-Host "🌐 Lokale IP-Adresse: $localIP" -ForegroundColor Green
    Write-Host "📱 iPhone URL: http://$localIP`:$port/mobile" -ForegroundColor Cyan
}
Write-Host "💻 Desktop URL: http://localhost:$port/mobile" -ForegroundColor Cyan

Write-Host "`n🚀 Nächste Schritte:" -ForegroundColor Yellow
Write-Host "1. Starten Sie die Einsatzüberwachung-App" -ForegroundColor White
Write-Host "2. Öffnen Sie 'Mobile Verbindung'" -ForegroundColor White
Write-Host "3. Klicken Sie 'Server starten'" -ForegroundColor White
Write-Host "4. Scannen Sie den QR-Code mit Ihrem iPhone" -ForegroundColor White

Write-Host "`n💡 Bei Problemen:" -ForegroundColor Yellow
Write-Host "- Prüfen Sie die App-Logs im Mobile Connection Window" -ForegroundColor White
Write-Host "- Testen Sie zuerst: http://localhost:$port/debug" -ForegroundColor White
Write-Host "- Stellen Sie sicher, dass beide Geräte im gleichen WLAN sind" -ForegroundColor White

Read-Host "`nDrücken Sie Enter zum Beenden"
