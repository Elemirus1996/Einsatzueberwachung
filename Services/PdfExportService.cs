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
                    
                    column.Item().Text("Einsatz-Chronologie").FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                    column.Item().PaddingBottom(8).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                    var relevantNotes = einsatzData.GlobalNotesEntries
                        .Where(n => IsEinsatzRelevantNote(n.EntryType))
                        .OrderBy(n => n.Timestamp)
                        .ToList();

                    if (!relevantNotes.Any())
                    {
                        column.Item().PaddingTop(8).Text("Keine Ereignisse protokolliert").FontSize(9).FontColor(Colors.Grey.Darken1);
                    }
                    else
                    {
                        // Limitiere auf maximal 50 Einträge um Platzprobleme zu vermeiden
                        var displayNotes = relevantNotes.Take(50).ToList();
                        
                        foreach (var note in displayNotes)
                        {
                            column.Item().PaddingTop(3).Row(row =>
                            {
                                row.ConstantItem(90).Text($"{note.FormattedTimestamp}").FontSize(8).FontColor(Colors.Grey.Darken2);
                                row.RelativeItem().Text($"{note.EntryTypeIcon} {note.Content}").FontSize(8);
                            });
                        }
                        
                        if (relevantNotes.Count > 50)
                        {
                            column.Item().PaddingTop(5).Text($"... und {relevantNotes.Count - 50} weitere Ereignisse")
                                .FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                        }
                    }
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
    }
}
