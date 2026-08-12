using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Trade;

using Cathedral.Game.Narrative;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Propose to sell" — the player offers goods to an NPC who buys that category.
/// Success opens the sell menu after the dialogue; failure declines for now.
///
/// <para>
/// Four ways to open: offer plainly, name what you are actually carrying, flatter their need, or ask
/// what they are short of. <c>{npc:buys}</c> gives the category they deal in and
/// <c>{you:goods}</c> names what is in the player's own pack that this buyer would take — so the
/// pitch is about real inventory, not a generic "some goods".
/// </para>
///
/// <para>
/// Every replica is the spoken line, plainly — the seller's patter and the buyer's suspicion are the
/// personas' to add. See "Authoring the neutral text" on <see cref="DialogueTree"/>.
/// </para>
/// </summary>
public class ProposeToSellTree : DialogueTree
{
    public override string TreeId           => "propose_to_sell";
    public override string DisplayName      => "Propose to Sell";
    public override string Description      => "offering the buyer goods and trying to interest them in a purchase";

    /// <summary>The other chair: you ARE the buyer being offered goods.</summary>
    public override string NpcDescription   => "being offered goods by someone hoping you will buy";
    public override string AssociatedVerbId => "propose_to_sell";

    /// <summary>What succeeding at this conversation teaches: talking a price into being.</summary>
    public override string? GrantedModusMentisId => "bargaining";

    // Success opens the sell menu; a routine bakes in that success so replaying opens trade directly.
    public override DialogueRoutineBehavior RoutineBehavior => DialogueRoutineBehavior.IncludeSuccess;

    public override IReadOnlyList<Outcome> SuccessOutcomes => new Outcome[]
    {
        new OpenTradeMenuOutcome(TradeMode.Sell),
    };

    public override IReadOnlyList<Outcome> FailureOutcomes => System.Array.Empty<Outcome>();

    /// <summary>A branch end. Getting a look at your goods is a low-stakes ask.</summary>
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
    //  A — offer plainly (deepest)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode OfferGoods() => new(
        nodeId:          "offer_goods",
        replica:         "I deal in {npc:buys} and nothing else. Before you unpack, what is it?",
        replicaIndirect: "I tell {you:name} I deal in {npc:buys} and nothing else, and ask what it is before they unpack.",
        replicaHeard:    "{npc:name} says they deal in {npc:buys} and nothing else, and asks what it is before I unpack.",

        new PlayerOption("offer_name_them", "name exactly what you are carrying",
            "{you:goods}. That is what I have.",
            "I tell {npc:name} I have {you:goods}.",
            End("offer_name_end", 2,
                "Let me see it, then. I will not turn away a fair deal.",
                "I ask {you:name} to let me see it, since I will not turn away a fair deal.",
                "I am not buying today. Keep your goods.",
                "I tell {you:name} I am not buying today.")),

        new PlayerOption("offer_say_condition", "speak to the condition of it before the price",
            "It is sound. You will see the state of it for yourself.",
            "I tell {npc:name} it is sound and they will see the state of it themselves.",
            OfferCondition()),

        new PlayerOption("offer_ask_terms", "ask what terms they buy on",
            "What terms do you buy on? I would rather know before I unpack.",
            "I ask {npc:name} what terms they buy on.",
            OfferTerms()));

    private static NpcLineNode OfferCondition() => new(
        nodeId:          "offer_condition",
        replica:         "Everyone's goods are sound until they are on my bench. What is wrong with it? There is always something.",
        replicaIndirect: "I tell {you:name} everyone's goods are sound until they are on my bench, and ask what is wrong with it.",
        replicaHeard:    "{npc:name} says everyone's goods are sound until they are on their bench, and asks what is wrong with it.",

        new PlayerOption("condition_admit_fault", "admit the flaw before they find it",
            "There is one fault. I will point it out before you find it. Take it off the price.",
            "I point out the one fault to {npc:name} before they find it, and ask them to take it off the price.",
            End("condition_admit_end", 3,
                "You pointed it out yourself. That is worth more than the fault costs you. Show me the lot.",
                "I tell {you:name} pointing it out themselves is worth more than the fault costs them.",
                "So there is something, and you would have let me find it. No sale.",
                "I tell {you:name} they would have let me find it, and refuse the sale.")),

        new PlayerOption("condition_stand_by", "say honestly there is nothing wrong with it",
            "Nothing. Look it over as long as you like.",
            "I invite {npc:name} to look it over as long as they like.",
            End("condition_stand_end", 3,
                "A man who invites inspection is usually telling the truth. Set it out.",
                "I tell {you:name} a man who invites inspection is usually telling the truth, and to set it out.",
                "They all say that. I have been at this too long to take confidence for proof.",
                "I tell {you:name} I have been at this too long to take confidence for proof.")));

    private static NpcLineNode OfferTerms() => new(
        nodeId:          "offer_terms",
        replica:         "Coin on the spot, my price, no promises either way. {npc:opinion_trade}",
        replicaIndirect: "I tell {you:name} my terms are coin on the spot at my price, and that of trade I think {npc:opinion_trade}.",
        replicaHeard:    "{npc:name} says their terms are coin on the spot at their price, and tells me what they think of trade.",

        new PlayerOption("terms_accept", "accept the terms without arguing",
            "That is fair. Coin on the spot suits me better than a promise.",
            "I tell {npc:name} coin on the spot suits me better than a promise.",
            End("terms_accept_end", 3,
                "Then we will get on. Set it out and let me see what you have brought.",
                "I ask {you:name} to set it out so I can see what they brought.",
                "It suits you. It does not suit me to buy today. Good day.",
                "I tell {you:name} it does not suit me to buy today.")),

        new PlayerOption("terms_push_back", "push back on being the one who takes the risk",
            "Your price and no promises. So the risk is mine and the choosing is yours.",
            "I tell {npc:name} that on those terms the risk is mine and the choosing theirs.",
            TermsPushBack()));

    private static NpcLineNode TermsPushBack() => new(
        nodeId:          "terms_push_back",
        replica:         "That is exactly what it is, and nobody has said it to my face. It is my stall, my coin and my loss if I buy wrong.",
        replicaIndirect: "I tell {you:name} nobody has said that to my face, and that it is my stall, my coin and my loss if I buy wrong.",
        replicaHeard:    "{npc:name} says nobody has said that to their face, and that it is their stall, their coin and their loss if they buy wrong.",

        new PlayerOption("push_concede", "concede the point and deal anyway",
            "Then it is your stall and your risk, and I will deal on it. I only wanted it said.",
            "I tell {npc:name} I will deal on it, and only wanted it said.",
            End("push_concede_end", 4,
                "Said, and taken. Set it out, and I will look harder than usual in your favour.",
                "I ask {you:name} to set it out, and promise to look harder than usual in their favour.",
                "Wanting it said has cost you the deal. Good day.",
                "I tell {you:name} wanting it said has cost them the deal.")),

        new PlayerOption("push_ask_fair", "ask what would make it fairer",
            "Then what would make it even? I would rather find that than argue.",
            "I ask {npc:name} what would make it even.",
            End("push_fair_end", 4,
                "Name the price you would walk away at, and I will not cross it. Let us trade.",
                "I tell {you:name} to name the price they would walk away at, and that I will not cross it.",
                "Trade is not even and never was. If you want fair, farm.",
                "I tell {you:name} that trade is not even and never was.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  B — appeal to what they need (rich)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode Flatter() => new(
        nodeId:          "flatter_opening",
        replica:         "What I could use is a season where nobody tries that on me. Go on, what have you?",
        replicaIndirect: "I tell {you:name} what I could use is a season where nobody tries that on me, and ask what they have.",
        replicaHeard:    "{npc:name} says what they could use is a season where nobody tries that on them, and asks what I have.",

        new PlayerOption("flatter_press", "press the pitch anyway",
            "Goods you will be glad of by the week's end, whether you say so now or not.",
            "I tell {npc:name} they will be glad of these goods by the week's end.",
            End("flatter_press_end", 2,
                "That earns you a look, at least. Show me.",
                "I tell {you:name} that earns them a look, and ask to be shown.",
                "Save the sweet talk. I have no need of your goods.",
                "I tell {you:name} to save the sweet talk.")),

        new PlayerOption("flatter_drop_it", "drop the pitch and speak straight",
            "Then I will drop it. I have {you:goods} and I would like it sold.",
            "I drop the pitch and tell {npc:name} I have {you:goods} to sell.",
            FlatterStraight()),

        new PlayerOption("flatter_ask_need", "ask what they actually run short of",
            "Then tell me what you actually run short of, and I will bring that next time.",
            "I ask {npc:name} what they actually run short of, and offer to bring it next time.",
            FlatterNeed()));

    private static NpcLineNode FlatterStraight() => new(
        nodeId:          "flatter_straight",
        replica:         "Straight, all at once. That is a relief. Half my day is people performing at me.",
        replicaIndirect: "I tell {you:name} that is a relief, since half my day is people performing at me.",
        replicaHeard:    "{npc:name} says that is a relief, since half their day is people performing at them.",

        new PlayerOption("straight_ask_look", "ask them to take a look",
            "Then look at it straight, and tell me straight what it is worth.",
            "I ask {npc:name} to look at it and tell me straight what it is worth.",
            End("straight_look_end", 3,
                "That I can do. Set it down and you will get an honest number, high or low.",
                "I tell {you:name} to set it down for an honest number, high or low.",
                "I will tell you straight now: nothing, to me, today.",
                "I tell {you:name} straight that it is worth nothing to me today.")),

        new PlayerOption("straight_apologise", "apologise for opening with the patter",
            "Sorry for the patter. It is a habit from busier markets.",
            "I apologise to {npc:name} for the patter, which is a habit from busier markets.",
            End("straight_sorry_end", 3,
                "No harm. It is how it is done elsewhere. Let me see it.",
                "I tell {you:name} there is no harm, and ask to see it.",
                "Habits like that are why I distrust travelling sellers. Move on.",
                "I tell {you:name} habits like that are why I distrust travelling sellers.")));

    private static NpcLineNode FlatterNeed() => new(
        nodeId:          "flatter_need",
        replica:         "That is a better question than your opening was. There are things I want and cannot reliably get hold of.",
        replicaIndirect: "I tell {you:name} that is a better question, and that there are things I want and cannot reliably get.",
        replicaHeard:    "{npc:name} says that is a better question, and that there are things they want and cannot reliably get.",

        new PlayerOption("need_ask_which", "ask which things",
            "Which things? I travel more than you do. I might find them.",
            "I ask {npc:name} which things, since I travel more than they do.",
            End("need_which_end", 3,
                "Then you are worth knowing. Come, and while I look at what you brought I will tell you the list.",
                "I tell {you:name} they are worth knowing, and offer the list while I look at what they brought.",
                "And have you buy them up and sell them back to me dear? No.",
                "I ask {you:name} whether they mean to buy them up and sell them back to me dear.")),

        new PlayerOption("need_offer_watch", "offer to keep an eye out on your travels",
            "Then I will watch for them. No promises, and no charge for looking.",
            "I offer {npc:name} to watch for them, with no charge for looking.",
            End("need_watch_end", 3,
                "No charge for looking. I will take that, and a look at your goods too. Set them out.",
                "I accept, and ask {you:name} to set out their goods as well.",
                "I have heard that from a dozen travellers. None of them came back.",
                "I tell {you:name} a dozen travellers have said that and none came back.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  C — name your goods first (short)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode NameGoods() => new(
        nodeId:          "name_goods",
        replica:         "{you:goods}. That is in my line, at least. What are you asking for it?",
        replicaIndirect: "I tell {you:name} that {you:goods} is in my line at least, and ask what they want for it.",
        replicaHeard:    "{npc:name} says my goods are in their line at least, and asks what I want for them.",

        new PlayerOption("named_ask_offer", "ask them to name a price instead",
            "You name it. You know what it is worth here better than I do.",
            "I ask {npc:name} to name the price, since they know what it is worth here.",
            End("named_offer_end", 2,
                "You are letting me open. Set it out and I will give you a number.",
                "I ask {you:name} to set it out and I will give them a number.",
                "I will name nothing. Bring me a price or bring me nothing.",
                "I tell {you:name} to bring me a price or bring me nothing.")),

        new PlayerOption("named_state_price", "state your price and stand by it",
            "I have a figure in mind and I will not go far under it. But I will hear you.",
            "I tell {npc:name} I have a figure and will not go far under it.",
            NamedPrice()),

        new PlayerOption("named_say_urgent", "admit you need it gone today",
            "The truth is I need it gone today. That is worth something to you.",
            "I tell {npc:name} I need it gone today.",
            End("named_urgent_end", 2,
                "It is, and you have just told me so, which costs you. But I will deal. Set it down.",
                "I tell {you:name} that telling me costs them, but that I will deal.",
                "If you need it gone you will take any offer. It will not be mine.",
                "I tell {you:name} that if they need it gone they will take any offer.")));

    private static NpcLineNode NamedPrice() => new(
        nodeId:          "named_price",
        replica:         "Everyone has a figure in mind, and it is always half again what the thing is worth. Let me hear how far off you are.",
        replicaIndirect: "I tell {you:name} everyone's figure is half again what the thing is worth, and ask how far off they are.",
        replicaHeard:    "{npc:name} says everyone's figure is half again what the thing is worth, and asks how far off I am.",

        new PlayerOption("price_hold_firm", "hold to your figure",
            "I will hold to my figure. If it is too dear for you, we part on good terms.",
            "I tell {npc:name} I will hold to my figure and we part on good terms if it is too dear.",
            End("price_firm_end", 3,
                "A seller who will walk away is rare, and it makes me want to look. Set it out.",
                "I tell {you:name} a seller who will walk away is rare, and ask them to set it out.",
                "Then we part. Good day.",
                "I tell {you:name} that then we part.")),

        new PlayerOption("price_invite_counter", "invite them to counter",
            "Then tell me how far off. I would rather meet you than argue.",
            "I ask {npc:name} how far off I am, since I would rather meet them than argue.",
            End("price_counter_end", 3,
                "That is how a bargain should go and rarely does. Let us find the middle.",
                "I tell {you:name} that is how a bargain should go, and propose we find the middle.",
                "I would rather not meet you at all today. Try the next stall.",
                "I tell {you:name} to try the next stall.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  D — ask what they are short of (short)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode AskNeeds() => new(
        nodeId:          "ask_needs",
        replica:         "{npc:buys}, mostly, and never enough of it at the right time.",
        replicaIndirect: "I tell {you:name} it is {npc:buys} mostly, and never enough of it at the right time.",
        replicaHeard:    "{npc:name} says it is {npc:buys} mostly, and never enough of it at the right time.",

        new PlayerOption("needs_have_some", "say you have some of exactly that",
            "Then you are in luck. I have {you:goods} with me now.",
            "I tell {npc:name} they are in luck, since I have {you:goods} with me.",
            End("needs_have_end", 2,
                "Then let us not stand about. Set it down and we will settle it.",
                "I tell {you:name} to set it down so we can settle it.",
                "With you now, and mine by evening. I am not buying.",
                "I tell {you:name} I am not buying.")),

        new PlayerOption("needs_ask_when", "ask when they are usually short",
            "When are you usually short? I would sooner come at the right time.",
            "I ask {npc:name} when they are usually short, so I can come at the right time.",
            NeedsWhen()),

        new PlayerOption("needs_ask_who", "ask who else supplies them",
            "Who brings it to you now, when it does come?",
            "I ask {npc:name} who brings it to them now.",
            End("needs_who_end", 2,
                "One household, and unreliable, which is why I am listening to you. Show me what you have.",
                "I tell {you:name} it is one household and unreliable, which is why I am listening.",
                "My suppliers are my business. Are you selling or surveying?",
                "I tell {you:name} my suppliers are my business.")));

    private static NpcLineNode NeedsWhen() => new(
        nodeId:          "needs_when",
        replica:         "Late in the year, mostly, when everyone has sold theirs elsewhere and I am asking favours. {npc:opinion_seasons}",
        replicaIndirect: "I tell {you:name} it is late in the year, when everyone has sold theirs elsewhere, and that of the seasons I think {npc:opinion_seasons}.",
        replicaHeard:    "{npc:name} says it is late in the year, when everyone has sold theirs elsewhere, and tells me what they think of the seasons.",

        new PlayerOption("when_promise_return", "say you will come back at that time of year",
            "Then that is when I will come. It costs me nothing to time it right.",
            "I tell {npc:name} that is when I will come, since it costs nothing to time it right.",
            End("when_return_end", 3,
                "You plan further ahead than this afternoon. Come, and let me see what you brought today as well.",
                "I tell {you:name} they plan further ahead than this afternoon, and ask to see today's goods too.",
                "Come then, and I will deal with whoever is standing there. It may not be you.",
                "I tell {you:name} I will deal with whoever is standing there, who may not be them.")),

        new PlayerOption("when_sell_now", "point out you are here now, which is worth something",
            "I am here now, though. That is worth more than a promise about autumn.",
            "I tell {npc:name} I am here now, which is worth more than a promise about autumn.",
            End("when_now_end", 3,
                "It is. Goods in hand beat a promise. Set them out.",
                "I agree that goods in hand beat a promise, and ask {you:name} to set them out.",
                "Here now and gone tomorrow. That is the trouble with travelling sellers.",
                "I tell {you:name} they are here now and gone tomorrow.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Entry
    // ══════════════════════════════════════════════════════════════════════════

    private static readonly NpcLineNode Opening = new(
        nodeId:          "opening",
        replica:         "{you:name}. What are you carrying?",
        replicaIndirect: "I greet {you:name} and ask what they are carrying.",
        replicaHeard:    "{npc:name} greets me and asks what I am carrying.",

        new PlayerOption("offer_goods", "plainly offer what you have to sell",
            "I have goods you might want to buy, {npc:name}.",
            "I tell {npc:name} I have goods they might want to buy.",
            OfferGoods()),

        new PlayerOption("flatter", "appeal to their needs to warm them to a purchase",
            "You look like someone who could use goods like these.",
            "I tell {npc:name} they look like someone who could use goods like these.",
            Flatter()),

        new PlayerOption("name_goods", "name your goods straight away",
            "{you:goods}. That is what I am carrying, and I would sell it.",
            "I tell {npc:name} I am carrying {you:goods} and would sell it.",
            NameGoods()),

        new PlayerOption("ask_needs", "ask what they are short of before offering anything",
            "Before I open my pack, what are you short of?",
            "I ask {npc:name} what they are short of, before I open my pack.",
            AskNeeds()));

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
