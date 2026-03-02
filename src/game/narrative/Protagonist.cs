using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// The player-controlled protagonist.  Extends <see cref="PartyMember"/> with features
/// that are exclusive to the protagonist: journal, companion party list, and location tracking.
///
/// Shared state (body, organs, skills, inventory, …) lives in <see cref="PartyMember"/>.
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

    // ── PartyMember abstract ─────────────────────────────────────
    public override string DisplayName => "Protagonist";
}
