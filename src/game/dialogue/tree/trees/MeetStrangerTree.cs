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
/// What the NPC volunteers about themselves comes from <c>{npc:introduction}</c>, so a smith and a
/// shepherd meet you differently.
/// </para>
///
/// <para>
/// <b>Neither party is named here</b> until they say so: <c>{npc:name}</c> and <c>{you:name}</c>
/// appear only on the branches where a name has actually been given (B and C), and the reports say
/// "them" elsewhere.
/// </para>
///
/// <para>
/// Every replica is the spoken line, plainly; see "Authoring the neutral text" on
/// <see cref="DialogueTree"/> for what that means and what the indirect twins are for.
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
    private static ResolutionNode End(string id, int depth,
                                      string success, string successIndirect,
                                      string failure, string failureIndirect) => new(
        nodeId:                 id,
        difficulty:             BranchDifficulty.Easy(depth),
        successReplica:         success,
        successReplicaIndirect: successIndirect,
        failureReplica:         failure,
        failureReplicaIndirect: failureIndirect);

    // The whole tree is built through static methods rather than static fields: a field graph would
    // depend on textual initialisation order, and this one is too large to keep straight by eye.

    // ══════════════════════════════════════════════════════════════════════════
    //  A — greeted them warmly (the deepest opening)
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode WarmOpening() => new(
        nodeId:          "warm_opening",
        replica:         "That is a friendlier greeting than I usually get. Are you passing through, or stopping?",
        replicaIndirect: "I ask them whether they are passing through or stopping.",
        replicaHeard:    "The stranger asks me whether I am passing through or stopping.",

        new PlayerOption("just_passing", "say you are only passing through",
            "Only passing through. I will not stay long.",
            "I tell them I am only passing through.",
            End("warm_passing", 2,
                "Then you are no trouble to anyone. Safe travelling.",
                "I wish them safe travelling.",
                "You stopped to bother me on your way. Good day.",
                "I tell them they stopped to bother me on their way.")),

        new PlayerOption("stopping_awhile", "say you mean to stop a while and would like to know the place",
            "Stopping, at least long enough to learn the place.",
            "I tell them I am stopping long enough to learn the place.",
            WarmStopping()),

        new PlayerOption("ask_their_day", "ask what has been keeping them busy",
            "What has kept you busy today?",
            "I ask them what has kept them busy today.",
            WarmTheirDay()));

    private static NpcLineNode WarmStopping() => new(
        nodeId:          "warm_stopping",
        replica:         "Then you will need to know who is who. It is a small place, so that matters.",
        replicaIndirect: "I tell them this is a small place, so they will need to know who is who.",
        replicaHeard:    "The stranger says this is a small place, so I will need to know who is who.",

        new PlayerOption("ask_who_matters", "ask who it would be wise to know",
            "Who is worth knowing here?",
            "I ask them who is worth knowing here.",
            End("warm_who_matters", 3,
                "You have made a start with me. Come back and I will name the others.",
                "I tell them to come back and I will name the others.",
                "You are sizing the place up already. I will keep my list to myself.",
                "I tell them they are sizing the place up already.")),

        new PlayerOption("offer_hands", "offer that you are willing to work while you are here",
            "I can work, if there is work needing doing.",
            "I offer them my work, if there is work needing doing.",
            End("warm_offer_hands", 3,
                "Willing workers are wanted here more than fine names. We will see what comes up.",
                "I tell them willing workers are wanted here more than fine names.",
                "Everyone says that on the first day. I do not believe it yet.",
                "I tell them everyone says that on the first day.")));

    private static NpcLineNode WarmTheirDay() => new(
        nodeId:          "warm_their_day",
        replica:         "{npc:labour}. The same as yesterday, and the same tomorrow.",
        replicaIndirect: "I tell them my day is {npc:labour}, the same as yesterday and the same tomorrow.",
        replicaHeard:    "The stranger tells me what their working day is, and says it is the same every day.",

        new PlayerOption("say_hard_work", "acknowledge that sounds like hard work",
            "That sounds like hard work, day after day.",
            "I tell them that sounds like hard work, day after day.",
            End("warm_hard_work", 3,
                "It is. Few people say so. Thank you.",
                "I tell them few people say so, and thank them.",
                "Do not pity me. I have done it thirty years and will do thirty more.",
                "I tell them not to pity me, since I have done it thirty years.")),

        new PlayerOption("ask_about_craft", "ask them to tell you more about their trade",
            "I know little about {npc:craft}. Tell me about it.",
            "I ask them to tell me about {npc:craft}, which I know little about.",
            WarmAboutCraft()));

    private static NpcLineNode WarmAboutCraft() => new(
        nodeId:          "warm_about_craft",
        replica:         "You are the first to ask. {npc:opinion_work}",
        replicaIndirect: "I tell them they are the first to ask, and that of the work I think {npc:opinion_work}.",
        replicaHeard:    "The stranger says I am the first to ask, and tells me what they think of their work.",

        new PlayerOption("ask_how_learned", "ask how they came to learn it",
            "How did you learn it?",
            "I ask them how they learned it.",
            End("warm_how_learned", 4,
                "Badly, for years, until one day it was not badly. I am glad to be asked.",
                "I tell them I learned it badly for years until one day it was not badly.",
                "That is a long story, and I do not know you well enough to tell it.",
                "I tell them it is a long story and I do not know them well enough to tell it.")),

        new PlayerOption("admire_plainly", "say plainly that it is a skill worth having",
            "That is a skill worth having. Few could do it.",
            "I tell them few could do it and it is a skill worth having.",
            End("warm_admire", 4,
                "You put that well. I am glad we met.",
                "I tell them they put that well and I am glad we met.",
                "Keep it. Praise from a stranger is worth nothing.",
                "I tell them praise from a stranger is worth nothing.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  B — asked who they are
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode WhoAreYou() => new(
        nodeId:          "who_are_you",
        replica:         "No one important. {npc:introduction}. And you?",
        replicaIndirect: "I tell them I am {npc:introduction}, and ask who they are.",
        replicaHeard:    "The stranger tells me who they are, and asks who I am.",

        new PlayerOption("give_name", "give your own name in return",
            "My name is {you:name}.",
            "I tell them my name is {you:name}.",
            NamesExchanged()),

        new PlayerOption("stay_vague", "answer without giving much away",
            "No one important. Someone travelling through.",
            "I tell them I am no one important, only someone travelling through.",
            End("who_vague", 2,
                "That is fine. Half the people here will not give a name either.",
                "I tell them half the people here will not give a name either.",
                "You will not say. Then we have nothing more to discuss.",
                "I tell them that since they will not say, we have nothing more to discuss.")));

    private static NpcLineNode NamesExchanged() => new(
        nodeId:          "names_exchanged",
        replica:         "{you:name}. That is more than most give at a first meeting.",
        replicaIndirect: "I tell {you:name} that a name is more than most give at a first meeting.",
        replicaHeard:    "{npc:name} says a name is more than most give at a first meeting.",

        new PlayerOption("ask_their_work", "ask what their work is",
            "What is your work here?",
            "I ask {npc:name} what their work here is.",
            End("names_their_work", 3,
                "{npc:job}. You will find me at {npc:workplace} most days. Come by.",
                "I tell {you:name} I am {npc:job}, and to find me at {npc:workplace} most days.",
                "You ask a great many questions for someone who arrived an hour ago.",
                "I tell {you:name} they ask a great many questions for someone who arrived an hour ago.")),

        new PlayerOption("say_glad", "say you are glad to have met them",
            "I am glad to have met you properly.",
            "I tell {npc:name} I am glad to have met them properly.",
            End("names_glad", 3,
                "And I you. A new face is an event in a place this small. Farewell.",
                "I tell {you:name} a new face is an event in a place this small.",
                "We will see whether either of us stays glad about it.",
                "I tell {you:name} we will see whether either of us stays glad about it.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  C — introduced yourself first
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode IntroOpening() => new(
        nodeId:          "intro_opening",
        replica:         "{you:name}. You come straight out with it. {npc:introduction}, for my part.",
        replicaIndirect: "I tell {you:name} they come straight out with it, and that I am {npc:introduction}.",
        replicaHeard:    "{npc:name} says I come straight out with it, and tells me who they are.",

        new PlayerOption("say_pleasure", "say the pleasure is yours and leave it there",
            "Good to meet you. I will not keep you from your work.",
            "I tell {npc:name} I will not keep them from their work.",
            End("intro_pleasure", 2,
                "Courteous, and you know when to stop. That is rare. Until next time.",
                "I tell {you:name} that knowing when to stop is rare.",
                "Then keep walking.",
                "I tell {you:name} to keep walking.")),

        new PlayerOption("ask_how_long", "ask how long they have been here",
            "How long have you done that?",
            "I ask {npc:name} how long they have done that.",
            IntroHowLong()));

    private static NpcLineNode IntroHowLong() => new(
        nodeId:          "intro_how_long",
        replica:         "Long enough that I no longer count the years. {npc:opinion_seasons}",
        replicaIndirect: "I tell {you:name} I no longer count the years, and that of the seasons I think {npc:opinion_seasons}.",
        replicaHeard:    "{npc:name} says they no longer count the years, and tells me what they think of the seasons.",

        new PlayerOption("say_rooted", "remark that they sound well rooted here",
            "You sound settled where you are.",
            "I tell {npc:name} they sound settled where they are.",
            End("intro_rooted", 3,
                "That is a kind way to put it. Most would say stuck. Well met, {you:name}.",
                "I tell {you:name} that is a kind way to put it, since most would say stuck.",
                "Settled is a polite word for it, and I do not care for polite words.",
                "I tell {you:name} that settled is a polite word for it, and I dislike polite words.")),

        new PlayerOption("say_envy", "admit you envy having a place of your own",
            "I have never had a place of my own that long. I envy it.",
            "I tell {npc:name} I envy having a place of my own that long.",
            End("intro_envy", 3,
                "Do not envy it too much, though I would not trade it. Come and talk again.",
                "I tell {you:name} not to envy it too much, and to come and talk again.",
                "Envy it if you like. That does not make us friends.",
                "I tell {you:name} that envying it does not make us friends.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  D — asked about the place
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode AboutPlace() => new(
        nodeId:          "about_place",
        replica:         "It is what it looks like. People work, eat, and talk about each other. What do you want to know?",
        replicaIndirect: "I tell them people here work and eat and talk about each other, and ask what they want to know.",
        replicaHeard:    "The stranger says people here work and eat and talk about each other, and asks what I want to know.",

        new PlayerOption("ask_the_folk", "ask what the people here are like",
            "What are the people here like?",
            "I ask them what the people here are like.",
            End("place_the_folk", 2,
                "{npc:opinion_neighbours} You will get on well enough if you do not give yourself airs.",
                "I tell them of my neighbours that {npc:opinion_neighbours}, and that they will get on if they do not give themselves airs.",
                "You will find out. I will not form your opinion for you.",
                "I tell them they will find out, and that I will not form their opinion for them.")),

        new PlayerOption("ask_the_road", "ask about the road and who comes down it",
            "Do many people come down that road?",
            "I ask them whether many people come down that road.",
            PlaceTheRoad()),

        new PlayerOption("ask_dangers", "ask whether there is anything to watch out for",
            "Is there anything a newcomer should watch out for?",
            "I ask them whether a newcomer should watch out for anything.",
            End("place_dangers", 2,
                "Nothing, so long as you take nothing that is not yours.",
                "I tell them nothing, so long as they take nothing that is not theirs.",
                "A newcomer asking what is worth watching. Move along.",
                "I tell them to move along.")));

    private static NpcLineNode PlaceTheRoad() => new(
        nodeId:          "place_the_road",
        replica:         "Some. {npc:opinion_roads}",
        replicaIndirect: "I tell them some do, and that of the roads I think {npc:opinion_roads}.",
        replicaHeard:    "The stranger says some do, and tells me what they think of the roads.",

        new PlayerOption("say_not_thief", "make clear you are not one of the bad sort",
            "I am neither of those. You have my word.",
            "I give them my word that I am neither of those.",
            End("road_not_thief", 3,
                "A stranger's word is worth little, but you gave it plainly, and that counts.",
                "I tell them a stranger's word is worth little, but that they gave it plainly.",
                "Everyone says that.",
                "I tell them everyone says that.")),

        new PlayerOption("ask_news", "ask what news the road has brought lately",
            "What news have they brought lately?",
            "I ask them what news those travellers brought lately.",
            End("road_news", 3,
                "Little, and half of it invented. Sit with me sometime and I will tell you the true half.",
                "I tell them it is little and half invented, and invite them to sit with me for the true half.",
                "I share news with people I know. Come back when you are one.",
                "I tell them I share news with people I know.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  E — kept your distance
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode Guarded() => new(
        nodeId:          "guarded",
        replica:         "You are not one for talking. I have work to do anyway.",
        replicaIndirect: "I tell them they are not one for talking, and that I have work to do anyway.",
        replicaHeard:    "The stranger says I am not one for talking, and that they have work to do anyway.",

        new PlayerOption("apologise_curt", "explain that you meant no discourtesy",
            "I meant no offence. I am slow with people I do not know.",
            "I tell them I meant no offence and am slow with people I do not know.",
            End("guarded_apologise", 2,
                "So am I. No offence taken. Go safely.",
                "I tell them I am the same, and wish them a safe road.",
                "I am quick with strangers, and I am done.",
                "I tell them I am quick with strangers, and that I am done.")),

        new PlayerOption("state_business", "state your business plainly instead",
            "Then I will be plain. I am new here and taking the measure of the place.",
            "I tell them I am new here and taking the measure of the place.",
            GuardedBusiness()));

    private static NpcLineNode GuardedBusiness() => new(
        nodeId:          "guarded_business",
        replica:         "Taking the measure of it. That is honest, at least.",
        replicaIndirect: "I tell them that taking the measure of it is honest, at least.",
        replicaHeard:    "The stranger says that taking the measure of it is honest, at least.",

        new PlayerOption("ask_measure", "ask how they would measure it themselves",
            "How would you measure it, in my place?",
            "I ask them how they would measure it in my place.",
            End("guarded_measure", 3,
                "By whether people look you in the eye. Here they mostly do. You will manage.",
                "I tell them to measure it by whether people look them in the eye, and that here they mostly do.",
                "I am not you, and I will not think for you.",
                "I tell them I will not think for them.")),

        new PlayerOption("leave_it_there", "say you have taken enough of their time",
            "I have taken enough of your day. Thank you for talking.",
            "I thank them for talking and say I have taken enough of their day.",
            End("guarded_leave", 3,
                "That is good manners. Come by again, when I have more time.",
                "I tell them that is good manners, and to come by again when I have more time.",
                "You took too much of it a while ago. Go on.",
                "I tell them they took too much of it a while ago.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Entry
    // ══════════════════════════════════════════════════════════════════════════

    private static readonly NpcLineNode Greeting = new(
        nodeId:          "greeting",
        replica:         "Good day. I do not think we have met before.",
        replicaIndirect: "I greet them and say I do not think we have met before.",
        replicaHeard:    "The stranger greets me and says they do not think we have met before.",

        new PlayerOption("greet_warmly", "greet them warmly",
            "Good day to you.",
            "I greet them warmly in return.",
            WarmOpening()),

        new PlayerOption("ask_who", "ask who they are",
            "Who are you?",
            "I ask them who they are.",
            WhoAreYou()),

        new PlayerOption("introduce", "introduce yourself first",
            "My name is {you:name}.",
            "I tell them my name is {you:name}.",
            IntroOpening()),

        new PlayerOption("ask_place", "ask about this place instead of about them",
            "I am new here. What sort of place is this?",
            "I tell them I am new here, and ask what sort of place this is.",
            AboutPlace()),

        new PlayerOption("keep_distance", "answer briefly and keep your distance",
            "No, we have not.",
            "I answer briefly that we have not met.",
            Guarded()));

    public override NpcLineNode EntryNode => Greeting;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
        => npc.AffinityTable.IsStranger(partyMemberId);
}
