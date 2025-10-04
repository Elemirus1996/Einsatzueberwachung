# Einsatzüberwachung Mobile - Automatische Problem-Behebung
# PowerShell Script für Server-Start-Probleme
# Version 1.6 - Erweiterte Diagnose und Reparatur

param(
    [switch]$DiagnoseOnly = $false,
    [switch]$Force = $false,
    [int]$Port = 8080
)

Write-Host "🔧 EINSATZÜBERWACHUNG MOBILE - AUTOMATISCHE REPARATUR v1.6" -ForegroundColor Cyan
Write-Host "=============================================================" -ForegroundColor Cyan
Write-Host ""

# Prüfe Administrator-Rechte
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")

if (-not $isAdmin) {
    Write-Host "❌ ADMINISTRATOR-RECHTE ERFORDERLICH" -ForegroundColor Red
    Write-Host ""
    Write-Host "💡 LÖSUNG:" -ForegroundColor Yellow
    Write-Host "1. PowerShell als Administrator öffnen:" -ForegroundColor Yellow
    Write-Host "   • Windows-Taste + X" -ForegroundColor Yellow
    Write-Host "   • 'Windows PowerShell (Administrator)' wählen" -ForegroundColor Yellow
    Write-Host "   • UAC-Dialog mit 'Ja' bestätigen" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "2. Script erneut ausführen:" -ForegroundColor Yellow
    Write-Host "   .\Fix-MobileServer.ps1" -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Drücken Sie Enter zum Beenden"
    exit 1
}

Write-Host "✅ Administrator-Rechte verfügbar" -ForegroundColor Green
Write-Host ""

# Funktion: Port-Check und Reparatur
function Test-AndFixPort {
    param([int]$PortNumber)
    
    Write-Host "🔌 PRÜFE PORT $PortNumber..." -ForegroundColor Yellow
    
    try {
        $connections = Get-NetTCPConnection -LocalPort $PortNumber -ErrorAction SilentlyContinue
        
        if ($connections) {
            Write-Host "⚠️  Port $PortNumber wird verwendet von:" -ForegroundColor Red
            
            foreach ($conn in $connections) {
                try {
                    $process = Get-Process -Id $conn.OwningProcess -ErrorAction SilentlyContinue
                    if ($process) {
                        Write-Host "   • $($process.ProcessName) (PID: $($process.Id))" -ForegroundColor Red
                        
                        # Wenn es eine alte Einsatzüberwachung-Instanz ist, beenden
                        if ($process.ProcessName -like "*Einsatz*" -or $process.ProcessName -like "*Mobile*") {
                            if ($Force -or (Read-Host "   Soll dieser Prozess beendet werden? (j/n)") -eq 'j') {
                                Write-Host "   🔄 Beende Prozess..." -ForegroundColor Yellow
                                Stop-Process -Id $process.Id -Force
                                Start-Sleep -Seconds 2
                                Write-Host "   ✅ Prozess beendet" -ForegroundColor Green
                            }
                        }
                    }
                } catch {
                    Write-Host "   • Unbekannter Prozess (PID: $($conn.OwningProcess))" -ForegroundColor Red
                }
            }
        } else {
            Write-Host "✅ Port $PortNumber ist verfügbar" -ForegroundColor Green
        }
    } catch {
        Write-Host "⚠️  Port-Check Fehler: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host ""
}

# Funktion: URL-Reservierung reparieren
function Repair-UrlReservation {
    param([int]$PortNumber)
    
    Write-Host "🔗 REPARIERE URL-RESERVIERUNG..." -ForegroundColor Yellow
    
    # Prüfe bestehende Reservierungen
    $existing = netsh http show urlacl | Select-String ":$PortNumber/"
    
    if ($existing) {
        Write-Host "ℹ️  Bestehende Reservierung gefunden:" -ForegroundColor Blue
        Write-Host "$existing" -ForegroundColor Blue
        
        if ($Force -or (Read-Host "Soll die bestehende Reservierung erneuert werden? (j/n)") -eq 'j') {
            Write-Host "🔄 Entferne alte Reservierung..." -ForegroundColor Yellow
            netsh http delete urlacl url="http://+:$PortNumber/" | Out-Null
        }
    }
    
    # Neue Reservierung hinzufügen
    Write-Host "🔄 Füge neue URL-Reservierung hinzu..." -ForegroundColor Yellow
    $result = netsh http add urlacl url="http://+:$PortNumber/" user=Everyone
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ URL-Reservierung erfolgreich hinzugefügt" -ForegroundColor Green
    } else {
        Write-Host "❌ URL-Reservierung fehlgeschlagen:" -ForegroundColor Red
        Write-Host "$result" -ForegroundColor Red
    }
    
    Write-Host ""
}

# Funktion: Firewall-Regel reparieren
function Repair-FirewallRule {
    param([int]$PortNumber)
    
    Write-Host "🛡️  REPARIERE FIREWALL-REGEL..." -ForegroundColor Yellow
    
    $ruleName = "Einsatzueberwachung_Mobile"
    
    # Prüfe bestehende Regel
    $existing = netsh advfirewall firewall show rule name="$ruleName" 2>$null
    
    if ($existing -match "No rules match") {
        Write-Host "ℹ️  Keine bestehende Firewall-Regel gefunden" -ForegroundColor Blue
    } else {
        Write-Host "ℹ️  Bestehende Firewall-Regel gefunden" -ForegroundColor Blue
        
        if ($Force -or (Read-Host "Soll die Regel erneuert werden? (j/n)") -eq 'j') {
            Write-Host "🔄 Entferne alte Regel..." -ForegroundColor Yellow
            netsh advfirewall firewall delete rule name="$ruleName" | Out-Null
        }
    }
    
    # Neue Regel hinzufügen
    Write-Host "🔄 Füge neue Firewall-Regel hinzu..." -ForegroundColor Yellow
    $result = netsh advfirewall firewall add rule name="$ruleName" dir=in action=allow protocol=TCP localport=$PortNumber
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Firewall-Regel erfolgreich hinzugefügt" -ForegroundColor Green
    } else {
        Write-Host "❌ Firewall-Regel fehlgeschlagen:" -ForegroundColor Red
        Write-Host "$result" -ForegroundColor Red
    }
    
    Write-Host ""
}

# Funktion: System-Diagnose
function Invoke-SystemDiagnosis {
    Write-Host "🔍 SYSTEM-DIAGNOSE..." -ForegroundColor Yellow
    Write-Host ""
    
    # Windows-Version
    $osInfo = Get-WmiObject -Class Win32_OperatingSystem
    Write-Host "🖥️  Windows: $($osInfo.Caption) (Build $($osInfo.BuildNumber))" -ForegroundColor Blue
    
    # .NET Versionen
    Write-Host "💻 .NET Versionen:" -ForegroundColor Blue
    try {
        $dotnetVersions = dotnet --list-runtimes 2>$null | Where-Object { $_ -like "*Microsoft.WindowsDesktop.App*" }
        if ($dotnetVersions) {
            foreach ($version in $dotnetVersions) {
                Write-Host "   • $version" -ForegroundColor Blue
            }
        } else {
            Write-Host "   ⚠️  .NET Desktop Runtime nicht gefunden" -ForegroundColor Red
        }
    } catch {
        Write-Host "   ⚠️  .NET-Informationen nicht verfügbar" -ForegroundColor Red
    }
    
    # Netzwerk-Interfaces
    Write-Host "🌐 Aktive Netzwerk-Interfaces:" -ForegroundColor Blue
    $networkAdapters = Get-NetAdapter | Where-Object { $_.Status -eq "Up" }
    foreach ($adapter in $networkAdapters) {
        $ipConfig = Get-NetIPAddress -InterfaceAlias $adapter.Name -AddressFamily IPv4 -ErrorAction SilentlyContinue
        if ($ipConfig) {
            Write-Host "   • $($adapter.Name): $($ipConfig.IPAddress)" -ForegroundColor Blue
        }
    }
    
    # Firewall-Status
    Write-Host "🛡️  Firewall-Status:" -ForegroundColor Blue
    $firewallProfiles = netsh advfirewall show allprofiles state
    if ($firewallProfiles -match "State\s+OFF") {
        Write-Host "   ✅ Firewall ist deaktiviert (keine Blockierung)" -ForegroundColor Green
    } elseif ($firewallProfiles -match "State\s+ON") {
        Write-Host "   ⚠️  Firewall ist aktiv (Port-Freigabe erforderlich)" -ForegroundColor Yellow
    } else {
        Write-Host "   ❓ Firewall-Status unbekannt" -ForegroundColor Red
    }
    
    Write-Host ""
}

# Funktion: Vollständige Reparatur
function Invoke-FullRepair {
    param([int]$PortNumber)
    
    Write-Host "🔧 STARTE VOLLSTÄNDIGE REPARATUR..." -ForegroundColor Cyan
    Write-Host ""
    
    # 1. System-Diagnose
    Invoke-SystemDiagnosis
    
    # 2. Port prüfen und reparieren
    Test-AndFixPort -PortNumber $PortNumber
    
    # 3. URL-Reservierung reparieren
    Repair-UrlReservation -PortNumber $PortNumber
    
    # 4. Firewall-Regel reparieren
    Repair-FirewallRule -PortNumber $PortNumber
    
    # 5. Abschlussprüfung
    Write-Host "✅ REPARATUR ABGESCHLOSSEN" -ForegroundColor Green
    Write-Host ""
    Write-Host "🎯 NÄCHSTE SCHRITTE:" -ForegroundColor Cyan
    Write-Host "1. Einsatzüberwachung als Administrator starten" -ForegroundColor Yellow
    Write-Host "2. Mobile Verbindung öffnen" -ForegroundColor Yellow
    Write-Host "3. 'Server starten' klicken" -ForegroundColor Yellow
    Write-Host "4. QR-Code mit iPhone scannen" -ForegroundColor Yellow
    Write-Host ""
}

# Hauptprogramm
if ($DiagnoseOnly) {
    Write-Host "🔍 NUR DIAGNOSE-MODUS" -ForegroundColor Cyan
    Write-Host ""
    Invoke-SystemDiagnosis
    Test-AndFixPort -PortNumber $Port
} else {
    if ($Force) {
        Write-Host "⚡ AUTOMATISCHER REPARATUR-MODUS (Ohne Nachfragen)" -ForegroundColor Cyan
        Write-Host ""
    } else {
        Write-Host "🔧 INTERAKTIVER REPARATUR-MODUS" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "💡 Dieses Script behebt automatisch häufige Server-Start-Probleme:" -ForegroundColor Yellow
        Write-Host "   • Port-Konflikte" -ForegroundColor Yellow
        Write-Host "   • URL-Reservierungen" -ForegroundColor Yellow
        Write-Host "   • Firewall-Regeln" -ForegroundColor Yellow
        Write-Host ""
        
        if ((Read-Host "Möchten Sie fortfahren? (j/n)") -ne 'j') {
            Write-Host "Reparatur abgebrochen." -ForegroundColor Red
            exit 0
        }
        Write-Host ""
    }
    
    Invoke-FullRepair -PortNumber $Port
}

Write-Host "📱 MOBILE SERVER REPARATUR ABGESCHLOSSEN" -ForegroundColor Cyan
Write-Host ""
Read-Host "Drücken Sie Enter zum Beenden"
