using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;

namespace Einsatzueberwachung.Services
{
    public class TimerDiagnosticService
    {
        private static TimerDiagnosticService? _instance;
        public static TimerDiagnosticService Instance => _instance ??= new TimerDiagnosticService();

        private readonly Dictionary<string, Stopwatch> _timerPerformance = new();
        private readonly Dictionary<string, long> _averageTickTimes = new();
        private readonly Dictionary<string, int> _tickCounts = new();

        private TimerDiagnosticService() { }

        public void StartTimerDiagnostic(string timerName)
        {
            if (!_timerPerformance.ContainsKey(timerName))
            {
                _timerPerformance[timerName] = new Stopwatch();
                _averageTickTimes[timerName] = 0;
                _tickCounts[timerName] = 0;
            }
            _timerPerformance[timerName].Restart();
        }

        public void EndTimerDiagnostic(string timerName)
        {
            if (_timerPerformance.ContainsKey(timerName))
            {
                _timerPerformance[timerName].Stop();
                var elapsed = _timerPerformance[timerName].ElapsedMilliseconds;
                
                _tickCounts[timerName]++;
                _averageTickTimes[timerName] = (_averageTickTimes[timerName] + elapsed) / 2;

                // Log slow timers
                if (elapsed > 50) // More than 50ms is concerning for a timer tick
                {
                    LoggingService.Instance.LogWarning($"Slow timer tick: {timerName} took {elapsed}ms");
                }

                // Log periodic performance summary
                if (_tickCounts[timerName] % 60 == 0) // Every 60 ticks (roughly every minute)
                {
                    LoggingService.Instance.LogInfo($"Timer Performance - {timerName}: " +
                        $"Average: {_averageTickTimes[timerName]}ms, " +
                        $"Last: {elapsed}ms, " +
                        $"Ticks: {_tickCounts[timerName]}");
                }
            }
        }

        public void LogAllTimerPerformance()
        {
            foreach (var timer in _averageTickTimes)
            {
                LoggingService.Instance.LogInfo($"Timer {timer.Key}: " +
                    $"Average: {timer.Value}ms, " +
                    $"Total Ticks: {_tickCounts[timer.Key]}");
            }
        }

        public void ResetDiagnostics()
        {
            _timerPerformance.Clear();
            _averageTickTimes.Clear();
            _tickCounts.Clear();
        }
    }
}