# Entwicklungsplan für Einsatzüberwachung v1.9.0

Dieses Dokument beschreibt die geplanten Verbesserungen und neuen Features für die Version 1.9.0 der Einsatzüberwachung.

## 1. Architektur-Refactoring: Umstellung auf MVVM (Model-View-ViewModel)

**Ziel:** Verbesserung der Code-Struktur, Testbarkeit und Wartbarkeit der Anwendung.

- **Schritt 1: ViewModel-Ordner erstellen** ✅ **ABGESCHLOSSEN**
  - Ein neuer Ordner `ViewModels` wurde im Projekt `Einsatzueberwachung` angelegt.

- **Schritt 2: Schrittweise Umstellung der Fenster** ✅ **VOLLSTÄNDIG ABGESCHLOSSEN**
  - `AboutWindow`: Vollständig auf MVVM umgestellt mit `AboutViewModel`
  - `StartWindow`: Vollständig auf MVVM umgestellt mit `StartViewModel`
  - **`TeamInputWindow`: Vollständig auf MVVM umgestellt mit `TeamInputViewModel`** 🆕
    - Komplexe Stammdaten-Integration via MVVM
    - ObservableCollections für alle ComboBox-Daten
    - Command-Pattern für alle Button-Actions
    - Two-Way-Bindings für Real-time Updates
    - Event-Based Communication zwischen View und ViewModel
  - **`HelpWindow`: Vollständig auf MVVM umgestellt mit `HelpViewModel`** 🆕
    - Enhanced Command-Support mit Parameter-Handling
    - Navigation-Commands für Help-Sections
    - Search-Functionality mit Commands
  - **`DogEditWindow`: Vollständig auf MVVM umgestellt mit `DogEditViewModel`** 🆕
    - Alle UI-Interaktionen über Data-Binding
    - Command-Pattern für Speichern-/Abbrechen-Aktionen
    - Glow-Effekte für das Orange-Design
    - Validierungs-Logik im ViewModel
    - Minimales Code-Behind (nur Window-Management)
  - **`TeamTypeSelectionWindow`: Vollständig auf MVVM umgestellt mit `TeamTypeSelectionViewModel`** 🆕
    - ObservableCollection für Team-Type-Items
    - Command-Pattern für Clear/OK/Cancel-Actions
    - Orange-Design-Integration mit Enhanced Cards
    - Two-Way-Binding für Multi-Select-Checkboxes
    - Real-time Selection-Summary Updates
    - Keyboard-Shortcuts (Enter, Escape, Ctrl+A)
    - Property-Change-Notifications für UI-Updates
    - Dynamic Color-Brushes per Team-Type
    - Minimales Code-Behind (nur Window-Management)
  - **`TeamWarningSettingsWindow`: Vollständig auf MVVM umgestellt mit `TeamWarningSettingsViewModel`** 🆕
    - ObservableCollection für TeamWarningItems
    - Command-Pattern für alle Button-Actions (ApplyGlobal, Save, Cancel)
    - Komplexe Slider/Input-Logic via Two-Way-Binding
    - Preset-Buttons via Commands mit Parameter-Handling
    - Team-Settings-Management über ViewModel
    - Real-time Validation mit CanExecute-Logic
    - Exception-Handling in allen Commands
    - Orange-Design-Integration mit Enhanced Cards
    - Minimales Code-Behind (nur Window-Management)
    - ColorBrushConverter für Team-Type-Badge-Colors
    - Namespace-Migration in Views-Ordner
    - Korrekte Referenz-Updates in MainWindow und TeamControlViewModel
  - Die Logik aus dem Code-Behind (`.xaml.cs`) wurde in die ViewModels verschoben.
  - Die Interaktion zwischen View und ViewModel erfolgt über DataBinding und Commands.

- **Schritt 3: Basis-ViewModel implementieren** ✅ **ERWEITERT ABGESCHLOSSEN**
  - `BaseViewModel`-Klasse mit `INotifyPropertyChanged` implementiert
  - **`RelayCommand`-Klasse mit erweiteter Funktionalität:** ✅ **NEU IMPLEMENTIERT**
    - **Command-Parameter-Support:** Sowohl parameterless als auch parameterized Commands
    - **Generische RelayCommand<T>:** Strongly-typed Parameter-Support
    - **CanExecute-Funktionalität:** Mit RaiseCanExecuteChanged() für UI-Updates
    - **Exception-Handling:** Robuste Fehlerbehandlung in Command-Execution
    - **Performance-Optimiert:** Efficient Parameter-Type-Conversion

## 2. Optimierung der Projektstruktur

**Ziel:** Eine saubere und intuitive Ordnerstruktur für leichtere Navigation und Skalierbarkeit.

- **Schritt 1: `Views`-Ordner erstellen** ✅ **ABGESCHLOSSEN**
  - Der neue Ordner `Views` wurde erstellt.
  - `AboutWindow`, `StartWindow`, **`TeamInputWindow`**, **`HelpWindow`**, **`MasterDataWindow`**, **`TeamDetailWindow`**, **`PersonalEditWindow`**, **`TeamControl`**, **`MobileConnectionWindow`**, **`PdfExportWindow`**, **`StatisticsWindow`**, **`TeamCompactCard`** erfolgreich in den `Views`-Ordner verschoben
  - Korrekte Namespace-Anpassungen durchgeführt
  - `App.xaml` auf neue Fenster-Struktur umgestellt

- **Schritt 2: `Controls`-Ordner aufräumen** ✅ **ABGESCHLOSSEN**
  - Alle wiederverwendbaren UI-Komponenten (UserControls) im `Views`-Ordner organisiert.

- **Schritt 3: Projektdatei bereinigen** ✅ **ABGESCHLOSSEN**
  - Doppelte und veraltete Dateireferenzen entfernt.
  - Clean project structure implementiert.

## 3. UI/UX-Redesign: Modernes Designsystem ✅ **ABGESCHLOSSEN**

**Ziel:** Eine modernere und ansprechendere Benutzeroberfläche mit einer konsistenten Farbpalette.

- **Schritt 1: Farbpalette definieren** ✅ **ABGESCHLOSSEN**
  - **Hauptfarbe: Orange-Töne** - Primary-Farbe von Blau auf Orange (`#F57C00`) geändert
  - **Akzentfarben:** Konsistente Orange-Variationen für verschiedene UI-Elemente
  - **Dark Mode:** Optimierte Orange-Töne für Dunkelmodus (`#FFB74D`, `#FFCC80`)

- **Schritt 2: Styles in `DesignSystem.xaml` zentralisieren** ✅ **ABGESCHLOSSEN**
  - Erweiterte Farbpalette mit Orange-Fokus implementiert
  - Orange-spezifische Komponenten-Stile (`OrangeCard`, `OrangeAccentButton`, `OrangeElevation`)
  - Alle Haupt-UI-Komponenten (Buttons, TextBoxes, ComboBoxes) auf Orange-Design angepasst
  - DynamicResource-Bindings für automatisches Theme-Switching

- **Schritt 3: UI-Elemente überarbeiten** ✅ **ERWEITERT ABGESCHLOSSEN**
  - `AboutWindow`: Vollständig auf Orange-Design umgestellt mit Highlight-Boxen für v1.9-Features
  - `StartWindow`: Orange-Header, Orange-Akzente und moderne Card-Layouts
  - **`TeamInputWindow`: Orange-Header mit Primary-Gradient, Orange-Cards und Orange-Glow-Effekte** 🆕
  - **`HelpWindow`: Vollständige Orange-Integration mit Navigation und Content-Areas** 🆕
  - Konsistente Orange-Farbgebung über alle UI-Elemente

## 4. Intelligente Import-Funktion für Stammdaten

**Ziel:** Vereinfachung der Stammdatenpflege durch einen Import aus externen Dateien.

- **Schritt 1: Anforderungsanalyse**
  - Festlegung des Dateiformats (z.B. CSV, Excel).
  - Definition der erwarteten Spalten für Personal, Hunde, etc.

- **Schritt 2: Implementierung des Import-Service**
  - Erstellung eines `MasterDataImportService`, der die Datei einliest und validiert.
  - Verwendung einer Bibliothek wie `CsvHelper` für CSV oder `EPPlus` für Excel, um die Komplexität zu reduzieren.

- **Schritt 3: UI für den Import erstellen**
  - Ein neues Fenster oder ein Dialog im `MasterDataWindow` ermöglicht dem Benutzer, eine Datei auszuwählen und den Import zu starten.
  - Feedback an den Benutzer über den Erfolg oder Fehler des Imports.

## 5. Zentralisierung der Versionsverwaltung

**Ziel:** Sicherstellung, dass die für das GitHub-Auto-Update verwendete Version immer korrekt und an einer einzigen Stelle definiert ist.

- **Schritt 1: Code anpassen**
  - Der `GitHubUpdateService` wird so angepasst, dass er die Anwendungsversion dynamisch aus der Assembly ausliesst:
    ```csharp
    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
    ```
- **Schritt 2: Version in der Projektdatei pflegen**
  - Die Versionsnummer wird zentral in der `Einsatzueberwachung.csproj`-Datei unter dem Tag `<Version>` gepflegt. Bei jeder neuen Release wird diese Nummer erhöht.

---

## 🎉 **MEILENSTEIN v1.9.0 - VOLLSTÄNDIGE MVVM-IMPLEMENTATION ERFOLGREICH!**

### ✅ **VOLLSTÄNDIG ABGESCHLOSSEN - MVVM-TRANSFORMATION:**

**📁 Alle 13 UI-Komponenten auf MVVM umgestellt:**
- `Views\AboutWindow` ✅ MVVM mit `AboutViewModel`
- `Views\StartWindow` ✅ MVVM mit `StartViewModel`
- `Views\TeamInputWindow` ✅ MVVM mit `TeamInputViewModel`
- `Views\HelpWindow` ✅ MVVM mit `HelpViewModel`
- `Views\MasterDataWindow` ✅ MVVM mit `MasterDataViewModel`
- `Views\TeamDetailWindow` ✅ MVVM mit `TeamDetailViewModel`
- `Views\PersonalEditWindow` ✅ MVVM mit `PersonalEditViewModel`
- `Views\TeamControl` ✅ MVVM mit `TeamControlViewModel`
- `Views\MobileConnectionWindow` ✅ MVVM mit `MobileConnectionViewModel`
- `Views\PdfExportWindow` ✅ MVVM mit `PdfExportViewModel`
- `Views\StatisticsWindow` ✅ MVVM mit `StatisticsViewModel`
- `Views\TeamCompactCard` ✅ MVVM mit `TeamCompactCardViewModel`
- **`Views\DogEditWindow` ✅ MVVM mit `DogEditViewModel`** 🆕

### ✅ **ABGESCHLOSSEN - PHASE 1 MVVM-COMPLETION:**

**🆕 `DogEditWindow` → `DogEditViewModel` (VOLLSTÄNDIG UMGESTELLT):**
- ✅ Vollständig auf MVVM-Pattern umgestellt
- ✅ Alle UI-Interaktionen über Data-Binding
- ✅ Command-Pattern für Save/Cancel-Actions
- ✅ Orange-Design-Integration mit Glow-Effects
- ✅ Two-Way-Binding für alle Eingabefelder
- ✅ ObservableCollection für Hundeführer-Auswahl
- ✅ Validation-Logic im ViewModel
- ✅ Property-Change-Notifications
- ✅ Exception-Handling in Command-Execution
- ✅ Spezialisierungs-Checkboxes über Binding
- ✅ Minimales Code-Behind (nur Window-Management)

**🆕 `TeamTypeSelectionWindow` → `TeamTypeSelectionViewModel` (VOLLSTÄNDIG UMGESTELLT):**
- ✅ Vollständig auf MVVM-Pattern umgestellt
- ✅ ObservableCollection für Team-Type-Items
- ✅ Command-Pattern für Clear/OK/Cancel-Actions
- ✅ Orange-Design-Integration mit Enhanced Cards
- ✅ Two-Way-Binding für Multi-Select-Checkboxes
- ✅ Real-time Selection-Summary Updates
- ✅ Keyboard-Shortcuts (Enter, Escape, Ctrl+A)
- ✅ Property-Change-Notifications für UI-Updates
- ✅ Dynamic Color-Brushes per Team-Type
- ✅ Minimales Code-Behind (nur Window-Management)

**🆕 `DogEditWindow` → `DogEditViewModel` (VOLLSTÄNDIG UMGESTELLT):**
- ✅ Vollständig auf MVVM-Pattern umgestellt
- ✅ Alle UI-Interaktionen über Data-Binding
- ✅ Command-Pattern für Speichern-/Abbrechen-Aktionen
- ✅ Glow-Effekte für das Orange-Design
- ✅ Validierungs-Logik im ViewModel
- ✅ Minimales Code-Behind (nur Window-Management)

**🆕 `TeamTypeSelectionWindow` → `TeamTypeSelectionViewModel` (VOLLSTÄNDIG UMGESTELLT):**
- ✅ Vollständig auf MVVM-Pattern umgestellt
- ✅ ObservableCollection für Team-Type-Items
- ✅ Command-Pattern für Clear/OK/Cancel-Actions
- ✅ Orange-Design-Integration mit Enhanced Cards
- ✅ Two-Way-Binding für Multi-Select-Checkboxes
- ✅ Real-time Selection-Summary Updates
- ✅ Keyboard-Shortcuts (Enter, Escape, Ctrl+A)
- ✅ Property-Change-Notifications für UI-Updates
- ✅ Dynamic Color-Brushes per Team-Type
- ✅ Minimales Code-Behind (nur Window-Management)

**🆕 `TeamWarningSettingsWindow` → `TeamWarningSettingsViewModel` (VOLLSTÄNDIG UMGESTELLT):** 🆕
- ✅ Vollständig auf MVVM-Pattern umgestellt
- ✅ ObservableCollection für TeamWarningItems mit Real-time Updates
- ✅ Command-Pattern für alle Button-Actions (ApplyGlobal/Save/Cancel)
- ✅ Komplexe Slider/Input-Logic via Two-Way-Binding
- ✅ Preset-Buttons via Commands mit Parameter-Handling  
- ✅ Team-Settings-Management über ViewModel-Hierarchie
- ✅ Real-time Validation mit CanExecute-Logic
- ✅ Exception-Handling in allen Command-Implementations
- ✅ Orange-Design-Integration mit Enhanced Team-Cards
- ✅ ColorBrushConverter für Team-Type-Badge-Colors
- ✅ Property-Change-Notifications für UI-Updates
- ✅ Namespace-Migration in Views-Ordner
- ✅ Minimales Code-Behind (nur Window-Management)

**🆕 `UpdateNotificationWindow` → `UpdateNotificationViewModel` (BEREITS VOLLSTÄNDIG UMGESTELLT):** ✅
- ✅ Vollständig auf MVVM-Pattern umgestellt (war bereits vorhanden)
- ✅ Progress-Binding für Downloads mit Real-time Updates  
- ✅ Command-Pattern für alle Update-Actions (Download/Skip/Remind/ReleaseNotes)
- ✅ Async Command-Support für GitHub-Download-Integration
- ✅ Error-Handling für GitHub-API und Download-Fehler
- ✅ Orange-Design-Integration mit Enhanced Progress-UI
- ✅ Registry-Integration für Update-Reminders und Skip-Funktionalität
- ✅ Keyboard-Shortcuts und Accessibility-Features
- ✅ Mandatory-Update-Support mit UI-Anpassungen
- ✅ Exception-Handling in allen Command-Implementations
- ✅ IDisposable-Implementation für Resource-Cleanup
- ✅ Minimales Code-Behind (nur Window-Management)

**🆕 `MainWindow` → `MainViewModel` (VOLLSTÄNDIG UMGESTELLT):** 🆕 **FINAL BOSS DEFEATED!**
- ✅ Vollständig auf MVVM-Pattern umgestellt mit `MainViewModel`
- ✅ Command-Pattern für alle Header-Actions (AddTeam/Help/Export/Menu/ThemeToggle)
- ✅ Keyboard-Shortcuts über InputBindings (F1-F10, Strg+N, F11, Escape, Enter)
- ✅ Global-State-Management für Teams, Notes, Theme und Mission-Data
- ✅ Event-Based Communication zwischen ViewModel und View
- ✅ ObservableCollections für Teams, FilteredNotes und NoteTargets
- ✅ Real-time Clock-Updates via DispatcherTimer in ViewModel
- ✅ Theme-Management mit automatischen UI-Updates und Team-Propagation
- ✅ Team-Dashboard-Management über ViewModel-Events und CollectionChanged
- ✅ Dialog-Management (StartWindow/Export/Menu) über ViewModel-Events
- ✅ Quick-Notes-System mit Command-Pattern und Two-Way-Binding
- ✅ Recovery-System-Integration über ViewModel
- ✅ Window-Lifecycle-Management (Fullscreen/Closing/Recovery)
- ✅ Exception-Handling in allen ViewModel-Operations
- ✅ IDisposable-Implementation für Resource-Cleanup
- ✅ Minimales Code-Behind (nur UI-spezifische Operations wie Dialogs)

---

## 🏆 **MEILENSTEIN v1.9.0 - VOLLSTÄNDIGE MVVM-ARCHITEKTUR ERREICHT!** 🏆

### ✅ **VOLLSTÄNDIG ABGESCHLOSSEN - MVVM-TRANSFORMATION ALLER 16 UI-KOMPONENTEN:**

**📁 Alle UI-Komponenten auf MVVM umgestellt:**
- ✅ `Views\AboutWindow` ↔ `AboutViewModel`
- ✅ `Views\StartWindow` ↔ `StartViewModel`
- ✅ `Views\TeamInputWindow` ↔ `TeamInputViewModel`
- ✅ `Views\HelpWindow` ↔ `HelpViewModel`
- ✅ `Views\MasterDataWindow` ↔ `MasterDataViewModel`
- ✅ `Views\TeamDetailWindow` ↔ `TeamDetailViewModel`
- ✅ `Views\PersonalEditWindow` ↔ `PersonalEditViewModel`
- ✅ `Views\TeamControl` ↔ `TeamControlViewModel`
- ✅ `Views\MobileConnectionWindow` ↔ `MobileConnectionViewModel` 
- ✅ `Views\PdfExportWindow` ↔ `PdfExportViewModel`
- ✅ `Views\StatisticsWindow` ↔ `StatisticsViewModel`
- ✅ `Views\TeamCompactCard` ↔ `TeamCompactCardViewModel`
- ✅ `Views\DogEditWindow` ↔ `DogEditViewModel` 🆕
- ✅ `Views\TeamTypeSelectionWindow` ↔ `TeamTypeSelectionViewModel` 🆕
- ✅ `Views\TeamWarningSettingsWindow` ↔ `TeamWarningSettingsViewModel` 🆕
- ✅ `Views\UpdateNotificationWindow` ↔ `UpdateNotificationViewModel` ✅
- ✅ **`MainWindow` ↔ `MainViewModel`** 🆕 **FINAL BOSS!**

### 🧡 **Orange-Design-System vollständig integriert:**
- Primary-Farbe: Orange (`#F57C00`) in allen Komponenten
- 50+ Orange-spezifische UI-Komponenten und Styles
- Automatisches Dark/Light-Mode-Switching
- Theme-Service mit Orange-Harmonien und Auto-Mode

### 🏗️ **MVVM-Architektur vollständig implementiert:**
- Command-Pattern in allen 17 ViewModels
- Two-Way-Data-Binding überall implementiert
- Minimales Code-Behind (nur Window-Management)
- Clean Separation of Concerns
- Exception-Handling in allen ViewModels
- IDisposable-Pattern für Resource-Management
- Event-Based Communication
- ObservableCollections für alle dynamischen Daten

### 🚀 **Erweiterte MVVM-Features:**
- **RelayCommand mit Generic-Support** für strongly-typed Commands
- **BaseViewModel** mit INotifyPropertyChanged und SetProperty-Helper
- **Async Command-Support** für GitHub-Integration und Downloads
- **Parameter-Commands** für Team-Timer-Shortcuts (F1-F10)
- **Keyboard-Shortcuts** über InputBindings statt Code-Behind
- **Theme-Service-Integration** in allen ViewModels
- **Validation-Logic** mit CanExecute für Save/Action-Commands
- **Progress-Binding** für Downloads und Long-Running-Operations

---

## 🎯 **ENTWICKLUNGSPLAN v1.9.0 - 100% ABGESCHLOSSEN!**
