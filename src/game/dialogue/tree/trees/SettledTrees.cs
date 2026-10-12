using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.ModiMentis;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Dialogue.Tree.Trees;

// ─────────────────────────────────────────────────────────────────────────────
//  The conversations of the settled country: what a stranger can ask of a priest, a lord, a soldier,
//  a traveller, a scholar, a singer - and what a crooked one can offer a guard. Each is gated on the
//  kind of person who can answer it, by type, and opened by a verb of the same id (SettledSocialVerbs).
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// "Ask a Blessing" — asking a holy person for a blessing.
///
/// <para>Asking a priest or a monk to bless you. Open to strangers - a blessing is not refused for not having been introduced - and gentle in both directions: a blessing given warms them to you, one refused changes nothing.</para>
///
/// <para>The <see cref="BranchDifficulty.Easy"/> ladder. Each of the six endings teaches its own
/// pair of lessons, so what is learned depends on how the thing was asked as much as whether it worked.</para>
/// </summary>
public class AskBlessingTree : DialogueTree
{
    public override string TreeId           => "ask_blessing";
    public override string DisplayName      => "Ask a Blessing";
    public override string Description      => "asking a holy person for a blessing";
    public override string NpcDescription   => "being asked for a blessing";
    public override string AssociatedVerbId => "ask_blessing";
    public override string? GrantedModusMentisId => "devotion";

    public override IReadOnlyList<Outcome> SuccessOutcomes => new Outcome[] { new AffinityIncrementOutcome(1) };
    public override IReadOnlyList<Outcome> FailureOutcomes => new Outcome[] { new AffinityIncrementOutcome(0) };

    private static ResolutionNode End(string id, int depth, string success, string successIndirect,
                                      string failure, string failureIndirect, params System.Type[] lessons) => new(
        nodeId:                 id,
        difficulty:             BranchDifficulty.Easy(depth),
        successReplica:         success,
        successReplicaIndirect: successIndirect,
        failureReplica:         failure,
        failureReplicaIndirect: failureIndirect,
        mode:                   ResolutionMode.DiceCheck,
        topic:                  null,
        lessons:                lessons);

    private static readonly NpcLineNode ForRoad = new(
        nodeId:          "for_road_pressed",
        replica:         "The road is long for everyone. Where does yours go?",
        replicaIndirect: "I ask them where their road goes.",
        replicaHeard:    "They ask me where my road goes.",

            new PlayerOption("road_honest", "tell them honestly I do not know",
                "I do not know. That is why I asked.",
                "I tell them honestly I do not know where it goes.",
                End("road_honest_end", 2,
                    "Then go with this, and it will find the way.",
                    "I bless them and tell them the way will be found.",
                    "Then come back when you know what you are asking for.",
                    "I tell them to come back when they know what they ask.",
                    typeof(VigilModusMentis), typeof(AuguryModusMentis))),

            new PlayerOption("road_far", "name somewhere far and grand",
                "To the great cities, and further.",
                "I tell them I am going to the great cities and further.",
                End("road_far_end", 2,
                    "Ambition is no sin, if it walks humbly. Go blessed.",
                    "I bless them and warn them to walk humbly.",
                    "That is pride talking, and pride needs no blessing of mine.",
                    "I tell them it is pride talking and I will not bless it.",
                    typeof(HomesicknessModusMentis), typeof(ChantModusMentis))));

    private static readonly NpcLineNode ForSin = new(
        nodeId:          "for_sin_pressed",
        replica:         "A blessing is not a pardon. Do you want one, or the other?",
        replicaIndirect: "I ask whether they want a blessing or a pardon.",
        replicaHeard:    "They ask whether I want a blessing or a pardon.",

            new PlayerOption("sin_blessing", "say a blessing is enough",
                "Only a blessing. The rest is mine to carry.",
                "I tell them a blessing is enough and the rest is mine to carry.",
                End("sin_blessing_end", 2,
                    "Then carry it with this on you. It is lighter than you think.",
                    "I bless them and tell them the weight is lighter than they think.",
                    "A thing carried that long wants more than words. Come back when you are ready.",
                    "I tell them it wants more than words, and to come back ready.",
                    typeof(ContritionModusMentis), typeof(MortificationModusMentis))),

            new PlayerOption("sin_pardon", "ask for the pardon instead",
                "Then the pardon, if you will give it.",
                "I ask them for the pardon instead.",
                End("sin_pardon_end", 2,
                    "Kneel, then. It is given.",
                    "I have them kneel, and I give it.",
                    "Pardon is not given at a street corner. Come to the temple.",
                    "I tell them pardon is not given at a street corner.",
                    typeof(AbsolutionModusMentis), typeof(DevotionModusMentis))));

    private static readonly NpcLineNode ForDead = new(
        nodeId:          "for_dead_pressed",
        replica:         "Who were they to you?",
        replicaIndirect: "I ask them who the dead were to them.",
        replicaHeard:    "They ask me who the dead were to me.",

            new PlayerOption("dead_kin", "say they were kin",
                "Kin. I was not there at the end.",
                "I tell them the dead were kin and I was not there at the end.",
                End("dead_kin_end", 2,
                    "Then I will say the words that were not said. Listen.",
                    "I say the words for the dead that were not said at the end.",
                    "Grief asks more than I can give on a doorstep. Come to the evening office.",
                    "I tell them grief asks more than I can give here.",
                    typeof(MourningModusMentis), typeof(ObsequiesModusMentis))),

            new PlayerOption("dead_stranger", "say I barely knew them",
                "Nobody. I found them on the road.",
                "I tell them I barely knew the dead and found them on the road.",
                End("dead_stranger_end", 2,
                    "Few would ask for a stranger. That is worth a blessing in itself.",
                    "I bless the stranger and the one who asked.",
                    "Then leave the stranger to the gods who knew them.",
                    "I tell them to leave the stranger to the gods.",
                    typeof(VigilModusMentis), typeof(ContritionModusMentis))));

    private static readonly NpcLineNode Approach = new(
        nodeId:          "approach",
        replica:         "You want something of me, child?",
        replicaIndirect: "I ask them gently what they want of me.",
        replicaHeard:    "They ask me gently what I want of them.",

        new PlayerOption("for_road", "ask a blessing for the road ahead",
            "Bless me for the road. I have far to go.",
            "I ask them to bless me for the long road ahead.",
            ForRoad),

        new PlayerOption("for_sin", "ask a blessing to lighten something I carry",
            "Bless me. I carry something I would rather not.",
            "I ask them to bless me, because I carry something heavy.",
            ForSin),

        new PlayerOption("for_dead", "ask a blessing for someone dead",
            "Bless someone for me. They cannot ask for themselves now.",
            "I ask them to bless someone who is dead.",
            ForDead));

    public override NpcLineNode EntryNode => Approach;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId) => npc.Archetype is PriestArchetype or MonkArchetype;
}

/// <summary>
/// "Confess" — confessing something to a priest.
///
/// <para>Confessing to a priest or a monk who knows you. Harder than asking a blessing: a confession that rings false costs their good opinion, one that rings true wins it.</para>
///
/// <para>The <see cref="BranchDifficulty.Hard"/> ladder. Each of the six endings teaches its own
/// pair of lessons, so what is learned depends on how the thing was asked as much as whether it worked.</para>
/// </summary>
public class ConfessTree : DialogueTree
{
    public override string TreeId           => "confess";
    public override string DisplayName      => "Confess";
    public override string Description      => "confessing something to a priest";
    public override string NpcDescription   => "hearing a confession";
    public override string AssociatedVerbId => "confess";
    public override string? GrantedModusMentisId => "confession";

    public override IReadOnlyList<Outcome> SuccessOutcomes => new Outcome[] { new AffinityIncrementOutcome(1) };
    public override IReadOnlyList<Outcome> FailureOutcomes => new Outcome[] { new AffinityIncrementOutcome(-1) };

    private static ResolutionNode End(string id, int depth, string success, string successIndirect,
                                      string failure, string failureIndirect, params System.Type[] lessons) => new(
        nodeId:                 id,
        difficulty:             BranchDifficulty.Hard(depth),
        successReplica:         success,
        successReplicaIndirect: successIndirect,
        failureReplica:         failure,
        failureReplicaIndirect: failureIndirect,
        mode:                   ResolutionMode.DiceCheck,
        topic:                  null,
        lessons:                lessons);

    private static readonly NpcLineNode Violence = new(
        nodeId:          "violence_pressed",
        replica:         "Meant to, or let yourself?",
        replicaIndirect: "I ask whether they meant it or let themselves.",
        replicaHeard:    "They ask whether I meant it or let myself.",

            new PlayerOption("violence_meant", "admit I meant it",
                "I meant it. At the time.",
                "I admit I meant it at the time.",
                End("violence_meant_end", 2,
                    "Then the sorrow you feel now is the beginning of the cure. Go on.",
                    "I tell them their sorrow now is the beginning of the cure.",
                    "Then you have not come to confess. You have come to be told it was fine.",
                    "I tell them they came to be told it was fine.",
                    typeof(ContritionModusMentis), typeof(AbsolutionModusMentis))),

            new PlayerOption("violence_let", "say I let myself",
                "I let myself. It was easier than stopping.",
                "I tell them I let myself because it was easier.",
                End("violence_let_end", 2,
                    "That is the truest thing anyone has said to me this week.",
                    "I tell them it is the truest thing I have heard this week.",
                    "Easier. Everyone says easier.",
                    "I tell them everyone says easier.",
                    typeof(MortificationModusMentis), typeof(TheologyModusMentis))));

    private static readonly NpcLineNode Greed = new(
        nodeId:          "greed_pressed",
        replica:         "Can it be given back?",
        replicaIndirect: "I ask whether it can be given back.",
        replicaHeard:    "They ask whether it can be given back.",

            new PlayerOption("greed_back", "say it can and I will",
                "It can. I will.",
                "I tell them it can be given back and I will do it.",
                End("greed_back_end", 2,
                    "Then you hardly need me. Go and do it.",
                    "I tell them they hardly need me and to go and do it.",
                    "Saying will is cheap here. Come back when it is done.",
                    "I tell them to come back when it is done.",
                    typeof(AbsolutionModusMentis), typeof(AlmsgivingModusMentis))),

            new PlayerOption("greed_gone", "say it is spent and gone",
                "It is spent. Long gone.",
                "I tell them it is spent and long gone.",
                End("greed_gone_end", 2,
                    "Then give to someone else what you cannot give to them. That is the penance.",
                    "I set them the penance of giving to others.",
                    "Then there is nothing to confess but the fact you would do it again.",
                    "I tell them they would do it again.",
                    typeof(TithingModusMentis), typeof(HomilyModusMentis), typeof(SimonyModusMentis))));

    private static readonly NpcLineNode Doubt = new(
        nodeId:          "doubt_pressed",
        replica:         "Doubt is not the opposite of faith. What is it you doubt?",
        replicaIndirect: "I ask what it is they doubt.",
        replicaHeard:    "They ask me what it is I doubt.",

            new PlayerOption("doubt_gods", "say I doubt the gods listen",
                "That anyone is listening.",
                "I tell them I doubt anyone is listening.",
                End("doubt_gods_end", 2,
                    "I have doubted that in the night myself. It passes, and it comes back. Both are prayer.",
                    "I tell them my own doubts, and that both are prayer.",
                    "Then why are you here, talking to me?",
                    "I ask why they are here talking to me.",
                    typeof(TheologyModusMentis), typeof(MysticismModusMentis))),

            new PlayerOption("doubt_priests", "say I doubt the priests",
                "Not the gods. You. All of you.",
                "I tell them I doubt the priests, not the gods.",
                End("doubt_priests_end", 2,
                    "Good. So do I, most days. Doubt us and keep the gods.",
                    "I tell them to doubt us and keep the gods.",
                    "Then you have come to the wrong door.",
                    "I tell them they have come to the wrong door.",
                    typeof(HeresyModusMentis), typeof(ZealModusMentis))));

    private static readonly NpcLineNode Approach = new(
        nodeId:          "approach",
        replica:         "Speak, then. Nothing said here leaves it.",
        replicaIndirect: "I tell them nothing said here leaves it.",
        replicaHeard:    "They tell me nothing said here leaves it.",

        new PlayerOption("violence", "confess to having hurt someone",
            "I have hurt someone. More than I meant to.",
            "I confess I hurt someone more than I meant to.",
            Violence),

        new PlayerOption("greed", "confess to having taken what was not mine",
            "I took what was not mine.",
            "I confess I took what was not mine.",
            Greed),

        new PlayerOption("doubt", "confess to doubting the gods",
            "I doubt. All of it.",
            "I confess I doubt all of it.",
            Doubt));

    public override NpcLineNode EntryNode => Approach;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId) => npc.Archetype is PriestArchetype or MonkArchetype;
}

/// <summary>
/// "Petition" — petitioning someone in authority.
///
/// <para>Putting a request to someone in authority - a lord, a steward, a captain. Open to strangers, as petitions always are; badly put, it costs their goodwill.</para>
///
/// <para>The <see cref="BranchDifficulty.Hard"/> ladder. Each of the six endings teaches its own
/// pair of lessons, so what is learned depends on how the thing was asked as much as whether it worked.</para>
/// </summary>
public class PetitionTree : DialogueTree
{
    public override string TreeId           => "petition";
    public override string DisplayName      => "Petition";
    public override string Description      => "petitioning someone in authority";
    public override string NpcDescription   => "being petitioned";
    public override string AssociatedVerbId => "petition";
    public override string? GrantedModusMentisId => "petitioning";

    public override IReadOnlyList<Outcome> SuccessOutcomes => new Outcome[] { new AffinityIncrementOutcome(1) };
    public override IReadOnlyList<Outcome> FailureOutcomes => new Outcome[] { new AffinityIncrementOutcome(-1) };

    private static ResolutionNode End(string id, int depth, string success, string successIndirect,
                                      string failure, string failureIndirect, params System.Type[] lessons) => new(
        nodeId:                 id,
        difficulty:             BranchDifficulty.Hard(depth),
        successReplica:         success,
        successReplicaIndirect: successIndirect,
        failureReplica:         failure,
        failureReplicaIndirect: failureIndirect,
        mode:                   ResolutionMode.DiceCheck,
        topic:                  null,
        lessons:                lessons);

    private static readonly NpcLineNode Justice = new(
        nodeId:          "justice_pressed",
        replica:         "By whom, and with what proof?",
        replicaIndirect: "I ask by whom and with what proof.",
        replicaHeard:    "They ask by whom and with what proof.",

            new PlayerOption("justice_law", "cite the law plainly",
                "The law is plain on it. I ask only that it be kept.",
                "I tell them the law is plain and ask only that it be kept.",
                End("justice_law_end", 2,
                    "It is plain. It will be kept. My clerk will hear the rest.",
                    "I tell them the law will be kept.",
                    "The law is plain to everyone who has not read it.",
                    "I tell them the law is plain only to those who have not read it.",
                    typeof(JurisprudenceModusMentis), typeof(RankModusMentis))),

            new PlayerOption("justice_mercy", "appeal to their sense of fairness",
                "No proof. Only that you are known to be fair.",
                "I appeal to their reputation for fairness.",
                End("justice_mercy_end", 2,
                    "Flattery and honesty together. I will look into it.",
                    "I tell them I will look into it.",
                    "Fairness without proof is favour. I do not grant favours to strangers.",
                    "I tell them I do not grant favours to strangers.",
                    typeof(FlatteryModusMentis), typeof(DiplomacyModusMentis))));

    private static readonly NpcLineNode Favour = new(
        nodeId:          "favour_pressed",
        replica:         "Generosity is not a purse anyone may open. What favour?",
        replicaIndirect: "I ask what favour.",
        replicaHeard:    "They ask me what favour.",

            new PlayerOption("favour_small", "ask for something small",
                "Only leave to pass, and a letter saying so.",
                "I ask only for leave to pass and a letter.",
                End("favour_small_end", 2,
                    "That is small enough. Take it.",
                    "I grant them the letter.",
                    "Small things add up. No.",
                    "I refuse; small things add up.",
                    typeof(PetitioningModusMentis), typeof(PrecedenceModusMentis))),

            new PlayerOption("favour_large", "ask for something large",
                "A place in your household.",
                "I ask them for a place in their household.",
                End("favour_large_end", 2,
                    "You ask boldly. I like that, once.",
                    "I tell them I like boldness, once.",
                    "You forget yourself.",
                    "I tell them they forget themselves.",
                    typeof(AmbitionModusMentis), typeof(DisdainModusMentis))));

    private static readonly NpcLineNode Tribute = new(
        nodeId:          "tribute_pressed",
        replica:         "So you buy your hearing. What is it?",
        replicaIndirect: "I ask what they bring.",
        replicaHeard:    "They ask what I bring.",

            new PlayerOption("tribute_news", "offer news from the road",
                "News from the road. Who moves, and where.",
                "I offer them news from the road.",
                End("tribute_news_end", 2,
                    "That is worth more than coin. Ask.",
                    "I tell them the news is worth more than coin.",
                    "Gossip. I have a steward for gossip.",
                    "I tell them I have a steward for gossip.",
                    typeof(IntrigueModusMentis), typeof(StatecraftModusMentis))),

            new PlayerOption("tribute_coin", "offer coin",
                "Coin. Not much, but it is yours.",
                "I offer them coin.",
                End("tribute_coin_end", 2,
                    "Honest of you to say so. Ask, then.",
                    "I tell them it is honest, and to ask.",
                    "You insult me with that.",
                    "I tell them they insult me.",
                    typeof(LargesseModusMentis), typeof(BriberyModusMentis))));

    private static readonly NpcLineNode Approach = new(
        nodeId:          "approach",
        replica:         "You have a petition. Be brief.",
        replicaIndirect: "I tell them to be brief with their petition.",
        replicaHeard:    "They tell me to be brief with my petition.",

        new PlayerOption("justice", "ask for justice against someone",
            "I ask for justice. I was wronged.",
            "I ask them for justice, because I was wronged.",
            Justice),

        new PlayerOption("favour", "ask for a favour",
            "I ask a favour of my lord's generosity.",
            "I ask a favour of their generosity.",
            Favour),

        new PlayerOption("tribute", "offer something first and then ask",
            "I bring something, and then I ask.",
            "I offer something first and then ask.",
            Tribute));

    public override NpcLineNode EntryNode => Approach;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId) => npc.Archetype is LordArchetype or StewardArchetype or CaptainArchetype;
}

/// <summary>
/// "Talk Soldiering" — talking soldiering with a soldier.
///
/// <para>Talking the trade with a guard or a captain you know: the drill, the old campaigns, the officers. Warms them if it goes well, and costs nothing if it does not.</para>
///
/// <para>The <see cref="BranchDifficulty.Easy"/> ladder. Each of the six endings teaches its own
/// pair of lessons, so what is learned depends on how the thing was asked as much as whether it worked.</para>
/// </summary>
public class TalkSoldieringTree : DialogueTree
{
    public override string TreeId           => "talk_soldiering";
    public override string DisplayName      => "Talk Soldiering";
    public override string Description      => "talking soldiering with a soldier";
    public override string NpcDescription   => "talking soldiering";
    public override string AssociatedVerbId => "talk_soldiering";
    public override string? GrantedModusMentisId => "barrack_wit";

    public override IReadOnlyList<Outcome> SuccessOutcomes => new Outcome[] { new AffinityIncrementOutcome(1) };
    public override IReadOnlyList<Outcome> FailureOutcomes => new Outcome[] { new AffinityIncrementOutcome(0) };

    private static ResolutionNode End(string id, int depth, string success, string successIndirect,
                                      string failure, string failureIndirect, params System.Type[] lessons) => new(
        nodeId:                 id,
        difficulty:             BranchDifficulty.Easy(depth),
        successReplica:         success,
        successReplicaIndirect: successIndirect,
        failureReplica:         failure,
        failureReplicaIndirect: failureIndirect,
        mode:                   ResolutionMode.DiceCheck,
        topic:                  null,
        lessons:                lessons);

    private static readonly NpcLineNode Campaign = new(
        nodeId:          "campaign_pressed",
        replica:         "The river forts, mostly. You heard about the ford?",
        replicaIndirect: "I ask whether they heard about the ford.",
        replicaHeard:    "They ask whether I heard about the ford.",

            new PlayerOption("campaign_heard", "say I heard it was a hard fight",
                "I heard it was hard.",
                "I tell them I heard it was hard.",
                End("campaign_heard_end", 2,
                    "Hard. That's one word. I'll tell you the rest over a pot.",
                    "I promise to tell them the rest over a pot.",
                    "You heard. You weren't there. Leave it.",
                    "I tell them to leave it.",
                    typeof(WarWearinessModusMentis), typeof(EspritDeCorpsModusMentis))),

            new PlayerOption("campaign_glory", "say I heard it was glorious",
                "I heard it was glorious.",
                "I tell them I heard it was glorious.",
                End("campaign_glory_end", 2,
                    "Ha! It was, for an hour. Then it was just loud.",
                    "I laugh and tell them it was glorious for an hour.",
                    "Glorious. Somebody's been reading broadsheets.",
                    "I scoff at the broadsheets.",
                    typeof(ValorModusMentis), typeof(BarrackWitModusMentis))));

    private static readonly NpcLineNode Officers = new(
        nodeId:          "officers_pressed",
        replica:         "Not me. Ours is a fool in a fine hat.",
        replicaIndirect: "I tell them ours is a fool in a fine hat.",
        replicaHeard:    "They tell me theirs is a fool in a fine hat.",

            new PlayerOption("officers_agree", "agree warmly",
                "They all are. The hat's the qualification.",
                "I agree that the hat is the qualification.",
                End("officers_agree_end", 2,
                    "Ha! You've served, all right.",
                    "I tell them they have served, all right.",
                    "Careful. Walls have ears and so do sergeants.",
                    "I warn them that sergeants have ears.",
                    typeof(RankModusMentis), typeof(BarrackWitModusMentis))),

            new PlayerOption("officers_defend", "defend the officer",
                "Someone has to give the orders.",
                "I tell them someone has to give the orders.",
                End("officers_defend_end", 2,
                    "True enough. Better a fool than nobody.",
                    "I allow that a fool is better than nobody.",
                    "Spoken like an officer's dog.",
                    "I call them an officer's dog.",
                    typeof(MusterModusMentis), typeof(DisciplineModusMentis))));

    private static readonly NpcLineNode Drill = new(
        nodeId:          "drill_pressed",
        replica:         "Walk. Never sit. And never the same round twice.",
        replicaIndirect: "I tell them to walk and never repeat the round.",
        replicaHeard:    "They tell me to walk and never repeat the round.",

            new PlayerOption("drill_listen", "ask what they listen for",
                "What do you listen for?",
                "I ask what they listen for.",
                End("drill_listen_end", 2,
                    "For what's stopped. The frogs, the dog. Silence comes first.",
                    "I tell them silence comes first.",
                    "For trouble. What else?",
                    "I tell them trouble, what else.",
                    typeof(PatrolModusMentis), typeof(BastionEyeModusMentis))),

            new PlayerOption("drill_ground", "ask where they stand",
                "And where do you stand?",
                "I ask where they stand.",
                End("drill_ground_end", 2,
                    "Wall at your back, door in your eye. Always.",
                    "I tell them wall at the back, door in the eye.",
                    "Where I'm told.",
                    "I tell them where I'm told.",
                    typeof(BastionEyeModusMentis), typeof(DrillModusMentis))));

    private static readonly NpcLineNode Approach = new(
        nodeId:          "approach",
        replica:         "You've the look of someone who's stood a watch. Have you?",
        replicaIndirect: "I ask whether they have stood a watch.",
        replicaHeard:    "They ask whether I have stood a watch.",

        new PlayerOption("campaign", "ask about their campaigns",
            "Where have you served?",
            "I ask them where they have served.",
            Campaign),

        new PlayerOption("officers", "grumble about officers",
            "Officers. Who'd have them?",
            "I grumble about officers.",
            Officers),

        new PlayerOption("drill", "ask how they keep the watch",
            "How do you keep awake on the dead watch?",
            "I ask how they keep awake on the dead watch.",
            Drill));

    public override NpcLineNode EntryNode => Approach;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId) => npc.Archetype is GuardArchetype or CaptainArchetype;
}

/// <summary>
/// "Talk of Far Places" — talking of far places with a traveller.
///
/// <para>Asking a merchant or a sailor about the places they have seen. Travellers like to be asked.</para>
///
/// <para>The <see cref="BranchDifficulty.Easy"/> ladder. Each of the six endings teaches its own
/// pair of lessons, so what is learned depends on how the thing was asked as much as whether it worked.</para>
/// </summary>
public class TalkOfFarPlacesTree : DialogueTree
{
    public override string TreeId           => "talk_far_places";
    public override string DisplayName      => "Talk of Far Places";
    public override string Description      => "talking of far places with a traveller";
    public override string NpcDescription   => "talking of far places";
    public override string AssociatedVerbId => "talk_far_places";
    public override string? GrantedModusMentisId => "worldliness";

    public override IReadOnlyList<Outcome> SuccessOutcomes => new Outcome[] { new AffinityIncrementOutcome(1) };
    public override IReadOnlyList<Outcome> FailureOutcomes => new Outcome[] { new AffinityIncrementOutcome(0) };

    private static ResolutionNode End(string id, int depth, string success, string successIndirect,
                                      string failure, string failureIndirect, params System.Type[] lessons) => new(
        nodeId:                 id,
        difficulty:             BranchDifficulty.Easy(depth),
        successReplica:         success,
        successReplicaIndirect: successIndirect,
        failureReplica:         failure,
        failureReplicaIndirect: failureIndirect,
        mode:                   ResolutionMode.DiceCheck,
        topic:                  null,
        lessons:                lessons);

    private static readonly NpcLineNode Sea = new(
        nodeId:          "sea_pressed",
        replica:         "Like the world forgot you. Three weeks of it, once.",
        replicaIndirect: "I tell them about three weeks out of sight of land.",
        replicaHeard:    "They tell me about three weeks out of sight of land.",

            new PlayerOption("sea_fear", "ask if they were afraid",
                "Were you afraid?",
                "I ask if they were afraid.",
                End("sea_fear_end", 2,
                    "Every night. You learn to sleep afraid. It's a skill.",
                    "I tell them sleeping afraid is a skill.",
                    "Afraid? Don't be soft.",
                    "I tell them not to be soft.",
                    typeof(SeaLegsModusMentis), typeof(NavigationModusMentis))),

            new PlayerOption("sea_work", "ask what the work is",
                "What did you do, all that time?",
                "I ask what they did all that time.",
                End("sea_work_end", 2,
                    "Hauled, spliced, mended canvas. The sea's hard on everything.",
                    "I tell them about hauling and splicing and mending.",
                    "Worked. What do you think?",
                    "I tell them we worked.",
                    typeof(RiggingModusMentis), typeof(SailmakingModusMentis))));

    private static readonly NpcLineNode Ports = new(
        nodeId:          "ports_pressed",
        replica:         "In the south they pay in salt and bargain with their fingers.",
        replicaIndirect: "I tell them about salt and finger-bargaining in the south.",
        replicaHeard:    "They tell me about salt and finger-bargaining in the south.",

            new PlayerOption("ports_words", "ask how they made themselves understood",
                "How did you make yourself understood?",
                "I ask how they made themselves understood.",
                End("ports_words_end", 2,
                    "Three words of theirs, two of mine, and the fingers. Here, like this.",
                    "I show them how to bargain with words and fingers.",
                    "You don't. You point and shout.",
                    "I tell them you point and shout.",
                    typeof(PidginModusMentis), typeof(WorldlinessModusMentis))),

            new PlayerOption("ports_wealth", "ask where the money is",
                "Where's the money, out there?",
                "I ask where the money is.",
                End("ports_wealth_end", 2,
                    "Spice. Always spice. Buy at the coast, sell inland, and lend the profit.",
                    "I tell them spice and lending are where the money is.",
                    "If I knew that I'd not be standing here.",
                    "I tell them if I knew I'd not be here.",
                    typeof(CovetousnessModusMentis), typeof(UsuryModusMentis))));

    private static readonly NpcLineNode Home = new(
        nodeId:          "home_pressed",
        replica:         "Which one? I've had five.",
        replicaIndirect: "I ask which one, having had five.",
        replicaHeard:    "They ask which one; they have had five.",

            new PlayerOption("home_first", "ask about the first",
                "The first one.",
                "I ask about the first.",
                End("home_first_end", 2,
                    "Every day. Smell of the bakehouse on the corner. Gone now, I expect.",
                    "I tell them I miss the first every day.",
                    "Don't. Ask me something else.",
                    "I ask them to ask something else.",
                    typeof(HomesicknessModusMentis), typeof(ElegyModusMentis))),

            new PlayerOption("home_none", "say perhaps home is the road",
                "Perhaps the road is home.",
                "I suggest the road is home.",
                End("home_none_end", 2,
                    "Now you've got it. The road and the rail and the next port.",
                    "I agree the road is home.",
                    "Spoken by someone who's never been far.",
                    "I tell them they've never been far.",
                    typeof(WanderlustModusMentis), typeof(WorldlinessModusMentis))));

    private static readonly NpcLineNode Approach = new(
        nodeId:          "approach",
        replica:         "You've the look of someone who wants to hear about elsewhere.",
        replicaIndirect: "I tell them they look like they want to hear about elsewhere.",
        replicaHeard:    "They tell me I look like I want to hear about elsewhere.",

        new PlayerOption("sea", "ask about the sea",
            "What is it like, out of sight of land?",
            "I ask what it is like out of sight of land.",
            Sea),

        new PlayerOption("ports", "ask about foreign ports",
            "Tell me about the ports.",
            "I ask about the foreign ports.",
            Ports),

        new PlayerOption("home", "ask if they miss home",
            "Do you miss home, out there?",
            "I ask if they miss home.",
            Home));

    public override NpcLineNode EntryNode => Approach;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId) => npc.Archetype is MerchantArchetype or SailorArchetype;
}

/// <summary>
/// "Offer a Bribe" — offering someone a bribe.
///
/// <para>Offering a guard, a clerk, a steward or a captain something to look the other way. Open to strangers - that is who bribes are for - and costly when refused.</para>
///
/// <para>The <see cref="BranchDifficulty.Hard"/> ladder. Each of the six endings teaches its own
/// pair of lessons, so what is learned depends on how the thing was asked as much as whether it worked.</para>
/// </summary>
public class OfferBribeTree : DialogueTree
{
    public override string TreeId           => "offer_bribe";
    public override string DisplayName      => "Offer a Bribe";
    public override string Description      => "offering someone a bribe";
    public override string NpcDescription   => "being offered a bribe";
    public override string AssociatedVerbId => "offer_bribe";
    public override string? GrantedModusMentisId => "bribery";

    public override IReadOnlyList<Outcome> SuccessOutcomes => new Outcome[] { new AffinityIncrementOutcome(1) };
    public override IReadOnlyList<Outcome> FailureOutcomes => new Outcome[] { new AffinityIncrementOutcome(-1) };

    private static ResolutionNode End(string id, int depth, string success, string successIndirect,
                                      string failure, string failureIndirect, params System.Type[] lessons) => new(
        nodeId:                 id,
        difficulty:             BranchDifficulty.Hard(depth),
        successReplica:         success,
        successReplicaIndirect: successIndirect,
        failureReplica:         failure,
        failureReplicaIndirect: failureIndirect,
        mode:                   ResolutionMode.DiceCheck,
        topic:                  null,
        lessons:                lessons);

    private static readonly NpcLineNode BlindEye = new(
        nodeId:          "blind_eye_pressed",
        replica:         "And why would I do that?",
        replicaIndirect: "I ask why I would do that.",
        replicaHeard:    "They ask why they would do that.",

            new PlayerOption("blind_coin", "let a coin drop",
                "I think you dropped this.",
                "I let a coin drop and say they dropped it.",
                End("blind_coin_end", 2,
                    "So I did. Careless of me. I wasn't looking.",
                    "I pick it up and stop looking.",
                    "Pick that up and get out of my sight.",
                    "I tell them to pick it up and get out.",
                    typeof(BriberyModusMentis), typeof(AnonymityModusMentis))),

            new PlayerOption("blind_threat", "hint at what I know about them",
                "Because I know what you did last market day.",
                "I hint at what I know about them.",
                End("blind_threat_end", 2,
                    "...Go on, then. Quickly.",
                    "I let them go, quickly.",
                    "Threaten me? Guard!",
                    "I call for the guard.",
                    typeof(ExtortionModusMentis), typeof(IntrigueModusMentis))));

    private static readonly NpcLineNode Papers = new(
        nodeId:          "papers_pressed",
        replica:         "They are not in order.",
        replicaIndirect: "I tell them the papers are not in order.",
        replicaHeard:    "They tell me the papers are not in order.",

            new PlayerOption("papers_fee", "offer a fee for the trouble",
                "For the trouble of finding them in order.",
                "I offer a fee for the trouble.",
                End("papers_fee_end", 2,
                    "Ah. On a second look, they're quite in order.",
                    "I find on a second look they are in order.",
                    "There is no fee for that, and there never will be.",
                    "I tell them there is no such fee.",
                    typeof(ReceivingModusMentis), typeof(BriberyModusMentis))),

            new PlayerOption("papers_friend", "drop the name of someone important",
                "The steward knows me. He'd vouch.",
                "I drop the name of someone important.",
                End("papers_friend_end", 2,
                    "Then I'll not trouble the steward. Go on.",
                    "I decide not to trouble the steward.",
                    "Then fetch him, and he can vouch to my face.",
                    "I tell them to fetch the man.",
                    typeof(PrecedenceModusMentis), typeof(SelfPreservationModusMentis))));

    private static readonly NpcLineNode Cargo = new(
        nodeId:          "cargo_pressed",
        replica:         "What's in it?",
        replicaIndirect: "I ask what is in it.",
        replicaHeard:    "They ask what is in it.",

            new PlayerOption("cargo_nothing", "say nothing worth the bother",
                "Nothing worth your trouble. Here's something that is.",
                "I tell them nothing worth the bother, and offer something that is.",
                End("cargo_nothing_end", 2,
                    "I see a cart of turnips. Turnips, I'll write.",
                    "I write down turnips.",
                    "Open it.",
                    "I tell them to open it.",
                    typeof(SmugglingModusMentis), typeof(BriberyModusMentis))),

            new PlayerOption("cargo_truth", "tell them the truth",
                "Salt. Untaxed.",
                "I tell them it is untaxed salt.",
                End("cargo_truth_end", 2,
                    "Honest smuggler. That's rare. Half the tax to me, then.",
                    "I take half the tax for myself.",
                    "Then it's the Crown's salt now.",
                    "I seize the salt for the Crown.",
                    typeof(SmugglingModusMentis), typeof(ReceivingModusMentis))));

    private static readonly NpcLineNode Approach = new(
        nodeId:          "approach",
        replica:         "What is it you want?",
        replicaIndirect: "I ask what they want.",
        replicaHeard:    "They ask what I want.",

        new PlayerOption("blind_eye", "ask them to look the other way",
            "Just for a moment, look the other way.",
            "I ask them to look the other way for a moment.",
            BlindEye),

        new PlayerOption("papers", "ask for papers to be found in order",
            "My papers. I'd like them found in order.",
            "I ask that my papers be found in order.",
            Papers),

        new PlayerOption("cargo", "ask that a load not be looked at",
            "There's a cart. I'd rather it not be opened.",
            "I ask that a cart not be opened.",
            Cargo));

    public override NpcLineNode EntryNode => Approach;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId) => npc.Archetype is GuardArchetype or ClerkArchetype or StewardArchetype or CaptainArchetype;
}

/// <summary>
/// "Ask to Be Taught" — asking a learned person to teach.
///
/// <para>Asking a scholar, a clerk or a monk you know to teach you something of what they know. A good pupil is a pleasure to them; a poor one is simply sent away.</para>
///
/// <para>The <see cref="BranchDifficulty.Hard"/> ladder. Each of the six endings teaches its own
/// pair of lessons, so what is learned depends on how the thing was asked as much as whether it worked.</para>
/// </summary>
public class AskTeachingTree : DialogueTree
{
    public override string TreeId           => "ask_teaching";
    public override string DisplayName      => "Ask to Be Taught";
    public override string Description      => "asking a learned person to teach";
    public override string NpcDescription   => "being asked to teach";
    public override string AssociatedVerbId => "ask_teaching";
    public override string? GrantedModusMentisId => "pedagogy";

    public override IReadOnlyList<Outcome> SuccessOutcomes => new Outcome[] { new AffinityIncrementOutcome(1) };
    public override IReadOnlyList<Outcome> FailureOutcomes => new Outcome[] { new AffinityIncrementOutcome(0) };

    private static ResolutionNode End(string id, int depth, string success, string successIndirect,
                                      string failure, string failureIndirect, params System.Type[] lessons) => new(
        nodeId:                 id,
        difficulty:             BranchDifficulty.Hard(depth),
        successReplica:         success,
        successReplicaIndirect: successIndirect,
        failureReplica:         failure,
        failureReplicaIndirect: failureIndirect,
        mode:                   ResolutionMode.DiceCheck,
        topic:                  null,
        lessons:                lessons);

    private static readonly NpcLineNode Heavens = new(
        nodeId:          "heavens_pressed",
        replica:         "Do you know how to find the pole?",
        replicaIndirect: "I ask whether they can find the pole.",
        replicaHeard:    "They ask whether I can find the pole.",

            new PlayerOption("heavens_yes", "say yes",
                "From the wagon stars, yes.",
                "I tell them I find it from the wagon stars.",
                End("heavens_yes_end", 2,
                    "Good. Then everything else turns around it. Watch.",
                    "I show them how everything turns around the pole.",
                    "Then you know enough to get home. That is all most need.",
                    "I tell them that is all most need.",
                    typeof(AstronomyModusMentis), typeof(NavigationModusMentis))),

            new PlayerOption("heavens_no", "admit I cannot",
                "No.",
                "I admit I cannot.",
                End("heavens_no_end", 2,
                    "Then we begin at the beginning. There - the wagon. Follow its edge.",
                    "I begin at the beginning with them.",
                    "Then learn that first and come back.",
                    "I tell them to learn that first.",
                    typeof(AstronomyModusMentis), typeof(EnciphermentModusMentis))));

    private static readonly NpcLineNode Body = new(
        nodeId:          "body_pressed",
        replica:         "You want to be a physician, or to stop bleeding in a ditch?",
        replicaIndirect: "I ask whether they want to be a physician or stop bleeding.",
        replicaHeard:    "They ask whether I want to be a physician or stop bleeding.",

            new PlayerOption("body_ditch", "say the ditch",
                "The ditch.",
                "I tell them the ditch.",
                End("body_ditch_end", 2,
                    "Honest. Press here, hard, and bind it so. And set a bone like this.",
                    "I show them how to press, bind and set a bone.",
                    "Then find a physician before you need one.",
                    "I tell them to find a physician.",
                    typeof(BonesettingModusMentis), typeof(SurgeryModusMentis))),

            new PlayerOption("body_physician", "say the physic",
                "The physic. All of it.",
                "I tell them all of the physic.",
                End("body_physician_end", 2,
                    "Then first the parts and where they lie. Liver, here; the great vessel, here.",
                    "I begin with the parts and where they lie.",
                    "That is years, not an afternoon.",
                    "I tell them that is years.",
                    typeof(AnatomyLoreModusMentis), typeof(DiagnosisModusMentis))));

    private static readonly NpcLineNode Law = new(
        nodeId:          "law_pressed",
        replica:         "Which do you want, the law or the truth?",
        replicaIndirect: "I ask whether they want the law or the truth.",
        replicaHeard:    "They ask whether I want the law or the truth.",

            new PlayerOption("law_law", "say the law",
                "The law.",
                "I tell them the law.",
                End("law_law_end", 2,
                    "Then remember: it turns on witnesses. Always ask who saw.",
                    "I teach them that law turns on witnesses.",
                    "Then go to a lawyer and pay him.",
                    "I tell them to pay a lawyer.",
                    typeof(JurisprudenceModusMentis), typeof(ClerkshipModusMentis))),

            new PlayerOption("law_truth", "say the truth",
                "The truth.",
                "I tell them the truth.",
                End("law_truth_end", 2,
                    "Dangerous. Sit down. The texts disagree with themselves, and here is how.",
                    "I show them how the texts disagree.",
                    "Then you are in the wrong building.",
                    "I tell them they are in the wrong building.",
                    typeof(TheologyModusMentis), typeof(AlchemyModusMentis))));

    private static readonly NpcLineNode Approach = new(
        nodeId:          "approach",
        replica:         "Teach you? What is it you want to know?",
        replicaIndirect: "I ask what they want to know.",
        replicaHeard:    "They ask what I want to know.",

        new PlayerOption("heavens", "ask about the stars",
            "The stars. What they are, how they move.",
            "I ask about the stars.",
            Heavens),

        new PlayerOption("body", "ask about the body and its healing",
            "How the body is made. How it mends.",
            "I ask how the body is made and mends.",
            Body),

        new PlayerOption("law", "ask about the law and the old texts",
            "The law. And the old books it comes from.",
            "I ask about the law and the old books.",
            Law));

    public override NpcLineNode EntryNode => Approach;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId) => npc.Archetype is ScholarArchetype or ClerkArchetype or MonkArchetype;
}

/// <summary>
/// "Sing Along" — singing along with someone.
///
/// <para>Joining in with a sailor, an innkeeper, a picker, a drover or a monk who sings at their work. Nothing is lost by singing badly.</para>
///
/// <para>The <see cref="BranchDifficulty.Easy"/> ladder. Each of the six endings teaches its own
/// pair of lessons, so what is learned depends on how the thing was asked as much as whether it worked.</para>
/// </summary>
public class SingAlongTree : DialogueTree
{
    public override string TreeId           => "sing_along";
    public override string DisplayName      => "Sing Along";
    public override string Description      => "singing along with someone";
    public override string NpcDescription   => "singing with someone";
    public override string AssociatedVerbId => "sing_along";
    public override string? GrantedModusMentisId => "solfege";

    public override IReadOnlyList<Outcome> SuccessOutcomes => new Outcome[] { new AffinityIncrementOutcome(1) };
    public override IReadOnlyList<Outcome> FailureOutcomes => new Outcome[] { new AffinityIncrementOutcome(0) };

    private static ResolutionNode End(string id, int depth, string success, string successIndirect,
                                      string failure, string failureIndirect, params System.Type[] lessons) => new(
        nodeId:                 id,
        difficulty:             BranchDifficulty.Easy(depth),
        successReplica:         success,
        successReplicaIndirect: successIndirect,
        failureReplica:         failure,
        failureReplicaIndirect: failureIndirect,
        mode:                   ResolutionMode.DiceCheck,
        topic:                  null,
        lessons:                lessons);

    private static readonly NpcLineNode Work = new(
        nodeId:          "work_pressed",
        replica:         "Haul on the answer, then. Ready?",
        replicaIndirect: "I tell them to haul on the answer.",
        replicaHeard:    "They tell me to haul on the answer.",

            new PlayerOption("work_loud", "answer loudly",
                "Away, haul away!",
                "I roar the answer.",
                End("work_loud_end", 2,
                    "That's it! You've done this before.",
                    "I tell them they have done this before.",
                    "Late on the beat. Never mind.",
                    "I tell them they were late on the beat.",
                    typeof(ShantyModusMentis), typeof(HarvestJoyModusMentis))),

            new PlayerOption("work_steady", "keep the beat quietly",
                "I hum the beat and keep it.",
                "I keep the beat quietly.",
                End("work_steady_end", 2,
                    "Steady's better than loud. Good.",
                    "I tell them steady is better than loud.",
                    "You'll have to open your mouth sometime.",
                    "I tell them to open their mouth.",
                    typeof(ShantyModusMentis), typeof(BuskingModusMentis))));

    private static readonly NpcLineNode Old = new(
        nodeId:          "old_pressed",
        replica:         "This was my mother's. Mind the turn in the middle.",
        replicaIndirect: "I tell them it was my mother's.",
        replicaHeard:    "They tell me it was their mother's.",

            new PlayerOption("old_learn", "learn it carefully",
                "Slowly, then. Again from the start.",
                "I ask to learn it slowly.",
                End("old_learn_end", 2,
                    "There. Now it's yours too.",
                    "I tell them it is theirs too now.",
                    "It's not one you learn in an afternoon.",
                    "I tell them it takes more than an afternoon.",
                    typeof(ChantModusMentis), typeof(HomesicknessModusMentis))),

            new PlayerOption("old_harmony", "sing a harmony under it",
                "I find a line under yours.",
                "I sing a harmony under it.",
                End("old_harmony_end", 2,
                    "Oh, that's lovely. Do it again.",
                    "I ask them to do it again.",
                    "Just the tune. Leave it as she sang it.",
                    "I ask them to leave it as she sang it.",
                    typeof(LutePlayingModusMentis), typeof(SolfegeModusMentis))));

    private static readonly NpcLineNode Town = new(
        nodeId:          "town_pressed",
        replica:         "There's a rude one and a proud one.",
        replicaIndirect: "I tell them there is a rude one and a proud one.",
        replicaHeard:    "They tell me there is a rude one and a proud one.",

            new PlayerOption("town_proud", "ask for the proud one",
                "The proud one.",
                "I ask for the proud one.",
                End("town_proud_end", 2,
                    "Stand up for this. Everybody does.",
                    "I have them stand up for it.",
                    "Not with strangers listening.",
                    "I refuse with strangers listening.",
                    typeof(CivicPrideModusMentis), typeof(ChantModusMentis))),

            new PlayerOption("town_rude", "ask for the rude one",
                "The rude one.",
                "I ask for the rude one.",
                End("town_rude_end", 2,
                    "Ha! Lean in. The second verse is about the captain.",
                    "I sing them the rude one.",
                    "Not in here. Not if you want to stay in here.",
                    "I refuse; not in here.",
                    typeof(BuskingModusMentis), typeof(BarrackWitModusMentis))));

    private static readonly NpcLineNode Approach = new(
        nodeId:          "approach",
        replica:         "You know this one?",
        replicaIndirect: "I ask whether they know this one.",
        replicaHeard:    "They ask whether I know this one.",

        new PlayerOption("work", "ask for a work song",
            "Give me one to work to.",
            "I ask for a song to work to.",
            Work),

        new PlayerOption("old", "ask for an old song",
            "Something old. From before.",
            "I ask for something old.",
            Old),

        new PlayerOption("town", "ask for a song about this place",
            "Is there a song about this place?",
            "I ask whether there is a song about this place.",
            Town));

    public override NpcLineNode EntryNode => Approach;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId) => npc.Archetype is SailorArchetype or InnkeeperArchetype or PickerArchetype or DroverArchetype or MonkArchetype;
}
