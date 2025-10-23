# MapWindow - Suchgebiete-Verwaltung

## Übersicht
Das MapWindow ermöglicht die Verwaltung von Suchgebieten auf einer interaktiven Karte mit folgenden Features:

## Features
- 🗺️ **Interaktive Karte** mit OpenStreetMap und Satelliten-Ansicht
- ✏️ **Suchgebiete zeichnen** durch Klicken auf die Karte
- 👥 **Team-Zuordnung** für jedes Suchgebiet
- 📍 **Adresssuche** mit automatischem Zoom zur Einsatzstelle
- 🎨 **Orange-Design** konsistent mit der Haupt-Anwendung
- 📝 **Notizen** für jedes Suchgebiet
- ✔️ **Status-Tracking** (abgesucht ja/nein)
- 🖨️ **Druck-Funktion** für Suchgebiete-Übersicht

## Installation
Die folgenden NuGet-Pakete wurden hinzugefügt:
- Mapsui 5.0.0-beta.3
- Mapsui.Wpf 5.0.0-beta.3
- NetTopologySuite 2.5.0

## Nutzung

### Im StartWindow
1. Geben Sie die genaue Adresse des Einsatzortes ein (z.B. "Hauptstraße 1, 12345 Musterstadt")
2. Klicken Sie auf "🗺️ Karte öffnen"
3. Die Karte öffnet sich und zoomt automatisch zur angegebenen Adresse

### Im MapWindow
1. **Adresse suchen**: Geben Sie eine neue Adresse ein und klicken Sie auf "🔍 Suchen"
2. **Suchgebiet zeichnen**:
   - Klicken Sie auf "✏️ Zeichnen starten"
   - Klicken Sie auf mindestens 3 Punkte auf der Karte
   - Klicken Sie auf "✔️ Fertig" um das Gebiet zu erstellen
3. **Kartenansicht wechseln**: Klicken Sie auf das Karten-Icon um zwischen Straßenkarte und Satelliten-Ansicht zu wechseln
4. **Suchgebiet bearbeiten**:
   - Name ändern
   - Team zuordnen
   - Als "Abgesucht" markieren
   - Notizen hinzufügen
5. **Karte drucken**: Klicken Sie auf "🖨️" um die Karte mit allen Suchgebieten zu drucken

## Bekannte Probleme
- **Build-Fehler**: Es gibt einen Kompilierungsfehler in `MapViewModel.cs` bei Zeile 483 (duplizierte Methode `ExecuteSelectArea`)
- **Lösung**: Entfernen Sie in `ViewModels/MapViewModel.cs` die zweite Definition der Methode `ExecuteSelectArea` bei Zeile 483-490

### Fix für Build-Fehler
Öffnen Sie `ViewModels/MapViewModel.cs` und löschen Sie die folgenden Zeilen (ca. Zeile 483-490):

```csharp
private void ExecuteSelectArea(SearchArea area)
{
    if (area == null) return;
    
    SelectedArea = area;
    LoggingService.Instance.LogInfo($"Search area selected: {area.Name}");
}
```

Die erste Definition bei Zeile 381 ist korrekt und sollte beibehalten werden.

##Architektur
- **MapViewModel**: ViewModel für Karten-Logik und Suchgebiete-Verwaltung
- **MapWindow**: View mit Mapsui-Karten-Control
- **SearchArea Model**: Datenmodell für Suchgebiete mit Koordinaten, Team-Zuordnung, etc.
- **Converter**: `ColorToBrushConverter`, `BoolToMapTypeConverter`, `BoolToDrawingStatusConverter`

## Zukünftige Erweiterungen
- [ ] Druckfunktion implementieren
- [ ] GPS-Import für Suchgebiete
- [ ] Export von Suchgebieten als GPX/KML
- [ ] Offline-Karten für Bereiche ohne Internet
- [ ] Echtzeit-Team-Positionen auf der Karte
- [ ] Heatmap für abgesuchte Bereiche

## Integration mit Hauptanwendung
Das MapWindow kann aus jedem Einsatzfenster geöffnet werden und die Suchgebiete werden in `EinsatzData` gespeichert für spätere Verwendung im PDF-Export.
