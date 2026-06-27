using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative.Routines;

namespace Cathedral.Game.Narrative;

/// <summary>
/// The player-controlled protagonist.  Extends <see cref="PartyMember"/> with features
/// that are exclusive to the protagonist: journal, companion party list, and location tracking.
///
/// Shared state (body, organs, modiMentis, inventory, …) lives in <see cref="PartyMember"/>.
/// </summary>
public class Protagonist : PartyMember
{
    // ── Protagonist-only data ────────────────────────────────────

    /// <summary>Journal entries written throughout the journey.</summary>
    public List<string> JournalEntries { get; set; } = new();

    /// <summary>Named companions travelling with the protagonist.</summary>
    public List<Companion> CompanionParty { get; set; } = new();

    /// <summary>Current location on the world sphere (used as RNG seed).</summary>
    public int CurrentLocationId { get; set; }

    /// <summary>
    /// Accumulated in-game time in hours, advanced by travel. Serves as the global clock used to
    /// timestamp item depletion and decide when depleted resources have regenerated.
    /// </summary>
    public double GameTimeHours { get; set; }

    /// <summary>
    /// The protagonist's childhood biography, populated during the childhood reminescence
    /// phase as fragments are remembered. Empty at run start.
    /// </summary>
    public ChildhoodHistory ChildhoodHistory { get; } = new();

    /// <summary>
    /// Learned routines, oldest first. A FIFO queue whose capacity is the anamnesis-derived
    /// <c>routine_queue_size</c> stat. Locked routines are protected from eviction.
    /// </summary>
    public List<Routine> RecordedRoutines { get; set; } = new();

    /// <summary>Maximum number of routines that can be held, from the anamnesis derived stat.</summary>
    public int GetRoutineQueueSize()
    {
        var stat = DerivedStats.FirstOrDefault(s => s.Name == "routine_queue_size");
        return stat?.GetValue(this) ?? 10;
    }

    /// <summary>
    /// Records a routine into the FIFO queue. When the queue is full, the oldest UNLOCKED routine is
    /// evicted to make room; if every routine is locked, the new routine is discarded.
    /// </summary>
    public void RecordRoutine(Routine routine)
    {
        int size = GetRoutineQueueSize();
        if (RecordedRoutines.Count < size)
        {
            RecordedRoutines.Add(routine);
            return;
        }

        int oldestUnlocked = RecordedRoutines.FindIndex(r => !r.Locked);
        if (oldestUnlocked < 0) return; // queue full and all locked → discard new routine

        RecordedRoutines.RemoveAt(oldestUnlocked);
        RecordedRoutines.Add(routine);
    }

    // ── PartyMember abstract ─────────────────────────────────────
    public override string DisplayName => "Protagonist";

    // ── Constructor ──────────────────────────────────────────────
    public Protagonist() : base(SpeciesRegistry.Human)
    {
        // No test equipment, no starter modus mentis: the protagonist starts the run with
        // only the ChildhoodReminescence MM (granted explicitly when entering the reminescence
        // phase) and an empty inventory. Items and skills are acquired via REMEMBER actions.
    }

}
