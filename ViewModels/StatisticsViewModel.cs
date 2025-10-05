using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    /// <summary>
    /// ViewModel für das StatisticsWindow - MVVM-Implementation v1.9.0
    /// Einsatz-Statistiken mit vollständiger Orange-Design-Integration
    /// </summary>
    public class StatisticsViewModel : BaseViewModel, IDisposable
    {
        private readonly EinsatzStatistics _statistics;
        private readonly List<Team> _teams;
        private readonly EinsatzData _einsatzData;

        // Quick Metrics Properties
        private string _einsatzdauerValue = "00:00:00";
        private string _anzahlTeamsValue = "0";
        private string _aktiveTeamsValue = "0";
        private string _durchschnittszeitValue = "00:00:00";
        private double _teamAuslastungProgress = 0.0;

        // Collections for Data Binding
        private ObservableCollection<TeamRankingItem> _teamRankings = new ObservableCollection<TeamRankingItem>();
        private ObservableCollection<TeamTypeDistributionItem> _teamTypeDistribution = new ObservableCollection<TeamTypeDistributionItem>();
        private ObservableCollection<RecommendationItem> _recommendations = new ObservableCollection<RecommendationItem>();
        private ObservableCollection<TimelineEvent> _timelineEvents = new ObservableCollection<TimelineEvent>();

        // UI State Properties
        private string _lastUpdateText = "Letzte Aktualisierung: Jetzt";
        private string _windowTitle = "📊 Einsatz-Statistiken - Einsatzüberwachung v1.9.0";
        private bool _isRefreshing = false;

        public StatisticsViewModel(List<Team> teams, EinsatzData einsatzData)
        {
            _teams = teams ?? new List<Team>();
            _einsatzData = einsatzData ?? new EinsatzData();
            _statistics = new EinsatzStatistics();

            InitializeCommands();
            UpdateStatistics();
            LoadData();

            LoggingService.Instance.LogInfo("StatisticsViewModel initialized with MVVM pattern v1.9.0");
        }

        #region Properties

        /// <summary>
        /// Formatierte Einsatzdauer
        /// </summary>
        public string EinsatzdauerValue
        {
            get => _einsatzdauerValue;
            set => SetProperty(ref _einsatzdauerValue, value);
        }

        /// <summary>
        /// Anzahl Teams gesamt
        /// </summary>
        public string AnzahlTeamsValue
        {
            get => _anzahlTeamsValue;
            set => SetProperty(ref _anzahlTeamsValue, value);
        }

        /// <summary>
        /// Anzahl aktive Teams
        /// </summary>
        public string AktiveTeamsValue
        {
            get => _aktiveTeamsValue;
            set => SetProperty(ref _aktiveTeamsValue, value);
        }

        /// <summary>
        /// Durchschnittliche Einsatzzeit
        /// </summary>
        public string DurchschnittszeitValue
        {
            get => _durchschnittszeitValue;
            set => SetProperty(ref _durchschnittszeitValue, value);
        }

        /// <summary>
        /// Team-Auslastung in Prozent (0-100)
        /// </summary>
        public double TeamAuslastungProgress
        {
            get => _teamAuslastungProgress;
            set => SetProperty(ref _teamAuslastungProgress, value);
        }

        /// <summary>
        /// Team-Rankings nach Einsatzzeit
        /// </summary>
        public ObservableCollection<TeamRankingItem> TeamRankings
        {
            get => _teamRankings;
            set => SetProperty(ref _teamRankings, value);
        }

        /// <summary>
        /// Team-Type-Verteilung
        /// </summary>
        public ObservableCollection<TeamTypeDistributionItem> TeamTypeDistribution
        {
            get => _teamTypeDistribution;
            set => SetProperty(ref _teamTypeDistribution, value);
        }

        /// <summary>
        /// Empfehlungen basierend auf Statistiken
        /// </summary>
        public ObservableCollection<RecommendationItem> Recommendations
        {
            get => _recommendations;
            set => SetProperty(ref _recommendations, value);
        }

        /// <summary>
        /// Timeline-Events des Einsatzes
        /// </summary>
        public ObservableCollection<TimelineEvent> TimelineEvents
        {
            get => _timelineEvents;
            set => SetProperty(ref _timelineEvents, value);
        }

        /// <summary>
        /// Text der letzten Aktualisierung
        /// </summary>
        public string LastUpdateText
        {
            get => _lastUpdateText;
            set => SetProperty(ref _lastUpdateText, value);
        }

        /// <summary>
        /// Fenstertitel
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        /// <summary>
        /// Ist gerade eine Aktualisierung aktiv?
        /// </summary>
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        #endregion

        #region Commands

        public ICommand RefreshStatsCommand { get; private set; } = null!;
        public ICommand ExportReportCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            RefreshStatsCommand = new RelayCommand(ExecuteRefreshStats, CanExecuteRefreshStats);
            ExportReportCommand = new RelayCommand(ExecuteExportReport);
        }

        #endregion

        #region Command Implementations

        private bool CanExecuteRefreshStats() => !IsRefreshing;

        private async void ExecuteRefreshStats()
        {
            try
            {
                IsRefreshing = true;
                
                // Simulate brief loading for better UX
                await System.Threading.Tasks.Task.Delay(200);
                
                UpdateStatistics();
                LoadData();
                
                LoggingService.Instance.LogInfo("Statistics refreshed via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error refreshing statistics via MVVM", ex);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Fehler beim Aktualisieren der Statistiken:\n{ex.Message}", 
                        "Aktualisierung fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                IsRefreshing = false;
                UpdateCommandStates();
            }
        }

        private void ExecuteExportReport()
        {
            try
            {
                var report = _statistics.GenerateReport();
                
                var saveDialog = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|Alle Dateien (*.*)|*.*",
                    FileName = $"EinsatzStatistik_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                    Title = "Statistik-Report exportieren"
                };

                var result = Application.Current.Dispatcher.Invoke(() => saveDialog.ShowDialog());
                
                if (result == true)
                {
                    var json = JsonSerializer.Serialize(report, new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });
                    
                    File.WriteAllText(saveDialog.FileName, json);
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Statistik-Report erfolgreich exportiert:\n{saveDialog.FileName}", 
                                      "Export erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                    
                    LoggingService.Instance.LogInfo($"Statistics report exported via MVVM: {saveDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error exporting statistics report via MVVM", ex);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Fehler beim Export:\n{ex.Message}", 
                                  "Export-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Aktualisiert die Statistiken mit aktuellen Team-Daten
        /// </summary>
        public void UpdateStatistics()
        {
            try
            {
                _statistics.EinsatzStart = _einsatzData.EinsatzDatum;
                
                foreach (var team in _teams)
                {
                    _statistics.UpdateTeamStatistic(team);
                }
                
                LoggingService.Instance.LogInfo("Statistics updated with current team data");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating statistics", ex);
            }
        }

        /// <summary>
        /// Lädt alle Daten für die UI
        /// </summary>
        public void LoadData()
        {
            try
            {
                LoadQuickMetrics();
                LoadTeamRankings();
                LoadTeamTypeDistribution();
                LoadRecommendations();
                LoadTimeline();
                
                LastUpdateText = $"Letzte Aktualisierung: {DateTime.Now:HH:mm:ss}";
                
                LoggingService.Instance.LogInfo("All statistics data loaded successfully");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading statistics data", ex);
            }
        }

        #endregion

        #region Private Methods

        private void LoadQuickMetrics()
        {
            try
            {
                var report = _statistics.GenerateReport();
                
                EinsatzdauerValue = FormatTimeSpan(report.EinsatzDauer);
                AnzahlTeamsValue = report.AnzahlTeams.ToString();
                AktiveTeamsValue = report.AktiveTeams.ToString();
                DurchschnittszeitValue = FormatTimeSpan(report.DurchschnittsEinsatzzeit);
                TeamAuslastungProgress = report.TeamAuslastung;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading quick metrics", ex);
            }
        }

        private void LoadTeamRankings()
        {
            try
            {
                TeamRankings.Clear();
                
                var rankings = _teams
                    .Select((team, index) => new TeamRankingItem
                    {
                        Rank = (index + 1).ToString(),
                        TeamName = $"{team.TeamName} ({string.Join(", ", team.MultipleTeamTypes?.SelectedTypes?.Select(t => t.ToString()) ?? new[] { "Unbekannt" })})",
                        Einsatzzeit = FormatTimeSpan(team.ElapsedTime),
                        Status = team.IsRunning ? "Aktiv" : "Bereit",
                        StatusColor = team.IsRunning 
                            ? GetThemeColor("Success") 
                            : GetThemeColor("OnSurfaceVariant")
                    })
                    .OrderByDescending(r => _teams.FirstOrDefault(t => r.TeamName.Contains(t.TeamName))?.ElapsedTime ?? TimeSpan.Zero)
                    .ToList();

                foreach (var ranking in rankings)
                {
                    TeamRankings.Add(ranking);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading team rankings", ex);
            }
        }

        private void LoadTeamTypeDistribution()
        {
            try
            {
                TeamTypeDistribution.Clear();
                
                var allTypes = _teams
                    .SelectMany(t => t.MultipleTeamTypes?.SelectedTypes?.ToList() ?? new List<TeamType>())
                    .Select(type => type.ToString())
                    .ToList();
                    
                var typeGroups = allTypes
                    .GroupBy(type => type)
                    .Select(g => new TeamTypeDistributionItem
                    {
                        TeamType = g.Key,
                        Count = g.Count(),
                        Percentage = allTypes.Count > 0 ? (double)g.Count() / allTypes.Count * 100 : 0,
                        Color = GetTeamTypeColor(g.Key),
                        DetailText = $"{g.Count()} Teams • {(allTypes.Count > 0 ? (double)g.Count() / allTypes.Count * 100 : 0):F1}%"
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                foreach (var typeGroup in typeGroups)
                {
                    TeamTypeDistribution.Add(typeGroup);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading team type distribution", ex);
            }
        }

        private void LoadRecommendations()
        {
            try
            {
                Recommendations.Clear();
                
                var report = _statistics.GenerateReport();
                
                // Empfehlungen aus dem Report
                foreach (var rec in report.Empfehlungen)
                {
                    var priority = GetRecommendationPriority(rec);
                    Recommendations.Add(new RecommendationItem
                    {
                        Title = ExtractTitle(rec),
                        Description = ExtractDescription(rec),
                        Priority = priority
                    });
                }

                // Zusätzliche intelligente Empfehlungen
                if (_teams.Any(t => t.ElapsedTime > TimeSpan.FromMinutes(60)))
                {
                    Recommendations.Add(new RecommendationItem
                    {
                        Title = "Lange Einsatzzeiten erkannt",
                        Description = "Einige Teams sind bereits über 60 Minuten im Einsatz. Erwägen Sie eine Pause oder Rotation.",
                        Priority = GetThemeColor("Warning")
                    });
                }

                if (!_teams.Any(t => t.IsRunning))
                {
                    Recommendations.Add(new RecommendationItem
                    {
                        Title = "Keine aktiven Teams",
                        Description = "Aktuell sind keine Teams aktiv. Starten Sie Teams für den Einsatz.",
                        Priority = GetThemeColor("Error")
                    });
                }

                if (_teams.Count > 0 && _teams.All(t => t.ElapsedTime < TimeSpan.FromMinutes(5)))
                {
                    Recommendations.Add(new RecommendationItem
                    {
                        Title = "Einsatz ist noch jung",
                        Description = "Der Einsatz hat gerade erst begonnen. Überwachen Sie die Team-Performance aufmerksam.",
                        Priority = GetThemeColor("Primary")
                    });
                }

                // Team-Type-spezifische Empfehlungen
                var teamTypes = _teams.SelectMany(t => t.MultipleTeamTypes?.SelectedTypes?.ToList() ?? new List<TeamType>()).Distinct().ToList();
                if (teamTypes.Count > 3)
                {
                    Recommendations.Add(new RecommendationItem
                    {
                        Title = "Vielseitige Team-Zusammensetzung",
                        Description = $"Sie haben {teamTypes.Count} verschiedene Team-Typen im Einsatz. Das erhöht die Erfolgswahrscheinlichkeit erheblich.",
                        Priority = GetThemeColor("Success")
                    });
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading recommendations", ex);
            }
        }

        private void LoadTimeline()
        {
            try
            {
                TimelineEvents.Clear();
                
                // Einsatzstart
                TimelineEvents.Add(new TimelineEvent
                {
                    Time = _einsatzData.EinsatzDatum.ToString("HH:mm"),
                    EventType = "Start",
                    TeamName = "Einsatzleitung",
                    EventColor = GetThemeColor("Success")
                });

                // Team-Events (vereinfacht - in echter Implementierung würden wir Timer-Events tracken)
                foreach (var team in _teams.Where(t => t.IsRunning))
                {
                    TimelineEvents.Add(new TimelineEvent
                    {
                        Time = DateTime.Now.AddMinutes(-team.ElapsedTime.TotalMinutes).ToString("HH:mm"),
                        EventType = "Timer Start",
                        TeamName = team.TeamName,
                        EventColor = GetThemeColor("Primary")
                    });
                }

                // Warnungen als Timeline-Events
                foreach (var team in _teams.Where(t => t.IsFirstWarning || t.IsSecondWarning))
                {
                    if (team.IsSecondWarning)
                    {
                        TimelineEvents.Add(new TimelineEvent
                        {
                            Time = DateTime.Now.AddMinutes(-5).ToString("HH:mm"),
                            EventType = "Warnung 2",
                            TeamName = team.TeamName,
                            EventColor = GetThemeColor("Error")
                        });
                    }
                    else if (team.IsFirstWarning)
                    {
                        TimelineEvents.Add(new TimelineEvent
                        {
                            Time = DateTime.Now.AddMinutes(-10).ToString("HH:mm"),
                            EventType = "Warnung 1",
                            TeamName = team.TeamName,
                            EventColor = GetThemeColor("Warning")
                        });
                    }
                }

                // Sortiere Events nach Zeit
                var sortedEvents = TimelineEvents.OrderBy(e => e.Time).ToList();
                TimelineEvents.Clear();
                foreach (var evt in sortedEvents)
                {
                    TimelineEvents.Add(evt);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading timeline", ex);
            }
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
                "Flächensuchhund" => GetThemeColor("FlächeColor") ?? GetThemeColor("Primary"),
                "Trümmersuchhund" => GetThemeColor("TrümmerColor") ?? GetThemeColor("Secondary"),
                "Mantrailer" => GetThemeColor("MantrailerColor") ?? GetThemeColor("Tertiary"),
                "Wasserortung" => GetThemeColor("WasserColor") ?? GetThemeColor("Success"),
                "Lawinensuchhund" => GetThemeColor("LawineColor") ?? GetThemeColor("Warning"),
                "Allgemein" => GetThemeColor("AllgemeinColor") ?? GetThemeColor("OnSurfaceVariant"),
                _ => GetThemeColor("OnSurfaceVariant")
            };
        }

        private Brush GetRecommendationPriority(string recommendation)
        {
            if (recommendation.Contains("⚠️") || recommendation.Contains("Warnung"))
                return GetThemeColor("Error");
            if (recommendation.Contains("🔄") || recommendation.Contains("⏰"))
                return GetThemeColor("Warning");
            if (recommendation.Contains("🏆"))
                return GetThemeColor("Success");
            
            return GetThemeColor("Primary");
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

        private Brush GetThemeColor(string resourceKey)
        {
            try
            {
                if (Application.Current?.FindResource(resourceKey) is Brush brush)
                {
                    return brush;
                }
            }
            catch
            {
                // Ignore and fall back
            }
            
            // Fallback colors
            return resourceKey switch
            {
                "Primary" => Brushes.DarkOrange,
                "Secondary" => Brushes.Orange,
                "Success" => Brushes.Green,
                "Error" => Brushes.Red,
                "Warning" => Brushes.Orange,
                "OnSurfaceVariant" => Brushes.Gray,
                _ => Brushes.DarkOrange
            };
        }

        private void UpdateCommandStates()
        {
            ((RelayCommand)RefreshStatsCommand).RaiseCanExecuteChanged();
        }

        #endregion

        #region IDisposable Implementation

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Clean up managed resources
                    TeamRankings.Clear();
                    TeamTypeDistribution.Clear();
                    Recommendations.Clear();
                    TimelineEvents.Clear();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    #region Data Models for Binding

    /// <summary>
    /// Datenmodell für Team-Ranking-Items
    /// </summary>
    public class TeamRankingItem
    {
        public string Rank { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string Einsatzzeit { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Brush StatusColor { get; set; } = Brushes.Gray;
    }

    /// <summary>
    /// Datenmodell für Team-Type-Distribution-Items
    /// </summary>
    public class TeamTypeDistributionItem
    {
        public string TeamType { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        public Brush Color { get; set; } = Brushes.Gray;
        public string DetailText { get; set; } = string.Empty;
    }

    /// <summary>
    /// Datenmodell für Recommendation-Items
    /// </summary>
    public class RecommendationItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Brush Priority { get; set; } = Brushes.Gray;
    }

    /// <summary>
    /// Datenmodell für Timeline-Events
    /// </summary>
    public class TimelineEvent
    {
        public string Time { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public Brush EventColor { get; set; } = Brushes.Gray;
    }

    #endregion
}
