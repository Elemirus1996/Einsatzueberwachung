using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// Interaction logic for TeamCompactCard.xaml - MVVM Implementation v1.9.0
    /// Dashboard-Hauptkomponente mit vollständiger Orange-Design-Integration
    /// </summary>
    public partial class TeamCompactCard : UserControl
    {
        private TeamCompactCardViewModel? _viewModel;

        public static readonly DependencyProperty TeamProperty =
            DependencyProperty.Register("Team", typeof(Team), typeof(TeamCompactCard),
                new PropertyMetadata(null, OnTeamChanged));

        public Team? Team
        {
            get => (Team?)GetValue(TeamProperty);
            set => SetValue(TeamProperty, value);
        }

        public event EventHandler<Team>? TeamClicked;

        public TeamCompactCard()
        {
            InitializeComponent();
            InitializeViewModel();
            
            LoggingService.Instance.LogInfo("TeamCompactCard initialized with MVVM + Orange Design v1.9.0");
        }

        private void InitializeViewModel()
        {
            try
            {
                _viewModel = new TeamCompactCardViewModel();
                DataContext = _viewModel;
                
                // Subscribe to ViewModel events
                _viewModel.TeamClicked += OnViewModelTeamClicked;
                
                LoggingService.Instance.LogInfo("TeamCompactCardViewModel initialized and connected");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing TeamCompactCardViewModel", ex);
                
                // Create a fallback DataContext to prevent binding errors
                DataContext = new { };
            }
        }

        private void OnViewModelTeamClicked(Team team)
        {
            try
            {
                // Forward the event to subscribers
                TeamClicked?.Invoke(this, team);
                
                LoggingService.Instance.LogInfo($"TeamCompactCard click forwarded via MVVM: {team.TeamName}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error forwarding team clicked event", ex);
            }
        }

        private static void OnTeamChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (d is TeamCompactCard control && control._viewModel != null)
                {
                    control._viewModel.SetTeam(e.NewValue as Team);
                    
                    LoggingService.Instance.LogInfo($"Team changed in TeamCompactCard via MVVM: {(e.NewValue as Team)?.TeamName ?? "null"}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling team change in TeamCompactCard", ex);
            }
        }

        #region Mouse Event Handlers

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                // Execute ViewModel command
                _viewModel?.CardMouseEnterCommand.Execute(null);
                
                // Play hover animation
                PlayHoverAnimation(true);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling mouse enter in TeamCompactCard", ex);
            }
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                // Execute ViewModel command
                _viewModel?.CardMouseLeaveCommand.Execute(null);
                
                // Play hover animation
                PlayHoverAnimation(false);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling mouse leave in TeamCompactCard", ex);
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Execute ViewModel command
                _viewModel?.CardClickCommand.Execute(null);
                
                // Play click animation
                PlayClickAnimation();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling mouse click in TeamCompactCard", ex);
            }
        }

        #endregion

        #region Animations

        private void PlayHoverAnimation(bool isEntering)
        {
            try
            {
                if (RenderTransform is ScaleTransform scaleTransform)
                {
                    var duration = TimeSpan.FromMilliseconds(200);
                    var targetScale = isEntering ? 1.02 : 1.0;
                    
                    var animationX = new DoubleAnimation
                    {
                        To = targetScale,
                        Duration = duration,
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    
                    var animationY = new DoubleAnimation
                    {
                        To = targetScale,
                        Duration = duration,
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animationX);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animationY);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error playing hover animation", ex);
            }
        }

        private void PlayClickAnimation()
        {
            try
            {
                if (RenderTransform is ScaleTransform scaleTransform)
                {
                    var duration = TimeSpan.FromMilliseconds(100);
                    
                    // Scale down quickly
                    var scaleDownX = new DoubleAnimation
                    {
                        To = 0.98,
                        Duration = duration,
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    
                    var scaleDownY = new DoubleAnimation
                    {
                        To = 0.98,
                        Duration = duration,
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    
                    // Scale back up
                    var scaleUpX = new DoubleAnimation
                    {
                        To = 1.0,
                        Duration = duration,
                        BeginTime = duration,
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    
                    var scaleUpY = new DoubleAnimation
                    {
                        To = 1.0,
                        Duration = duration,
                        BeginTime = duration,
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleDownX);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleDownY);
                    
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUpX);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUpY);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error playing click animation", ex);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Wendet das Theme auf die Card an (für externe Theme-Updates)
        /// </summary>
        /// <param name="isDarkMode">Ist Dark-Mode aktiv?</param>
        public void ApplyTheme(bool isDarkMode)
        {
            try
            {
                _viewModel?.ApplyTheme(isDarkMode);
                LoggingService.Instance.LogInfo($"Theme applied to TeamCompactCard via MVVM: {(isDarkMode ? "Dark" : "Light")}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to TeamCompactCard", ex);
            }
        }

        /// <summary>
        /// Aktualisiert die UI mit aktuellen Team-Daten (für externe Updates)
        /// </summary>
        public void RefreshData()
        {
            try
            {
                if (_viewModel != null && Team != null)
                {
                    _viewModel.SetTeam(Team);
                    LoggingService.Instance.LogInfo($"Data refreshed in TeamCompactCard via MVVM: {Team.TeamName}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error refreshing data in TeamCompactCard", ex);
            }
        }

        #endregion

        #region Lifecycle

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Clean up ViewModel
                if (_viewModel != null)
                {
                    _viewModel.TeamClicked -= OnViewModelTeamClicked;
                    _viewModel.Dispose();
                    _viewModel = null;
                }
                
                LoggingService.Instance.LogInfo("TeamCompactCard unloaded and cleaned up via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during TeamCompactCard cleanup", ex);
            }
        }

        #endregion
    }
}
