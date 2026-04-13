using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// An effect applied when a terminal dialogue node resolves.
/// </summary>
public interface IDialogueOutcome
{
    /// <summary>Natural-language description used in LLM prompts and debug output.</summary>
    string Description { get; }

    /// <summary>Applies the outcome to the given NPC relative to the speaking party member.</summary>
    void Apply(NpcEntity npc, string partyMemberId);
}
