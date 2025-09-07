using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace Einsatzueberwachung.Services
{
    public class ThemeService : INotifyPropertyChanged
    {
        private static ThemeService? _instance;
        private bool _isDarkMode;
        private bool _isAutoMode = true;
        private DispatcherTimer? _timeCheckTimer;

        public static ThemeService Instance => _instance ??= new ThemeService();

        private ThemeService()
        {
            CheckAutoTheme();
            StartTimeCheckTimer();
        }

        public bool IsDarkMode
        {
            get => _isDarkMode;
            private set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    OnPropertyChanged();
                    ThemeChanged?.Invoke(value);
                }
            }
        }

        public bool IsAutoMode
        {
            get => _isAutoMode;
            set
            {
                _isAutoMode = value;
                OnPropertyChanged();
                if (value)
                {
                    CheckAutoTheme();
                    StartTimeCheckTimer();
                }
                else
                {
                    StopTimeCheckTimer();
                }
            }
        }

        public event Action<bool>? ThemeChanged;

        public void SetDarkMode(bool isDark)
        {
            if (!IsAutoMode)
            {
                IsDarkMode = isDark;
            }
        }

        public void ToggleTheme()
        {
            if (!IsAutoMode)
            {
                IsDarkMode = !IsDarkMode;
            }
        }

        private void CheckAutoTheme()
        {
            if (!IsAutoMode) return;

            var now = DateTime.Now.TimeOfDay;
            var darkStart = new TimeSpan(18, 0, 0); // 18:00
            var darkEnd = new TimeSpan(7, 0, 0);    // 07:00

            bool shouldBeDark = now >= darkStart || now < darkEnd;
            IsDarkMode = shouldBeDark;
        }

        private void StartTimeCheckTimer()
        {
            if (_timeCheckTimer == null)
            {
                _timeCheckTimer = new DispatcherTimer();
                _timeCheckTimer.Interval = TimeSpan.FromMinutes(1); // Check every minute
                _timeCheckTimer.Tick += (s, e) => CheckAutoTheme();
            }
            _timeCheckTimer.Start();
        }

        private void StopTimeCheckTimer()
        {
            _timeCheckTimer?.Stop();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}