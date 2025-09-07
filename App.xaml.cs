using System.Configuration;
using System.Data;
using System.Windows;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Initialize theme system early
                var themeService = ThemeService.Instance;
                
                // Log startup
                LoggingService.Instance.LogInfo("Einsatzüberwachung Professional v1.5 starting up...");
                
                base.OnStartup(e);
            }
            catch (System.Exception ex)
            {
                LoggingService.Instance.LogError("Error during application startup", ex);
                base.OnStartup(e);
            }
        }
    }
}
