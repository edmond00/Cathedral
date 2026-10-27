using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using OpenTK.Audio.OpenAL;

namespace Cathedral.Audio;

/// <summary>Fills one block of stereo audio, -1..1, on the stream's own thread.</summary>
internal delegate void PcmFill(float[] left, float[] right, int frames);

/// <summary>A running stream of synthesised audio to the speakers. Disposing stops it.</summary>
internal interface IPcmStream : IDisposable
{
    string Name { get; }
}

internal static class PcmStreams
{
    /// <summary>Frames per block: 16 ms at 32 kHz.</summary>
    public const int Frames = 512;

    /// <summary>Blocks the stream may hold at most: the start-up depth.</summary>
    public const int MaxBlocks = 16;

    /// <summary>
    /// How many blocks to keep queued. The game's first twenty seconds are its busiest (the world
    /// built, the language pipeline loaded, a garbage collection or two that stop every managed
    /// thread, the audio thread included), and 128 ms of queue starved there in testing while it
    /// never did afterwards. So the stream starts with 256 ms in hand and settles to 128 ms, which
    /// is what an outcome's sting is late by: about what the GS Wavetable Synth always was.
    /// </summary>
    public static int Depth(double secondsRunning) => secondsRunning < 20 ? MaxBlocks : 8;

    /// <summary>
    /// The music's stream on this backend: WinMM's waveOut on Windows (no DLL to ship, the device
    /// every Windows machine has), an OpenAL source elsewhere or with <c>--audio openal</c>. Null when
    /// there is no output, a state the engines already handle.
    /// </summary>
    public static IPcmStream? Open(int sampleRate, PcmFill fill)
    {
        IPcmStream? s = null;
        try
        {
            s = AudioOutput.Backend == AudioBackend.OpenAL
                ? OpenAlPcmStream.TryOpen(sampleRate, fill)
                : WaveOutPcmStream.TryOpen(sampleRate, fill);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[audio] music stream: {ex.GetType().Name}: {ex.Message}");
        }
        Console.WriteLine(s == null ? $"[audio] {AudioOutput.Backend}: no music output." : $"[audio] {AudioOutput.Backend}: music through {s.Name}.");
        return s;
    }

    /// <summary>Float to 16-bit with triangular dither, interleaved.</summary>
    public static void ToPcm16(float[] l, float[] r, short[] dst, int frames, ref uint seed)
    {
        for (int i = 0; i < frames; i++)
        {
            // Triangular dither, one LSB: the difference of two uniform draws.
            seed = seed * 1664525u + 1013904223u;
            float a = (seed >> 8) * (1f / 16777216f);
            seed = seed * 1664525u + 1013904223u;
            float b = (seed >> 8) * (1f / 16777216f);
            float d = (a - b) / 32768f;
            dst[2 * i] = (short)Math.Clamp((l[i] + d) * 32767f, short.MinValue, short.MaxValue);
            dst[2 * i + 1] = (short)Math.Clamp((r[i] + d) * 32767f, short.MinValue, short.MaxValue);
        }
    }
}

/// <summary>
/// What a stream did, said once when it closes: how much music it played against how long it ran,
/// how often it starved and when, and its slowest block. Underruns are the one fault this path can
/// have that nobody would otherwise report.
/// </summary>
internal sealed class StreamStats
{
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly List<double> _starvedAt = new();
    private long _frames;
    private int _starved;
    private double _slowestMs;

    public double Seconds => _clock.Elapsed.TotalSeconds;

    public long Begin() => _clock.ElapsedTicks;

    public void End(long t0, int frames)
    {
        _frames += frames;
        _slowestMs = Math.Max(_slowestMs, (_clock.ElapsedTicks - t0) * 1000.0 / Stopwatch.Frequency);
    }

    public void Starved()
    {
        _starved++;
        if (_starvedAt.Count < 8) _starvedAt.Add(Seconds);
    }

    public void Report(int rate) => Console.WriteLine(
        $"[audio] music stream: {_frames / (double)rate:F1}s played in {Seconds:F1}s, {_starved} underrun(s)" +
        (_starved > 0 ? $" (at {string.Join(", ", _starvedAt.ConvertAll(t => t.ToString("F1")))}s)" : "") +
        $", slowest block {_slowestMs:F1} ms of {PcmStreams.Frames * 1000.0 / rate:F1}.");
}

/// <summary>The music through WinMM: a queue of waveOut buffers, refilled by a thread as each one finishes.</summary>
internal sealed class WaveOutPcmStream : IPcmStream
{
    [DllImport("winmm.dll")] private static extern uint waveOutOpen(out nint hwo, uint devId, nint fmt, nint cb, nint inst, uint flags);
    [DllImport("winmm.dll")] private static extern uint waveOutPrepareHeader(nint hwo, nint hdr, uint sz);
    [DllImport("winmm.dll")] private static extern uint waveOutUnprepareHeader(nint hwo, nint hdr, uint sz);
    [DllImport("winmm.dll")] private static extern uint waveOutWrite(nint hwo, nint hdr, uint sz);
    [DllImport("winmm.dll")] private static extern uint waveOutReset(nint hwo);
    [DllImport("winmm.dll")] private static extern uint waveOutClose(nint hwo);

    private const uint WAVE_MAPPER = 0xFFFF_FFFFu;
    private const uint WHDR_DONE = 0x00000001;
    // WAVEHDR on x64 (PlatformTarget = x64), as BrownNoiseStreamer uses it.
    private const int OFF_lpData = 0, OFF_dwBufferLength = 8, OFF_dwFlags = 24, WAVEHDR_SIZE = 48;
    private const int Blocks = PcmStreams.MaxBlocks, Frames = PcmStreams.Frames;

    private readonly int _rate;
    private readonly PcmFill _fill;
    private readonly nint _fmt;
    private readonly nint[] _hdr = new nint[Blocks], _pcm = new nint[Blocks];
    private nint _hwo;
    private readonly Thread _thread;
    private volatile bool _running = true;
    private readonly StreamStats _stats = new();

    public string Name => "waveOut (WinMM)";

    private WaveOutPcmStream(int rate, PcmFill fill, nint hwo, nint fmt)
    {
        _rate = rate;
        _fill = fill;
        _hwo = hwo;
        _fmt = fmt;
        for (int i = 0; i < Blocks; i++)
        {
            _pcm[i] = Marshal.AllocHGlobal(Frames * 4);
            _hdr[i] = Marshal.AllocHGlobal(WAVEHDR_SIZE);
            for (int b = 0; b < WAVEHDR_SIZE; b++) Marshal.WriteByte(_hdr[i], b, 0);
            Marshal.WriteIntPtr(_hdr[i], OFF_lpData, _pcm[i]);
            Marshal.WriteInt32(_hdr[i], OFF_dwBufferLength, Frames * 4);
        }
        _thread = new Thread(Loop) { IsBackground = true, Name = "MusicWaveOut", Priority = ThreadPriority.AboveNormal };
        _thread.Start();
    }

    public static WaveOutPcmStream? TryOpen(int rate, PcmFill fill)
    {
        if (!OperatingSystem.IsWindows()) return null;
        nint fmt = Marshal.AllocHGlobal(18);
        for (int b = 0; b < 18; b++) Marshal.WriteByte(fmt, b, 0);
        Marshal.WriteInt16(fmt, 0, 1);            // PCM
        Marshal.WriteInt16(fmt, 2, 2);            // stereo
        Marshal.WriteInt32(fmt, 4, rate);
        Marshal.WriteInt32(fmt, 8, rate * 4);
        Marshal.WriteInt16(fmt, 12, 4);
        Marshal.WriteInt16(fmt, 14, 16);
        if (waveOutOpen(out nint hwo, WAVE_MAPPER, fmt, 0, 0, 0) != 0)
        {
            Marshal.FreeHGlobal(fmt);
            return null;
        }
        return new WaveOutPcmStream(rate, fill, hwo, fmt);
    }

    private void Loop()
    {
        var l = new float[Frames];
        var r = new float[Frames];
        var pcm = new short[Frames * 2];
        uint seed = 22222;
        var queued = new Queue<int>();
        var idle = new Stack<int>();
        for (int i = Blocks - 1; i >= 0; i--) idle.Push(i);
        try
        {
            while (_running)
            {
                // Collect what has played; if everything had, the device ran dry.
                bool any = false;
                while (queued.Count > 0 && ((uint)Marshal.ReadInt32(_hdr[queued.Peek()], OFF_dwFlags) & WHDR_DONE) != 0)
                {
                    int i = queued.Dequeue();
                    waveOutUnprepareHeader(_hwo, _hdr[i], WAVEHDR_SIZE);
                    idle.Push(i);
                    any = true;
                }
                if (any && queued.Count == 0) _stats.Starved();
                // Top the queue up to the depth for this moment.
                while (queued.Count < PcmStreams.Depth(_stats.Seconds) && idle.Count > 0 && _running)
                {
                    int i = idle.Pop();
                    long t0 = _stats.Begin();
                    _fill(l, r, Frames);
                    PcmStreams.ToPcm16(l, r, pcm, Frames, ref seed);
                    Marshal.Copy(pcm, 0, _pcm[i], pcm.Length);
                    Marshal.WriteInt32(_hdr[i], OFF_dwFlags, 0);
                    waveOutPrepareHeader(_hwo, _hdr[i], WAVEHDR_SIZE);
                    waveOutWrite(_hwo, _hdr[i], WAVEHDR_SIZE);
                    queued.Enqueue(i);
                    _stats.End(t0, Frames);
                }
                Thread.Sleep(2);
            }
        }
        catch (Exception ex)
        {
            if (_running) Console.Error.WriteLine($"[audio] waveOut music thread: {ex.GetType().Name}: {ex.Message}");
        }
        _stats.Report(_rate);
    }

    public void Dispose()
    {
        if (!_running) return;
        _running = false;
        _thread.Join(500);
        try
        {
            waveOutReset(_hwo);
            foreach (var h in _hdr) waveOutUnprepareHeader(_hwo, h, WAVEHDR_SIZE);
            waveOutClose(_hwo);
        }
        catch { /* shutting down */ }
        _hwo = 0;
        foreach (var h in _hdr) Marshal.FreeHGlobal(h);
        foreach (var p in _pcm) Marshal.FreeHGlobal(p);
        Marshal.FreeHGlobal(_fmt);
    }
}

/// <summary>The music through one streaming OpenAL source: a queue of buffers, refilled as each plays out.</summary>
internal sealed class OpenAlPcmStream : IPcmStream
{
    private const int Frames = PcmStreams.Frames;
    private readonly int _rate;
    private readonly PcmFill _fill;
    private readonly int _source;
    private readonly int[] _buffers;
    private readonly Thread _thread;
    private volatile bool _running = true;
    private readonly StreamStats _stats = new();

    public string Name => "OpenAL stream";

    private OpenAlPcmStream(int rate, PcmFill fill)
    {
        _rate = rate;
        _fill = fill;
        _source = AL.GenSource();
        _buffers = AL.GenBuffers(PcmStreams.MaxBlocks);
        _thread = new Thread(Loop) { IsBackground = true, Name = "MusicOpenAL", Priority = ThreadPriority.AboveNormal };
        _thread.Start();
    }

    public static OpenAlPcmStream? TryOpen(int rate, PcmFill fill) =>
        OpenAlAudio.EnsureOpen() ? new OpenAlPcmStream(rate, fill) : null;

    private void Loop()
    {
        var l = new float[Frames];
        var r = new float[Frames];
        var pcm = new short[Frames * 2];
        uint seed = 11111;
        var idle = new Stack<int>(_buffers);
        int queued = 0;
        try
        {
            while (_running)
            {
                AL.GetSource(_source, ALGetSourcei.BuffersProcessed, out int processed);
                while (processed-- > 0)
                {
                    idle.Push(AL.SourceUnqueueBuffer(_source));
                    queued--;
                }
                while (queued < PcmStreams.Depth(_stats.Seconds) && idle.Count > 0 && _running)
                {
                    int buffer = idle.Pop();
                    long t0 = _stats.Begin();
                    _fill(l, r, Frames);
                    PcmStreams.ToPcm16(l, r, pcm, Frames, ref seed);
                    AL.BufferData(buffer, ALFormat.Stereo16, pcm, _rate);
                    AL.SourceQueueBuffer(_source, buffer);
                    queued++;
                    _stats.End(t0, Frames);
                }
                // A source that ran dry stops; start it again rather than fall silent for good.
                AL.GetSource(_source, ALGetSourcei.SourceState, out int state);
                if ((ALSourceState)state != ALSourceState.Playing)
                {
                    if (_stats.Seconds > 0.5) _stats.Starved();
                    AL.SourcePlay(_source);
                }
                Thread.Sleep(3);
            }
        }
        catch (Exception ex)
        {
            if (_running) Console.Error.WriteLine($"[audio] OpenAL music thread: {ex.GetType().Name}: {ex.Message}");
        }
        _stats.Report(_rate);
    }

    public void Dispose()
    {
        if (!_running) return;
        _running = false;
        _thread.Join(500);
        try
        {
            AL.SourceStop(_source);
            AL.DeleteSource(_source);
            AL.DeleteBuffers(_buffers);
        }
        catch { /* context already gone at shutdown */ }
    }
}
