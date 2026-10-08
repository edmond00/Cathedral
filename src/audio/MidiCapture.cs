using System;
using System.Collections.Generic;
using System.Linq;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;

namespace Cathedral.Audio;

/// <summary>
/// Records every MIDI event the <see cref="AmbianceEngine"/> sends, timestamped on a clock supplied by
/// the caller, and writes them out as a standard MIDI file. This is how <c>--record</c> keeps the
/// game's music without playing it aloud: the engine composes exactly as it would for a player, no
/// device is opened, and tools/video renders the file to audio afterwards on the footage's clock.
///
/// <para>The file runs at 120 bpm with 480 ticks per quarter note, so one second is 960 ticks and the
/// event times survive the round trip to the millisecond — the music has no bar lines worth keeping;
/// what matters is that a note sounds at the moment the game played it.</para>
/// </summary>
public static class MidiCapture
{
    private static readonly List<(double Seconds, MidiEvent Event)> Events = new();
    private static readonly object Lock = new();
    private static Func<double> _clock = () => 0;

    public static bool IsActive { get; private set; }

    /// <summary>Starts capturing. <paramref name="clock"/> is read for every event's timestamp.</summary>
    public static void Start(Func<double> clock)
    {
        _clock = clock;
        IsActive = true;
    }

    /// <summary>Records one event (cloned: the sender may reuse it). Thread-safe; a no-op when inactive.</summary>
    public static void Record(MidiEvent ev)
    {
        if (!IsActive) return;
        double t = _clock();
        var copy = ev.Clone();
        lock (Lock) Events.Add((t, copy));
    }

    public static int Count { get { lock (Lock) return Events.Count; } }

    /// <summary>Writes everything captured so far to <paramref name="path"/>.</summary>
    public static void Write(string path)
    {
        const int ticksPerQuarter = 480;
        const double ticksPerSecond = ticksPerQuarter * 2.0;   // 120 bpm

        List<(double Seconds, MidiEvent Event)> events;
        lock (Lock) events = Events.OrderBy(e => e.Seconds).ToList();

        var track = new TrackChunk();
        track.Events.Add(new SetTempoEvent(500_000));
        long last = 0;
        foreach (var (seconds, ev) in events)
        {
            long tick = (long)Math.Round(Math.Max(0, seconds) * ticksPerSecond);
            ev.DeltaTime = tick - last;
            last = tick;
            track.Events.Add(ev);
        }

        var file = new MidiFile(track) { TimeDivision = new TicksPerQuarterNoteTimeDivision(ticksPerQuarter) };
        file.Write(path, overwriteFile: true);
    }
}
