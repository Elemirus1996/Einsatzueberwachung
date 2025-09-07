# 📦 Setup-Erstellung für Einsatzüberwachung Professional v1.6

## 🎯 **Übersicht - Wie erstelle ich eine Setup.exe?**

Es gibt mehrere Wege, eine professionelle Setup.exe für die Einsatzüberwachung zu erstellen:

---

## 🚀 **Methode 1: Automatisches Build-Script (Empfohlen)**

### **✅ Einfachste Methode - Ein Klick:**

1. **Doppelklick auf `Create-Setup.bat`**
2. **Option 1** wählen ("Vollständiges Setup erstellen")
3. **Warten** bis Setup.exe erstellt ist
4. **Fertig!** Setup in `Setup\Output\` Ordner

### **📋 Was passiert automatisch:**
- .NET Projekt wird kompiliert
- Alle Dateien werden vorbereitet
- Inno Setup Script wird ausgeführt
- Setup.exe wird erstellt
- Dokumentation wird hinzugefügt

---

## 🔧 **Methode 2: PowerShell Build-Script**

### **Für erweiterte Konfiguration:**

```powershell
# Vollständiges Setup:
.\Build-Setup.ps1

# Clean Build (empfohlen bei Problemen):
.\Build-Setup.ps1 -CleanBuild

# Ohne Tests:
.\Build-Setup.ps1 -SkipTests

# Custom Configuration:
.\Build-Setup.ps1 -Configuration Release -Version "1.6.0"
```

### **📊 Script-Features:**
- Automatische Umgebungsprüfung
- .NET Build und Publish
- Setup-Dateien vorbereitung
- Inno Setup Ausführung
- Ergebnis-Statistiken

---

## 🛠️ **Methode 3: MSBuild Integration**

### **Für automatische Builds:**

```cmd
# Setup automatisch nach Release-Build:
dotnet build --configuration Release

# Setup explizit erstellen:
dotnet build --target CreateSetup --configuration Release

# PowerShell Build-Script via MSBuild:
dotnet build --target RunBuildScript
```

### **📦 MSBuild-Integration:**
- Automatische Setup-Erstellung nach Publish
- Konfigurierbar über Project-Properties
- CI/CD Pipeline Integration möglich

---

## 📋 **Methode 4: Manuell mit Inno Setup**

### **Für Experten-Kontrolle:**

1. **Inno Setup installieren:** https://jrsoftware.org/isinfo.php
2. **.NET Projekt bauen:**
   ```cmd
   dotnet publish "Einsatzüberwachung V1.5.csproj" --configuration Release --output "bin\Release\net8.0-windows"
   ```
3. **Inno Setup Script öffnen:** `Setup\Einsatzueberwachung_Setup.iss`
4. **Kompilieren** (F9 oder Build → Compile)
5. **Setup.exe** in `Setup\Output\` verfügbar

---

## 🎯 **Was enthält die Setup.exe?**

### **📦 Installierte Komponenten:**
- **Hauptanwendung** (Einsatzüberwachung.exe)
- **.NET 8 Runtime** (automatisch falls nicht vorhanden)
- **PowerShell-Scripts** für Troubleshooting:
  - Fix-MobileServer.ps1
  - Setup-MobileNetwork.ps1
- **Vollständige Dokumentation:**
  - Server-Start-Lösungsanleitung
  - Mobile Setup Guide
  - HTTP 400 Troubleshooting
  - Installation Guide

### **🔧 Automatische Konfiguration:**
- **URL-Reservierungen** für Mobile Server (Port 8080)
- **Firewall-Regeln** für iPhone/Android-Zugriff
- **Registry-Einträge** für Updates und Konfiguration
- **PowerShell ExecutionPolicy** für Scripts
- **Desktop/Startmenü-Verknüpfungen**

### **🛡️ Sicherheits-Features:**
- **Administrator-Installation** für Mobile Server-Features
- **Digitale Signatur** (optional konfigurierbar)
- **Saubere Deinstallation** mit Cleanup
- **Firewall-Integration** ohne manuelle Konfiguration

---

## 📋 **Anforderungen für Setup-Erstellung**

### **🔧 Entwickler-System:**
- **Windows 10/11** mit .NET 8 SDK
- **Inno Setup 6** (kostenlos)
- **PowerShell 5.1** oder neuer
- **Administrator-Rechte** für Testing

### **🎯 Ziel-System (Endbenutzer):**
- **Windows 10** oder neuer
- **.NET 8 Runtime** (wird automatisch installiert)
- **Administrator-Rechte** für vollständige Funktionalität
- **500 MB** freier Speicherplatz

---

## 🚀 **Schritt-für-Schritt: Erste Setup-Erstellung**

### **1. Vorbereitung (einmalig):**
```cmd
# Inno Setup herunterladen und installieren:
# https://jrsoftware.org/isinfo.php

# Repository klonen/herunterladen falls nicht vorhanden
```

### **2. Setup erstellen:**
```cmd
# Option A: Batch-Script (einfachste)
Doppelklick auf Create-Setup.bat → Option 1

# Option B: PowerShell (erweitert)
.\Build-Setup.ps1 -CleanBuild

# Option C: MSBuild (automatisch)
dotnet build --configuration Release --target CreateSetup
```

### **3. Setup testen:**
```cmd
# Setup.exe findet sich in: Setup\Output\
# Als Administrator ausführen
# Installation durchlaufen lassen
# Anwendung starten und Mobile Server testen
```

### **4. Verteilung:**
```cmd
# Setup.exe ist vollständig selbständig
# Kann per E-Mail, USB-Stick oder Download verteilt werden
# Keine zusätzlichen Dateien erforderlich
```

---

## 💡 **Häufige Probleme und Lösungen**

### **❌ "Inno Setup nicht gefunden"**
**Lösung:** Download von https://jrsoftware.org/isinfo.php

### **❌ "Build fehlgeschlagen"**
**Lösung:** 
```cmd
# Clean Build versuchen:
.\Build-Setup.ps1 -CleanBuild

# Oder manuell:
dotnet clean
dotnet restore
dotnet build --configuration Release
```

### **❌ "Setup.exe funktioniert nicht"**
**Lösung:**
```cmd
# Setup als Administrator ausführen
# Windows Defender/Antivirus temporär deaktivieren
# .NET 8 Runtime manuell installieren
```

### **❌ "Mobile Server funktioniert nach Installation nicht"**
**Lösung:**
```cmd
# Automatische Reparatur verwenden:
Startmenü → Einsatzüberwachung → Mobile Server Reparatur

# Oder PowerShell:
.\Fix-MobileServer.ps1 -Force
```

---

## 🎉 **Erfolg-Checkliste**

### **✅ Setup erfolgreich erstellt wenn:**
- [ ] Setup.exe in `Setup\Output\` vorhanden
- [ ] Größe ca. 50-100 MB (je nach .NET Runtime)
- [ ] Installation startet ohne Fehler
- [ ] Anwendung startet nach Installation
- [ ] Mobile Server funktioniert
- [ ] Deinstallation funktioniert sauber

### **✅ Bereit für Verteilung wenn:**
- [ ] Setup auf mindestens 2 verschiedenen PCs getestet
- [ ] Admin und Non-Admin Installation getestet
- [ ] Mobile Server auf verschiedenen Netzwerken getestet
- [ ] Deinstallation vollständig getestet
- [ ] Dokumentation aktuell und vollständig

---

## 📞 **Setup-Erstellung Support**

### **Bei Problemen mit Setup-Erstellung:**
1. **Create-Setup.bat** verwenden (einfachste Methode)
2. **Build-Setup.ps1 -CleanBuild** für Clean Build
3. **Inno Setup Logs** prüfen bei Fehlern
4. **System-Anforderungen** überprüfen

### **Bei Setup-Installation-Problemen:**
1. **Als Administrator ausführen**
2. **Windows Update** durchführen
3. **Antivirus temporär deaktivieren**
4. **.NET 8 Runtime manuell installieren**

---

**🚀 Mit diesen Methoden können Sie professionelle Setup.exe-Dateien für die Einsatzüberwachung erstellen!**

**📦 Die Setup.exe enthält alles was für die vollständige Installation und Konfiguration benötigt wird.**
