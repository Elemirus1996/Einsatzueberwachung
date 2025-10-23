# 🚁 Einsatzüberwachung Professional v1.9.6

## 🧡 Professionelle Software für Rettungshunde-Einsätze

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-MVVM-0078D4?style=flat)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.txt)
[![GitHub Release](https://img.shields.io/github/v/release/Elemirus1996/Einsatzueberwachung)](https://github.com/Elemirus1996/Einsatzueberwachung/releases)

**Einsatzüberwachung Professional** ist eine hochmoderne WPF-Anwendung für die professionelle Verwaltung und Überwachung von Rettungshunde-Teams während Einsätzen. Mit vollständiger MVVM-Architektur, Orange-Design-System und umfassender mobiler Integration.

---

## ✨ **Hauptfeatures v1.9.6**

### 🧡 **Modernes Orange-Design-System**
- **Dark/Light-Mode** mit automatischem Tageszeit-Switching (18:00-08:00)
- **Orange-fokussierte Farbpalette** für optimale Erkennbarkeit und Professionalität
- **Material Design 3** Integration mit Elevation-System und Animationen
- **Responsive Design** für verschiedene Bildschirmgrößen (1200px - 4K)
- **UnifiedThemeManager** für zentrale Theme-Verwaltung

### 📱 **Professional Mobile Integration** 
- **Professional Mobile Website** für iPhone/Android mit Orange-Branding
- **QR-Code-Verbindung** für schnelle Mobile-Anbindung ohne App-Installation
- **Real-time Updates** alle 5-10 Sekunden mit Live-Timer-Synchronisation
- **Touch-optimierte Bedienung** für Einsätze im Feld mit Handschuhen
- **Reply-System** für strukturierte Kommunikation zwischen Teams
- **Mobile API** mit RESTful Endpoints für externe Integration

### 👥 **Intelligente Team-Verwaltung**
- **Multiple Team-Types** (Flächensuche, Trümmersuche, Mantrailing, Wasserrettung, Lawinen, Allgemein)
- **Flexible Warnzeiten** pro Team individuell konfigurierbar (1-120 Minuten)
- **Real-time Timer-System** mit präzisen visuellen und akustischen Warnungen
- **Team-Status-Dashboard** mit kompakter Card-Ansicht im Orange-Design
- **Keyboard-Shortcuts** (F1-F10) für schnelle Timer-Steuerung
- **Team-Assignment** mit Personal- und Hunde-Zuordnung aus Stammdaten

### 📊 **Umfassende Stammdaten-Integration**
- **Personal-Management** mit Skills (Gruppenführer, Hundeführer, Zugführer, Verbandsführer, Fachberater)
- **Hunde-Management** mit Spezialisierungen (Flächensuche, Trümmersuche, Mantrailing, etc.)
- **Auto-Vervollständigung** in Team-Erstellungs-Dialogen mit MVVM-Bindings
- **CRUD-Operations** für alle Stammdaten mit JSON-Persistierung
- **Import/Export-Funktionalität** für Daten-Migration zwischen Systemen

### 💬 **Erweiterte Kommunikation**
- **Reply-System** mit Thread-Management für strukturierte Kommunikation
- **Globale Notizen** mit Team-Zuordnung und Kategorisierung
- **Thread-Visualisierung** mit hierarchischer Darstellung (max. 3 Ebenen)
- **Real-time Messaging** zwischen Desktop und Mobile
- **Export-Funktionalität** für komplette Kommunikations-Logs

### 💾 **Enterprise Session-Management**
- **Auto-Save** alle 30 Sekunden mit konfigurierbare Intervalle
- **Crash-Recovery** mit automatischer Wiederherstellung nach Systemfehlern
- **Session-Persistence** für unterbrechungsfreie mehrtägige Einsätze
- **Backup-System** mit Versionierung im LocalAppData-Verzeichnis
- **Recovery-Dialog** mit Daten-Validierung beim Neustart

### 🔄 **Professionelles Auto-Update-System**
- **GitHub-Integration** für automatische Update-Checks alle 24 Stunden
- **Download-Progress** mit moderner Orange-UI und Abbruch-Möglichkeit
- **Release-Notes-Display** für detaillierte Änderungsübersicht
- **Skip-Version-Funktionalität** für optionale Updates
- **Background-Updates** ohne Unterbrechung des laufenden Einsatzes

### 📄 **Professional PDF-Export-System**
- **Corporate Design** mit Orange-Branding und professionellem Layout
- **Einsatz-Statistiken** mit Grafiken, Tabellen und Timeline-Darstellung
- **Team-Performance-Tracking** mit detaillierten Auswertungen
- **Multi-Format-Export** (PDF, TXT, CSV-ready) für verschiedene Anforderungen
- **Template-System** mit anpassbaren Layouts für verschiedene Organisationen

---

## 🚀 **Installation & Setup**

### **Automatische Installation (Empfohlen)**
```bash
1. Download: Neueste Setup.exe von GitHub Releases
2. Als Administrator ausführen (UAC-Dialog bestätigen)
3. Installations-Assistent folgen
4. Automatischer Start nach Installation
5. Optional: Desktop-Verknüpfung erstellen
```

### **Portable Version für Einsatz-Laptops**
```bash
1. Download: Portable.zip von Releases
2. Entpacken in beliebigen Ordner
3. Einsatzueberwachung.exe starten
4. Alle Daten werden lokal im Ordner gespeichert
```

### **Systemanforderungen**
- **Windows 10/11** (x64)
- **.NET 8.0 Runtime** (wird automatisch installiert)
- **4 GB RAM** (empfohlen: 8 GB)
- **500 MB Festplattenspeicher**
- **Internetverbindung** für Updates und Mobile-Features

---

## 🎯 **Schnellstart-Guide**

### **1. Neuen Einsatz initialisieren**
```
🏃‍♂️ Schnellstart (< 2 Minuten):
1. 📍 Einsatzort: "Waldgebiet Musterstadt, Sektor B"
2. 🚨 Alarmiert durch: "Polizei Musterstadt, Vermisste Person"
3. 👮 Einsatzleiter: "Max Mustermann, Zugführer"
4. ⏱️ Warnzeiten: Standard (10/20 Min) oder individuell
5. ▶️ "Einsatz starten" → Hauptfenster mit Orange-Dashboard
```

### **2. Teams effizient hinzufügen**
```
👥 Team-Erstellung MVVM-Style:
1. 🟠 "+ Team" Button im Orange-Header
2. 🎯 Team-Typ: Flächensuche/Mantrailing/Trümmersuche/etc.
3. 🐕 Hund: Aus Stammdaten oder neu eingeben
4. 👤 Hundeführer: Auto-Fill bei Hund-Auswahl
5. 👥 Helfer: Optional aus Personal-Liste
6. 📍 Suchgebiet: "Sektor A, Nordwest-Bereich"
7. ⏰ Individuelle Warnzeiten (optional)
8. ✅ Team erstellen → Sofort im Dashboard sichtbar
```

### **3. Mobile Integration aktivieren**
```
📱 iPhone/Android-Zugriff:
1. 🟠 Einstellungen → Mobile Server
2. 🔑 "Als Administrator starten" (für Netzwerk-Zugriff)
3. ▶️ "Server starten" → Orange-Status-Indikator
4. 📷 QR-Code mit Smartphone scannen
5. 🌐 Mobile Website öffnet automatisch
6. 📊 Real-time Team-Status auf Handy verfügbar
7. 💬 Reply-System für Kommunikation nutzen
```

### **4. Timer-Steuerung & Monitoring**
```
⏱️ Timer-Bedienung:
• F1-F10: Direkte Timer-Steuerung für Teams 1-10
• 🟠 Dashboard-Buttons: Maus-Bedienung mit Orange-Feedback
• 📱 Mobile: Touch-Bedienung für Feldsteuerung
• ⚠️ Automatische Warnungen: Erste (Orange) und Zweite (Rot)
• 🔊 Audio-Feedback: Konfigurierbarer Sound-Alarm
```

---

## 🏗️ **Technische Architektur**

### **MVVM-Pattern Excellence**
```
📁 Projektstruktur:
├── 📱 Views/              - UI-Components (XAML) mit Orange-Design
├── 🧠 ViewModels/         - Business Logic & Data-Binding
├── 📊 Models/             - Datenmodelle mit INotifyPropertyChanged
├── ⚙️ Services/           - Backend-Services (Singleton-Pattern)
├── 🧡 Resources/          - Orange-Design-System & Assets
├── 🌐 API/               - Mobile-Integration & RESTful Services
└── 📄 Documentation/     - Umfassende Entwickler-Dokumentation
```

### **Technologie-Stack**
- **.NET 8.0** - Modernes Cross-Platform Framework
- **WPF** - Windows Presentation Foundation mit Hardware-Acceleration
- **MVVM** - Model-View-ViewModel Pattern mit Command-Binding
- **HttpListener** - High-Performance Mobile Server
- **JSON** - Strukturierte Datenserialisierung
- **QuestPDF** - Professional PDF-Generation
- **QRCoder** - QR-Code-Generation für Mobile-Integration

### **Performance-Optimierungen**
- **Async/Await** Pattern für alle I/O-Operationen
- **ObservableCollection** für automatische UI-Updates
- **Timer-Diagnostics** für Performance-Monitoring
- **Memory-Management** mit IDisposable-Pattern
- **Resource-Caching** für Orange-Design-Assets

---

## ⚙️ **Erweiterte Konfiguration**

### **Einstellungen-Kategorien**
```
🎨 Darstellung:
├── Orange-Design-System (Primary/Secondary/Tertiary Colors)
├── Dark/Light-Mode mit Auto-Switching (18:00-08:00)
├── Responsive Design für verschiedene Bildschirmgrößen
└── Animation-Settings und Performance-Tuning

⏰ Einsatz-Management:
├── Globale Warnzeiten (Standard: 10/20 Minuten)
├── Team-spezifische Schwellenwerte (1-120 Minuten)
├── Audio-Alarm-Konfiguration (System/WAV-Dateien)
└── Auto-Save-Intervall (10-300 Sekunden)

📱 Mobile Server:
├── Port-Konfiguration (Standard: 8080, Fallback: 8081-8083)
├── Netzwerk-Interface-Auswahl
├── QR-Code-Generierung und -Styling
└── API-Endpoint-Konfiguration

🔄 Updates & Wartung:
├── Auto-Update-Checks (täglich/wöchentlich/manuell)
├── GitHub-Release-Integration
├── Backup-Verwaltung (automatisch/manuell)
└── Log-Level und Debug-Informationen
```

### **JSON-Konfiguration**
```json
{
  "appearance": {
    "theme": "Auto",
    "orangePrimary": "#F57C00",
    "darkModeStart": "18:00",
    "darkModeEnd": "08:00",
    "enableAnimations": true
  },
  "einsatz": {
    "defaultFirstWarning": 10,
    "defaultSecondWarning": 20,
    "autoSaveInterval": 30,
    "maxTeams": 50
  },
  "mobile": {
    "serverPort": 8080,
    "enableQRCode": true,
    "updateInterval": 5000,
    "maxConnections": 100
  },
  "updates": {
    "autoCheck": true,
    "checkInterval": "24:00:00",
    "skipVersions": []
  }
}
```

---

## 📱 **Mobile Integration Details**

### **Unterstützte Geräte & Browser**
```
✅ iOS Geräte:
├── iPhone (iOS 12+): Safari, Chrome, Firefox
├── iPad (iOS 12+): Safari, Chrome mit Touch-Optimierung
└── iPod Touch: Vollständige Funktionalität

✅ Android Geräte:
├── Android (7.0+): Chrome, Firefox, Samsung Internet
├── Tablets: Optimierte Tablet-Ansicht
└── Chromebooks: Desktop-ähnliche Erfahrung

✅ Desktop Browser:
├── Windows: Chrome, Edge, Firefox
├── macOS: Safari, Chrome, Firefox
└── Linux: Chrome, Firefox (Test-Zwecke)
```

### **Mobile API-Endpoints (RESTful)**
```http
GET  /api/teams          # Team-Daten mit Real-time Status
GET  /api/status         # Einsatz-Status und Statistiken  
GET  /api/notes          # Globale Notizen mit Reply-System
POST /api/notes/{id}/reply # Reply zu bestehender Notiz
GET  /api/threads/{id}   # Thread-Messages für hierarchische Darstellung
GET  /debug              # Server-Diagnostics und Netzwerk-Info
POST /api/test           # Connection-Testing für Troubleshooting
```

### **Mobile Features im Detail**
- 📊 **Real-time Dashboard** mit Live-Timer-Updates (5s Intervall)
- 📝 **Notes & Reply-System** für strukturierte Team-Kommunikation
- 🎯 **Mission Status** mit Einsatzleiter und Ort-Informationen
- 🔄 **Auto-Refresh** mit Pull-to-Refresh-Geste
- 📳 **Touch-optimiert** für Bedienung mit Einsatz-Handschuhen
- 🌐 **Offline-Detection** mit automatischer Reconnection

---

## 📊 **Statistiken & Analytics**

### **PDF-Export-System Professional**
```
📄 Report-Typen:
├── 📊 Vollbericht: Alle Teams, Zeiten, Notizen und Statistiken
├── 📈 Kurzber Zusammenfassung mit KPIs und Highlights
├── 📉 Statistik-Report: Grafiken und Performance-Auswertungen
├── ⏰ Timeline-Export: Chronologischer Ereignis-Verlauf
└── 💬 Kommunikations-Log: Vollständige Notizen und Replies

🎯 Corporate Design:
├── Orange-Branding mit Logo-Integration
├── Professionelle Layouts für verschiedene Organisationen
├── Print-optimierte Farbgebung und Schriftarten
└── Automatische Seitennummerierung und Inhaltsverzeichnis
```

### **Performance-Metriken**
```
📈 Einsatz-KPIs:
├── Durchschnittliche Einsatzzeit pro Team
├── Warnschwellen-Überschreitungen und -Häufigkeit
├── Team-Effizienz-Vergleich (Zeit vs. Ergebnis)
├── Kommunikations-Frequenz und -Verteilung
└── Suchgebiet-Abdeckung und -Timing

🔍 System-Performance:
├── Timer-Präzision (< 100ms Abweichung)
├── Memory-Usage (< 200MB bei 50 Teams)
├── Mobile-Response-Time (< 500ms)
├── Database-Query-Performance
└── UI-Rendering-Metriken
```

---

## 🔧 **Entwicklung & Customization**

### **Entwicklungsumgebung Setup**
```bash
# Repository klonen
git clone https://github.com/Elemirus1996/Einsatzueberwachung.git
cd Einsatzueberwachung

# .NET 8 SDK installieren (falls nicht vorhanden)
winget install Microsoft.DotNet.SDK.8

# Dependencies installieren
dotnet restore

# Debug-Build erstellen
dotnet build --configuration Debug

# Anwendung starten (Development-Mode)
dotnet run --configuration Debug
```

### **Release-Erstellung automatisiert**
```bash
# Automatisches Release mit Version aus VersionService.cs
.\Create-Release-Tag.bat

# Erweiterte PowerShell-Version mit Parametern
.\Create-Release-Tag.ps1 -Force -Port 8080

# Manueller Build für spezielle Anforderungen
dotnet publish -c Release -r win-x64 --self-contained false
```

### **Testing & Quality Assurance**
```bash
# Unit Tests ausführen (falls verfügbar)
dotnet test

# Code-Analyse mit .NET Analyzers
dotnet build --verbosity normal

# Performance-Profiling
dotnet-counters monitor --process-id [PID]

# Memory-Leak-Detection
dotnet-dump collect -p [PID]
```

---

## 🔍 **Troubleshooting & Support**

### **Häufige Probleme & Lösungen**

#### **🚨 Mobile Server startet nicht**
```
🔧 Diagnose-Schritte:
1. ✅ Als Administrator starten (UAC-Dialog bestätigen)
2. 🔥 Windows Firewall temporär deaktivieren (Test)
3. 🔌 Port 8080 verfügbar? netstat -an | findstr :8080
4. 🌐 Antivirus-Software blockiert HTTP-Listener?
5. 🔄 Alternative Ports: 8081, 8082, 8083 versuchen

💡 Automatische Reparatur:
.\Fix-MobileServer.ps1 -Force (als Administrator)
```

#### **⚡ Performance-Probleme bei vielen Teams**
```
🚀 Optimierungen:
1. 📊 Performance-Mode in Einstellungen aktivieren
2. 🎨 Animationen deaktivieren (Einstellungen → Darstellung)
3. 💾 Auto-Save-Intervall erhöhen (60s statt 30s)
4. 🧹 Regelmäßiger Memory-Cleanup (automatisch alle 5 Min)
5. 📱 Mobile-Update-Intervall reduzieren (10s statt 5s)
```

#### **🎨 Dark Mode wechselt nicht automatisch**
```
🌙 Theme-Debugging:
1. ⚙️ Einstellungen → Darstellung → Theme: "Auto"
2. 🕐 Zeiten überprüfen: Standard 18:00-08:00
3. 🔄 Theme-Service neu starten: App neu starten
4. 🧡 Orange-Design-Cache leeren: %LocalAppData%\Einsatzueberwachung\Cache\
```

#### **💾 Session-Recovery funktioniert nicht**
```
🔄 Recovery-Probleme:
1. 📁 Backup-Ordner prüfen: %LocalAppData%\Einsatzueberwachung\Sessions\
2. 📝 Crash-Recovery aktiviert? Einstellungen → Erweitert
3. 🕐 Auto-Save-Funktion aktiv? (alle 30s Status in UI)
4. 📋 Manuelle Wiederherstellung: Session-Datei laden
```

### **Debug-Informationen sammeln**
```
📋 Support-Informationen:
├── 📁 Logs: %LocalAppData%\Einsatzueberwachung\Logs\
├── ⚙️ Settings: %LocalAppData%\Einsatzueberwachung\Settings\
├── 💾 Sessions: %LocalAppData%\Einsatzueberwachung\Sessions\
├── 🧡 Cache: %LocalAppData%\Einsatzueberwachung\Cache\
└── 📊 Performance: Einstellungen → Informationen → System-Info

🔍 Erweiterte Diagnose:
• Windows Event Log: Anwendungs- und Systemfehler
• Network Analysis: netstat -an für Port-Konflikte
• Performance Counters: Task Manager → Leistung
• Mobile Debug: http://localhost:8080/debug
```

---

## 🤝 **Community & Beiträge**

### **Bug Reports & Feature Requests**
- 🐛 **Issues:** [GitHub Issues](https://github.com/Elemirus1996/Einsatzueberwachung/issues) mit Templates
- 💡 **Feature Requests:** Detaillierte Beschreibung mit Use-Cases
- 📧 **Direkter Kontakt:** Über GitHub-Profile oder Discussions

### **Code-Beiträge & Pull Requests**
```bash
# Contribution Workflow:
1. 🍴 Fork des Repositories erstellen
2. 🌿 Feature-Branch: git checkout -b feature/AmazingFeature
3. 💻 Entwicklung mit .NET 8 und Orange-Design-Guidelines
4. ✅ Testing und Code-Review-Vorbereitung
5. 📝 Commit: git commit -m 'Add AmazingFeature with Orange integration'
6. 🚀 Push: git push origin feature/AmazingFeature
7. 🔄 Pull Request mit detaillierter Beschreibung öffnen
```

### **Entwicklungs-Guidelines**
- 🧡 **Orange-Design-System** befolgen (Farben, Spacing, Typography)
- 🏗️ **MVVM-Pattern** für alle UI-Komponenten verwenden
- ⚡ **Performance** bei großen Datenmengen berücksichtigen
- 📱 **Mobile-Kompatibilität** für neue Features einplanen
- 📝 **Dokumentation** für neue Features aktualisieren

---

## 📋 **Roadmap & Zukunft**

### **🆕 Version 2.0.0 - "Map Integration Edition"**
```
🗺️ Hauptfeatures:
├── 📍 OpenStreetMap-Integration für Einsatzort-Visualisierung
├── 🎯 Suchgebiet-Management mit Polygon-Drawing-Tools
├── 🌐 Offline-Map-Caching für Gebiete ohne Internet
└── 📱 Mobile-Map-Viewer mit Touch-Navigation

💬 Communication Enhancement:
├── 🎤 Sprachnachrichten für Mobile-Kommunikation
├── 📸 Foto-Anhänge für Situationsberichte
├── 🤖 AI-basierte Antwort-Vorschläge (Smart-Replies)
└── 💬 WhatsApp-ähnliche Thread-Funktionen

🔧 Advanced Features:
├── 🔐 Enhanced Security für Mobile-Verbindungen
├── 📊 Advanced Analytics mit Machine Learning
└── 🌍 Multi-Language-Support (EN, DE, FR)
```

### **🔄 Technische Verbesserungen v2.1+**
- 🏗️ **Microservices-Architektur** für bessere Skalierbarkeit
- 🌐 **Web-API** für externe Integrations-Möglichkeiten
- 📊 **Real-time Analytics** mit SignalR-Integration
- 🔒 **Enterprise-Security** mit Azure AD-Integration
- ☁️ **Cloud-Sync** für multi-device Scenarios

---

## 📜 **Lizenz & Rechtliches**

**MIT License** - Vollständige Nutzung für kommerzielle und private Zwecke

```
Copyright © 2024 RescueDog_SW

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

[Vollständige Lizenz in LICENSE.txt]
```

**Third-Party Libraries:**
- **.NET 8.0** - Microsoft Corporation (MIT)
- **FontAwesome.WPF** - charri (MIT)
- **QuestPDF** - QuestPDF (MIT)
- **QRCoder** - Raffael Herrmann (MIT)

---

## 🙏 **Danksagung & Credits**

- 🚁 **Rettungshunde-Teams** für wertvolles Feedback und Real-World-Testing
- 🧡 **Design-Community** für Orange-Design-Inspiration und Best-Practices
- 💻 **Open Source Community** für verwendete Libraries und Frameworks
- 🏢 **Microsoft** für .NET 8.0 und WPF-Framework
- 🐙 **GitHub** für Hosting, CI/CD und Release-Management
- 📱 **Mobile-Testing-Community** für Cross-Platform-Validierung

---

## 📞 **Support & Ressourcen**

### **Dokumentation & Hilfe**
- 📚 **Wiki:** [GitHub Wiki](https://github.com/Elemirus1996/Einsatzueberwachung/wiki) mit detaillierter Anleitung
- 🎥 **Video-Tutorials:** Coming Soon mit Step-by-Step-Guides
- 📖 **API-Dokumentation:** Für Entwickler und Integrations-Partner
- 🔧 **Troubleshooting-Guide:** Umfassende Problem-Lösungen

### **Community & Support**
- 💬 **Discussions:** [GitHub Discussions](https://github.com/Elemirus1996/Einsatzueberwachung/discussions)
- 🐛 **Bug Reports:** [GitHub Issues](https://github.com/Elemirus1996/Einsatzueberwachung/issues)
- 📧 **Direct Contact:** Über GitHub-Profile für dringende Anfragen
- 🌟 **Feature Voting:** Community-driven Feature-Priorisierung

### **Training & Schulungen**
- 📋 **Benutzer-Handbuch:** Umfassende Dokumentation für Einsatzleiter
- 🎓 **Admin-Training:** Setup und Konfiguration für IT-Administratoren
- 📱 **Mobile-Guide:** iPhone/Android-Optimierung für Feldteams
- 🔧 **Developer-Guide:** Integration und Customization

---

## ⭐ **Star History & Statistiken**

[![Stargazers over time](https://starchart.cc/Elemirus1996/Einsatzueberwachung.svg)](https://starchart.cc/Elemirus1996/Einsatzueberwachung)

**Wenn Ihnen dieses Projekt gefällt, geben Sie ihm einen Stern ⭐ auf GitHub!**

### **Projekt-Metriken**
- 📊 **Lines of Code:** ~30,000 (C#, XAML, Documentation)
- 🏗️ **Architecture:** 100% MVVM mit 80+ ViewModels und Services
- 🧪 **Quality:** Enterprise-Grade mit umfassendem Error-Handling
- 🌍 **Einsätze:** Bereits in realen Rettungseinsätzen bewährt
- 📱 **Mobile:** Cross-Platform-Kompatibilität getestet

---

**🚁 Einsatzüberwachung Professional v1.9.6 - Wo Technologie Leben rettet! 🧡**

*Professionelle Software für professionelle Retter - Made with ❤️ and ☕ by RescueDog_SW*

---

**© 2024 RescueDog_SW - Alle Rechte vorbehalten**
