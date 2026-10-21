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
        : base($"affinity becomes {targetLevel.ToShortLabel()}", OutcomeSeverity.Neutral, VerbatimFor(targetLevel))
        => _targetLevel = targetLevel;

    /// <summary>
    /// What the new footing was, read after "I " — the occasion an emotion's neutral line names. Left
    /// empty, an emotion about an introduction had nothing to be about, and the persona, its slot still
    /// holding the conversation, carried on addressing the person instead.
    /// </summary>
    private static string VerbatimFor(AffinityLevel level) => level switch
    {
        AffinityLevel.AnnoyingAcquaintance => "made a poor first impression",
        AffinityLevel.DistantAcquaintance
            or AffinityLevel.CloseAcquaintance => "made an acquaintance",
        AffinityLevel.DistantFriend        => "made a friend",
        AffinityLevel.CloseFriend          => "made a close friend",
        AffinityLevel.Suspicious           => "came to be regarded with suspicion",
        _                                  => "",
    };


    
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
