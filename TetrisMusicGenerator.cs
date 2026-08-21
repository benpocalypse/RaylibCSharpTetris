using NAudio.Wave;
//using NAudio.Alsa;
using NAudio.Wave.Alsa; // Correct
// NOT: using NAudio.Alsa;

namespace RaylibCSharpTetris
{
    public static class TetrisMusicGenerator
    {
        private static AlsaOut? _musicPlayer;
        private static AlsaOut? _sfxPlayer;
        private static ISampleProvider? _currentMusic;
        private static CancellationTokenSource? _musicCts;

        // Simple chiptune-style notes
        private static readonly Dictionary<string, float> Notes = new()
        {
            {"C4", 261.63f}, {"C#4", 277.18f}, {"D4", 293.66f}, {"D#4", 311.13f},
            {"E4", 329.63f}, {"F4", 349.23f}, {"F#4", 369.99f}, {"G4", 392.00f},
            {"G#4", 415.30f}, {"A4", 440.00f}, {"A#4", 466.16f}, {"B4", 493.88f},
            {"C5", 523.25f}, {"C#5", 554.37f}, {"D5", 587.33f}, {"D#5", 622.25f},
            {"E5", 659.25f}, {"F5", 698.46f}, {"F#5", 739.99f}, {"G5", 783.99f},
            {"G#5", 830.61f}, {"A5", 880.00f}, {"A#5", 932.33f}, {"B5", 987.77f}
        };

        // Original melody
        private static readonly List<(string note, int duration)> Melody1 = new()
        {
            ("E4", 4), ("B4", 4), ("C5", 4), ("D5", 4), ("C5", 4), ("B4", 4), ("A4", 4), ("G4", 4),
            ("A4", 4), ("B4", 4), ("C5", 4), ("D5", 4), ("C5", 4), ("B4", 4), ("A4", 4), ("G4", 4),
            ("E4", 4), ("B4", 4), ("C5", 4), ("D5", 4), ("C5", 4), ("B4", 4), ("A4", 4), ("G4", 4),
            ("A4", 4), ("B4", 4), ("C5", 4), ("D5", 4), ("C5", 4), ("B4", 4), ("A4", 4), ("G4", 4),
            ("E4", 2), ("G4", 2), ("E4", 2), ("F4", 2), ("G4", 2), ("A4", 2), ("B4", 2), ("C5", 2),
            ("D5", 2), ("C5", 2), ("B4", 2), ("A4", 2), ("G4", 2), ("F4", 2), ("E4", 2), ("D4", 2),
        };

        // Alternative melody
        private static readonly List<(string note, int duration)> Melody2 = new()
        {
            ("G4", 4), ("A4", 4), ("B4", 4), ("C5", 4), ("B4", 4), ("A4", 4), ("G4", 4), ("F4", 4),
            ("G4", 4), ("A4", 4), ("B4", 4), ("C5", 4), ("B4", 4), ("A4", 4), ("G4", 4), ("F4", 4),
            ("E4", 4), ("F4", 4), ("G4", 4), ("A4", 4), ("B4", 4), ("C5", 4), ("D5", 4), ("E5", 4),
            ("C5", 2), ("D5", 2), ("E5", 2), ("F5", 2), ("G5", 2), ("F5", 2), ("E5", 2), ("D5", 2),
        };

        // Sound effects
        private static readonly List<(string note, int duration)> MoveSound = new() { ("C4", 8) };
        private static readonly List<(string note, int duration)> RotateSound = new() { ("E4", 8), ("G4", 8) };
        private static readonly List<(string note, int duration)> DropSound = new() { ("C4", 6), ("F4", 6), ("A4", 6) };
        private static readonly List<(string note, int duration)> ClearSound = new() { ("G4", 4), ("E5", 4), ("G5", 4), ("C6", 8) };

        public static void Initialize()
        {
            try
            {
                // Initialize ALSA players with standard format
                var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
                _musicPlayer = new AlsaOut(format);
                _sfxPlayer = new AlsaOut(format);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize audio: {ex.Message}");
            }
        }

        public static void PlayBackgroundMusic(bool loop = true)
        {
            StopBackgroundMusic();

            _musicCts = new CancellationTokenSource();
            var melody = Melody1; // Use Melody2 for variation

            var sampleProvider = GenerateMelody(melody, 120, _musicCts.Token);
            _currentMusic = sampleProvider;

            if (_musicPlayer != null)
            {
                _musicPlayer.Init(sampleProvider);
                _musicPlayer.Play();
            }
        }

        public static void StopBackgroundMusic()
        {
            _musicCts?.Cancel();
            _musicPlayer?.Stop();
            _currentMusic = null;
        }

        private static ISampleProvider GenerateMelody(List<(string note, int duration)> melody, int bpm, CancellationToken token)
        {
            var samples = new List<float>();
            float sampleRate = 44100;
            float secondsPerBeat = 60f / bpm;

            // Add short intro
            samples.AddRange(GenerateSilence((int)(sampleRate * 0.5f)));

            foreach (var (note, duration) in melody)
            {
                if (token.IsCancellationRequested)
                    break;

                float frequency = Notes.TryGetValue(note, out var freq) ? freq : 440f;
                float durationSeconds = secondsPerBeat * (duration / 4f);

                var noteSamples = GenerateSquareWave(frequency, durationSeconds, sampleRate);
                samples.AddRange(noteSamples);

                // Small gap between notes
                samples.AddRange(GenerateSilence((int)(sampleRate * 0.02f)));
            }

            // Convert to SampleProvider
            var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat((int)sampleRate, 1);
            var buffer = samples.ToArray();
            var memoryStream = new MemoryStream();
            using (var writer = new BinaryWriter(memoryStream))
            {
                foreach (var sample in buffer)
                {
                    writer.Write(sample);
                }
            }
            memoryStream.Position = 0;
            return new RawSourceWaveStream(memoryStream, waveFormat);
        }

        private static float[] GenerateSquareWave(float frequency, float duration, float sampleRate)
        {
            int sampleCount = (int)(sampleRate * duration);
            var samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / sampleRate;
                float value = Math.Sign(Math.Sin(2 * Math.PI * frequency * t));
                
                // Add harmonics for chiptune feel
                float harmonic = 0.3f * (float)Math.Sign(Math.Sin(2 * Math.PI * frequency * 2 * t));
                harmonic += 0.15f * (float)Math.Sign(Math.Sin(2 * Math.PI * frequency * 3 * t));
                
                value = (float)(value * 0.5 + harmonic * 0.5);
                
                // Simple envelope
                float envelope = 1.0f;
                if (i < sampleRate * 0.02f)
                    envelope = i / (sampleRate * 0.02f);
                if (i > sampleCount - sampleRate * 0.05f)
                    envelope = (sampleCount - i) / (sampleRate * 0.05f);
                
                samples[i] = value * envelope * 0.3f;
            }

            return samples;
        }

        private static float[] GenerateSilence(int sampleCount)
        {
            return new float[sampleCount];
        }

        // Sound Effects
        public static void PlayMoveSound()
        {
            PlaySfx(GenerateMelody(MoveSound, 240, CancellationToken.None), 0.5f);
        }

        public static void PlayRotateSound()
        {
            PlaySfx(GenerateMelody(RotateSound, 240, CancellationToken.None), 0.6f);
        }

        public static void PlayDropSound()
        {
            PlaySfx(GenerateMelody(DropSound, 180, CancellationToken.None), 0.7f);
        }

        public static void PlayClearSound()
        {
            PlaySfx(GenerateMelody(ClearSound, 160, CancellationToken.None), 0.8f);
        }

        private static void PlaySfx(ISampleProvider sampleProvider, float volume)
        {
            if (_sfxPlayer == null) return;

            if (_sfxPlayer.PlaybackState == PlaybackState.Playing)
                _sfxPlayer.Stop();

            // Apply volume manually since VolumeSampleProvider may not be available
            var volumeProvider = new VolumeSampleProvider(sampleProvider);
            volumeProvider.Volume = volume;

            _sfxPlayer.Init(volumeProvider);
            _sfxPlayer.Play();
        }

        public static void Dispose()
        {
            StopBackgroundMusic();
            _musicPlayer?.Dispose();
            _sfxPlayer?.Dispose();
        }
    }
}

