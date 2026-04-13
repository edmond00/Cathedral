using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Sets the NPC's affinity with the party member to a fixed <see cref="AffinityLevel"/>.
/// Used in the "Meet Stranger" tree to establish the first relationship.
/// </summary>
public class AffinityTransitionOutcome : IDialogueOutcome
{
    private readonly AffinityLevel _targetLevel;

    public AffinityTransitionOutcome(AffinityLevel targetLevel) => _targetLevel = targetLevel;

    public string Description => $"affinity becomes {_targetLevel.ToShortLabel()}";

    public void Apply(NpcEntity npc, string partyMemberId)
        => npc.AffinityTable.SetLevel(partyMemberId, _targetLevel);
}
