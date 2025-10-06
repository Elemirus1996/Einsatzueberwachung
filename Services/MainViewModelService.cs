using System;

namespace Einsatzueberwachung.Services
{
    /// <summary>
    /// Singleton-Service für globalen Zugriff auf das MainViewModel
    /// Ermöglicht es anderen Fenstern, auf Teams und Einsatz-Daten zuzugreifen
    /// </summary>
    public class MainViewModelService
    {
        private static MainViewModelService? _instance;
        private static readonly object _lock = new object();
        
        public static MainViewModelService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new MainViewModelService();
                        }
                    }
                }
                return _instance;
            }
        }

        private ViewModels.MainViewModel? _mainViewModel;

        private MainViewModelService() { }

        /// <summary>
        /// Registriert das MainViewModel für globalen Zugriff
        /// </summary>
        /// <param name="mainViewModel">Das MainViewModel der Hauptanwendung</param>
        public void RegisterMainViewModel(ViewModels.MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            LoggingService.Instance.LogInfo("MainViewModel registered for global access");
        }

        /// <summary>
        /// Gibt das registrierte MainViewModel zurück
        /// </summary>
        /// <returns>Das MainViewModel oder null, falls nicht registriert</returns>
        public ViewModels.MainViewModel? GetMainViewModel()
        {
            return _mainViewModel;
        }

        /// <summary>
        /// Prüft, ob ein MainViewModel registriert ist
        /// </summary>
        /// <returns>True, wenn MainViewModel verfügbar ist</returns>
        public bool IsMainViewModelAvailable()
        {
            return _mainViewModel != null;
        }

        /// <summary>
        /// Entfernt die MainViewModel-Registrierung (für Cleanup)
        /// </summary>
        public void UnregisterMainViewModel()
        {
            _mainViewModel = null;
            LoggingService.Instance.LogInfo("MainViewModel unregistered from global access");
        }
    }
}
