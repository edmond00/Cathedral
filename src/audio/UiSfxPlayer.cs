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
internal sealed class UiSfxPlayer : IDisposable
{
    // ── WinMM P/Invoke ────────────────────────────────────────────────────────
    // Use nint so we can pass the pinned pointer directly (no managed marshalling).
    [DllImport("winmm.dll")]
    private static extern bool PlaySound(nint pszSound, nint hmod, uint fdwSound);

    private const uint SND_ASYNC     = 0x0001;
    private const uint SND_NODEFAULT = 0x0002;
    private const uint SND_MEMORY   = 0x0004;

    // ── Pinned WAV buffers ────────────────────────────────────────────────────
    private readonly GCHandle _hoverHandle;
    private readonly GCHandle _clickHandle;
    private readonly nint _hoverPtr;
    private readonly nint _clickPtr;
    private bool _disposed;

    public UiSfxPlayer()
    {
        var hoverWav = BuildHoverWav();
        var clickWav = BuildClickWav();
        _hoverHandle = GCHandle.Alloc(hoverWav, GCHandleType.Pinned);
        _clickHandle = GCHandle.Alloc(clickWav, GCHandleType.Pinned);
        _hoverPtr    = _hoverHandle.AddrOfPinnedObject();
        _clickPtr    = _clickHandle.AddrOfPinnedObject();
    }

    /// <summary>Play a faint hi-hat tick for mouse-over UI feedback.</summary>
    public void PlayHover()
        => PlaySound(_hoverPtr, nint.Zero, SND_ASYNC | SND_NODEFAULT | SND_MEMORY);

    /// <summary>Play a short digital click for mouse-button UI feedback.</summary>
    public void PlayClick()
        => PlaySound(_clickPtr, nint.Zero, SND_ASYNC | SND_NODEFAULT | SND_MEMORY);

    // ── PCM generation ────────────────────────────────────────────────────────

    /// <summary>25 ms high-pass filtered noise burst — hi-hat simulation.</summary>
    private static byte[] BuildHoverWav()
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
            samples[i]  = (short)Math.Clamp(hp * decay * 14000f, short.MinValue, short.MaxValue);
        }
        return BuildWav(samples, sampleRate);
    }

    /// <summary>80 ms decaying harmonic tone (520 + 1040 + 1560 Hz) — digital click.</summary>
    private static byte[] BuildClickWav()
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
            samples[i]  = (short)Math.Clamp(tone * decay * 11000f, short.MinValue, short.MaxValue);
        }
        return BuildWav(samples, sampleRate);
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
internal sealed class BrownNoiseStreamer : IDisposable
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
        const int sampleRate   = 11025;
        const int blockSeconds = 3;
        int numSamples = sampleRate * blockSeconds;
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
        FillBrownNoise(_pcm0, numSamples, 0xBB_0001, amplitude);
        FillBrownNoise(_pcm1, numSamples, 0xBB_0002, amplitude);

        _hdr0 = AllocWavHdr(_pcm0, _blockBytes);
        _hdr1 = AllocWavHdr(_pcm1, _blockBytes);
    }

    public void Start()
    {
        if (_hwo != nint.Zero) return;
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
    private static void FillBrownNoise(nint mem, int numSamples, int seed, float amplitude)
    {
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
        {
            short s = (short)Math.Clamp(buf[i] * scale, short.MinValue, short.MaxValue);
            Marshal.WriteInt16(mem, i * 2, s);
        }
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
