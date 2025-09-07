using System;
using System.Collections.Generic;
using System.Linq;

namespace Einsatzueberwachung.Models
{
    public class EinsatzStatistics
    {
        public DateTime EinsatzStart { get; set; }
        public DateTime? EinsatzEnd { get; set; }
        public TimeSpan GesamtEinsatzzeit => EinsatzEnd?.Subtract(EinsatzStart) ?? DateTime.Now.Subtract(EinsatzStart);
        
        public List<TeamStatistic> TeamStatistiken { get; set; } = new();
        public Dictionary<string, TeamTypeStatistic> TeamTypeStatistiken { get; set; } = new();
        
        // Performance Metriken
        public int AnzahlTeams => TeamStatistiken.Count;
        public int AktiveTeams => TeamStatistiken.Count(t => t.IsActive);
        public TimeSpan DurchschnittlicheEinsatzzeit => TeamStatistiken.Any() 
            ? TimeSpan.FromTicks((long)TeamStatistiken.Average(t => t.GesamtEinsatzzeit.Ticks))
            : TimeSpan.Zero;
        
        // Effizienz-Metriken
        public double TeamAuslastung => AnzahlTeams > 0 ? (double)AktiveTeams / AnzahlTeams * 100 : 0;
        public int AnzahlWarnungen => TeamStatistiken.Sum(t => t.AnzahlWarnungen);
        public int AnzahlPausen => TeamStatistiken.Sum(t => t.AnzahlPausen);
        
        public void UpdateTeamStatistic(Team team)
        {
            var existing = TeamStatistiken.FirstOrDefault(ts => ts.TeamId == team.TeamName);
            if (existing == null)
            {
                existing = new TeamStatistic { TeamId = team.TeamName, TeamName = team.TeamName };
                TeamStatistiken.Add(existing);
            }
            
            existing.UpdateFromTeam(team);
            UpdateTeamTypeStatistics(team);
        }
        
        private void UpdateTeamTypeStatistics(Team team)
        {
            if (team.MultipleTeamTypes?.SelectedTypes == null) return;
            
            foreach (var teamType in team.MultipleTeamTypes.SelectedTypes)
            {
                var teamTypeStr = teamType.ToString();
                if (!TeamTypeStatistiken.ContainsKey(teamTypeStr))
                {
                    TeamTypeStatistiken[teamTypeStr] = new TeamTypeStatistic { TeamType = teamTypeStr };
                }
                
                TeamTypeStatistiken[teamTypeStr].UpdateFromTeam(team);
            }
        }
        
        public EinsatzReport GenerateReport()
        {
            return new EinsatzReport
            {
                EinsatzDauer = GesamtEinsatzzeit,
                AnzahlTeams = AnzahlTeams,
                AktiveTeams = AktiveTeams,
                TeamAuslastung = TeamAuslastung,
                DurchschnittsEinsatzzeit = DurchschnittlicheEinsatzzeit,
                TopPerformingTeamType = GetTopPerformingTeamType(),
                Empfehlungen = GenerateRecommendations(),
                DetailStatistiken = TeamStatistiken.ToList(),
                TeamTypeVerteilung = TeamTypeStatistiken.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }
        
        private string GetTopPerformingTeamType()
        {
            if (!TeamTypeStatistiken.Any()) return "Keine Daten";
            
            return TeamTypeStatistiken
                .OrderByDescending(kvp => kvp.Value.Effizienz)
                .First().Key;
        }
        
        private List<string> GenerateRecommendations()
        {
            var recommendations = new List<string>();
            
            if (TeamAuslastung < 50)
                recommendations.Add("🔄 Niedrige Team-Auslastung: Weitere Teams aktivieren");
            
            if (AnzahlWarnungen > AnzahlTeams * 2)
                recommendations.Add("⚠️ Viele Warnungen: Timer-Limits überprüfen");
            
            if (DurchschnittlicheEinsatzzeit > TimeSpan.FromMinutes(90))
                recommendations.Add("⏰ Lange Einsatzzeiten: Team-Rotation einplanen");
            
            var topType = GetTopPerformingTeamType();
            if (topType != "Keine Daten")
                recommendations.Add($"🏆 Top-Performance: {topType} Teams zeigen beste Effizienz");
            
            return recommendations;
        }
    }
    
    public class TeamStatistic
    {
        public string TeamId { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public TimeSpan GesamtEinsatzzeit { get; set; }
        public bool IsActive { get; set; }
        public int AnzahlStarts { get; set; }
        public int AnzahlStops { get; set; }
        public int AnzahlResets { get; set; }
        public int AnzahlWarnungen { get; set; }
        public int AnzahlPausen { get; set; }
        public DateTime? LetzterStart { get; set; }
        public DateTime? LetzterStop { get; set; }
        public List<TimeSpan> EinsatzIntervalle { get; set; } = new();
        
        public double Effizienz => AnzahlStarts > 0 ? (double)AnzahlStops / AnzahlStarts * 100 : 0;
        public TimeSpan DurchschnittlichesIntervall => EinsatzIntervalle.Any() 
            ? TimeSpan.FromTicks((long)EinsatzIntervalle.Average(i => i.Ticks))
            : TimeSpan.Zero;
        
        public void UpdateFromTeam(Team team)
        {
            TeamName = team.TeamName;
            GesamtEinsatzzeit = team.ElapsedTime;
            IsActive = team.IsRunning;
            // Weitere Updates basierend auf Team-Properties
        }
    }
    
    public class TeamTypeStatistic
    {
        public string TeamType { get; set; } = string.Empty;
        public int AnzahlTeams { get; set; }
        public TimeSpan GesamtEinsatzzeit { get; set; }
        public double DurchschnittlicheEinsatzzeit => AnzahlTeams > 0 
            ? GesamtEinsatzzeit.TotalMinutes / AnzahlTeams : 0;
        public double Effizienz { get; set; }
        
        public void UpdateFromTeam(Team team)
        {
            // Update logic für Team-Type spezifische Statistiken
        }
    }
    
    public class EinsatzReport
    {
        public TimeSpan EinsatzDauer { get; set; }
        public int AnzahlTeams { get; set; }
        public int AktiveTeams { get; set; }
        public double TeamAuslastung { get; set; }
        public TimeSpan DurchschnittsEinsatzzeit { get; set; }
        public string TopPerformingTeamType { get; set; } = string.Empty;
        public List<string> Empfehlungen { get; set; } = new();
        public List<TeamStatistic> DetailStatistiken { get; set; } = new();
        public Dictionary<string, TeamTypeStatistic> TeamTypeVerteilung { get; set; } = new();
    }
}
