using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// Interaction logic for TeamControl.xaml
    /// Vollständig auf MVVM-Pattern umgestellt - minimales Code-Behind
    /// v1.9.0 mit Orange-Design-System
    /// </summary>
    public partial class TeamControl : UserControl
    {
        private readonly TeamControlViewModel _viewModel;
        private Storyboard? _blinkingStoryboard;
        private Storyboard? _entranceStoryboard;
        private Team? _team;

        public static readonly DependencyProperty TeamProperty =
            DependencyProperty.Register("Team", typeof(Team), typeof(TeamControl),
                new PropertyMetadata(null, OnTeamChanged));

        /// <summary>
        /// Das Team-Objekt für diese Control-Instanz
        /// </summary>
        public Team? Team
        {
            get => _team;
            set
            {
                if (_team != value)
                {
                    _team = value;
                    if (value != null)
                    {
                        _viewModel.SetTeam(value);
                    }
                }
            }
        }

        /// <summary>
        /// Event wird ausgelöst, wenn das Team gelöscht werden soll
        /// </summary>
        public event EventHandler<Team>? TeamDeleteRequested;

        public TeamControl()
        {
            InitializeComponent();
            
            try
            {
                // ViewModel initialisieren
                _viewModel = new TeamControlViewModel();
                DataContext = _viewModel;
                
                // ViewModel-Events abonnieren
                _viewModel.TeamDeleteRequested += OnViewModelTeamDeleteRequested;
                _viewModel.DataChanged += OnViewModelDataChanged;
                _viewModel.EntranceAnimationRequested += OnViewModelEntranceAnimationRequested;
                _viewModel.BlinkingAnimationRequested += OnViewModelBlinkingAnimationRequested;
                
                // Storyboards laden
                LoadStoryboards();
                
                LoggingService.Instance.LogInfo("TeamControl v1.9.0 initialized with MVVM pattern and Orange design");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing TeamControl with MVVM", ex);
            }
        }

        /// <summary>
        /// Wendet das aktuelle Theme auf die Control an
        /// </summary>
        public void ApplyTheme(bool isDarkMode)
        {
            try
            {
                _viewModel.ApplyTheme(isDarkMode);
                LoggingService.Instance.LogInfo($"Theme applied to TeamControl: {(isDarkMode ? "Dark" : "Light")} mode with Orange design");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to TeamControl", ex);
            }
        }

        /// <summary>
        /// Aktualisiert die Team-Daten
        /// </summary>
        public void RefreshTeamData()
        {
            try
            {
                if (_team != null)
                {
                    // ViewModel benachrichtigen, dass sich die Daten geändert haben könnten
                    _viewModel.SetTeam(_team);
                    LoggingService.Instance.LogInfo($"Team data refreshed for {_team.TeamName}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error refreshing team data in TeamControl", ex);
            }
        }

        /// <summary>
        /// Startet die Entrance-Animation
        /// </summary>
        public void TriggerEntranceAnimation()
        {
            try
            {
                _viewModel.TriggerEntranceAnimation();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error triggering entrance animation", ex);
            }
        }

        private static void OnTeamChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TeamControl control && e.NewValue is Team team)
            {
                try
                {
                    control._viewModel.SetTeam(team);
                    control._viewModel.TriggerEntranceAnimation();
                    LoggingService.Instance.LogInfo($"Team set in TeamControl via MVVM: {team.TeamName}");
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("Error setting team in TeamControl", ex);
                }
            }
        }

        private void LoadStoryboards()
        {
            try
            {
                _blinkingStoryboard = FindResource("BlinkingAnimation") as Storyboard;
                _entranceStoryboard = FindResource("CardEntranceAnimation") as Storyboard;
                
                if (_blinkingStoryboard == null)
                {
                    LoggingService.Instance.LogWarning("BlinkingAnimation storyboard not found");
                }
                
                if (_entranceStoryboard == null)
                {
                    LoggingService.Instance.LogWarning("CardEntranceAnimation storyboard not found");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading storyboards in TeamControl", ex);
            }
        }

        #region ViewModel Event Handlers

        private void OnViewModelTeamDeleteRequested(Team team)
        {
            try
            {
                TeamDeleteRequested?.Invoke(this, team);
                LoggingService.Instance.LogInfo($"Team delete request forwarded from ViewModel: {team.TeamName}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling team delete request", ex);
            }
        }

        private void OnViewModelDataChanged()
        {
            try
            {
                // Notify parent window of data change
                if (Window.GetWindow(this) is MainWindow window)
                {
                    // Use reflection to call private method
                    var method = window.GetType().GetMethod("NotifyDataChanged", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    method?.Invoke(window, null);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error notifying data change", ex);
            }
        }

        private void OnViewModelEntranceAnimationRequested()
        {
            try
            {
                if (_entranceStoryboard != null)
                {
                    _entranceStoryboard.Begin(TeamBorder);
                    LoggingService.Instance.LogInfo("Entrance animation started for TeamControl");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error starting entrance animation", ex);
            }
        }

        private void OnViewModelBlinkingAnimationRequested(bool shouldBlink)
        {
            try
            {
                if (shouldBlink)
                {
                    StartBlinking();
                }
                else
                {
                    StopBlinking();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling blinking animation request", ex);
            }
        }

        #endregion

        #region Animation Methods

        private void StartBlinking()
        {
            try
            {
                // Stop existing animation first
                StopBlinking();
                
                if (_blinkingStoryboard != null)
                {
                    _blinkingStoryboard.Begin(TeamBorder, true);
                    LoggingService.Instance.LogInfo($"Blinking animation started for team {Team?.TeamName}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error starting blinking animation for team {Team?.TeamName}", ex);
            }
        }

        private void StopBlinking()
        {
            try
            {
                if (_blinkingStoryboard != null)
                {
                    _blinkingStoryboard.Stop(TeamBorder);
                    _blinkingStoryboard.Remove(TeamBorder);
                    LoggingService.Instance.LogInfo($"Blinking animation stopped for team {Team?.TeamName}");
                }
                
                // Ensure opacity is reset
                TeamBorder.BeginAnimation(UIElement.OpacityProperty, null);
                TeamBorder.Opacity = 1.0;
                TeamBorder.RenderTransform = null;
            }
            catch (Exception ex)
            {
                // Fallback: At least reset opacity
                try
                {
                    TeamBorder.BeginAnimation(UIElement.OpacityProperty, null);
                    TeamBorder.Opacity = 1.0;
                }
                catch { }
                
                LoggingService.Instance.LogError($"Error stopping blinking animation for team {Team?.TeamName}", ex);
            }
        }

        #endregion

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            try
            {
                base.OnRenderSizeChanged(sizeInfo);
                
                // Notify ViewModel about size changes if needed
                // This can be used for responsive design adjustments
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling size change in TeamControl", ex);
            }
        }

        #region IDisposable Pattern (for proper cleanup)

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        // Unsubscribe from ViewModel events
                        if (_viewModel != null)
                        {
                            _viewModel.TeamDeleteRequested -= OnViewModelTeamDeleteRequested;
                            _viewModel.DataChanged -= OnViewModelDataChanged;
                            _viewModel.EntranceAnimationRequested -= OnViewModelEntranceAnimationRequested;
                            _viewModel.BlinkingAnimationRequested -= OnViewModelBlinkingAnimationRequested;
                            
                            _viewModel.Dispose();
                        }
                        
                        // Stop any running animations
                        StopBlinking();
                        
                        // Clear storyboard references
                        _blinkingStoryboard = null;
                        _entranceStoryboard = null;
                        
                        LoggingService.Instance.LogInfo($"TeamControl disposed for team {Team?.TeamName}");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogError("Error disposing TeamControl", ex);
                    }
                }
                _disposed = true;
            }
        }

        ~TeamControl()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Entrance-Animation beim Laden starten
                Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        var storyboard = FindResource("CardEntranceAnimation") as Storyboard;
                        if (storyboard != null)
                        {
                            Storyboard.SetTarget(storyboard, TeamBorder);
                            storyboard.Begin();
                            LoggingService.Instance.LogInfo("TeamControl loaded and entrance animation triggered");
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogError("Error triggering entrance animation on load", ex);
                    }
                }, System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during TeamControl load", ex);
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Events aufräumen
                _viewModel.TeamDeleteRequested -= OnViewModelTeamDeleteRequested;
                _viewModel.DataChanged -= OnViewModelDataChanged;
                _viewModel.EntranceAnimationRequested -= OnViewModelEntranceAnimationRequested;
                _viewModel.BlinkingAnimationRequested -= OnViewModelBlinkingAnimationRequested;
                
                // ViewModel aufräumen
                if (_viewModel is IDisposable disposableViewModel)
                {
                    disposableViewModel.Dispose();
                }
                
                LoggingService.Instance.LogInfo("TeamControl unloaded and cleaned up");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during TeamControl unload", ex);
            }
        }
    }
}
