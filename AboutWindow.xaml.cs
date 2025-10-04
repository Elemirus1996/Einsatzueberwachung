using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            LoadVersionInfo();
        }

        private void LoadVersionInfo()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                
                // Update version display for v1.7.0
                TxtVersion.Text = $"Version {version?.ToString(3) ?? "1.7.0"}";
                
                LoggingService.Instance?.LogInfo($"AboutWindow loaded - Version: {TxtVersion.Text}");
            }
            catch (Exception ex)
            {
                TxtVersion.Text = "Version 1.7.0";
                // Log error if logging service is available
                LoggingService.Instance?.LogError("Error loading version info in AboutWindow", ex);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
