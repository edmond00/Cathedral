using Cathedral.Game.Narrative.ModiMentis;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// Small-talk subjects about the household: food and drink, kin, rest, and health. Health runs
/// deepest of all twelve subjects — it is where someone stops performing and admits something —
/// and so is the only branch here that reaches four replies.
/// See <see cref="StrengthenRelationshipTree"/> for the shape rules, and "Authoring the neutral
/// text" on <see cref="DialogueTree"/> for what a replica may and may not carry.
/// </summary>
public partial class StrengthenRelationshipTree
{
    // ══════════════════════════════════════════════════════════════════════════
    //  Food — rich
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode FoodTopic() => new(
        nodeId:          "food_topic",
        replica:         "Nothing worth boasting of. {npc:opinion_food}",
        replicaIndirect: "I tell {you:name} there is nothing worth boasting of, and that of the food I think {npc:opinion_food}.",
        replicaHeard:    "{npc:name} says there is nothing worth boasting of, and tells me what they think of the food.",

        new PlayerOption("food_agree_plain", "say plain food eaten hot beats anything",
            "Plain food eaten hot is better than fine food eaten cold.",
            "I tell {npc:name} plain food eaten hot beats fine food eaten cold.",
            End("food_plain_end", 2,
                "That is someone I can talk to. Sit down sometime and I will prove it.",
                "I tell {you:name} that is someone I can talk to, and to sit down sometime.",
                "Easy to say when you are not the one cooking it.",
                "I tell {you:name} it is easy to say when they are not the one cooking.",
                typeof(AbstinenceModusMentis))),

        new PlayerOption("food_ask_best", "ask what the best thing they ever ate was",
            "What is the best thing you have ever eaten?",
            "I ask {npc:name} the best thing they have ever eaten.",
            FoodBest()),

        new PlayerOption("food_ask_lean", "ask what they eat when times are thin",
            "And when food is short? What do you fall back on?",
            "I ask {npc:name} what they fall back on when food is short.",
            FoodLean()));

    private static NpcLineNode FoodBest() => new(
        nodeId:          "food_best",
        replica:         "A feast day when I was small. Meat, and as much as I wanted. I have eaten better since and never anything like it.",
        replicaIndirect: "I tell {you:name} about a feast day when I was small, with as much meat as I wanted, and that I have never eaten its like since.",
        replicaHeard:    "{npc:name} tells me about a feast day when they were small, with as much meat as they wanted, and says they have never eaten its like since.",

        new PlayerOption("food_ask_why_best", "say it sounds like it was about more than the meat",
            "I do not think that was about the meat.",
            "I tell {npc:name} I do not think that was about the meat.",
            End("food_why_best_end", 3,
                "No, it was not. You notice things. I like that in a person.",
                "I admit it was not, and tell {you:name} they notice things.",
                "It was about the meat. Do not make it into something else.",
                "I tell {you:name} it was about the meat and not to make it into something else.",
                typeof(RecollectionModusMentis))),

        new PlayerOption("food_share_own", "tell them the best thing you ever ate",
            "Mine was bread from a fire I built myself, after two days without eating.",
            "I tell {npc:name} mine was bread from my own fire after two days without eating.",
            End("food_share_end", 3,
                "Hunger makes anything taste better, and it is a poor way to meet good bread. I know that one.",
                "I tell {you:name} hunger makes anything taste better, and that I know that one.",
                "Everyone has a hunger story. I have heard a hundred.",
                "I tell {you:name} everyone has a hunger story and I have heard a hundred.",
                typeof(HospitalityModusMentis))));

    private static NpcLineNode FoodLean() => new(
        nodeId:          "food_lean",
        replica:         "Pottage. Whatever is in the pot, stretched with whatever else there is. You get clever about it or you go hungry.",
        replicaIndirect: "I tell {you:name} it is pottage stretched with whatever else there is, and you get clever about it or go hungry.",
        replicaHeard:    "{npc:name} says it is pottage stretched with whatever else there is, and you get clever about it or go hungry.",

        new PlayerOption("food_ask_trick", "ask for the trick of stretching a pot",
            "How is it done? I could stand to learn.",
            "I ask {npc:name} how it is done, since I could stand to learn.",
            End("food_trick_end", 3,
                "Nettle tops in spring, and never let the pot go empty. That is worth more than it sounds.",
                "I tell {you:name} it is nettle tops in spring and never letting the pot go empty.",
                "The trick is being hungry enough not to care. You will learn it free.",
                "I tell {you:name} the trick is being hungry enough not to care.",
                typeof(ThriftModusMentis))),

        new PlayerOption("food_offer_share", "offer to bring something to the pot next time",
            "Next time I have something worth adding, I will bring it to your pot.",
            "I offer {npc:name} to bring something to their pot next time.",
            End("food_offer_end", 3,
                "Then you will be welcome at it. That is how it should work, and mostly does not.",
                "I tell {you:name} they will be welcome at it, and that this is how it should work.",
                "I do not need charity, and I would not know what to do with it.",
                "I tell {you:name} I do not need charity.",
                typeof(TrenchermanModusMentis))));

    // ══════════════════════════════════════════════════════════════════════════
    //  Kin — rich
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode KinTopic() => new(
        nodeId:          "kin_topic",
        replica:         "Well enough, thank you for asking. {npc:opinion_kin}",
        replicaIndirect: "I tell {you:name} they are well enough, and that of my household {npc:opinion_kin}.",
        replicaHeard:    "{npc:name} says they are well enough, and tells me how their household stands.",

        new PlayerOption("kin_glad", "say you are glad to hear they are well",
            "I am glad to hear it.",
            "I tell {npc:name} I am glad to hear it.",
            End("kin_glad_end", 2,
                "That is kindly meant and I take it as such. Farewell, {you:name}.",
                "I tell {you:name} that is kindly meant, and bid them farewell.",
                "That is asked and answered, then.",
                "I tell {you:name} that is asked and answered.",
                typeof(LoyaltyModusMentis))),

        new PlayerOption("kin_ask_young", "ask after the young ones",
            "And the children? Growing quickly, I expect.",
            "I ask {npc:name} after the children.",
            KinYoung()),

        new PlayerOption("kin_ask_old", "ask after the older folk of the house",
            "And the older ones in the house? How are they?",
            "I ask {npc:name} how the older ones in the house are.",
            KinOld()));

    private static NpcLineNode KinYoung() => new(
        nodeId:          "kin_young",
        replica:         "Faster than their shoes last. One of them is already stronger than I was at that age, and will not be told anything.",
        replicaIndirect: "I tell {you:name} they grow faster than their shoes last, and that one is already stronger than I was.",
        replicaHeard:    "{npc:name} says the children grow faster than their shoes last, and that one is already stronger than they were.",

        new PlayerOption("kin_say_proud", "say they sound proud of them",
            "You sound prouder than you are letting on.",
            "I tell {npc:name} they sound prouder than they are letting on.",
            End("kin_proud_end", 3,
                "I am. I would not say it to their face. Good of you to notice.",
                "I admit that I am, and thank {you:name} for noticing.",
                "Do not put words in my mouth. I said what I said.",
                "I tell {you:name} not to put words in my mouth.",
                typeof(LineageLoreModusMentis))),

        new PlayerOption("kin_ask_future", "ask what they hope for the young ones",
            "What do you hope for them?",
            "I ask {npc:name} what they hope for them.",
            End("kin_future_end", 3,
                "Something easier than this. Every parent here would say the same and none of them say it aloud.",
                "I tell {you:name} I hope for something easier than this, and that no parent here says it aloud.",
                "Hope is a luxury. They will get what there is, as I did.",
                "I tell {you:name} hope is a luxury, and they will get what there is.",
                typeof(ForesightModusMentis))));

    private static NpcLineNode KinOld() => new(
        nodeId:          "kin_old",
        replica:         "Slower every year, and sharper with it. They have opinions about how I do everything, and they were right about most of it.",
        replicaIndirect: "I tell {you:name} they grow slower every year and sharper with it, and were right about most of it.",
        replicaHeard:    "{npc:name} says the older ones grow slower every year and sharper with it, and were right about most of it.",

        new PlayerOption("kin_laugh", "laugh and say that never changes",
            "That never stops, for anyone.",
            "I tell {npc:name} that never stops, for anyone.",
            End("kin_laugh_end", 3,
                "No. And I will be the same, and worse. It is good to laugh at it with someone.",
                "I tell {you:name} I will be the same and worse, and that it is good to laugh at it with someone.",
                "It is not funny from where I stand.",
                "I tell {you:name} it is not funny from where I stand.",
                typeof(BanterModusMentis))),

        new PlayerOption("kin_ask_learned", "ask what they learned from them",
            "What did you learn from them that stayed with you?",
            "I ask {npc:name} what they learned from them that stayed.",
            End("kin_learned_end", 3,
                "How to keep going when there is no reason to. I have used it every year since.",
                "I tell {you:name} I learned to keep going when there is no reason to.",
                "Bruises and bad habits. Let us leave it there.",
                "I tell {you:name} it was bruises and bad habits.",
                typeof(RoteModusMentis))));

    // ══════════════════════════════════════════════════════════════════════════
    //  Rest — short
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode RestTopic() => new(
        nodeId:          "rest_topic",
        replica:         "When the day is done? {npc:opinion_rest}",
        replicaIndirect: "I tell {you:name} that when the day is done {npc:opinion_rest}.",
        replicaHeard:    "{npc:name} tells me what they do when the day is done.",

        new PlayerOption("rest_agree", "say that sounds like the right way to spend an evening",
            "That sounds like an evening well spent.",
            "I tell {npc:name} that sounds like an evening well spent.",
            End("rest_agree_end", 2,
                "It is. People make it complicated and it is not. Good to hear someone say so.",
                "I tell {you:name} people make it complicated and it is not.",
                "It is an evening. It does not need admiring.",
                "I tell {you:name} it is an evening and does not need admiring.",
                typeof(PatienceModusMentis))),

        new PlayerOption("rest_ask_feast", "ask about the feast days here",
            "And the feast days? How are they kept here?",
            "I ask {npc:name} how the feast days are kept here.",
            RestFeast()),

        new PlayerOption("rest_ask_sleep", "ask whether they sleep well",
            "Do you sleep well after a day like yours?",
            "I ask {npc:name} whether they sleep well after a day like theirs.",
            End("rest_sleep_end", 2,
                "Deeply. It is the one thing this life gives for nothing.",
                "I tell {you:name} I sleep deeply, and that it is the one thing this life gives for nothing.",
                "That is a strange thing to ask.",
                "I tell {you:name} that is a strange thing to ask.",
                typeof(DreamloreModusMentis))));

    private static NpcLineNode RestFeast() => new(
        nodeId:          "rest_feast",
        replica:         "Properly. Nobody works, and anyone who tries is talked about all year. There is little here that is ours. That is.",
        replicaIndirect: "I tell {you:name} nobody works on them, that anyone who tries is talked about all year, and that little else here is ours.",
        replicaHeard:    "{npc:name} says nobody works on the feast days, that anyone who tries is talked about all year, and that little else there is theirs.",

        new PlayerOption("rest_ask_join", "ask whether you would be welcome at the next one",
            "Would there be room for me at the next one?",
            "I ask {npc:name} whether there would be room for me at the next one.",
            End("rest_join_end", 3,
                "There would. Come, and bring nothing but yourself. That is the rule.",
                "I tell {you:name} there would be, and to bring nothing but themselves.",
                "It is for the people of this place. You would stand out.",
                "I tell {you:name} it is for the people of this place and they would stand out.",
                typeof(GregariousnessModusMentis))),

        new PlayerOption("rest_ask_songs", "ask what gets sung at them",
            "What is sung at them?",
            "I ask {npc:name} what is sung at them.",
            End("rest_songs_end", 3,
                "{npc:opinion_stories} You would know the chorus by the third round. Everyone does.",
                "I tell {you:name} that {npc:opinion_stories}, and that they would know the chorus by the third round.",
                "Songs are for the night itself, not for explaining to strangers in daylight.",
                "I tell {you:name} songs are for the night itself, not for explaining in daylight.",
                typeof(SolfegeModusMentis), typeof(PoetryModusMentis))));

    // ══════════════════════════════════════════════════════════════════════════
    //  Health — deepest branch in the tree
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode HealthTopic() => new(
        nodeId:          "health_topic",
        replica:         "Well enough, thank you for asking. {npc:opinion_health} And you?",
        replicaIndirect: "I tell {you:name} that of my health {npc:opinion_health}, and ask after theirs.",
        replicaHeard:    "{npc:name} tells me how they have been keeping, and asks after me.",

        new PlayerOption("health_answer_warmly", "answer warmly and share a little",
            "I have nothing to complain of, and it is good to see a friendly face.",
            "I tell {npc:name} I have nothing to complain of, and that it is good to see a friendly face.",
            End("health_warm_end", 2,
                "It was kind of you to ask after me. It means more than you know. Farewell.",
                "I tell {you:name} it was kind of them to ask, and bid them farewell.",
                "I have no time for idle talk today.",
                "I tell {you:name} I have no time for idle talk today.",
                typeof(EmpathyModusMentis))),

        new PlayerOption("health_ask_remedy", "ask what they do about it",
            "Do you do anything for it, or do you just carry it?",
            "I ask {npc:name} whether they do anything for it or simply carry it.",
            HealthRemedy()),

        new PlayerOption("health_press_gently", "say gently that it sounds like more than they are letting on",
            "That sounds like more than you are saying.",
            "I tell {npc:name} it sounds like more than they are saying.",
            HealthPress()));

    private static NpcLineNode HealthRemedy() => new(
        nodeId:          "health_remedy",
        replica:         "I carry it, mostly. There is a woman up the way who does willow-bark and knows her work, but she charges, and I would sooner ache.",
        replicaIndirect: "I tell {you:name} I carry it, and that the woman up the way who does willow-bark charges more than I will pay.",
        replicaHeard:    "{npc:name} says they carry it, and that the woman up the way who does willow-bark charges more than they will pay.",

        new PlayerOption("health_urge_go", "urge them to go anyway",
            "Go anyway. An ache you have carried a year will not leave on its own.",
            "I urge {npc:name} to go anyway, since an ache carried a year will not leave on its own.",
            End("health_urge_end", 3,
                "You are not the first to say it. You may be the first I listen to.",
                "I tell {you:name} they may be the first I listen to.",
                "I will decide what I do with my own body, thank you.",
                "I tell {you:name} I will decide what I do with my own body.",
                typeof(MercyModusMentis))),

        new PlayerOption("health_ask_worse", "ask whether it is getting worse",
            "Is it worse than it was last year?",
            "I ask {npc:name} whether it is worse than last year.",
            End("health_worse_end", 3,
                "It is. I have not said that to anyone. Do not make anything of it.",
                "I admit to {you:name} that it is, and ask them not to make anything of it.",
                "That is not a question you get to ask.",
                "I tell {you:name} that is not a question they get to ask.",
                typeof(CondolenceModusMentis))));

    private static NpcLineNode HealthPress() => new(
        nodeId:          "health_press",
        replica:         "You are more observant than you look. It is nothing that stops me working. It is simply there, every morning, before anything else.",
        replicaIndirect: "I tell {you:name} it does not stop me working, but is there every morning before anything else.",
        replicaHeard:    "{npc:name} says it does not stop them working, but is there every morning before anything else.",

        new PlayerOption("health_say_heard", "say simply that you heard them",
            "I hear you. That is all. I am not going to make a fuss of it.",
            "I tell {npc:name} I hear them and will not make a fuss of it.",
            End("health_heard_end", 3,
                "Then you have done more than most. That is enough.",
                "I tell {you:name} they have done more than most, and that it is enough.",
                "Good. Then we will say no more about it, and you can be on your way.",
                "I tell {you:name} we will say no more about it.",
                typeof(HearkeningModusMentis))),

        new PlayerOption("health_ask_fear", "ask what they are afraid it means",
            "What are you afraid it is the start of?",
            "I ask {npc:name} what they are afraid it is the start of.",
            HealthFear()));

    private static NpcLineNode HealthFear() => new(
        nodeId:          "health_fear",
        replica:         "The same thing that took my own family, most likely. It starts small, and then one spring you cannot do the work, and then you are a burden in your own house.",
        replicaIndirect: "I tell {you:name} it is likely what took my own family, and that one spring I will be a burden in my own house.",
        replicaHeard:    "{npc:name} says it is likely what took their own family, and that one spring they will be a burden in their own house.",

        new PlayerOption("health_promise", "promise they would not be left to it alone",
            "If that day comes, you will not face it alone. You have my word.",
            "I give {npc:name} my word they will not face that day alone.",
            End("health_promise_end", 4,
                "I will hold you to that, {you:name}.",
                "I tell {you:name} I will hold them to that.",
                "Words. Everyone is brave about someone else's bad year.",
                "I tell {you:name} everyone is brave about someone else's bad year.",
                typeof(OathmakingModusMentis))),

        new PlayerOption("health_say_not_yet", "point out that day is not today",
            "Perhaps. But it is not today, and you are still standing here talking to me.",
            "I tell {npc:name} it is not today, and they are still standing here talking to me.",
            End("health_not_yet_end", 4,
                "No, it is not today. I needed someone to say that aloud. Thank you.",
                "I agree it is not today, and thank {you:name} for saying it aloud.",
                "Do not do the cheerful thing at me. I have had it from everyone.",
                "I tell {you:name} not to do the cheerful thing at me.",
                typeof(StonefaceModusMentis))));
}
