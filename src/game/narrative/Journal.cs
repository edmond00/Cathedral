using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>What a journal entry records.</summary>
public enum JournalEntryKind
{
    /// <summary>A block of narration the player read.</summary>
    Narration,
    /// <summary>A line spoken in a conversation — the NPC's, or the reply the player picked.</summary>
    Dialogue,
    /// <summary>An outcome chip: what an act changed, as the chip put it.</summary>
    Fact,
    /// <summary>A turn in the run worth a line of its own: a lesson, a wound, a companion, a place, a fight.</summary>
    Milestone,
}

/// <summary>
/// Where an entry stands with the chronicler. Milestones are never rewritten and stay
/// <see cref="Raw"/>; everything else moves Raw → Pending (its group has closed and waits in the
/// queue) → Written (the group's paragraph exists).
/// </summary>
public enum JournalEntryState { Raw, Pending, Written }

/// <summary>
/// One thing the run's journal remembers. Plain data with public setters, saved as-is in
/// <c>PartyState.Journal</c> — the same treatment as a mental map.
///
/// <para><b>A paragraph belongs to a group, and is stored on the group's first entry.</b> The
/// chronicler turns a whole group (a visit) into one paragraph; <see cref="Written"/> carries it on the
/// first entry, and the rest of the group (milestones aside) is then removed (see
/// <c>Journal.Compact</c>): the paragraph is what the journal keeps, and the source is the bulk of
/// the save.</para>
/// </summary>
public sealed class JournalEntry
{
    public int Seq { get; set; }
    public JournalEntryKind Kind { get; set; }
    /// <summary>Whole days on the calendar when it happened (<see cref="GameClock.Days"/>, floored).</summary>
    public int Day { get; set; }
    /// <summary>The location's name, or empty on the world map.</summary>
    public string Place { get; set; } = "";
    /// <summary>The area inside it, or empty.</summary>
    public string Area { get; set; } = "";
    public int LocationId { get; set; } = -1;
    /// <summary>Who acted: the protagonist's name, or a companion's. For dialogue, who spoke.</summary>
    public string Actor { get; set; } = "";
    /// <summary>A companion carried this passage (the Speak About hand-off).</summary>
    public bool ByCompanion { get; set; }
    /// <summary>The text as the player saw it. Emptied once the group's paragraph is written.</summary>
    public string Source { get; set; } = "";
    /// <summary>The chronicler's paragraph for this entry's group, on its first entry only.</summary>
    public string Written { get; set; } = "";
    public JournalEntryState State { get; set; }
    /// <summary>The chronicler's unit of work: one visit. Milestones carry the group they fell in.</summary>
    public int Group { get; set; }
    /// <summary>For narration, which kind of block it was (an observation, a thought, an outcome…).</summary>
    public NarrationBlockType? Block { get; set; }
    /// <summary>For a milestone, what it is about, so the same lesson or place is not announced twice.</summary>
    public string Key { get; set; } = "";
    /// <summary>
    /// For a fact, the outcome's plain first-person phrase ("received a knife") — what the chronicler is
    /// told, where <see cref="Source"/> keeps the chip as the screen showed it ("Item received: Knife").
    /// </summary>
    public string Phrase { get; set; } = "";
    /// <summary>
    /// A milestone that is a modus mentis learned. Shown in the journal, but not given to the
    /// chronicler: a lesson's name is the game's vocabulary, and it came back as "learning social
    /// interaction" in the prose.
    /// </summary>
    public bool Lesson { get; set; }
    /// <summary>
    /// For an action's outcome: the act in plain words ("catch the crab"), or empty. With
    /// <see cref="Succeeded"/>, what the chronicler is told happened — the narration beside it is the
    /// acting mind's musing on it.
    /// </summary>
    public string Attempt { get; set; } = "";
    public bool Succeeded { get; set; }
}

/// <summary>
/// The protagonist's journal: an append-only list of <see cref="JournalEntry"/> grouped by visit, and
/// the queue of closed groups awaiting the chronicler — which is not a separate structure but the
/// entries in state <see cref="JournalEntryState.Pending"/>, so saving the entries saves the queue.
/// </summary>
public sealed class Journal
{
    public List<JournalEntry> Entries { get; set; } = new();
    public int NextGroup { get; set; } = 1;

    /// <summary>
    /// Source text kept, in characters, across groups not yet written. When the chronicler never runs
    /// (no model, or a run outpacing it) the oldest pending groups lose their narration source past this,
    /// keeping their facts and milestones — so the journal stays readable and the save bounded.
    /// </summary>
    public const int MaxPendingSourceChars = 200_000;

    /// <summary>The longest a single source may be — a narration block is a few hundred characters.</summary>
    public const int MaxSourceChars = 2_000;

    private int CurrentGroup => NextGroup;

    public JournalEntry Add(JournalEntryKind kind, string source, string actor, bool byCompanion,
                            int locationId, string place, string area)
    {
        source = (source ?? "").Trim();
        if (source.Length > MaxSourceChars) source = source[..MaxSourceChars];
        var e = new JournalEntry
        {
            Seq = Entries.Count == 0 ? 1 : Entries[^1].Seq + 1,
            Kind = kind,
            Day = (int)Math.Floor(GameClock.Days),
            Place = place ?? "",
            Area = area ?? "",
            LocationId = locationId,
            Actor = actor ?? "",
            ByCompanion = byCompanion,
            Source = source,
            State = JournalEntryState.Raw,
            Group = CurrentGroup,
        };
        Entries.Add(e);
        return e;
    }

    /// <summary>
    /// Ends the current group (a visit ended): its entries go on the queue, unless it holds nothing the
    /// chronicler could write about, and the next entry opens a new group.
    /// </summary>
    public void CloseGroup()
    {
        var open = Entries.Where(e => e.Group == CurrentGroup).ToList();
        if (open.Count == 0) return;
        NextGroup++;
        bool writable = open.Any(e => e.Kind != JournalEntryKind.Milestone && e.Source.Length > 0);
        if (!writable) return;
        foreach (var e in open)
            if (e.Kind != JournalEntryKind.Milestone) e.State = JournalEntryState.Pending;
        TrimPendingSource();
    }

    /// <summary>The oldest group waiting for the chronicler, or null.</summary>
    public int? NextPendingGroup() =>
        Entries.FirstOrDefault(e => e.State == JournalEntryState.Pending)?.Group;

    public IReadOnlyList<JournalEntry> GroupEntries(int group) =>
        Entries.Where(e => e.Group == group).ToList();

    public int PendingGroupCount => Entries.Where(e => e.State == JournalEntryState.Pending)
                                           .Select(e => e.Group).Distinct().Count();

    /// <summary>
    /// Stores the paragraph for a group, all at once. Called only with a whole paragraph — a cancelled
    /// job never reaches here, so a half-written one is never kept.
    /// </summary>
    public void Write(int group, string paragraph)
    {
        var members = Entries.Where(e => e.Group == group && e.Kind != JournalEntryKind.Milestone).ToList();
        if (members.Count == 0) return;
        members[0].Written = paragraph.Trim();
        foreach (var e in members) e.State = JournalEntryState.Written;
        Compact(group);
    }

    /// <summary>
    /// A written group keeps its paragraph and its milestones; the entries the paragraph was written
    /// from are removed. They are the bulk of the save (~250 bytes of JSON each before any text) and
    /// nothing reads them once the paragraph exists. The carrier keeps its kind and place for the header.
    /// </summary>
    private void Compact(int group)
    {
        var carrier = Entries.First(e => e.Group == group && e.Written.Length > 0);
        carrier.Source = "";
        Entries.RemoveAll(e => e.Group == group && e.Kind != JournalEntryKind.Milestone && !ReferenceEquals(e, carrier));
    }

    private void TrimPendingSource()
    {
        int total = Entries.Where(e => e.State == JournalEntryState.Pending).Sum(e => e.Source.Length);
        foreach (var e in Entries)
        {
            if (total <= MaxPendingSourceChars) break;
            if (e.State != JournalEntryState.Pending || e.Kind != JournalEntryKind.Narration || e.Source.Length == 0) continue;
            total -= e.Source.Length;
            e.Source = "";
        }
    }

    /// <summary>A deep copy, for the save's capture-not-share rule.</summary>
    public Journal Clone() => new()
    {
        NextGroup = NextGroup,
        Entries = Entries.Select(e => new JournalEntry
        {
            Seq = e.Seq, Kind = e.Kind, Day = e.Day, Place = e.Place, Area = e.Area, LocationId = e.LocationId,
            Actor = e.Actor, ByCompanion = e.ByCompanion, Source = e.Source, Written = e.Written,
            State = e.State, Group = e.Group, Block = e.Block, Key = e.Key, Phrase = e.Phrase, Lesson = e.Lesson,
            Attempt = e.Attempt, Succeeded = e.Succeeded,
        }).ToList(),
    };
}
