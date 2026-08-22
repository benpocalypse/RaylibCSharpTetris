using System.Threading;

namespace RaylibCSharpTetris
{
    public static class TetrisMusicGenerator
    {
        private static CancellationTokenSource? _musicCts;
        private static Task? _musicTask;
        private static bool _musicPlaying = false;

        // Simple note frequencies
        private static readonly Dictionary<string, int> Notes = new()
        {
            {"C4", 262}, {"D4", 294}, {"E4", 330}, {"F4", 349},
            {"G4", 392}, {"A4", 440}, {"B4", 494}, {"C5", 523},
            {"D5", 587}, {"E5", 659}, {"F5", 698}, {"G5", 784},
            {"A5", 880}, {"B5", 988}, {"C6", 1047}
        };

        // Original melody (simplified)
        private static readonly List<(string note, int duration)> Melody1 = new()
        {
            ("E4", 400), ("B4", 400), ("C5", 400), ("D5", 400), 
            ("C5", 400), ("B4", 400), ("A4", 400), ("G4", 400),
            ("A4", 400), ("B4", 400), ("C5", 400), ("D5", 400), 
            ("C5", 400), ("B4", 400), ("A4", 400), ("G4", 400),
            ("E4", 400), ("B4", 400), ("C5", 400), ("D5", 400), 
            ("C5", 400), ("B4", 400), ("A4", 400), ("G4", 400),
            ("A4", 400), ("B4", 400), ("C5", 400), ("D5", 400), 
            ("C5", 400), ("B4", 400), ("A4", 400), ("G4", 400),
        };

        // Sound effects
        private static readonly List<(string note, int duration)> MoveSound = new() { ("C4", 100) };
        private static readonly List<(string note, int duration)> RotateSound = new() { ("E4", 100), ("G4", 100) };
        private static readonly List<(string note, int duration)> DropSound = new() { ("C4", 150), ("F4", 150), ("A4", 150) };
        private static readonly List<(string note, int duration)> ClearSound = new() { ("G4", 200), ("E5", 200), ("G5", 200), ("C6", 200) };

        public static void Initialize()
        {
            // Check if Console.Beep works on this system
            try
            {
                Console.Beep(440, 50);
            }
            catch
            {
                Console.WriteLine("Console.Beep not supported. Audio disabled.");
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
        }

        private static void PlayMelodyLoop(List<(string note, int duration)> melody, CancellationToken token)
        {
            while (!token.IsCancellationRequested && _musicPlaying)
            {
                foreach (var (note, duration) in melody)
                {
                    if (token.IsCancellationRequested || !_musicPlaying)
                        break;

                    int frequency = Notes.TryGetValue(note, out var freq) ? freq : 440;
                    PlayNote(frequency, duration);
                    Thread.Sleep(20);
                }
            }
        }

        private static void PlayNote(int frequency, int duration)
        {
            try
            {
                Console.Beep(frequency, duration);
            }
            catch
            {
                // Silently fail if beep doesn't work
            }
        }

        private static void PlaySfx(List<(string note, int duration)> pattern)
        {
            Task.Run(() =>
            {
                foreach (var (note, duration) in pattern)
                {
                    int frequency = Notes.TryGetValue(note, out var freq) ? freq : 440;
                    PlayNote(frequency, duration);
                    Thread.Sleep(20);
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
        }
    }
}
