using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Trade;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Dialogue outcome that flags the NPC as having agreed to trade.
/// <see cref="NpcEntity.TradeRequest"/> is set so the game controller can open the
/// buy/sell menu immediately after the dialogue session ends (mirrors <c>FightRequestOutcome</c>).
/// </summary>
public class OpenTradeMenuOutcome : Outcome
{
    private readonly TradeMode _mode;

    public OpenTradeMenuOutcome(TradeMode mode)
        : base("NPC agrees to trade", OutcomeSeverity.Positive, "") => _mode = mode;


    
    // Ordinary Outcome, like every other consequence. The chip text is settled in Apply because a
    // conversation's effect only knows its own wording once it has seen this NPC's before/after
    // state; ShowInUI stays false when nothing actually changed, which is what returning null used
    // to mean. Trees hand out a fresh set per access precisely so this per-conversation state is safe.
    public override bool ShowInUI => Reported;

    public override void Apply(OutcomeContext ctx)
    {
        var npc = ctx.Npc!;
        npc.TradeRequest = _mode;
        Report(_mode == TradeMode.Buy
                ? $"{npc.DisplayName} agrees to sell to you"
                : $"{npc.DisplayName} agrees to buy from you");
    }
}
