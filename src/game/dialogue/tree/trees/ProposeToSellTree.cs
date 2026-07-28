using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Trade;

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

    /// <summary>A branch end. Getting a look at your goods is a low-stakes ask.</summary>
    private static ResolutionNode End(string id, int depth, string success, string failure) => new(
        nodeId:         id,
        difficulty:     BranchDifficulty.Easy(depth),
        successReplica: success,
        failureReplica: failure);

    // ══════════════════════════════════════════════════════════════════════════
    //  A — offer plainly (deepest)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode OfferGoods() => new(
        nodeId:  "offer_goods",
        replica: "Goods I might want. I deal in {npc:buys} and nothing beyond it — so before you unpack, what is it?",

        new PlayerOption("offer_name_them", "name exactly what you are carrying",
            "{you:goods}. That's what I've got.",
            End("offer_name_end", 2,
                "Let's see what you've got, then. I'll not turn away a fair deal.",
                "I'm not buying today. Keep your goods.")),

        new PlayerOption("offer_say_condition", "speak to the condition of it before the price",
            "It's sound. I'll not dress it up — you'll see for yourself what state it's in.",
            OfferCondition()),

        new PlayerOption("offer_ask_terms", "ask what terms they buy on",
            "And what terms do you buy on? I'd rather know before I unpack.",
            OfferTerms()));

    private static NpcLineNode OfferCondition() => new(
        nodeId:  "offer_condition",
        replica: "Sound, you say. Everyone's is sound until it's on my bench. What's wrong with it? There's always something.",

        new PlayerOption("condition_admit_fault", "admit the flaw before they find it",
            "There's a fault, and I'll point it out before you find it. Knock it off the price.",
            End("condition_admit_end", 3,
                "...You pointed it out yourself. That buys you more than the fault costs you. Show me the lot.",
                "So there is something. And you'd have let me find it. No sale.")),

        new PlayerOption("condition_stand_by", "say honestly there is nothing wrong with it",
            "Nothing. Look it over as hard as you like — I'll wait.",
            End("condition_stand_end", 3,
                "Hah — a man who invites inspection is usually telling the truth. Out with it, then.",
                "They all say that. I've been at this too long to be flattered by confidence.")));

    private static NpcLineNode OfferTerms() => new(
        nodeId:  "offer_terms",
        replica: "Terms? Coin on the spot, my price, no promises either way. {npc:opinion_trade}",

        new PlayerOption("terms_accept", "accept the terms without arguing",
            "That's fair enough. Coin on the spot suits me better than a promise anyway.",
            End("terms_accept_end", 3,
                "Then we'll get on. Set it out and let's see what you've brought me.",
                "Suits you. It doesn't suit me to buy today. Good day.")),

        new PlayerOption("terms_push_back", "push back on being the one who takes the risk",
            "Your price and no promises. So the risk is all mine and the choosing is all yours.",
            TermsPushBack()));

    private static NpcLineNode TermsPushBack() => new(
        nodeId:  "terms_push_back",
        replica: "...That's exactly what it is. Nobody's ever said it to my face. It's my stall, my coin and my ruin if I buy wrong — so aye, that's the arrangement.",

        new PlayerOption("push_concede", "concede the point and deal anyway",
            "Then it's your stall and your risk, and I'll deal on it. I only wanted it said.",
            End("push_concede_end", 4,
                "...Said, and taken. Alright. Set it out — and I'll look harder than I usually do, in your favour.",
                "Wanting it said cost you the deal. Good day to you.")),

        new PlayerOption("push_ask_fair", "ask what would make it fairer",
            "Then what would make it even? I'd rather find that than argue about it.",
            End("push_fair_end", 4,
                "...Ask me to name the price where you'd walk away, and I'll not cross it. That's what. Come on, let's trade.",
                "Even? Trade's not even and never was. If you want fair, farm.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  B — appeal to what they need (rich)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode Flatter() => new(
        nodeId:  "flatter_opening",
        replica: "Someone who could use fine wares. Hm. What I could use is a season where nobody tries this on me. Go on — what have you?",

        new PlayerOption("flatter_press", "press the pitch anyway",
            "Wares you'd be glad of by the week's end, whether you'll say so now or not.",
            End("flatter_press_end", 2,
                "Hah, well — a silver tongue earns a look, at least. Show me.",
                "Save the sweet talk. I've no need of your wares.")),

        new PlayerOption("flatter_drop_it", "drop the pitch and speak straight",
            "Fair enough — I'll drop it. I've {you:goods} and I'd like it sold.",
            FlatterStraight()),

        new PlayerOption("flatter_ask_need", "ask what they actually run short of",
            "Then tell me what you actually run short of, and I'll bring that next time.",
            FlatterNeed()));

    private static NpcLineNode FlatterStraight() => new(
        nodeId:  "flatter_straight",
        replica: "...Straight, all at once. That's a relief, if I'm honest. Half my day is people performing at me.",

        new PlayerOption("straight_ask_look", "ask them to take a look",
            "Then take a look at it straight, and tell me straight what it's worth.",
            End("straight_look_end", 3,
                "That I can do. Set it down — you'll get an honest number, high or low.",
                "I'll tell you straight now: nothing, to me, today.")),

        new PlayerOption("straight_apologise", "apologise for opening with the patter",
            "Sorry for the patter. Habit from busier markets than this one.",
            End("straight_sorry_end", 3,
                "No harm. It's how it's done elsewhere, I know. Come on, let's see it.",
                "Habits like that are why I distrust travelling sellers. Move on.")));

    private static NpcLineNode FlatterNeed() => new(
        nodeId:  "flatter_need",
        replica: "What I run short of. Now that's a better question than your opening was. There's things I want and can't reliably get hold of, aye.",

        new PlayerOption("need_ask_which", "ask which things",
            "Which things? I go about more than you do — I might turn them up.",
            End("need_which_end", 3,
                "...Then you're worth knowing. Come here, and while I'm looking at what you've brought I'll tell you the list.",
                "And have you buy them up and sell them back to me dear? I think not.")),

        new PlayerOption("need_offer_watch", "offer to keep an eye out on your travels",
            "Then I'll keep an eye out. No promises and no charge for looking.",
            End("need_watch_end", 3,
                "No charge for looking. Aye — I'll take that, and I'll take a look at your goods too. Set them out.",
                "I've heard that from a dozen wanderers. None of them came back.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  C — name your goods first (short)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode NameGoods() => new(
        nodeId:  "name_goods",
        replica: "{you:goods}, is it. That's in my line, at least. What are you asking for it?",

        new PlayerOption("named_ask_offer", "ask them to name a price instead",
            "You name it. You know what it's worth here better than I do.",
            End("named_offer_end", 2,
                "Letting me open. Bold, or lazy. Either way — set it out and I'll give you a number.",
                "I'll name nothing. Bring me a price or bring me nothing.")),

        new PlayerOption("named_state_price", "state your price and stand by it",
            "I've a figure in mind and I'll not go far under it. But I'll hear you out.",
            NamedPrice()),

        new PlayerOption("named_say_urgent", "admit you need it gone today",
            "Truth is I need it gone today. That's worth something to you, I'd think.",
            End("named_urgent_end", 2,
                "It is, and you've just told me so — which costs you. But I'll deal. Set it down.",
                "Need it gone? Then you'll take what anyone offers. It won't be me.")));

    private static NpcLineNode NamedPrice() => new(
        nodeId:  "named_price",
        replica: "A figure in mind. Everyone has one, and it's always half again what the thing is worth. Let's hear how far off you are.",

        new PlayerOption("price_hold_firm", "hold to your figure",
            "I'll stand where I stand. If it's too dear for you, we'll shake hands and part.",
            End("price_firm_end", 3,
                "...A seller who'll walk. That's rare, and it makes me want to look. Set it out.",
                "Then part we shall. Good day.")),

        new PlayerOption("price_invite_counter", "invite them to counter",
            "Then tell me how far off. I'd rather meet you than argue at you.",
            End("price_counter_end", 3,
                "That's how a bargain is meant to go and hardly ever does. Come — let's find the middle.",
                "I'd rather not meet you at all today. Try the next stall.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  D — ask what they are short of (short)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode AskNeeds() => new(
        nodeId:  "ask_needs",
        replica: "What am I short of. Straight to my side of it — that's unusual. {npc:buys}, mostly, and never enough of it at the right time.",

        new PlayerOption("needs_have_some", "say you have some of exactly that",
            "Then you're in luck. I've {you:goods} on me now.",
            End("needs_have_end", 2,
                "Then let's not stand about. Set it down and we'll settle it.",
                "On you now, and mine by evening, you're thinking. I'm not buying.")),

        new PlayerOption("needs_ask_when", "ask when they are usually short",
            "And when are you usually short? I'd sooner come at the right time.",
            NeedsWhen()),

        new PlayerOption("needs_ask_who", "ask who else supplies them",
            "Who brings it to you now, when it does come?",
            End("needs_who_end", 2,
                "One household, and unreliable. Which is why I'm listening to you at all. Show me what you have.",
                "My suppliers are my business. Are you selling or surveying?")));

    private static NpcLineNode NeedsWhen() => new(
        nodeId:  "needs_when",
        replica: "Late in the year, mostly, when everyone's sold theirs elsewhere and I'm left calling in favours. {npc:opinion_seasons}",

        new PlayerOption("when_promise_return", "say you will come back at that time of year",
            "Then that's when I'll come. It costs me nothing to time it right.",
            End("when_return_end", 3,
                "...Someone who plans further out than this afternoon. Come — let's see what you've brought today as well.",
                "Come then, and I'll deal with whoever's standing there. It may not be you.")),

        new PlayerOption("when_sell_now", "point out you are here now, which is worth something",
            "I'm here now, though. That's worth a little more than a promise about autumn.",
            End("when_now_end", 3,
                "It is. Ready goods beat a promise every time. Alright — set them out.",
                "Here now and gone tomorrow. That's the trouble with your sort.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Entry
    // ══════════════════════════════════════════════════════════════════════════

    private static readonly NpcLineNode Opening = new(
        nodeId:  "opening",
        replica: "Aye, {you:name}? What is it you're carrying?",

        new PlayerOption("offer_goods", "plainly offer what you have to sell",
            "I've goods you might want to buy, {npc:name}.", OfferGoods()),

        new PlayerOption("flatter", "appeal to their needs to warm them to a purchase",
            "You look like someone who could use fine wares like these.", Flatter()),

        new PlayerOption("name_goods", "name your goods straight away",
            "{you:goods}. That's what I'm carrying, and I'd sell it.", NameGoods()),

        new PlayerOption("ask_needs", "ask what they are short of before offering anything",
            "Before I open my pack — what are you short of?", AskNeeds()));

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
