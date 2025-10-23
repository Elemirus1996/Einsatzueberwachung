using System;
using System.ComponentModel;
using System.Windows.Input;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    /// <summary>
    /// ViewModel für den Reply-Dialog
    /// </summary>
    public class ReplyDialogViewModel : BaseViewModel
    {
        private string _replyText = string.Empty;
        private bool _canSend = false;
        private GlobalNotesEntry? _originalNote;
        private NoteTarget? _selectedTarget;

        public ReplyDialogViewModel()
        {
            InitializeCommands();
        }

        public ReplyDialogViewModel(GlobalNotesEntry originalNote, NoteTarget? selectedTarget = null)
        {
            _originalNote = originalNote;
            _selectedTarget = selectedTarget;
            InitializeCommands();
        }

        #region Properties

        public string ReplyText
        {
            get => _replyText;
            set
            {
                if (SetProperty(ref _replyText, value))
                {
                    CanSend = !string.IsNullOrWhiteSpace(value);
                }
            }
        }

        public bool CanSend
        {
            get => _canSend;
            private set => SetProperty(ref _canSend, value);
        }

        public GlobalNotesEntry? OriginalNote
        {
            get => _originalNote;
            set => SetProperty(ref _originalNote, value);
        }

        public NoteTarget? SelectedTarget
        {
            get => _selectedTarget;
            set => SetProperty(ref _selectedTarget, value);
        }

        public string? OriginalNotePreview => _originalNote?.Content?.Length > 100 
            ? _originalNote.Content.Substring(0, 100) + "..." 
            : _originalNote?.Content;

        #endregion

        #region Commands

        public ICommand SendReplyCommand { get; private set; } = null!;
        public ICommand CancelCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            SendReplyCommand = new RelayCommand(ExecuteSendReply, () => CanSend);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        #endregion

        #region Command Implementations

        private void ExecuteSendReply()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ReplyText) || _originalNote == null)
                {
                    return;
                }

                var reply = new GlobalNotesEntry
                {
                    Content = ReplyText,
                    Timestamp = DateTime.Now,
                    TeamName = _selectedTarget?.DisplayName ?? "Antwort",
                    EntryType = GlobalNotesEntryType.Manual,
                    ReplyToEntryId = _originalNote.Id,
                    ReplyToEntry = _originalNote,
                    ThreadId = _originalNote.ThreadId ?? _originalNote.Id,
                    ThreadDepth = Math.Min(_originalNote.ThreadDepth + 1, 3)
                };

                ReplyCreated?.Invoke(reply);
                RequestClose?.Invoke();
                
                LoggingService.Instance.LogInfo($"Reply created for note {_originalNote.Id}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error sending reply", ex);
                ShowMessage?.Invoke("Fehler beim Senden der Antwort: " + ex.Message);
            }
        }

        private void ExecuteCancel()
        {
            try
            {
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error canceling reply", ex);
            }
        }

        #endregion

        #region UI Integration Methods

        public void InitializeReply(GlobalNotesEntry originalNote, System.Collections.Generic.List<NoteTarget> availableTargets)
        {
            OriginalNote = originalNote;
            // UI-spezifische Initialisierung kann hier hinzugefügt werden
            LoggingService.Instance.LogInfo($"Reply dialog initialized for note {originalNote.Id}");
        }

        public void InitializeSimpleReply(string content)
        {
            ReplyText = content;
            LoggingService.Instance.LogInfo("Simple reply initialized");
        }

        public GlobalNotesEntry? CreateThreadEntry()
        {
            if (string.IsNullOrWhiteSpace(ReplyText) || _originalNote == null)
            {
                return null;
            }

            var reply = new GlobalNotesEntry
            {
                Content = ReplyText,
                Timestamp = DateTime.Now,
                TeamName = _selectedTarget?.DisplayName ?? "Thread-Eintrag",
                EntryType = GlobalNotesEntryType.Manual,
                ReplyToEntryId = _originalNote.Id,
                ReplyToEntry = _originalNote,
                ThreadId = _originalNote.ThreadId ?? _originalNote.Id,
                ThreadDepth = Math.Min(_originalNote.ThreadDepth + 1, 3)
            };

            LoggingService.Instance.LogInfo($"Thread entry created for note {_originalNote.Id}");
            return reply;
        }

        #endregion

        #region Events

        public event Action<GlobalNotesEntry>? ReplyCreated;
        public event Action? RequestClose;
        public event Action<string>? ShowMessage;

        #endregion
    }
}
