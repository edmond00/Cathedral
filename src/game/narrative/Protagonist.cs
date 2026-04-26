using System.Collections.Generic;

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
    /// The protagonist's childhood biography, populated during the childhood reminescence
    /// phase as fragments are remembered. Empty at run start.
    /// </summary>
    public ChildhoodHistory ChildhoodHistory { get; } = new();

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
