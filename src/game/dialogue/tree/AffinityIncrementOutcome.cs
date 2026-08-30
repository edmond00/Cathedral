using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Adjusts the NPC's affinity with the party member by one step up or down,
/// clamped to the given min/max boundaries. A delta of 0 is a declared no-op — the conversation
/// happened and left the relationship exactly where it was — and reports nothing.
/// Used in the "Strengthen Relationship" and "Gather Knowledge" trees.
/// </summary>
public class AffinityIncrementOutcome : Outcome
{
    private readonly int _delta;           // +1 or -1
    private readonly AffinityLevel _min;
    private readonly AffinityLevel _max;

    public AffinityIncrementOutcome(
        int delta,
        AffinityLevel min = AffinityLevel.AnnoyingAcquaintance,
        AffinityLevel max = AffinityLevel.CloseFriend)
        : base(delta == 0 ? "" : delta > 0 ? "affinity increases one step" : "affinity decreases one step",
               delta > 0 ? OutcomeSeverity.Positive : OutcomeSeverity.Negative, "")
    {
        _delta = delta;
        _min   = min;
        _max   = max;
    }


    
    // Ordinary Outcome, like every other consequence. The chip text is settled in Apply because a
    // conversation's effect only knows its own wording once it has seen this NPC's before/after
    // state; ShowInUI stays false when nothing actually changed, which is what returning null used
    // to mean. Trees hand out a fresh set per access precisely so this per-conversation state is safe.
    public override bool ShowInUI => Reported;

    protected override void Apply(OutcomeContext ctx)
    {
        var npc = ctx.Npc!;
        var before = npc.AffinityTable.GetLevel(ctx.PartyMemberId!);
        npc.AffinityTable.Adjust(ctx.PartyMemberId!, _delta, _min, _max);
        // The step can be a no-op at the clamp boundary, in which case AffinityChange returns null
        // and no chip is shown — which is the honest report.
        ReportAffinity(ctx.Npc!, before, npc.AffinityTable.GetLevel(ctx.PartyMemberId!));
    }
}
