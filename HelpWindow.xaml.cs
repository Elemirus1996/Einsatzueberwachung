using System;
using System.Windows;
using System.Windows.Controls;

namespace Einsatzueberwachung
{
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
            ShowQuickStart_Click(null, null); // Show default content
        }

        private void ClearContent()
        {
            ContentPanel.Children.Clear();
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox?.Text == "Suchen...")
            {
                textBox.Text = "";
                textBox.Foreground = (System.Windows.Media.Brush)FindResource("OnSurface");
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (string.IsNullOrWhiteSpace(textBox?.Text))
            {
                textBox.Text = "Suchen...";
                textBox.Foreground = (System.Windows.Media.Brush)FindResource("OnSurfaceVariant");
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox?.Text != "Suchen..." && !string.IsNullOrWhiteSpace(textBox?.Text))
            {
                ShowSearchResults(textBox.Text);
            }
        }

        private void ShowQuickStart_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            ContentPanel.Children.Add(QuickStartContent);
        }

        // NEU in v1.7 - Dashboard-Übersicht Hilfe
        private void ShowDashboardGuide_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("📊 Dashboard-Übersicht (NEU in v1.7)",
                "Moderne Team-Cards|Teams werden jetzt in einer übersichtlichen Card-Ansicht dargestellt. Jede Karte zeigt alle wichtigen Informationen kompakt an.",
                "Responsive Layout|Das Dashboard passt sich automatisch an verschiedene Bildschirmgrößen an. Bei kleineren Bildschirmen werden Teams in einer Spalte angezeigt.",
                "Status-Indikatoren|Jede Team-Karte zeigt den aktuellen Status mit Farben an: Grün=Bereit, Gelb=Im Einsatz, Orange=Warnung, Rot=Kritisch.",
                "Quick-Actions|Direkt auf jeder Karte können Sie Timer starten/stoppen, Team bearbeiten oder Details anzeigen.",
                "Team-Organisation|Teams sind automatisch sortiert und können nach verschiedenen Kriterien gefiltert werden.",
                "Scroll-Optimierung|Auch bei vielen Teams bleibt die Performance durch optimierte Darstellung flüssig.",
                "Kompakte Darstellung|Alle wichtigen Informationen (Timer, Status, Typ, Personal) auf einen Blick sichtbar.",
                "Hover-Effekte|Interaktive Elemente reagieren auf Mausbewegungen für bessere Benutzerführung."
            );
            
            ContentPanel.Children.Add(content);
        }

        // NEU in v1.7 - PDF-Export Hilfe
        private void ShowPdfExportGuide_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("📄 Erweiterter PDF-Export (NEU in v1.7)",
                "Professionelle Berichte|Der PDF-Export wurde komplett überarbeitet und erstellt jetzt professionelle Einsatz-Dokumentationen.",
                "Vollständige Einsatz-Daten|Alle Team-Informationen, Timer-Verläufe, Notizen und Statistiken werden strukturiert exportiert.",
                "Optimierte Layouts|Verschiedene Template-Optionen für unterschiedliche Berichtsarten (Kurzbericht, Vollbericht, Statistik).",
                "Team-Details|Detaillierte Aufstellung aller Teams mit Zeiten, Status-Verläufen und individuellen Informationen.",
                "Timeline-Export|Chronologische Darstellung aller Ereignisse während des Einsatzes.",
                "Statistik-Integration|Automatische Berechnung und grafische Darstellung von Einsatz-Kennzahlen.",
                "Corporate Design|Professionelles Layout mit konfigurierbarem Branding für verschiedene Organisationen.",
                "Print-Optimierung|Perfekte Formatierung für Druck und digitale Archivierung.",
                "Mehrere Formate|Export als PDF mit verschiedenen Detail-Stufen je nach Verwendungszweck."
            );
            
            ContentPanel.Children.Add(content);
        }

        // NEU in v1.7 - Team-Warnschwellen Hilfe  
        private void ShowTeamWarningsGuide_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("⚠️ Individuelle Team-Warnschwellen (NEU in v1.7)",
                "Pro-Team-Konfiguration|Jedes Team kann jetzt eigene Warnzeiten definieren, unabhängig von den globalen Einstellungen.",
                "Flexible Warnstufen|Erste und zweite Warnung können für jedes Team individuell in Minuten konfiguriert werden.",
                "Zugriff über Team-Menü|Rechtsklick auf Team → 'Warnschwellen bearbeiten' öffnet das Konfigurationsfenster.",
                "Standard-Fallback|Teams ohne eigene Konfiguration verwenden automatisch die globalen Standard-Warnzeiten.",
                "Visuelle Unterscheidung|Teams mit eigenen Warnschwellen werden in der Übersicht entsprechend markiert.",
                "Verschiedene Einsatzarten|Ideal für unterschiedliche Hundeteam-Typen mit verschiedenen Belastungszyklen.",
                "Einfache Verwaltung|Intuitive Benutzeroberfläche mit Vorschau der konfigurierten Zeiten.",
                "Sofortige Anwendung|Änderungen werden direkt übernommen und gespeichert.",
                "Akustische Warnungen|Optional können für jedes Team auch individuelle Warntöne konfiguriert werden.",
                "Automatische Dokumentation|Erreichen von Warnschwellen wird automatisch in den Notizen protokolliert."
            );
            
            ContentPanel.Children.Add(content);
        }

        // NEU in v1.7 - Notizen-System Hilfe
        private void ShowNotesSystemGuide_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("📝 Verbessertes Notizen-System (NEU in v1.7)",
                "Integration im Hauptfenster|Das Notizen-System ist jetzt direkt im Hauptfenster integriert für schnelleren Zugriff.",
                "Optimierte Eingabe|Verbesserter Workflow ermöglicht schnelle Notiz-Erstellung ohne Fenster-Wechsel.",
                "Echzeit-Updates|Neue Notizen werden sofort angezeigt ohne manuelle Aktualisierung.",
                "Tastatur-Shortcuts|Schnelle Bedienung über Enter-Taste für neue Notizen oder Strg+N für erweiterte Eingabe.",
                "Automatische Zeitstempel|Jede Notiz erhält automatisch einen präzisen Zeitstempel.",
                "Kategorisierung|Notizen werden automatisch nach Typ kategorisiert (System, Benutzer, Warnungen, Timer-Events).",
                "Erweiterte Suchfunktion|Schnelles Auffinden bestimmter Notizen über Textsuche oder Kategoriefilter.",
                "Export-Integration|Alle Notizen werden vollständig in PDF-Berichte eingebunden.",
                "Team-spezifische Notizen|Notizen können Teams zugeordnet und entsprechend organisiert werden.",
                "Unlimited Storage|Keine Begrenzung der Anzahl Notizen - optimiert für lange Einsätze."
            );
            
            ContentPanel.Children.Add(content);
        }

        // NEU in v1.7 - Stammdaten Hilfe
        private void ShowMasterDataGuide_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("📊 Stammdatenverwaltung (NEU in v1.7)",
                "Zentrale Verwaltung|Alle Personen und Hunde werden zentral in der Stammdatenverwaltung erfasst und verwaltet.",
                "Personal-Verwaltung|Erfassung von Vorname, Nachname, Fähigkeiten und Notizen für alle Einsatzkräfte.",
                "Mehrfach-Fähigkeiten|Eine Person kann mehrere Fähigkeiten haben: HF, H, FA, GF, ZF, VF, DP.",
                "Hunde-Verwaltung|Vollständige Erfassung von Hunden mit Name, Rasse, Alter und Spezialisierungen.",
                "Mehrfach-Spezialisierungen|Ein Hund kann mehrere Spezialisierungen haben: FL, TR, MT, WO, LA, GE, LS.",
                "Integration in Team-Erstellung|Bei neuen Teams können Personal und Hunde aus Dropdown-Listen ausgewählt werden.",
                "Auto-Fill Funktion|Bei Hund-Auswahl wird der zugeordnete Hundeführer automatisch vorgeschlagen.",
                "Flexible Workflows|Stammdaten sind optional - manuelle Eingabe weiterhin möglich.",
                "Statistik-Übersichten|Übersichtliche Auswertungen aller verfügbaren Ressourcen nach Fähigkeiten/Spezialisierungen.",
                "Aktiv/Inaktiv Status|Personen und Hunde können als aktiv/inaktiv markiert werden.",
                "Persistente Speicherung|Alle Stammdaten werden lokal als JSON-Dateien gespeichert.",
                "Import/Export|Daten können für Backup oder Austausch zwischen Installationen exportiert werden."
            );
            
            ContentPanel.Children.Add(content);
        }

        // NEU in v1.7 - Auto-Updates Hilfe (falls vorhanden)
        private void ShowAutoUpdates_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("🔄 Update-System Übersicht",
                "Manuelle Update-Prüfung|Updates können manuell über das Menü geprüft werden.",
                "Version-Anzeige|Aktuelle Version wird im About-Dialog angezeigt.",
                "Release Notes|Neue Features werden in den Release Notes dokumentiert.",
                "Kompatibilität|Updates sind vollständig rückwärtskompatibel mit älteren Versionen."
            );
            
            ContentPanel.Children.Add(content);
        }

        // NEU in v1.7 - Professional Setup Guide (falls vorhanden)
        private void ShowSetupGuide_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("🛠️ Installation & Setup",
                "Portable Version|Die Anwendung kann als Portable-Version ohne Installation genutzt werden.",
                "System-Anforderungen|Windows 10 oder neuer, .NET 8 Runtime erforderlich.",
                "Erste Schritte|Nach dem Start Einsatzleiter und Ort eingeben, dann Teams erstellen.",
                "Konfiguration|Alle Einstellungen werden automatisch gespeichert."
            );
            
            ContentPanel.Children.Add(content);
        }

        // NEU in v1.7 - Enhanced Mobile Features (falls vorhanden)
        private void ShowMobileEnhanced_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("📱 Mobile Funktionen",
                "Mobile Verbindung|Optional kann ein Mobile Server gestartet werden für Smartphone-Zugriff.",
                "QR-Code Setup|Einfaches Setup durch QR-Code scannen mit dem Smartphone.",
                "Timer-Fernsteuerung|Remote-Steuerung der Timer über das Smartphone möglich.",
                "Netzwerk-Anforderungen|WiFi-Verbindung zwischen Computer und Smartphone erforderlich."
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowStatistics_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("📊 Erweiterte Statistiken & Analytics",
                "Real-time Dashboard|Live-Übersicht aller wichtigen Einsatz-Metriken und Team-Performance.",
                "Team-Performance Analysis|Detaillierte Analyse der Team-Effizienz mit Rankings und Bewertungen.",
                "Einsatz-Timeline|Chronologische Darstellung aller Ereignisse mit Zeitstempeln.",
                "Effizienz-Bewertungen|Automatische Bewertung der Team-Performance mit Optimierungsvorschlägen.",
                "Team-Type Statistiken|Visuelle Verteilung der verschiedenen Hundeteam-Spezialisierungen.",
                "Performance-Metriken|Detaillierte Messung von Timer-Genauigkeit und System-Performance.",
                "Automatische Empfehlungen|Empfehlungen für Einsatzoptimierung basierend auf Daten.",
                "Export-Funktionen|Vollständige Statistiken als PDF oder JSON für Dokumentation.",
                "Vergleichsanalysen|Vergleich verschiedener Einsätze für Trend-Analyse.",
                "Interaktive Charts|Klickbare Diagramme für detaillierte Drill-Down-Analysen."
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowShortcuts_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = new StackPanel();
            
            // Header
            var header = new TextBlock
            {
                Text = "⌨️ Tastenkürzel-Referenz v1.7",
                Style = (Style)FindResource("SectionHeaderStyle")
            };
            content.Children.Add(header);
            
            // Timer shortcuts section
            var timerSection = CreateShortcutSection("🕐 Timer-Steuerung");
            AddShortcut(timerSection, "F1", "Team 1 Timer start/stop");
            AddShortcut(timerSection, "F2", "Team 2 Timer start/stop");
            AddShortcut(timerSection, "F3", "Team 3 Timer start/stop");
            AddShortcut(timerSection, "F4", "Team 4 Timer start/stop");
            AddShortcut(timerSection, "F5", "Team 5 Timer start/stop");
            AddShortcut(timerSection, "F6", "Team 6 Timer start/stop");
            AddShortcut(timerSection, "F7", "Team 7 Timer start/stop");
            AddShortcut(timerSection, "F8", "Team 8 Timer start/stop");
            AddShortcut(timerSection, "F9", "Team 9 Timer start/stop");
            AddShortcut(timerSection, "F10", "Team 10 Timer start/stop");
            content.Children.Add(timerSection);

            // General shortcuts section
            var generalSection = CreateShortcutSection("🎛️ Allgemeine Steuerung");
            AddShortcut(generalSection, "F11", "Vollbild ein/ausschalten");
            AddShortcut(generalSection, "Esc", "Vollbild beenden oder App schließen");
            AddShortcut(generalSection, "Strg + N", "Neues Team hinzufügen oder neue Notiz");
            AddShortcut(generalSection, "Strg + E", "Einsatz exportieren");
            AddShortcut(generalSection, "Strg + T", "Theme umschalten (Hell/Dunkel)");
            AddShortcut(generalSection, "Strg + H", "Hilfe anzeigen");
            AddShortcut(generalSection, "Strg + M", "Stammdaten-Verwaltung öffnen (NEU)");
            content.Children.Add(generalSection);

            // Input shortcuts section
            var inputSection = CreateShortcutSection("📝 Eingabe & Navigation (v1.7 erweitert)");
            AddShortcut(inputSection, "Enter", "Schnellnotiz hinzufügen (im Notiz-Feld)");
            AddShortcut(inputSection, "Tab", "Zwischen Eingabefeldern wechseln");
            AddShortcut(inputSection, "Strg + A", "Alles markieren (in Textfeldern)");
            AddShortcut(inputSection, "Strg + C", "Kopieren");
            AddShortcut(inputSection, "Strg + V", "Einfügen");
            AddShortcut(inputSection, "Strg + F", "Suche in Notizen (NEU)");
            content.Children.Add(inputSection);
            
            var tip = new TextBlock
            {
                Text = "💡 Tipp: Alle Shortcuts funktionieren global in der Anwendung und sind optimiert für den Einsatz unter Stress. Neu in v1.7: Erweiterte Shortcuts für Stammdaten und Notizen.",
                Style = (Style)FindResource("ContentStyle"),
                Margin = new Thickness(0, 20, 0, 0),
                FontStyle = FontStyles.Italic,
                Background = (System.Windows.Media.Brush)FindResource("WarningContainer"),
                Foreground = (System.Windows.Media.Brush)FindResource("OnWarningContainer"),
                Padding = new Thickness(12)
            };
            content.Children.Add(tip);
            
            ContentPanel.Children.Add(content);
        }

        private StackPanel CreateShortcutSection(string title)
        {
            var section = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
            
            var header = new TextBlock
            {
                Text = title,
                Style = (Style)FindResource("SubHeaderStyle")
            };
            section.Children.Add(header);

            return section;
        }

        private void AddShortcut(StackPanel parent, string key, string description)
        {
            var border = new Border
            {
                Style = (Style)FindResource("ShortcutStyle")
            };
            
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            var keyText = new TextBlock
            {
                Text = key,
                Style = (Style)FindResource("KeyStyle")
            };
            Grid.SetColumn(keyText, 0);
            
            var arrow = new TextBlock
            {
                Text = "→",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10, 0, 10, 0),
                Foreground = System.Windows.Media.Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(arrow, 1);
            
            var descText = new TextBlock
            {
                Text = description,
                Style = (Style)FindResource("ContentStyle"),
                Margin = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(descText, 2);
            
            grid.Children.Add(keyText);
            grid.Children.Add(arrow);
            grid.Children.Add(descText);
            
            border.Child = grid;
            parent.Children.Add(border);
        }

        private void ShowTeamManagement_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("👥 Team-Management (v1.7 erweitert)",
                "Team-Typen|Es stehen 7 verschiedene Rettungshunde-Kategorien zur Verfügung: Fläche, Trümmer, Mantrailer, Wasser, Lawine, Gelände und Leichen.",
                "Dashboard-Ansicht|Teams werden in der neuen Dashboard-Übersicht als kompakte Karten dargestellt.",
                "Stammdaten-Integration|Teams können jetzt aus der Stammdatenverwaltung erstellt werden - Personal und Hunde aus Dropdown-Listen wählen.",
                "Team hinzufügen|Klicken Sie auf '+ Team' und wählen Sie entweder aus Stammdaten oder geben Sie manuell ein.",
                "Mehrfach-Spezialisierung|Ein Hund kann mehrere Spezialisierungen haben. Wählen Sie einfach mehrere Kategorien aus.",
                "Individuelle Warnschwellen|Jedes Team kann eigene Warnzeiten definieren über Rechtsklick → 'Warnschwellen bearbeiten'.",
                "Team löschen|Klicken Sie auf das rote Mülleimer-Symbol. Eine Bestätigung wird angefordert.",
                "Team-Informationen|Füllen Sie Hund, Hundeführer und Helfer aus. Alle Änderungen werden automatisch gespeichert.",
                "Farbkodierung|Jeder Team-Typ hat eine eigene Farbe für bessere Übersichtlichkeit.",
                "Team-Status|Grün=Bereit, Gelb=Im Einsatz, Orange=Warnung, Rot=Kritisch, Grau=Pause"
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowTimer_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("⏱️ Timer-System (v1.7 erweitert)",
                "Individuelle Timer|Jedes Team hat einen unabhängigen Timer mit Start/Stop/Reset-Funktionalität.",
                "Dashboard-Integration|Timer werden jetzt prominent in der Dashboard-Karten-Ansicht angezeigt.",
                "Tastenkürzel|F1-F10 steuern die Timer der Teams 1-10 direkt - optimal für schnelle Reaktionen.",
                "Präzision|Timer arbeiten mit Millisekunden-Genauigkeit und kompensieren automatisch Systemverzögerungen.",
                "Individuelle Warnzeiten|Jedes Team kann eigene Warnschwellen definieren, unabhängig von globalen Einstellungen.",
                "Status-Anzeige|Farbkodierung: Grün=Bereit, Gelb=Läuft, Orange=Warnung, Rot=Kritisch.",
                "Automatische Dokumentation|Timer-Ereignisse werden automatisch in den Notizen protokolliert.",
                "Synchronisation|Alle Timer sind synchron zu Systemzeit und werden bei Zeitsprüngen korrigiert."
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowNotesSystem_Click(object sender, RoutedEventArgs e)
        {
            ShowNotesSystemGuide_Click(sender, e);
        }

        private void ShowAutoSave_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("💾 Automatisches Speichern (v1.7 optimiert)",
                "Auto-Save Intervall|Alle 30 Sekunden werden Änderungen automatisch gespeichert.",
                "Erweiterte Datenstrukturen|Neue Stammdaten und Team-Warnschwellen werden ebenfalls automatisch gespeichert.",
                "Crash-Recovery|Bei Programmabsturz werden alle Daten wiederhergestellt beim nächsten Start.",
                "Speicherort|%LocalAppData%\\Einsatzueberwachung\\AutoSave\\ für Einsätze, \\MasterData\\ für Stammdaten.",
                "Change Detection|Nur tatsächliche Änderungen werden gespeichert für optimale Performance.",
                "Export-Funktion|Erstellt JSON-Dateien und jetzt auch erweiterte PDF-Berichte für Archivierung.",
                "Versionierung|Bis zu 10 automatische Backup-Versionen werden vorgehalten.",
                "Stammdaten-Backup|Personal und Hunde-Daten werden separat gesichert und können einzeln wiederhergestellt werden.",
                "Integrität|Checksummen-Prüfung verhindert Datenverlust durch defekte Dateien."
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowThemes_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("🎨 Themes & Benutzeroberfläche (v1.7 erweitert)",
                "Dashboard-Design|Neue moderne Card-Ansicht mit Material Design-Prinzipien.",
                "Automatischer Dark Mode|Wechselt automatisch zwischen 18:00 und 07:00 Uhr in den Dunkelmodus.",
                "Manueller Wechsel|Klicken Sie auf das Sonnen/Mond-Symbol oder verwenden Sie Strg+T.",
                "Responsive Design|UI passt sich automatisch an Bildschirmgröße an - optimiert für Dashboard-Ansicht.",
                "Erweiterte Animationen|Smooth Hover-Effekte bei Team-Karten, Entrance-Animationen und Blinking-Warnungen.",
                "Accessibility|High-DPI Support, klare Kontraste, große Klickflächen für bessere Benutzerfreundlichkeit.",
                "Konsistente Farbgebung|Einheitliche Farben für Team-Typen über alle UI-Elemente hinweg.",
                "Optimierte Performance|Effiziente Rendering auch bei vielen Teams in der Dashboard-Ansicht."
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowPerformance_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("⚡ Performance-Optimierungen (v1.7 erweitert)",
                "Dashboard-Performance|Virtualized Scrolling und Lazy Loading für flüssige Darstellung vieler Teams.",
                "PDF-Export-Optimierung|Streaming-Export und Parallel Processing für schnelle Bericht-Generierung.",
                "Stammdaten-Performance|Efficient Caching und asynchrone Operationen für schnelle Stammdaten-Zugriffe.",
                "Timer-Diagnostics|Automatische Überwachung der Timer-Performance mit Warnungen bei Verzögerungen >50ms.",
                "Memory Management|Optimierte Garbage Collection und Memory Pools für Dashboard-Karten.",
                "UI-Virtualisierung|Effiziente Darstellung auch bei vielen Teams durch intelligente Virtualisierung.",
                "Background Processing|Nicht-kritische Tasks laufen mit niedriger Priorität im Hintergrund.",
                "Notizen-Performance|Incremental Search und Paged Loading bei großen Notiz-Mengen.",
                "Startup-Optimierung|Lazy Loading von Stammdaten und asynchrone Initialisierung für schnellen Start."
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowTroubleshooting_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("🔧 Fehlerbehebung (v1.7 erweitert)",
                "Dashboard-Probleme|Bei Darstellungsproblemen Theme wechseln oder Anwendung neu starten.",
                "PDF-Export-Fehler|Prüfen Sie Schreibrechte im Zielordner oder wählen Sie einen anderen Speicherort.",
                "Stammdaten-Issues|Bei Problemen mit Stammdaten: Backup aus %LocalAppData%\\Einsatzueberwachung\\MasterData\\ wiederherstellen.",
                "Team-Warnung-Probleme|Individuelle Warnschwellen über Team-Rechtsklick → 'Warnschwellen bearbeiten' zurücksetzen.",
                "Notizen-System-Fehler|Bei Notizen-Problemen: Neustart der Anwendung - alle Daten bleiben erhalten.",
                "Timer springt|Prüfen Sie die Systemzeit. Bei Zeitsprüngen werden Timer automatisch korrigiert.",
                "Langsame Performance|Bei vielen Teams: Dashboard-Ansicht nutzen, die für bessere Performance optimiert ist.",
                "Speicher-Probleme|Starten Sie die Anwendung neu - Auto-Save stellt alle Daten wieder her.",
                "Theme-Probleme|Wechseln Sie manuell das Theme oder starten Sie die App neu.",
                "Backup-Wiederherstellung|Backups finden Sie unter %LocalAppData%\\Einsatzueberwachung\\ in verschiedenen Unterordnern."
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowFAQ_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("❓ Häufige Fragen (v1.7 erweitert)",
                "Was ist neu in v1.7?|Dashboard-Übersicht, erweiterter PDF-Export, individuelle Team-Warnschwellen, verbessertes Notizen-System und Stammdatenverwaltung.",
                "Wie nutze ich die Stammdaten?|Menü → Stammdaten öffnen, Personal und Hunde erfassen, dann bei Team-Erstellung aus Dropdown wählen.",
                "Wie setze ich Team-Warnschwellen?|Rechtsklick auf Team → 'Warnschwellen bearbeiten' → eigene Zeiten eingeben.",
                "Wo finde ich die Dashboard-Ansicht?|Nach dem Start werden Teams automatisch in der neuen Card-Ansicht dargestellt.",
                "Wie erstelle ich PDF-Berichte?|Export-Menü → PDF-Export → gewünschte Optionen wählen → Bericht generieren.",
                "Funktioniert alles auch offline?|Ja, alle neuen Features funktionieren vollständig ohne Internetverbindung.",
                "Wie viele Teams kann ich erstellen?|Bis zu 50 Teams gleichzeitig - Dashboard-Ansicht ist für viele Teams optimiert.",
                "Wo werden Stammdaten gespeichert?|%LocalAppData%\\Einsatzueberwachung\\MasterData\\ - vollständig lokal.",
                "Kann ich alte Einsätze importieren?|Ja, alle Daten aus v1.6 werden automatisch übernommen und sind kompatibel.",
                "Sind individuelle Warnschwellen für alle Teams?|Ja, jedes Team kann eigene Zeiten haben oder die globalen Standards nutzen.",
                "Wie funktioniert das neue Notizen-System?|Direkt im Hauptfenster eingeben - Enter drücken für schnelle Notizen.",
                "Kann ich Stammdaten exportieren?|Ja, über die Stammdaten-Verwaltung können Sie alle Daten als JSON exportieren."
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowSearchResults(string searchText)
        {
            ClearContent();
            
            var header = new TextBlock
            {
                Text = $"🔍 Suchergebnisse für '{searchText}'",
                Style = (Style)FindResource("SectionHeaderStyle")
            };
            ContentPanel.Children.Add(header);
            
            var results = new TextBlock
            {
                Text = "Suchfunktion wird in einer zukünftigen Version implementiert.\nVerwenden Sie die Navigation links, um Hilfe-Themen zu durchsuchen.\n\nNeu in v1.7: Umfassende Hilfe für Dashboard, PDF-Export, Team-Warnschwellen, Notizen-System und Stammdatenverwaltung.",
                Style = (Style)FindResource("ContentStyle")
            };
            ContentPanel.Children.Add(results);
        }

        private StackPanel CreateHelpSection(string title, params string[] items)
        {
            var panel = new StackPanel();
            
            var header = new TextBlock
            {
                Text = title,
                Style = (Style)FindResource("SectionHeaderStyle")
            };
            panel.Children.Add(header);
            
            foreach (var item in items)
            {
                var parts = item.Split('|');
                if (parts.Length == 2)
                {
                    var subHeader = new TextBlock
                    {
                        Text = parts[0],
                        Style = (Style)FindResource("SubHeaderStyle")
                    };
                    panel.Children.Add(subHeader);
                    
                    var content = new TextBlock
                    {
                        Text = parts[1],
                        Style = (Style)FindResource("ContentStyle")
                    };
                    panel.Children.Add(content);
                }
            }
            
            return panel;
        }
    }
}
