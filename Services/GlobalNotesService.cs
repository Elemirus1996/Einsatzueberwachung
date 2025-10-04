using System;
using System.Collections.ObjectModel;
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

        private GlobalNotesService() { }

        public void Initialize(ObservableCollection<GlobalNotesEntry> notesCollection,
            Action<string> addInfoNote, Action<string> addWarningNote, Action<string> addErrorNote)
        {
            _notesCollection = notesCollection;
            _addInfoNote = addInfoNote;
            _addWarningNote = addWarningNote;
            _addErrorNote = addErrorNote;
        }

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
    }
}
