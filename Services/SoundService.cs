using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;

namespace Einsatzueberwachung.Services
{
    public class SoundService
    {
        private static SoundService? _instance;
        public static SoundService Instance => _instance ??= new SoundService();

        private readonly string _soundsPath;
        private bool _isEnabled = true;

        private SoundService()
        {
            _soundsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
            Directory.CreateDirectory(_soundsPath);
            CreateDefaultSounds();
            
            LoggingService.Instance.LogInfo($"SoundService initialized. Sounds path: {_soundsPath}");
        }

        /// <summary>
        /// Aktiviert oder deaktiviert Sound-Alarme
        /// </summary>
        public bool IsEnabled 
        { 
            get => _isEnabled;
            set 
            {
                _isEnabled = value;
                LoggingService.Instance.LogInfo($"SoundService {(value ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Spielt den entsprechenden Warnsound ab
        /// </summary>
        public async Task PlayWarningSound(bool isSecondWarning)
        {
            if (!_isEnabled)
            {
                LoggingService.Instance.LogInfo("SoundService: Sound playback skipped (disabled)");
                return;
            }

            try
            {
                LoggingService.Instance.LogInfo($"SoundService: Playing {(isSecondWarning ? "second" : "first")} warning sound");

                string soundFile = isSecondWarning ? "warning2.wav" : "warning1.wav";
                string fullPath = Path.Combine(_soundsPath, soundFile);

                if (File.Exists(fullPath))
                {
                    await PlaySoundFile(fullPath);
                    LoggingService.Instance.LogInfo($"SoundService: Successfully played sound file: {soundFile}");
                }
                else
                {
                    // Fallback - System beeps mit unterschiedlichen Tönen
                    await PlaySystemBeeps(isSecondWarning);
                    LoggingService.Instance.LogInfo($"SoundService: Used system beeps as fallback for {(isSecondWarning ? "second" : "first")} warning");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("SoundService: Error during sound playback", ex);
                
                // Notfall-Fallback: Einfacher Systemsound
                try
                {
                    await Task.Run(() => SystemSounds.Exclamation.Play());
                }
                catch (Exception fallbackEx)
                {
                    LoggingService.Instance.LogError("SoundService: Even fallback sound failed", fallbackEx);
                }
            }
        }

        /// <summary>
        /// Spielt eine Testmeldung für Sound-Settings
        /// </summary>
        public async Task PlayTestSound(bool isSecondWarning = false)
        {
            try
            {
                LoggingService.Instance.LogInfo("SoundService: Playing test sound");
                await PlayWarningSound(isSecondWarning);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("SoundService: Error during test sound", ex);
            }
        }

        /// <summary>
        /// Spielt eine Sound-Datei ab
        /// </summary>
        private async Task PlaySoundFile(string filePath)
        {
            await Task.Run(() =>
            {
                try
                {
                    using var player = new SoundPlayer(filePath);
                    player.LoadTimeout = 3000; // 3 Sekunden Timeout
                    player.Load();
                    player.PlaySync();
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError($"SoundService: Error playing sound file {filePath}", ex);
                    throw;
                }
            });
        }

        /// <summary>
        /// Spielt System-Beeps als Fallback
        /// </summary>
        private async Task PlaySystemBeeps(bool isSecondWarning)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (isSecondWarning)
                    {
                        // Zwei kurze, hohe Beeps für zweite Warnung (kritisch)
                        Console.Beep(1000, 300);
                        System.Threading.Thread.Sleep(150);
                        Console.Beep(1000, 300);
                        System.Threading.Thread.Sleep(150);
                        Console.Beep(1000, 300); // Drei Beeps für Dringlichkeit
                    }
                    else
                    {
                        // Ein längerer, mittlerer Beep für erste Warnung
                        Console.Beep(750, 500);
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("SoundService: Error playing system beeps", ex);
                    throw;
                }
            });
        }

        /// <summary>
        /// Erstellt Default-Sound-Dateien falls nicht vorhanden
        /// </summary>
        private void CreateDefaultSounds()
        {
            try
            {
                var warning1Path = Path.Combine(_soundsPath, "warning1.wav");
                var warning2Path = Path.Combine(_soundsPath, "warning2.wav");

                if (!File.Exists(warning1Path))
                {
                    CreateSimpleWaveFile(warning1Path, 750, 500); // Erste Warnung: 750Hz, 500ms
                    LoggingService.Instance.LogInfo("Created default warning1.wav");
                }

                if (!File.Exists(warning2Path))
                {
                    CreateSimpleWaveFile(warning2Path, 1000, 300); // Zweite Warnung: 1000Hz, 300ms (wird 3x gespielt)
                    LoggingService.Instance.LogInfo("Created default warning2.wav");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("SoundService: Error creating default sounds", ex);
            }
        }

        /// <summary>
        /// Erstellt eine einfache WAV-Datei mit einem Sinuston
        /// </summary>
        private void CreateSimpleWaveFile(string filePath, int frequency, int durationMs)
        {
            try
            {
                var sampleRate = 44100;
                var samples = durationMs * sampleRate / 1000;
                var amplitude = 0.3; // 30% der maximalen Amplitude für angenehme Lautstärke

                using var fileStream = new FileStream(filePath, FileMode.Create);
                using var writer = new BinaryWriter(fileStream);

                // WAV-Header schreiben
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + samples * 2);
                writer.Write("WAVE".ToCharArray());
                writer.Write("fmt ".ToCharArray());
                writer.Write(16);
                writer.Write((short)1); // PCM
                writer.Write((short)1); // Mono
                writer.Write(sampleRate);
                writer.Write(sampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write("data".ToCharArray());
                writer.Write(samples * 2);

                // Audio-Daten schreiben (Sinuston)
                for (int i = 0; i < samples; i++)
                {
                    // Sinuston mit fade-in und fade-out für weicheren Klang
                    var fadeMultiplier = 1.0;
                    if (i < samples * 0.1) // Fade-in erste 10%
                    {
                        fadeMultiplier = (double)i / (samples * 0.1);
                    }
                    else if (i > samples * 0.9) // Fade-out letzte 10%
                    {
                        fadeMultiplier = 1.0 - ((double)(i - samples * 0.9) / (samples * 0.1));
                    }

                    var sample = (short)(amplitude * short.MaxValue * Math.Sin(2 * Math.PI * frequency * i / sampleRate) * fadeMultiplier);
                    writer.Write(sample);
                }

                LoggingService.Instance.LogInfo($"Created sound file: {filePath} ({frequency}Hz, {durationMs}ms)");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"SoundService: Failed to create sound file {filePath}", ex);
            }
        }

        /// <summary>
        /// Gibt Informationen über verfügbare Sounds zurück
        /// </summary>
        public (bool hasCustomSounds, int soundCount, string soundsPath) GetSoundInfo()
        {
            try
            {
                var customSounds = Directory.GetFiles(_soundsPath, "*.wav").Length;
                return (customSounds > 0, customSounds, _soundsPath);
            }
            catch
            {
                return (false, 0, _soundsPath);
            }
        }

        /// <summary>
        /// Testet ob Sound-System funktioniert
        /// </summary>
        public async Task<bool> TestSoundSystem()
        {
            try
            {
                LoggingService.Instance.LogInfo("Testing sound system...");
                
                // Test system beep
                await Task.Run(() => Console.Beep(800, 200));
                
                // Test sound files if available
                var warning1Path = Path.Combine(_soundsPath, "warning1.wav");
                if (File.Exists(warning1Path))
                {
                    await PlaySoundFile(warning1Path);
                }
                
                LoggingService.Instance.LogInfo("Sound system test completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Sound system test failed", ex);
                return false;
            }
        }
    }
}
