# 📦 Setup & Installation - Einsatzüberwachung Professional v1.6

## 🎯 **Übersicht - Verfügbare Setup-Optionen**

Für die professionelle Installation der Einsatzüberwachung-Anwendung stehen verschiedene Methoden zur Verfügung:

---

## 🥇 **Option 1: Windows Application Packaging Project (MSIX)**
**Empfohlen für Windows 10/11 - Moderne App-Installation**

### **✅ Vorteile:**
- Automatische Updates über Microsoft Store
- Sandbox-Sicherheit mit kontrollierten Berechtigungen
- Saubere Installation/Deinstallation
- Keine Admin-Rechte für Installation erforderlich
- Automatische Desktop/Startmenü-Verknüpfungen

### **📦 Features:**
- Automatische Firewall-Konfiguration nach Installation
- Integrierte Mobile Server Setup-Routine
- .NET 8 Runtime automatisch mitgeliefert
- Crash-Recovery und Auto-Update-Funktionalität

---

## 🥈 **Option 2: Inno Setup - Klassisches Setup.exe**
**Bewährt für alle Windows-Versionen - Maximale Kompatibilität**

### **✅ Vorteile:**
- Funktioniert auf Windows 7, 8, 10, 11
- Vollständige Admin-Integration
- Benutzerdefinierte Installation
- Firewall/Netzwerk-Konfiguration während Setup
- Portable Version möglich

### **📦 Features:**
- Automatische .NET 8 Runtime Installation
- Admin-Rechte-Setup für Mobile Server
- Firewall-Regeln während Installation
- URL-Reservierungen automatisch konfiguriert
- Desktop/Startmenü-Verknüpfungen

---

## 🥉 **Option 3: ClickOnce Deployment**
**Einfach für Updates - Web-basierte Installation**

### **✅ Vorteile:**
- Automatische Updates
- Einfache Web-basierte Installation
- Keine Administrator-Rechte erforderlich
- Rollback-Funktionalität

### **⚠️ Limitierungen:**
- Eingeschränkte System-Integration
- Mobile Server benötigt manuelle Konfiguration

---

## 🎯 **Empfohlene Lösung: Inno Setup (für Ihre Anwendung)**

Da Ihre Einsatzüberwachung-Anwendung Admin-Rechte für die Mobile Server-Funktionalität benötigt, ist **Inno Setup** die beste Wahl:

### **🔧 Was das Setup automatisch macht:**
1. **.NET 8 Runtime** Installation (falls nicht vorhanden)
2. **Firewall-Regeln** für Port 8080 erstellen
3. **URL-Reservierungen** für Mobile Server konfigurieren
4. **Desktop-Verknüpfung** mit "Als Administrator ausführen"
5. **Startmenü-Eintrag** erstellen
6. **Automatic Updates** Setup
7. **Crash Recovery** Ordner erstellen

### **📋 Installation umfasst:**
- Hauptanwendung mit allen Dependencies
- PowerShell-Scripts (Fix-MobileServer.ps1)
- Dokumentation und Hilfe-Dateien
- Mobile Server Trouble-Shooting Guides
- System-Diagnose Tools

---

## 🛠️ **Setup-Konfiguration Details**

### **Installationsoptionen:**
- **Standard-Installation:** `C:\Program Files\Einsatzüberwachung Professional`
- **Portable Installation:** Wahlweise auf USB-Stick
- **Multi-User-Support:** Separate Konfigurationen pro Benutzer
- **Silent Installation:** Für IT-Administratoren

### **Automatische Konfiguration:**
```cmd
# Diese Befehle werden automatisch während Installation ausgeführt:
netsh http add urlacl url=http://+:8080/ user=Everyone
netsh advfirewall firewall add rule name="Einsatzueberwachung_Mobile" dir=in action=allow protocol=TCP localport=8080

# Registry-Einträge für Auto-Start und Updates
```

### **Deinstallation:**
- Vollständig saubere Entfernung aller Komponenten
- Firewall-Regeln optional beibehalten
- Benutzerdaten optional behalten
- URL-Reservierungen automatisch entfernt

---

## 📋 **Implementierung - Schritt für Schritt**

### **1. Inno Setup Script erstellen**
- Automatische Datei-Installation
- Registry-Einträge für Mobile Server
- .NET 8 Runtime Dependency Check
- Admin-Rechte-Anforderung

### **2. Mobile Server Integration**
- Automatische Netzwerk-Konfiguration
- Firewall-Regeln während Installation
- PowerShell-Scripts für Troubleshooting
- System-Diagnose Tools

### **3. Professional Features**
- Auto-Update-Mechanismus
- Crash Recovery Setup
- Benutzer-spezifische Konfiguration
- IT-Administrator-Tools

### **4. Qualitätssicherung**
- Setup auf verschiedenen Windows-Versionen testen
- Admin/Non-Admin-Installation testen
- Mobile Server Funktionalität nach Installation prüfen
- Deinstallation vollständig testen

---

## 🎯 **Ergebnis nach Installation**

### **✅ Benutzer erhält:**
- **Vollständig konfigurierte** Einsatzüberwachung-Anwendung
- **Automatisch funktionierenden** Mobile Server (mit Admin-Rechten)
- **Desktop-Verknüpfung** mit korrekten Berechtigungen
- **Alle Troubleshooting-Tools** vorinstalliert
- **Hilfe-Dokumentation** lokal verfügbar

### **🔧 IT-Administrator erhält:**
- **Silent Installation** Möglichkeiten
- **Group Policy** Integration
- **Zentrale Konfiguration** über MSI-Parameter
- **Automatische Updates** über Netzwerk-Share

---

## 💡 **Zusätzliche Features**

### **Update-Mechanismus:**
- Automatische Update-Prüfung beim Start
- Download neuer Versionen im Hintergrund
- Ein-Klick-Update mit Datensicherung
- Rollback bei Update-Problemen

### **Enterprise-Features:**
- **MSI-Package** für Enterprise-Deployment
- **Group Policy ADMX** Templates
- **Silent Configuration** Tools
- **Zentrale Lizenz-Verwaltung**

---

**🚀 Die Inno Setup Lösung bietet die beste Balance zwischen Benutzerfreundlichkeit und professioneller Installation!**

**📦 Möchten Sie, dass ich das komplette Setup-Script und die Konfiguration für Sie erstelle?**
