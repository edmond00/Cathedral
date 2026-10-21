using Cathedral.Game.Narrative;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Records that a trader agreed to take the player to their premises — the shop, barn or shed where
/// their goods are kept, and where alone they trade (<c>TradeGate</c>).
///
/// <para>The introduction's pattern, for a place instead of a person: the outcome can reach the NPC
/// and nothing else, so it raises <see cref="Npc.NpcEntity.LeadToPremisesGranted"/> and the controller
/// does the walking once the conversation closes (<c>NarrativeController.WalkToPremises</c>).</para>
/// </summary>
public class LedToPremisesOutcome : Outcome
{
    public LedToPremisesOutcome()
        : base("the trader agrees to lead the player to their premises", OutcomeSeverity.Positive, "was promised to be led to the trader's premises") { }

    // Worded at apply time, like every conversation outcome: only then is the NPC to hand.
    public override bool ShowInUI => Reported;

    protected override void Apply(OutcomeContext ctx)
    {
        var npc = ctx.Npc!;
        npc.LeadToPremisesGranted = true;
        Report($"{Cathedral.Game.Npc.NpcLabelResolver.ShortLabelCapitalized(npc)} will take you to {npc.Archetype.Workplace}");
    }
}
