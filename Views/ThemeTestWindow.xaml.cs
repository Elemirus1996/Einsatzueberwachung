using System;
using System.Windows;
using System.Windows.Controls;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// Theme-Test-Fenster zur Überprüfung der Light/Dark Modi
    /// </summary>
    public partial class ThemeTestWindow : Window
    {
        public ThemeTestWindow()
        {
            InitializeComponent();
            
            // Subscribe to theme changes
            UnifiedThemeManager.Instance.ThemeChanged += OnThemeChanged;
            
            // Update initial status
            UpdateThemeStatus();
        }

        private void OnThemeChanged(bool isDarkMode)
        {
            UpdateThemeStatus();
        }

        private void UpdateThemeStatus()
        {
            var themeManager = UnifiedThemeManager.Instance;
            
            if (StatusText != null)
            {
                StatusText.Text = $"Aktuelles Theme: {themeManager.CurrentThemeStatus}";
            }
            
            if (ModeText != null)
            {
                ModeText.Text = themeManager.IsDarkMode ? "🌙 Dark Mode" : "☀️ Light Mode";
            }
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            UnifiedThemeManager.Instance.ToggleTheme();
        }

        private void SetLightMode_Click(object sender, RoutedEventArgs e)
        {
            UnifiedThemeManager.Instance.SetDarkMode(false);
        }

        private void SetDarkMode_Click(object sender, RoutedEventArgs e)
        {
            UnifiedThemeManager.Instance.SetDarkMode(true);
        }

        private void EnableAutoMode_Click(object sender, RoutedEventArgs e)
        {
            UnifiedThemeManager.Instance.EnableAutoMode();
        }

        protected override void OnClosed(EventArgs e)
        {
            UnifiedThemeManager.Instance.ThemeChanged -= OnThemeChanged;
            base.OnClosed(e);
        }
    }
}
