using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Trade;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Propose to sell" — the player offers goods to an NPC who buys that category.
///
/// Tree structure:
///   opening (entry)
///     ├─ offer_goods → haggle
///     └─ flatter     → haggle
///   haggle (terminal):
///     ✓ Success → OpenTradeMenuOutcome(Sell)  (the sell menu opens after dialogue)
///     ✗ Failure → nothing (the NPC isn't buying for now)
/// </summary>
public class ProposeToSellTree : DialogueTree
{
    public override string TreeId           => "propose_to_sell";
    public override string DisplayName      => "Propose to Sell";
    public override string Description      => "offering the buyer goods and trying to interest them in a purchase";
    public override string AssociatedVerbId => "propose_to_sell";

    private static readonly DialogueTreeNode Haggle = new(
        nodeId:      "haggle",
        description: "talking up your goods and pressing them to name a price",
        outcomes: new List<DialogueOutcomeCase>
        {
            new(new OpenTradeMenuOutcome(TradeMode.Sell), BranchCondition.Success),
        });

    private static readonly DialogueTreeNode OfferGoods = new(
        nodeId:      "offer_goods",
        description: "plainly offering what you have to sell",
        branches: new List<DialogueBranch> { new(Haggle, BranchCondition.Either) });

    private static readonly DialogueTreeNode Flatter = new(
        nodeId:      "flatter",
        description: "appealing to their needs to warm them to a purchase",
        branches: new List<DialogueBranch> { new(Haggle, BranchCondition.Either) });

    private static readonly DialogueTreeNode Opening = new(
        nodeId:      "opening",
        description: "opening the conversation with an offer to do business",
        branches: new List<DialogueBranch>
        {
            new(OfferGoods, BranchCondition.Either),
            new(Flatter,    BranchCondition.Either),
        });

    public override DialogueTreeNode EntryNode => Opening;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
    {
        if (npc.BuyTag is null) return false;
        if (npc.AffinityTable.IsEnemy(partyMemberId)) return false;
        var level = npc.AffinityTable.GetLevel(partyMemberId);
        return level is AffinityLevel.DistantAcquaintance
                     or AffinityLevel.CloseAcquaintance
                     or AffinityLevel.DistantFriend
                     or AffinityLevel.CloseFriend;
    }
}
