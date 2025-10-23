using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Einsatzueberwachung.Models
{
    /// <summary>
    /// Globale Notizen-Einträge für einsatzübergreifende Dokumentation mit Reply-System
    /// </summary>
    public class GlobalNotesEntry : INotifyPropertyChanged
    {
        private string _content = string.Empty;
        private DateTime _timestamp = DateTime.Now;
        private GlobalNotesEntryType _entryType = GlobalNotesEntryType.Info;
        private string _teamName = string.Empty;
        private string? _id;
        
        // Reply-System Properties
        private string? _replyToEntryId;
        private GlobalNotesEntry? _replyToEntry;
        private string? _replyPreview;
        private List<GlobalNotesEntry> _replies = new();
        private string? _threadId;
        private int _threadDepth = 0;

        /// <summary>
        /// Eindeutige ID für diese Notiz (für Reply-System)
        /// </summary>
        public string Id
        {
            get => _id ??= Guid.NewGuid().ToString();
            set { _id = value; OnPropertyChanged(); }
        }

        public DateTime Timestamp 
        { 
            get => _timestamp; 
            set { _timestamp = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedTimestamp)); }
        }

        public string Content 
        { 
            get => _content; 
            set { _content = value; OnPropertyChanged(); UpdateReplyPreview(); }
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

        // Reply-System Properties

        /// <summary>
        /// ID der ursprünglichen Nachricht auf die geantwortet wird
        /// </summary>
        public string? ReplyToEntryId
        {
            get => _replyToEntryId;
            set { _replyToEntryId = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsReply)); OnPropertyChanged(nameof(IsThreadRoot)); }
        }

        /// <summary>
        /// Referenz auf die ursprüngliche Nachricht
        /// </summary>
        public GlobalNotesEntry? ReplyToEntry
        {
            get => _replyToEntry;
            set { _replyToEntry = value; OnPropertyChanged(); UpdateReplyPreview(); }
        }

        /// <summary>
        /// Kurze Vorschau der ursprünglichen Nachricht (max 50 Zeichen)
        /// </summary>
        public string? ReplyPreview
        {
            get => _replyPreview;
            private set { _replyPreview = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Liste aller Antworten auf diese Nachricht
        /// </summary>
        public List<GlobalNotesEntry> Replies
        {
            get => _replies;
            set { _replies = value; OnPropertyChanged(); OnPropertyChanged(nameof(RepliesCount)); OnPropertyChanged(nameof(HasReplies)); }
        }

        /// <summary>
        /// Thread-ID für zusammengehörige Nachrichten
        /// </summary>
        public string? ThreadId
        {
            get => _threadId;
            set { _threadId = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Verschachtelungstiefe in der Thread-Hierarchie (max 3)
        /// </summary>
        public int ThreadDepth
        {
            get => _threadDepth;
            set { _threadDepth = Math.Min(value, 3); OnPropertyChanged(); }
        }

        // Computed Properties für UI

        /// <summary>
        /// Ist dies eine Antwort auf eine andere Nachricht?
        /// </summary>
        public bool IsReply => !string.IsNullOrEmpty(ReplyToEntryId);

        /// <summary>
        /// Ist dies der Anfang eines Threads (keine Antwort)?
        /// </summary>
        public bool IsThreadRoot => string.IsNullOrEmpty(ReplyToEntryId);

        /// <summary>
        /// Anzahl der Antworten auf diese Nachricht
        /// </summary>
        public int RepliesCount => Replies?.Count ?? 0;

        /// <summary>
        /// Hat diese Nachricht Antworten?
        /// </summary>
        public bool HasReplies => RepliesCount > 0;

        /// <summary>
        /// Formatierte Anzeige für Replies-Counter
        /// </summary>
        public string RepliesCountText => RepliesCount switch
        {
            0 => "",
            1 => "1 Antwort",
            _ => $"{RepliesCount} Antworten"
        };

        /// <summary>
        /// CSS-ähnliche Margin für Thread-Einrückung
        /// </summary>
        public double ThreadMarginLeft => ThreadDepth * 30.0; // 30px pro Ebene

        /// <summary>
        /// Icon für Reply-Visualisierung
        /// </summary>
        public string ReplyIcon => IsReply ? "↳" : "";

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

        /// <summary>
        /// Erstellt automatisch eine Vorschau der ursprünglichen Nachricht
        /// </summary>
        private void UpdateReplyPreview()
        {
            if (ReplyToEntry != null)
            {
                var content = ReplyToEntry.Content ?? "";
                ReplyPreview = content.Length > 50 
                    ? content.Substring(0, 47) + "..." 
                    : content;
            }
            else if (!string.IsNullOrEmpty(ReplyToEntryId) && !string.IsNullOrEmpty(Content))
            {
                // Fallback: Versuche Vorschau aus eigenem Content zu extrahieren
                var lines = Content.Split('\n');
                if (lines.Length > 1 && lines[0].StartsWith("@"))
                {
                    ReplyPreview = lines[0].Length > 50 
                        ? lines[0].Substring(0, 47) + "..." 
                        : lines[0];
                }
            }
        }

        /// <summary>
        /// Fügt eine Antwort zu dieser Nachricht hinzu
        /// </summary>
        public void AddReply(GlobalNotesEntry reply)
        {
            if (reply == null) return;
            
            reply.ReplyToEntryId = this.Id;
            reply.ReplyToEntry = this;
            reply.ThreadId = this.ThreadId ?? this.Id; // Thread-ID verwenden oder neue erstellen
            reply.ThreadDepth = Math.Min(this.ThreadDepth + 1, 3); // Max 3 Ebenen
            
            Replies.Add(reply);
            OnPropertyChanged(nameof(Replies));
            OnPropertyChanged(nameof(RepliesCount));
            OnPropertyChanged(nameof(HasReplies));
            OnPropertyChanged(nameof(RepliesCountText));
        }

        /// <summary>
        /// Entfernt eine Antwort von dieser Nachricht
        /// </summary>
        public bool RemoveReply(GlobalNotesEntry reply)
        {
            if (reply == null) return false;
            
            var removed = Replies.Remove(reply);
            if (removed)
            {
                OnPropertyChanged(nameof(Replies));
                OnPropertyChanged(nameof(RepliesCount));
                OnPropertyChanged(nameof(HasReplies));
                OnPropertyChanged(nameof(RepliesCountText));
            }
            return removed;
        }

        /// <summary>
        /// Erstellt eine neue Antwort auf diese Nachricht
        /// </summary>
        public GlobalNotesEntry CreateReply(string content, string? teamName = null, GlobalNotesEntryType entryType = GlobalNotesEntryType.Manual)
        {
            return new GlobalNotesEntry
            {
                Content = content,
                TeamName = teamName ?? string.Empty,
                EntryType = entryType,
                ReplyToEntryId = this.Id,
                ReplyToEntry = this,
                ThreadId = this.ThreadId ?? this.Id,
                ThreadDepth = Math.Min(this.ThreadDepth + 1, 3),
                Timestamp = DateTime.Now
            };
        }

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
