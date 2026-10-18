using System;
using System.Threading;
using MeltySynth;
using Melanchall.DryWetMidi.Core;
using OpenTK.Audio.OpenAL;

namespace Cathedral.Audio;

/// <summary>
/// The one OpenAL device and context this process uses, opened on first need. Every OpenAL output
/// (ticks, wash, music) is a source in it, so they mix in OpenAL Soft rather than competing for a
/// device the way WinMM's single PlaySound channel does.
///
/// <para>Opening can fail three ways — no OpenAL Soft library (<c>libopenal.so.1</c>, or
/// <c>openal32.dll</c> beside the executable on Windows), no output device, no context — and each is
/// the same answer as a machine with no sound card: a log line, and silence.</para>
/// </summary>
internal static class OpenAlAudio
{
    private static readonly object Gate = new();
    private static ALDevice _device;
    private static ALContext _context;
    private static bool _tried, _ok;

    public static bool EnsureOpen()
    {
        lock (Gate)
        {
            if (_tried) return _ok;
            _tried = true;
            try
            {
                _device = ALC.OpenDevice(null);
                if (_device == ALDevice.Null)
                {
                    Console.Error.WriteLine("[audio] OpenAL: no output device.");
                    return false;
                }
                _context = ALC.CreateContext(_device, new ALContextAttributes());
                if (_context == ALContext.Null || !ALC.MakeContextCurrent(_context))
                {
                    Console.Error.WriteLine("[audio] OpenAL: could not create a context.");
                    ALC.CloseDevice(_device);
                    return false;
                }
                _ok = true;
                Console.WriteLine($"[audio] OpenAL: {ALC.GetString(_device, AlcGetString.DeviceSpecifier)}");
                AppDomain.CurrentDomain.ProcessExit += (_, _) => Close();
            }
            catch (DllNotFoundException)
            {
                Console.Error.WriteLine(OperatingSystem.IsWindows()
                    ? "[audio] OpenAL: openal32.dll not found. --audio openal needs OpenAL Soft's soft_oal.dll, renamed openal32.dll, beside the game."
                    : "[audio] OpenAL: libopenal.so.1 not found (install OpenAL Soft: libopenal1).");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[audio] OpenAL: {ex.GetType().Name}: {ex.Message}");
            }
            return _ok;
        }
    }

    private static void Close()
    {
        lock (Gate)
        {
            if (!_ok) return;
            _ok = false;
            try
            {
                ALC.MakeContextCurrent(ALContext.Null);
                ALC.DestroyContext(_context);
                ALC.CloseDevice(_device);
            }
            catch { /* shutting down */ }
        }
    }

    /// <summary>A mono 16-bit buffer holding <paramref name="samples"/>.</summary>
    public static int MonoBuffer(short[] samples, int sampleRate)
    {
        int buffer = AL.GenBuffer();
        AL.BufferData(buffer, ALFormat.Mono16, samples, sampleRate);
        return buffer;
    }
}

/// <summary>The hover and click ticks through OpenAL: the very samples WinMM plays.</summary>
internal sealed class OpenAlUiSfx : IUiSfx
{
    // A few sources in rotation, so a click does not cut off the hover that came a frame before it.
    private const int Voices = 4;

    private readonly object _lock = new();
    private int _hover, _click;
    private int[]? _sources;
    private int _next;
    private float _volume = 1f;
    private bool _disposed;

    public void SetVolume(float v01)
    {
        lock (_lock)
        {
            _volume = Math.Clamp(v01, 0f, 1f);
            if (_sources == null) return;
            foreach (var s in _sources) AL.Source(s, ALSourcef.Gain, _volume);
        }
    }

    public void PlayHover() => Play(hover: true);
    public void PlayClick() => Play(hover: false);

    private void Play(bool hover)
    {
        if (Cathedral.Config.Debug.Silent) return;
        lock (_lock)
        {
            if (_disposed || !EnsureBuffers()) return;
            int source = _sources![_next];
            _next = (_next + 1) % Voices;
            AL.SourceStop(source);
            AL.Source(source, ALSourcei.Buffer, hover ? _hover : _click);
            AL.SourcePlay(source);
        }
    }

    // Built on first use, so a run that never makes a sound never opens a device — as WinMM does.
    private bool EnsureBuffers()
    {
        if (_sources != null) return true;
        if (!OpenAlAudio.EnsureOpen()) return false;
        _hover = OpenAlAudio.MonoBuffer(UiSfxPlayer.HoverSamples(), UiSfxPlayer.SfxSampleRate);
        _click = OpenAlAudio.MonoBuffer(UiSfxPlayer.ClickSamples(), UiSfxPlayer.SfxSampleRate);
        _sources = new int[Voices];
        for (int i = 0; i < Voices; i++)
        {
            _sources[i] = AL.GenSource();
            AL.Source(_sources[i], ALSourcef.Gain, _volume);
        }
        return true;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            if (_sources == null) return;
            try
            {
                foreach (var s in _sources) { AL.SourceStop(s); AL.DeleteSource(s); }
                AL.DeleteBuffer(_hover);
                AL.DeleteBuffer(_click);
            }
            catch { /* context already gone at shutdown */ }
        }
    }
}

/// <summary>
/// The brown-noise wash through OpenAL: WinMM's two blocks, back to back in one looping buffer,
/// which is the order its double-buffering plays them in.
/// </summary>
internal sealed class OpenAlNoiseStream : INoiseStream
{
    private readonly float _amplitude;
    private int _source, _buffer;
    private bool _started, _disposed;

    public OpenAlNoiseStream(float amplitude) => _amplitude = amplitude;

    public void Start()
    {
        if (_started || _disposed) return;
        _started = true;
        if (!OpenAlAudio.EnsureOpen()) return;

        int n = BrownNoiseStreamer.NoiseSampleRate * BrownNoiseStreamer.NoiseBlockSeconds;
        var both = new short[n * 2];
        BrownNoiseStreamer.BrownNoiseSamples(n, BrownNoiseStreamer.NoiseSeed0, _amplitude).CopyTo(both, 0);
        BrownNoiseStreamer.BrownNoiseSamples(n, BrownNoiseStreamer.NoiseSeed1, _amplitude).CopyTo(both, n);

        _buffer = OpenAlAudio.MonoBuffer(both, BrownNoiseStreamer.NoiseSampleRate);
        _source = AL.GenSource();
        AL.Source(_source, ALSourcei.Buffer, _buffer);
        AL.Source(_source, ALSourceb.Looping, true);
        AL.SourcePlay(_source);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_source == 0) return;
        try
        {
            AL.SourceStop(_source);
            AL.DeleteSource(_source);
            AL.DeleteBuffer(_buffer);
        }
        catch { /* context already gone at shutdown */ }
    }
}

/// <summary>
/// General MIDI rendered in-process: MeltySynth plays the SoundFont, and a thread streams what it
/// renders into one OpenAL source. This stands in for the system synthesiser Linux does not have.
///
/// <para>The engine sends events from several threads (one per track, plus sound effects), and
/// MeltySynth is not thread-safe, so every event and every render takes one lock. A block is 512
/// frames (11.6 ms at 44.1 kHz) and four are queued, so a note is heard ~50 ms after it is sent —
/// in line with the GS Wavetable Synth's own latency.</para>
/// </summary>
internal sealed class SoundFontMidiOut : IMidiOut
{
    private const int SampleRate  = 44100;
    private const int BlockFrames = 512;
    private const int QueueDepth  = 4;

    private readonly object _lock = new();
    private readonly Synthesizer _synth;
    private readonly int _source;
    private readonly int[] _buffers;
    private readonly Thread _thread;
    private volatile bool _running = true;

    public string Name { get; }

    private SoundFontMidiOut(Synthesizer synth, string name)
    {
        _synth = synth;
        Name = name;
        _source = AL.GenSource();
        _buffers = AL.GenBuffers(QueueDepth);
        _thread = new Thread(RenderLoop) { IsBackground = true, Name = "SoundFontSynth" };
        _thread.Start();
    }

    public static SoundFontMidiOut? TryOpen()
    {
        var path = SoundFontLocator.Find();
        if (path == null)
        {
            Console.Error.WriteLine(Config.Debug.SoundFontPath is { } p
                ? $"[audio] --soundfont {p}: no such file."
                : $"[audio] No SoundFont at {SoundFontLocator.RelativePath}: music is off. See assets/audio/README.md.");
            return null;
        }
        if (!OpenAlAudio.EnsureOpen()) return null;
        try
        {
            var synth = new Synthesizer(new SoundFont(path), new SynthesizerSettings(SampleRate));
            return new SoundFontMidiOut(synth, $"SoundFont {System.IO.Path.GetFileName(path)} (MeltySynth, OpenAL)");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[audio] SoundFont {path} could not be loaded: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    public void SendEvent(MidiEvent ev)
    {
        int command, data1, data2;
        switch (ev)
        {
            // A NoteOn at velocity 0 is a NoteOff; MeltySynth's NoteOn treats it as one.
            case NoteOnEvent on:        command = 0x90; data1 = on.NoteNumber;       data2 = on.Velocity;      break;
            case NoteOffEvent off:      command = 0x80; data1 = off.NoteNumber;      data2 = off.Velocity;     break;
            case ProgramChangeEvent pc: command = 0xC0; data1 = pc.ProgramNumber;    data2 = 0;                break;
            case ControlChangeEvent cc: command = 0xB0; data1 = cc.ControlNumber;    data2 = cc.ControlValue;  break;
            case PitchBendEvent pb:     command = 0xE0; data1 = pb.PitchValue & 0x7F; data2 = pb.PitchValue >> 7; break;
            default: return;   // the engine sends nothing else
        }
        int channel = ((ChannelEvent)ev).Channel;
        lock (_lock) _synth.ProcessMidiMessage(channel, command, data1, data2);
    }

    public void TurnAllNotesOff()
    {
        lock (_lock) _synth.NoteOffAll(false);
    }

    private void RenderLoop()
    {
        var left  = new float[BlockFrames];
        var right = new float[BlockFrames];
        var pcm   = new short[BlockFrames * 2];

        void Fill(int buffer)
        {
            lock (_lock) _synth.Render(left, right);
            for (int i = 0; i < BlockFrames; i++)
            {
                pcm[2 * i]     = (short)Math.Clamp(left[i]  * 32767f, short.MinValue, short.MaxValue);
                pcm[2 * i + 1] = (short)Math.Clamp(right[i] * 32767f, short.MinValue, short.MaxValue);
            }
            AL.BufferData(buffer, ALFormat.Stereo16, pcm, SampleRate);
        }

        try
        {
            foreach (var b in _buffers) Fill(b);
            AL.SourceQueueBuffers(_source, _buffers);
            AL.SourcePlay(_source);

            while (_running)
            {
                AL.GetSource(_source, ALGetSourcei.BuffersProcessed, out int processed);
                while (processed-- > 0 && _running)
                {
                    int buffer = AL.SourceUnqueueBuffer(_source);
                    Fill(buffer);
                    AL.SourceQueueBuffer(_source, buffer);
                }
                // A starved source stops; start it again rather than fall silent for good.
                AL.GetSource(_source, ALGetSourcei.SourceState, out int state);
                if ((ALSourceState)state != ALSourceState.Playing) AL.SourcePlay(_source);
                Thread.Sleep(3);
            }
        }
        catch (Exception ex)
        {
            if (_running) Console.Error.WriteLine($"[audio] SoundFont render thread: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void Dispose()
    {
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
