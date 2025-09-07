using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Models
{
    public class Team : INotifyPropertyChanged
    {
        private string _teamName = string.Empty;
        private string _hundName = string.Empty;
        private string _hundefuehrer = string.Empty;
        private string _helfer = string.Empty;
        private string _notizen = string.Empty;
        private TimeSpan _elapsedTime = TimeSpan.Zero;
        private bool _isRunning = false;
        private bool _isFirstWarning = false;
        private bool _isSecondWarning = false;
        private DateTime _startTime;
        private DispatcherTimer? _timer;
        private MultipleTeamTypes _multipleTeamTypes;
        private string _cachedElapsedTimeString = "00:00:00";

        public int TeamId { get; set; }

        // Enhanced Notes System
        public ObservableCollection<NotesEntry> NotesEntries { get; } = new ObservableCollection<NotesEntry>();
        
        public string TeamName
        {
            get => _teamName;
            set { _teamName = value; OnPropertyChanged(); }
        }

        // Multiple Team Types Support
        public MultipleTeamTypes MultipleTeamTypes
        {
            get => _multipleTeamTypes;
            set 
            { 
                _multipleTeamTypes = value ?? new MultipleTeamTypes(); 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(TeamTypeDisplayName));
                OnPropertyChanged(nameof(TeamTypeColorHex));
            }
        }

        // Backward compatibility - primary type for single-type scenarios
        public TeamType TeamType
        {
            get => MultipleTeamTypes.SelectedTypes.Count > 0 
                ? MultipleTeamTypes.SelectedTypes.First() 
                : TeamType.Allgemein;
            set 
            { 
                MultipleTeamTypes = new MultipleTeamTypes(value);
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(MultipleTeamTypes));
                OnPropertyChanged(nameof(TeamTypeDisplayName));
                OnPropertyChanged(nameof(TeamTypeColorHex));
            }
        }

        // Display properties for UI binding
        public string TeamTypeDisplayName => MultipleTeamTypes.DisplayName;
        public string TeamTypeColorHex => MultipleTeamTypes.ColorHex;
        public string TeamTypeShortName => MultipleTeamTypes.ShortName;
        public string TeamTypeDescription => MultipleTeamTypes.Description;

        // Backward compatibility
        public TeamTypeInfo TeamTypeInfo => TeamTypeInfo.GetTypeInfo(TeamType);

        public string HundName
        {
            get => _hundName;
            set 
            { 
                _hundName = value; 
                OnPropertyChanged(); 
                
                // Automatically update team name when dog name changes
                if (!string.IsNullOrWhiteSpace(_hundName))
                {
                    TeamName = $"Team {_hundName}";
                }
            }
        }

        public string Hundefuehrer
        {
            get => _hundefuehrer;
            set { _hundefuehrer = value; OnPropertyChanged(); }
        }

        public string Helfer
        {
            get => _helfer;
            set { _helfer = value; OnPropertyChanged(); }
        }

        public string Notizen
        {
            get => _notizen;
            set { _notizen = value; OnPropertyChanged(); }
        }

        public TimeSpan ElapsedTime
        {
            get => _elapsedTime;
            set 
            { 
                _elapsedTime = value; 
                UpdateCachedTimeString();
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(ElapsedTimeString)); 
            }
        }

        // Cached string to avoid repeated string formatting
        public string ElapsedTimeString => _cachedElapsedTimeString;

        private void UpdateCachedTimeString()
        {
            _cachedElapsedTimeString = $"{(int)_elapsedTime.TotalHours:00}:{_elapsedTime.Minutes:00}:{_elapsedTime.Seconds:00}";
        }

        public bool IsRunning
        {
            get => _isRunning;
            set { _isRunning = value; OnPropertyChanged(); }
        }

        public bool IsFirstWarning
        {
            get => _isFirstWarning;
            set { _isFirstWarning = value; OnPropertyChanged(); }
        }

        public bool IsSecondWarning
        {
            get => _isSecondWarning;
            set { _isSecondWarning = value; OnPropertyChanged(); }
        }

        public int FirstWarningMinutes { get; set; } = 10;
        public int SecondWarningMinutes { get; set; } = 20;

        public event Action<Team, bool>? WarningTriggered;

        public Team()
        {
            _multipleTeamTypes = new MultipleTeamTypes();
        }

        // Enhanced Notes Methods
        public void AddTimestampedNote(string content)
        {
            var entry = new NotesEntry
            {
                Content = content,
                Timestamp = DateTime.Now,
                EntryType = NotesEntryType.Manual
            };
            NotesEntries.Add(entry);
        }

        private void AddAutomaticNote(NotesEntryType entryType)
        {
            var entry = new NotesEntry
            {
                Timestamp = DateTime.Now,
                EntryType = entryType
            };
            NotesEntries.Add(entry);
        }

        public void StartTimer()
        {
            if (_timer == null)
            {
                // Use Normal Priority instead of Background for responsive timers
                _timer = new DispatcherTimer(DispatcherPriority.Normal)
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _timer.Tick += Timer_Tick;
            }

            if (!IsRunning)
            {
                _startTime = DateTime.Now - ElapsedTime;
                IsRunning = true;
                _timer.Start();
                
                // Add automatic note
                AddAutomaticNote(NotesEntryType.TimerStart);
            }
        }

        public void StopTimer()
        {
            if (IsRunning)
            {
                IsRunning = false;
                _timer?.Stop();
                
                // Add automatic note
                AddAutomaticNote(NotesEntryType.TimerStop);
            }
        }

        public void ResetTimer()
        {
            StopTimer();
            ElapsedTime = TimeSpan.Zero;
            
            // Explizit Warning-Status zurücksetzen und Events auslösen
            if (IsFirstWarning)
            {
                IsFirstWarning = false;
            }
            
            if (IsSecondWarning)
            {
                IsSecondWarning = false;
            }
            
            // Add automatic note
            AddAutomaticNote(NotesEntryType.TimerReset);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!IsRunning) return;

            TimerDiagnosticService.Instance.StartTimerDiagnostic($"Team_{TeamId}");

            try
            {
                var currentTime = DateTime.Now - _startTime;
                
                // Always update elapsed time for responsive display
                ElapsedTime = currentTime;

                // Check for warnings every second to ensure accuracy
                CheckWarnings();
            }
            finally
            {
                TimerDiagnosticService.Instance.EndTimerDiagnostic($"Team_{TeamId}");
            }
        }

        private void CheckWarnings()
        {
            var totalMinutes = (int)ElapsedTime.TotalMinutes;

            // First warning
            if (!IsFirstWarning && totalMinutes >= FirstWarningMinutes)
            {
                IsFirstWarning = true;
                AddAutomaticNote(NotesEntryType.Warning1);
                WarningTriggered?.Invoke(this, false);
            }

            // Second warning
            if (!IsSecondWarning && totalMinutes >= SecondWarningMinutes)
            {
                IsSecondWarning = true;
                AddAutomaticNote(NotesEntryType.Warning2);
                WarningTriggered?.Invoke(this, true);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
