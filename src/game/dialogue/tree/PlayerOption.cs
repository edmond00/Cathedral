namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// One authored player reply hanging off an <see cref="NpcLineNode"/>. A player option is not
/// sampled — the tree author writes it explicitly. At runtime a sampled speaking Modus Mentis
/// grades the available options by their <see cref="Intent"/> and, if it picks this one, re-says
/// <see cref="Replica"/> in its own voice to produce a shown choice.
///
/// <para>
/// The Modus Mentis that voices a chosen option contributes its level to the branch's accumulated
/// dice pool (see the controller). Two Modi Mentis may pick the same option and word it differently;
/// both then advance to the same <see cref="Next"/> node.
/// </para>
/// </summary>
public class PlayerOption
{
    /// <summary>Identifier, unique within its parent node (used for viewer/debug and dedup).</summary>
    public string OptionId { get; }

    /// <summary>
    /// Short, simplified intent tag used for Modus Mentis grading — e.g. "greeting", "ask who they
    /// are", "haggle". This is what the persona evaluator sees, NOT the neutral replica text.
    /// </summary>
    public string Intent { get; }

    /// <summary>
    /// What the player says, as plain spoken words — "Who are you?". This is the line the voicing
    /// Modus Mentis re-says in its own voice, so it carries content only. See "Authoring the neutral
    /// text" on <see cref="DialogueTree"/>.
    /// </summary>
    public string Replica { get; }

    /// <summary>
    /// The same line as an indirect-speech report from the player's side — "I ask them who they are".
    /// Not used at runtime today; kept alongside <see cref="Replica"/> because the two have swapped
    /// roles before and the pair is cheaper to keep than to re-derive. See
    /// <see cref="NpcLineNode.ReplicaIndirect"/>.
    /// </summary>
    public string ReplicaIndirect { get; }

    /// <summary>Where choosing this option leads: another <see cref="NpcLineNode"/> or a <see cref="ResolutionNode"/>.</summary>
    public DialogueNode Next { get; }

    public PlayerOption(string optionId, string intent, string replica, string replicaIndirect, DialogueNode next)
    {
        OptionId        = optionId;
        Intent          = intent;
        Replica         = replica;
        ReplicaIndirect = replicaIndirect;
        Next            = next;
    }
}
