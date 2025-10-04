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

        private SoundService()
        {
            _soundsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
            Directory.CreateDirectory(_soundsPath);
            CreateDefaultSounds();
        }

        public async Task PlayWarningSound(bool isSecondWarning)
        {
            try
            {
                string soundFile = isSecondWarning ? "warning2.wav" : "warning1.wav";
                string fullPath = Path.Combine(_soundsPath, soundFile);

                if (File.Exists(fullPath))
                {
                    await Task.Run(() =>
                    {
                        using var player = new SoundPlayer(fullPath);
                        player.PlaySync();
                    });
                }
                else
                {
                    // Fallback - simple beep pattern
                    await Task.Run(() =>
                    {
                        if (isSecondWarning)
                        {
                            // Two beeps for second warning
                            Console.Beep(800, 300);
                            System.Threading.Thread.Sleep(100);
                            Console.Beep(800, 300);
                        }
                        else
                        {
                            // One beep for first warning
                            Console.Beep(600, 400);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Sound playback failed", ex);
            }
        }

        private void CreateDefaultSounds()
        {
            // Create simple wave files if they don't exist
            // This is a simplified approach - in production you'd want actual sound files
            var warning1Path = Path.Combine(_soundsPath, "warning1.wav");
            var warning2Path = Path.Combine(_soundsPath, "warning2.wav");

            if (!File.Exists(warning1Path))
            {
                CreateSimpleWaveFile(warning1Path, 600, 400);
            }

            if (!File.Exists(warning2Path))
            {
                CreateSimpleWaveFile(warning2Path, 800, 300);
            }
        }

        private void CreateSimpleWaveFile(string filePath, int frequency, int duration)
        {
            try
            {
                // Create a simple tone - this is basic and you might want to use better sound files
                var sampleRate = 44100;
                var samples = duration * sampleRate / 1000;
                var amplitude = 0.3;

                using var fileStream = new FileStream(filePath, FileMode.Create);
                using var writer = new BinaryWriter(fileStream);

                // WAV header
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + samples * 2);
                writer.Write("WAVE".ToCharArray());
                writer.Write("fmt ".ToCharArray());
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(sampleRate);
                writer.Write(sampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write("data".ToCharArray());
                writer.Write(samples * 2);

                // Audio data
                for (int i = 0; i < samples; i++)
                {
                    var sample = (short)(amplitude * short.MaxValue * Math.Sin(2 * Math.PI * frequency * i / sampleRate));
                    writer.Write(sample);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Failed to create sound file {filePath}", ex);
            }
        }
    }
}