# 🔧 HTTP Error 400 - Troubleshooting Guide

## 🚨 **HTTP Error 400 - Bad Request**

### **Häufige Ursachen & Lösungen**

#### **1. 🌐 URL-Format Probleme**

**Problem:** Browser zeigt "HTTP Error 400"
**Ursache:** Falsche URL oder ungültiges Format

**✅ Lösungen:**
```
Korrekte URLs:
✅ http://localhost:8080/mobile
✅ http://192.168.1.100:8080/mobile
✅ http://127.0.0.1:8080/mobile

Falsche URLs:
❌ http://localhost:8080 (ohne /mobile)
❌ https://localhost:8080/mobile (HTTPS nicht unterstützt)
❌ http://localhost/mobile (ohne Port)
```

#### **2. 🛡️ Firewall & Port-Blockierung**

**Problem:** Verbindung wird abgelehnt
**Ursache:** Windows Firewall blockiert Port 8080

**✅ Lösung:**
1. **Windows-Taste + R** → `wf.msc`
2. **Eingehende Regeln** → **Neue Regel**
3. **Port** → **TCP** → **8080**
4. **Verbindung zulassen** → **Regel erstellen**

#### **3. 🔑 Administrator-Rechte fehlen**

**Problem:** Server startet nur mit localhost
**Ursache:** Keine Admin-Rechte für Netzwerk-Binding

**✅ Lösung:**
1. **Einsatzüberwachung.exe** → **Rechtsklick**
2. **"Als Administrator ausführen"**
3. **UAC-Dialog** → **"Ja"** klicken
4. **Mobile Server** neu starten

#### **4. 📱 Browser-Cache Probleme**

**Problem:** Alte/defekte Seite wird geladen
**Ursache:** Browser-Cache enthält veraltete Daten

**✅ Lösung für iPhone Safari:**
1. **Einstellungen** → **Safari**
2. **Verlauf und Website-Daten löschen**
3. **Löschen bestätigen**
4. **URL erneut eingeben**

**✅ Lösung für Desktop-Browser:**
1. **Strg + F5** (Hard Refresh)
2. **Entwicklertools** → **Netzwerk** → **Cache deaktivieren**
3. **Inkognito-Modus** testen

---

## 🔍 **Erweiterte Diagnose**

### **Schritt 1: Server-Status prüfen**

**Desktop-Anwendung:**
1. **Mobile Verbindung** öffnen
2. **Status-Indikator** prüfen:
   - 🟢 **Grün**: Server läuft korrekt
   - 🔴 **Rot**: Server gestoppt
   - 🟡 **Orange**: Server-Fehler

3. **Server-Info** beachten:
```
✅ Status: Mobile Server gestartet auf 192.168.1.100:8080
❌ Status: Mobile Server gestartet (localhost-only)
❌ Status: Server-Start fehlgeschlagen
```

### **Schritt 2: Netzwerk-Konnektivität testen**

**Lokaler Test (Desktop-Browser):**
1. **Browser** öffnen → `http://localhost:8080/mobile`
2. **Erwartung**: Mobile Web-App lädt
3. **Fehler**: Prüfen Sie Server-Logs im Mobile Connection Window

**Netzwerk-Test (iPhone/Android):**
1. **Gleiche WLAN-Verbindung** sicherstellen
2. **Netzwerk-IP** verwenden (nicht localhost)
3. **URL**: `http://[Desktop-IP]:8080/mobile`

### **Schritt 3: API-Endpoints testen**

**API-Status prüfen:**
```
http://localhost:8080/api/status
→ Erwartung: JSON mit Server-Informationen

http://localhost:8080/api/teams  
→ Erwartung: JSON Array mit Team-Daten
```

**Manueller Test im Browser:**
1. **Desktop**: URL in Browser eingeben
2. **JSON-Response** erwarten
3. **HTTP 400**: Server-Problem
4. **Timeout**: Netzwerk-Problem

---

## 🛠️ **Schritt-für-Schritt Fehlerbehebung**

### **Level 1: Basis-Checks**

```
✅ Checkliste:
□ Einsatzüberwachung-App läuft
□ Mobile Server ist gestartet (grüner Status)
□ Windows Firewall-Ausnahme für Port 8080
□ Desktop und iPhone im gleichen WLAN
□ Korrekte URL mit /mobile Endung
```

### **Level 2: Administrator-Setup**

```
✅ Erweiterte Checkliste:
□ App als Administrator gestartet
□ Netzwerk-IP wird angezeigt (nicht localhost)
□ QR-Code wird korrekt generiert
□ Browser-Test auf Desktop erfolgreich
□ API-Endpoints antworten mit JSON
```

### **Level 3: Detaillierte Diagnose**

```
✅ Expert-Level:
□ Windows Event Log auf Firewall-Blocks prüfen
□ Netzwerk-Scanner (netstat -an | findstr 8080)
□ Wireshark für Packet-Analyse
□ Alternative Port testen (8081, 8082)
□ IIS/Apache Konflikte ausschließen
```

---

## 📱 **Device-spezifische Probleme**

### **iPhone Safari:**

**Problem:** Leere weiße Seite
**Lösung:**
1. **Private-Modus** testen
2. **JavaScript aktiviert** prüfen
3. **Content-Blocker deaktivieren**
4. **iOS-Version** prüfen (mindestens iOS 12)

**Problem:** "Diese Webseite kann nicht geöffnet werden"
**Lösung:**
1. **WLAN-Verbindung** überprüfen
2. **URL korrekt eingeben** (ohne Tippfehler)
3. **http://** Prefix explizit verwenden

### **Android Chrome:**

**Problem:** "Diese Website ist nicht erreichbar"
**Lösung:**
1. **Chrome aktualisieren**
2. **HTTP-Seiten erlauben** (Chrome-Einstellungen)
3. **Data Saver deaktivieren**

---

## 🔧 **Häufige Konfigurationsfehler**

### **1. Port bereits in Verwendung**

**Fehlermeldung:** "Port 8080 is already in use"
**Lösung:**
1. **Andere Apps** schließen (Skype, Jenkins, etc.)
2. **Process finden:** `netstat -ano | findstr :8080`
3. **Process beenden:** `taskkill /PID [ProcessID] /F`
4. **Alternative Port** verwenden (Code-Änderung nötig)

### **2. Antivirus-Interferenz**

**Problem:** Verbindung wird blockiert
**Lösung:**
1. **Windows Defender** → **Ausnahme hinzufügen**
2. **Antivirus-Software** temporär deaktivieren
3. **Netzwerk-Scan** deaktivieren

### **3. Router-Konfiguration**

**Problem:** Geräte können sich nicht verbinden
**Lösung:**
1. **Client-Isolation deaktivieren** (Router-Einstellungen)
2. **Guest-Netzwerk vermeiden**
3. **Router neu starten**

---

## 📊 **Performance-Optimierung**

### **Langsame Ladezeiten:**

1. **WLAN-Signal** verbessern
2. **Background-Apps** schließen
3. **Browser-Cache** leeren
4. **Auto-Refresh** Intervall erhöhen

### **Verbindungsabbrüche:**

1. **Energiespareinstellungen** prüfen
2. **WLAN-Sleep** deaktivieren
3. **Keep-Alive** verwenden

---

## 🆘 **Notfall-Lösungen**

### **Wenn gar nichts funktioniert:**

1. **Alternative URL-Eingabe:**
   - URL per WhatsApp/E-Mail senden
   - QR-Code per Screenshot teilen
   - Manuelle IP-Adresse ermitteln (`ipconfig`)

2. **Fallback-Lösungen:**
   - **Screenshot-Sharing** der Desktop-App
   - **Handy-Kamera** für Desktop-Monitor
   - **Voice-Communication** für Updates

3. **Expert-Support:**
   - **Systeminfo sammeln** (ipconfig, netstat)
   - **Error-Logs exportieren**
   - **Screenshots der Fehlermeldungen**

---

## 📞 **Kontakt & Support**

**Bei anhaltenden Problemen:**
- 📄 **Logs sammeln** aus Mobile Connection Window
- 📸 **Screenshots** der Fehlermeldungen
- 🌐 **Netzwerk-Konfiguration** dokumentieren
- 💻 **System-Informationen** (Windows-Version, etc.)

**Self-Service Checks:**
- ✅ Server läuft und ist grün
- ✅ URL ist korrekt (mit /mobile)
- ✅ Beide Geräte im gleichen WLAN
- ✅ Firewall-Ausnahme für Port 8080
- ✅ Administrator-Rechte verwendet

**🔧 Die meisten HTTP 400 Fehler sind Konfigurationsprobleme - nicht Code-Bugs!**

---

**📱 Nach erfolgreicher Fehlerbehebung sollte die Mobile-App problemlos funktionieren.**
