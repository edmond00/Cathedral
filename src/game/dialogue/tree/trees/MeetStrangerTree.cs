using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// "Meet Stranger" — available only before any dialogue has occurred (Stranger affinity).
/// A first meeting: the NPC greets, the player opens in one of five ways, and after two to four
/// exchanges the single check decides whether the acquaintance starts warm or sour.
///
/// <para>
/// Five openings, each with its own shape: warmth, curiosity about them, introducing yourself,
/// asking about the place, and holding back. The warm opening runs deepest (it is the one that
/// invites a real conversation) and the guarded one is shortest — the imbalance is the point.
/// Nothing here uses <c>{npc:name}</c>: you do not know it yet. What the NPC volunteers about
/// themselves comes from <c>{npc:introduction}</c>, so a smith and a shepherd meet you differently.
/// </para>
/// </summary>
public class MeetStrangerTree : DialogueTree
{
    public override string TreeId           => "meet_stranger";
    public override string DisplayName      => "Meet Stranger";
    public override string Description      => "meeting this person for the first time and exchanging introductions";
    public override string AssociatedVerbId => "meet_stranger";

    public override IReadOnlyList<IDialogueOutcome> SuccessOutcomes { get; } = new IDialogueOutcome[]
    {
        new AffinityTransitionOutcome(AffinityLevel.DistantAcquaintance),
    };

    public override IReadOnlyList<IDialogueOutcome> FailureOutcomes { get; } = new IDialogueOutcome[]
    {
        new AffinityTransitionOutcome(AffinityLevel.AnnoyingAcquaintance),
    };

    // ── Authoring helpers ──────────────────────────────────────────────────────

    /// <summary>A branch end. <paramref name="depth"/> is how many player replies reached it.</summary>
    private static ResolutionNode End(string id, int depth, string success, string failure) => new(
        nodeId:         id,
        difficulty:     BranchDifficulty.Easy(depth),
        successReplica: success,
        failureReplica: failure);

    // The whole tree is built through static methods rather than static fields: a field graph would
    // depend on textual initialisation order, and this one is too large to keep straight by eye.

    // ══════════════════════════════════════════════════════════════════════════
    //  A — greeted them warmly (the deepest opening)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode WarmOpening() => new(
        nodeId:  "warm_opening",
        replica: "Well — that's a friendlier greeting than I usually get out here. Are you passing through, or stopping?",

        new PlayerOption("just_passing", "say you are only passing through",
            "Only passing. I'll not trouble anyone long.",
            End("warm_passing", 2,
                "Passing folk are the easiest sort. Safe roads to you, then.",
                "Passing, and stopping to bother me on the way. Aye. Good day.")),

        new PlayerOption("stopping_awhile", "say you mean to stop a while and would like to know the place",
            "Stopping, I think — long enough to learn the place, at least.",
            WarmStopping()),

        new PlayerOption("ask_their_day", "ask what has been keeping them busy",
            "And what's been keeping you busy today?",
            WarmTheirDay()));

    private static NpcLineNode WarmStopping() => new(
        nodeId:  "warm_stopping",
        replica: "Then you'll want to know who's who before long. It's a small enough place that it matters.",

        new PlayerOption("ask_who_matters", "ask who it would be wise to know",
            "Who is it worth knowing hereabouts?",
            End("warm_who_matters", 3,
                "Well — you've made a start with me, haven't you. Come find me again and I'll name you the rest.",
                "Sizing up the place already, are you. I'll keep my list to myself.")),

        new PlayerOption("offer_hands", "offer that you are willing to work while you are here",
            "I've hands and I'm willing to use them, if there's work needing doing.",
            End("warm_offer_hands", 3,
                "Willing hands. Now that's worth more here than a fine name. We'll see what turns up.",
                "Everyone says that on the first day. I'll believe it when I see the second.")));

    private static NpcLineNode WarmTheirDay() => new(
        nodeId:  "warm_their_day",
        replica: "Me? {npc:labour}. Same as yesterday, and same as it'll be tomorrow.",

        new PlayerOption("say_hard_work", "acknowledge that sounds like hard work",
            "That sounds like heavy going, day after day.",
            End("warm_hard_work", 3,
                "It is. It's a rare thing for anyone to say so out loud. Thank you for that.",
                "Don't pity me. I've done it thirty years and I'll do thirty more.")),

        new PlayerOption("ask_about_craft", "ask them to tell you more about their trade",
            "I don't know much about {npc:craft}. Tell me something about it.",
            WarmAboutCraft()));

    private static NpcLineNode WarmAboutCraft() => new(
        nodeId:  "warm_about_craft",
        replica: "Ha — you'd be the first to ask. {npc:opinion_work} That's the short of it, anyway.",

        new PlayerOption("ask_how_learned", "ask how they came to learn it",
            "And how did you come to it?",
            End("warm_how_learned", 4,
                "The usual way — badly, and for years, until one day it wasn't badly any more. It's good to be asked.",
                "That's a long story and you've not earned it yet.")),

        new PlayerOption("admire_plainly", "say plainly that it is a skill worth having",
            "That's a skill worth having. Not many could do it.",
            End("warm_admire", 4,
                "You've a way of saying a thing that lands. Aye — well met, truly.",
                "Save it. Praise from a stranger is worth exactly what it cost you.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  B — asked who they are
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode WhoAreYou() => new(
        nodeId:  "who_are_you",
        replica: "Me? No one grand — {npc:introduction}. And you?",

        new PlayerOption("give_name", "give your own name in return",
            "Well met. I'm {you:name}.",
            NamesExchanged()),

        new PlayerOption("stay_vague", "answer without giving much away",
            "Nobody worth writing down. Just someone on the road.",
            End("who_vague", 2,
                "Fair enough. Half the folk here won't give a name either, and they were born down the lane.",
                "Won't say, won't you. Then we've nothing more to say to each other.")));

    private static NpcLineNode NamesExchanged() => new(
        nodeId:  "names_exchanged",
        replica: "{you:name}. Well. That's more than most give up on a first meeting.",

        new PlayerOption("ask_their_work", "ask what their work is",
            "And what's your work here?",
            End("names_their_work", 3,
                "{npc:job}, for my sins — and you'll find me at {npc:workplace} most days. Come by.",
                "You ask a lot of questions for someone who's been here an hour.")),

        new PlayerOption("say_glad", "say you are glad to have met them",
            "Then I'm glad to have met you properly.",
            End("names_glad", 3,
                "And I you. It's a small enough place that a new face is an event. Fare well.",
                "Mm. We'll see whether either of us stays glad about it.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  C — introduced yourself first
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode IntroOpening() => new(
        nodeId:  "intro_opening",
        replica: "{you:name}, is it. Straight out with it — I'll say that for you. {npc:introduction}, for my part.",

        new PlayerOption("say_pleasure", "say the pleasure is yours and leave it there",
            "The pleasure's mine. I'll not keep you from your work.",
            End("intro_pleasure", 2,
                "Courteous, and knows when to stop. That's twice rarer than it should be. Until next time.",
                "Aye, well. Keep walking, then.")),

        new PlayerOption("ask_how_long", "ask how long they have been here",
            "And how long have you been at that?",
            IntroHowLong()));

    private static NpcLineNode IntroHowLong() => new(
        nodeId:  "intro_how_long",
        replica: "Long enough that I don't count it any more. {npc:opinion_seasons}",

        new PlayerOption("say_rooted", "remark that they sound well rooted here",
            "You sound like someone who belongs where they're standing.",
            End("intro_rooted", 3,
                "That's a kind way to put it. Most would say stuck. Aye — well met, {you:name}.",
                "Rooted. That's a polite word for it, and I don't much care for polite words.")),

        new PlayerOption("say_envy", "admit you envy having a place of your own",
            "I've had no place of my own that long. I think I envy it.",
            End("intro_envy", 3,
                "Don't envy it too hard — but I'll not pretend I'd trade. Come back and talk again sometime.",
                "Envy it if you like. It doesn't make us friends.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  D — asked about the place
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode AboutPlace() => new(
        nodeId:  "about_place",
        replica: "This place? It's what it looks like. Folk work, folk eat, folk talk about each other. What is it you want to know?",

        new PlayerOption("ask_the_folk", "ask what the people here are like",
            "What are the folk here like?",
            End("place_the_folk", 2,
                "{npc:opinion_neighbours} You'll get on well enough if you don't go giving yourself airs.",
                "You'll find out. I'll not do the work of forming your opinion for you.")),

        new PlayerOption("ask_the_road", "ask about the road and who comes down it",
            "Do many come down that road?",
            PlaceTheRoad()),

        new PlayerOption("ask_dangers", "ask whether there is anything to watch out for",
            "Anything a newcomer ought to watch out for?",
            End("place_dangers", 2,
                "Nothing that'll trouble you if you keep your hands where they belong. Mind that and you'll do.",
                "Aye. Newcomers asking what's worth watching out for. Move along.")));

    private static NpcLineNode PlaceTheRoad() => new(
        nodeId:  "place_the_road",
        replica: "Some. {npc:opinion_roads}",

        new PlayerOption("say_not_thief", "make clear you are not one of the bad sort",
            "Well, I'm neither of those. You've my word on it.",
            End("road_not_thief", 3,
                "A stranger's word isn't worth much, but you offered it plainly, and that counts for something.",
                "Everyone says that. Every single one of them says exactly that.")),

        new PlayerOption("ask_news", "ask what news the road has brought lately",
            "And what news have they brought lately?",
            End("road_news", 3,
                "Little enough, and half of it invented. Still — sit with me sometime and I'll tell you the good half.",
                "News is for people I know. Come back when you're one.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  E — kept your distance
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode Guarded() => new(
        nodeId:  "guarded",
        replica: "Not much for talking, then. Fair enough — I've work to be at anyway.",

        new PlayerOption("apologise_curt", "explain that you meant no discourtesy",
            "I meant no discourtesy. I'm slow with strangers, that's all.",
            End("guarded_apologise", 2,
                "So am I, if it comes to that. No offence taken. Go safely.",
                "Slow with strangers. Aye. Well, I'm quick with them, and I'm done.")),

        new PlayerOption("state_business", "state your business plainly instead",
            "Then I'll be plain. I'm new here and I'm taking the measure of the place.",
            GuardedBusiness()));

    private static NpcLineNode GuardedBusiness() => new(
        nodeId:  "guarded_business",
        replica: "Taking the measure of it. Hm. That's honest, at least — more than most manage.",

        new PlayerOption("ask_measure", "ask how they would measure it themselves",
            "How would you measure it, if you were me?",
            End("guarded_measure", 3,
                "By whether folk look you in the eye. Here, mostly they do. You'll manage.",
                "I wouldn't be you, and I'll not think for you either.")),

        new PlayerOption("leave_it_there", "say you have taken enough of their time",
            "That's enough of your day. My thanks for the words.",
            End("guarded_leave", 3,
                "Now that's a good manner to have. Come by again — I'll have more time then.",
                "It was too much of my day a while ago. Go on.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Entry
    // ══════════════════════════════════════════════════════════════════════════

    private static readonly NpcLineNode Greeting = new(
        nodeId:  "greeting",
        replica: "Well met. I don't believe we've crossed paths before.",

        new PlayerOption("greet_warmly", "greet them warmly",
            "Good day to you! Well met, friend.", WarmOpening()),

        new PlayerOption("ask_who", "ask who they are",
            "And who might you be?", WhoAreYou()),

        new PlayerOption("introduce", "introduce yourself first",
            "Let me introduce myself — I'm {you:name}.", IntroOpening()),

        new PlayerOption("ask_place", "ask about this place instead of about them",
            "I'm new to this place. What sort of place is it?", AboutPlace()),

        new PlayerOption("keep_distance", "answer briefly and keep your distance",
            "Aye. We haven't.", Guarded()));

    public override NpcLineNode EntryNode => Greeting;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
        => npc.AffinityTable.IsStranger(partyMemberId);
}
