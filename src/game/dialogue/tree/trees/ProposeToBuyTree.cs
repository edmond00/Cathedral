using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Trade;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Propose to buy" — the player asks an NPC merchant to show their wares.
///
/// Tree structure:
///   opening (entry)
///     ├─ ask_wares  → haggle
///     └─ flatter    → haggle
///   haggle (terminal):
///     ✓ Success → OpenTradeMenuOutcome(Buy)  (the buy menu opens after dialogue)
///     ✗ Failure → nothing (the NPC declines for now)
/// </summary>
public class ProposeToBuyTree : DialogueTree
{
    public override string TreeId           => "propose_to_buy";
    public override string DisplayName      => "Propose to Buy";
    public override string Description      => "asking the merchant to show what they have for sale";
    public override string AssociatedVerbId => "propose_to_buy";

    private static readonly DialogueTreeNode Haggle = new(
        nodeId:      "haggle",
        description: "pressing them to open their stock and name their prices",
        outcomes: new List<DialogueOutcomeCase>
        {
            new(new OpenTradeMenuOutcome(TradeMode.Buy), BranchCondition.Success),
        });

    private static readonly DialogueTreeNode AskWares = new(
        nodeId:      "ask_wares",
        description: "asking plainly what goods they have to sell",
        branches: new List<DialogueBranch> { new(Haggle, BranchCondition.Either) });

    private static readonly DialogueTreeNode Flatter = new(
        nodeId:      "flatter",
        description: "praising their craft to warm them to a sale",
        branches: new List<DialogueBranch> { new(Haggle, BranchCondition.Either) });

    private static readonly DialogueTreeNode Opening = new(
        nodeId:      "opening",
        description: "opening the conversation with an offer to do business",
        branches: new List<DialogueBranch>
        {
            new(AskWares, BranchCondition.Either),
            new(Flatter,  BranchCondition.Either),
        });

    public override DialogueTreeNode EntryNode => Opening;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
    {
        if (npc.SellTag is null) return false;
        if (npc.AffinityTable.IsEnemy(partyMemberId)) return false;
        var level = npc.AffinityTable.GetLevel(partyMemberId);
        return level is AffinityLevel.DistantAcquaintance
                     or AffinityLevel.CloseAcquaintance
                     or AffinityLevel.DistantFriend
                     or AffinityLevel.CloseFriend;
    }
}
