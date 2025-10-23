using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Einsatzueberwachung.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Einsatzueberwachung.Services
{
    /// <summary>
    /// Service für PDF-Export von Karten mit Suchgebieten
    /// </summary>
    public class MapPdfExportService
    {
        public bool ExportMapToPdf(string filePath, EinsatzData? einsatzData, List<SearchArea> searchAreas, string mapImagePath)
        {
            try
            {
                LoggingService.Instance.LogInfo($"Starting map PDF export to: {filePath}");
                LoggingService.Instance.LogInfo($"Map image path: {mapImagePath}");
                LoggingService.Instance.LogInfo($"Map image exists: {File.Exists(mapImagePath)}");
                LoggingService.Instance.LogInfo($"Search areas count: {searchAreas?.Count ?? 0}");
                LoggingService.Instance.LogInfo($"ELW position: {einsatzData?.ElwPosition?.ToString() ?? "null"}");

                if (!File.Exists(mapImagePath))
                {
                    LoggingService.Instance.LogError($"Map image file not found: {mapImagePath}");
                    return false;
                }

                // QuestPDF License für Community-Nutzung
                QuestPDF.Settings.License = LicenseType.Community;
                LoggingService.Instance.LogInfo("QuestPDF license set to Community");

                // Erstelle PDF-Dokument im Querformat
                LoggingService.Instance.LogInfo("Creating PDF document...");
                
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        // Querformat: A4 29.7cm x 21cm
                        page.Size(29.7f, 21, Unit.Centimetre);
                        page.Margin(1, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Black));

                        // Header mit ausreichend Platz (kompakt aber nicht zu eng)
                        page.Header().Element(c => ComposeHeader(c, einsatzData));

                        // Content: Zwei-Spalten-Layout
                        page.Content().Element(c => ComposeContent(c, einsatzData, searchAreas, mapImagePath));

                        // Footer
                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Seite ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                            x.Span(" • Einsatzüberwachung Professional");
                        });
                    });
                })
                .GeneratePdf(filePath);

                LoggingService.Instance.LogInfo("PDF generation completed");
                
                // Überprüfe ob Datei erstellt wurde
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    LoggingService.Instance.LogInfo($"Map PDF export completed successfully: {filePath} ({fileInfo.Length} bytes)");
                    return true;
                }
                else
                {
                    LoggingService.Instance.LogError($"PDF file was not created: {filePath}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error creating map PDF: {ex.Message}", ex);
                LoggingService.Instance.LogError($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private void ComposeHeader(IContainer container, EinsatzData? einsatzData)
        {
            container.Background("#F57C00").Padding(8).Column(column =>
            {
                // Titel-Zeile
                column.Item().Text("🗺️ KARTEN-ÜBERSICHT")
                    .FontSize(16)
                    .Bold()
                    .FontColor(Colors.White);

                if (einsatzData != null)
                {
                    // Info-Zeile mit mehreren Elementen nebeneinander
                    column.Item().PaddingTop(4).Row(infoRow =>
                    {
                        // Einsatzort
                        infoRow.AutoItem().Text($"📍 {einsatzData.Einsatzort}")
                            .FontSize(10)
                            .FontColor(Colors.White);

                        // Einsatzleiter
                        if (!string.IsNullOrEmpty(einsatzData.Einsatzleiter))
                        {
                            infoRow.AutoItem().PaddingLeft(15).Text($"👤 {einsatzData.Einsatzleiter}")
                                .FontSize(10)
                                .FontColor(Colors.White);
                        }

                        // Datum
                        infoRow.AutoItem().PaddingLeft(15).Text($"📅 {einsatzData.EinsatzDatum:dd.MM.yyyy HH:mm}")
                            .FontSize(10)
                            .FontColor(Colors.White);
                    });
                }

                // Aktuelle Zeit rechts oben
                column.Item().AlignRight().Text($"{DateTime.Now:dd.MM.yyyy HH:mm}")
                    .FontSize(8)
                    .FontColor(Colors.White);
            });
        }

        private void ComposeContent(IContainer container, EinsatzData? einsatzData, List<SearchArea> searchAreas, string mapImagePath)
        {
            container.PaddingTop(10).Row(row =>
            {
                // LINKE SPALTE: Legende & Informationen (30% Breite)
                row.RelativeItem(3).PaddingRight(10).Column(leftColumn =>
                {
                    // Legende mit Suchgebieten
                    if (searchAreas != null && searchAreas.Any())
                    {
                        leftColumn.Item().Element(c => ComposeLegendSidebar(c, searchAreas));
                        leftColumn.Item().PaddingTop(10);
                    }

                    // ELW-Position
                    if (einsatzData?.ElwPosition != null)
                    {
                        leftColumn.Item().Element(c => ComposeElwSection(c, einsatzData.ElwPosition.Value));
                        leftColumn.Item().PaddingTop(10);
                    }

                    // Statistik
                    if (searchAreas != null && searchAreas.Any())
                    {
                        leftColumn.Item().Element(c => ComposeStatisticsSection(c, searchAreas));
                    }
                });

                // RECHTE SPALTE: Karten-Bild (70% Breite)
                row.RelativeItem(7).Column(rightColumn =>
                {
                    if (File.Exists(mapImagePath))
                    {
                        rightColumn.Item().Element(c => ComposeMapImage(c, mapImagePath));
                    }
                    else
                    {
                        rightColumn.Item().AlignMiddle().AlignCenter().Text("❌ Karten-Bild konnte nicht geladen werden")
                            .FontColor(Colors.Red.Medium)
                            .FontSize(14);
                    }
                });
            });
        }

        private void ComposeMapImage(IContainer container, string mapImagePath)
        {
            container.Border(3).BorderColor("#F57C00").Background(Colors.Grey.Lighten4).Column(column =>
            {
                column.Item().Image(mapImagePath, ImageScaling.FitArea);
            });
        }

        private void ComposeLegendSidebar(IContainer container, List<SearchArea> searchAreas)
        {
            container.Border(1).BorderColor("#F57C00").Background("#FFF3E0").Padding(10).Column(column =>
            {
                column.Item().Text("📍 SUCHGEBIETE")
                    .FontSize(11)
                    .Bold()
                    .FontColor("#F57C00");

                column.Item().PaddingTop(5).LineHorizontal(1).LineColor("#F57C00");

                column.Item().PaddingTop(8);

                // Liste der Suchgebiete
                foreach (var area in searchAreas.OrderBy(a => a.Name))
                {
                    column.Item().PaddingBottom(6).Row(legendRow =>
                    {
                        // Farbbox
                        legendRow.ConstantItem(20).Height(15)
                            .Background($"#{area.Color.R:X2}{area.Color.G:X2}{area.Color.B:X2}")
                            .Border(1)
                            .BorderColor(Colors.Grey.Darken2);

                        legendRow.ConstantItem(8); // Abstand

                        // Informationen
                        legendRow.RelativeItem().Column(infoCol =>
                        {
                            infoCol.Item().Text(area.Name)
                                .Bold()
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken3);

                            if (!string.IsNullOrEmpty(area.AssignedTeam))
                            {
                                infoCol.Item().Text($"👥 {area.AssignedTeam}")
                                    .FontSize(8)
                                    .FontColor(Colors.Grey.Darken2);
                            }

                            infoCol.Item().Text($"📏 {area.FormattedArea}")
                                .FontSize(8)
                                .FontColor("#F57C00");

                            if (area.IsCompleted)
                            {
                                infoCol.Item().Text("✅ Abgesucht")
                                    .FontSize(7)
                                    .FontColor("#4CAF50");
                            }
                        });
                    });

                    // Trennlinie zwischen Gebieten
                    if (area != searchAreas.Last())
                    {
                        column.Item().PaddingVertical(3).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    }
                }
            });
        }

        private void ComposeElwSection(IContainer container, (double Latitude, double Longitude) elwPos)
        {
            container.Border(1).BorderColor("#DC143C").Background("#FFE4E1").Padding(8).Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.ConstantItem(25).AlignMiddle().Text("🚒")
                        .FontSize(16);

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("EINSATZLEITWAGEN")
                            .FontSize(10)
                            .Bold()
                            .FontColor("#DC143C");

                        col.Item().Text($"Lat: {elwPos.Latitude:F6}° N")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Darken3);

                        col.Item().Text($"Lon: {elwPos.Longitude:F6}° E")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Darken3);
                    });
                });
            });
        }

        private void ComposeStatisticsSection(IContainer container, List<SearchArea> searchAreas)
        {
            container.Border(1).BorderColor("#F57C00").Background("#FFF8E1").Padding(8).Column(column =>
            {
                column.Item().Text("📊 STATISTIK")
                    .FontSize(10)
                    .Bold()
                    .FontColor("#F57C00");

                column.Item().PaddingTop(5).LineHorizontal(1).LineColor("#F57C00");

                column.Item().PaddingTop(6);

                var totalArea = searchAreas.Sum(a => a.AreaInSquareMeters);
                var completedArea = searchAreas.Where(a => a.IsCompleted).Sum(a => a.AreaInSquareMeters);
                var completedCount = searchAreas.Count(a => a.IsCompleted);
                var teamsAssigned = searchAreas.Count(a => !string.IsNullOrEmpty(a.AssignedTeam));

                column.Item().PaddingBottom(3).Row(row =>
                {
                    row.RelativeItem().Text("Gesamt Gebiete:");
                    row.RelativeItem().AlignRight().Text($"{searchAreas.Count}").Bold();
                });

                column.Item().PaddingBottom(3).Row(row =>
                {
                    row.RelativeItem().Text("Zugeordnete Teams:");
                    row.RelativeItem().AlignRight().Text($"{teamsAssigned}").Bold().FontColor("#F57C00");
                });

                column.Item().PaddingBottom(3).Row(row =>
                {
                    row.RelativeItem().Text("Abgesucht:");
                    row.RelativeItem().AlignRight().Text($"{completedCount}").Bold().FontColor("#4CAF50");
                });

                column.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                column.Item().PaddingTop(6).PaddingBottom(3).Row(row =>
                {
                    row.RelativeItem().Text("Gesamtfläche:");
                    row.RelativeItem().AlignRight().Text(FormatArea(totalArea)).Bold();
                });

                column.Item().PaddingBottom(3).Row(row =>
                {
                    row.RelativeItem().Text("Abgesucht:");
                    row.RelativeItem().AlignRight().Text(FormatArea(completedArea)).Bold().FontColor("#4CAF50");
                });

                if (totalArea > 0)
                {
                    var percentage = (completedArea / totalArea) * 100;
                    column.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem().Text("Fortschritt:").Bold();
                        row.RelativeItem().AlignRight().Text($"{percentage:F1}%").Bold().FontSize(11).FontColor("#F57C00");
                    });
                }
            });
        }

        private string FormatArea(double sqm)
        {
            if (sqm < 50000)
                return $"{sqm:N0} m²";
            else if (sqm < 1000000)
                return $"{sqm / 10000.0:N2} ha";
            else
                return $"{sqm / 1000000.0:N2} km²";
        }
    }
}
