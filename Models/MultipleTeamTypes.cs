using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Einsatzueberwachung.Models
{
    /// <summary>
    /// Repräsentiert eine Sammlung von Team-Typen für einen Hund
    /// Ermöglicht Multiple-Selection (z.B. Fläche + Trümmer + Mantrailer)
    /// </summary>
    public class MultipleTeamTypes : INotifyPropertyChanged
    {
        private HashSet<TeamType> _selectedTypes = new HashSet<TeamType>();

        public HashSet<TeamType> SelectedTypes
        {
            get => _selectedTypes;
            set
            {
                _selectedTypes = value ?? new HashSet<TeamType>();
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(ColorHex));
                OnPropertyChanged(nameof(ShortName));
                OnPropertyChanged(nameof(Description));
            }
        }

        public string DisplayName
        {
            get
            {
                if (!SelectedTypes.Any())
                    return "Kein Typ ausgewählt";

                if (SelectedTypes.Count == 1)
                    return TeamTypeInfo.GetTypeInfo(SelectedTypes.First()).DisplayName;

                var names = SelectedTypes.Select(t => TeamTypeInfo.GetTypeInfo(t).ShortName);
                return string.Join(" + ", names.OrderBy(n => n));
            }
        }

        public string ShortName
        {
            get
            {
                if (!SelectedTypes.Any())
                    return "N/A";

                if (SelectedTypes.Count == 1)
                    return TeamTypeInfo.GetTypeInfo(SelectedTypes.First()).ShortName;

                var shortNames = SelectedTypes.Select(t => TeamTypeInfo.GetTypeInfo(t).ShortName);
                return string.Join("+", shortNames.OrderBy(n => n));
            }
        }

        public string ColorHex
        {
            get
            {
                if (!SelectedTypes.Any())
                    return "#9E9E9E"; // Gray for no selection

                if (SelectedTypes.Count == 1)
                    return TeamTypeInfo.GetTypeInfo(SelectedTypes.First()).ColorHex;

                // For multiple types, use a gradient-like approach or return primary type color
                var primaryType = SelectedTypes.OrderBy(t => (int)t).First();
                return TeamTypeInfo.GetTypeInfo(primaryType).ColorHex;
            }
        }

        public string Description
        {
            get
            {
                if (!SelectedTypes.Any())
                    return "Kein Spezialisierungstyp ausgewählt";

                if (SelectedTypes.Count == 1)
                    return TeamTypeInfo.GetTypeInfo(SelectedTypes.First()).Description;

                var descriptions = SelectedTypes.Select(t => TeamTypeInfo.GetTypeInfo(t).ShortName);
                return $"Mehrfach-Spezialisierung: {string.Join(", ", descriptions.OrderBy(d => d))}";
            }
        }

        public MultipleTeamTypes()
        {
            // NICHT mehr automatisch Allgemein setzen!
            // Lasse SelectedTypes leer - das System soll nur spezifische Spezialisierungen verwenden
        }

        public MultipleTeamTypes(TeamType singleType)
        {
            SelectedTypes.Add(singleType);
        }

        public MultipleTeamTypes(IEnumerable<TeamType> types)
        {
            SelectedTypes = new HashSet<TeamType>(types);
            // ENTFERNT: Automatisches Hinzufügen von Allgemein wenn leer
            // Wenn keine Types gewählt wurden, bleibt es leer
        }

        public bool HasType(TeamType type)
        {
            return SelectedTypes.Contains(type);
        }

        public void AddType(TeamType type)
        {
            // Entferne Allgemein nur wenn wir einen anderen spezifischen Typ hinzufügen
            // UND falls Allgemein bereits vorhanden ist
            if (type != TeamType.Allgemein && SelectedTypes.Contains(TeamType.Allgemein))
            {
                SelectedTypes.Remove(TeamType.Allgemein);
            }

            SelectedTypes.Add(type);
            OnPropertyChanged(nameof(SelectedTypes));
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(ColorHex));
            OnPropertyChanged(nameof(ShortName));
            OnPropertyChanged(nameof(Description));
        }

        public void RemoveType(TeamType type)
        {
            SelectedTypes.Remove(type);

            // ENTFERNT: Automatisches Hinzufügen von Allgemein wenn leer
            // Lasse die SelectedTypes leer - das ist jetzt der gewünschte Zustand
            // if (!SelectedTypes.Any())
            // {
            //     SelectedTypes.Add(TeamType.Allgemein);
            // }

            OnPropertyChanged(nameof(SelectedTypes));
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(ColorHex));
            OnPropertyChanged(nameof(ShortName));
            OnPropertyChanged(nameof(Description));
        }

        public void ToggleType(TeamType type)
        {
            if (HasType(type))
            {
                RemoveType(type);
            }
            else
            {
                AddType(type);
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
