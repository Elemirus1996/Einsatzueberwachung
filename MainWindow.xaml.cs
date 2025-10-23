using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Views;

namespace Einsatzueberwachung
{
    /// <summary>
    /// MainWindow - Enhanced with Full Theme Integration v1.9.0
    /// Now inherits from BaseThemeWindow for automatic theme support
    /// MVVM-Implementation with comprehensive theme integration
    /// </summary>
    public partial class MainWindow : BaseThemeWindow
    {
        private MainViewModel? _viewModel;
        private bool _isFullscreen = false;
        private WindowState _previousWindowState;
        private WindowStyle _previousWindowStyle;

        // Dashboard View Management
        private System.Collections.Generic.Dictionary<int, Views.TeamCompactCard> _teamCompactCards = new();

        public MainWindow()
        {
            InitializeComponent();
            InitializeThemeSupport(); // Initialize theme after component initialization
            // Don't initialize ViewModel here - this is called during StartWindow transition
            
            LoggingService.Instance.LogInfo("MainWindow initialized with enhanced theme integration v1.9.2");
        }

        public MainWindow(EinsatzData einsatzData, int firstWarningMinutes, int secondWarningMinutes) : this()
        {
            try
            {
                // Initialize ViewModel with mission data
                _viewModel = new MainViewModel(einsatzData, firstWarningMinutes, secondWarningMinutes);
                DataContext = _viewModel;
                SubscribeToViewModelEvents();
                
                // MainViewModel für globalen Zugriff registrieren
                MainViewModelService.Instance.RegisterMainViewModel(_viewModel);
                
                // WICHTIG: GlobalNotesService mit den Collections des ViewModels initialisieren
                InitializeGlobalNotesService();
                
                LoggingService.Instance.LogInfo($"MainWindow initialized with mission data, theme support and Reply system - {einsatzData.EinsatzTyp}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing MainWindow with mission data", ex);
            }
        }

        /// <summary>
        /// Constructor for MainWindow when started without mission data (for recovery scenarios)
        /// </summary>
        public static MainWindow CreateForRecovery()
        {
            var mainWindow = new MainWindow();
            mainWindow.InitializeViewModel();
            return mainWindow;
        }

        /// <summary>
        /// Initialisiert den GlobalNotesService mit den MainViewModel Collections
        /// KRITISCH für das Reply-System!
        /// </summary>
        private void InitializeGlobalNotesService()
        {
            try
            {
                if (_viewModel == null)
                {
                    LoggingService.Instance.LogError("Cannot initialize GlobalNotesService - MainViewModel is null");
                    return;
                }

                // GlobalNotesService mit ViewModel-Collections initialisieren
                // REPARIERT: Verwende die richtige öffentliche Methode
                GlobalNotesService.Instance.Initialize(
                    _viewModel.FilteredNotesCollection,
                    (message) => _viewModel.AddGlobalNote(message, GlobalNotesEntryType.Info),
                    (message) => _viewModel.AddGlobalNote(message, GlobalNotesEntryType.Warnung),
                    (message) => _viewModel.AddGlobalNote(message, GlobalNotesEntryType.Fehler)
                );

                LoggingService.Instance.LogInfo("GlobalNotesService successfully initialized with MainViewModel collections for Reply system");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Critical error initializing GlobalNotesService for Reply system", ex);
            }
        }

        #region Reply-System Event Handlers - VOLLSTÄNDIG REPARIERT

        /// <summary>
        /// Event-Handler für Reply-Button Klicks - REPARIERT!
        /// </summary>
        private void ReplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is GlobalNotesEntry originalNote)
                {
                    LoggingService.Instance.LogInfo($"Reply button clicked for note: {originalNote.Id}");
                    ShowReplyDialog(originalNote);
                }
                else
                {
                    LoggingService.Instance.LogWarning("Reply button clicked but no valid note found in Tag");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling reply button click", ex);
                MessageBox.Show($"Fehler beim Öffnen des Antwort-Dialogs:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Zeigt den Thread-Entry-Dialog für eine bestimmte Notiz - VOLLSTÄNDIG REPARIERT!
        /// </summary>
        private void ShowReplyDialog(GlobalNotesEntry originalNote)
        {
            try
            {
                LoggingService.Instance.LogInfo($"Opening reply dialog for note {originalNote.Id}: {originalNote.Content.Substring(0, Math.Min(50, originalNote.Content.Length))}...");
                
                // Sammle verfügbare Ziele für den Thread-Eintrag
                var availableTargets = _viewModel?.NoteTargets?.ToList() ?? new System.Collections.Generic.List<NoteTarget>();

                // Zeige Thread-Entry-Dialog mit VOLLSTÄNDIGER Integration
                var reply = ReplyDialogWindow.ShowThreadEntryDialog(this, originalNote, availableTargets);
                
                if (reply != null)
                {
                    LoggingService.Instance.LogInfo($"Reply created successfully: {reply.Content.Substring(0, Math.Min(50, reply.Content.Length))}...");
                    LoggingService.Instance.LogInfo($"Reply properties - ID: {reply.Id}, ReplyToEntryId: {reply.ReplyToEntryId}, ThreadDepth: {reply.ThreadDepth}");
                    
                    // KRITISCH: Reply über GlobalNotesService hinzufügen, damit alle Verknüpfungen korrekt erstellt werden
                    GlobalNotesService.Instance.AddNote(reply);
                    
                    // ZUSÄTZLICH: Auch über ViewModel hinzufügen für UI-Updates
                    // ABER: Verwende eine Spezial-Methode um Duplikate zu vermeiden
                    _viewModel?.AddReply(reply.ReplyToEntryId ?? "", reply.Content, reply.TeamName);
                    
                    LoggingService.Instance.LogInfo($"Reply added to both GlobalNotesService and MainViewModel - Thread depth: {reply.ThreadDepth}");
                    
                    // Scroll zur neuesten Notiz
                    Dispatcher.BeginInvoke(() =>
                    {
                        GlobalNotesScrollViewer?.ScrollToEnd();
                    });
                }
                else
                {
                    LoggingService.Instance.LogInfo("Reply dialog was cancelled or returned null");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing thread entry dialog", ex);
                MessageBox.Show($"Fehler beim Erstellen des Thread-Eintrags:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        private void InitializeViewModel()
        {
            try
            {
                _viewModel = new MainViewModel();
                DataContext = _viewModel;
                SubscribeToViewModelEvents();
                
                // MainViewModel für globalen Zugriff registrieren
                MainViewModelService.Instance.RegisterMainViewModel(_viewModel);
                
                // WICHTIG: Auch hier GlobalNotesService initialisieren
                InitializeGlobalNotesService();
                
                // Don't check for recovery here - this is only called from CreateForRecovery
                // Recovery is already handled in App.xaml.cs
                LoggingService.Instance.LogInfo("MainWindow ViewModel initialized for recovery scenario with Reply system");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing MainWindow ViewModel", ex);
            }
        }

        private void SubscribeToViewModelEvents()
        {
            if (_viewModel != null)
            {
                _viewModel.TeamAddRequested += ViewModel_TeamAddRequested;
                _viewModel.HelpRequested += ViewModel_HelpRequested;
                _viewModel.ExportRequested += ViewModel_ExportRequested;
                _viewModel.MenuRequested += ViewModel_MenuRequested;
                _viewModel.ExportLogRequested += ViewModel_ExportLogRequested;
                _viewModel.FullscreenToggleRequested += ViewModel_FullscreenToggleRequested;
                _viewModel.ScrollToLatestNoteRequested += ViewModel_ScrollToLatestNoteRequested;
                _viewModel.ThemeChanged += ViewModel_ThemeChanged;
                _viewModel.Teams.CollectionChanged += Teams_CollectionChanged;
            }
        }

        #region ViewModel Event Handlers

        private void ViewModel_TeamAddRequested()
        {
            try
            {
                if (_viewModel?.Teams.Count >= 50)
                {
                    MessageBox.Show("Maximale Anzahl von 50 Teams erreicht!", 
                        "Limit erreicht", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Übergebe verfügbare Suchgebiete an TeamInputWindow
                var inputWindow = _viewModel?.EinsatzData?.SearchAreas != null
                    ? new Views.TeamInputWindow(_viewModel.EinsatzData.SearchAreas)
                    : new Views.TeamInputWindow();
                    
                inputWindow.Owner = this;
                inputWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                
                if (inputWindow.ShowDialog() == true)
                {
                    // Verwende die Standard-Team-Erstellung
                    var newTeam = new Team
                    {
                        TeamName = inputWindow.TeamName,
                        HundName = inputWindow.HundName,
                        Hundefuehrer = inputWindow.Hundefuehrer,
                        Helfer = inputWindow.Helfer,
                        Suchgebiet = inputWindow.Suchgebiet
                    };
                    
                    // Setze MultipleTeamTypes falls verfügbar
                    if (inputWindow.PreselectedTeamTypes != null)
                    {
                        newTeam.MultipleTeamTypes = inputWindow.PreselectedTeamTypes;
                        LoggingService.Instance.LogInfo($"Team created with preselected types: {inputWindow.PreselectedTeamTypes.DisplayName}");
                    }
                    else
                    {
                        // Erstelle leere MultipleTeamTypes ohne automatisches Allgemein
                        newTeam.MultipleTeamTypes = new MultipleTeamTypes();
                        newTeam.MultipleTeamTypes.SelectedTypes.Clear(); // Explizit leeren um sicherzugehen
                        LoggingService.Instance.LogWarning("Team created without any team types - this should not happen in normal flow");
                    }
                    
                    _viewModel?.AddTeam(newTeam);
                    
                    LoggingService.Instance.LogInfo($"Team created with search area: {newTeam.Suchgebiet}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling team add request via MVVM", ex);
            }
        }

        private void ViewModel_HelpRequested()
        {
            try
            {
                var helpWindow = new Views.HelpWindow();
                helpWindow.Owner = this;
                helpWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                helpWindow.Show();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening help window via MVVM", ex);
            }
        }

        private void ViewModel_ExportRequested()
        {
            try
            {
                ShowExportDialog();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling export request via MVVM", ex);
            }
        }

        private void ViewModel_MenuRequested()
        {
            try
            {
                ShowContextMenu();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing menu via MVVM", ex);
            }
        }

        private void ViewModel_ExportLogRequested()
        {
            try
            {
                ExportEinsatzLog();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error exporting log via MVVM", ex);
            }
        }

        private void ViewModel_FullscreenToggleRequested()
        {
            try
            {
                ToggleFullscreen();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error toggling fullscreen via MVVM", ex);
            }
        }

        private void ViewModel_ScrollToLatestNoteRequested()
        {
            try
            {
                Dispatcher.BeginInvoke(() =>
                {
                    GlobalNotesScrollViewer?.ScrollToEnd();
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error scrolling to latest note via MVVM", ex);
            }
        }

        private void ViewModel_ThemeChanged(bool isDarkMode)
        {
            try
            {
                ApplyThemeToAllTeams(isDarkMode);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme changes via MVVM", ex);
            }
        }

        private void Teams_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (e.NewItems != null)
                {
                    foreach (Team team in e.NewItems)
                    {
                        CreateTeamCompactCard(team);
                    }
                }

                if (e.OldItems != null)
                {
                    foreach (Team team in e.OldItems)
                    {
                        RemoveTeamCompactCard(team);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling teams collection change via MVVM", ex);
            }
        }

        #endregion

        #region Team Management

        private void CreateTeamCompactCard(Team team)
        {
            try
            {
                var teamCompactCard = new Views.TeamCompactCard { Team = team };
                teamCompactCard.TeamClicked += OnTeamCompactCardClicked;
                teamCompactCard.ApplyTheme(UnifiedThemeManager.Instance.IsDarkMode);
                
                _teamCompactCards[team.TeamId] = teamCompactCard;
                DashboardGrid?.Children.Add(teamCompactCard);
                
                LoggingService.Instance.LogInfo($"Team compact card created via MVVM: {team.TeamName}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error creating team compact card via MVVM: {team?.TeamName}", ex);
            }
        }

        private void RemoveTeamCompactCard(Team team)
        {
            try
            {
                if (_teamCompactCards.ContainsKey(team.TeamId))
                {
                    var teamCompactCard = _teamCompactCards[team.TeamId];
                    DashboardGrid?.Children.Remove(teamCompactCard);
                    _teamCompactCards.Remove(team.TeamId);
                    
                    if (teamCompactCard is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error removing team compact card via MVVM: {team?.TeamName}", ex);
            }
        }

        private void OnTeamCompactCardClicked(object? sender, Team team)
        {
            try
            {
                var detailWindow = new Views.TeamDetailWindow(team);
                detailWindow.Owner = this;
                detailWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                detailWindow.Show();
                
                LoggingService.Instance.LogInfo($"Team detail window opened via MVVM: {team.TeamName}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening team detail window via MVVM", ex);
            }
        }

        private void ApplyThemeToAllTeams(bool isDarkMode)
        {
            try
            {
                foreach (var teamCompactCard in _teamCompactCards.Values)
                {
                    teamCompactCard.ApplyTheme(isDarkMode);
                }
                
                LoggingService.Instance.LogInfo($"Applied theme to {_teamCompactCards.Count} team compact cards via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to teams via MVVM", ex);
            }
        }

        #endregion

        #region UI Operations (Delegated from ViewModel)

        private void ToggleFullscreen()
        {
            try
            {
                if (_isFullscreen)
                {
                    WindowState = _previousWindowState;
                    WindowStyle = _previousWindowStyle;
                    _isFullscreen = false;
                    
                    Width = 1600;
                    Height = 900;
                    Top = 100;
                    Left = 100;
                }
                else
                {
                    _previousWindowState = WindowState;
                    _previousWindowStyle = WindowStyle;
                    
                    WindowStyle = WindowStyle.None;
                    WindowState = WindowState.Normal;
                    _isFullscreen = true;
                    
                    var screenWidth = SystemParameters.PrimaryScreenWidth;
                    var screenHeight = SystemParameters.PrimaryScreenHeight;
                    Width = screenWidth;
                    Height = screenHeight;
                    Top = 0;
                    Left = 0;
                }
                
                LoggingService.Instance.LogInfo($"Fullscreen toggled via MVVM: {_isFullscreen}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error toggling fullscreen via MVVM", ex);
            }
        }

        private void ShowExportDialog()
        {
            try
            {
                // Überprüfen ob Einsatz-Daten verfügbar sind
                if (_viewModel?.Teams == null || !_viewModel.Teams.Any())
                {
                    MessageBox.Show("Keine Teams zum Exportieren vorhanden.", 
                        "PDF-Export", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Erstelle EinsatzData für den Export
                var einsatzData = CreateEinsatzDataForExport();
                var teams = _viewModel.Teams.ToList();

                // Öffne PDF-Export-Window
                var exportWindow = new Views.PdfExportWindow(einsatzData, teams);
                exportWindow.Owner = this;
                exportWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                exportWindow.ShowDialog();
                
                LoggingService.Instance.LogInfo("PDF export window opened via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing export dialog via MVVM", ex);
                MessageBox.Show($"Fehler beim Öffnen des Export-Dialogs:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Erstellt EinsatzData-Objekt für den PDF-Export aus den aktuellen ViewModel-Daten
        /// </summary>
        private EinsatzData CreateEinsatzDataForExport()
        {
            var einsatzData = new EinsatzData
            {
                EinsatzDatum = DateTime.Now,
                IstEinsatz = true, // Dies setzt EinsatzTyp automatisch auf "Einsatz"
                Einsatzort = _viewModel?.Einsatzort ?? "Unbekannt",
                Einsatzleiter = _viewModel?.Einsatzleiter ?? "Unbekannt"
            };

            // Kopiere globale Notizen wenn verfügbar
            if (_viewModel?.FilteredNotesCollection != null)
            {
                foreach (var note in _viewModel.FilteredNotesCollection)
                {
                    einsatzData.GlobalNotesEntries.Add(note);
                }
            }

            return einsatzData;
        }

        private void ExportEinsatzLog()
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Text Dateien (*.txt)|*.txt|Alle Dateien (*.*)|*.*",
                    FileName = $"Einsatzlog_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                    Title = "Einsatz-Log exportieren"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    LoggingService.Instance.LogInfo($"Log export completed via MVVM: {saveDialog.FileName}");
                    MessageBox.Show($"Log würde exportiert werden nach:\n{saveDialog.FileName}", 
                        "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error exporting log via MVVM", ex);
            }
        }

        private void ShowContextMenu()
        {
            try
            {
                // Direkt zu den Settings-Fenster weiterleiten
                OpenSettingsWindow();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing context menu via MVVM", ex);
            }
        }

        private void OpenMasterDataWindow()
        {
            try
            {
                var window = new Views.MasterDataWindow();
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Show();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening master data window via MVVM", ex);
            }
        }

        private void OpenMobileConnectionWindow()
        {
            try
            {
                // Stelle sicher, dass MainViewModel verfügbar ist
                if (_viewModel == null)
                {
                    LoggingService.Instance.LogError("Cannot open MobileConnectionWindow - MainViewModel is null");
                    MessageBox.Show("Fehler: Hauptdaten sind nicht verfügbar.\n\n" +
                                   "Bitte starten Sie die Anwendung neu.",
                                   "Datenintegration-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                LoggingService.Instance.LogInfo($"Opening MobileConnectionWindow with data integration. Teams: {_viewModel.Teams.Count}, Notes: {_viewModel.FilteredNotesCollection.Count}");
                
                // Verwende die neue Factory-Methode mit Daten-Integration
                var window = Views.MobileConnectionWindow.CreateWithDataIntegration(_viewModel);
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Show();
                
                LoggingService.Instance.LogInfo("MobileConnectionWindow opened with full data integration");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening mobile connection window with data integration", ex);
                
                // Erweiterte Fehlerdiagnose
                var errorDetails = $"Error: {ex.Message}";
                if (_viewModel != null)
                {
                    errorDetails += $"\nTeams verfügbar: {_viewModel.Teams?.Count ?? -1}";
                    errorDetails += $"\nNotizen verfügbar: {_viewModel.FilteredNotesCollection?.Count ?? -1}";
                }
                
                var result = MessageBox.Show($"Fehler beim Öffnen der Mobile-Verbindung mit Datenintegration:\n\n" +
                                           $"{errorDetails}\n\n" +
                                           $"Möchten Sie das Mobile-Fenster ohne Datenintegration öffnen?\n\n" +
                                           $"JA: Mobile-Fenster mit Demo-Daten\n" +
                                           $"NEIN: Abbrechen und Problem beheben",
                    "Mobile-Verbindung Fehler", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Fallback: Normale MobileConnectionWindow ohne Daten-Integration
                    try
                    {
                        var fallbackWindow = new Views.MobileConnectionWindow();
                        fallbackWindow.Owner = this;
                        fallbackWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        fallbackWindow.Show();
                        
                        LoggingService.Instance.LogWarning("MobileConnectionWindow opened without data integration (fallback mode)");
                        
                        MessageBox.Show("Mobile-Fenster wurde im Notfall-Modus geöffnet.\n\n" +
                                       "Demo-Daten werden angezeigt statt echter Einsatzdaten.\n\n" +
                                       "Für vollständige Funktionalität bitte die Anwendung neu starten.",
                                       "Notfall-Modus aktiv", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception fallbackEx)
                    {
                        LoggingService.Instance.LogError("Error opening fallback mobile connection window", fallbackEx);
                        MessageBox.Show($"Kritischer Fehler: Auch der Notfall-Modus ist fehlgeschlagen:\n\n{fallbackEx.Message}\n\n" +
                                       "Bitte starten Sie die Anwendung neu oder kontaktieren Sie den Support.",
                            "Kritischer Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void OpenStatisticsWindow()
        {
            try
            {
                if (_viewModel?.Teams != null)
                {
                    var window = new Views.StatisticsWindow(_viewModel.Teams.ToList(), new EinsatzData());
                    window.Owner = this;
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.Show();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening statistics window via MVVM", ex);
            }
        }

        private void OpenWarningSettingsWindow()
        {
            try
            {
                if (_viewModel?.Teams != null)
                {
                    var window = new Views.TeamWarningSettingsWindow(_viewModel.Teams.ToList(), 
                        _viewModel.GlobalFirstWarningMinutes, _viewModel.GlobalSecondWarningMinutes);
                    window.Owner = this;
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening warning settings window via MVVM", ex);
            }
        }

        private void OpenAboutWindow()
        {
            try
            {
                var window = new Views.AboutWindow();
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening about window via MVVM", ex);
            }
        }

        private void OpenSettingsWindow()
        {
            try
            {
                var window = new Views.SettingsWindow();
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error opening settings window via MVVM", ex);
            }
        }

        private void OpenMapWindow()
        {
            try
            {
                if (_viewModel?.EinsatzData == null)
                {
                    LoggingService.Instance.LogError("Cannot open MapWindow - EinsatzData is null");
                    MessageBox.Show("Fehler: Einsatzdaten sind nicht verfügbar.\n\n" +
                                   "Bitte starten Sie die Anwendung neu.",
                                   "Datenintegration-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                LoggingService.Instance.LogInfo($"Opening MapWindow with {_viewModel.Teams.Count} teams");
                
                // Zeige Lade-Indikator
                Mouse.OverrideCursor = Cursors.Wait;
                
                try
                {
                    var mapWindow = new Views.MapWindow(
                        _viewModel.EinsatzData, 
                        _viewModel.Teams.ToList(),
                        _viewModel.Einsatzort
                    );
                    mapWindow.Owner = this;
                    mapWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    
                    // Stelle sicher dass der Cursor zurückgesetzt wird
                    mapWindow.Loaded += (s, e) => Mouse.OverrideCursor = null;
                    mapWindow.Show();
                    
                    LoggingService.Instance.LogInfo("MapWindow opened with team integration");
                }
                finally
                {
                    // Setze Cursor zurück auch bei Fehler
                    Mouse.OverrideCursor = null;
                }
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                LoggingService.Instance.LogError("Error opening map window", ex);
                MessageBox.Show($"Fehler beim Öffnen der Karte:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Event-Handler für Karte-Button im Header
        /// </summary>
        private void OpenMapButton_Click(object sender, RoutedEventArgs e)
        {
            OpenMapWindow();
        }

        #endregion

        #region Recovery and Startup

        // Recovery is now handled in App.xaml.cs during application startup
        // This region is kept for potential future recovery-related functionality

        #endregion

        #region Window Lifecycle

        protected override void OnClosing(CancelEventArgs e)
        {
            try
            {
                if (_viewModel?.Teams.Any(t => t.IsRunning) == true)
                {
                    var result = MessageBox.Show(
                        "Es sind noch aktive Timer vorhanden.\n\nMöchten Sie die Anwendung wirklich beenden?",
                        "Aktive Einsatzzeiten",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                
                LoggingService.Instance.LogInfo("MainWindow closing via MVVM - starting cleanup");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during window closing via MVVM", ex);
            }
            finally
            {
                base.OnClosing(e);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // MainViewModel unregistrieren
                MainViewModelService.Instance.UnregisterMainViewModel();
                
                _viewModel?.Dispose();
                _teamCompactCards.Clear();
                
                LoggingService.Instance.LogInfo("MainWindow closed via MVVM with theme support - cleanup completed");
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during window cleanup via MVVM", ex);
            }
            finally
            {
                base.OnClosed(e);
            }
        }

        #endregion
    }
}
