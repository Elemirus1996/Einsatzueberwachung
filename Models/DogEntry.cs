using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Einsatzueberwachung.Models
{
    public class DogEntry : INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private string _rasse;
        private DogSpecialization _specializations;
        private string _hundefuehrerId; // Reference to PersonalEntry
        private string _notizen;
        private bool _isActive;
        private int _alter;

        public DogEntry()
        {
            _id = Guid.NewGuid().ToString();
            _name = string.Empty;
            _rasse = string.Empty;
            _specializations = DogSpecialization.None;
            _hundefuehrerId = string.Empty;
            _notizen = string.Empty;
            _isActive = true;
            _alter = 0;
        }

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Rasse
        {
            get => _rasse;
            set { _rasse = value; OnPropertyChanged(); }
        }

        public int Alter
        {
            get => _alter;
            set { _alter = value; OnPropertyChanged(); }
        }

        public DogSpecialization Specializations
        {
            get => _specializations;
            set 
            { 
                _specializations = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(SpecializationsDisplay));
                OnPropertyChanged(nameof(PrimarySpecializationColor));
            }
        }

        public string SpecializationsDisplay
        {
            get
            {
                if (Specializations == DogSpecialization.None)
                    return "Keine Spezialisierung";

                var specList = new List<string>();
                foreach (DogSpecialization spec in Enum.GetValues(typeof(DogSpecialization)))
                {
                    if (spec != DogSpecialization.None && Specializations.HasFlag(spec))
                    {
                        specList.Add(spec.GetDisplayName());
                    }
                }
                return string.Join(", ", specList);
            }
        }

        public string SpecializationsShortDisplay
        {
            get
            {
                if (Specializations == DogSpecialization.None)
                    return "-";

                var specList = new List<string>();
                foreach (DogSpecialization spec in Enum.GetValues(typeof(DogSpecialization)))
                {
                    if (spec != DogSpecialization.None && Specializations.HasFlag(spec))
                    {
                        specList.Add(spec.GetShortName());
                    }
                }
                return string.Join(", ", specList);
            }
        }

        public string PrimarySpecializationColor => Specializations.GetColorHex();

        public string HundefuehrerId
        {
            get => _hundefuehrerId;
            set { _hundefuehrerId = value; OnPropertyChanged(); }
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
