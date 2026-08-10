using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Dialogue outcome that clears the enemy flag on the NPC for the given party member.
/// Used after a successful reconciliation.
/// </summary>
public class ClearEnemyOutcome : Outcome
{
    public ClearEnemyOutcome()
        : base("NPC is no longer considered an enemy", OutcomeSeverity.Positive, "") { }


    
    // Ordinary Outcome, like every other consequence. The chip text is settled in Apply because a
    // conversation's effect only knows its own wording once it has seen this NPC's before/after
    // state; ShowInUI stays false when nothing actually changed, which is what returning null used
    // to mean. Trees hand out a fresh set per access precisely so this per-conversation state is safe.
    public override bool ShowInUI => Reported;

    public override void Apply(OutcomeContext ctx)
    {
        var npc = ctx.Npc!;
        if (!npc.AffinityTable.IsEnemy(ctx.PartyMemberId!)) return;
        npc.AffinityTable.ClearEnemy(ctx.PartyMemberId!);
        Report($"{npc.DisplayName} no longer counts you an enemy");
    }
}
