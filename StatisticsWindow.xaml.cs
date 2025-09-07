using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Einsatzueberwachung.Models;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;

namespace Einsatzueberwachung
{
    public partial class StatisticsWindow : Window
    {
        private readonly EinsatzStatistics _statistics;
        private readonly List<Team> _teams;
        private readonly EinsatzData _einsatzData;

        public StatisticsWindow(List<Team> teams, EinsatzData einsatzData)
        {
            InitializeComponent();
            _teams = teams ?? new List<Team>();
            _einsatzData = einsatzData ?? new EinsatzData();
            _statistics = new EinsatzStatistics();
            
            UpdateStatistics();
            LoadData();
        }

        private void UpdateStatistics()
        {
            _statistics.EinsatzStart = _einsatzData.EinsatzDatum;
            
            foreach (var team in _teams)
            {
                _statistics.UpdateTeamStatistic(team);
            }
        }

        private void LoadData()
        {
            LoadQuickMetrics();
            LoadTeamRankings();
            LoadTeamTypeDistribution();
            LoadRecommendations();
            LoadTimeline();
            
            LastUpdateText.Text = $"Letzte Aktualisierung: {DateTime.Now:HH:mm:ss}";
        }

        private void LoadQuickMetrics()
        {
            var report = _statistics.GenerateReport();
            
            EinsatzdauerValue.Text = FormatTimeSpan(report.EinsatzDauer);
            AnzahlTeamsValue.Text = report.AnzahlTeams.ToString();
            AktiveTeamsValue.Text = report.AktiveTeams.ToString();
            DurchschnittszeitValue.Text = FormatTimeSpan(report.DurchschnittsEinsatzzeit);
            
            TeamAuslastungProgress.Value = report.TeamAuslastung;
        }

        private void LoadTeamRankings()
        {
            var rankings = _teams
                .Select((team, index) => new TeamRankingItem
                {
                    Rank = (index + 1).ToString(),
                    TeamName = $"{team.TeamName} ({string.Join(", ", team.MultipleTeamTypes?.SelectedTypes?.Select(t => t.ToString()) ?? new[] { "Unbekannt" })})",
                    Einsatzzeit = FormatTimeSpan(team.ElapsedTime),
                    Status = team.IsRunning ? "Aktiv" : "Bereit",
                    StatusColor = team.IsRunning ? Brushes.Green : Brushes.Gray
                })
                .OrderByDescending(r => _teams.FirstOrDefault(t => r.TeamName.Contains(t.TeamName))?.ElapsedTime ?? TimeSpan.Zero)
                .ToList();

            TeamRankingsList.ItemsSource = rankings;
        }

        private void LoadTeamTypeDistribution()
        {
            var allTypes = _teams
                .SelectMany(t => t.MultipleTeamTypes?.SelectedTypes?.Select(type => type.ToString()) ?? new[] { "Unbekannt" })
                .ToList();
                
            var typeGroups = allTypes
                .GroupBy(type => type)
                .Select(g => new TeamTypeDistributionItem
                {
                    TeamType = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / allTypes.Count * 100,
                    Color = GetTeamTypeColor(g.Key),
                    DetailText = $"{g.Count()} Teams • {(double)g.Count() / allTypes.Count * 100:F1}%"
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            TeamTypeDistribution.ItemsSource = typeGroups;
        }

        private void LoadRecommendations()
        {
            var report = _statistics.GenerateReport();
            var recommendations = new List<RecommendationItem>();

            foreach (var rec in report.Empfehlungen)
            {
                var priority = GetRecommendationPriority(rec);
                recommendations.Add(new RecommendationItem
                {
                    Title = ExtractTitle(rec),
                    Description = ExtractDescription(rec),
                    Priority = priority
                });
            }

            // Zusätzliche intelligente Empfehlungen
            if (_teams.Any(t => t.ElapsedTime > TimeSpan.FromMinutes(60)))
            {
                recommendations.Add(new RecommendationItem
                {
                    Title = "Lange Einsatzzeiten erkannt",
                    Description = "Einige Teams sind bereits über 60 Minuten im Einsatz. Erwägen Sie eine Pause oder Rotation.",
                    Priority = Brushes.Orange
                });
            }

            if (!_teams.Any(t => t.IsRunning))
            {
                recommendations.Add(new RecommendationItem
                {
                    Title = "Keine aktiven Teams",
                    Description = "Aktuell sind keine Teams aktiv. Starten Sie Teams für den Einsatz.",
                    Priority = Brushes.Red
                });
            }

            RecommendationsList.ItemsSource = recommendations;
        }

        private void LoadTimeline()
        {
            var events = new List<TimelineEvent>();
            
            // Einsatzstart
            events.Add(new TimelineEvent
            {
                Time = _einsatzData.EinsatzDatum.ToString("HH:mm"),
                EventType = "Start",
                TeamName = "Einsatzleitung",
                EventColor = Brushes.Green
            });

            // Team-Events (vereinfacht - in echter Implementierung würden wir Timer-Events tracken)
            foreach (var team in _teams.Where(t => t.IsRunning))
            {
                events.Add(new TimelineEvent
                {
                    Time = DateTime.Now.AddMinutes(-team.ElapsedTime.TotalMinutes).ToString("HH:mm"),
                    EventType = "Timer Start",
                    TeamName = team.TeamName,
                    EventColor = Brushes.Blue
                });
            }

            TimelineEvents.ItemsSource = events.OrderBy(e => e.Time);
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.Days > 0)
                return $"{timeSpan.Days}d {timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            
            return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }

        private Brush GetTeamTypeColor(string teamType)
        {
            return teamType switch
            {
                "Flächensuchhund" => Brushes.Blue,
                "Trümmersuchhund" => Brushes.Orange,
                "Mantrailer" => Brushes.Green,
                "Wasserortung" => Brushes.Cyan,
                "Lawinensuchhund" => Brushes.Purple,
                "Allgemein" => Brushes.Gray,
                _ => Brushes.DarkGray
            };
        }

        private Brush GetRecommendationPriority(string recommendation)
        {
            if (recommendation.Contains("⚠️") || recommendation.Contains("Warnung"))
                return Brushes.Red;
            if (recommendation.Contains("🔄") || recommendation.Contains("⏰"))
                return Brushes.Orange;
            if (recommendation.Contains("🏆"))
                return Brushes.Green;
            
            return Brushes.Blue;
        }

        private string ExtractTitle(string recommendation)
        {
            var parts = recommendation.Split(':');
            return parts.Length > 0 ? parts[0].Trim() : recommendation;
        }

        private string ExtractDescription(string recommendation)
        {
            var parts = recommendation.Split(':');
            return parts.Length > 1 ? string.Join(":", parts.Skip(1)).Trim() : string.Empty;
        }

        private void RefreshStats_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatistics();
            LoadData();
        }

        private void ExportReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var report = _statistics.GenerateReport();
                
                var saveDialog = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|Alle Dateien (*.*)|*.*",
                    FileName = $"EinsatzStatistik_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var json = JsonSerializer.Serialize(report, new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });
                    
                    File.WriteAllText(saveDialog.FileName, json);
                    
                    MessageBox.Show($"Statistik-Report erfolgreich exportiert:\n{saveDialog.FileName}", 
                                  "Export erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Export:\n{ex.Message}", 
                              "Export-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Helper Classes für Data Binding
    public class TeamRankingItem
    {
        public string Rank { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string Einsatzzeit { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Brush StatusColor { get; set; } = Brushes.Gray;
    }

    public class TeamTypeDistributionItem
    {
        public string TeamType { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        public Brush Color { get; set; } = Brushes.Gray;
        public string DetailText { get; set; } = string.Empty;
    }

    public class RecommendationItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Brush Priority { get; set; } = Brushes.Blue;
    }

    public class TimelineEvent
    {
        public string Time { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public Brush EventColor { get; set; } = Brushes.Blue;
    }
}
