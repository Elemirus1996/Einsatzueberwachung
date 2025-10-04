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
                    // UPDATED: Use design system colors instead of hardcoded Brushes
                    StatusColor = team.IsRunning 
                        ? (Brush)Application.Current.FindResource("Success") 
                        : (Brush)Application.Current.FindResource("OnSurfaceVariant")
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
                    // UPDATED: Use Tertiary (Orange) statt Brushes.Orange
                    Priority = (Brush)Application.Current.FindResource("Tertiary")
                });
            }

            if (!_teams.Any(t => t.IsRunning))
            {
                recommendations.Add(new RecommendationItem
                {
                    Title = "Keine aktiven Teams",
                    Description = "Aktuell sind keine Teams aktiv. Starten Sie Teams für den Einsatz.",
                    // UPDATED: Use Error (Rot) statt Brushes.Red
                    Priority = (Brush)Application.Current.FindResource("Error")
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
                // UPDATED: Use design system color
                EventColor = (Brush)Application.Current.FindResource("Success")
            });

            // Team-Events (vereinfacht - in echter Implementierung würden wir Timer-Events tracken)
            foreach (var team in _teams.Where(t => t.IsRunning))
            {
                events.Add(new TimelineEvent
                {
                    Time = DateTime.Now.AddMinutes(-team.ElapsedTime.TotalMinutes).ToString("HH:mm"),
                    EventType = "Timer Start",
                    TeamName = team.TeamName,
                    // UPDATED: Use design system color
                    EventColor = (Brush)Application.Current.FindResource("Primary")
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
            // UPDATED: Use design system colors instead of hardcoded Brushes
            return teamType switch
            {
                "Flächensuchhund" => (Brush)Application.Current.FindResource("FlächeColor"),
                "Trümmersuchhund" => (Brush)Application.Current.FindResource("TrümmerColor"),
                "Mantrailer" => (Brush)Application.Current.FindResource("MantrailerColor"),
                "Wasserortung" => (Brush)Application.Current.FindResource("WasserColor"),
                "Lawinensuchhund" => (Brush)Application.Current.FindResource("LawineColor"),
                "Allgemein" => (Brush)Application.Current.FindResource("AllgemeinColor"),
                _ => (Brush)Application.Current.FindResource("OnSurfaceVariant")
            };
        }

        private Brush GetRecommendationPriority(string recommendation)
        {
            // UPDATED: Use design system colors - ORANGE statt Lila
            if (recommendation.Contains("⚠️") || recommendation.Contains("Warnung"))
                return (Brush)Application.Current.FindResource("Error");
            if (recommendation.Contains("🔄") || recommendation.Contains("⏰"))
                return (Brush)Application.Current.FindResource("Warning");
            if (recommendation.Contains("🏆"))
                return (Brush)Application.Current.FindResource("Success");
            
            // UPDATED: Verwende Tertiary (Orange) statt Primary
            return (Brush)Application.Current.FindResource("Tertiary");
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
        // UPDATED: Default to design system color
        public Brush StatusColor { get; set; } = (Brush)Application.Current.FindResource("OnSurfaceVariant");
    }

    public class TeamTypeDistributionItem
    {
        public string TeamType { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        // UPDATED: Default to design system color
        public Brush Color { get; set; } = (Brush)Application.Current.FindResource("OnSurfaceVariant");
        public string DetailText { get; set; } = string.Empty;
    }

    public class RecommendationItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        // UPDATED: Default to design system color
        public Brush Priority { get; set; } = (Brush)Application.Current.FindResource("Primary");
    }

    public class TimelineEvent
    {
        public string Time { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        // UPDATED: Default to design system color
        public Brush EventColor { get; set; } = (Brush)Application.Current.FindResource("Primary");
    }
}
