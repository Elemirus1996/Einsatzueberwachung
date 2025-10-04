using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Einsatzueberwachung.Models
{
    /// <summary>
    /// Globale Notizen-Einträge für einsatzübergreifende Dokumentation
    /// </summary>
    public class GlobalNotesEntry : INotifyPropertyChanged
    {
        private string _content = string.Empty;
        private DateTime _timestamp = DateTime.Now;
        private GlobalNotesEntryType _entryType = GlobalNotesEntryType.Info;
        private string _teamName = string.Empty;

        public DateTime Timestamp 
        { 
            get => _timestamp; 
            set { _timestamp = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedTimestamp)); }
        }

        public string Content 
        { 
            get => _content; 
            set { _content = value; OnPropertyChanged(); }
        }

        public GlobalNotesEntryType EntryType 
        { 
            get => _entryType; 
            set { _entryType = value; OnPropertyChanged(); OnPropertyChanged(nameof(EntryTypeIcon)); }
        }

        /// <summary>
        /// Optional: Zugeordnetes Team (leer für einsatzweite Notizen)
        /// </summary>
        public string TeamName
        {
            get => _teamName;
            set { _teamName = value; OnPropertyChanged(); }
        }

        public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss");

        public string EntryTypeIcon => EntryType switch
        {
            GlobalNotesEntryType.Info => "ℹ️",
            GlobalNotesEntryType.Warnung => "⚠️",
            GlobalNotesEntryType.Fehler => "❌",
            GlobalNotesEntryType.EinsatzUpdate => "🚨",
            GlobalNotesEntryType.TeamEvent => "👥",
            GlobalNotesEntryType.SystemEvent => "⚙️",
            GlobalNotesEntryType.TimerStart => "▶️",
            GlobalNotesEntryType.TimerStop => "⏸️",
            GlobalNotesEntryType.TimerReset => "🔄",
            GlobalNotesEntryType.Warning1 => "⚠️",
            GlobalNotesEntryType.Warning2 => "🚨",
            GlobalNotesEntryType.Funkspruch => "📻",
            GlobalNotesEntryType.Manual => "✏️",
            _ => "📝"
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum GlobalNotesEntryType
    {
        Info = 0,
        Warnung = 1,
        Fehler = 2,
        EinsatzUpdate = 3,
        TeamEvent = 4,
        SystemEvent = 5,
        Manual = 6,              // Manuelle Notiz
        Funkspruch = 7,          // Funkspruch
        TimerStart = 8,          // Team Timer gestartet
        TimerStop = 9,           // Team Timer gestoppt
        TimerReset = 10,         // Team Timer zurückgesetzt
        Warning1 = 11,           // Erste Warnung
        Warning2 = 12            // Zweite Warnung
    }
}
