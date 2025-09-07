using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Einsatzueberwachung.Models
{
    public class NotesEntry : INotifyPropertyChanged
    {
        private string _content = string.Empty;
        private DateTime _timestamp;
        private NotesEntryType _entryType = NotesEntryType.Manual;

        public DateTime Timestamp 
        { 
            get => _timestamp; 
            set { _timestamp = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedTimestamp)); }
        }

        public string Content 
        { 
            get => _content; 
            set { _content = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedEntry)); }
        }

        public NotesEntryType EntryType 
        { 
            get => _entryType; 
            set { _entryType = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedEntry)); }
        }

        public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss");

        public string FormattedEntry => EntryType switch
        {
            NotesEntryType.TimerStart => $"[{FormattedTimestamp}] ? Timer gestartet",
            NotesEntryType.TimerStop => $"[{FormattedTimestamp}] ?? Timer gestoppt",
            NotesEntryType.TimerReset => $"[{FormattedTimestamp}] ?? Timer zurückgesetzt",
            NotesEntryType.Warning1 => $"[{FormattedTimestamp}] ?? Erste Warnung erreicht",
            NotesEntryType.Warning2 => $"[{FormattedTimestamp}] ?? KRITISCHE Warnung!",
            NotesEntryType.Manual => $"[{FormattedTimestamp}] {Content}",
            _ => $"[{FormattedTimestamp}] {Content}"
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum NotesEntryType
    {
        Manual,
        TimerStart,
        TimerStop,
        TimerReset,
        Warning1,
        Warning2
    }
}