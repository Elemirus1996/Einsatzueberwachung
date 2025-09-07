using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Einsatzueberwachung.Models
{
    public class EinsatzTemplate : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _description = string.Empty;
        private int _standardTeamCount = 1;
        private int _firstWarningMinutes = 10;
        private int _secondWarningMinutes = 20;
        private bool _isDefault = false;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public int StandardTeamCount
        {
            get => _standardTeamCount;
            set { _standardTeamCount = Math.Max(1, Math.Min(10, value)); OnPropertyChanged(); }
        }

        public int FirstWarningMinutes
        {
            get => _firstWarningMinutes;
            set { _firstWarningMinutes = Math.Max(1, value); OnPropertyChanged(); }
        }

        public int SecondWarningMinutes
        {
            get => _secondWarningMinutes;
            set { _secondWarningMinutes = Math.Max(_firstWarningMinutes + 1, value); OnPropertyChanged(); }
        }

        public bool IsDefault
        {
            get => _isDefault;
            set { _isDefault = value; OnPropertyChanged(); }
        }

        public ObservableCollection<TeamTemplate> TeamTemplates { get; } = new ObservableCollection<TeamTemplate>();

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastUsed { get; set; } = DateTime.Now;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TeamTemplate : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private TeamType _teamType = TeamType.Allgemein;
        private string _hundefuehrer = string.Empty;
        private string _helfer = string.Empty;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public TeamType TeamType
        {
            get => _teamType;
            set { _teamType = value; OnPropertyChanged(); }
        }

        public string Hundefuehrer
        {
            get => _hundefuehrer;
            set { _hundefuehrer = value; OnPropertyChanged(); }
        }

        public string Helfer
        {
            get => _helfer;
            set { _helfer = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}