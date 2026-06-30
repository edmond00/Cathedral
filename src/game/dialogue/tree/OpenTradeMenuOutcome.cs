using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Trade;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Dialogue outcome that flags the NPC as having agreed to trade.
/// <see cref="NpcEntity.TradeRequest"/> is set so the game controller can open the
/// buy/sell menu immediately after the dialogue session ends (mirrors <c>FightRequestOutcome</c>).
/// </summary>
public class OpenTradeMenuOutcome : IDialogueOutcome
{
    private readonly TradeMode _mode;

    public OpenTradeMenuOutcome(TradeMode mode) => _mode = mode;

    public string Description => _mode == TradeMode.Buy
        ? "NPC agrees to sell to you"
        : "NPC agrees to buy from you";

    public void Apply(NpcEntity npc, string partyMemberId) => npc.TradeRequest = _mode;
}
