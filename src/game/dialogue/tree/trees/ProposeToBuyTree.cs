using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Trade;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Propose to buy" — the player asks an NPC merchant to show their wares.
/// Success opens the buy menu after the dialogue; failure declines for now.
///
/// <para>
/// Four ways to open a sale: ask plainly, flatter the craft, ask after one thing in particular, or
/// talk about money before goods. What is being sold is never hard-coded — <c>{npc:sells}</c> gives
/// the trade in general and <c>{npc:wares}</c> names two or three things actually in this merchant's
/// catalogue, so the haggling is about the goods the trade menu will really offer.
/// </para>
/// </summary>
public class ProposeToBuyTree : DialogueTree
{
    public override string TreeId           => "propose_to_buy";
    public override string DisplayName      => "Propose to Buy";
    public override string Description      => "asking the merchant to show what they have for sale";
    public override string AssociatedVerbId => "propose_to_buy";

    // Success opens the buy menu; a routine bakes in that success so replaying opens trade directly.
    public override DialogueRoutineBehavior RoutineBehavior => DialogueRoutineBehavior.IncludeSuccess;

    public override IReadOnlyList<IDialogueOutcome> SuccessOutcomes { get; } = new IDialogueOutcome[]
    {
        new OpenTradeMenuOutcome(TradeMode.Buy),
    };

    public override IReadOnlyList<IDialogueOutcome> FailureOutcomes { get; } = System.Array.Empty<IDialogueOutcome>();

    /// <summary>A branch end. Opening a stall is not high-stakes, so the easy ladder applies.</summary>
    private static ResolutionNode End(string id, int depth, string success, string failure) => new(
        nodeId:         id,
        difficulty:     BranchDifficulty.Easy(depth),
        successReplica: success,
        failureReplica: failure);

    // ══════════════════════════════════════════════════════════════════════════
    //  A — ask plainly what they have (deepest)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode AskWares() => new(
        nodeId:  "ask_wares",
        replica: "What have I? {npc:sells}, chiefly. {npc:wares} — that sort of thing. What is it you're actually after?",

        new PlayerOption("wares_just_looking", "admit you are only looking for now",
            "Nothing in particular yet. I'd see what there is first.",
            End("wares_looking_end", 2,
                "Aye, coin's coin. Take a look, then — fair prices for a fair customer.",
                "Naught here for you today. Move along.")),

        new PlayerOption("wares_ask_best", "ask which of it is their best work",
            "Which of it is the best you've made? Not the dearest — the best.",
            WaresBest()),

        new PlayerOption("wares_ask_lasting", "ask which of it will actually last",
            "Which of it will still be sound in five years? That's what I'd buy.",
            WaresLasting()));

    private static NpcLineNode WaresBest() => new(
        nodeId:  "wares_best",
        replica: "...Not the dearest. Nobody asks it that way round. There's a piece I'd not have sold at all if the winter had been kinder.",

        new PlayerOption("best_ask_see", "ask to be shown it",
            "Then show me that one, and we'll talk about the rest after.",
            End("best_see_end", 3,
                "Heh — you've an eye, or you've luck. Come on, then. Everything's out.",
                "It's not for showing to people who won't buy. Off with you.")),

        new PlayerOption("best_say_understand", "say you understand not wanting to part with it",
            "Then I'll not press you on that one. A maker's allowed to keep something.",
            End("best_keep_end", 3,
                "...That's a decent thing to say to a tradesman. Aye — come and see what I will sell.",
                "Don't tell me what I'm allowed. Look or leave.")));

    private static NpcLineNode WaresLasting() => new(
        nodeId:  "wares_lasting",
        replica: "Now that's the right question, and most never ask it. {npc:opinion_work} So: some of what's here will outlive you, and some of it is what folk can afford.",

        new PlayerOption("lasting_want_good", "say you would rather pay once for the good sort",
            "Then I'd rather pay once and be done. Show me the sort that lasts.",
            End("lasting_good_end", 3,
                "A customer after my own heart. Come — I'll not waste your time with the cheap end.",
                "Everyone says that until they hear the price. Come back with the coin.")),

        new PlayerOption("lasting_ask_cheap", "ask honestly what the cheap end is like",
            "And the affordable sort — is it honest, or is it rubbish?",
            LastingCheap()));

    private static NpcLineNode LastingCheap() => new(
        nodeId:  "lasting_cheap",
        replica: "...You'd have me speak ill of my own stock. It's honest. It's not what I'd choose. There's a difference and I'll not hide it from you.",

        new PlayerOption("cheap_thank_honesty", "thank them for not overselling it",
            "That's more honesty than I get from most stalls. My thanks.",
            End("cheap_honesty_end", 4,
                "Aye, well. I'd rather sell you the right thing once than the wrong thing twice. Come and look properly.",
                "Honesty doesn't feed me. Are you buying or admiring?")),

        new PlayerOption("cheap_take_it", "say the honest cheap sort is exactly what you need",
            "Then the honest sort is what I need. I've not the coin for better and I'll not pretend I have.",
            End("cheap_take_end", 4,
                "Now there's a man who knows his purse. That's no shame here. Come — I'll see you right.",
                "Then you're browsing, not buying. I've customers.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  B — praise their craft (rich)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode Flatter() => new(
        nodeId:  "flatter_opening",
        replica: "Fine craft, is it. Hm. You'd not be the first to warm me up before opening your purse — but go on, I'm listening.",

        new PlayerOption("flatter_mean_it", "insist you meant it",
            "I meant it. I've seen worse sold for more, and often.",
            End("flatter_mean_end", 2,
                "Heh — you've a honeyed tongue. Go on, see what catches your eye.",
                "Flattery won't open my stall. Off with you.")),

        new PlayerOption("flatter_name_detail", "point out a specific thing you noticed",
            "The finish on it. That's not something you get by hurrying.",
            FlatterDetail()),

        new PlayerOption("flatter_ask_learn", "ask where they learned to work like that",
            "Where does a person learn to work like that?",
            FlatterLearn()));

    private static NpcLineNode FlatterDetail() => new(
        nodeId:  "flatter_detail",
        replica: "...The finish. Aye. Nobody notices the finish. They notice the price and the colour and nothing between.",

        new PlayerOption("detail_ask_time", "ask how long that takes",
            "How long does that part take you, out of the whole?",
            End("detail_time_end", 3,
                "Half of it, and it earns me nothing. Which is why I like being asked. Come, look at the rest.",
                "Long enough that I'd rather be doing it than talking. Buy or go.")),

        new PlayerOption("detail_say_worth", "say it is worth paying for",
            "It's worth paying for. I'd not haggle you down on that part.",
            End("detail_worth_end", 3,
                "...Then you and I will do business happily. Everything's out — take your time.",
                "You'll haggle. You all haggle. Spare me the preamble.")));

    private static NpcLineNode FlatterLearn() => new(
        nodeId:  "flatter_learn",
        replica: "Years of doing it badly where nobody could see. That's where. {npc:labour} — you get good or you get another trade.",

        new PlayerOption("learn_respect", "say that sounds like a hard road",
            "That's a hard road to have walked. It shows in the work.",
            End("learn_respect_end", 3,
                "It does, doesn't it. Alright — you've earned a proper look at the stock.",
                "Aye, it was hard. It doesn't make my prices softer.")),

        new PlayerOption("learn_ask_apprentice", "ask whether they have anyone learning it from them",
            "And is there anyone learning it off you now?",
            End("learn_apprentice_end", 3,
                "One, and slow, and I was slower. It's a fair question and few think to ask it. Come, let's trade.",
                "That's my business, not yours. Are you buying?")));

    // ══════════════════════════════════════════════════════════════════════════
    //  C — ask after one particular thing (short)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode AskSpecific() => new(
        nodeId:  "ask_specific",
        replica: "Something particular. Well — I deal in {npc:sells} and little else. Tell me what it's for and I'll tell you if I've got it.",

        new PlayerOption("specific_for_work", "say it is for work you have coming",
            "It's for work I've got coming. I'd rather have the right thing than the near-enough thing.",
            End("specific_work_end", 2,
                "The right thing for the job. Aye — come and I'll match you to it properly.",
                "Then find a stall that stocks whatever it is. It isn't here.")),

        new PlayerOption("specific_for_road", "say it is for the road ahead",
            "It's for the road. Whatever won't break on me a long way from a repair.",
            SpecificRoad()),

        new PlayerOption("specific_dont_know", "admit you are not sure what you need",
            "Honestly? I'm not certain what I need. I'd take a word of advice with it.",
            End("specific_advice_end", 2,
                "Ha — an honest customer. Come here, then, and I'll not sell you what you can't use.",
                "I'm a tradesman, not a nursemaid. Come back when you know your own mind.")));

    private static NpcLineNode SpecificRoad() => new(
        nodeId:  "specific_road",
        replica: "For the road. Then you want the plain, heavy sort and none of the pretty. {npc:opinion_roads}",

        new PlayerOption("road_agree", "agree and ask for the plain and heavy",
            "Plain and heavy suits me. Fancy is for people who stay put.",
            End("road_agree_end", 3,
                "Ha! Right you are. Come — I keep that sort at the back, where it belongs.",
                "Then you'll want a stall that outfits wanderers. This one supplies a village.")),

        new PlayerOption("road_ask_advice", "ask what they would take, in your place",
            "If you were the one walking out of here, what would you carry?",
            End("road_advice_end", 3,
                "...In your place? Two things, both dull, both mine. Come, I'll put them in your hand.",
                "I'd not be walking out of here at all. That's my advice and it's free.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  D — talk money before goods (short)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode TalkCoin() => new(
        nodeId:  "talk_coin",
        replica: "Price first, is it? That's either a careful man or a poor one. {npc:opinion_trade}",

        new PlayerOption("coin_say_careful", "say you would rather know before you want something",
            "Careful. I'd rather know the price before I've decided I want the thing.",
            End("coin_careful_end", 2,
                "Sensible. Most fall in love with a piece and then argue about it. Come, prices and all.",
                "Careful, poor — it's the same walk out of my stall either way.")),

        new PlayerOption("coin_say_poor", "admit your purse is light",
            "Poor, then. My purse is light and I'll not waste your day pretending.",
            CoinPoor()),

        new PlayerOption("coin_haggle_early", "make clear you intend to haggle",
            "And I'll tell you now — I mean to argue about every one of them.",
            End("coin_haggle_end", 2,
                "Hah! At least you say so up front. Come on then, let's have it out over the stock.",
                "Then we'll save each other the trouble. Good day.")));

    private static NpcLineNode CoinPoor() => new(
        nodeId:  "coin_poor",
        replica: "...Light purse, said plainly. That's not nothing. Half this village pretends otherwise and settles up in promises.",

        new PlayerOption("poor_ask_cheapest", "ask what is within reach",
            "Then what's within reach of a light purse? I'll take the honest answer.",
            End("poor_reach_end", 3,
                "There's a fair bit, if you're not proud about it. Come — I'll show you what's yours to have.",
                "Nothing. That's the honest answer. Come back with coin.")),

        new PlayerOption("poor_offer_later", "offer to come back when you can pay properly",
            "Then I'll come back when I can pay you properly. I'll not ask for credit.",
            End("poor_later_end", 3,
                "...Not asking is what gets you offered. Come here — we'll find something you can carry off today.",
                "Aye, do that. Come back with a heavier purse and we'll talk.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Entry
    // ══════════════════════════════════════════════════════════════════════════

    private static readonly NpcLineNode Opening = new(
        nodeId:  "opening",
        replica: "Aye, {you:name}? Something you're after?",

        new PlayerOption("ask_wares", "ask plainly what they have for sale",
            "What goods do you have for sale, {npc:name}?", AskWares()),

        new PlayerOption("flatter", "praise their craft to warm them to a sale",
            "Fine craft you keep here — a pleasure to behold.", Flatter()),

        new PlayerOption("ask_specific", "ask whether they have one particular thing",
            "I'm after something particular. Have you anything of the sort?", AskSpecific()),

        new PlayerOption("talk_coin", "ask about prices before looking at anything",
            "Before I look — what sort of prices are we talking about?", TalkCoin()));

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
