using System;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// The one door into the run's <see cref="Journal"/>. Everything the journal records is written at the
/// point it is finalised — a block committed to the narration, a reply picked, a wound laid, a companion
/// gone — and those points are scattered across the controllers, the fight resolver and the party. So
/// they call here, and this knows the two things none of them should have to: which journal is live,
/// and where the party is.
///
/// <para><b>What reaches it, and what does not.</b> Narration is taken from
/// <c>NarrationScrollBuffer.BlockAdded</c>, which fires only for blocks the player was shown (a preview
/// part that was reset never commits). Dialogue options are skipped there and the picked reply arrives
/// separately, so the replies left unpicked are never recorded. Chips are taken from a shown block's
/// <c>OutcomeReports</c>, filtered on <c>ShowInUI</c> — never from <c>Outcome.ApplyTo</c>, which also
/// runs for outcomes nobody saw.</para>
/// </summary>
public static class JournalRecorder
{
    /// <summary>The protagonist whose journal is live; null before a run. Set by the game controller.</summary>
    public static Func<Protagonist?>? Source { get; set; }

    private static Protagonist? Protagonist => Source?.Invoke();

    /// <summary>Where the party is: location vertex, its name, the area. Set by the game controller.</summary>
    public static Func<(int LocationId, string Place, string Area)>? Where { get; set; }

    private static Journal? Journal => Protagonist?.Journal;

    private static JournalEntry? Add(JournalEntryKind kind, string text, PartyMember? actor,
                                     (int, string, string)? where = null)
    {
        var j = Journal;
        if (j == null || string.IsNullOrWhiteSpace(text)) return null;
        var (loc, place, area) = where ?? Where?.Invoke() ?? (-1, "", "");
        actor ??= Protagonist;
        bool companion = actor != null && !ReferenceEquals(actor, Protagonist);
        return j.Add(kind, text, actor?.DisplayName ?? "", companion, loc, place, area);
    }

    /// <summary>A block the player was shown, with whoever was acting when it was committed.</summary>
    public static void Narration(NarrationBlock block, PartyMember? actor)
    {
        if (block.Type == NarrationBlockType.DialogueOptions) return;   // the pick arrives on its own

        if (block.Type is NarrationBlockType.Speaking or NarrationBlockType.PlayerSpeaking)
        {
            // An NPC's line arrives as "the reeve: …", already named the way the player knows them —
            // SpeakerName is the generated proper name, which the player may not have been told.
            int colon = block.Text.IndexOf(": ", StringComparison.Ordinal);
            bool labelled = block.Type == NarrationBlockType.Speaking && block.SpeakerName != null
                         && colon > 0 && colon < 40 && !block.Text.StartsWith('"');
            var e = Add(JournalEntryKind.Dialogue, labelled ? block.Text[(colon + 2)..] : block.Text, actor);
            if (e != null && labelled) { e.Actor = block.Text[..colon]; e.ByCompanion = false; }
        }
        // A block no mind narrated is the game talking — a trade note, the tail of a fight's log
        // ("— Modus mentis learned: …"). The screen shows it; the story is told elsewhere.
        else if (block.ModusMentis is not null)
        {
            string text = block.Text;
            // The outcome block opens on its verdict for the screen; the chips say what came of it.
            foreach (var prefix in new[] { "[SUCCESS] ", "[FAILURE] " })
                if (text.StartsWith(prefix, StringComparison.Ordinal)) text = text[prefix.Length..];
            // A bracketed line is a hand-written system note ("[The conversation has ended.]").
            bool systemNote = text.Length >= 2 && text[0] == '[' && text[^1] == ']';
            if (!systemNote)
            {
                var e = Add(JournalEntryKind.Narration, text, actor);
                if (e != null) e.Block = block.Type;
            }
        }

        if (block.OutcomeReports != null)
            foreach (var chip in block.OutcomeReports.Where(r => r.ShowInUI && IsStory(r)))
            {
                // The chip is what the screen showed; the chronicler is given the outcome's own plain
                // phrase instead ("received a knife"), or it copies the chip's "Item received: Knife".
                var e = Add(JournalEntryKind.Fact, chip.Text, actor);
                if (e != null) e.Phrase = chip.Verbatim;
            }
    }

    /// <summary>
    /// Whether a chip says something about the story rather than the bookkeeping. Practice and spent
    /// noetic points are the game's arithmetic, and a lesson or a wound is already a milestone; given
    /// to the chronicler, they came back as "gaining two more experience points" (tools/journal/).
    /// </summary>
    private static bool IsStory(Outcome chip) => chip is not (ModusMentisPracticeOutcome or ModusMentisGrantOutcome
                                                              or SkillAcquisitionOutcome
                                                              or Cathedral.Game.Scene.FocusLostOutcome or WoundInflictionOutcome);

    /// <summary>A line spoken in a conversation: an NPC's, or the reply the player picked.</summary>
    public static void Line(string speaker, string text)
    {
        var e = Add(JournalEntryKind.Dialogue, text, null);
        if (e != null) { e.Actor = speaker; e.ByCompanion = false; }
    }

    /// <summary>
    /// A turn in the run. <paramref name="key"/> names what it is about; a milestone whose key the
    /// journal already holds is not recorded again (a place is first visited once, a lesson first
    /// learned once). An empty key never deduplicates.
    /// </summary>
    public static void Milestone(string text, PartyMember? who = null, string key = "",
                                 (int LocationId, string Place, string Area)? where = null, bool lesson = false)
    {
        var j = Journal;
        if (j == null) return;
        if (key.Length > 0 && j.Entries.Any(e => e.Kind == JournalEntryKind.Milestone && e.Key == key)) return;
        var e = Add(JournalEntryKind.Milestone, text, who, where);
        if (e != null) { e.Key = key; e.Lesson = lesson; }
    }

    /// <summary>True when <paramref name="m"/> travels with the protagonist (or is the protagonist).</summary>
    public static bool IsInParty(PartyMember m) =>
        Protagonist != null && (ReferenceEquals(m, Protagonist) || Protagonist.CompanionParty.Any(c => ReferenceEquals(c, m)));

    /// <summary>A visit ended: what it recorded goes on the chronicler's queue.</summary>
    public static void CloseGroup() => Journal?.CloseGroup();
}
