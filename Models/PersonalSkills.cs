using System;

namespace Einsatzueberwachung.Models
{
    [Flags]
    public enum PersonalSkills
    {
        None = 0,
        Hundefuehrer = 1 << 0,      // 1
        Helfer = 1 << 1,             // 2
        Fuehrungsassistent = 1 << 2, // 4
        Gruppenfuehrer = 1 << 3,     // 8
        Zugfuehrer = 1 << 4,         // 16
        Verbandsfuehrer = 1 << 5,    // 32
        Drohnenpilot = 1 << 6        // 64
    }

    public static class PersonalSkillsExtensions
    {
        public static string GetDisplayName(this PersonalSkills skill)
        {
            return skill switch
            {
                PersonalSkills.Hundefuehrer => "Hundeführer",
                PersonalSkills.Helfer => "Helfer",
                PersonalSkills.Fuehrungsassistent => "Führungsassistent",
                PersonalSkills.Gruppenfuehrer => "Gruppenführer",
                PersonalSkills.Zugfuehrer => "Zugführer",
                PersonalSkills.Verbandsfuehrer => "Verbandsführer",
                PersonalSkills.Drohnenpilot => "Drohnenpilot",
                _ => skill.ToString()
            };
        }

        public static string GetShortName(this PersonalSkills skill)
        {
            return skill switch
            {
                PersonalSkills.Hundefuehrer => "HF",
                PersonalSkills.Helfer => "H",
                PersonalSkills.Fuehrungsassistent => "FA",
                PersonalSkills.Gruppenfuehrer => "GF",
                PersonalSkills.Zugfuehrer => "ZF",
                PersonalSkills.Verbandsfuehrer => "VF",
                PersonalSkills.Drohnenpilot => "DP",
                _ => ""
            };
        }
    }
}
