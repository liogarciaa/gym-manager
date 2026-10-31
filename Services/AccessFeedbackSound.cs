using System.IO;
using System.Media;
using System.Windows.Threading;

namespace GymManager.Services;

public static class AccessFeedbackSound
{
    private const int SampleRate = 44100;
    private const double Frequency = 2400;
    private const short Amplitude = 11000;
    private static SoundPlayer? _player;
    private static MemoryStream? _stream;
    private static DispatcherTimer? _cleanupTimer;

    public static void PlayAllowed()
    {
        Play([new ToneSegment(550, true)]);
    }

    public static void PlayDenied()
    {
        Play([
            new ToneSegment(110, true),
            new ToneSegment(100, false),
            new ToneSegment(110, true),
            new ToneSegment(100, false),
            new ToneSegment(110, true)
        ]);
    }

    private static void Play(ToneSegment[] segments)
    {
        Stop();
        _stream = new MemoryStream(CreateWave(segments));
        _player = new SoundPlayer(_stream);
        _player.Play();

        _cleanupTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(segments.Sum(segment => segment.DurationMs) + 100)
        };
        _cleanupTimer.Tick += CleanupTimer_Tick;
        _cleanupTimer.Start();
    }

    private static byte[] CreateWave(ToneSegment[] segments)
    {
        var samples = new List<short>();
        double phase = 0;

        foreach (ToneSegment segment in segments)
        {
            int sampleCount = SampleRate * segment.DurationMs / 1000;
            int fadeSamples = Math.Min(SampleRate / 200, sampleCount / 2);
            for (int index = 0; index < sampleCount; index++)
            {
                short sample = 0;
                if (segment.Enabled)
                {
                    double envelope = fadeSamples == 0
                        ? 1
                        : Math.Min(1, Math.Min(index + 1, sampleCount - index) / (double)fadeSamples);
                    sample = (short)((phase < 0.5 ? Amplitude : -Amplitude) * envelope);
                    phase += Frequency / SampleRate;
                    if (phase >= 1)
                    {
                        phase -= 1;
                    }
                }

                samples.Add(sample);
            }
        }

        int dataLength = samples.Count * sizeof(short);
        using var stream = new MemoryStream(44 + dataLength);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + dataLength);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(SampleRate);
        writer.Write(SampleRate * sizeof(short));
        writer.Write((short)sizeof(short));
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(dataLength);
        foreach (short sample in samples)
        {
            writer.Write(sample);
        }

        return stream.ToArray();
    }

    private static void CleanupTimer_Tick(object? sender, EventArgs e)
    {
        Stop();
    }

    private static void Stop()
    {
        if (_cleanupTimer != null)
        {
            _cleanupTimer.Stop();
            _cleanupTimer.Tick -= CleanupTimer_Tick;
            _cleanupTimer = null;
        }

        _player?.Stop();
        _player?.Dispose();
        _player = null;
        _stream?.Dispose();
        _stream = null;
    }

    private readonly record struct ToneSegment(int DurationMs, bool Enabled);
}