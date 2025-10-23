using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private string _mapAddress = string.Empty;
        private bool _istEinsatz = true;
        private int _anzahlTeams = 1;
        private DateTime _einsatzDatum = DateTime.Now;
        
        // NEU v1.7: PDF-Export Felder
        private string _einsatzNummer = string.Empty;
        private string _staffelName = string.Empty;
        private string _staffelLogoPfad = string.Empty;
        private DateTime? _alarmierungsZeit = null;
        private double _elwLatitude;
        private double _elwLongitude;

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

        public string MapAddress
        {
            get => _mapAddress;
            set { _mapAddress = value; OnPropertyChanged(); }
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

        // NEU v1.7: PDF-Export Properties
        public string EinsatzNummer
        {
            get => _einsatzNummer;
            set { _einsatzNummer = value; OnPropertyChanged(); }
        }

        public string StaffelName
        {
            get => _staffelName;
            set { _staffelName = value; OnPropertyChanged(); }
        }

        public string StaffelLogoPfad
        {
            get => _staffelLogoPfad;
            set { _staffelLogoPfad = value; OnPropertyChanged(); }
        }

        public DateTime? AlarmierungsZeit
        {
            get => _alarmierungsZeit;
            set { _alarmierungsZeit = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// NEU v1.7: Globale Notizen-Collection für die Mobile Website
        /// </summary>
        public ObservableCollection<GlobalNotesEntry> GlobalNotesEntries { get; set; } = new ObservableCollection<GlobalNotesEntry>();
        
        // NEU v2.0.0: Map Integration - Suchgebiete
        public ObservableCollection<SearchArea> SearchAreas { get; set; } = new ObservableCollection<SearchArea>();

        /// <summary>
        /// Optionale ELW-Position (Einsatzleitwagen) für Karten-Orientierung
        /// Format: (Latitude, Longitude)
        /// </summary>
        public (double Latitude, double Longitude)? ElwPosition { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
