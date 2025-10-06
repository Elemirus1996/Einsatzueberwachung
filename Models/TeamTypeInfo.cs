using System;
using System.Collections.Generic;
using System.Linq;

namespace Einsatzueberwachung.Models
{
    /// <summary>
    /// Statische Informationen über Team-Typen
    /// Stellt Metadaten für alle verfügbaren Team-Typen bereit
    /// </summary>
    public class TeamTypeInfo
    {
        public TeamType Type { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#9E9E9E";

        private static readonly Dictionary<TeamType, TeamTypeInfo> _typeInfos = new Dictionary<TeamType, TeamTypeInfo>
        {
            {
                TeamType.Flaechensuchhund,
                new TeamTypeInfo
                {
                    Type = TeamType.Flaechensuchhund,
                    DisplayName = "Flächensuchhund",
                    ShortName = "FL",
                    Description = "Suche in weitläufigen Gebieten nach vermissten Personen",
                    ColorHex = "#4CAF50" // Grün
                }
            },
            {
                TeamType.Truemmersuchhund,
                new TeamTypeInfo
                {
                    Type = TeamType.Truemmersuchhund,
                    DisplayName = "Trümmersuchhund",
                    ShortName = "TR",
                    Description = "Suche in eingestürzten Gebäuden und Trümmern",
                    ColorHex = "#FF9800" // Orange
                }
            },
            {
                TeamType.Mantrailer,
                new TeamTypeInfo
                {
                    Type = TeamType.Mantrailer,
                    DisplayName = "Mantrailer",
                    ShortName = "MT",
                    Description = "Verfolgung von Individualgeruch über große Distanzen",
                    ColorHex = "#2196F3" // Blau
                }
            },
            {
                TeamType.Wasserrettungshund,
                new TeamTypeInfo
                {
                    Type = TeamType.Wasserrettungshund,
                    DisplayName = "Wasserrettungshund",
                    ShortName = "WR",
                    Description = "Suche und Rettung in und am Wasser",
                    ColorHex = "#00BCD4" // Cyan
                }
            },
            {
                TeamType.Lawinensuchhund,
                new TeamTypeInfo
                {
                    Type = TeamType.Lawinensuchhund,
                    DisplayName = "Lawinensuchhund",
                    ShortName = "LW",
                    Description = "Suche nach Verschütteten in Lawinen",
                    ColorHex = "#607D8B" // Blaugrau statt Lila
                }
            },
            {
                TeamType.Allgemein,
                new TeamTypeInfo
                {
                    Type = TeamType.Allgemein,
                    DisplayName = "Allgemein",
                    ShortName = "AL",
                    Description = "Allgemeine Rettungshunde-Einsätze",
                    ColorHex = "#607D8B" // Blaugrau
                }
            }
        };

        /// <summary>
        /// Gibt die Informationen für einen bestimmten Team-Typ zurück
        /// </summary>
        public static TeamTypeInfo GetTypeInfo(TeamType type)
        {
            return _typeInfos.TryGetValue(type, out var info) ? info : _typeInfos[TeamType.Allgemein];
        }

        /// <summary>
        /// Gibt alle verfügbaren Team-Typ-Informationen zurück
        /// </summary>
        public static IEnumerable<TeamTypeInfo> GetAllTypes()
        {
            return _typeInfos.Values;
        }

        /// <summary>
        /// Gibt alle verfügbaren Team-Typen zurück (nur die Enum-Werte)
        /// </summary>
        public static IEnumerable<TeamType> GetAllTeamTypes()
        {
            return _typeInfos.Keys;
        }

        /// <summary>
        /// Sucht Team-Typ-Informationen nach Display-Name
        /// </summary>
        public static TeamTypeInfo? FindByDisplayName(string displayName)
        {
            return _typeInfos.Values.FirstOrDefault(info => 
                string.Equals(info.DisplayName, displayName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Sucht Team-Typ-Informationen nach Short-Name
        /// </summary>
        public static TeamTypeInfo? FindByShortName(string shortName)
        {
            return _typeInfos.Values.FirstOrDefault(info => 
                string.Equals(info.ShortName, shortName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
