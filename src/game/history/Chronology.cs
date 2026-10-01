using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.History;

/// <summary>
/// An ordered list of events: by round, and within a round in the order they were recorded.
///
/// <para>Recording links the event into every involved info's <see cref="HistoricInfo.Events"/>, so
/// the two directions can never disagree.</para>
///
/// <para><b>The cursor.</b> A generated history is written while time runs forward, and nothing may be
/// written into its past: <see cref="Cursor"/> is the round the simulation stands at, and recording
/// an earlier event throws. That is the rule that keeps a generated history coherent: a person made
/// up at round R to found a realm cannot be given a birth event at R-30, so their birth stays unknown
/// instead. The empire's hardcoded chronology is written out of order and has no cursor
/// (<see cref="Cursor"/> stays at <see cref="int.MinValue"/>).</para>
/// </summary>
public sealed class Chronology
{
    public Chronology(HistoryScope scope) => Scope = scope;

    /// <summary>
    /// Whose history this is. Recording links an event only into infos of the same scope: a world's
    /// chronology names empire infos (Oox, the Inquisition) but must not add to their event lists,
    /// because the empire catalogue is shared by every world generated in the process.
    /// </summary>
    public HistoryScope Scope { get; }

    private readonly List<(HistoricEvent Event, long Seq)> _entries = new();
    private long _nextSeq;
    private bool _sorted = true;

    /// <summary>The earliest round an event may still be recorded at.</summary>
    public int Cursor { get; private set; } = int.MinValue;

    public int Count => _entries.Count;

    /// <summary>Moves the cursor forward. It never moves back.</summary>
    public void Advance(int round)
    {
        if (round > Cursor) Cursor = round;
    }

    public void Record(HistoricEvent evt)
    {
        if (!evt.Date.IsKnown)
            throw new InvalidOperationException($"an event needs a date: {evt.Describe()}");
        if (evt.Date.Round < Cursor)
            throw new InvalidOperationException(
                $"cannot record an event at round {evt.Date.Round}, before the cursor at {Cursor}: {evt.Describe()}");

        if (_entries.Count > 0 && evt.Date.Round < _entries[^1].Event.Date.Round) _sorted = false;
        _entries.Add((evt, _nextSeq++));
        _view = null;

        foreach (var info in evt.Involved)
            if (info.Scope == Scope && !info.Events.Contains(evt)) info.Events.Add(evt);
    }

    /// <summary>Every event, in order.</summary>
    public IReadOnlyList<HistoricEvent> Events
    {
        get
        {
            if (_view != null) return _view;
            if (!_sorted)
            {
                _entries.Sort((a, b) =>
                {
                    int c = a.Event.Date.Round.CompareTo(b.Event.Date.Round);
                    return c != 0 ? c : a.Seq.CompareTo(b.Seq);
                });
                _sorted = true;
            }
            return _view = _entries.Select(e => e.Event).ToList();
        }
    }

    // The ordered view, rebuilt only after a Record.
    private List<HistoricEvent>? _view;

    public IEnumerable<HistoricEvent> Between(int fromRound, int toRound)
        => Events.Where(e => e.Date.Round >= fromRound && e.Date.Round <= toRound);
}
