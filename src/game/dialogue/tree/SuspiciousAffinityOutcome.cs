using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Dialogue outcome that sets the NPC's affinity with the party member to
/// <see cref="AffinityLevel.Suspicious"/> — the post-reconcile wary state.
/// Gives 0 bonus dice (same as Stranger) but signals the relationship is no longer hostile.
///
/// <para><b>Only when there was real hostility to come down from.</b> Suspicious sits off the
/// ladder at 0 bonus dice, so imposing it on somebody who merely found you annoying (1 die) is a
/// success that leaves the player worse off than before they spoke — which is how a won
/// reconciliation came to read as "Annoying Acq. → Suspicious", a downgrade dressed as a win. With
/// <paramref name="onlyWhenHostile"/> set, a non-enemy is stepped one rung UP the ordinary ladder
/// instead, which is what mending an irritation actually is.</para>
/// </summary>
public class SuspiciousAffinityOutcome : Outcome
{
    private readonly bool _onlyWhenHostile;

    /// <param name="onlyWhenHostile">
    /// When true, the wary state is imposed only on an NPC who currently counts the party member an
    /// enemy; anyone else is incremented one affinity step instead. Callers passing true must apply
    /// this outcome BEFORE any <see cref="ClearEnemyOutcome"/> in the same set — the flag it reads is
    /// the one that outcome removes.
    /// </param>
    public SuspiciousAffinityOutcome(bool onlyWhenHostile = false)
        : base("affinity becomes suspicious", OutcomeSeverity.Neutral, "")
    {
        _onlyWhenHostile = onlyWhenHostile;
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

        if (_onlyWhenHostile && !npc.AffinityTable.IsEnemy(ctx.PartyMemberId!))
        {
            npc.AffinityTable.Adjust(ctx.PartyMemberId!, +1);
            ReportAffinity(npc, before, npc.AffinityTable.GetLevel(ctx.PartyMemberId!));
            return;
        }

        npc.AffinityTable.SetLevel(ctx.PartyMemberId!, AffinityLevel.Suspicious);
        ReportAffinity(npc, before, AffinityLevel.Suspicious);
    }
}
