using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    public class HelpViewModel : BaseViewModel
    {
        private string _currentContent = string.Empty;
        private string _searchText = string.Empty;
        private HelpSection _selectedSection = HelpSection.QuickStart;
        private HelpMenuItem? _selectedMenuItem;

        public ObservableCollection<HelpMenuItem> MenuItems { get; }
        public ObservableCollection<HelpMenuItem> NewFeatureItems { get; }
        public ObservableCollection<HelpMenuItem> StandardFeatureItems { get; }
        public ObservableCollection<HelpMenuItem> SystemFeatureItems { get; }

        public string CurrentContent
        {
            get => _currentContent;
            set
            {
                _currentContent = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                // Trigger search command when text changes
                ((RelayCommand)SearchCommand).RaiseCanExecuteChanged();
            }
        }

        public HelpSection SelectedSection
        {
            get => _selectedSection;
            set
            {
                _selectedSection = value;
                OnPropertyChanged();
                LoadSectionContent();
            }
        }

        public HelpMenuItem? SelectedMenuItem
        {
            get => _selectedMenuItem;
            set
            {
                _selectedMenuItem = value;
                OnPropertyChanged();
                if (value != null)
                {
                    SelectedSection = value.Section;
                }
            }
        }

        // Enhanced commands with parameter support
        public ICommand SelectSectionCommand { get; private set; } = null!;
        public ICommand SearchCommand { get; private set; } = null!;
        public ICommand ClearSearchCommand { get; private set; } = null!;
        public ICommand NavigateToSectionCommand { get; private set; } = null!;
        public ICommand CopyContentCommand { get; private set; } = null!;
        public ICommand CloseCommand { get; private set; } = null!;

        // Events für View-Kommunikation
        public event EventHandler? RequestClose;
        public event EventHandler<string>? ShowMessage;

        public HelpViewModel()
        {
            MenuItems = new ObservableCollection<HelpMenuItem>();
            NewFeatureItems = new ObservableCollection<HelpMenuItem>();
            StandardFeatureItems = new ObservableCollection<HelpMenuItem>();
            SystemFeatureItems = new ObservableCollection<HelpMenuItem>();

            InitializeCommands();
            InitializeMenuItems();
            LoadSectionContent(); // Load default content

            LoggingService.Instance?.LogInfo("HelpViewModel initialized with enhanced command support and v1.9.0 Orange design");
        }

        private void InitializeCommands()
        {
            SelectSectionCommand = new RelayCommand<object>(ExecuteSelectSection, CanExecuteSelectSection);
            SearchCommand = new RelayCommand(ExecuteSearch, CanExecuteSearch);
            ClearSearchCommand = new RelayCommand(ExecuteClearSearch, CanExecuteClearSearch);
            NavigateToSectionCommand = new RelayCommand<HelpSection>(ExecuteNavigateToSection);
            CopyContentCommand = new RelayCommand(ExecuteCopyContent, CanExecuteCopyContent);
            CloseCommand = new RelayCommand(ExecuteClose);
        }

        private void InitializeMenuItems()
        {
            // 🆕 Neue Features v1.9.0
            NewFeatureItems.Add(new HelpMenuItem 
            { 
                Section = HelpSection.TeamInputMVVM,
                Icon = "UserPlus", 
                Title = "🧡 Team-Input MVVM (NEU)",
                Description = "Vollständig überarbeitetes Team-Eingabefenster"
            });

            NewFeatureItems.Add(new HelpMenuItem 
            { 
                Section = HelpSection.OrangeDesign,
                Icon = "Palette", 
                Title = "🧡 Orange-Design-System (NEU)",
                Description = "Modernes Orange-Farbschema mit Material Design"
            });

            NewFeatureItems.Add(new HelpMenuItem 
            { 
                Section = HelpSection.DashboardGuide,
                Icon = "Dashboard", 
                Title = "📊 Dashboard-Übersicht",
                Description = "Kompakte Team-Cards in responsiver Ansicht"
            });

            NewFeatureItems.Add(new HelpMenuItem 
            { 
                Section = HelpSection.PdfExportGuide,
                Icon = "FilePdfOutline", 
                Title = "📄 Erweiterter PDF-Export",
                Description = "Professionelle Einsatz-Dokumentation"
            });

            NewFeatureItems.Add(new HelpMenuItem 
            { 
                Section = HelpSection.TeamWarningsGuide,
                Icon = "ExclamationTriangle", 
                Title = "⚠️ Team-Warnschwellen",
                Description = "Individuelle Warnzeiten pro Team"
            });

            NewFeatureItems.Add(new HelpMenuItem 
            { 
                Section = HelpSection.NotesSystemGuide,
                Icon = "StickyNoteOutline", 
                Title = "📝 Notizen-System",
                Description = "Integriertes Notizen-System im Hauptfenster"
            });

            NewFeatureItems.Add(new HelpMenuItem 
            { 
                Section = HelpSection.MasterDataGuide,
                Icon = "Database", 
                Title = "📊 Stammdatenverwaltung",
                Description = "Zentrale Personal- und Hunde-Verwaltung"
            });

            // 📚 Standard-Features
            StandardFeatureItems.Add(new HelpMenuItem 
            { 
                Section = HelpSection.Shortcuts,
                Icon = "KeyboardOutline", 
                Title = "Tastenkürzel",
                Description = "F1-F10 für Timer, Strg+Shortcuts"
            });

            StandardFeatureItems.Add(new HelpMenuItem 
            { 
                Section = HelpSection.TeamManagement,
                Icon = "Users", 
                Title = "Team-Verwaltung",
                Description = "Teams erstellen, bearbeiten und verwalten"
            });

            StandardFeatureItems.Add(new HelpMenuItem 
            { 
                Section = HelpSection.Timer,
                Icon = "ClockOutline", 
                Title = "Timer-System",
                Description = "Präzise Timer-Steuerung mit Warnungen"
            });

            StandardFeatureItems.Add(new HelpMenuItem 
            { 
                Section = HelpSection.Statistics,
                Icon = "BarChart", 
                Title = "Statistiken & Analytics",
                Description = "Einsatz-Auswertungen und Performance-Metriken"
            });

            StandardFeatureItems.Add(new HelpMenuItem 
            { 
                Section = HelpSection.AutoSave,
                Icon = "Save", 
                Title = "Auto-Speichern",
                Description = "Automatische Sicherung aller Daten"
            });

            StandardFeatureItems.Add(new HelpMenuItem 
            { 
                Section = HelpSection.Themes,
                Icon = "Adjust", 
                Title = "Themes und UI",
                Description = "Dark/Light Mode und Orange-Design"
            });

            StandardFeatureItems.Add(new HelpMenuItem 
            { 
                Section = HelpSection.Performance,
                Icon = "Tachometer", 
                Title = "Performance",
                Description = "System-Performance und Optimierungen"
            });

            StandardFeatureItems.Add(new HelpMenuItem 
            { 
                Section = HelpSection.Troubleshooting,
                Icon = "Wrench", 
                Title = "Fehlerbehebung",
                Description = "Lösungen für häufige Probleme"
            });

            StandardFeatureItems.Add(new HelpMenuItem 
            { 
                Section = HelpSection.FAQ,
                Icon = "QuestionCircleOutline", 
                Title = "Häufige Fragen",
                Description = "FAQ zu v1.9.0 Features"
            });

            // 🔧 System & Updates
            SystemFeatureItems.Add(new HelpMenuItem 
            { 
                Section = HelpSection.AutoUpdates,
                Icon = "Refresh", 
                Title = "Update-System",
                Description = "Automatische Updates und Versionsverwaltung"
            });

            SystemFeatureItems.Add(new HelpMenuItem 
            { 
                Section = HelpSection.SetupGuide,
                Icon = "Cog", 
                Title = "Installation & Setup",
                Description = "Erste Schritte und Konfiguration"
            });

            SystemFeatureItems.Add(new HelpMenuItem 
            { 
                Section = HelpSection.MobileEnhanced,
                Icon = "Mobile", 
                Title = "Mobile Funktionen",
                Description = "Smartphone-Integration und Remote-Steuerung"
            });
        }

        // Command implementations with parameter support
        private bool CanExecuteSelectSection(object? parameter)
        {
            return parameter != null;
        }

        private void ExecuteSelectSection(object? parameter)
        {
            try
            {
                if (parameter is HelpSection section)
                {
                    SelectedSection = section;
                    LoggingService.Instance?.LogInfo($"Help section selected: {section}");
                }
                else if (parameter is string sectionName && Enum.TryParse<HelpSection>(sectionName, out var parsedSection))
                {
                    SelectedSection = parsedSection;
                    LoggingService.Instance?.LogInfo($"Help section selected from string: {parsedSection}");
                }
                else if (parameter is HelpMenuItem menuItem)
                {
                    SelectedMenuItem = menuItem;
                    LoggingService.Instance?.LogInfo($"Help menu item selected: {menuItem.Title}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error selecting help section", ex);
            }
        }

        private bool CanExecuteSearch()
        {
            return !string.IsNullOrWhiteSpace(SearchText);
        }

        private void ExecuteSearch()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    SelectedSection = HelpSection.QuickStart;
                    return;
                }

                CurrentContent = GenerateSearchResults(SearchText);
                LoggingService.Instance?.LogInfo($"Help search executed: {SearchText}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error executing search", ex);
                ShowMessage?.Invoke(this, $"Fehler bei der Suche: {ex.Message}");
            }
        }

        private bool CanExecuteClearSearch()
        {
            return !string.IsNullOrWhiteSpace(SearchText);
        }

        private void ExecuteClearSearch()
        {
            try
            {
                SearchText = string.Empty;
                SelectedSection = HelpSection.QuickStart;
                LoggingService.Instance?.LogInfo("Help search cleared");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error clearing search", ex);
            }
        }

        private void ExecuteNavigateToSection(HelpSection section)
        {
            try
            {
                SelectedSection = section;
                LoggingService.Instance?.LogInfo($"Navigated to help section: {section}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error navigating to section", ex);
            }
        }

        private bool CanExecuteCopyContent()
        {
            return !string.IsNullOrWhiteSpace(CurrentContent);
        }

        private void ExecuteCopyContent()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(CurrentContent))
                {
                    System.Windows.Clipboard.SetText(CurrentContent);
                    ShowMessage?.Invoke(this, "Hilfe-Inhalt in die Zwischenablage kopiert");
                    LoggingService.Instance?.LogInfo("Help content copied to clipboard");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error copying help content", ex);
                ShowMessage?.Invoke(this, $"Fehler beim Kopieren: {ex.Message}");
            }
        }

        private void ExecuteClose()
        {
            try
            {
                LoggingService.Instance?.LogInfo("Help window closed by user");
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LoggingService.Instance?.LogError("Error closing help window", ex);
            }
        }

        private void LoadSectionContent()
        {
            CurrentContent = SelectedSection switch
            {
                HelpSection.QuickStart => GenerateQuickStartContent(),
                HelpSection.TeamInputMVVM => GenerateTeamInputMVVMContent(),
                HelpSection.OrangeDesign => GenerateOrangeDesignContent(),
                HelpSection.DashboardGuide => GenerateDashboardGuideContent(),
                HelpSection.PdfExportGuide => GeneratePdfExportGuideContent(),
                HelpSection.TeamWarningsGuide => GenerateTeamWarningsGuideContent(),
                HelpSection.NotesSystemGuide => GenerateNotesSystemGuideContent(),
                HelpSection.MasterDataGuide => GenerateMasterDataGuideContent(),
                HelpSection.Shortcuts => GenerateShortcutsContent(),
                HelpSection.TeamManagement => GenerateTeamManagementContent(),
                HelpSection.Timer => GenerateTimerContent(),
                HelpSection.Statistics => GenerateStatisticsContent(),
                HelpSection.AutoSave => GenerateAutoSaveContent(),
                HelpSection.Themes => GenerateThemesContent(),
                HelpSection.Performance => GeneratePerformanceContent(),
                HelpSection.Troubleshooting => GenerateTroubleshootingContent(),
                HelpSection.FAQ => GenerateFAQContent(),
                HelpSection.AutoUpdates => GenerateAutoUpdatesContent(),
                HelpSection.SetupGuide => GenerateSetupGuideContent(),
                HelpSection.MobileEnhanced => GenerateMobileEnhancedContent(),
                _ => GenerateQuickStartContent()
            };
        }

        private string GenerateQuickStartContent()
        {
            return @"🚀 Schnellstart Guide v1.9.0 - Orange Edition

🎯 Einsatz-Workflow v1.9.0 mit Orange-Design & MVVM
1. StartWindow: Einsatzleiter und Ort eingeben (Orange-Design!)
2. Dashboard-Übersicht: Teams in modernen Orange-Cards verwalten
3. Team-Input MVVM: Stammdaten aus Dropdown wählen oder manuell eingeben
4. Orange-Akzente: Durchgängiges Orange-Farbschema für bessere Orientierung
5. Individuelle Warnschwellen: Pro Team konfigurierbar
6. Timer-Bedienung: F1-F10 oder Dashboard-Buttons
7. Notizen-System: Direkt im Hauptfenster integriert
8. PDF-Export: Professionelle Berichte mit Orange-Branding

🧡 Haupt-Neuerungen in Version 1.9.0
📱 MVVM-Architektur - Vollständige Trennung von UI und Logik
🧡 Orange-Design-System - Modernes, konsistentes Farbschema
🎨 Material Design - Elevation-Effekte und Animationen
🔄 Two-Way-Bindings - Real-time Updates ohne Code-Behind
⚡ Command-Pattern - Saubere Event-Behandlung mit Parameter-Support
📊 Enhanced Dashboard - Noch bessere Team-Übersicht
🎯 Improved UX - Optimierte Benutzerführung

🐕 Team-Erstellung MVVM-Style
Das neue TeamInputWindow nutzt vollständig MVVM-Pattern:
• ObservableCollections für alle Dropdown-Listen
• Two-Way-Bindings für Real-time Validation
• Command-Pattern mit Parameter-Support für alle Button-Actions
• Event-Based Communication zwischen View und ViewModel

📊 Orange-Dashboard nutzen
Teams werden in Orange-Cards mit Glow-Effekten dargestellt:
• Orange-Header mit Primary-Gradient
• Orange-Akzent-Buttons für Aktionen
• Orange-Elevation-Effekte bei Hover
• Konsistente Orange-Farbharmonie

⚠️ Team-Warnschwellen (Orange-UI)
Neue Orange-Dialoge für Warnschwellen-Konfiguration mit verbesserter UX.

📝 Notizen-System (Orange-Integration)
Orange-Akzente im integrierten Notizen-Panel mit modernem Design.";
        }

        private string GenerateTeamInputMVVMContent()
        {
            return @"🧡 Team-Input MVVM (NEU in v1.9.0)

🏗️ MVVM-Architektur Features
• Vollständige Trennung von UI-Logik (View) und Business-Logik (ViewModel)
• ObservableCollections für alle ComboBox-Daten (Hunde, Personal)
• Two-Way-Bindings mit UpdateSourceTrigger=PropertyChanged für Real-time Updates
• Command-Pattern mit Parameter-Support und CanExecute-Validation für alle Button-Actions
• Event-Based Communication zwischen View und ViewModel

🧡 Orange-Design Features
• Orange Header-Gradient mit PrimaryGradient-Resource
• Orange-Cards (OrangeCard-Style) für alle Info-Panels
• Orange-Akzent-Button (OrangeAccentButton) für Team-Erstellung
• Orange-Glow-Effekte (OrangeElevation2) auf Icons
• Konsistente Orange-Farbharmonie durch gesamte UI

📊 Enhanced Command Support
• RelayCommand mit generischen Parameter-Support
• RelayCommand<T> für strongly-typed Parameter
• Automatic CanExecute-Updates bei Property-Änderungen
• Exception-Handling in Command-Execution
• Async Command-Support für Master-Data-Loading

🔄 Real-time Features
• Live Team-Name-Preview während der Eingabe
• Sofortige Form-Validation mit IsFormValid-Property
• Dynamic ComboBox-Population basierend auf Stammdaten
• Event-gesteuerte Kommunikation zwischen Komponenten

⚡ Performance-Optimierungen
• Lazy Loading von Stammdaten
• Efficient Caching von ComboBox-Items
• Minimal UI-Updates durch selektive PropertyChanged-Events
• Memory-optimierte ObservableCollections";
        }

        private string GenerateOrangeDesignContent()
        {
            return @"🧡 Orange-Design-System (NEU in v1.9.0)

🎨 Farbpalette
• Primary: #F57C00 (Orange 600) - Hauptfarbe für alle Akzente
• Secondary: #FF9800 (Orange 500) - Sekundäre Elemente
• Tertiary: #FFB74D (Orange 300) - Helle Akzente
• Error: #F44336 (Red 500) - Fehler und kritische Warnungen
• Warning: #FF9800 (Orange 500) - Warnungen in Orange-Harmonie

🌓 Dark/Light Mode Integration
• Automatische Orange-Anpassung für beide Modi
• Optimierte Orange-Töne für Dark Mode (#FFB74D, #FFCC80)
• Konsistente Kontrast-Verhältnisse für Accessibility
• Dynamic Resource-Switching für Theme-Wechsel

🧡 Orange-spezifische Komponenten
• OrangeCard - Cards mit Orange-Border und Subtle-Background
• OrangeAccentButton - Orange-Buttons mit White-Text und Shadows
• OrangeElevation (1-5) - Orange-basierte Schatten-Effekte
• PrimaryGradient - Orange-Gradienten für Header-Bereiche

🎭 Material Design Integration
• Elevation-System mit Orange-Schatten
• Ripple-Effekte in Orange-Tönen
• Card-Layouts mit Orange-Akzenten
• Smooth Transitions zwischen UI-States

✨ Animation & Effects
• Orange Glow-Effekte bei Hover-States
• Smooth Color-Transitions bei Theme-Wechsel
• Entrance-Animationen für neue UI-Elemente
• Subtle Orange-Highlights für aktive Elemente

🚀 Implementierung
Alle UI-Elemente nutzen DynamicResource-Bindings:
• Automatisches Theme-Switching
• Konsistente Farbgebung app-weit
• Einfache Wartung und Erweiterung
• Performance-optimierte Resource-Lookups";
        }

        private string GenerateDashboardGuideContent()
        {
            return @"📊 Dashboard-Übersicht mit Orange-Design

🧡 Orange-Card-Layout
• Teams in modernen Orange-Cards dargestellt
• Orange-Header mit Team-Typ-Badges
• Orange-Glow-Effekte bei Hover-Interactions
• Konsistente Orange-Akzente für Status-Indikatoren

🎨 Responsive Design
• Automatische Anpassung an Bildschirmgröße
• Optimale Darstellung von 1-50 Teams
• WrapPanel-Layout für flexible Team-Anordnung
• Scroll-Optimierung für große Team-Mengen

⚡ Performance-Features
• Virtualized Scrolling bei vielen Teams
• Lazy Loading von Team-Details
• Efficient Memory-Management für Cards
• Smooth Animations ohne Performance-Verlust

🔄 Status-System
• Grün: Team bereit (mit Orange-Akzenten)
• Orange: Team im Einsatz (Primary-Color)
• Rot: Kritische Warnung (Error-Color)
• Grau: Team pausiert (Neutral-Colors)

📱 Interactive Elements
• Direkte Timer-Steuerung auf Cards
• Quick-Access zu Team-Details
• Inline-Editing von Team-Informationen
• Context-Menus für erweiterte Aktionen";
        }

        private string GeneratePdfExportGuideContent()
        {
            return @"📄 PDF-Export mit Orange-Branding

🧡 Corporate Design
• Orange-Header und Akzente in PDF-Layout
• Professionelles Branding mit Orange-Logo-Bereich
• Konsistente Farbgebung entsprechend App-Design
• Print-optimierte Orange-Töne für bessere Lesbarkeit

📊 Export-Optionen
• Vollbericht: Alle Teams, Zeiten und Notizen
• Kurzbericht: Zusammenfassung mit Kennzahlen
• Statistik-Report: Grafiken und Auswertungen
• Timeline-Export: Chronologischer Ereignis-Verlauf

⚡ Performance-Optimierungen
• Streaming-Export für große Datenmengen
• Parallel Processing für schnelle Generierung
• Memory-effiziente PDF-Erstellung
• Progress-Feedback während Export-Prozess

🎯 Professionelle Features
• Automatische Seitennummerierung
• Inhaltsverzeichnis mit Hyperlinks
• Watermark-Option für Entwürfe
• Digitale Signatur-Unterstützung

📋 Template-System
• Vordefinierte Orange-Templates
• Anpassbare Header und Footer
• Flexible Spalten-Layouts
• Logo-Integration für Organisationen";
        }

        private string GenerateTeamWarningsGuideContent()
        {
            return @"⚠️ Team-Warnschwellen mit Orange-UI

🧡 Orange-Dialog-Design
• Moderne Orange-Dialoge für Warnschwellen-Konfiguration
• Orange-Slider für intuitive Zeit-Einstellung
• Orange-Akzent-Buttons für Speichern/Abbrechen
• Live-Preview der Warnzeiten mit Orange-Indikatoren

⏱️ Individuelle Konfiguration
• Pro Team eigene Erste und Zweite Warnung
• Eingabe in Minuten mit Slider oder Textfeld
• Sofortige Validierung der eingegebenen Werte
• Backup zu globalen Einstellungen wenn nicht konfiguriert

🎨 Visuelle Indikatoren
• Orange-Warning-States für erste Warnung
• Red-Critical-States für zweite Warnung
• Smooth Color-Transitions zwischen Zuständen
• Orange-Glow-Effekte bei aktiven Warnungen

⚡ Sofortige Anwendung
• Änderungen werden live übernommen
• Automatische Speicherung in Team-Profil
• Keine Neustart erforderlich
• Integration in Auto-Save-System

📊 Team-spezifische Optimierung
• Verschiedene Hundeteam-Typen berücksichtigen
• Erfahrungsbasierte Warnzeit-Empfehlungen
• Historische Daten für Optimierungs-Vorschläge
• Flexible Anpassung je nach Einsatzart";
        }

        private string GenerateNotesSystemGuideContent()
        {
            return @"📝 Notizen-System mit Orange-Integration

🧡 Orange-UI-Integration
• Orange-Akzente im Notizen-Panel
• Orange-Highlight für neue Notizen
• Orange-Icons für verschiedene Notiz-Typen
• Konsistente Orange-Farbgebung im gesamten System

⚡ Hauptfenster-Integration
• Direkte Eingabe ohne separate Fenster
• Enter-Taste für schnelle Notiz-Erstellung
• Auto-Scroll zu neuesten Einträgen
• Real-time Updates ohne Manual Refresh

🏷️ Kategorisierung-System
• Automatische Typ-Erkennung (System, Benutzer, Warnungen)
• Farbkodierte Kategorien mit Orange-Harmonie
• Filterung nach Notiz-Typen
• Team-spezifische Zuordnung

📊 Export & Integration
• Vollständige PDF-Integration mit Orange-Layout
• JSON-Export für externe Systeme
• Chronologische Timeline-Darstellung
• Suchfunktion über alle Notizen

🔍 Erweiterte Features
• Unlimited Notiz-Speicher
• Automatische Zeitstempel
• Team-Zuordnung über Dropdown
• Quick-Access über Tastenkürzel";
        }

        private string GenerateMasterDataGuideContent()
        {
            return @"📊 Stammdatenverwaltung mit Orange-Design

🧡 Orange-Management-UI
• Moderne Orange-Tabs für Personal/Hunde-Verwaltung
• Orange-Cards für Individual-Einträge
• Orange-Add-Buttons für neue Datensätze
• Konsistente Orange-Akzente durch gesamte Verwaltung

👥 Personal-Management
• Vollständige Personal-Erfassung mit Orange-Forms
• Mehrfach-Fähigkeiten: HF, H, FA, GF, ZF, VF, DP
• Aktiv/Inaktiv-Status mit Orange-Toggle-Switches
• Notiz-Feld für zusätzliche Informationen

🐕 Hunde-Management
• Comprehensive Hunde-Profile mit Orange-Styling
• Mehrfach-Spezialisierungen: FL, TR, MT, WO, LA, GE, LS
• Zuordnung zu Hundeführern über Dropdown
• Rasse, Alter und Notizen-Erfassung

🔄 Team-Integration
• Seamless Integration in TeamInputWindow MVVM
• Auto-Population von ComboBox-Listen
• Auto-Fill bei Hund-Auswahl (Hundeführer)
• Fallback zu manueller Eingabe

💾 Speicher & Backup
• Local JSON-Storage in %LocalAppData%
• Automatisches Backup bei Änderungen
• Import/Export für Daten-Migration
• Versionierung für Wiederherstellung";
        }

        private string GenerateShortcutsContent()
        {
            return @"⌨️ Tastenkürzel-Referenz v1.9.0

🧡 Orange-UI Shortcuts
Strg + O: Orange-Theme-Optionen öffnen
Strg + Shift + O: Orange-Design-Einstellungen

🕐 Timer-Steuerung
F1: Team 1 Timer start/stop
F2: Team 2 Timer start/stop
F3: Team 3 Timer start/stop
F4: Team 4 Timer start/stop
F5: Team 5 Timer start/stop
F6: Team 6 Timer start/stop
F7: Team 7 Timer start/stop
F8: Team 8 Timer start/stop
F9: Team 9 Timer start/stop
F10: Team 10 Timer start/stop

🎛️ Allgemeine Steuerung
F11: Vollbild ein/ausschalten
Esc: Vollbild beenden oder App schließen
Strg + N: Neues Team hinzufügen (MVVM-Dialog)
Strg + E: Einsatz exportieren (mit Orange-PDF)
Strg + T: Theme umschalten (Orange-Integration)
Strg + H: Hilfe anzeigen (dieses Fenster)
Strg + M: Stammdaten-Verwaltung öffnen

📝 Eingabe & Navigation
Enter: Schnellnotiz hinzufügen (im Notiz-Feld)
Tab: Zwischen Eingabefeldern wechseln
Strg + A: Alles markieren (in Textfeldern)
Strg + F: Suche in Notizen
Strg + Shift + N: Erweiterte Notiz-Eingabe

💡 Tipp: Alle Shortcuts sind Orange-Theme-aware und funktionieren in allen Modi!";
        }

        // Placeholder methods for other content sections
        private string GenerateTimerContent() => GenerateContentForSection("Timer-System", "Präzise Timer mit Orange-Status-Indikatoren und MVVM-Bindings.");
        private string GenerateTeamManagementContent() => GenerateContentForSection("Team-Management", "Team-Verwaltung mit Orange-Cards und MVVM-Integration.");
        private string GenerateStatisticsContent() => GenerateContentForSection("Statistiken & Analytics", "Einsatz-Auswertungen mit Orange-Charts und erweiterten Metriken.");
        private string GenerateAutoSaveContent() => GenerateContentForSection("Auto-Speichern", "Automatische Sicherung mit Orange-Status-Feedback.");
        private string GenerateThemesContent() => GenerateContentForSection("Themes & UI", "Orange-Design-System mit Dark/Light Mode Integration.");
        private string GeneratePerformanceContent() => GenerateContentForSection("Performance", "Optimierte Performance für Orange-UI und MVVM-Pattern.");
        private string GenerateTroubleshootingContent() => GenerateContentForSection("Fehlerbehebung", "Lösungen für Orange-Design und MVVM-spezifische Probleme.");
        private string GenerateFAQContent() => GenerateContentForSection("Häufige Fragen", "FAQ zu v1.9.0 mit Orange-Design und MVVM-Features.");
        private string GenerateAutoUpdatesContent() => GenerateContentForSection("Update-System", "Automatische Updates mit Orange-Progress-Indikatoren.");
        private string GenerateSetupGuideContent() => GenerateContentForSection("Installation & Setup", "Setup-Guide für v1.9.0 mit Orange-Design-Konfiguration.");
        private string GenerateMobileEnhancedContent() => GenerateContentForSection("Mobile Funktionen", "Mobile Features mit Orange-Branding und responsive Design.");

        private string GenerateSearchResults(string searchTerm)
        {
            return $@"🔍 Suchergebnisse für '{searchTerm}'

Die Suchfunktion wird in einer zukünftigen Version implementiert.
Verwenden Sie die Orange-Navigation links, um Hilfe-Themen zu durchsuchen.

🧡 Neu in v1.9.0:
• Vollständige MVVM-Architektur mit Command-Parameter-Support
• Orange-Design-System
• Erweiterte Team-Input-Funktionen
• Verbesserte Performance und UX";
        }

        private string GenerateContentForSection(string title, string description)
        {
            return $@"{title}

{description}

Diese Sektion wird in einer zukünftigen Version erweitert.
Verwenden Sie die Orange-Navigation für andere Hilfe-Themen.

🧡 v1.9.0 Features verfügbar in dieser Sektion:
• Orange-Design-Integration
• MVVM-Pattern-Unterstützung mit Command-Parameter-Support
• Verbesserte Performance
• Enhanced User Experience";
        }
    }

    public enum HelpSection
    {
        QuickStart,
        TeamInputMVVM,
        OrangeDesign,
        DashboardGuide,
        PdfExportGuide,
        TeamWarningsGuide,
        NotesSystemGuide,
        MasterDataGuide,
        Shortcuts,
        TeamManagement,
        Timer,
        Statistics,
        AutoSave,
        Themes,
        Performance,
        Troubleshooting,
        FAQ,
        AutoUpdates,
        SetupGuide,
        MobileEnhanced
    }

    public class HelpMenuItem : BaseViewModel
    {
        public HelpSection Section { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
