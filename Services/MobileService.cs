using System;

namespace Einsatzueberwachung.Services
{
    public class MobileService
    {
        private static MobileService? _instance;
        private static readonly object _lock = new object();
        private MobileIntegrationService? _mobileIntegrationService;
        private bool _isConnected = false;

        public static MobileService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new MobileService();
                        }
                    }
                }
                return _instance;
            }
        }

        private MobileService()
        {
        }

        public void Connect()
        {
            if (!_isConnected)
            {
                _mobileIntegrationService = new MobileIntegrationService();
                _isConnected = true;
                LoggingService.Instance.LogInfo("MobileService connected");
            }
        }

        public void Disconnect()
        {
            if (_isConnected && _mobileIntegrationService != null)
            {
                try
                {
                    // Stoppe Server falls aktiv
                    if (_mobileIntegrationService.IsRunning)
                    {
                        var stopTask = _mobileIntegrationService.StopServer();
                        // Warte maximal 2 Sekunden
                        if (!stopTask.Wait(TimeSpan.FromSeconds(2)))
                        {
                            LoggingService.Instance.LogWarning("MobileServer stop timeout");
                        }
                    }
                    
                    // Dispose Service
                    if (_mobileIntegrationService is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    
                    _mobileIntegrationService = null;
                    _isConnected = false;
                    
                    LoggingService.Instance.LogInfo("MobileService disconnected successfully");
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("Error disconnecting MobileService", ex);
                }
            }
        }

        public MobileIntegrationService? GetMobileIntegrationService()
        {
            return _mobileIntegrationService;
        }
    }
}
