using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Sets the NPC's affinity with the party member to a fixed <see cref="AffinityLevel"/>.
/// Used in the "Meet Stranger" tree to establish the first relationship.
/// </summary>
public class AffinityTransitionOutcome : Outcome
{
    private readonly AffinityLevel _targetLevel;

    public AffinityTransitionOutcome(AffinityLevel targetLevel)
        : base($"affinity becomes {targetLevel.ToShortLabel()}", OutcomeSeverity.Neutral, "")
        => _targetLevel = targetLevel;


    
    // Ordinary Outcome, like every other consequence. The chip text is settled in Apply because a
    // conversation's effect only knows its own wording once it has seen this NPC's before/after
    // state; ShowInUI stays false when nothing actually changed, which is what returning null used
    // to mean. Trees hand out a fresh set per access precisely so this per-conversation state is safe.
    public override bool ShowInUI => Reported;

    protected override void Apply(OutcomeContext ctx)
    {
        var npc = ctx.Npc!;
        var before = npc.AffinityTable.GetLevel(ctx.PartyMemberId!);
        npc.AffinityTable.SetLevel(ctx.PartyMemberId!, _targetLevel);
        ReportAffinity(ctx.Npc!, before, _targetLevel);
    }
}
