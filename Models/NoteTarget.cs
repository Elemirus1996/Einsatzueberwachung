using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Einsatzueberwachung.Models
{
    /// <summary>
    /// Repräsentiert ein Ziel für eine Notiz (Team, Einsatzleiter, Drohnenstaffel)
    /// </summary>
    public class NoteTarget : INotifyPropertyChanged
    {
        private int _teamId;
        private string _displayName = string.Empty;
        private string _detailInfo = string.Empty;
        private bool _isSpecialTarget = false;
        
        /// <summary>
        /// Team-ID (0 für spezielle Targets wie Einsatzleiter/Drohnenstaffel)
        /// </summary>
        public int TeamId
        {
            get => _teamId;
            set { _teamId = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Anzeigename (z.B. "Team Rex", "Einsatzleiter", "Drohnenstaffel")
        /// </summary>
        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Detail-Info (z.B. Hundename bei Teams, leer bei speziellen Targets)
        /// </summary>
        public string DetailInfo
        {
            get => _detailInfo;
            set { _detailInfo = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Ist dies ein spezielles Target (kein normales Team)?
        /// </summary>
        public bool IsSpecialTarget
        {
            get => _isSpecialTarget;
            set { _isSpecialTarget = value; OnPropertyChanged(); }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
