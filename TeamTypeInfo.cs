using System;

namespace Einsatzueberwachung
{
    public enum TeamType
    {
        Flaechensuchhund,
        Truemmersuchhund,
        Mantrailer,
        Wasserrettungshund,
        Lawinensuchhund,
        Allgemein
    }

    public class TeamTypeInfo
    {
        public TeamType Type { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#2196F3";
        public string Description { get; set; } = string.Empty;

        public static TeamTypeInfo[] GetAllTypes()
        {
            return new[]
            {
                new TeamTypeInfo 
                { 
                    Type = TeamType.Flaechensuchhund, 
                    DisplayName = "Flächensuchhund", 
                    ShortName = "FL",
                    ColorHex = "#4CAF50",
                    Description = "Suche in großen Flächen"
                },
                new TeamTypeInfo 
                { 
                    Type = TeamType.Truemmersuchhund, 
                    DisplayName = "Trümmersuchhund", 
                    ShortName = "TR",
                    ColorHex = "#FF5722",
                    Description = "Suche in Trümmern und Gebäuden"
                },
                new TeamTypeInfo 
                { 
                    Type = TeamType.Mantrailer, 
                    DisplayName = "Mantrailer", 
                    ShortName = "MT",
                    ColorHex = "#9C27B0",
                    Description = "Personenverfolgung über Geruchsspuren"
                },
                new TeamTypeInfo 
                { 
                    Type = TeamType.Wasserrettungshund, 
                    DisplayName = "Wasserrettungshund", 
                    ShortName = "WR",
                    ColorHex = "#2196F3",
                    Description = "Rettung aus und Suche im Wasser"
                },
                new TeamTypeInfo 
                { 
                    Type = TeamType.Lawinensuchhund, 
                    DisplayName = "Lawinensuchhund", 
                    ShortName = "LW",
                    ColorHex = "#607D8B",
                    Description = "Suche von Verschütteten in Lawinen"
                },
                new TeamTypeInfo 
                { 
                    Type = TeamType.Allgemein, 
                    DisplayName = "Allgemein", 
                    ShortName = "AL",
                    ColorHex = "#795548",
                    Description = "Allgemeine Rettungshundeteams"
                }
            };
        }

        public static TeamTypeInfo GetTypeInfo(TeamType type)
        {
            var types = GetAllTypes();
            return Array.Find(types, t => t.Type == type) ?? types[^1]; // Default to Allgemein
        }
    }
}
