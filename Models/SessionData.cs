using System;
using System.Collections.Generic;

namespace Einsatzueberwachung.Models
{
    /// <summary>
    /// Enthält alle Daten einer Einsatz-Session für Auto-Save und Crash-Recovery
    /// </summary>
    public class EinsatzSessionData
    {
        public EinsatzData? EinsatzData { get; set; }
        public List<TeamSessionData> Teams { get; set; } = new List<TeamSessionData>();
        public int FirstWarningMinutes { get; set; } = 10;
        public int SecondWarningMinutes { get; set; } = 20;
        public int NextTeamId { get; set; } = 1;
        public DateTime LastSaved { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Enthält die Session-Daten eines einzelnen Teams
    /// </summary>
    public class TeamSessionData
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string HundName { get; set; } = string.Empty;
        public string Hundefuehrer { get; set; } = string.Empty;
        public string Helfer { get; set; } = string.Empty;
        public string Suchgebiet { get; set; } = string.Empty;
        public string TeamType { get; set; } = "Allgemein";
        public TimeSpan ElapsedTime { get; set; }
        public bool IsRunning { get; set; }
        public bool IsFirstWarning { get; set; }
        public bool IsSecondWarning { get; set; }
        public int FirstWarningMinutes { get; set; } = 10;
        public int SecondWarningMinutes { get; set; } = 20;
        public DateTime? StartTime { get; set; }
    }
}
