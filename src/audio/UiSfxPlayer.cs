using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Cathedral.Audio;

/// <summary>
/// Low-latency UI feedback sounds using WinMM PlaySound with permanently-pinned PCM buffers.
/// Bypasses the MIDI synthesizer entirely, eliminating the GS Wavetable voice-activation delay.
///
/// IMPORTANT: arrays must be pinned with GCHandle.Pinned for the object's lifetime.
/// PlaySound with SND_ASYNC returns immediately while winmm still reads the buffer —
/// passing an unpinned managed array lets the GC move the memory mid-playback, causing
/// silent failures or corrupted audio.
/// </summary>
internal sealed class UiSfxPlayer : IUiSfx
{
    // ── WinMM P/Invoke ────────────────────────────────────────────────────────
    // Use nint so we can pass the pinned pointer directly (no managed marshalling).
    [DllImport("winmm.dll", EntryPoint = "PlaySound")]
    private static extern bool WinMMPlaySound(nint pszSound, nint hmod, uint fdwSound);

    // WinMM exists only on Windows. Elsewhere the ticks are silent until the cross-platform audio path
    // replaces this player (see tools/linux/PORT-BRIEF.md, step 5); without the guard the first call
    // throws DllNotFoundException from the AmbianceEngine constructor and takes the game down.
    private static bool PlaySound(nint pszSound, nint hmod, uint fdwSound) =>
        OperatingSystem.IsWindows() && WinMMPlaySound(pszSound, hmod, fdwSound);

    private const uint SND_ASYNC     = 0x0001;
    private const uint SND_NODEFAULT = 0x0002;
    private const uint SND_MEMORY   = 0x0004;

    // ── Pinned WAV buffers ────────────────────────────────────────────────────
    private GCHandle _hoverHandle;
    private GCHandle _clickHandle;
    private GCHandle _transitionHandle;
    private nint _hoverPtr;
    private nint _clickPtr;
    private nint _transitionPtr;
    private float _volume = 1f;
    private bool _disposed;

    public UiSfxPlayer()
    {
        RebuildBuffers();
    }

    /// <summary>
    /// Sets the playback volume (0..1) by rebuilding the PCM buffers at a scaled amplitude.
    /// PlaySound has no per-call volume, so the waveform amplitude is baked into the buffer.
    /// </summary>
    public void SetVolume(float v01)
    {
        float v = Math.Clamp(v01, 0f, 1f);
        if (v == _volume) return;
        _volume = v;
        RebuildBuffers();
    }

    private void RebuildBuffers()
    {
        // Stop any in-flight async playback before freeing the buffers it may still read.
        PlaySound(nint.Zero, nint.Zero, 0);
        if (_hoverHandle.IsAllocated) _hoverHandle.Free();
        if (_clickHandle.IsAllocated) _clickHandle.Free();
        if (_transitionHandle.IsAllocated) _transitionHandle.Free();

        var hoverWav = BuildHoverWav(_volume);
        var clickWav = BuildClickWav(_volume);
        var transitionWav = BuildTransitionWav(_volume);
        _hoverHandle = GCHandle.Alloc(hoverWav, GCHandleType.Pinned);
        _clickHandle = GCHandle.Alloc(clickWav, GCHandleType.Pinned);
        _transitionHandle = GCHandle.Alloc(transitionWav, GCHandleType.Pinned);
        _hoverPtr    = _hoverHandle.AddrOfPinnedObject();
        _clickPtr    = _clickHandle.AddrOfPinnedObject();
        _transitionPtr = _transitionHandle.AddrOfPinnedObject();
    }

    // Both ticks go through WinMM rather than the MIDI device, so --silent (which works by opening no
    // device) never reached them: a hidden test run, or a --record running in the background, ticked on
    // the developer's speakers at every hover. They answer the flag themselves.

    /// <summary>Play a faint hi-hat tick for mouse-over UI feedback.</summary>
    public void PlayHover()
    {
        if (Cathedral.Config.Debug.Silent) return;
        PlaySound(_hoverPtr, nint.Zero, SND_ASYNC | SND_NODEFAULT | SND_MEMORY);
    }

    /// <summary>Play a short digital click for mouse-button UI feedback.</summary>
    public void PlayClick()
    {
        if (Cathedral.Config.Debug.Silent) return;
        PlaySound(_clickPtr, nint.Zero, SND_ASYNC | SND_NODEFAULT | SND_MEMORY);
    }

    /// <summary>
    /// Play the phase-transition swish. WinMM plays one sound at a time, so this cuts short the click
    /// that usually caused it; the swish stands in for it.
    /// </summary>
    public void PlayTransition()
    {
        if (Cathedral.Config.Debug.Silent) return;
        PlaySound(_transitionPtr, nint.Zero, SND_ASYNC | SND_NODEFAULT | SND_MEMORY);
    }

    /// <summary>
    /// Writes the UI sounds as WAV files (<c>hover.wav</c>, <c>click.wav</c>, <c>transition.wav</c>) — <c>--export-sfx</c>, which
    /// is where tools/video gets the click it lays under a recording's cursor. The very bytes the game
    /// plays, so the video's click is the game's click.
    /// </summary>
    public static void ExportWavs(string directory)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, "hover.wav"), BuildHoverWav());
        File.WriteAllBytes(Path.Combine(directory, "click.wav"), BuildClickWav());
        File.WriteAllBytes(Path.Combine(directory, "transition.wav"), BuildTransitionWav());
    }

    // ── PCM generation ────────────────────────────────────────────────────────

    /// <summary>25 ms high-pass filtered noise burst — hi-hat simulation.</summary>
    private static byte[] BuildHoverWav(float amplitude = 1f) => BuildWav(HoverSamples(amplitude), SfxSampleRate);

    /// <summary>80 ms decaying harmonic tone (520 + 1040 + 1560 Hz) — digital click.</summary>
    private static byte[] BuildClickWav(float amplitude = 1f) => BuildWav(ClickSamples(amplitude), SfxSampleRate);

    /// <summary>170 ms swish of filtered noise — the phase-transition mark.</summary>
    private static byte[] BuildTransitionWav(float amplitude = 1f) => BuildWav(TransitionSamples(amplitude), SfxSampleRate);

    /// <summary>The ticks' sample rate. Shared with the OpenAL player, which plays these same samples.</summary>
    internal const int SfxSampleRate = 22050;

    /// <summary>The hover tick as 16-bit mono PCM at <see cref="SfxSampleRate"/>.</summary>
    internal static short[] HoverSamples(float amplitude = 1f)
    {
        const int sampleRate = 22050;
        const int durationMs = 25;
        int n = sampleRate * durationMs / 1000;
        var rng = new Random(0x4A9B);
        var samples = new short[n];
        float prev = 0f;
        for (int i = 0; i < n; i++)
        {
            float t     = (float)i / n;
            float decay = MathF.Exp(-t * 14f);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float hp    = noise - prev * 0.7f;   // 1-pole high-pass → airy hi-hat character
            prev = noise;
            samples[i]  = (short)Math.Clamp(hp * decay * 14000f * amplitude, short.MinValue, short.MaxValue);
        }
        return samples;
    }

    /// <summary>The click tick as 16-bit mono PCM at <see cref="SfxSampleRate"/>.</summary>
    internal static short[] ClickSamples(float amplitude = 1f)
    {
        const int sampleRate = 22050;
        const int durationMs = 80;
        int n = sampleRate * durationMs / 1000;
        var samples = new short[n];
        for (int i = 0; i < n; i++)
        {
            float t    = (float)i / n;
            float decay = MathF.Exp(-t * 7f);
            float tone  = MathF.Sin(2f * MathF.PI * 520f  * i / sampleRate)
                        + 0.6f * MathF.Sin(2f * MathF.PI * 1040f * i / sampleRate)
                        + 0.3f * MathF.Sin(2f * MathF.PI * 1560f * i / sampleRate);
            samples[i]  = (short)Math.Clamp(tone * decay * 11000f * amplitude, short.MinValue, short.MaxValue);
        }
        return samples;
    }

    /// <summary>
    /// The transition swish as 16-bit mono PCM at <see cref="SfxSampleRate"/>: a breath of air, as of a
    /// page turned or a curtain drawn. Noise through a gentle band-pass whose centre glides down from
    /// 2.4 kHz to 900 Hz, its hiss taken off by a low-pass at 2.5 kHz, swelling in over the first 40 ms and dying away by 170 ms. Nothing tonal, so
    /// it never sounds like the synthesiser and never clashes with the key of the music; quieter than
    /// the click, since it plays far more often than any outcome. Deterministic (a fixed xorshift, no
    /// generator), so the bytes are the same on every machine, as the other ticks are.
    /// </summary>
    internal static short[] TransitionSamples(float amplitude = 1f)
    {
        const int sampleRate = SfxSampleRate;
        const int durationMs = 170;
        const float swellSeconds = 0.04f;
        int n = sampleRate * durationMs / 1000;
        var samples = new short[n];
        uint noise = 0x9E3779B9u;
        float low = 0f, band = 0f;          // state-variable filter state
        float soft = 0f;                    // one-pole low-pass after it: takes the hiss off the top
        float softCoeff = 1f - MathF.Exp(-2f * MathF.PI * 2500f / sampleRate);
        const float damping = 0.8f;         // 1/Q: wide enough to sound like air, not a whistle
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float seconds = (float)i / sampleRate;

            noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5;
            float white = (noise & 0xFFFF) / 32767.5f - 1f;

            float centre = 2400f * MathF.Pow(900f / 2400f, t);
            float f = 2f * MathF.Sin(MathF.PI * centre / sampleRate);
            low  += f * band;
            float high = white - low - damping * band;
            band += f * high;
            soft += (band - soft) * softCoeff;

            // A raised-cosine swell, then a smooth fall to silence at the end.
            float swell = seconds < swellSeconds
                ? 0.5f - 0.5f * MathF.Cos(MathF.PI * seconds / swellSeconds)
                : 1f;
            float fall = seconds < swellSeconds
                ? 1f
                : MathF.Pow(1f - (seconds - swellSeconds) / (durationMs / 1000f - swellSeconds), 2f);

            samples[i] = (short)Math.Clamp(soft * swell * fall * 10000f * amplitude, short.MinValue, short.MaxValue);
        }
        return samples;
    }

    private static byte[] BuildWav(short[] samples, int sampleRate)
    {
        int byteCount = samples.Length * 2;
        using var ms = new MemoryStream(44 + byteCount);
        using var bw = new BinaryWriter(ms);
        bw.Write("RIFF"u8); bw.Write(36 + byteCount);
        bw.Write("WAVE"u8);
        bw.Write("fmt "u8); bw.Write(16);
        bw.Write((short)1);         // PCM
        bw.Write((short)1);         // mono
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2);   // byte rate
        bw.Write((short)2);         // block align
        bw.Write((short)16);        // bits per sample
        bw.Write("data"u8); bw.Write(byteCount);
        foreach (var s in samples) bw.Write(s);
        return ms.ToArray();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        PlaySound(nint.Zero, nint.Zero, 0); // stop any pending async sound
        if (_hoverHandle.IsAllocated) _hoverHandle.Free();
        if (_clickHandle.IsAllocated) _clickHandle.Free();
        if (_transitionHandle.IsAllocated) _transitionHandle.Free();
    }
}

/// <summary>
/// Streams looping PCM brown noise via waveOut, independent of PlaySound's single channel.
/// This lets the loading-filter background noise coexist with click/hover sounds without
/// interrupting each other.
///
/// WAVEHDR field offsets are fixed for x64 (PlatformTarget = x64):
///   lpData(8) + dwBufferLength(4) + dwBytesRecorded(4) + dwUser(8) + dwFlags(4) + ...
/// </summary>
internal sealed class BrownNoiseStreamer : INoiseStream
{
    [DllImport("winmm.dll")] private static extern uint waveOutOpen(out nint hwo, uint devId, nint fmt, nint cb, nint inst, uint flags);
    [DllImport("winmm.dll")] private static extern uint waveOutPrepareHeader(nint hwo, nint hdr, uint sz);
    [DllImport("winmm.dll")] private static extern uint waveOutUnprepareHeader(nint hwo, nint hdr, uint sz);
    [DllImport("winmm.dll")] private static extern uint waveOutWrite(nint hwo, nint hdr, uint sz);
    [DllImport("winmm.dll")] private static extern uint waveOutReset(nint hwo);
    [DllImport("winmm.dll")] private static extern uint waveOutClose(nint hwo);

    private const uint WAVE_MAPPER   = 0xFFFF_FFFFu;
    private const uint CALLBACK_NULL = 0;
    private const uint WHDR_DONE     = 0x00000001;

    // WAVEHDR field offsets on x64 Windows
    private const int OFF_lpData         = 0;
    private const int OFF_dwBufferLength = 8;
    private const int OFF_dwFlags        = 24;
    private const int WAVEHDR_SIZE       = 48;
    private const int WAVEFORMATEX_SIZE  = 18;

    private readonly nint _fmt, _hdr0, _hdr1, _pcm0, _pcm1;
    private readonly int  _blockBytes;
    private nint _hwo;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public BrownNoiseStreamer(float amplitude = 1.0f)
    {
        int numSamples = NoiseSampleRate * NoiseBlockSeconds;
        const int sampleRate = NoiseSampleRate;
        _blockBytes    = numSamples * 2;

        // WAVEFORMATEX
        _fmt = Marshal.AllocHGlobal(WAVEFORMATEX_SIZE);
        ZeroMem(_fmt, WAVEFORMATEX_SIZE);
        Marshal.WriteInt16(_fmt,  0, 1);                // wFormatTag = PCM
        Marshal.WriteInt16(_fmt,  2, 1);                // nChannels = 1
        Marshal.WriteInt32(_fmt,  4, sampleRate);       // nSamplesPerSec
        Marshal.WriteInt32(_fmt,  8, sampleRate * 2);   // nAvgBytesPerSec
        Marshal.WriteInt16(_fmt, 12, 2);                // nBlockAlign
        Marshal.WriteInt16(_fmt, 14, 16);               // wBitsPerSample

        // PCM buffers — two blocks for gapless double-buffering
        _pcm0 = Marshal.AllocHGlobal(_blockBytes);
        _pcm1 = Marshal.AllocHGlobal(_blockBytes);
        FillBrownNoise(_pcm0, numSamples, NoiseSeed0, amplitude);
        FillBrownNoise(_pcm1, numSamples, NoiseSeed1, amplitude);

        _hdr0 = AllocWavHdr(_pcm0, _blockBytes);
        _hdr1 = AllocWavHdr(_pcm1, _blockBytes);
    }

    public void Start()
    {
        if (_hwo != nint.Zero) return;
        if (!OperatingSystem.IsWindows()) return;   // waveOut is WinMM; silent elsewhere, as with no device
        if (waveOutOpen(out _hwo, WAVE_MAPPER, _fmt, nint.Zero, nint.Zero, CALLBACK_NULL) != 0)
        {
            _hwo = nint.Zero;
            return;
        }
        // Prime both buffers
        waveOutPrepareHeader(_hwo, _hdr0, WAVEHDR_SIZE);
        waveOutWrite(_hwo, _hdr0, WAVEHDR_SIZE);
        waveOutPrepareHeader(_hwo, _hdr1, WAVEHDR_SIZE);
        waveOutWrite(_hwo, _hdr1, WAVEHDR_SIZE);

        _cts = new CancellationTokenSource();
        _ = Task.Run(() => LoopAsync(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        if (_hwo == nint.Zero) return;
        waveOutReset(_hwo);
        waveOutUnprepareHeader(_hwo, _hdr0, WAVEHDR_SIZE);
        waveOutUnprepareHeader(_hwo, _hdr1, WAVEHDR_SIZE);
        waveOutClose(_hwo);
        _hwo = nint.Zero;
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        nint[] hdrs = [_hdr0, _hdr1];
        int i = 0;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                nint hwo = _hwo;
                if (hwo == nint.Zero) break;

                nint hdr = hdrs[i & 1];
                uint flags = (uint)Marshal.ReadInt32(hdr, OFF_dwFlags);
                if ((flags & WHDR_DONE) != 0 && !ct.IsCancellationRequested)
                {
                    waveOutUnprepareHeader(hwo, hdr, WAVEHDR_SIZE);
                    Marshal.WriteInt32(hdr, OFF_dwFlags, 0); // clear before re-prepare
                    waveOutPrepareHeader(hwo, hdr, WAVEHDR_SIZE);
                    waveOutWrite(hwo, hdr, WAVEHDR_SIZE);
                    i++;
                }
                await Task.Delay(30, ct);
            }
        }
        catch (OperationCanceledException) { }
    }

    private static nint AllocWavHdr(nint pcm, int bytes)
    {
        nint hdr = Marshal.AllocHGlobal(WAVEHDR_SIZE + 8); // +8 safety margin
        ZeroMem(hdr, WAVEHDR_SIZE + 8);
        Marshal.WriteIntPtr(hdr, OFF_lpData, pcm);
        Marshal.WriteInt32(hdr, OFF_dwBufferLength, bytes);
        return hdr;
    }

    /// <summary>Leaky integrator: brown[n] = 0.997·brown[n-1] + 0.003·white[n] → 1/f² spectrum.
    /// Samples are generated into a float buffer first, then peak-normalized so amplitude
    /// is consistent regardless of the integrator's steady-state RMS.</summary>
    // Shared with the OpenAL noise stream, which loops the same two blocks in the same order.
    internal const int NoiseSampleRate   = 11025;
    internal const int NoiseBlockSeconds = 3;
    internal const int NoiseSeed0 = 0xBB_0001, NoiseSeed1 = 0xBB_0002;

    private static void FillBrownNoise(nint mem, int numSamples, int seed, float amplitude)
    {
        var samples = BrownNoiseSamples(numSamples, seed, amplitude);
        for (int i = 0; i < numSamples; i++) Marshal.WriteInt16(mem, i * 2, samples[i]);
    }

    /// <summary>One block of the wash as 16-bit mono PCM at <see cref="NoiseSampleRate"/>.</summary>
    internal static short[] BrownNoiseSamples(int numSamples, int seed, float amplitude)
    {
        var result = new short[numSamples];
        var rng  = new Random(seed);
        var buf  = new float[numSamples];
        float brown = 0f, peak = 1e-6f;

        for (int i = 0; i < numSamples; i++)
        {
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            brown       = brown * 0.997f + white * 0.003f;
            buf[i]      = brown;
            float abs   = MathF.Abs(brown);
            if (abs > peak) peak = abs;
        }

        // Normalise to target peak — 28000 ≈ 85% of short.MaxValue, leaving headroom
        float scale = amplitude * 28000f / peak;
        for (int i = 0; i < numSamples; i++)
            result[i] = (short)Math.Clamp(buf[i] * scale, short.MinValue, short.MaxValue);
        return result;
    }

    private static void ZeroMem(nint ptr, int count)
    {
        for (int i = 0; i < count; i++) Marshal.WriteByte(ptr, i, 0);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        Marshal.FreeHGlobal(_fmt);
        Marshal.FreeHGlobal(_hdr0);
        Marshal.FreeHGlobal(_hdr1);
        Marshal.FreeHGlobal(_pcm0);
        Marshal.FreeHGlobal(_pcm1);
    }
}
