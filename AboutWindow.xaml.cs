using System;
using System.Reflection;
using System.Windows;

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
                var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
                var buildDate = System.IO.File.GetCreationTime(assembly.Location);

                TxtVersion.Text = $"Version {version?.ToString(3) ?? "Unknown"}";
                
                // You can also add build date
                var buildInfo = $"Build {buildDate:yyyy.MM.dd}";
                // Update build info if needed
            }
            catch (Exception ex)
            {
                TxtVersion.Text = "Version Information Unavailable";
                // Log error if logging service is available
                Services.LoggingService.Instance?.LogError("Error loading version info", ex);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}