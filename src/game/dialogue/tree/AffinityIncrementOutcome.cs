using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Adjusts the NPC's affinity with the party member by one step up or down,
/// clamped to the given min/max boundaries.
/// Used in the "Strengthen Relationship" tree.
/// </summary>
public class AffinityIncrementOutcome : IDialogueOutcome
{
    private readonly int _delta;           // +1 or -1
    private readonly AffinityLevel _min;
    private readonly AffinityLevel _max;

    public AffinityIncrementOutcome(
        int delta,
        AffinityLevel min = AffinityLevel.AnnoyingAcquaintance,
        AffinityLevel max = AffinityLevel.CloseFriend)
    {
        _delta = delta;
        _min   = min;
        _max   = max;
    }

    public string Description => _delta > 0 ? "affinity increases one step" : "affinity decreases one step";

    public void Apply(NpcEntity npc, string partyMemberId)
        => npc.AffinityTable.Adjust(partyMemberId, _delta, _min, _max);
}
