# 📊 Stammdatenverwaltung - Dokumentation

## Übersicht

Die Stammdatenverwaltung in Version 1.7 ermöglicht es, Personal und Hunde zentral zu verwalten und diese Daten schnell bei der Einsatzerfassung zu verwenden.

## Features

### 👤 Personal-Verwaltung

**Erfasste Daten:**
- Vorname und Nachname
- Mehrere Fähigkeiten gleichzeitig möglich:
  - 🎯 Hundeführer (HF)
  - 🤝 Helfer (H)
  - 📋 Führungsassistent (FA)
  - 👥 Gruppenführer (GF)
  - 🚀 Zugführer (ZF)
  - 👨‍✈️ Verbandsführer (VF)
  - 🚁 Drohnenpilot (DP)
- Notizen
- Aktiv/Inaktiv Status

**Funktionen:**
- Neu anlegen, Bearbeiten, Löschen
- Filterung nach Fähigkeiten
- Übersicht aller aktiven Personen

### 🐕 Hunde-Verwaltung

**Erfasste Daten:**
- Name des Hundes
- Rasse
- Alter
- Mehrere Spezialisierungen gleichzeitig möglich:
  - 🔵 Flächensuchhund (FL)
  - 🟠 Trümmersuchhund (TR)
  - 🟢 Mantrailer (MT)
  - 🔷 Wasserortung (WO)
  - 🟣 Lawinensuchhund (LA)
  - 🟩 Geländesuchhund (GE)
  - 🟤 Leichenspürhund (LS)
- Zugeordneter Hundeführer (Referenz zu Personal)
- Notizen
- Aktiv/Inaktiv Status

**Funktionen:**
- Neu anlegen, Bearbeiten, Löschen
- Filterung nach Spezialisierungen
- Automatische Verknüpfung mit Hundeführer
- Übersicht aller aktiven Hunde

## Verwendung

### Stammdaten verwalten

1. **Öffnen:** Menü (☰) → "Stammdaten (Personal & Hunde)..."
2. **Tabs:**
   - 👤 Personal - Verwaltung aller Personen
   - 🐕 Hunde - Verwaltung aller Hunde
   - 📊 Übersicht - Statistiken über Stammdaten

### Personal hinzufügen

1. Tab "👤 Personal" auswählen
2. Button "➕ Neu" klicken
3. Eingaben:
   - Vorname und Nachname (Pflichtfelder)
   - Mindestens eine Fähigkeit auswählen
   - Optional: Notizen
   - Aktiv-Status (Standard: Ja)
4. "✓ Speichern" klicken

### Hund hinzufügen

1. Tab "🐕 Hunde" auswählen
2. Button "➕ Neu" klicken
3. Eingaben:
   - Name (Pflichtfeld)
   - Optional: Rasse, Alter
   - Mindestens eine Spezialisierung auswählen
   - Optional: Hundeführer aus Liste wählen
   - Optional: Notizen
   - Aktiv-Status (Standard: Ja)
4. "✓ Speichern" klicken

### Teams mit Stammdaten erstellen

1. Haupt-Fenster: "+ Team" klicken
2. **Hund auswählen:**
   - Aus Dropdown-Liste einen bereits erfassten Hund wählen
   - ODER "Manuell eingeben" wählen und Namen eintippen
   - Bei Auswahl aus Liste: Hundeführer wird automatisch vorgeschlagen
3. **Hundeführer auswählen:**
   - Aus Dropdown-Liste wählen (nur Personen mit Hundeführer-Fähigkeit)
   - ODER manuell eingeben
4. **Helfer auswählen (optional):**
   - Aus Dropdown-Liste wählen (nur Personen mit Helfer-Fähigkeit)
   - ODER manuell eingeben
5. "Weiter zu Spezialisierung" klicken
6. Team-Spezialisierung(en) auswählen
7. Team wird erstellt mit Name "Team [Hundename]"

## Datenspeicherung

Die Stammdaten werden persistent gespeichert unter:
```
%LOCALAPPDATA%\Einsatzueberwachung\MasterData\
├── personal.json    # Personal-Daten
└── dogs.json        # Hunde-Daten
```

### Datenformat

**Personal (personal.json):**
```json
{
  "Id": "guid",
  "Vorname": "Max",
  "Nachname": "Mustermann",
  "Skills": 3,  // Bitwise Flags: Hundefuehrer + Helfer
  "Notizen": "Erfahrener Hundeführer",
  "IsActive": true
}
```

**Hunde (dogs.json):**
```json
{
  "Id": "guid",
  "Name": "Rex",
  "Rasse": "Deutscher Schäferhund",
  "Alter": 5,
  "Specializations": 5,  // Bitwise Flags: Flaechensuche + Mantrailing
  "HundefuehrerId": "guid-of-handler",
  "Notizen": "Zertifiziert seit 2020",
  "IsActive": true
}
```

## Mehrfachauswahl (Flags)

Sowohl Fähigkeiten als auch Spezialisierungen verwenden **Bitwise Flags**, sodass mehrere Werte gleichzeitig gespeichert werden können:

### Personal Skills (Bit-Werte):
- Hundefuehrer: 1
- Helfer: 2
- Fuehrungsassistent: 4
- Gruppenfuehrer: 8
- Zugfuehrer: 16
- Verbandsfuehrer: 32
- Drohnenpilot: 64

**Beispiel:** Eine Person mit Hundeführer + Helfer = 1 + 2 = **3**

### Hunde-Spezialisierungen (Bit-Werte):
- Flaechensuche: 1
- Truemmersuche: 2
- Mantrailing: 4
- Wasserortung: 8
- Lawinensuche: 16
- Gelaendesuche: 32
- Leichensuche: 64

**Beispiel:** Ein Hund mit Flächen + Trümmer = 1 + 2 = **3**

## Vorteile

✅ **Schnellere Einsatzerfassung:** Auswahl statt Tippen
✅ **Konsistente Daten:** Keine Tippfehler
✅ **Übersichtlich:** Alle Ressourcen auf einen Blick
✅ **Flexibel:** Manuelle Eingabe weiterhin möglich
✅ **Mehrfachfähigkeiten:** Realistische Abbildung von Multi-Skills
✅ **Verknüpfungen:** Hunde können Hundeführern zugeordnet werden

## Integration mit bestehendem System

Die Stammdatenverwaltung ist **optional**:
- Teams können weiterhin komplett manuell erstellt werden
- Stammdaten-Auswahl ist ein zusätzliches Feature
- Keine Änderung an bestehenden Export-Formaten
- Abwärtskompatibel mit älteren Versionen

## API / Service

```csharp
// Zugriff auf MasterDataService
var service = MasterDataService.Instance;

// Daten laden (beim Start)
await service.LoadDataAsync();

// Personal abrufen
var allPersonal = service.PersonalList;
var activePersonal = service.GetActivePersonal();
var hundefuehrer = service.GetPersonalBySkill(PersonalSkills.Hundefuehrer);

// Hunde abrufen
var allDogs = service.DogList;
var activeDogs = service.GetActiveDogs();
var flaechenhunde = service.GetDogsBySpecialization(DogSpecialization.Flaechensuche);

// Hinzufügen
service.AddPersonal(personalEntry);
service.AddDog(dogEntry);

// Aktualisieren
service.UpdatePersonal(personalEntry);
service.UpdateDog(dogEntry);

// Löschen
service.RemovePersonal(id);
service.RemoveDog(id);

// Automatisches Speichern nach jeder Änderung
```

## Zukünftige Erweiterungen

💡 Mögliche Features für zukünftige Versionen:
- Import/Export von Stammdaten (CSV, Excel)
- Foto-Upload für Personal und Hunde
- Verfügbarkeitskalender
- Qualifikations-/Zertifikats-Verwaltung
- Team-History (welche Hunde/Handler haben zusammengearbeitet)
- Statistiken pro Person/Hund

## Troubleshooting

**Problem:** Stammdaten werden nicht geladen
- **Lösung:** Prüfen Sie die Logs unter `%LOCALAPPDATA%\Einsatzueberwachung\Logs\`
- Stellen Sie sicher, dass Schreibrechte für den Ordner vorhanden sind

**Problem:** ComboBox zeigt keine Einträge
- **Lösung:** Zuerst Stammdaten über Menü → Stammdaten anlegen
- Falls vorhanden: Prüfen Sie den "Aktiv"-Status der Einträge

**Problem:** Änderungen werden nicht gespeichert
- **Lösung:** Prüfen Sie Speicherplatz und Zugriffsrechte
- Logs überprüfen für Fehlerdetails

## Version History

- **v1.7.0:** Initiale Implementierung der Stammdatenverwaltung
  - Personal-Verwaltung mit Mehrfach-Fähigkeiten
  - Hunde-Verwaltung mit Mehrfach-Spezialisierungen
  - Integration in Team-Erstellung
  - Persistente JSON-Speicherung
