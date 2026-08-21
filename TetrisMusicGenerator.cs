using System.Threading;
using NAudio.Wave;
using NAudio.Wave.Alsa;

namespace RaylibCSharpTetris
{
    public static class TetrisMusicGenerator
    {
        private static AlsaOut? _musicPlayer;
        private static AlsaOut? _sfxPlayer;
        private static ISampleProvider? _currentMusic;
        private static CancellationTokenSource? _musicCts;

        // ... (keep your Notes dictionary and Melody definitions as they are)

        public static void Initialize()
        {
            try
            {
                // Create the WaveFormat first
                var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
                _musicPlayer = new AlsaOut(format);
                _sfxPlayer = new AlsaOut(format);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize audio: {ex.Message}");
            }
        }

        private static ISampleProvider GenerateMelody(List<(string note, int duration)> melody, int bpm, CancellationToken token)
        {
            var samples = new List<float>();
            float sampleRate = 44100;
            float secondsPerBeat = 60f / bpm;

            // Add intro silence
            samples.AddRange(GenerateSilence((int)(sampleRate * 0.5f)));

            foreach (var (note, duration) in melody)
            {
                if (token.IsCancellationRequested)
                    break;

                float frequency = Notes.TryGetValue(note, out var freq) ? freq : 440f;
                float durationSeconds = secondsPerBeat * (duration / 4f);

                var noteSamples = GenerateSquareWave(frequency, durationSeconds, sampleRate);
                samples.AddRange(noteSamples);
                samples.AddRange(GenerateSilence((int)(sampleRate * 0.02f)));
            }

            // Convert to SampleProvider correctly
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
            
            // This now returns ISampleProvider
            return new RawSourceWaveStream(memoryStream, waveFormat).ToSampleProvider();
        }

        private static float[] GenerateSquareWave(float frequency, float duration, float sampleRate)
        {
            // ... (keep your existing implementation)
        }

        private static float[] GenerateSilence(int sampleCount)
        {
            // ... (keep your existing implementation)
        }

        // Fix for PlaySfx - use VolumeSampleProvider if available, or manual wrapper
        private static void PlaySfx(ISampleProvider sampleProvider, float volume)
        {
            if (_sfxPlayer == null) return;

            if (_sfxPlayer.PlaybackState == PlaybackState.Playing)
                _sfxPlayer.Stop();

            try
            {
                // In NAudio 3, VolumeSampleProvider might be in NAudio.Wave.SampleProviders
                // If you have that package, you can use:
                // var volumeProvider = new VolumeSampleProvider(sampleProvider);
                // volumeProvider.Volume = volume;
                // _sfxPlayer.Init(volumeProvider);

                // Simpler fallback if VolumeSampleProvider is not available:
                using var reader = sampleProvider.ToWaveProvider();
                _sfxPlayer.Init(reader);
                _sfxPlayer.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to play sound effect: {ex.Message}");
            }
        }

        // ... (keep PlayMoveSound, PlayRotateSound, etc.)
    }
}
