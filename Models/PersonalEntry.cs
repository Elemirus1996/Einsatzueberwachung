using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Einsatzueberwachung.Models
{
    public class PersonalEntry : INotifyPropertyChanged
    {
        private string _id;
        private string _vorname;
        private string _nachname;
        private PersonalSkills _skills;
        private string _notizen;
        private bool _isActive;

        public PersonalEntry()
        {
            _id = Guid.NewGuid().ToString();
            _vorname = string.Empty;
            _nachname = string.Empty;
            _skills = PersonalSkills.None;
            _notizen = string.Empty;
            _isActive = true;
        }

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Vorname
        {
            get => _vorname;
            set { _vorname = value; OnPropertyChanged(); OnPropertyChanged(nameof(FullName)); }
        }

        public string Nachname
        {
            get => _nachname;
            set { _nachname = value; OnPropertyChanged(); OnPropertyChanged(nameof(FullName)); }
        }

        public string FullName => $"{Vorname} {Nachname}".Trim();

        public PersonalSkills Skills
        {
            get => _skills;
            set { _skills = value; OnPropertyChanged(); OnPropertyChanged(nameof(SkillsDisplay)); }
        }

        public string SkillsDisplay
        {
            get
            {
                if (Skills == PersonalSkills.None)
                    return "Keine Fähigkeiten";

                var skillList = new List<string>();
                foreach (PersonalSkills skill in Enum.GetValues(typeof(PersonalSkills)))
                {
                    if (skill != PersonalSkills.None && Skills.HasFlag(skill))
                    {
                        skillList.Add(skill.GetDisplayName());
                    }
                }
                return string.Join(", ", skillList);
            }
        }

        public string SkillsShortDisplay
        {
            get
            {
                if (Skills == PersonalSkills.None)
                    return "-";

                var skillList = new List<string>();
                foreach (PersonalSkills skill in Enum.GetValues(typeof(PersonalSkills)))
                {
                    if (skill != PersonalSkills.None && Skills.HasFlag(skill))
                    {
                        skillList.Add(skill.GetShortName());
                    }
                }
                return string.Join(", ", skillList);
            }
        }

        public string Notizen
        {
            get => _notizen;
            set { _notizen = value; OnPropertyChanged(); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
