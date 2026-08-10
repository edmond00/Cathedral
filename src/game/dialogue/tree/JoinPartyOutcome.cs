using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Records that an NPC agreed to travel with the player.
///
/// <para>Sets a flag rather than doing the recruiting, exactly as <c>OpenTradeMenuOutcome</c> and
/// <c>OpenJobMenuOutcome</c> do: a dialogue outcome has no scene and no party to reach, so the
/// controller acts on the flag once the conversation closes. That is also why the flag lives on the
/// NPC — it is the NPC's decision, and it survives the session ending.</para>
/// </summary>
public class JoinPartyOutcome : Outcome
{
    public JoinPartyOutcome()
        : base("the NPC agrees to travel with the player", OutcomeSeverity.Positive, "") { }


    
    // Ordinary Outcome, like every other consequence. The chip text is settled in Apply because a
    // conversation's effect only knows its own wording once it has seen this NPC's before/after
    // state; ShowInUI stays false when nothing actually changed, which is what returning null used
    // to mean. Trees hand out a fresh set per access precisely so this per-conversation state is safe.
    public override bool ShowInUI => Reported;

    public override void Apply(OutcomeContext ctx)
    {
        var npc = ctx.Npc!;
        npc.JoinRequested = true;
        Report($"{npc.DisplayName} will travel with you");
    }
}
