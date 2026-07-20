namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// One authored player reply hanging off an <see cref="NpcLineNode"/>. A player option is not
/// sampled — the tree author writes it explicitly. At runtime a sampled speaking Modus Mentis
/// grades the available options by their <see cref="Intent"/> and, if it picks this one, rewrites
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
    /// The neutral player line (direct speech; may contain <c>{scope:field}</c> template tokens).
    /// Rewritten by the voicing Modus Mentis before it is shown.
    /// </summary>
    public string Replica { get; }

    /// <summary>Where choosing this option leads: another <see cref="NpcLineNode"/> or a <see cref="ResolutionNode"/>.</summary>
    public DialogueNode Next { get; }

    public PlayerOption(string optionId, string intent, string replica, DialogueNode next)
    {
        OptionId = optionId;
        Intent   = intent;
        Replica  = replica;
        Next     = next;
    }
}
