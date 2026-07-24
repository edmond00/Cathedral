using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// An effect applied when a terminal dialogue node resolves.
/// </summary>
public interface IDialogueOutcome
{
    /// <summary>Natural-language description used in LLM prompts and debug output.</summary>
    string Description { get; }

    /// <summary>
    /// Applies the outcome to the given NPC relative to the speaking party member, and returns a
    /// chip describing what actually changed — or null when nothing observable did. Reporting from
    /// <c>Apply</c> rather than a separate method is what lets an outcome phrase a transition
    /// ("Stranger → Distant Acq."): only the code doing the change sees both sides of it.
    /// </summary>
    OutcomeReport? Apply(NpcEntity npc, string partyMemberId);
}
