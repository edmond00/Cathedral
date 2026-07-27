using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Trade;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Propose to sell" — the player offers goods to an NPC who buys that category.
/// Success opens the sell menu after the dialogue; failure declines for now.
/// </summary>
public class ProposeToSellTree : DialogueTree
{
    public override string TreeId           => "propose_to_sell";
    public override string DisplayName      => "Propose to Sell";
    public override string Description      => "offering the buyer goods and trying to interest them in a purchase";
    public override string AssociatedVerbId => "propose_to_sell";

    // Success opens the sell menu; a routine bakes in that success so replaying opens trade directly.
    public override DialogueRoutineBehavior RoutineBehavior => DialogueRoutineBehavior.IncludeSuccess;

    public override IReadOnlyList<IDialogueOutcome> SuccessOutcomes { get; } = new IDialogueOutcome[]
    {
        new OpenTradeMenuOutcome(TradeMode.Sell),
    };

    public override IReadOnlyList<IDialogueOutcome> FailureOutcomes { get; } = System.Array.Empty<IDialogueOutcome>();

    private static ResolutionNode Haggle(string id, string success, string failure) => new(
        nodeId:         id,
        difficulty:     1,
        successReplica: success,
        failureReplica: failure);

    private static readonly ResolutionNode OfferOutcome = Haggle(
        "offer_outcome",
        "Let's see what you've got, then. I'll not turn away a fair deal.",
        "I'm not buying today. Keep your goods.");

    private static readonly ResolutionNode FlatterOutcome = Haggle(
        "flatter_outcome",
        "Hah, well — a silver tongue earns a look, at least. Show me.",
        "Save the sweet talk. I've no need of your wares.");

    private static readonly NpcLineNode Opening = new(
        nodeId:  "opening",
        replica: "Aye, {you:name}? What is it you're carrying?",
        new PlayerOption("offer_goods", "plainly offer what you have to sell",
            "I've goods you might want to buy, {npc:name}.", OfferOutcome),
        new PlayerOption("flatter", "appeal to their needs to warm them to a purchase",
            "You look like someone who could use fine wares like these.", FlatterOutcome));

    public override NpcLineNode EntryNode => Opening;

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
