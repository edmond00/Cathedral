using System;
using System.IO;

namespace Cathedral.Audio.Music;

/// <summary>16-bit stereo PCM WAV, written as it is rendered so a long render needs no buffer.</summary>
internal sealed class WavWriter : IDisposable
{
    private readonly FileStream _fs;
    private readonly BinaryWriter _w;
    private readonly int _rate;
    private long _frames;

    public WavWriter(string path, int rate)
    {
        _rate = rate;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        _fs = File.Create(path);
        _w = new BinaryWriter(_fs);
        WriteHeader(0);
    }

    private void WriteHeader(long frames)
    {
        long data = frames * 4;
        _w.Write("RIFF"u8);
        _w.Write((uint)(36 + data));
        _w.Write("WAVE"u8);
        _w.Write("fmt "u8);
        _w.Write(16u);
        _w.Write((ushort)1);
        _w.Write((ushort)2);
        _w.Write((uint)_rate);
        _w.Write((uint)(_rate * 4));
        _w.Write((ushort)4);
        _w.Write((ushort)16);
        _w.Write("data"u8);
        _w.Write((uint)data);
    }

    public void Write(float[] l, float[] r, int n)
    {
        for (int i = 0; i < n; i++)
        {
            _w.Write((short)Math.Clamp(l[i] * 32767f, -32768f, 32767f));
            _w.Write((short)Math.Clamp(r[i] * 32767f, -32768f, 32767f));
        }
        _frames += n;
    }

    /// <summary>
    /// Rewrites the header for what has been written so far and returns to the end, so a file whose
    /// writer is never disposed (a process ended abruptly) still reads as everything up to here.
    /// </summary>
    public void Checkpoint()
    {
        _w.Flush();
        long end = _fs.Position;
        _fs.Seek(0, SeekOrigin.Begin);
        WriteHeader(_frames);
        _w.Flush();
        _fs.Seek(end, SeekOrigin.Begin);
    }

    public void Dispose()
    {
        _fs.Seek(0, SeekOrigin.Begin);
        WriteHeader(_frames);
        _w.Dispose();
    }
}
