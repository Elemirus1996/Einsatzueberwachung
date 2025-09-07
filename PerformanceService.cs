using System;
using System.ComponentModel;

namespace Einsatzueberwachung.Services
{
    public class PerformanceService
    {
        private static PerformanceService? _instance;
        public static PerformanceService Instance => _instance ??= new PerformanceService();

        private System.Timers.Timer? _memoryCleanupTimer;
        private readonly object _lock = new object();

        private PerformanceService()
        {
            StartMemoryCleanup();
        }

        private void StartMemoryCleanup()
        {
            _memoryCleanupTimer = new System.Timers.Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
            _memoryCleanupTimer.Elapsed += (s, e) =>
            {
                lock (_lock)
                {
                    try
                    {
                        // Force garbage collection periodically to prevent memory buildup
                        GC.Collect(0, GCCollectionMode.Optimized);
                        GC.WaitForPendingFinalizers();
                        
                        LoggingService.Instance.LogInfo($"Memory cleanup completed. Working set: {GC.GetTotalMemory(false) / 1024 / 1024} MB");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.LogError("Memory cleanup error", ex);
                    }
                }
            };
            _memoryCleanupTimer.Start();
        }

        public void ForceMemoryCleanup()
        {
            lock (_lock)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        public void StopCleanup()
        {
            _memoryCleanupTimer?.Stop();
            _memoryCleanupTimer?.Dispose();
        }

        // Performance monitoring
        public void LogPerformanceMetrics()
        {
            try
            {
                var workingSet = Environment.WorkingSet;
                var managedMemory = GC.GetTotalMemory(false);
                var gen0Collections = GC.CollectionCount(0);
                var gen1Collections = GC.CollectionCount(1);
                var gen2Collections = GC.CollectionCount(2);

                LoggingService.Instance.LogInfo($"Performance Metrics - " +
                    $"Working Set: {workingSet / 1024 / 1024} MB, " +
                    $"Managed Memory: {managedMemory / 1024 / 1024} MB, " +
                    $"GC Collections: Gen0({gen0Collections}) Gen1({gen1Collections}) Gen2({gen2Collections})");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Performance metrics error", ex);
            }
        }
    }
}