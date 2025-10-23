using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Einsatzueberwachung.Models;

namespace Einsatzueberwachung.Services
{
    public class GlobalNotesService
    {
        private static GlobalNotesService? _instance;
        public static GlobalNotesService Instance => _instance ??= new GlobalNotesService();

        private ObservableCollection<GlobalNotesEntry>? _notesCollection;
        private Action<string>? _addInfoNote;
        private Action<string>? _addWarningNote;
        private Action<string>? _addErrorNote;
        
        // Thread-Management Dictionary für schnelle Lookups
        private readonly Dictionary<string, GlobalNotesEntry> _notesById = new();
        private readonly Dictionary<string, List<GlobalNotesEntry>> _threadsByRootId = new();

        private GlobalNotesService() { }

        public void Initialize(ObservableCollection<GlobalNotesEntry> notesCollection,
            Action<string> addInfoNote, Action<string> addWarningNote, Action<string> addErrorNote)
        {
            _notesCollection = notesCollection;
            _addInfoNote = addInfoNote;
            _addWarningNote = addWarningNote;
            _addErrorNote = addErrorNote;
            
            // Index bestehende Notizen für Thread-Management
            RebuildIndex();
        }

        /// <summary>
        /// Baut den Index für schnelle Thread-Lookups neu auf
        /// </summary>
        public void RebuildIndex()
        {
            _notesById.Clear();
            _threadsByRootId.Clear();
            
            if (_notesCollection == null) return;
            
            // Indexiere alle Notizen nach ID
            foreach (var note in _notesCollection)
            {
                if (!string.IsNullOrEmpty(note.Id))
                {
                    _notesById[note.Id] = note;
                }
            }
            
            // Organisiere Threads
            foreach (var note in _notesCollection)
            {
                if (note.IsThreadRoot && !string.IsNullOrEmpty(note.Id))
                {
                    _threadsByRootId[note.Id] = GetThreadMessages(note.Id);
                }
            }
        }

        // Bestehende Methoden
        public void AddInfo(string message)
        {
            _addInfoNote?.Invoke(message);
        }

        public void AddWarning(string message)
        {
            _addWarningNote?.Invoke(message);
        }

        public void AddError(string message)
        {
            _addErrorNote?.Invoke(message);
        }

        // Neue Reply-System Methoden

        /// <summary>
        /// Fügt eine neue Notiz hinzu und indexiert sie
        /// </summary>
        public void AddNote(GlobalNotesEntry note)
        {
            if (note == null || _notesCollection == null) return;
            
            // Stelle sicher, dass die Notiz eine ID hat
            if (string.IsNullOrEmpty(note.Id))
            {
                note.Id = Guid.NewGuid().ToString();
            }
            
            // Füge zur Collection hinzu
            _notesCollection.Add(note);
            
            // Indexiere
            _notesById[note.Id] = note;
            
            // Falls es eine Antwort ist, verknüpfe mit Parent
            if (note.IsReply && !string.IsNullOrEmpty(note.ReplyToEntryId))
            {
                LinkReplyToParent(note);
            }
        }

        /// <summary>
        /// Erstellt eine Antwort auf eine bestehende Notiz
        /// </summary>
        public GlobalNotesEntry? CreateReply(string parentId, string content, string? teamName = null, 
            GlobalNotesEntryType entryType = GlobalNotesEntryType.Manual)
        {
            if (string.IsNullOrEmpty(parentId) || string.IsNullOrEmpty(content)) return null;
            
            var parentNote = GetNoteById(parentId);
            if (parentNote == null) return null;
            
            var reply = parentNote.CreateReply(content, teamName, entryType);
            AddNote(reply);
            
            return reply;
        }

        /// <summary>
        /// Findet eine Notiz anhand ihrer ID
        /// </summary>
        public GlobalNotesEntry? GetNoteById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _notesById.TryGetValue(id, out var note) ? note : null;
        }

        /// <summary>
        /// Holt alle Nachrichten eines Threads (inkl. Root)
        /// </summary>
        public List<GlobalNotesEntry> GetThreadMessages(string rootId)
        {
            var messages = new List<GlobalNotesEntry>();
            
            var rootNote = GetNoteById(rootId);
            if (rootNote == null) return messages;
            
            messages.Add(rootNote);
            CollectRepliesRecursive(rootNote, messages);
            
            return messages.OrderBy(m => m.Timestamp).ToList();
        }

        /// <summary>
        /// Sammelt alle Antworten rekursiv
        /// </summary>
        private void CollectRepliesRecursive(GlobalNotesEntry note, List<GlobalNotesEntry> collector)
        {
            if (note.Replies == null) return;
            
            foreach (var reply in note.Replies.OrderBy(r => r.Timestamp))
            {
                collector.Add(reply);
                CollectRepliesRecursive(reply, collector);
            }
        }

        /// <summary>
        /// Verknüpft eine Antwort mit ihrer Parent-Nachricht
        /// </summary>
        private void LinkReplyToParent(GlobalNotesEntry reply)
        {
            if (string.IsNullOrEmpty(reply.ReplyToEntryId)) return;
            
            var parent = GetNoteById(reply.ReplyToEntryId);
            if (parent != null)
            {
                reply.ReplyToEntry = parent;
                parent.AddReply(reply);
                
                // Thread-ID setzen falls nicht vorhanden
                if (string.IsNullOrEmpty(reply.ThreadId))
                {
                    reply.ThreadId = parent.ThreadId ?? parent.Id;
                }
            }
        }

        /// <summary>
        /// Holt alle Thread-Root-Nachrichten (Nachrichten ohne Parent)
        /// </summary>
        public List<GlobalNotesEntry> GetThreadRoots()
        {
            if (_notesCollection == null) return new List<GlobalNotesEntry>();
            
            return _notesCollection
                .Where(n => n.IsThreadRoot)
                .OrderBy(n => n.Timestamp)
                .ToList();
        }

        /// <summary>
        /// Holt alle Antworten zu einer bestimmten Nachricht (nur direkte Kinder)
        /// </summary>
        public List<GlobalNotesEntry> GetDirectReplies(string parentId)
        {
            if (string.IsNullOrEmpty(parentId) || _notesCollection == null) return new List<GlobalNotesEntry>();
            
            return _notesCollection
                .Where(n => n.ReplyToEntryId == parentId)
                .OrderBy(n => n.Timestamp)
                .ToList();
        }

        /// <summary>
        /// Zählt die Gesamtanzahl der Antworten in einem Thread
        /// </summary>
        public int GetThreadTotalReplies(string rootId)
        {
            return GetThreadMessages(rootId).Count - 1; // -1 für Root-Nachricht
        }

        /// <summary>
        /// Findet die neueste Aktivität in einem Thread
        /// </summary>
        public DateTime GetThreadLastActivity(string rootId)
        {
            var messages = GetThreadMessages(rootId);
            return messages.Count > 0 ? messages.Max(m => m.Timestamp) : DateTime.MinValue;
        }

        /// <summary>
        /// Erstellt einen "Smart Reply" basierend auf dem Kontext
        /// </summary>
        public string GenerateSmartReply(GlobalNotesEntry originalNote, string replyType = "acknowledge")
        {
            if (originalNote == null) return string.Empty;
            
            var teamPrefix = !string.IsNullOrEmpty(originalNote.TeamName) ? $"@{originalNote.TeamName} " : "";
            
            return replyType.ToLower() switch
            {
                "acknowledge" => $"{teamPrefix}✅ Verstanden",
                "question" => $"{teamPrefix}❓ Kannst du das präzisieren?",
                "completed" => $"{teamPrefix}✅ Erledigt",
                "help" => $"{teamPrefix}🆘 Benötige Unterstützung",
                "update" => $"{teamPrefix}📍 Status-Update folgt",
                _ => $"{teamPrefix}📝 "
            };
        }

        /// <summary>
        /// Sucht nach Notizen mit bestimmtem Inhalt
        /// </summary>
        public List<GlobalNotesEntry> SearchNotes(string searchTerm, bool includeReplies = true)
        {
            if (string.IsNullOrEmpty(searchTerm) || _notesCollection == null) 
                return new List<GlobalNotesEntry>();
            
            var query = _notesCollection.Where(n => 
                n.Content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(n.TeamName) && n.TeamName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
            
            if (!includeReplies)
            {
                query = query.Where(n => n.IsThreadRoot);
            }
            
            return query.OrderByDescending(n => n.Timestamp).ToList();
        }

        /// <summary>
        /// Exportiert einen Thread als strukturierten Text
        /// </summary>
        public string ExportThreadAsText(string rootId)
        {
            var messages = GetThreadMessages(rootId);
            if (messages.Count == 0) return string.Empty;
            
            var result = new System.Text.StringBuilder();
            result.AppendLine($"=== THREAD EXPORT ===");
            result.AppendLine($"Erstellt: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            result.AppendLine($"Nachrichten: {messages.Count}");
            result.AppendLine();
            
            foreach (var message in messages)
            {
                var indent = new string(' ', message.ThreadDepth * 2);
                var replyPrefix = message.IsReply ? "↳ " : "";
                var teamInfo = !string.IsNullOrEmpty(message.TeamName) ? $"[{message.TeamName}] " : "";
                
                result.AppendLine($"{indent}{replyPrefix}{message.FormattedTimestamp} {message.EntryTypeIcon} {teamInfo}{message.Content}");
            }
            
            return result.ToString();
        }

        /// <summary>
        /// Statistiken für das Reply-System
        /// </summary>
        public ReplySystemStats GetReplyStats()
        {
            if (_notesCollection == null) return new ReplySystemStats();
            
            var totalNotes = _notesCollection.Count;
            var threadRoots = _notesCollection.Count(n => n.IsThreadRoot);
            var replies = _notesCollection.Count(n => n.IsReply);
            var threadsWithReplies = _notesCollection.Count(n => n.IsThreadRoot && n.HasReplies);
            
            return new ReplySystemStats
            {
                TotalNotes = totalNotes,
                ThreadRoots = threadRoots,
                Replies = replies,
                ThreadsWithReplies = threadsWithReplies,
                AverageRepliesPerThread = threadRoots > 0 ? (double)replies / threadRoots : 0
            };
        }
    }

    /// <summary>
    /// Statistiken für das Reply-System
    /// </summary>
    public class ReplySystemStats
    {
        public int TotalNotes { get; set; }
        public int ThreadRoots { get; set; }
        public int Replies { get; set; }
        public int ThreadsWithReplies { get; set; }
        public double AverageRepliesPerThread { get; set; }
    }
}
