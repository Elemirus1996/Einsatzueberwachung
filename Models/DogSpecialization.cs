using System;

namespace Einsatzueberwachung.Models
{
    [Flags]
    public enum DogSpecialization
    {
        None = 0,
        Flaechensuche = 1 << 0,        // 1 - Flächensuchhund
        Truemmersuche = 1 << 1,        // 2 - Trümmersuchhund
        Mantrailing = 1 << 2,          // 4 - Mantrailer
        Wasserortung = 1 << 3,         // 8 - Wasserortung
        Lawinensuche = 1 << 4,         // 16 - Lawinensuchhund
        Gelaendesuche = 1 << 5,        // 32 - Geländesuchhund
        Leichensuche = 1 << 6          // 64 - Leichenspürhund
    }

    public static class DogSpecializationExtensions
    {
        public static string GetDisplayName(this DogSpecialization spec)
        {
            return spec switch
            {
                DogSpecialization.Flaechensuche => "Flächensuchhund",
                DogSpecialization.Truemmersuche => "Trümmersuchhund",
                DogSpecialization.Mantrailing => "Mantrailer",
                DogSpecialization.Wasserortung => "Wasserortung",
                DogSpecialization.Lawinensuche => "Lawinensuchhund",
                DogSpecialization.Gelaendesuche => "Geländesuchhund",
                DogSpecialization.Leichensuche => "Leichenspürhund",
                _ => spec.ToString()
            };
        }

        public static string GetShortName(this DogSpecialization spec)
        {
            return spec switch
            {
                DogSpecialization.Flaechensuche => "FL",
                DogSpecialization.Truemmersuche => "TR",
                DogSpecialization.Mantrailing => "MT",
                DogSpecialization.Wasserortung => "WO",
                DogSpecialization.Lawinensuche => "LA",
                DogSpecialization.Gelaendesuche => "GE",
                DogSpecialization.Leichensuche => "LS",
                _ => ""
            };
        }

        public static string GetColorHex(this DogSpecialization spec)
        {
            // Return color for primary specialization (first one set)
            if (spec.HasFlag(DogSpecialization.Flaechensuche))
                return "#2196F3"; // Blue
            if (spec.HasFlag(DogSpecialization.Truemmersuche))
                return "#FF9800"; // Orange
            if (spec.HasFlag(DogSpecialization.Mantrailing))
                return "#4CAF50"; // Green
            if (spec.HasFlag(DogSpecialization.Wasserortung))
                return "#00BCD4"; // Cyan
            if (spec.HasFlag(DogSpecialization.Lawinensuche))
                return "#9C27B0"; // Purple
            if (spec.HasFlag(DogSpecialization.Gelaendesuche))
                return "#8BC34A"; // Light Green
            if (spec.HasFlag(DogSpecialization.Leichensuche))
                return "#795548"; // Brown
            
            return "#9E9E9E"; // Gray - None
        }
    }
}
