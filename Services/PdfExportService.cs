using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Einsatzueberwachung.Models;

namespace Einsatzueberwachung.Services
{
    /// <summary>
    /// PDF-Export-Service für Einsatzdokumentation v1.7
    /// Erstellt professionelle PDF-Berichte mit konfigurierbaren Feldern
    /// </summary>
    public class PdfExportService
    {
        private static PdfExportService? _instance;
        public static PdfExportService Instance => _instance ??= new PdfExportService();

        private PdfExportService()
        {
            // Lizenz für QuestPDF (Community License)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Exportiert Einsatzdaten als PDF
        /// </summary>
        public void ExportToPdf(string filePath, EinsatzData einsatzData, List<Team> teams, PdfExportOptions options)
        {
            try
            {
                LoggingService.Instance.LogInfo($"Starting PDF export to: {filePath}");

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                        // Header mit Logo und Einsatzinformationen
                        page.Header().Element(c => ComposeHeader(c, einsatzData, options));

                        // Hauptinhalt
                        page.Content().Element(c => ComposeContent(c, einsatzData, teams, options));

                        // Footer mit Seitenzahlen
                        page.Footer().Element(ComposeFooter);
                    });
                });

                document.GeneratePdf(filePath);
                
                LoggingService.Instance.LogInfo($"PDF export completed successfully: {filePath}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during PDF export", ex);
                throw;
            }
        }

        private void ComposeHeader(IContainer container, EinsatzData einsatzData, PdfExportOptions options)
        {
            container.Column(column =>
            {
                // Oberste Zeile: Logo und Staffelname
                column.Item().Row(row =>
                {
                    // Logo (falls vorhanden) - KLEINERE HÖHE
                    if (options.IncludeLogo && !string.IsNullOrEmpty(options.LogoPath) && File.Exists(options.LogoPath))
                    {
                        row.ConstantItem(80).Height(50).AlignLeft().Image(options.LogoPath, ImageScaling.FitArea);
                    }

                    // Titel und Staffelname
                    row.RelativeItem().Column(titleColumn =>
                    {
                        titleColumn.Item().AlignCenter().Text("EINSATZDOKUMENTATION")
                            .FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                        
                        if (options.IncludeStaffelName && !string.IsNullOrEmpty(options.StaffelName))
                        {
                            titleColumn.Item().AlignCenter().Text(options.StaffelName)
                                .FontSize(12).Bold().FontColor(Colors.Grey.Darken2);
                        }
                    });

                    // Rechts: Platz für zusätzliche Infos oder leer lassen
                    row.ConstantItem(80);
                });

                // Trennlinie
                column.Item().PaddingVertical(8).LineHorizontal(1.5f).LineColor(Colors.Blue.Darken2);

                // Einsatzinformationen in kompakter Grid-Form
                column.Item().PaddingTop(8).Row(row =>
                {
                    // Linke Spalte
                    row.RelativeItem().Column(leftCol =>
                    {
                        if (options.IncludeEinsatzNummer && !string.IsNullOrEmpty(options.EinsatzNummer))
                        {
                            leftCol.Item().Text(text =>
                            {
                                text.Span("Einsatznr: ").FontSize(9).Bold();
                                text.Span(options.EinsatzNummer).FontSize(9);
                            });
                        }
                        
                        leftCol.Item().Text(text =>
                        {
                            text.Span("Typ: ").FontSize(9).Bold();
                            text.Span(einsatzData.EinsatzTyp).FontSize(9);
                        });
                        
                        leftCol.Item().Text(text =>
                        {
                            text.Span("Datum: ").FontSize(9).Bold();
                            text.Span(einsatzData.EinsatzDatum.ToString("dd.MM.yyyy HH:mm")).FontSize(9);
                        });
                        
                        if (options.IncludeAlarmierungsZeit && options.AlarmierungsZeit.HasValue)
                        {
                            leftCol.Item().Text(text =>
                            {
                                text.Span("Alarmierung: ").FontSize(9).Bold();
                                text.Span(options.AlarmierungsZeit.Value.ToString("dd.MM.yyyy HH:mm")).FontSize(9);
                            });
                        }
                    });

                    // Rechte Spalte
                    row.RelativeItem().Column(rightCol =>
                    {
                        rightCol.Item().Text(text =>
                        {
                            text.Span("Ort: ").FontSize(9).Bold();
                            text.Span(einsatzData.Einsatzort).FontSize(9);
                        });
                        
                        rightCol.Item().Text(text =>
                        {
                            text.Span("Einsatzleiter: ").FontSize(9).Bold();
                            text.Span(einsatzData.Einsatzleiter).FontSize(9);
                        });
                        
                        if (!string.IsNullOrEmpty(einsatzData.Fuehrungsassistent))
                        {
                            rightCol.Item().Text(text =>
                            {
                                text.Span("Führungsass.: ").FontSize(9).Bold();
                                text.Span(einsatzData.Fuehrungsassistent).FontSize(9);
                            });
                        }
                        
                        if (options.IncludeAlarmierendeOrg && !string.IsNullOrEmpty(options.AlarmierendeOrganisation))
                        {
                            rightCol.Item().Text(text =>
                            {
                                text.Span("Alarmiert: ").FontSize(9).Bold();
                                text.Span(options.AlarmierendeOrganisation).FontSize(9);
                            });
                        }
                    });
                });
            });
        }

        private void ComposeContent(IContainer container, EinsatzData einsatzData, List<Team> teams, PdfExportOptions options)
        {
            container.PaddingTop(15).Column(column =>
            {
                // Teamübersicht
                column.Item().Text("Eingesetzte Teams").FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                column.Item().PaddingBottom(8).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                if (!teams.Any())
                {
                    column.Item().PaddingTop(8).Text("Keine Teams im Einsatz").FontSize(9).FontColor(Colors.Grey.Darken1);
                }
                else
                {
                    // Teams-Tabelle mit flexibler Höhe
                    column.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(25);  // Nr.
                            columns.RelativeColumn(2);   // Team/Hund
                            columns.RelativeColumn(2);   // Hundeführer
                            columns.RelativeColumn(1.5f); // Typ
                            columns.ConstantColumn(55);   // Zeit
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Element(TableHeaderStyle).Text("Nr.").FontSize(9);
                            header.Cell().Element(TableHeaderStyle).Text("Team / Hund").FontSize(9);
                            header.Cell().Element(TableHeaderStyle).Text("Hundeführer").FontSize(9);
                            header.Cell().Element(TableHeaderStyle).Text("Typ").FontSize(9);
                            header.Cell().Element(TableHeaderStyle).Text("Zeit").FontSize(9);
                        });

                        // Team-Daten
                        int index = 1;
                        foreach (var team in teams.OrderBy(t => t.TeamId))
                        {
                            var isEven = index % 2 == 0;
                            
                            table.Cell().Element(c => TableCellStyle(c, isEven)).AlignCenter().Text(index.ToString()).FontSize(9);
                            table.Cell().Element(c => TableCellStyle(c, isEven))
                                .Column(col =>
                                {
                                    col.Item().Text(team.TeamName).FontSize(9).Bold();
                                    col.Item().Text(team.HundName).FontSize(8).FontColor(Colors.Grey.Darken1);
                                });
                            table.Cell().Element(c => TableCellStyle(c, isEven)).Text(team.Hundefuehrer).FontSize(9);
                            table.Cell().Element(c => TableCellStyle(c, isEven)).Text(team.TeamTypeShortName).FontSize(8);
                            table.Cell().Element(c => TableCellStyle(c, isEven)).AlignRight().Text(team.ElapsedTimeString).FontSize(9).Bold();

                            index++;
                        }
                    });
                }

                // Statistiken (KOMPAKT)
                column.Item().PaddingTop(15).Text("Einsatz-Statistik").FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                column.Item().PaddingBottom(8).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                column.Item().PaddingTop(8).Row(statsRow =>
                {
                    statsRow.RelativeItem().Column(leftStats =>
                    {
                        leftStats.Item().Text(text =>
                        {
                            text.Span("Anzahl Teams: ").FontSize(9).Bold();
                            text.Span(teams.Count.ToString()).FontSize(9);
                        });
                        
                        leftStats.Item().Text(text =>
                        {
                            text.Span("Aktive Teams: ").FontSize(9).Bold();
                            text.Span(teams.Count(t => t.IsRunning).ToString()).FontSize(9);
                        });
                        
                        if (teams.Any())
                        {
                            var maxTime = teams.Max(t => t.ElapsedTime);
                            leftStats.Item().Text(text =>
                            {
                                text.Span("Längste Zeit: ").FontSize(9).Bold();
                                text.Span(FormatTimeSpan(maxTime)).FontSize(9);
                            });
                        }
                    });

                    statsRow.RelativeItem().Column(rightStats =>
                    {
                        if (teams.Any())
                        {
                            var avgTime = TimeSpan.FromSeconds(teams.Average(t => t.ElapsedTime.TotalSeconds));
                            rightStats.Item().Text(text =>
                            {
                                text.Span("Ø Einsatzzeit: ").FontSize(9).Bold();
                                text.Span(FormatTimeSpan(avgTime)).FontSize(9);
                            });
                        }
                        
                        var totalWarnings = einsatzData.GlobalNotesEntries.Count(n =>
                            n.EntryType == GlobalNotesEntryType.Warning1 ||
                            n.EntryType == GlobalNotesEntryType.Warning2);

                        rightStats.Item().Text(text =>
                        {
                            text.Span("Warnungen: ").FontSize(9).Bold();
                            text.Span(totalWarnings.ToString()).FontSize(9);
                        });
                    });
                });

                // Einsatz-Chronologie (falls aktiviert) - auf neuer Seite falls nötig
                if (options.IncludeTimeline)
                {
                    column.Item().PaddingTop(15).PageBreak();
                    
                    column.Item().Text("Einsatz-Chronologie & Thread-Diskussionen").FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                    column.Item().PaddingBottom(8).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                    // Neue Thread-basierte Darstellung
                    RenderThreadBasedTimeline(column, einsatzData, options);
                }
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Row(row =>
            {
                row.RelativeItem().AlignLeft().Text(time =>
                {
                    time.Span("Erstellt: ").FontSize(7).FontColor(Colors.Grey.Darken1);
                    time.Span($"{DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(7).FontColor(Colors.Grey.Darken1);
                });

                row.RelativeItem().AlignCenter().Text(text =>
                {
                    text.Span("Seite ").FontSize(7);
                    text.CurrentPageNumber().FontSize(7);
                    text.Span(" / ").FontSize(7);
                    text.TotalPages().FontSize(7);
                });

                row.RelativeItem().AlignRight().Text("Einsatzüberwachung Professional v1.7")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);
            });
        }

        // Helper-Methoden für Styling
        private static IContainer TableHeaderStyle(IContainer container)
        {
            return container
                .Background(Colors.Blue.Darken2)
                .Padding(4)
                .DefaultTextStyle(x => x.FontColor(Colors.White).Bold());
        }

        private static IContainer TableCellStyle(IContainer container, bool isEven)
        {
            return container
                .Background(isEven ? Colors.Grey.Lighten4 : Colors.White)
                .Padding(4)
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten1);
        }

        private bool IsEinsatzRelevantNote(GlobalNotesEntryType type)
        {
            return type switch
            {
                GlobalNotesEntryType.EinsatzUpdate => true,
                GlobalNotesEntryType.TeamEvent => true,
                GlobalNotesEntryType.TimerReset => true,
                GlobalNotesEntryType.Warning1 => true,
                GlobalNotesEntryType.Warning2 => true,
                GlobalNotesEntryType.Funkspruch => true,
                GlobalNotesEntryType.Manual => true,
                _ => false
            };
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.Days > 0)
                return $"{timeSpan.Days}d {timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";

            return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }

        /// <summary>
        /// Rendert die Timeline mit Thread-Struktur für PDF-Export
        /// </summary>
        private void RenderThreadBasedTimeline(ColumnDescriptor column, EinsatzData einsatzData, PdfExportOptions options)
        {
            try
            {
                // Hole alle relevanten Notizen und organisiere sie in Threads
                var allNotes = einsatzData.GlobalNotesEntries
                    .Where(n => IsEinsatzRelevantNote(n.EntryType))
                    .OrderBy(n => n.Timestamp)
                    .ToList();

                if (!allNotes.Any())
                {
                    column.Item().PaddingTop(8).Text("Keine Ereignisse protokolliert").FontSize(9).FontColor(Colors.Grey.Darken1);
                    return;
                }

                // Organisiere Notizen in Thread-Struktur
                var threadStructure = OrganizeNotesIntoThreads(allNotes);
                
                // Limitiere für PDF-Darstellung
                var maxEntries = options.MaxTimelineEntries ?? 100;
                var displayedCount = 0; // Changed from ref parameter to regular variable

                foreach (var threadGroup in threadStructure.Take(maxEntries / 5)) // Durchschnittlich 5 Entries pro Thread
                {
                    if (displayedCount >= maxEntries) break;

                    displayedCount = RenderThreadInPdf(column, threadGroup, displayedCount, maxEntries); // Return new count
                    
                    // Abstand zwischen Thread-Gruppen
                    column.Item().PaddingVertical(8);
                }

                if (threadStructure.Count() * 5 > maxEntries)
                {
                    var remainingCount = allNotes.Count - displayedCount;
                    column.Item().PaddingTop(5).Text($"... und {remainingCount} weitere Ereignisse (gekürzt für PDF)")
                        .FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                }

                // Thread-Statistiken am Ende
                if (options.IncludeThreadStats)
                {
                    RenderThreadStatistics(column, allNotes, threadStructure);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error rendering thread-based timeline", ex);
                
                // Fallback zur alten Darstellung
                column.Item().PaddingTop(8).Text("Fehler beim Laden der Thread-Struktur - Darstellung als einfache Liste")
                    .FontSize(8).FontColor(Colors.Red.Medium);
                
                RenderSimpleTimeline(column, einsatzData);
            }
        }

        /// <summary>
        /// Organisiert Notizen in Thread-Struktur für PDF-Darstellung
        /// </summary>
        private IEnumerable<ThreadGroup> OrganizeNotesIntoThreads(List<GlobalNotesEntry> notes)
        {
            var threadGroups = new List<ThreadGroup>();
            var processedNotes = new HashSet<string>();

            foreach (var note in notes.Where(n => n.IsThreadRoot))
            {
                if (processedNotes.Contains(note.Id)) continue;

                var threadGroup = new ThreadGroup
                {
                    RootNote = note,
                    AllReplies = CollectAllReplies(note, notes, processedNotes)
                };

                threadGroups.Add(threadGroup);
                processedNotes.Add(note.Id);
            }

            // Einzelne Notizen ohne Threads hinzufügen
            foreach (var note in notes.Where(n => !n.IsReply && !processedNotes.Contains(n.Id)))
            {
                var threadGroup = new ThreadGroup
                {
                    RootNote = note,
                    AllReplies = new List<GlobalNotesEntry>()
                };

                threadGroups.Add(threadGroup);
                processedNotes.Add(note.Id);
            }

            return threadGroups.OrderBy(tg => tg.RootNote.Timestamp);
        }

        /// <summary>
        /// Sammelt alle Antworten eines Threads rekursiv
        /// </summary>
        private List<GlobalNotesEntry> CollectAllReplies(GlobalNotesEntry rootNote, List<GlobalNotesEntry> allNotes, HashSet<string> processedNotes)
        {
            var replies = new List<GlobalNotesEntry>();

            // Finde direkte Antworten
            var directReplies = allNotes.Where(n => n.ReplyToEntryId == rootNote.Id).OrderBy(n => n.Timestamp).ToList();

            foreach (var reply in directReplies)
            {
                if (processedNotes.Contains(reply.Id)) continue;

                replies.Add(reply);
                processedNotes.Add(reply.Id);

                // Rekursiv: Sammle Antworten auf diese Antwort
                var subReplies = CollectAllReplies(reply, allNotes, processedNotes);
                replies.AddRange(subReplies);
            }

            return replies;
        }

        /// <summary>
        /// Rendert einen Thread im PDF
        /// </summary>
        private int RenderThreadInPdf(ColumnDescriptor column, ThreadGroup threadGroup, int displayedCount, int maxEntries)
        {
            if (displayedCount >= maxEntries) return displayedCount;

            // Root-Nachricht (Haupteintrag)
            column.Item().PaddingTop(3).Border(1).BorderColor(Colors.Blue.Lighten3).Padding(8).Column(threadCol =>
            {
                // Zeitstempel und Icon
                threadCol.Item().Row(headerRow =>
                {
                    headerRow.ConstantItem(90).Text($"{threadGroup.RootNote.FormattedTimestamp}").FontSize(9).Bold().FontColor(Colors.Blue.Darken2);
                    headerRow.ConstantItem(30).Text($"{threadGroup.RootNote.EntryTypeIcon}").FontSize(10).AlignCenter();
                    headerRow.RelativeItem().Column(contentCol =>
                    {
                        // Hauptinhalt
                        contentCol.Item().Text($"{threadGroup.RootNote.Content}").FontSize(9).Bold();
                        
                        // Team-Info falls vorhanden
                        if (!string.IsNullOrEmpty(threadGroup.RootNote.TeamName))
                        {
                            contentCol.Item().Text($"Team: {threadGroup.RootNote.TeamName}")
                                .FontSize(8).FontColor(Colors.Grey.Darken1).Italic();
                        }
                    });
                    
                    // Thread-Info (Anzahl Antworten)
                    if (threadGroup.AllReplies.Any())
                    {
                        headerRow.ConstantItem(80).AlignRight().Text($"💬 {threadGroup.AllReplies.Count} Antwort{(threadGroup.AllReplies.Count != 1 ? "en" : "")}")
                            .FontSize(7).FontColor(Colors.Orange.Medium);
                    }
                });
            });

            displayedCount++;

            // Antworten (verschachtelt)
            if (threadGroup.AllReplies.Any() && displayedCount < maxEntries)
            {
                column.Item().PaddingTop(8).Column(repliesCol =>
                {
                    var repliesToShow = Math.Min(threadGroup.AllReplies.Count, maxEntries - displayedCount);
                    for (int i = 0; i < repliesToShow; i++)
                    {
                        RenderReplyInPdf(repliesCol, threadGroup.AllReplies[i], threadGroup.AllReplies);
                        displayedCount++;
                    }

                    // Falls mehr Antworten vorhanden sind als angezeigt werden können
                    var remainingReplies = threadGroup.AllReplies.Count - repliesToShow;
                    if (remainingReplies > 0)
                    {
                        repliesCol.Item().PaddingLeft(20).Text($"... und {remainingReplies} weitere Antworten")
                            .FontSize(7).Italic().FontColor(Colors.Grey.Darken1);
                    }
                });
            }

            return displayedCount;
        }

        /// <summary>
        /// Rendert eine einzelne Antwort im PDF
        /// </summary>
        private void RenderReplyInPdf(ColumnDescriptor column, GlobalNotesEntry reply, List<GlobalNotesEntry> allReplies)
        {
            var indentLevel = Math.Min(reply.ThreadDepth, 3); // Max 3 Einrückungsebenen
            var leftPadding = 10 + (indentLevel * 15); // 10px base + 15px pro Ebene

            column.Item().PaddingTop(4).PaddingLeft(leftPadding).Row(replyRow =>
            {
                // Thread-Linie und Pfeil
                replyRow.ConstantItem(20).Column(arrowCol =>
                {
                    arrowCol.Item().AlignCenter().Text("↳").FontSize(10).FontColor(Colors.Orange.Medium);
                });

                // Zeitstempel
                replyRow.ConstantItem(70).Text($"{reply.FormattedTimestamp}").FontSize(8).FontColor(Colors.Grey.Darken1);

                // Icon
                replyRow.ConstantItem(20).Text($"{reply.EntryTypeIcon}").FontSize(8).AlignCenter();

                // Inhalt
                replyRow.RelativeItem().Column(contentCol =>
                {
                    contentCol.Item().Text($"{reply.Content}").FontSize(8);
                    
                    // Team-Info für Antwort
                    if (!string.IsNullOrEmpty(reply.TeamName))
                    {
                        contentCol.Item().Text($"→ {reply.TeamName}")
                            .FontSize(7).FontColor(Colors.Grey.Darken2).Italic();
                    }
                });
            });
        }

        /// <summary>
        /// Rendert Thread-Statistiken im PDF
        /// </summary>
        private void RenderThreadStatistics(ColumnDescriptor column, List<GlobalNotesEntry> allNotes, IEnumerable<ThreadGroup> threadStructure)
        {
            column.Item().PaddingTop(20).Column(statsCol =>
            {
                statsCol.Item().Text("Thread-Statistiken").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                statsCol.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                var threadsWithReplies = threadStructure.Count(tg => tg.AllReplies.Any());
                var totalReplies = threadStructure.Sum(tg => tg.AllReplies.Count);
                var averageRepliesPerThread = threadsWithReplies > 0 ? (double)totalReplies / threadsWithReplies : 0;

                statsCol.Item().PaddingTop(5).Row(statsRow =>
                {
                    statsRow.RelativeItem().Column(leftStatsCol =>
                    {
                        leftStatsCol.Item().Text($"Gesamt Threads: {threadStructure.Count()}").FontSize(9);
                        leftStatsCol.Item().Text($"Threads mit Antworten: {threadsWithReplies}").FontSize(9);
                        leftStatsCol.Item().Text($"Gesamt Antworten: {totalReplies}").FontSize(9);
                    });

                    statsRow.RelativeItem().Column(rightStatsCol =>
                    {
                        rightStatsCol.Item().Text($"Ø Antworten/Thread: {averageRepliesPerThread:F1}").FontSize(9);
                        
                        var mostActiveThread = threadStructure.OrderByDescending(tg => tg.AllReplies.Count).FirstOrDefault();
                        if (mostActiveThread != null && mostActiveThread.AllReplies.Any())
                        {
                            rightStatsCol.Item().Text($"Aktivster Thread: {mostActiveThread.AllReplies.Count} Antworten").FontSize(9);
                        }
                    });
                });
            });
        }

        /// <summary>
        /// Fallback: Rendert Timeline ohne Thread-Struktur
        /// </summary>
        private void RenderSimpleTimeline(ColumnDescriptor column, EinsatzData einsatzData)
        {
            var relevantNotes = einsatzData.GlobalNotesEntries
                .Where(n => IsEinsatzRelevantNote(n.EntryType))
                .OrderBy(n => n.Timestamp)
                .Take(50)
                .ToList();

            foreach (var note in relevantNotes)
            {
                column.Item().PaddingTop(3).Row(row =>
                {
                    row.ConstantItem(90).Text($"{note.FormattedTimestamp}").FontSize(8).FontColor(Colors.Grey.Darken2);
                    row.RelativeItem().Text($"{note.EntryTypeIcon} {note.Content}").FontSize(8);
                });
            }
        }
    }

    /// <summary>
    /// Konfigurationsoptionen für PDF-Export
    /// </summary>
    public class PdfExportOptions
    {
        public bool IncludeEinsatzNummer { get; set; } = true;
        public string EinsatzNummer { get; set; } = string.Empty;

        public bool IncludeAlarmierendeOrg { get; set; } = true;
        public string AlarmierendeOrganisation { get; set; } = string.Empty;

        public bool IncludeStaffelName { get; set; } = true;
        public string StaffelName { get; set; } = string.Empty;

        public bool IncludeLogo { get; set; } = true;
        public string LogoPath { get; set; } = string.Empty;

        public bool IncludeAlarmierungsZeit { get; set; } = true;
        public DateTime? AlarmierungsZeit { get; set; }

        public bool IncludeTimeline { get; set; } = true;

        // NEUE Thread-Optionen
        public bool IncludeThreadStats { get; set; } = true;
        public int? MaxTimelineEntries { get; set; } = 100;
        public bool ShowThreadStructure { get; set; } = true;
        public int MaxThreadDepth { get; set; } = 3;
        public bool GroupByThreads { get; set; } = true;
    }

    /// <summary>
    /// Repräsentiert eine Thread-Gruppe für PDF-Export
    /// </summary>
    public class ThreadGroup
    {
        public GlobalNotesEntry RootNote { get; set; } = null!;
        public List<GlobalNotesEntry> AllReplies { get; set; } = new();

        /// <summary>
        /// Gesamtanzahl der Nachrichten in diesem Thread (Root + Replies)
        /// </summary>
        public int TotalMessages => 1 + AllReplies.Count;

        /// <summary>
        /// Zeitspanne des Threads (von der ersten bis zur letzten Nachricht)
        /// </summary>
        public TimeSpan ThreadDuration 
        {
            get
            {
                if (!AllReplies.Any()) return TimeSpan.Zero;
                
                var firstTime = RootNote.Timestamp;
                var lastTime = AllReplies.Max(r => r.Timestamp);
                return lastTime - firstTime;
            }
        }

        /// <summary>
        /// Beteiligten Teams in diesem Thread
        /// </summary>
        public IEnumerable<string> ParticipatingTeams
        {
            get
            {
                var teams = new HashSet<string>();
                
                if (!string.IsNullOrEmpty(RootNote.TeamName))
                    teams.Add(RootNote.TeamName);
                
                foreach (var reply in AllReplies.Where(r => !string.IsNullOrEmpty(r.TeamName)))
                {
                    teams.Add(reply.TeamName);
                }
                
                return teams;
            }
        }

        /// <summary>
        /// Maximale Verschachtelungstiefe in diesem Thread
        /// </summary>
        public int MaxDepth => AllReplies.Any() ? AllReplies.Max(r => r.ThreadDepth) : 0;
    }
}
