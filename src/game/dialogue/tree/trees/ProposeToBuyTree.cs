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
///
/// <para>
/// Every replica is the spoken line, plainly — the trader's patter is the persona's to add. See
/// "Authoring the neutral text" on <see cref="DialogueTree"/>.
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
    private static ResolutionNode End(string id, int depth,
                                      string success, string successIndirect,
                                      string failure, string failureIndirect) => new(
        nodeId:                 id,
        difficulty:             BranchDifficulty.Easy(depth),
        successReplica:         success,
        successReplicaIndirect: successIndirect,
        failureReplica:         failure,
        failureReplicaIndirect: failureIndirect);

    // ══════════════════════════════════════════════════════════════════════════
    //  A — ask plainly what they have (deepest)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode AskWares() => new(
        nodeId:          "ask_wares",
        replica:         "{npc:sells}, chiefly. {npc:wares}, and things of that kind. What are you after?",
        replicaIndirect: "I tell {you:name} I deal chiefly in {npc:sells} — {npc:wares} and such — and ask what they are after.",
        replicaHeard:    "{npc:name} says they deal chiefly in {npc:sells}, names {npc:wares}, and asks what I am after.",

        new PlayerOption("wares_just_looking", "admit you are only looking for now",
            "Nothing in particular yet. I would see what there is first.",
            "I tell {npc:name} I would see what there is first.",
            End("wares_looking_end", 2,
                "Take a look, then. I charge fair prices.",
                "I tell {you:name} to take a look, and that I charge fair prices.",
                "There is nothing here for you today. Move along.",
                "I tell {you:name} there is nothing here for them today.")),

        new PlayerOption("wares_ask_best", "ask which of it is their best work",
            "Which of it is the best you have made? Not the dearest, the best.",
            "I ask {npc:name} which of it is the best they have made, not the dearest.",
            WaresBest()),

        new PlayerOption("wares_ask_lasting", "ask which of it will actually last",
            "Which of it will still be sound in five years? That is what I would buy.",
            "I ask {npc:name} which of it will still be sound in five years.",
            WaresLasting()));

    private static NpcLineNode WaresBest() => new(
        nodeId:          "wares_best",
        replica:         "Nobody asks it that way. There is one piece I would not have sold at all if the winter had been easier.",
        replicaIndirect: "I tell {you:name} there is one piece I would not have sold at all if the winter had been easier.",
        replicaHeard:    "{npc:name} says there is one piece they would not have sold at all if the winter had been easier.",

        new PlayerOption("best_ask_see", "ask to be shown it",
            "Show me that one, and we will talk about the rest afterwards.",
            "I ask {npc:name} to show me that one first.",
            End("best_see_end", 3,
                "You have an eye for it. Everything is out. Come and look.",
                "I tell {you:name} they have an eye for it, and that everything is out.",
                "It is not for showing to people who will not buy. Off with you.",
                "I tell {you:name} it is not for showing to people who will not buy.")),

        new PlayerOption("best_say_understand", "say you understand not wanting to part with it",
            "Then I will not press you on that one. A maker may keep something back.",
            "I tell {npc:name} a maker may keep something back, and that I will not press them.",
            End("best_keep_end", 3,
                "That is a decent thing to say to a tradesman. Come and see what I will sell.",
                "I tell {you:name} that is a decent thing to say to a tradesman, and to come and see what I will sell.",
                "Do not tell me what I may do. Look or leave.",
                "I tell {you:name} not to tell me what I may do.")));

    private static NpcLineNode WaresLasting() => new(
        nodeId:          "wares_lasting",
        replica:         "That is the right question, and few ask it. {npc:opinion_work} Some of what is here will outlast you, and some of it is what people can afford.",
        replicaIndirect: "I tell {you:name} few ask that, that of the work I think {npc:opinion_work}, and that some of the stock will outlast them and some is what people can afford.",
        replicaHeard:    "{npc:name} says few ask that, tells me what they think of the work, and says some of the stock will outlast me and some is what people can afford.",

        new PlayerOption("lasting_want_good", "say you would rather pay once for the good sort",
            "Then I would rather pay once and be done. Show me the sort that lasts.",
            "I tell {npc:name} I would rather pay once, and ask for the sort that lasts.",
            End("lasting_good_end", 3,
                "Then I will not waste your time with the cheap end. Come.",
                "I tell {you:name} I will not waste their time with the cheap end.",
                "Everyone says that until they hear the price. Come back with the coin.",
                "I tell {you:name} everyone says that until they hear the price.")),

        new PlayerOption("lasting_ask_cheap", "ask honestly what the cheap end is like",
            "And the affordable sort. Is it honest work, or is it poor?",
            "I ask {npc:name} whether the affordable sort is honest work or poor.",
            LastingCheap()));

    private static NpcLineNode LastingCheap() => new(
        nodeId:          "lasting_cheap",
        replica:         "You would have me speak against my own stock. It is honest. It is not what I would choose, and I will not hide that.",
        replicaIndirect: "I tell {you:name} the cheap end is honest but not what I would choose, and that I will not hide it.",
        replicaHeard:    "{npc:name} says the cheap end is honest but not what they would choose, and that they will not hide it.",

        new PlayerOption("cheap_thank_honesty", "thank them for not overselling it",
            "That is more honesty than I get at most stalls. Thank you.",
            "I thank {npc:name} for more honesty than I get at most stalls.",
            End("cheap_honesty_end", 4,
                "I would rather sell you the right thing once than the wrong thing twice. Come and look properly.",
                "I tell {you:name} I would rather sell the right thing once than the wrong thing twice.",
                "Honesty does not feed me. Are you buying or admiring?",
                "I ask {you:name} whether they are buying or admiring.")),

        new PlayerOption("cheap_take_it", "say the honest cheap sort is exactly what you need",
            "Then the honest sort is what I need. I have not the coin for better.",
            "I tell {npc:name} the honest sort is what I need, since I have not the coin for better.",
            End("cheap_take_end", 4,
                "There is no shame in knowing your purse. Come, I will see you right.",
                "I tell {you:name} there is no shame in knowing your purse, and that I will see them right.",
                "Then you are browsing, not buying. I have customers.",
                "I tell {you:name} they are browsing, not buying.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  B — praise their craft (rich)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode Flatter() => new(
        nodeId:          "flatter_opening",
        replica:         "You would not be the first to praise me before opening a purse. Go on, I am listening.",
        replicaIndirect: "I tell {you:name} they would not be the first to praise me before opening a purse, but that I am listening.",
        replicaHeard:    "{npc:name} says I would not be the first to praise them before opening a purse, but that they are listening.",

        new PlayerOption("flatter_mean_it", "insist you meant it",
            "I meant it. I have seen worse sold for more.",
            "I tell {npc:name} I meant it, and have seen worse sold for more.",
            End("flatter_mean_end", 2,
                "Go on, then. See what catches your eye.",
                "I tell {you:name} to see what catches their eye.",
                "Flattery will not open my stall. Off with you.",
                "I tell {you:name} that flattery will not open my stall.")),

        new PlayerOption("flatter_name_detail", "point out a specific thing you noticed",
            "The finish on it. That does not come from hurrying.",
            "I tell {npc:name} the finish on it does not come from hurrying.",
            FlatterDetail()),

        new PlayerOption("flatter_ask_learn", "ask where they learned to work like that",
            "Where does a person learn to work like that?",
            "I ask {npc:name} where a person learns to work like that.",
            FlatterLearn()));

    private static NpcLineNode FlatterDetail() => new(
        nodeId:          "flatter_detail",
        replica:         "The finish. Nobody notices the finish. They notice the price and the colour and nothing in between.",
        replicaIndirect: "I tell {you:name} nobody notices the finish, only the price and the colour.",
        replicaHeard:    "{npc:name} says nobody notices the finish, only the price and the colour.",

        new PlayerOption("detail_ask_time", "ask how long that takes",
            "How much of the work is that part?",
            "I ask {npc:name} how much of the work that part is.",
            End("detail_time_end", 3,
                "Half of it, and it earns me nothing, which is why I like being asked. Come, look at the rest.",
                "I tell {you:name} it is half of it and earns me nothing, which is why I like being asked.",
                "Long enough that I would rather be doing it than talking. Buy or go.",
                "I tell {you:name} I would rather be doing it than talking about it.")),

        new PlayerOption("detail_say_worth", "say it is worth paying for",
            "It is worth paying for. I would not haggle you down on that part.",
            "I tell {npc:name} I would not haggle them down on that part.",
            End("detail_worth_end", 3,
                "Then we will do business gladly. Everything is out. Take your time.",
                "I tell {you:name} everything is out, and to take their time.",
                "You will haggle. You all haggle. Spare me the preamble.",
                "I tell {you:name} they will haggle as they all do.")));

    private static NpcLineNode FlatterLearn() => new(
        nodeId:          "flatter_learn",
        replica:         "Years of doing it badly where nobody could see. {npc:labour}. You get good at it or you take another trade.",
        replicaIndirect: "I tell {you:name} I learned it over years of doing it badly where nobody could see, and that my day is {npc:labour}.",
        replicaHeard:    "{npc:name} says they learned it over years of doing it badly where nobody could see, and describes their working day.",

        new PlayerOption("learn_respect", "say that sounds like a hard road",
            "That was a hard way to learn it. It shows in the work.",
            "I tell {npc:name} it was a hard way to learn it, and that it shows in the work.",
            End("learn_respect_end", 3,
                "It does. You have earned a proper look at the stock.",
                "I tell {you:name} they have earned a proper look at the stock.",
                "It was hard. That does not make my prices lower.",
                "I tell {you:name} that a hard road does not make my prices lower.")),

        new PlayerOption("learn_ask_apprentice", "ask whether they have anyone learning it from them",
            "Is anyone learning it from you now?",
            "I ask {npc:name} whether anyone is learning it from them now.",
            End("learn_apprentice_end", 3,
                "One, and slow, and I was slower. Few think to ask. Come, let us trade.",
                "I tell {you:name} there is one and slow, that I was slower, and that few think to ask.",
                "That is my business, not yours. Are you buying?",
                "I tell {you:name} that is my business, and ask whether they are buying.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  C — ask after one particular thing (short)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode AskSpecific() => new(
        nodeId:          "ask_specific",
        replica:         "I deal in {npc:sells} and little else. Tell me what it is for and I will tell you if I have it.",
        replicaIndirect: "I tell {you:name} I deal in {npc:sells} and little else, and ask what it is for.",
        replicaHeard:    "{npc:name} says they deal in {npc:sells} and little else, and asks what it is for.",

        new PlayerOption("specific_for_work", "say it is for work you have coming",
            "It is for work I have coming. I want the right thing, not the nearest thing.",
            "I tell {npc:name} it is for work I have coming, and I want the right thing.",
            End("specific_work_end", 2,
                "The right thing for the work. Come, and I will match you to it.",
                "I tell {you:name} I will match them to the right thing for the work.",
                "Then find a stall that stocks it. This one does not.",
                "I tell {you:name} to find a stall that stocks it.")),

        new PlayerOption("specific_for_road", "say it is for the road ahead",
            "It is for travelling. Something that will not break far from a repair.",
            "I tell {npc:name} it is for travelling, and must not break far from a repair.",
            SpecificRoad()),

        new PlayerOption("specific_dont_know", "admit you are not sure what you need",
            "I am not certain what I need. I would take advice with it.",
            "I tell {npc:name} I am not certain what I need, and ask for advice.",
            End("specific_advice_end", 2,
                "An honest customer. Come, and I will not sell you what you cannot use.",
                "I tell {you:name} they are an honest customer, and that I will not sell them what they cannot use.",
                "I am a tradesman, not a nursemaid. Come back when you know your own mind.",
                "I tell {you:name} I am a tradesman, not a nursemaid.")));

    private static NpcLineNode SpecificRoad() => new(
        nodeId:          "specific_road",
        replica:         "Then you want the plain heavy sort and none of the decorated. {npc:opinion_roads}",
        replicaIndirect: "I tell {you:name} they want the plain heavy sort and none of the decorated, and that of the roads I think {npc:opinion_roads}.",
        replicaHeard:    "{npc:name} says I want the plain heavy sort and none of the decorated, and tells me what they think of the roads.",

        new PlayerOption("road_agree", "agree and ask for the plain and heavy",
            "Plain and heavy suits me. Decoration is for people who stay put.",
            "I tell {npc:name} plain and heavy suits me, since decoration is for people who stay put.",
            End("road_agree_end", 3,
                "I keep that sort at the back. Come.",
                "I tell {you:name} I keep that sort at the back.",
                "Then you want a stall that outfits travellers. This one supplies a village.",
                "I tell {you:name} this stall supplies a village, not travellers.")),

        new PlayerOption("road_ask_advice", "ask what they would take, in your place",
            "If you were the one leaving here, what would you carry?",
            "I ask {npc:name} what they would carry if they were the one leaving.",
            End("road_advice_end", 3,
                "Two things, both plain, both mine. Come, I will put them in your hand.",
                "I tell {you:name} I would take two things, both plain and both mine.",
                "I would not be leaving at all. That is my advice, and it is free.",
                "I tell {you:name} I would not be leaving at all, and that the advice is free.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  D — talk money before goods (short)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode TalkCoin() => new(
        nodeId:          "talk_coin",
        replica:         "Price first. That is either a careful man or a poor one. {npc:opinion_trade}",
        replicaIndirect: "I tell {you:name} asking the price first means a careful man or a poor one, and that of trade I think {npc:opinion_trade}.",
        replicaHeard:    "{npc:name} says asking the price first means a careful man or a poor one, and tells me what they think of trade.",

        new PlayerOption("coin_say_careful", "say you would rather know before you want something",
            "Careful. I would rather know the price before I decide I want the thing.",
            "I tell {npc:name} I would rather know the price before I decide I want the thing.",
            End("coin_careful_end", 2,
                "Sensible. Most decide first and argue afterwards. Come, prices and all.",
                "I tell {you:name} that is sensible, since most decide first and argue afterwards.",
                "Careful or poor, it is the same walk out of my stall.",
                "I tell {you:name} careful or poor is the same walk out of my stall.")),

        new PlayerOption("coin_say_poor", "admit your purse is light",
            "Poor, then. My purse is light and I will not waste your day pretending.",
            "I tell {npc:name} my purse is light and I will not pretend otherwise.",
            CoinPoor()),

        new PlayerOption("coin_haggle_early", "make clear you intend to haggle",
            "And I will tell you now, I mean to argue about every one of them.",
            "I warn {npc:name} I mean to argue about every price.",
            End("coin_haggle_end", 2,
                "At least you say so first. Come, let us argue over the stock.",
                "I tell {you:name} at least they say so first, and to come and argue over the stock.",
                "Then we will save each other the trouble. Good day.",
                "I tell {you:name} we will save each other the trouble.")));

    private static NpcLineNode CoinPoor() => new(
        nodeId:          "coin_poor",
        replica:         "A light purse, said plainly. Half this village pretends otherwise and settles up in promises.",
        replicaIndirect: "I tell {you:name} a light purse said plainly is worth something, since half this village settles up in promises.",
        replicaHeard:    "{npc:name} says a light purse said plainly is worth something, since half the village settles up in promises.",

        new PlayerOption("poor_ask_cheapest", "ask what is within reach",
            "What is within reach of a light purse? I will take the honest answer.",
            "I ask {npc:name} what is within reach of a light purse.",
            End("poor_reach_end", 3,
                "A fair amount, if you are not proud about it. I will show you what you can have.",
                "I tell {you:name} there is a fair amount if they are not proud about it.",
                "Nothing. That is the honest answer. Come back with coin.",
                "I tell {you:name} nothing is, and to come back with coin.")),

        new PlayerOption("poor_offer_later", "offer to come back when you can pay properly",
            "Then I will come back when I can pay you properly. I will not ask for credit.",
            "I tell {npc:name} I will come back when I can pay, and will not ask for credit.",
            End("poor_later_end", 3,
                "Not asking is what gets you offered. Come, we will find something you can take today.",
                "I tell {you:name} not asking is what gets you offered, and that we will find something for today.",
                "Do that. Come back with a heavier purse and we will talk.",
                "I tell {you:name} to come back with a heavier purse.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Entry
    // ══════════════════════════════════════════════════════════════════════════

    private static readonly NpcLineNode Opening = new(
        nodeId:          "opening",
        replica:         "{you:name}. Is there something you are after?",
        replicaIndirect: "I greet {you:name} and ask whether there is something they are after.",
        replicaHeard:    "{npc:name} greets me and asks whether there is something I am after.",

        new PlayerOption("ask_wares", "ask plainly what they have for sale",
            "What goods do you have for sale, {npc:name}?",
            "I ask {npc:name} what goods they have for sale.",
            AskWares()),

        new PlayerOption("flatter", "praise their craft to warm them to a sale",
            "You keep fine craft here.",
            "I tell {npc:name} they keep fine craft here.",
            Flatter()),

        new PlayerOption("ask_specific", "ask whether they have one particular thing",
            "I am after something particular. Have you anything of the sort?",
            "I ask {npc:name} whether they have something particular I am after.",
            AskSpecific()),

        new PlayerOption("talk_coin", "ask about prices before looking at anything",
            "Before I look, what sort of prices are these?",
            "I ask {npc:name} what sort of prices these are, before I look.",
            TalkCoin()));

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
