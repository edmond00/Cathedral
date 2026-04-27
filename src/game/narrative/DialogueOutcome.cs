using System.Collections.Generic;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Outcome that triggers a dialogue with an NPC using a specific dialogue tree.
/// When applied, the narrative controller pauses and enters dialogue mode.
/// Extends ConcreteOutcome so it can appear as a sub-outcome inside NpcObservationObject.
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

    public DialogueOutcome(NpcEntity target, string? treeId = null, DialogueTree? tree = null)
    {
        Target = target;
        TreeId = treeId;
        Tree   = tree;
    }

    public override string DisplayName => $"Talk to {Target.DisplayName}";

    public override string ToNaturalLanguageString()
        => $"engage in conversation with {Target.DisplayName}";

}
