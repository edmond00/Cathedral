using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Dialogue outcome that sets the NPC's affinity with the party member to
/// <see cref="AffinityLevel.Suspicious"/> — the post-reconcile wary state.
/// Gives 0 bonus dice (same as Stranger) but signals the relationship is no longer hostile.
/// </summary>
public class SuspiciousAffinityOutcome : Outcome
{
    public SuspiciousAffinityOutcome()
        : base("affinity becomes suspicious", OutcomeSeverity.Neutral, "") { }


    
    // Ordinary Outcome, like every other consequence. The chip text is settled in Apply because a
    // conversation's effect only knows its own wording once it has seen this NPC's before/after
    // state; ShowInUI stays false when nothing actually changed, which is what returning null used
    // to mean. Trees hand out a fresh set per access precisely so this per-conversation state is safe.
    public override bool ShowInUI => Reported;

    protected override void Apply(OutcomeContext ctx)
    {
        var npc = ctx.Npc!;
        var before = npc.AffinityTable.GetLevel(ctx.PartyMemberId!);
        npc.AffinityTable.SetLevel(ctx.PartyMemberId!, AffinityLevel.Suspicious);
        ReportAffinity(ctx.Npc!, before, AffinityLevel.Suspicious);
    }
}
