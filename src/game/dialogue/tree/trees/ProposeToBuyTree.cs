using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Trade;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Propose to buy" — the player asks an NPC merchant to show their wares.
/// Success opens the buy menu after the dialogue; failure declines for now.
/// </summary>
public class ProposeToBuyTree : DialogueTree
{
    public override string TreeId           => "propose_to_buy";
    public override string DisplayName      => "Propose to Buy";
    public override string Description      => "asking the merchant to show what they have for sale";
    public override string AssociatedVerbId => "propose_to_buy";

    private static ResolutionNode Haggle(string id, string success, string failure) => new(
        nodeId:         id,
        difficulty:     1,
        successReplica: success,
        failureReplica: failure,
        outcomes: new List<DialogueOutcomeCase>
        {
            new(new OpenTradeMenuOutcome(TradeMode.Buy), BranchCondition.Success),
        });

    private static readonly ResolutionNode AskOutcome = Haggle(
        "ask_outcome",
        "Aye, coin's coin. Take a look, then — fair prices for a fair customer.",
        "Naught here for you today. Move along.");

    private static readonly ResolutionNode FlatterOutcome = Haggle(
        "flatter_outcome",
        "Heh — you've a honeyed tongue. Go on, see what catches your eye.",
        "Flattery won't open my stall. Off with you.");

    private static readonly NpcLineNode Opening = new(
        nodeId:  "opening",
        replica: "Aye, {you:name}? Something you're after?",
        new PlayerOption("ask_wares", "ask plainly what they have for sale",
            "What goods do you have for sale, {npc:name}?", AskOutcome),
        new PlayerOption("flatter", "praise their craft to warm them to a sale",
            "Fine craft you keep here — a pleasure to behold.", FlatterOutcome));

    public override NpcLineNode EntryNode => Opening;

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
