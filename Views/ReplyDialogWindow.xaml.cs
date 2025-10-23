using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// ReplyDialogWindow - Enhanced with Theme Integration v1.9.0
    /// Now inherits from BaseThemeWindow for automatic theme support
    /// VOLLSTÄNDIG REPARIERT mit korrekter ViewModel-Integration
    /// </summary>
    public partial class ReplyDialogWindow : BaseThemeWindow
    {
        private ReplyDialogViewModel? _viewModel;
        
        public GlobalNotesEntry? ThreadEntry { get; private set; }

        public ReplyDialogWindow()
        {
            InitializeComponent();
            InitializeThemeSupport();
            
            LoggingService.Instance.LogInfo("ReplyDialogWindow initialized with theme integration v1.9.0");
        }

        /// <summary>
        /// Konstruktor mit ViewModel - REPARIERT!
        /// </summary>
        public ReplyDialogWindow(ReplyDialogViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = _viewModel;
            
            // Subscribe to ViewModel events
            _viewModel.RequestClose += () => this.Close();
            _viewModel.ReplyCreated += (reply) => 
            {
                ThreadEntry = reply;
                DialogResult = true;
            };
            _viewModel.ShowMessage += (message) => 
                MessageBox.Show(message, "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            
            InitializeUIFromViewModel();
            
            LoggingService.Instance.LogInfo("ReplyDialogWindow initialized with ViewModel integration");
        }

        /// <summary>
        /// Initialisiert UI-Elemente basierend auf dem ViewModel
        /// </summary>
        private void InitializeUIFromViewModel()
        {
            if (_viewModel?.OriginalNote != null)
            {
                // Zeige ursprüngliche Nachricht an
                OriginalNoteContainer.Visibility = Visibility.Visible;
                OriginalNoteText.Text = _viewModel.OriginalNotePreview ?? "Ursprüngliche Nachricht nicht verfügbar";
                
                // Fokus auf Textbox
                ReplyTextBox.Focus();
            }
            else
            {
                // Verstecke ursprüngliche Nachricht für einfache Notizen
                OriginalNoteContainer.Visibility = Visibility.Collapsed;
            }
        }

        protected override void ApplyThemeToWindow(bool isDarkMode)
        {
            try
            {
                // Apply theme-specific styling if needed
                base.ApplyThemeToWindow(isDarkMode);
                
                LoggingService.Instance.LogInfo($"Theme applied to ReplyDialogWindow: {(isDarkMode ? "Dark" : "Light")} mode");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to ReplyDialogWindow", ex);
            }
        }

        #region Static Factory Methods - VOLLSTÄNDIG REPARIERT

        /// <summary>
        /// Zeigt einen Thread-Entry-Dialog für eine bestimmte Notiz - REPARIERT!
        /// </summary>
        public static GlobalNotesEntry? ShowThreadEntryDialog(Window owner, GlobalNotesEntry originalNote, IEnumerable<NoteTarget> availableTargets)
        {
            try
            {
                // Erstelle ViewModel mit Daten
                var viewModel = new ReplyDialogViewModel(originalNote);
                
                // Initialisiere mit verfügbaren Zielen
                viewModel.InitializeReply(originalNote, availableTargets.ToList());
                
                // Erstelle Dialog mit ViewModel
                var dialog = new ReplyDialogWindow(viewModel);
                dialog.Owner = owner;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dialog.Title = $"Antwort auf: {originalNote.Content.Substring(0, Math.Min(30, originalNote.Content.Length))}...";
                
                LoggingService.Instance.LogInfo($"Opening thread entry dialog for note {originalNote.Id}");
                
                if (dialog.ShowDialog() == true)
                {
                    var result = dialog.ThreadEntry;
                    if (result != null)
                    {
                        LoggingService.Instance.LogInfo($"Thread entry created successfully: {result.Content.Substring(0, Math.Min(50, result.Content.Length))}...");
                        return result;
                    }
                }
                
                LoggingService.Instance.LogInfo("Thread entry dialog cancelled or no result");
                return null;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing thread entry dialog", ex);
                MessageBox.Show($"Fehler beim Öffnen des Antwort-Dialogs:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// Zeigt einen einfachen Reply-Dialog - REPARIERT!
        /// </summary>
        public static string? ShowReplyDialog(Window owner, string title = "Antwort erstellen", string defaultText = "")
        {
            try
            {
                // Erstelle ViewModel für einfache Antwort
                var viewModel = new ReplyDialogViewModel();
                viewModel.InitializeSimpleReply(defaultText);
                
                var dialog = new ReplyDialogWindow(viewModel);
                dialog.Owner = owner;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dialog.Title = title;
                
                LoggingService.Instance.LogInfo($"Opening simple reply dialog: {title}");
                
                if (dialog.ShowDialog() == true)
                {
                    var result = dialog.ThreadEntry?.Content;
                    LoggingService.Instance.LogInfo($"Simple reply created: {result?.Substring(0, Math.Min(50, result?.Length ?? 0))}...");
                    return result;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing simple reply dialog", ex);
                MessageBox.Show($"Fehler beim Öffnen des Antwort-Dialogs:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        #endregion

        #region Event Handlers - REPARIERT!

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel != null)
                {
                    // Verwende ViewModel um Thread Entry zu erstellen
                    var result = _viewModel.CreateThreadEntry();
                    if (result != null)
                    {
                        ThreadEntry = result;
                        DialogResult = true;
                        LoggingService.Instance.LogInfo("Reply dialog completed successfully via ViewModel");
                    }
                    else
                    {
                        LoggingService.Instance.LogWarning("Reply dialog validation failed - no result from ViewModel");
                        MessageBox.Show("Bitte geben Sie eine Antwort ein.", "Eingabe erforderlich", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    // Fallback für Fälle ohne ViewModel
                    LoggingService.Instance.LogWarning("ReplyDialogWindow used without ViewModel - using fallback");
                    
                    var replyText = ReplyTextBox.Text.Trim();
                    if (string.IsNullOrEmpty(replyText))
                    {
                        MessageBox.Show("Bitte geben Sie eine Antwort ein.", "Eingabe erforderlich", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    // Erstelle einfachen Thread Entry
                    ThreadEntry = new GlobalNotesEntry
                    {
                        Content = replyText,
                        Timestamp = DateTime.Now,
                        EntryType = GlobalNotesEntryType.Manual,
                        TeamName = "Manueller Eintrag"
                    };
                    
                    DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling OK button in ReplyDialogWindow", ex);
                MessageBox.Show($"Fehler beim Erstellen der Antwort:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = false;
                LoggingService.Instance.LogInfo("Reply dialog cancelled");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling Cancel button in ReplyDialogWindow", ex);
            }
        }

        #endregion

        #region Window Events

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Cleanup ViewModel subscriptions
                if (_viewModel != null)
                {
                    // Events werden automatisch garbage collected, aber explizites cleanup ist sauberer
                    LoggingService.Instance.LogInfo("ReplyDialogWindow closed and cleaned up");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during ReplyDialogWindow cleanup", ex);
            }
            finally
            {
                base.OnClosed(e);
            }
        }

        #endregion
    }
}
