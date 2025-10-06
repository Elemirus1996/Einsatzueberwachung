# Feature-Highlight System

## Übersicht
Das Feature-Highlight System zeigt neue Features nur bei den ersten 3 Anwendungsstarts einer Version an, um Benutzer nicht mit sich wiederholenden Informationen zu belästigen.

## Funktionsweise

### Automatisches Verhalten
- **Neue Version**: Bei einer neuen Version wird der Zähler zurückgesetzt und das Feature-Highlight wird wieder angezeigt
- **Erste 3 Starts**: Das Feature-Highlight wird bei den ersten 3 Starts der aktuellen Version angezeigt
- **Nach 3 Starts**: Das Feature-Highlight wird automatisch ausgeblendet bis eine neue Version installiert wird

### Benutzerinteraktion
- **Dismiss-Button**: Benutzer können das Feature-Highlight manuell schließen (X-Button oben rechts)
- **Automatisches Markieren**: Jede Anzeige wird automatisch als "angezeigt" markiert
- **Settings-Integration**: Status und Reset-Funktion sind in den Einstellungen verfügbar

## Technische Implementierung

### Services
- **FeatureHighlightService**: Zentrale Verwaltung der Feature-Highlight-Logik
- **Speicherort**: `%LocalAppData%\Einsatzueberwachung\Settings\feature_highlight.json`

### Datenstruktur
```json
{
  "LastSeenVersion": "1.9.0",
  "ShowCount": 2,
  "LastShownAt": "2024-01-15T10:30:00",
  "CreatedAt": "2024-01-10T08:00:00"
}
```

### ViewModel-Integration
- **StartViewModel**: 
  - Property `ShowFeatureHighlight` für UI-Binding
  - Command `DismissFeatureHighlightCommand` für manuelles Schließen
  - Automatische Initialisierung beim Start

### UI-Integration
- **StartWindow.xaml**: 
  - Conditional Visibility basierend auf `ShowFeatureHighlight`
  - Dismiss-Button mit Command-Binding
  - Hinweis auf begrenzte Anzeige (3×)

## Vorteile

### Benutzerfreundlichkeit
- ✅ Keine nervenden, sich wiederholenden Popups
- ✅ Benutzer werden über neue Features informiert
- ✅ Automatisches Ausblenden nach wenigen Anzeigen
- ✅ Manuelle Schließmöglichkeit

### Entwicklerfreundlichkeit
- ✅ Einfache Integration in bestehende ViewModels
- ✅ Zentrale Konfiguration
- ✅ Versionierung automatisch durch VersionService
- ✅ Debugging-Möglichkeiten in Settings

### Wartbarkeit
- ✅ Persistente Speicherung der Einstellungen
- ✅ Fehlerbehandlung und Logging
- ✅ Reset-Funktion für Tests/Debugging
- ✅ Status-Anzeige für Administratoren

## Verwendung für Entwickler

### Neue Version veröffentlichen
1. Versionsnummer in `VersionService.cs` erhöhen
2. Feature-Highlights in StartWindow.xaml aktualisieren
3. Das System erkennt automatisch die neue Version und zeigt das Highlight wieder an

### Debugging/Testing
1. Einstellungen-Fenster öffnen
2. In der Info-Sektion den Feature-Highlight Status prüfen
3. "Feature-Highlight zurücksetzen" Button verwenden
4. Anwendung neu starten um Test durchzuführen

### Für andere Fenster verwenden
```csharp
// In einem beliebigen ViewModel:
public bool ShowFeatureHighlight => 
    FeatureHighlightService.Instance.ShouldShowFeatureHighlight();

// Bei Anzeige markieren:
FeatureHighlightService.Instance.MarkAsShown();
```

## Logging
Alle Feature-Highlight-Aktionen werden geloggt:
- Anzeige-Status beim Start
- Manuelle Dismiss-Aktionen
- Version-Wechsel
- Reset-Aktionen
- Fehler bei Speicherung/Laden

## Dateipfad
```
%LocalAppData%\Einsatzueberwachung\Settings\feature_highlight.json
```

Diese Datei wird automatisch erstellt und verwaltet. Manuelle Änderungen sind möglich, aber nicht empfohlen.
