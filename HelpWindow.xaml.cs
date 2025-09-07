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

        private void ShowShortcuts_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = new StackPanel();
            
            // Header
            var header = new TextBlock
            {
                Text = "⌨️ Tastenkürzel-Referenz",
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
            AddShortcut(generalSection, "Strg + N", "Neues Team hinzufügen");
            AddShortcut(generalSection, "Strg + E", "Einsatz exportieren");
            AddShortcut(generalSection, "Strg + T", "Theme umschalten (Hell/Dunkel)");
            AddShortcut(generalSection, "Strg + H", "Hilfe anzeigen");
            content.Children.Add(generalSection);

            // Input shortcuts section
            var inputSection = CreateShortcutSection("📝 Eingabe & Navigation");
            AddShortcut(inputSection, "Enter", "Schnellnotiz hinzufügen (im Notiz-Feld)");
            AddShortcut(inputSection, "Tab", "Zwischen Eingabefeldern wechseln");
            AddShortcut(inputSection, "Strg + A", "Alles markieren (in Textfeldern)");
            AddShortcut(inputSection, "Strg + C", "Kopieren");
            AddShortcut(inputSection, "Strg + V", "Einfügen");
            content.Children.Add(inputSection);
            
            var tip = new TextBlock
            {
                Text = "💡 Tipp: Alle Shortcuts funktionieren global in der Anwendung und sind optimiert für den Einsatz unter Stress.",
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
            
            var content = CreateHelpSection("👥 Team-Management",
                "Team-Typen|Es stehen 6 verschiedene Rettungshunde-Kategorien zur Verfügung: Flächensuchhund, Trümmersuchhund, Mantrailer, Wasserortung, Lawinensuchhund und Allgemein.",
                "Team hinzufügen|Klicken Sie auf '+ Team' und wählen Sie den gewünschten Team-Typ. Teams werden automatisch nummeriert.",
                "Mehrfach-Spezialisierung|NEU in v1.5: Ein Hund kann mehrere Spezialisierungen haben. Wählen Sie einfach mehrere Kategorien aus.",
                "Team löschen|Klicken Sie auf das rote Mülleimer-Symbol. Eine Bestätigung wird angefordert.",
                "Team-Informationen|Füllen Sie Hund, Hundeführer und Helfer aus. Alle Änderungen werden automatisch gespeichert.",
                "Farbkodierung|Jeder Team-Typ hat eine eigene Farbe für bessere Übersichtlichkeit.",
                "Team-Limit|Bis zu 50 Teams gleichzeitig möglich (praktisches Limit bei großen Einsätzen).",
                "Team-Status|Grün=Bereit, Gelb=Im Einsatz, Rot=Warnung/Problem, Grau=Pause"
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowTimer_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("⏱️ Timer-System",
                "Individuelle Timer|Jedes Team hat einen unabhängigen Timer mit Start/Stop/Reset-Funktionalität.",
                "Tastenkürzel|F1-F10 steuern die Timer der Teams 1-10 direkt - optimal für schnelle Reaktionen.",
                "Präzision|Timer arbeiten mit Millisekunden-Genauigkeit und kompensieren automatisch Systemverzögerungen.",
                "Warnzeiten|Konfigurierbare Warnungen bei 30min, 60min, 90min - anpassbar in den Einstellungen.",
                "Status-Anzeige|Farbkodierung: Grün=Bereit, Gelb=Läuft, Orange=Warnung, Rot=Kritisch.",
                "Statistiken|Doppelklick auf Timer zeigt Detailstatistiken: Gesamtzeit, Pausen, Durchschnittszeiten.",
                "Synchronisation|Alle Timer sind synchron zu Systemzeit und werden bei Zeitsprüngen korrigiert."
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowNotesSystem_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("📝 Erweiterte Notizen",
                "Automatische Zeitstempel|Alle Aktionen werden automatisch mit Zeitstempel protokolliert: Timer-Start, -Stop, -Reset, Warnungen.",
                "Schnellnotizen|Verwenden Sie das obere Eingabefeld für schnelle Notizen. Enter-Taste oder '+' Button zum Hinzufügen.",
                "Formatierung|Notizen werden mit Emojis und klarer Formatierung angezeigt: [14:25:30] ⏱️ Timer gestartet",
                "Zusätzliche Notizen|Das untere Textfeld für längere Notizen und Freitext-Eingaben.",
                "Auto-Scroll|Neue Notizen werden automatisch sichtbar gemacht.",
                "Kategorisierung|Notizen werden automatisch nach Typ kategorisiert: System, Benutzer, Warnung.",
                "Export|Alle Notizen werden in Export-Dateien mit vollständiger Historie gespeichert.",
                "Backup|Notizen werden kontinuierlich gesichert und bei Absturz wiederhergestellt."
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowAutoSave_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("💾 Automatisches Speichern",
                "Auto-Save Intervall|Alle 30 Sekunden werden Änderungen automatisch gespeichert.",
                "Crash-Recovery|Bei Programmabsturz werden alle Daten wiederhergestellt beim nächsten Start.",
                "Speicherort|%LocalAppData%\\Einsatzueberwachung\\AutoSave\\",
                "Change Detection|Nur tatsächliche Änderungen werden gespeichert für optimale Performance.",
                "Export-Funktion|Erstellt JSON-Dateien mit allen Einsatzdaten für Archivierung und Dokumentation.",
                "Versionierung|Bis zu 10 automatische Backup-Versionen werden vorgehalten.",
                "Komprimierung|Automatische Komprimierung älterer Backups spart Speicherplatz.",
                "Integrität|Checksummen-Prüfung verhindert Datenverlust durch defekte Dateien."
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowThemes_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("🎨 Themes & Benutzeroberfläche",
                "Automatischer Dark Mode|Wechselt automatisch zwischen 18:00 und 07:00 Uhr in den Dunkelmodus.",
                "Manueller Wechsel|Klicken Sie auf das Sonnen/Mond-Symbol oder verwenden Sie Strg+T.",
                "Responsive Design|UI passt sich automatisch an Bildschirmgröße an (800px, 1200px Breakpoints).",
                "Animationen|Smooth Hover-Effekte, Entrance-Animationen und Blinking-Warnungen.",
                "Accessibility|High-DPI Support, klare Kontraste, große Klickflächen.",
                "Farbkontraste|Alle Farben erfüllen WCAG 2.1 Richtlinien für Barrierefreiheit.",
                "Schriftgrößen|Skalierbare Schriftgrößen für bessere Lesbarkeit.",
                "Kompaktmodus|Reduzierte UI für kleine Bildschirme oder maximale Übersicht."
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowPerformance_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("⚡ Performance-Optimierungen",
                "Timer-Diagnostics|Automatische Überwachung der Timer-Performance mit Warnungen bei Verzögerungen >50ms.",
                "Memory Management|Automatische Garbage Collection alle 5 Minuten verhindert Memory-Leaks.",
                "UI-Virtualisierung|Effiziente Darstellung auch bei vielen Teams durch Virtualisierung.",
                "Background Processing|Nicht-kritische Tasks laufen mit niedriger Priorität im Hintergrund.",
                "Change Detection|Intelligente Erkennung von Änderungen reduziert unnötige Updates.",
                "Throttled Updates|Layout-Updates werden auf 250ms gedrosselt für flüssige Performance.",
                "Caching|Intelligentes Caching von UI-Elementen und Daten reduziert CPU-Last.",
                "Startup-Optimierung|Lazy Loading und asynchrone Initialisierung für schnellen Start."
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowTroubleshooting_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("🔧 Fehlerbehebung",
                "Timer springt|Prüfen Sie die Systemzeit. Bei Zeitsprüngen werden Timer automatisch korrigiert.",
                "Langsame Performance|Reduzieren Sie die Anzahl aktiver Teams oder aktivieren Sie den Kompaktmodus.",
                "Speicher-Probleme|Starten Sie die Anwendung neu - Auto-Save stellt alle Daten wieder her.",
                "Theme-Probleme|Wechseln Sie manuell das Theme oder starten Sie die App neu.",
                "Export-Fehler|Prüfen Sie Schreibrechte im Zielordner oder wählen Sie einen anderen Speicherort.",
                "Backup-Wiederherstellung|Backups finden Sie unter %LocalAppData%\\Einsatzueberwachung\\AutoSave\\",
                "Netzwerk-Probleme|Arbeitet offline - alle Features funktionieren ohne Internetverbindung.",
                "Tastenkürzel funktionieren nicht|Prüfen Sie, ob andere Anwendungen die gleichen Shortcuts verwenden.",
                "Absturz-Wiederherstellung|Beim nächsten Start werden alle Daten automatisch wiederhergestellt."
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowFAQ_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("❓ Häufige Fragen",
                "Wie viele Teams kann ich erstellen?|Bis zu 50 Teams gleichzeitig - mehr als genug für die größten Einsätze.",
                "Funktioniert die App offline?|Ja, alle Features funktionieren ohne Internetverbindung.",
                "Wo werden die Daten gespeichert?|Lokal unter %LocalAppData%\\Einsatzueberwachung\\ - keine Cloud-Abhängigkeit.",
                "Kann ich Daten exportieren?|Ja, mit Strg+E oder über das Export-Menu als JSON-Datei.",
                "Wie genau sind die Timer?|Millisekunden-genau mit automatischer Systemzeit-Synchronisation.",
                "Was passiert bei einem Absturz?|Auto-Save stellt beim nächsten Start alle Daten wieder her.",
                "Kann ich das Design anpassen?|Ja, zwischen hellem und dunklem Theme wechseln oder eigene Farben definieren.",
                "Wie funktioniert die Mehrfach-Spezialisierung?|Ein Hund kann mehrere Kategorien haben - einfach mehrere auswählen.",
                "Sind die Tastenkürzel anpassbar?|Aktuell fest definiert, aber in zukünftigen Versionen konfigurierbar geplant."
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
                Text = "Suchfunktion wird in einer zukünftigen Version implementiert.\nVerwenden Sie die Navigation links, um Hilfe-Themen zu durchsuchen.",
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
