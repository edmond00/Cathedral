using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.ModiMentis;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Dialogue.Tree.Trees;

// ─────────────────────────────────────────────────────────────────────────────
//  The conversations of a dense city's trades and streets: ordering something made of a craftsman,
//  asking an apothecary or a barber for a remedy, giving alms to a beggar, and hearing the gossip of
//  the people who go into every house. Each is gated on the kind of person who can answer it, by
//  type, and opened by a verb of the same id (CitySocialVerbs).
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// "Commission Work" — ordering something made by a craftsman of the town.
///
/// <para>Asking a cobbler, tailor, potter, chandler or tanner to make something to order. An ordinary
/// matter between acquaintances: an order well placed warms them to you, one that goes nowhere changes
/// nothing.</para>
///
/// <para>The <see cref="BranchDifficulty.Easy"/> ladder. Each of the six endings teaches its own pair
/// of lessons, so what is learned depends on how the order was put as much as whether it was taken.</para>
/// </summary>
public class CommissionWorkTree : DialogueTree
{
    public override string TreeId           => "commission_work";
    public override string DisplayName      => "Commission Work";
    public override string Description      => "ordering something made by a craftsman";
    public override string NpcDescription   => "being asked to make something to order";
    public override string AssociatedVerbId => "commission_work";
    public override string? GrantedModusMentisId => "bargaining";

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

    private static readonly NpcLineNode ForFine = new(
        nodeId:          "fine_pressed",
        replica:         "The best I make costs the best you have. Can you pay for it?",
        replicaIndirect: "I ask them whether they can pay for my best work.",
        replicaHeard:    "They ask me whether I can pay for their best work.",

            new PlayerOption("fine_pay", "promise to pay whatever it costs",
                "Name the price. I will pay it.",
                "I tell them to name the price and I will pay it.",
                End("fine_pay_end", 2,
                    "Then you will have something nobody else in this town has. Come back in a week.",
                    "I take the order and promise them something unique in a week.",
                    "Everyone says that until they see the price. Come back with the money first.",
                    "I tell them to come back with the money first.",
                    typeof(AppraisalModusMentis), typeof(LargesseModusMentis))),

            new PlayerOption("fine_praise", "praise their work instead of answering",
                "I have seen your work. Nobody else in this town comes close.",
                "I tell them nobody else in this town comes close to their work.",
                End("fine_praise_end", 2,
                    "Well. That is true. And since you have an eye for it, I will do it at a fair price.",
                    "I agree, and take the order at a fair price for someone with an eye.",
                    "Flattery does not buy leather. Or cloth. Or anything else.",
                    "I tell them flattery buys nothing.",
                    typeof(FlatteryModusMentis), typeof(HallmarkModusMentis))));

    private static readonly NpcLineNode ForCheap = new(
        nodeId:          "cheap_pressed",
        replica:         "Cheap and quick, or cheap and good. Not both. Which?",
        replicaIndirect: "I tell them they may have cheap and quick or cheap and good, and ask which.",
        replicaHeard:    "They tell me I may have cheap and quick or cheap and good, and ask which.",

            new PlayerOption("cheap_quick", "say quick, I am on the road",
                "Quick. I am leaving soon.",
                "I tell them quick, because I am leaving soon.",
                End("cheap_quick_end", 2,
                    "Tomorrow, then. It will not last long, but neither will you, here.",
                    "I promise it for tomorrow, though it will not last.",
                    "I do not make rubbish, even for travellers. Try the market.",
                    "I tell them I do not make rubbish and to try the market.",
                    typeof(ImpatienceModusMentis), typeof(WayfaringModusMentis))),

            new PlayerOption("cheap_good", "say good, I will wait",
                "Good. I can wait.",
                "I tell them good, and that I can wait.",
                End("cheap_good_end", 2,
                    "A patient customer. Rare. It will be ready when it is ready, and it will be right.",
                    "I take the order and promise it right, whenever it is ready.",
                    "Patience does not pay my rent. It will cost more than you think.",
                    "I tell them patience does not pay the rent.",
                    typeof(PatienceModusMentis), typeof(ThriftModusMentis))));

    private static readonly NpcLineNode ForMend = new(
        nodeId:          "mend_pressed",
        replica:         "Let me see it. Hmm. Who did this to it?",
        replicaIndirect: "I look the thing over and ask who did this to it.",
        replicaHeard:    "They look the thing over and ask me who did this to it.",

            new PlayerOption("mend_road", "blame the road",
                "The road. Months of it.",
                "I tell them the road did it, months of it.",
                End("mend_road_end", 2,
                    "Honest wear. That I can mend. It will be better than new.",
                    "I agree it is honest wear and promise to mend it better than new.",
                    "The road did not do this. Someone took a knife to it. Take it elsewhere.",
                    "I tell them a knife did that, not the road, and send them elsewhere.",
                    typeof(WearReadingModusMentis), typeof(WayfaringModusMentis))),

            new PlayerOption("mend_self", "admit I tried to mend it myself",
                "I did. I tried to fix it myself.",
                "I admit I tried to fix it myself.",
                End("mend_self_end", 2,
                    "I can see. Well, you meant well. Leave it with me and watch how it is done.",
                    "I take it, and tell them to watch how it is done.",
                    "Then you have ruined it. There is nothing left to mend.",
                    "I tell them they have ruined it past mending.",
                    typeof(HumilityModusMentis), typeof(JourneymanEyeModusMentis))));

    private static readonly NpcLineNode Approach = new(
        nodeId:          "approach",
        replica:         "You want something made? Or mended?",
        replicaIndirect: "I ask them whether they want something made or mended.",
        replicaHeard:    "They ask me whether I want something made or mended.",

        new PlayerOption("for_fine", "ask for their very best work",
            "I want the best thing you can make.",
            "I ask them for the best thing they can make.",
            ForFine),

        new PlayerOption("for_cheap", "ask for something plain and cheap",
            "Something plain. Cheap. It only has to serve.",
            "I ask them for something plain and cheap that only has to serve.",
            ForCheap),

        new PlayerOption("for_mend", "hold out something worn for mending",
            "Can you mend this?",
            "I hold out something worn and ask whether they can mend it.",
            ForMend));

    public override NpcLineNode EntryNode => Approach;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
        => npc.Archetype is CobblerArchetype or TailorArchetype or PotterArchetype or ChandlerArchetype or TannerArchetype;
}

/// <summary>
/// "Ask for a Remedy" — asking an apothecary or a barber-surgeon for a cure.
///
/// <para>Describing an ailment to an apothecary or a barber and asking for something for it. Open to
/// strangers - nobody is refused a remedy for not having been introduced - and gentle in both
/// directions.</para>
///
/// <para>The <see cref="BranchDifficulty.Easy"/> ladder. Each of the six endings teaches its own pair
/// of lessons.</para>
/// </summary>
public class AskRemedyTree : DialogueTree
{
    public override string TreeId           => "ask_remedy";
    public override string DisplayName      => "Ask for a Remedy";
    public override string Description      => "asking for a remedy for an ailment";
    public override string NpcDescription   => "being asked for a remedy";
    public override string AssociatedVerbId => "ask_remedy";
    public override string? GrantedModusMentisId => "physic";

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

    private static readonly NpcLineNode ForPain = new(
        nodeId:          "pain_pressed",
        replica:         "Show me where. Here? And when you press it - so?",
        replicaIndirect: "I ask them to show me where it hurts, and press on it.",
        replicaHeard:    "They ask me to show them where it hurts, and press on it.",

            new PlayerOption("pain_endure", "grit my teeth and say nothing",
                "...",
                "I grit my teeth and say nothing while they press.",
                End("pain_endure_end", 2,
                    "Tough. Good. It is nothing broken. Rub this in at night and it will pass.",
                    "I tell them it is nothing broken and give them a salve.",
                    "If you will not tell me where it hurts, I cannot help you.",
                    "I tell them I cannot help if they will not say where it hurts.",
                    typeof(ClenchedGritModusMentis), typeof(BonesettingModusMentis))),

            new PlayerOption("pain_describe", "describe the pain exactly",
                "Sharp, here, when I lift. Dull the rest of the time.",
                "I describe the pain to them exactly.",
                End("pain_describe_end", 2,
                    "A good patient. That is the sinew, not the bone. Rest it, and take this.",
                    "I tell them it is the sinew and what to do for it.",
                    "That could be a dozen things. Come back when it is worse.",
                    "I tell them it could be anything and to come back when it is worse.",
                    typeof(DiagnosisModusMentis), typeof(AnatomyLoreModusMentis))));

    private static readonly NpcLineNode ForFever = new(
        nodeId:          "fever_pressed",
        replica:         "Hot and cold? Sweating at night? What have you been drinking?",
        replicaIndirect: "I ask them about the sweats, and what they have been drinking.",
        replicaHeard:    "They ask me about the sweats, and what I have been drinking.",

            new PlayerOption("fever_water", "say only well water",
                "Only water. From the wells here.",
                "I tell them only water, from the town wells.",
                End("fever_water_end", 2,
                    "Which well? The square. Of course. Boil it, and take this bitter powder morning and night.",
                    "I name the bad well and give them a powder.",
                    "Then I do not know what it is. Pray, and keep warm.",
                    "I tell them I do not know, and to pray and keep warm.",
                    typeof(TaintSenseModusMentis), typeof(PhysicModusMentis))),

            new PlayerOption("fever_ale", "admit to a great deal of ale",
                "Ale. A lot of ale.",
                "I admit to a great deal of ale.",
                End("fever_ale_end", 2,
                    "Ha. Then the cure is less ale. And this, to settle the bile.",
                    "I tell them to drink less and give them something for the bile.",
                    "I do not treat drunkards. Sleep it off.",
                    "I tell them I do not treat drunkards.",
                    typeof(ContinenceModusMentis), typeof(ApothecaryNoseModusMentis))));

    private static readonly NpcLineNode ForTooth = new(
        nodeId:          "tooth_pressed",
        replica:         "Open. Wider. Ah. That one is rotten through. It comes out, or it poisons you.",
        replicaIndirect: "I look in their mouth and tell them the tooth must come out.",
        replicaHeard:    "They look in my mouth and tell me the tooth must come out.",

            new PlayerOption("tooth_pull", "tell them to pull it now",
                "Then pull it. Now, before I lose my nerve.",
                "I tell them to pull it now, before I lose my nerve.",
                End("tooth_pull_end", 2,
                    "Brave. Hold the chair. One, two -- there. Spit.",
                    "I pull the tooth in one go.",
                    "You are shaking too much. Come back tomorrow, sober and brave.",
                    "I tell them they are shaking too much and to come back tomorrow.",
                    typeof(IronNervesModusMentis), typeof(ToothDrawingModusMentis))),

            new PlayerOption("tooth_salve", "ask for something to dull it instead",
                "Is there not something to dull it instead?",
                "I ask for something to dull the pain instead.",
                End("tooth_salve_end", 2,
                    "Clove oil, then. It will buy you a week. Then you will be back.",
                    "I give them clove oil and tell them they will be back.",
                    "Nothing dulls a tooth like that. It comes out or you suffer.",
                    "I tell them nothing will dull it.",
                    typeof(HerbloreModusMentis), typeof(SelfPreservationModusMentis))));

    private static readonly NpcLineNode Approach = new(
        nodeId:          "approach",
        replica:         "Ill, are you? Tell me what ails you.",
        replicaIndirect: "I ask them what ails them.",
        replicaHeard:    "They ask me what ails me.",

        new PlayerOption("for_pain", "complain of an ache that will not go",
            "It hurts. Here. It will not go away.",
            "I tell them of an ache that will not go away.",
            ForPain),

        new PlayerOption("for_fever", "complain of fever and sweats",
            "Fever. I sweat at night and shiver all day.",
            "I tell them of fever and night sweats.",
            ForFever),

        new PlayerOption("for_tooth", "point at a throbbing tooth",
            "This tooth. It is killing me.",
            "I point at a throbbing tooth.",
            ForTooth));

    public override NpcLineNode EntryNode => Approach;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
        => npc.Archetype is ApothecaryArchetype or BarberArchetype;
}

/// <summary>
/// "Give Alms" — giving something to a beggar, and hearing what they have to say for it.
///
/// <para>Offering alms to a beggar. Addressed to strangers by its nature, like begging itself, and
/// gentle in both directions: alms well given win their regard, alms clumsily given cost nothing.</para>
///
/// <para>The <see cref="BranchDifficulty.Easy"/> ladder. Each of the six endings teaches its own pair
/// of lessons.</para>
/// </summary>
public class GiveAlmsTree : DialogueTree
{
    public override string TreeId           => "give_alms";
    public override string DisplayName      => "Give Alms";
    public override string Description      => "giving alms to a beggar";
    public override string NpcDescription   => "being given alms";
    public override string AssociatedVerbId => "give_alms";
    public override string? GrantedModusMentisId => "almsgiving";

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

    private static readonly NpcLineNode ForPity = new(
        nodeId:          "pity_pressed",
        replica:         "Bless you. You have a kind face. Not many stop.",
        replicaIndirect: "I bless them and tell them few stop.",
        replicaHeard:    "They bless me and tell me few stop.",

            new PlayerOption("pity_story", "ask how they came to this",
                "How did you come to this?",
                "I ask them how they came to this.",
                End("pity_story_end", 2,
                    "A long story. A ship, a debt, a fever. Sit, if you have time, and I will tell it.",
                    "I tell them a little of how I came to this.",
                    "That is my business, friend. The coin is enough.",
                    "I tell them how I came to this is my business.",
                    typeof(EmpathyModusMentis), typeof(CondolenceModusMentis))),

            new PlayerOption("pity_pray", "ask them to pray for me in return",
                "Pray for me, then. I need it.",
                "I ask them to pray for me in return.",
                End("pity_pray_end", 2,
                    "Every night, for every one who gives. Your name will be in it.",
                    "I promise to pray for them every night.",
                    "The gods do not listen to beggars. Spend your coin on a priest.",
                    "I tell them the gods do not listen to beggars.",
                    typeof(PietyModusMentis), typeof(HumilityModusMentis))));

    private static readonly NpcLineNode ForNews = new(
        nodeId:          "news_pressed",
        replica:         "A coin for a poor soul - and what else do you want for it? I see it in your face.",
        replicaIndirect: "I take the coin and ask what else they want for it.",
        replicaHeard:    "They take the coin and ask what else I want for it.",

            new PlayerOption("news_watch", "ask who has been watching the gate",
                "Who has been coming and going at the gate?",
                "I ask them who has been coming and going at the gate.",
                End("news_watch_end", 2,
                    "Three riders at dawn, in a hurry, no colours. And the watch looked the other way.",
                    "I tell them about three riders at dawn and the watch looking away.",
                    "I see nothing. A beggar sees nothing. Good day.",
                    "I tell them a beggar sees nothing.",
                    typeof(StreetwiseModusMentis), typeof(VigilanceModusMentis))),

            new PlayerOption("news_house", "ask what goes on in the great houses",
                "What goes on in the merchants' houses?",
                "I ask them what goes on in the merchants' houses.",
                End("news_house_end", 2,
                    "The counting house sent away three clerks this week. Something has gone wrong in there.",
                    "I tell them the counting house has sent away its clerks.",
                    "For one coin? Come back with five.",
                    "I tell them to come back with more money.",
                    typeof(EavesdroppingModusMentis), typeof(IntrigueModusMentis))));

    private static readonly NpcLineNode ForBread = new(
        nodeId:          "bread_pressed",
        replica:         "Bread? Not coin? Well. Bread does not get stolen off me in the night.",
        replicaIndirect: "I take the bread and tell them bread at least is not stolen in the night.",
        replicaHeard:    "They take the bread and tell me bread at least is not stolen in the night.",

            new PlayerOption("bread_share", "sit and eat with them",
                "Here. I will eat with you.",
                "I sit down and eat with them.",
                End("bread_share_end", 2,
                    "Nobody has sat with me in a year. Thank you. Truly.",
                    "I thank them for sitting with me, the first in a year.",
                    "People are looking at you. Go on, before they think you are one of us.",
                    "I tell them to go before people think they are one of us.",
                    typeof(FellowFeelingModusMentis), typeof(TrenchermanModusMentis))),

            new PlayerOption("bread_advice", "tell them where they could find work",
                "The porters are hiring at the gate. You could carry.",
                "I tell them the porters are hiring at the gate.",
                End("bread_advice_end", 2,
                    "Maybe. Maybe I will. Thank you for thinking I could.",
                    "I thank them for thinking I could still work.",
                    "With this back? Look at me. Keep your advice.",
                    "I tell them to keep their advice.",
                    typeof(EnterpriseModusMentis), typeof(HardLaborModusMentis))));

    private static readonly NpcLineNode Approach = new(
        nodeId:          "approach",
        replica:         "Alms, for the love of the gods? Anything at all?",
        replicaIndirect: "I hold out the bowl and ask for anything at all.",
        replicaHeard:    "They hold out the bowl and ask for anything at all.",

        new PlayerOption("for_pity", "drop a coin in the bowl",
            "Here.",
            "I drop a coin in their bowl.",
            ForPity),

        new PlayerOption("for_news", "give a coin and look for something in return",
            "Here. And perhaps you could help me in turn.",
            "I give them a coin and look for something in return.",
            ForNews),

        new PlayerOption("for_bread", "give them bread instead of coin",
            "Not coin. Bread. Eat it.",
            "I give them bread instead of coin.",
            ForBread));

    public override NpcLineNode EntryNode => Approach;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId) => npc.Archetype is BeggarArchetype;
}

/// <summary>
/// "Hear the Gossip" — getting the town's talk out of someone who goes into every house.
///
/// <para>Asking a laundress, a water-carrier, a porter or a barber what is being said in town. An
/// ordinary matter between acquaintances, gentle in both directions.</para>
///
/// <para>The <see cref="BranchDifficulty.Easy"/> ladder. Each of the six endings teaches its own pair
/// of lessons.</para>
/// </summary>
public class HearGossipTree : DialogueTree
{
    public override string TreeId           => "hear_gossip";
    public override string DisplayName      => "Hear the Gossip";
    public override string Description      => "getting the town's talk out of someone";
    public override string NpcDescription   => "being asked what is being said in town";
    public override string AssociatedVerbId => "hear_gossip";
    public override string? GrantedModusMentisId => "gossip";

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

    private static readonly NpcLineNode ForScandal = new(
        nodeId:          "scandal_pressed",
        replica:         "Oh, there is always something. But you will not repeat it? Swear.",
        replicaIndirect: "I ask them to swear they will not repeat it.",
        replicaHeard:    "They ask me to swear I will not repeat it.",

            new PlayerOption("scandal_swear", "swear to keep it to myself",
                "I swear. Not a word.",
                "I swear not to repeat a word.",
                End("scandal_swear_end", 2,
                    "Well then. The guard captain's wife, and the tailor. Twice a week. Everyone knows but him.",
                    "I tell them about the captain's wife and the tailor.",
                    "You have a face that repeats things. I have said nothing.",
                    "I tell them they have a face that repeats things.",
                    typeof(OathmakingModusMentis), typeof(GossipModusMentis))),

            new PlayerOption("scandal_trade", "offer a piece of gossip in exchange",
                "I will trade you. I heard something on the road too.",
                "I offer them some gossip of my own in exchange.",
                End("scandal_trade_end", 2,
                    "Now that is fair. You first. ...Is that so! Well, here is mine.",
                    "We trade gossip, theirs first.",
                    "Road gossip. Everyone has road gossip. Mine is worth more.",
                    "I tell them road gossip is worth less than mine.",
                    typeof(BanterModusMentis), typeof(BrokerageModusMentis))));

    private static readonly NpcLineNode ForDanger = new(
        nodeId:          "danger_pressed",
        replica:         "Trouble? Why - are you trouble?",
        replicaIndirect: "I ask them whether they are trouble themselves.",
        replicaHeard:    "They ask me whether I am trouble myself.",

            new PlayerOption("danger_honest", "say I only want to keep out of it",
                "No. I only want to keep out of it.",
                "I tell them I only want to keep out of trouble.",
                End("danger_honest_end", 2,
                    "Then stay off the alleys after the bell, and do not drink at the alehouse by the gate.",
                    "I tell them which places to keep away from.",
                    "Everyone says that. Keep out of my way and you will keep out of trouble.",
                    "I tell them to keep out of my way.",
                    typeof(CautionModusMentis), typeof(StreetwiseModusMentis))),

            new PlayerOption("danger_coin", "slip them a coin",
                "Perhaps this helps you remember.",
                "I slip them a coin.",
                End("danger_coin_end", 2,
                    "It does. The watch is paid off on the east side. Do what you like there; nobody will come.",
                    "I tell them the east side's watch is paid off.",
                    "I am not a spy for hire. Keep your money.",
                    "I refuse the coin.",
                    typeof(BriberyModusMentis), typeof(IntrigueModusMentis))));

    private static readonly NpcLineNode ForWork = new(
        nodeId:          "work_pressed",
        replica:         "Work? For a stranger? It depends what you can do.",
        replicaIndirect: "I ask them what they can do.",
        replicaHeard:    "They ask me what I can do.",

            new PlayerOption("work_back", "say I have a strong back",
                "I can carry. I can lift.",
                "I tell them I have a strong back.",
                End("work_back_end", 2,
                    "Then the merchants on the square want porters. Tell them I sent you.",
                    "I send them to the merchants who want porters.",
                    "So can every lout in town. That is not work, that is waiting.",
                    "I tell them every lout can carry.",
                    typeof(HaulageModusMentis), typeof(EnterpriseModusMentis))),

            new PlayerOption("work_hands", "say I am good with my hands",
                "I am good with my hands. I can learn a trade.",
                "I tell them I am good with my hands.",
                End("work_hands_end", 2,
                    "The potter lost an apprentice to the fever last month. Go and see.",
                    "I tell them the potter needs an apprentice.",
                    "The guilds take nobody without a name. You have no name here.",
                    "I tell them the guilds take nobody without a name.",
                    typeof(DiligenceModusMentis), typeof(JourneymanEyeModusMentis))));

    private static readonly NpcLineNode Approach = new(
        nodeId:          "approach",
        replica:         "You want to know what is being said? I hear everything, going house to house.",
        replicaIndirect: "I tell them I hear everything going house to house.",
        replicaHeard:    "They tell me they hear everything going house to house.",

        new PlayerOption("for_scandal", "ask who is doing what with whom",
            "Who is doing what with whom?",
            "I ask them who is doing what with whom.",
            ForScandal),

        new PlayerOption("for_danger", "ask where the trouble in town is",
            "Where is the trouble in this town?",
            "I ask them where the trouble in this town is.",
            ForDanger),

        new PlayerOption("for_work", "ask who is hiring",
            "Who is hiring?",
            "I ask them who is hiring.",
            ForWork));

    public override NpcLineNode EntryNode => Approach;

    public override bool IsAvailable(NpcEntity npc, string partyMemberId)
        => npc.Archetype is LaundressArchetype or WaterCarrierArchetype or PorterArchetype or BarberArchetype;
}
