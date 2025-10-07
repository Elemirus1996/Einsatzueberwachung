# 🚁 Einsatzüberwachung Professional v1.9.1

## 🧡 Professionelle Software für Rettungshunde-Einsätze

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-MVVM-0078D4?style=flat)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.txt)
[![GitHub Release](https://img.shields.io/github/v/release/Elemirus1996/Einsatzueberwachung)](https://github.com/Elemirus1996/Einsatzueberwachung/releases)

**Einsatzüberwachung Professional** ist eine hochmoderne WPF-Anwendung für die professionelle Verwaltung und Überwachung von Rettungshunde-Teams während Einsätzen. Mit MVVM-Architektur, Orange-Design-System und mobiler Integration.

---

## ✨ **Hauptfeatures v1.9.1**

### 🧡 **Modernes Orange-Design-System**
- **Dark/Light-Mode** mit automatischem Tageszeit-Switching
- **Orange-fokussierte Farbpalette** für bessere Erkennbarkeit
- **Responsive Design** für verschiedene Bildschirmgrößen
- **Professional UI-Components** mit Elevation-System

### 📱 **Mobile Integration** 
- **Professional Mobile Website** für iPhone/Android-Zugriff
- **QR-Code-Verbindung** für schnelle Mobile-Anbindung
- **Real-time Updates** alle 10 Sekunden
- **Touch-optimierte Bedienung** für Einsätze im Feld

### 👥 **Intelligente Team-Verwaltung**
- **Multiple Team-Types** (Flächensuche, Trümmersuche, Mantrailing, etc.)
- **Flexible Warnzeiten** pro Team konfigurierbar
- **Real-time Timer-System** mit visuellen und akustischen Warnungen
- **Team-Status-Dashboard** mit kompakter Card-Ansicht

### 📊 **Stammdaten-Integration**
- **Personal-Management** mit Skills (Gruppenführer, Hundeführer, etc.)
- **Hunde-Verwaltung** mit Spezialisierungen
- **Auto-Vervollständigung** in Team-Erstellungs-Dialogen
- **CRUD-Operations** für alle Stammdaten

### 💾 **Session-Management**
- **Auto-Save** alle 30 Sekunden
- **Crash-Recovery** mit automatischer Wiederherstellung
- **Session-Persistence** für unterbrechungsfreie Einsätze
- **Backup-System** mit Versionierung

### 🔄 **Auto-Update-System**
- **GitHub-Integration** für automatische Updates
- **Download-Progress** mit moderner UI
- **Release-Notes-Display** für Änderungsübersicht
- **Skip-Version-Funktionalität** für optionale Updates

---

## 🚀 **Installation**

### **Automatische Installation (Empfohlen)**
1. Laden Sie die neueste **Setup.exe** von den [GitHub Releases](https://github.com/Elemirus1996/Einsatzueberwachung/releases) herunter
2. Führen Sie die Setup-Datei als **Administrator** aus
3. Folgen Sie dem Installations-Assistenten
4. **Fertig!** Die Anwendung startet automatisch

### **Portable Version**
1. Laden Sie die **Portable.zip** von den Releases herunter
2. Entpacken Sie in einen beliebigen Ordner
3. Starten Sie `Einsatzueberwachung.exe`

---

## 🎯 **Schnellstart**

### **1. Neuen Einsatz starten**
```
1. 📍 Einsatzort eingeben (z.B. "Waldgebiet Musterstadt")
2. 👮 Alarmiert durch (z.B. "Polizei Musterstadt")
3. 🚁 Einsatz-Template wählen oder individuell konfigurieren
4. ▶️ "Einsatz starten" klicken
```

### **2. Teams hinzufügen**
```
1. 👥 "Team hinzufügen" Button klicken
2. 🎯 Team-Typ auswählen (Flächensuche, Mantrailing, etc.)
3. 👤 Personal aus Stammdaten wählen oder neu eingeben
4. 🐕 Hund(e) zuweisen
5. ⏰ Individuelle Warnzeiten setzen (optional)
6. ✅ Team erstellen
```

### **3. Mobile Verbindung nutzen**
```
1. 📱 "Mobile Server" in Einstellungen aktivieren
2. 📷 QR-Code mit iPhone/Android scannen
3. 🌐 Mobile Website öffnet sich automatisch
4. 📊 Real-time Team-Status auf dem Handy verfolgen
```

---

## 🏗️ **Architektur**

### **MVVM-Pattern**
```
📁 Views/          - UI-Components (XAML)
📁 ViewModels/     - Business Logic & Binding
📁 Models/         - Datenmodelle
📁 Services/       - Backend-Services
📁 Resources/      - Design-System & Assets
```

### **Technologie-Stack**
- **.NET 8.0** - Modern Cross-Platform Framework
- **WPF** - Windows Presentation Foundation
- **MVVM** - Model-View-ViewModel Pattern
- **HttpListener** - Mobile Server Integration
- **JSON** - Datenserialisierung
- **Inno Setup** - Professional Installation System

---

## ⚙️ **Konfiguration**

### **Einstellungen-Kategorien**
1. **🎨 Darstellung** - Theme-Management, Orange-Design-Optionen
2. **⏰ Warnzeiten** - Globale Timer-Konfiguration, Presets
3. **📱 Mobile Server** - Port-Konfiguration, Netzwerk-Tests
4. **🔄 Updates** - Auto-Update-Checks, GitHub-Integration
5. **👥 Stammdaten** - Personal- und Hunde-Management
6. **ℹ️ Informationen** - About, Help, Debug-Informationen

### **Erweiterte Konfiguration**
```json
// AppSettings.json (automatisch erstellt)
{
  "Theme": "Auto",
  "DarkModeStartTime": "18:00",
  "DarkModeEndTime": "08:00",
  "AutoSaveInterval": 30,
  "MobileServerPort": 8080,
  "CheckForUpdates": true
}
```

---

## 📱 **Mobile Integration**

### **Unterstützte Geräte**
- ✅ **iPhone** (iOS 12+)
- ✅ **Android** (Chrome 70+)
- ✅ **iPad/Tablets**
- ✅ **Alle modernen Browser**

### **Mobile Features**
- 📊 **Real-time Team-Status** mit Live-Timern
- 📝 **Global Notes Timeline** für Ereignisse
- 🎯 **Mission Status Dashboard** 
- 🔄 **Auto-Refresh** alle 10 Sekunden
- 📳 **Touch-optimierte Bedienung**

### **API-Endpoints**
```
GET  /api/teams      - Team-Daten mit Real-time Status
GET  /api/status     - Einsatz-Status und Statistiken
GET  /api/notes      - Globale Notizen und Ereignisse
GET  /debug          - Server-Diagnostics
POST /api/test       - Connection-Testing
```

---

## 🔧 **Entwicklung**

### **Voraussetzungen**
- **Visual Studio 2022** (17.8+)
- **.NET 8.0 SDK**
- **Windows 10/11** (für WPF-Entwicklung)

### **Projekt klonen und starten**
```bash
git clone https://github.com/Elemirus1996/Einsatzueberwachung.git
cd Einsatzueberwachung
dotnet restore
dotnet build
dotnet run
```

### **Release erstellen**
```bash
# Automatisches Release-Script
.\Create-Release-Tag.bat

# Oder PowerShell-Version mit erweiterten Features
.\Create-Release-Tag.ps1 -Force
```

---

## 📊 **Statistiken & Export**

### **PDF-Export-System**
- 📄 **Professional Reports** mit Logo und Corporate Design
- 📊 **Einsatz-Statistiken** mit Grafiken und Tabellen
- ⏰ **Team-Timeline** mit allen Ereignissen
- 📝 **Global Notes** chronologisch sortiert
- 🎯 **Multi-Format-Export** (PDF, TXT, CSV-ready)

### **Auswertungs-Features**
- 📈 **Einsatz-Dauer-Analyse**
- 👥 **Team-Performance-Tracking**
- ⚠️ **Warning-Statistiken**
- 📍 **Einsatzort-Historie**

---

## 🔍 **Troubleshooting**

### **Häufige Probleme**

#### **Mobile Verbindung funktioniert nicht**
```
✅ Lösung:
1. Windows Firewall temporär deaktivieren
2. Als Administrator starten
3. Port 8080 freigeben
4. QR-Code neu generieren
```

#### **Dark Mode wechselt nicht automatisch**
```
✅ Lösung:
1. Einstellungen → Darstellung
2. Theme auf "Auto" setzen
3. Zeiten überprüfen (Standard: 18:00-08:00)
4. Anwendung neu starten
```

#### **Teams verschwinden nach Neustart**
```
✅ Lösung:
1. Auto-Save ist aktiv (alle 30 Sekunden)
2. Session-Recovery beim Start nutzen
3. Backup aus "AppData\Local\Einsatzueberwachung" wiederherstellen
```

### **Debug-Informationen**
```
📁 Logs: %LocalAppData%\Einsatzueberwachung\Logs\
📁 Settings: %LocalAppData%\Einsatzueberwachung\Settings\
📁 Sessions: %LocalAppData%\Einsatzueberwachung\Sessions\
```

---

## 🤝 **Mitwirken**

### **Bug Reports & Feature Requests**
- 🐛 **Issues:** [GitHub Issues](https://github.com/Elemirus1996/Einsatzueberwachung/issues)
- 💡 **Feature Requests:** Nutzen Sie die Issue-Templates
- 📧 **Kontakt:** Über GitHub oder direkte Nachricht

### **Code-Beiträge**
1. **Fork** des Repositories erstellen
2. **Feature-Branch** erstellen (`git checkout -b feature/AmazingFeature`)
3. **Änderungen committen** (`git commit -m 'Add AmazingFeature'`)
4. **Branch pushen** (`git push origin feature/AmazingFeature`)
5. **Pull Request** öffnen

---

## 📋 **Roadmap v2.0.0**

### **🆕 Geplante Features**
- 💬 **WhatsApp-ähnliches Reply-System** für strukturierte Kommunikation
- 🗺️ **Map-Integration** mit Suchgebiets-Management
- 🎤 **Sprachnachrichten** für mobile Kommunikation
- 📸 **Foto-Anhänge** für Situationsberichte
- 🤖 **AI-basierte Antwort-Vorschläge**

### **🔧 Technische Verbesserungen**
- 🚀 **Performance-Optimierungen** für große Einsätze
- 🌐 **Offline-Modus** für Gebiete ohne Internet
- 📡 **GPS-Integration** für Live-Team-Tracking
- 🔐 **Enhanced Security** für Mobile-Verbindungen

---

## 📜 **Lizenz**

Dieses Projekt steht unter der **MIT License** - siehe [LICENSE.txt](LICENSE.txt) für Details.

**Copyright © 2024 RescueDog_SW** - Alle Rechte vorbehalten.

---

## 🙏 **Danksagung**

- **Rettungshunde-Teams** für Feedback und Testing
- **Open Source Community** für verwendete Libraries
- **Microsoft** für .NET und WPF Framework
- **GitHub** für Hosting und CI/CD-Integration

---

## 📞 **Support & Kontakt**

- 📚 **Dokumentation:** [GitHub Wiki](https://github.com/Elemirus1996/Einsatzueberwachung/wiki)
- 🐛 **Bug Reports:** [GitHub Issues](https://github.com/Elemirus1996/Einsatzueberwachung/issues)
- 💬 **Diskussionen:** [GitHub Discussions](https://github.com/Elemirus1996/Einsatzueberwachung/discussions)
- 📧 **Direct Contact:** Über GitHub Profile

---

## ⭐ **Star History**

Wenn Ihnen dieses Projekt gefällt, geben Sie ihm einen **Stern** ⭐ auf GitHub!

[![Stargazers over time](https://starchart.cc/Elemirus1996/Einsatzueberwachung.svg)](https://starchart.cc/Elemirus1996/Einsatzueberwachung)

---

**🚁 Einsatzüberwachung Professional - Professionelle Software für professionelle Retter! 🧡**
