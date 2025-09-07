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
        }

        private void ClearContent()
        {
            ContentPanel.Children.Clear();
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
                Text = "?? Tastenkürzel-Referenz",
                Style = (Style)FindResource("SectionHeaderStyle")
            };
            content.Children.Add(header);
            
            // Shortcuts
            AddShortcut(content, "F1 - F10", "Team-Timer starten/stoppen (Team 1-10)");
            AddShortcut(content, "F11", "Vollbild ein/aus");
            AddShortcut(content, "Esc", "Vollbild beenden oder App schließen");
            AddShortcut(content, "Enter", "Schnellnotiz hinzufügen (im Notiz-Feld)");
            AddShortcut(content, "Strg + N", "Neues Team hinzufügen");
            AddShortcut(content, "Strg + E", "Export starten");
            AddShortcut(content, "Strg + T", "Theme umschalten");
            
            var tip = new TextBlock
            {
                Text = "?? Tipp: Alle Shortcuts funktionieren global in der Anwendung",
                Style = (Style)FindResource("ContentStyle"),
                Margin = new Thickness(0, 15, 0, 0),
                FontStyle = FontStyles.Italic
            };
            content.Children.Add(tip);
            
            ContentPanel.Children.Add(content);
        }

        private void AddShortcut(StackPanel parent, string key, string description)
        {
            var border = new Border
            {
                Style = (Style)FindResource("ShortcutStyle")
            };
            
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
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
                Text = "?",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10, 0, 10, 0),
                Foreground = System.Windows.Media.Brushes.Gray
            };
            Grid.SetColumn(arrow, 1);
            
            var descText = new TextBlock
            {
                Text = description,
                Style = (Style)FindResource("ContentStyle"),
                Margin = new Thickness(0)
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
            
            var content = CreateHelpSection("?? Team-Management",
                "Team-Typen|Es stehen 6 verschiedene Rettungshunde-Kategorien zur Verfügung: Flächensuchhund, Trümmersuchhund, Mantrailer, Wasserortung, Lawinensuchhund und Allgemein.",
                "Team hinzufügen|Klicken Sie auf '+ Team' und wählen Sie den gewünschten Team-Typ. Teams werden automatisch nummeriert.",
                "Team löschen|Klicken Sie auf das rote Mülleimer-Symbol. Eine Bestätigung wird angefordert.",
                "Team-Informationen|Füllen Sie Hund, Hundeführer und Helfer aus. Alle Änderungen werden automatisch gespeichert.",
                "Farbkodierung|Jeder Team-Typ hat eine eigene Farbe für bessere Übersichtlichkeit."
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowNotesSystem_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("?? Erweiterte Notizen",
                "Automatische Zeitstempel|Alle Aktionen werden automatisch mit Zeitstempel protokolliert: Timer-Start, -Stop, -Reset, Warnungen.",
                "Schnellnotizen|Verwenden Sie das obere Eingabefeld für schnelle Notizen. Enter-Taste oder '+' Button zum Hinzufügen.",
                "Formatierung|Notizen werden mit Emojis und klarer Formatierung angezeigt: [14:25:30] ? Timer gestartet",
                "Zusätzliche Notizen|Das untere Textfeld für längere Notizen und Freitext-Eingaben.",
                "Auto-Scroll|Neue Notizen werden automatisch sichtbar gemacht."
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowAutoSave_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("?? Automatisches Speichern",
                "Auto-Save Intervall|Alle 30 Sekunden werden Änderungen automatisch gespeichert.",
                "Crash-Recovery|Bei Programmabsturz werden alle Daten wiederhergestellt beim nächsten Start.",
                "Speicherort|%LocalAppData%\\Einsatzueberwachung\\AutoSave\\",
                "Change Detection|Nur tatsächliche Änderungen werden gespeichert für optimale Performance.",
                "Export-Funktion|Erstellt JSON-Dateien mit allen Einsatzdaten für Archivierung und Dokumentation."
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowThemes_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("?? Themes & Benutzeroberfläche",
                "Automatischer Dark Mode|Wechselt automatisch zwischen 18:00 und 07:00 Uhr in den Dunkelmodus.",
                "Manueller Wechsel|Klicken Sie auf das Sonnen/Mond-Symbol oder verwenden Sie Strg+T.",
                "Responsive Design|UI passt sich automatisch an Bildschirmgröße an (800px, 1200px Breakpoints).",
                "Animationen|Smooth Hover-Effekte, Entrance-Animationen und Blinking-Warnungen.",
                "Accessibility|High-DPI Support, klare Kontraste, große Klickflächen."
            );
            
            ContentPanel.Children.Add(content);
        }

        private void ShowPerformance_Click(object sender, RoutedEventArgs e)
        {
            ClearContent();
            
            var content = CreateHelpSection("? Performance-Optimierungen",
                "Timer-Diagnostics|Automatische Überwachung der Timer-Performance mit Warnungen bei Verzögerungen >50ms.",
                "Memory Management|Automatische Garbage Collection alle 5 Minuten verhindert Memory-Leaks.",
                "UI-Virtualisierung|Effiziente Darstellung auch bei vielen Teams durch Virtualisierung.",
                "Background Processing|Nicht-kritische Tasks laufen mit niedriger Priorität im Hintergrund.",
                "Change Detection|Intelligente Erkennung von Änderungen reduziert unnötige Updates.",
                "Throttled Updates|Layout-Updates werden auf 250ms gedrosselt für flüssige Performance."
            );
            
            ContentPanel.Children.Add(content);
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