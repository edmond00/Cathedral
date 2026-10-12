using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Cathedral.Glyph;
using OpenTK.Graphics.OpenGL4;

namespace Cathedral.Game.Record;

/// <summary>
/// Films the window for <c>--record</c>. The frame is resolved into a framebuffer of the recorder's own
/// (so it can be read back whether or not the window is ever shown), the cursor is painted over it,
/// and it is piped to ffmpeg as raw RGB; then it is copied to the window as usual.
///
/// <para><b>Real time, not frame-locked.</b> Frame <c>n</c> of the file is whatever the window showed at
/// <c>n / fps</c> seconds on <see cref="RecordMode.Clock"/>: when the game renders faster than the video
/// rate, frames are skipped (and not even read back); when it renders slower — a model call, a world
/// being generated — the last frame is repeated. So the footage and timeline.jsonl share one clock,
/// which is what lets a clip be cut out of it by its timestamps.</para>
/// </summary>
public sealed class FrameRecorder : IFrameSink, IDisposable
{
    private readonly RecordPointer _pointer;
    private readonly int _fps;
    private readonly int _width, _height;
    private int _fbo, _tex, _rbo, _targetW, _targetH;

    private readonly Process? _ffmpeg;
    private readonly Stream? _stdin;
    private readonly Thread? _writer;
    /// <summary>Frames for the writer; a null frame means "repeat the last one written".</summary>
    private readonly BlockingCollection<(byte[]? Frame, long Count)> _queue = new(boundedCapacity: 90);
    private readonly ConcurrentBag<byte[]> _pool = new();
    private long _written;
    private volatile bool _failed;
    private bool _warnedSize;

    public long FramesWritten => Interlocked.Read(ref _written);
    public string OutputFile { get; }

    public FrameRecorder(RecordPointer pointer)
    {
        _pointer = pointer;
        _fps = RecordMode.Fps;
        _width = RecordMode.Width;
        _height = RecordMode.Height;
        OutputFile = Path.Combine(RecordMode.OutputDir, "game.mkv");

        string? ffmpeg = RecordMode.FindFfmpeg();
        if (ffmpeg == null)
        {
            Console.WriteLine("[record] ffmpeg not found — set CATHEDRAL_FFMPEG or run tools/video/setup.ps1. Nothing will be filmed.");
            _failed = true;
            return;
        }

        // Lossless enough to cut and re-encode later, fast enough to keep up in real time. 4:4:4 because
        // the dither is a one-pixel pattern of yellow and purple, which 4:2:0 would smear into mud; the
        // final encode in tools/video takes it down to something every player can open. Matroska because
        // a run that dies mid-recording still leaves a playable file, which an MP4 without its index is not.
        var psi = new ProcessStartInfo(ffmpeg)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var a in new[]
        {
            "-y", "-loglevel", "error",
            "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", $"{_width}x{_height}", "-r", _fps.ToString(), "-i", "-",
            "-vf", "vflip",
            "-c:v", "libx264", "-preset", "veryfast", "-crf", "12", "-pix_fmt", "yuv444p", "-g", (_fps * 2).ToString(),
            OutputFile,
        }) psi.ArgumentList.Add(a);

        try
        {
            _ffmpeg = Process.Start(psi)!;
            var log = new StreamWriter(Path.Combine(RecordMode.OutputDir, "ffmpeg.log")) { AutoFlush = true };
            _ffmpeg.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (log) log.WriteLine(e.Data); };
            _ffmpeg.BeginErrorReadLine();
            _stdin = _ffmpeg.StandardInput.BaseStream;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[record] could not start ffmpeg ({ex.Message}). Nothing will be filmed.");
            _failed = true;
            return;
        }

        _writer = new Thread(WriteLoop) { IsBackground = true, Name = "record-ffmpeg" };
        _writer.Start();
        Console.WriteLine($"[record] filming {_width}x{_height} @ {_fps} fps to {OutputFile}");
    }

    // ── IFrameSink ────────────────────────────────────────────────────────────

    public int BeginFrame(int width, int height)
    {
        EnsureTarget(width, height);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        GL.Viewport(0, 0, width, height);
        return _fbo;
    }

    public void EndFrame(int width, int height)
    {
        if (!_failed)
        {
            if (width != _width || height != _height)
            {
                if (!_warnedSize)
                    Console.WriteLine($"[record] window is {width}x{height}, not {_width}x{_height} — frames skipped until it is");
                _warnedSize = true;
            }
            else
            {
                // Frames due by now, against frames already handed to the writer. Only a frame that is
                // due is read back — the read-back is the expensive part.
                double now = RecordMode.Now;
                long due = (long)Math.Floor(now * _fps) + 1;
                if (due > _queued) Capture(width, height, now, due);
            }
        }

        // Present: the window shows exactly what was filmed, minus the drawn cursor.
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _fbo);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        GL.BlitFramebuffer(0, 0, width, height, 0, 0, width, height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    /// <summary>Frames handed to the writer so far, duplicates included.</summary>
    private long _queued;
    private bool _firstQueued;

    private void Capture(int w, int h, double now, long due)
    {
        long count = due - _queued;

        if (!_pool.TryTake(out var buf) || buf.Length != w * h * 3) buf = new byte[w * h * 3];
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _fbo);
        GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
        GL.ReadPixels(0, 0, w, h, PixelFormat.Rgb, PixelType.UnsignedByte, buf);

        CursorPainter.Paint(buf, w, h, _pointer, now);

        // The slots missed while nothing was rendered showed the previous frame — that is what was on
        // screen — and only the newest slot shows this one.
        _queued += count;
        // The very first frame has no predecessor, so it fills the slots before it itself. (Leaving them
        // to "the previous frame" dropped them, and every timestamp after ran ~0.4 s ahead of the
        // timeline — a clip cut by its markers ended on whatever the next command opened.)
        try
        {
            if (!_firstQueued) { _queue.Add((buf, count)); _firstQueued = true; }
            else
            {
                if (count > 1) _queue.Add((null, count - 1));
                _queue.Add((buf, 1));
            }
        }
        catch (InvalidOperationException) { /* shutting down */ }
    }

    private void WriteLoop()
    {
        byte[]? last = null;
        try
        {
            foreach (var (frame, count) in _queue.GetConsumingEnumerable())
            {
                var f = frame ?? last;
                if (f == null) continue;
                for (long i = 0; i < count; i++)
                {
                    _stdin!.Write(f, 0, f.Length);
                    Interlocked.Increment(ref _written);
                }
                if (frame != null)
                {
                    if (last != null) _pool.Add(last);
                    last = frame;
                }
            }
        }
        catch (Exception ex)
        {
            _failed = true;
            Console.WriteLine($"[record] ffmpeg stopped accepting frames ({ex.GetType().Name}: {ex.Message}) — see ffmpeg.log");
        }
    }

    private void EnsureTarget(int w, int h)
    {
        if (_fbo != 0 && w == _targetW && h == _targetH) return;
        DisposeTarget();

        _tex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _tex);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, w, h, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        // Depth too: with the dither layer off, the whole scene is drawn straight into this target.
        _rbo = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rbo);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, w, h);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

        _fbo = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _tex, 0);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _rbo);
        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        if (status != FramebufferErrorCode.FramebufferComplete)
        {
            Console.WriteLine($"[record] capture framebuffer incomplete ({status}) — nothing will be filmed");
            _failed = true;
        }
        _targetW = w; _targetH = h;
    }

    private void DisposeTarget()
    {
        if (_fbo != 0) { GL.DeleteFramebuffer(_fbo); _fbo = 0; }
        if (_tex != 0) { GL.DeleteTexture(_tex); _tex = 0; }
        if (_rbo != 0) { GL.DeleteRenderbuffer(_rbo); _rbo = 0; }
    }

    /// <summary>Flushes the queue and closes the file. Must run before the process exits, or the tail is lost.</summary>
    public void Dispose()
    {
        _queue.CompleteAdding();
        _writer?.Join(TimeSpan.FromSeconds(60));
        try { _stdin?.Close(); } catch { /* ffmpeg already gone */ }
        if (_ffmpeg != null && !_ffmpeg.WaitForExit(60_000))
        {
            Console.WriteLine("[record] ffmpeg did not finish in 60s — killing it");
            try { _ffmpeg.Kill(); } catch { }
        }
        Console.WriteLine($"[record] {FramesWritten} frames ({FramesWritten / (double)_fps:F1}s) written to {OutputFile}");
    }
}

/// <summary>
/// The cursor and its click ripple, painted straight into a read-back frame (bottom-up RGB). Drawn by
/// hand rather than rendered by the game, so it can never be confused with anything the game draws and
/// costs the game nothing.
/// </summary>
internal static class CursorPainter
{
    // '#' outline, 'o' fill. The hot spot is the top-left pixel.
    private static readonly string[] Arrow =
    {
        "#",
        "##",
        "#o#",
        "#oo#",
        "#ooo#",
        "#oooo#",
        "#ooooo#",
        "#oooooo#",
        "#ooooooo#",
        "#oooooooo#",
        "#ooooo#####",
        "#oo#oo#",
        "#o# #oo#",
        "##  #oo#",
        "#    #oo#",
        "     #oo#",
        "      ##",
    };

    // The game's own palette (Config.Colors): a pale yellow body, gold while pressed, a light-purple ripple.
    private static readonly (byte R, byte G, byte B) Outline = (10, 8, 14);
    private static readonly (byte R, byte G, byte B) Fill = (255, 245, 175);
    private static readonly (byte R, byte G, byte B) FillPressed = (255, 217, 51);
    private static readonly (byte R, byte G, byte B) Ripple = (217, 140, 255);

    private const double RippleSeconds = 0.38;

    public static void Paint(byte[] buf, int w, int h, RecordPointer pointer, double now)
    {
        if (!pointer.Visible) return;
        int scale = Math.Max(1, (int)Math.Round(h / 540.0));

        if (pointer.LastPress is { } press && now - press.Time < RippleSeconds)
        {
            double u = (now - press.Time) / RippleSeconds;
            float radius = (float)(4 + 22 * u) * scale * 0.5f + 3;
            float alpha = (float)(1 - u);
            Ring(buf, w, h, press.At.X, press.At.Y, radius, 1.2f * scale, Ripple, alpha);
        }

        var p = pointer.PositionAt(now);
        var fill = pointer.IsPressed(now) ? FillPressed : Fill;
        int ox = (int)MathF.Round(p.X), oy = (int)MathF.Round(p.Y);
        for (int row = 0; row < Arrow.Length; row++)
            for (int col = 0; col < Arrow[row].Length; col++)
            {
                char c = Arrow[row][col];
                if (c == ' ') continue;
                var colour = c == '#' ? Outline : fill;
                for (int sy = 0; sy < scale; sy++)
                    for (int sx = 0; sx < scale; sx++)
                        Put(buf, w, h, ox + col * scale + sx, oy + row * scale + sy, colour, 1f);
            }
    }

    private static void Ring(byte[] buf, int w, int h, float cx, float cy, float r, float thickness, (byte R, byte G, byte B) c, float alpha)
    {
        int x0 = (int)(cx - r - thickness - 1), x1 = (int)(cx + r + thickness + 1);
        int y0 = (int)(cy - r - thickness - 1), y1 = (int)(cy + r + thickness + 1);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float d = MathF.Abs(MathF.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r);
                if (d <= thickness) Put(buf, w, h, x, y, c, alpha * Math.Clamp(1 - d / thickness * 0.5f, 0, 1));
            }
    }

    private static void Put(byte[] buf, int w, int h, int x, int yTop, (byte R, byte G, byte B) c, float alpha)
    {
        if (x < 0 || x >= w || yTop < 0 || yTop >= h) return;
        int i = ((h - 1 - yTop) * w + x) * 3;
        if (alpha >= 1f) { buf[i] = c.R; buf[i + 1] = c.G; buf[i + 2] = c.B; return; }
        buf[i]     = (byte)(buf[i]     + (c.R - buf[i])     * alpha);
        buf[i + 1] = (byte)(buf[i + 1] + (c.G - buf[i + 1]) * alpha);
        buf[i + 2] = (byte)(buf[i + 2] + (c.B - buf[i + 2]) * alpha);
    }
}
