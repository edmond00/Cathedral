namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// Small-talk subjects about the household: food and drink, kin, rest, and health. Health runs
/// deepest of all twelve subjects — it is where someone stops performing and admits something —
/// and so is the only branch here that reaches four replies.
/// See <see cref="StrengthenRelationshipTree"/> for the shape rules.
/// </summary>
public partial class StrengthenRelationshipTree
{
    // ══════════════════════════════════════════════════════════════════════════
    //  Food — rich
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode FoodTopic() => new(
        nodeId:  "food_topic",
        replica: "My table? Nothing to boast of. {npc:opinion_food}",

        new PlayerOption("food_agree_plain", "say plain food eaten hot beats anything",
            "Plain and hot beats fine and cold. Every time.",
            End("food_plain_end", 2,
                "Now that's a person I can talk to. Sit down sometime and I'll prove it to you.",
                "Mm. Easy to say when you're not the one cooking it.")),

        new PlayerOption("food_ask_best", "ask what the best thing they ever ate was",
            "What's the best thing you ever put in your mouth?",
            FoodBest()),

        new PlayerOption("food_ask_lean", "ask what they eat when times are thin",
            "And when it's thin? What do you fall back on?",
            FoodLean()));

    private static NpcLineNode FoodBest() => new(
        nodeId:  "food_best",
        replica: "There was a feast-day when I was small — meat, actual meat, and as much as I wanted. I've eaten better since and never once eaten anything like it.",

        new PlayerOption("food_ask_why_best", "say it sounds like it was about more than the meat",
            "I don't think that was about the meat.",
            End("food_why_best_end", 3,
                "...No. No, it wasn't. You see things, don't you. I like that in a person.",
                "It was about the meat. Don't go making it into something.")),

        new PlayerOption("food_share_own", "tell them the best thing you ever ate",
            "Mine was bread from a fire I'd built myself, when I'd not eaten in two days.",
            End("food_share_end", 3,
                "Hunger's the best cook there is, and the worst way to meet a good loaf. Aye — I know that one.",
                "Everyone's got a hunger story. I've heard a hundred.")));

    private static NpcLineNode FoodLean() => new(
        nodeId:  "food_lean",
        replica: "Pottage. Whatever's in the pot, stretched with whatever else there is. You get clever about it or you get hungry.",

        new PlayerOption("food_ask_trick", "ask for the trick of stretching a pot",
            "What's the trick of it? I could stand to learn one.",
            End("food_trick_end", 3,
                "Nettle tops in spring, and never let the pot go empty — you build on it. There. That's worth more than it sounds.",
                "The trick is being hungry enough not to care. You'll learn it free.")),

        new PlayerOption("food_offer_share", "offer to bring something to the pot next time",
            "Next time I've something worth adding, I'll bring it to your pot.",
            End("food_offer_end", 3,
                "Then you'll be welcome at it. That's how it's meant to work, and it mostly doesn't.",
                "I don't need charity, and I'd not know what to do with it if I did.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Kin — rich
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode KinTopic() => new(
        nodeId:  "kin_topic",
        replica: "Well enough, thank you for asking. {npc:opinion_kin}",

        new PlayerOption("kin_glad", "say you are glad to hear they are well",
            "I'm glad to hear it. Truly.",
            End("kin_glad_end", 2,
                "That's kindly meant, and I'll take it as such. Fare well, {you:name}.",
                "Aye. Well. That's that asked and answered.")),

        new PlayerOption("kin_ask_young", "ask after the young ones",
            "And the young ones? Growing too fast, I'd wager.",
            KinYoung()),

        new PlayerOption("kin_ask_old", "ask after the older folk of the house",
            "And the older heads in the house — how do they fare?",
            KinOld()));

    private static NpcLineNode KinYoung() => new(
        nodeId:  "kin_young",
        replica: "Faster than the shoes can keep up with. One of them's already stronger than I was at that age, and won't be told anything.",

        new PlayerOption("kin_say_proud", "say they sound proud of them",
            "You sound prouder than you're letting on.",
            End("kin_proud_end", 3,
                "...Caught. Aye. I'd not say it to their face, but I am. Good of you to notice.",
                "Don't put words in me. I said what I said.")),

        new PlayerOption("kin_ask_future", "ask what they hope for the young ones",
            "What do you hope comes to them?",
            End("kin_future_end", 3,
                "Something easier than this. That's the whole of it. Every parent here would say the same and none of them say it out loud.",
                "Hope's a luxury. They'll get what there is, same as I did.")));

    private static NpcLineNode KinOld() => new(
        nodeId:  "kin_old",
        replica: "Slower every year, and sharper with it. They've opinions about how I do everything, and they were right about most of it, which is the worst part.",

        new PlayerOption("kin_laugh", "laugh and say that never changes",
            "That never stops, does it. Not for anyone.",
            End("kin_laugh_end", 3,
                "Ha! No. And I'll be the same, and worse. Good to laugh at it with someone.",
                "It's not funny from where I stand.")),

        new PlayerOption("kin_ask_learned", "ask what they learned from them",
            "What did you get from them that stuck?",
            End("kin_learned_end", 3,
                "How to keep going when there's no reason to. It's not a warm thing to be given, but I've used it every year since.",
                "Bruises and bad habits. Let's leave it there.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Rest — short
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode RestTopic() => new(
        nodeId:  "rest_topic",
        replica: "When the day's done? {npc:opinion_rest}",

        new PlayerOption("rest_agree", "say that sounds like the right way to spend an evening",
            "That sounds like an evening well spent to me.",
            End("rest_agree_end", 2,
                "It is. Folk make it complicated and it isn't. Good to hear someone else say so.",
                "It's an evening. It doesn't need admiring.")),

        new PlayerOption("rest_ask_feast", "ask about the feast days here",
            "And the feast days? How are they kept here?",
            RestFeast()),

        new PlayerOption("rest_ask_sleep", "ask whether they sleep well",
            "Do you sleep well, after a day like yours?",
            End("rest_sleep_end", 2,
                "Like a felled tree. It's the one thing this life gives you for nothing.",
                "That's a strange thing to ask a person.")));

    private static NpcLineNode RestFeast() => new(
        nodeId:  "rest_feast",
        replica: "Properly. Nobody works, nobody's asked to, and anyone who tries gets talked about all year. There's not much here that's ours — that is.",

        new PlayerOption("rest_ask_join", "ask whether you would be welcome at the next one",
            "Would there be room at the next one for me?",
            End("rest_join_end", 3,
                "There'd be room. Come, and bring nothing but yourself — that's the rule and I'll not have it broken.",
                "It's for the folk of this place. You'd stand out.")),

        new PlayerOption("rest_ask_songs", "ask what gets sung at them",
            "And what gets sung, when it comes round to that?",
            End("rest_songs_end", 3,
                "{npc:opinion_stories} You'd learn the chorus by the third round. Everyone does.",
                "Songs are for the night itself, not for explaining to strangers in daylight.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Health — deepest branch in the tree
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode HealthTopic() => new(
        nodeId:  "health_topic",
        replica: "Well enough, thank you for asking. {npc:opinion_health} And yourself?",

        new PlayerOption("health_answer_warmly", "answer warmly and share a little",
            "Can't complain — and all the better for seeing a friendly face.",
            End("health_warm_end", 2,
                "Kind of you to ask after me. It means more than you know. Fare well.",
                "I've no time for idle chatter today.")),

        new PlayerOption("health_ask_remedy", "ask what they do about it",
            "And do you do anything for it, or just carry it?",
            HealthRemedy()),

        new PlayerOption("health_press_gently", "say gently that it sounds like more than they are letting on",
            "That sounds like more than you're saying.",
            HealthPress()));

    private static NpcLineNode HealthRemedy() => new(
        nodeId:  "health_remedy",
        replica: "Carry it, mostly. There's a woman up the way who does willow-bark and knows what she's about, but she costs, and I'd sooner ache.",

        new PlayerOption("health_urge_go", "urge them to go anyway",
            "Go anyway. An ache you've carried a year isn't going to leave on its own.",
            End("health_urge_end", 3,
                "...You're not the first to say it. You might be the first I listen to. We'll see.",
                "I'll decide what I do with my own body, thank you.")),

        new PlayerOption("health_ask_worse", "ask whether it is getting worse",
            "Is it worse than it was last year?",
            End("health_worse_end", 3,
                "Aye. It is. I've not said that to anyone. Don't make a thing of it.",
                "That's not a question you get to ask.")));

    private static NpcLineNode HealthPress() => new(
        nodeId:  "health_press",
        replica: "...Hm. You're sharper than you look. It's nothing that stops me working. It's just there, every morning, before anything else is.",

        new PlayerOption("health_say_heard", "say simply that you heard them",
            "I hear you. That's all — I'm not going to make it a fuss.",
            End("health_heard_end", 3,
                "Then you've done more than most. That's enough. That's plenty.",
                "Good. Then we'll say no more about it, and you can be on your way.")),

        new PlayerOption("health_ask_fear", "ask what they are afraid it means",
            "And what are you afraid it's the start of?",
            HealthFear()));

    private static NpcLineNode HealthFear() => new(
        nodeId:  "health_fear",
        replica: "...The same thing that took my own people, likely. It starts small. It always starts small, and then one spring you can't do the work, and then you're a burden in your own house.",

        new PlayerOption("health_promise", "promise they would not be left to it alone",
            "If that day comes, you'll not face it with nobody. You have my word.",
            End("health_promise_end", 4,
                "...I'll hold you to that, {you:name}. God help me, I'll hold you to it.",
                "Words. Everyone's brave about someone else's bad year.")),

        new PlayerOption("health_say_not_yet", "point out that day is not today",
            "Maybe. But it isn't today, and you're still standing here talking to me.",
            End("health_not_yet_end", 4,
                "...No. It isn't today. I needed someone to say that out loud. Thank you.",
                "Don't. Don't do the cheerful thing at me. I've had it from everyone.")));
}
