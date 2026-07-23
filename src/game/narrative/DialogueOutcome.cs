using System.Collections.Generic;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Outcome that triggers a dialogue with an NPC using a specific dialogue tree.
/// When applied, the narrative controller pauses and enters dialogue mode. Also used as the
/// pending-transition payload the controller hands to dialogue mode when a verb starts a dialogue.
/// </summary>
public class DialogueOutcome : ConcreteOutcome
{
    /// <summary>The NPC to talk to.</summary>
    public NpcEntity Target { get; }

    /// <summary>
    /// The dialogue tree ID to use (e.g. "meet_stranger", "strengthen_relationship").
    /// Null when the tree should be resolved at runtime based on current affinity,
    /// or when <see cref="Tree"/> is provided directly.
    /// </summary>
    public string? TreeId { get; init; }

    /// <summary>
    /// A pre-built dialogue tree to use directly, bypassing <see cref="DialogueTreeRegistry"/>.
    /// Takes precedence over <see cref="TreeId"/> when set.
    /// Used for dynamically-created trees such as the "caught red-handed" confrontation.
    /// </summary>
    public DialogueTree? Tree { get; init; }

    /// <summary>
    /// Contextual label substituted for the proper name in the LLM-facing goal phrase; the
    /// human-facing <see cref="DisplayName"/> always keeps the real name.
    /// </summary>
    public string? ContextLabel { get; set; }

    public DialogueOutcome(NpcEntity target, string? treeId = null, DialogueTree? tree = null)
    {
        Target = target;
        TreeId = treeId;
        Tree   = tree;
    }

    public override string DisplayName => $"Talk to {Target.DisplayName}";

    public override string ToNaturalLanguageString()
        => $"engage in conversation with {ContextLabel ?? Target.DisplayName}";

}
