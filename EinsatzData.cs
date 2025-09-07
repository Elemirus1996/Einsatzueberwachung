using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Einsatzueberwachung.Models
{
    public class EinsatzData : INotifyPropertyChanged
    {
        private string _einsatzleiter = string.Empty;
        private string _fuehrungsassistent = string.Empty;
        private string _alarmiert = string.Empty;
        private string _einsatzort = string.Empty;
        private string _exportPfad = string.Empty;
        private bool _istEinsatz = true;
        private int _anzahlTeams = 1;
        private DateTime _einsatzDatum = DateTime.Now;

        public string Einsatzleiter
        {
            get => _einsatzleiter;
            set { _einsatzleiter = value; OnPropertyChanged(); }
        }

        public string Fuehrungsassistent
        {
            get => _fuehrungsassistent;
            set { _fuehrungsassistent = value; OnPropertyChanged(); }
        }

        public string Alarmiert
        {
            get => _alarmiert;
            set { _alarmiert = value; OnPropertyChanged(); }
        }

        public string Einsatzort
        {
            get => _einsatzort;
            set { _einsatzort = value; OnPropertyChanged(); }
        }

        public string ExportPfad
        {
            get => _exportPfad;
            set { _exportPfad = value; OnPropertyChanged(); }
        }

        public bool IstEinsatz
        {
            get => _istEinsatz;
            set { _istEinsatz = value; OnPropertyChanged(); OnPropertyChanged(nameof(EinsatzTyp)); }
        }

        public string EinsatzTyp => IstEinsatz ? "Einsatz" : "Übung";

        public int AnzahlTeams
        {
            get => _anzahlTeams;
            set { _anzahlTeams = Math.Max(1, value); OnPropertyChanged(); }
        }

        public DateTime EinsatzDatum
        {
            get => _einsatzDatum;
            set { _einsatzDatum = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}