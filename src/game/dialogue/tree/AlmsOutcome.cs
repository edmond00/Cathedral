using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Records that an NPC gave the player a coin or two.
///
/// <para>Sets an amount on the NPC rather than crediting the wallet, because an
/// <see cref="Outcome"/> is handed the NPC and the party-member id and nothing else — no
/// scene, no protagonist, no purse. The controller pays it out when the session closes, the same way
/// it opens the trade menu and the work menu.</para>
///
/// <para>Deliberately small. Begging is meant to keep somebody alive, not to be an income.</para>
/// </summary>
public class AlmsOutcome : Outcome
{
    private readonly int _copper;

    public AlmsOutcome(int copper = 2)
        : base($"the NPC gives {copper} copper", OutcomeSeverity.Positive, "") => _copper = copper;


    
    // Ordinary Outcome, like every other consequence. The chip text is settled in Apply because a
    // conversation's effect only knows its own wording once it has seen this NPC's before/after
    // state; ShowInUI stays false when nothing actually changed, which is what returning null used
    // to mean. Trees hand out a fresh set per access precisely so this per-conversation state is safe.
    public override bool ShowInUI => Reported;

    protected override void Apply(OutcomeContext ctx)
    {
        var npc = ctx.Npc!;
        npc.AlmsGiven = _copper;
        Report($"{npc.DisplayName} gives you {_copper} copper");
    }
}
