using NAudio.Wave;
using NAudio.Wave.Alsa;
using System.Threading;

namespace RaylibCSharpTetris
{
    public static class TetrisMusicGenerator
    {
        private static AlsaOut? _musicPlayer;
        private static AlsaOut? _sfxPlayer;
        private static CancellationTokenSource? _musicCts;
        private static Task? _musicTask;
        private static bool _musicPlaying = false;

        // Simple note frequencies
        private static readonly Dictionary<string, int> Notes = new()
        {
            {"G#4", 415},  // Add this
            {"A4", 440},
            {"B4", 494},
            {"C5", 523},
            {"D5", 587},
            {"E5", 659},
            {"F5", 698},
            {"G5", 784},
            {"A5", 880},
            {"B5", 988},
            {"C6", 1047}
        };
        
        // Soundalike melody inspired by the classic NES Tetris theme (Korobeiniki - public domain folk song)
        // This is an original arrangement, not a direct transcription of the NES version.
        private static readonly List<(string note, int duration)> Melody1 = new()
        {
            // Phrase 1 - the iconic opening motif
            ("E5", 4), ("B4", 2), ("C5", 2), ("D5", 4), ("C5", 2), ("B4", 2),
            ("A4", 4), ("A4", 2), ("C5", 2), ("E5", 4), ("D5", 2), ("C5", 2),
            ("B4", 6), ("C5", 2), ("D5", 4), ("E5", 4),
            ("C5", 4), ("A4", 4), ("A4", 4), ("REST", 4),

            // Phrase 2 - descending response
            ("D5", 6), ("F5", 2), ("A5", 4), ("G5", 2), ("F5", 2),
            ("E5", 6), ("C5", 2), ("E5", 4), ("D5", 2), ("C5", 2),
            ("B4", 4), ("B4", 2), ("C5", 2), ("D5", 4), ("E5", 4),
            ("C5", 4), ("A4", 4), ("A4", 4), ("REST", 4),

            // Phrase 3 - higher register variation
            ("E5", 8), ("C5", 8),
            ("D5", 8), ("B4", 8),
            ("C5", 8), ("A4", 8),
            ("G#4", 8), ("B4", 8),

            // Phrase 4 - resolution back to the opening motif
            ("E5", 4), ("B4", 2), ("C5", 2), ("D5", 4), ("C5", 2), ("B4", 2),
            ("A4", 4), ("A4", 2), ("C5", 2), ("E5", 4), ("D5", 2), ("C5", 2),
            ("B4", 6), ("C5", 2), ("D5", 4), ("E5", 4),
            ("C5", 4), ("A4", 4), ("A4", 4), ("REST", 4),
        };

        // Sound effects
        private static readonly List<(string note, int duration)> MoveSound = new() { ("C4", 100) };
        private static readonly List<(string note, int duration)> RotateSound = new() { ("E4", 100), ("G4", 100) };
        private static readonly List<(string note, int duration)> DropSound = new() { ("C4", 150), ("F4", 150), ("A4", 150) };
        private static readonly List<(string note, int duration)> ClearSound = new() { ("G4", 200), ("E5", 200), ("G5", 200), ("C6", 200) };

        public static void Initialize()
        {
            try
            {
                // AlsaOut constructor expects a string (device name) or null for default
                _musicPlayer = new AlsaOut();  // Use default ALSA device
                _sfxPlayer = new AlsaOut();   // Use default ALSA device
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Audio initialization failed: {ex.Message}");
                Console.WriteLine("Audio disabled.");
            }
        }

        public static void PlayBackgroundMusic(bool loop = true)
        {
            StopBackgroundMusic();

            _musicPlaying = true;
            _musicCts = new CancellationTokenSource();
            _musicTask = Task.Run(() => PlayMelodyLoop(Melody1, _musicCts.Token));
        }

        public static void StopBackgroundMusic()
        {
            _musicPlaying = false;
            _musicCts?.Cancel();
            _musicTask?.Wait(100);
            _musicTask = null;
            _musicPlayer?.Stop();
        }

        private static void PlayMelodyLoop(List<(string note, int duration)> melody, CancellationToken token)
        {
            while (!token.IsCancellationRequested && _musicPlaying)
            {
                foreach (var (note, duration) in melody)
                {
                    if (token.IsCancellationRequested || !_musicPlaying)
                        break;

                    byte[] wavData = GenerateWav(note, duration);
                    PlayWavData(wavData, ref _musicPlayer);
                    Thread.Sleep(duration + 50); // Reduced gap for smoother melody
                }
            }
        }

        private static void PlayWavData(byte[] wavData, ref AlsaOut? player)
        {
            try
            {
                // Dispose the old player before creating a new one
                player?.Dispose();
        
                // Create a fresh player
                player = new AlsaOut();
        
                using var stream = new MemoryStream(wavData);
                using var reader = new WaveFileReader(stream);
                player.Init(reader);
                player.Play();
        
                // Wait for playback to finish
                while (player.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Playback error: {ex.Message}");
            }
        }

        private static byte[] GenerateWav(string noteName, int durationMs)
        {
            int frequency = 0;
            if (noteName != "REST")
            {
                if (!Notes.TryGetValue(noteName, out frequency))
                {
                    frequency = 440; // Fallback
                }
            }
    
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * durationMs / 1000.0);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            WriteWavHeader(writer, sampleRate, sampleCount);

            for (int i = 0; i < sampleCount; i++)
            {
                short sample = 0;
        
                if (frequency > 0)
                {
                    double t = i / (double)sampleRate;
                    double value = Math.Sin(2 * Math.PI * frequency * t);
                    value += 0.3 * Math.Sin(2 * Math.PI * frequency * 2 * t);
                    value += 0.15 * Math.Sin(2 * Math.PI * frequency * 3 * t);
                    value = Math.Max(-1, Math.Min(1, value));
                    sample = (short)(value * 32767 * 0.3);
                }
        
                writer.Write(sample);
            }

            stream.Seek(0, SeekOrigin.Begin);
            WriteWavHeader(writer, sampleRate, sampleCount, update: true);

            return stream.ToArray();
        }

        private static void WriteWavHeader(BinaryWriter writer, int sampleRate, int sampleCount, bool update = false)
        {
            if (!update)
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + sampleCount * 2); // File size
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16); // Chunk size
                writer.Write((short)1); // Audio format (PCM)
                writer.Write((short)1); // Number of channels (mono)
                writer.Write(sampleRate);
                writer.Write(sampleRate * 2); // Byte rate
                writer.Write((short)2); // Block align
                writer.Write((short)16); // Bits per sample
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(sampleCount * 2); // Data size
            }
            else
            {
                // Update sizes
                writer.Seek(4, SeekOrigin.Begin);
                writer.Write(36 + sampleCount * 2);
                writer.Seek(40, SeekOrigin.Begin);
                writer.Write(sampleCount * 2);
            }
        }

        private static void PlaySfx(List<(string note, int duration)> pattern)
        {
            Task.Run(() =>
            {
                foreach (var (note, duration) in pattern)
                {
                    byte[] wavData = GenerateWav(note, duration);
                    PlayWavData(wavData, ref _sfxPlayer);
                    Thread.Sleep(duration + 10);
                }
            });
        }

        public static void PlayMoveSound() => PlaySfx(MoveSound);
        public static void PlayRotateSound() => PlaySfx(RotateSound);
        public static void PlayDropSound() => PlaySfx(DropSound);
        public static void PlayClearSound() => PlaySfx(ClearSound);

        public static void Dispose()
        {
            StopBackgroundMusic();
            _musicPlayer?.Dispose();
            _sfxPlayer?.Dispose();
        }
    }
}
