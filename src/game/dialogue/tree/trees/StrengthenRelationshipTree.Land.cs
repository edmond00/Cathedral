namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// Small-talk subjects about the land and the year: the weather, the turning seasons, the harvest,
/// and the water. Weather and harvest run rich (four ways to follow them); seasons and water run
/// short. See <see cref="StrengthenRelationshipTree"/> for the shape rules.
/// </summary>
public partial class StrengthenRelationshipTree
{
    // ══════════════════════════════════════════════════════════════════════════
    //  Weather — rich
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode WeatherTopic() => new(
        nodeId:  "weather_topic",
        replica: "The weather, aye. {npc:opinion_weather}",

        new PlayerOption("weather_agree", "agree and let it rest there",
            "That's the truth of it. There's no arguing with a sky.",
            End("weather_agree_end", 2,
                "No, there isn't. It's good to talk to someone who knows that much.",
                "Aye, aye. Was that all you wanted?")),

        new PlayerOption("weather_ask_worst", "ask about the worst weather they remember",
            "What's the worst you've ever seen come over?",
            WeatherWorst()),

        new PlayerOption("weather_ask_reading", "ask how they tell what is coming",
            "How do you tell what's coming, before it comes?",
            WeatherReading()));

    private static NpcLineNode WeatherWorst() => new(
        nodeId:  "weather_worst",
        replica: "There was a winter — years back now — that took the roof off two houses and a good part of my temper with it. We were three weeks digging out.",

        new PlayerOption("weather_ask_after", "ask how they came through it",
            "And how did you come through it?",
            End("weather_after_end", 3,
                "Same as everyone: badly, and together. That's how anyone comes through anything here.",
                "We came through it. That's all there is to say, and I've said it.")),

        new PlayerOption("weather_share_own", "offer a hard season of your own",
            "I've had a season or two like it. You learn what you can do without.",
            End("weather_share_end", 3,
                "That you do. It's a comfort, hearing it from someone who's been under the same sky.",
                "Everybody's had a hard winter. It's not a thing to trade like coins.")));

    private static NpcLineNode WeatherReading() => new(
        nodeId:  "weather_reading",
        replica: "You watch. The way the smoke leans, the way the beasts stand, whether the light goes green before a storm. Nobody teaches it — you just stop being surprised.",

        new PlayerOption("weather_ask_teach", "ask them to teach you one sign",
            "Teach me one. Just one I can use.",
            End("weather_teach_end", 3,
                "Alright — if the birds go quiet all at once, get under something. That's the one I'd give away. Use it well.",
                "It's not a thing you hand over. Watch the sky for twenty years like I did.")),

        new PlayerOption("weather_mention_omens", "ask whether they hold with signs and omens",
            "And do you hold with the old signs? The ones folk swear by?",
            End("weather_omens_end", 3,
                "{npc:opinion_omens} There. Now you know something about me most don't.",
                "That's a question for someone with time to waste on it. I've none today.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Seasons — short
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode SeasonsTopic() => new(
        nodeId:  "seasons_topic",
        replica: "It does, doesn't it. {npc:opinion_seasons}",

        new PlayerOption("seasons_favourite", "ask which part of the year they like best",
            "Which part of it do you like best?",
            End("seasons_favourite_end", 2,
                "The turn into the long light. Everything's ahead of you then and nothing's gone wrong yet.",
                "I like the part where people let me get on with it.")),

        new PlayerOption("seasons_hardest", "ask which part they dread",
            "And which part do you dread coming round?",
            SeasonsHardest()),

        new PlayerOption("seasons_agree", "say the year moves faster every time round",
            "It comes round quicker every year. Or I do.",
            End("seasons_quicker_end", 2,
                "Ha — that's exactly it, and I've never heard it said so plainly. You're good company.",
                "Speak for yourself. My years are quite long enough.")));

    private static NpcLineNode SeasonsHardest() => new(
        nodeId:  "seasons_hardest",
        replica: "The dark half. Not the cold — the dark. You go out in it and you come back in it and you never quite see the day you worked through.",

        new PlayerOption("seasons_ask_endure", "ask what gets them through it",
            "What gets you through that stretch?",
            End("seasons_endure_end", 3,
                "Company, mostly. Which is a roundabout way of thanking you for stopping.",
                "I get through it. I don't need it examined.")),

        new PlayerOption("seasons_offer_company", "offer to come by more often through the dark months",
            "Then I'll come by more often through the dark of it, if you'd not mind.",
            End("seasons_company_end", 3,
                "...I'd not mind. I'd not mind at all. Mind you hold to it.",
                "Don't make promises about winter in the summer. Everyone does and nobody keeps them.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Harvest — rich
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode HarvestTopic() => new(
        nodeId:  "harvest_topic",
        replica: "The crop. {npc:opinion_harvest}",

        new PlayerOption("harvest_wish_well", "wish them a good year of it",
            "Then here's to a heavy one, and dry weather to carry it.",
            End("harvest_wish_end", 2,
                "Aye. Say that again in three months and I'll buy you a drink on it.",
                "Wishing doesn't fill a barn. But thank you, I suppose.")),

        new PlayerOption("harvest_ask_lean", "ask what happens in a lean year",
            "And in a bad year? What happens then?",
            HarvestLean()),

        new PlayerOption("harvest_ask_share", "ask how much of it they actually keep",
            "How much of it ends up yours, when it's all counted?",
            HarvestShare()));

    private static NpcLineNode HarvestLean() => new(
        nodeId:  "harvest_lean",
        replica: "Then you eat what you'd meant to sow, and you pray about the rest. I've seen two of those. I'd rather not see a third.",

        new PlayerOption("harvest_ask_survive", "ask how a household survives it",
            "How does a household come out the other side of that?",
            End("harvest_survive_end", 3,
                "Thin. Very thin. And leaning on whoever will lend — which is why I'm careful who I refuse.",
                "You've never gone hungry, have you. It shows.")),

        new PlayerOption("harvest_offer_help", "say you would help if it came to that",
            "If it comes to a third, you'll not carry it alone. I'll see to that.",
            End("harvest_help_end", 3,
                "...That's a heavy thing to say to someone. I'll remember you said it.",
                "Easy words. Everyone's generous in a good year.")));

    private static NpcLineNode HarvestShare() => new(
        nodeId:  "harvest_share",
        replica: "Less than you'd hope and more than some get. There's the tithe, there's the toll, there's what's owed from last year. What's left is mine.",

        new PlayerOption("harvest_ask_fair", "ask whether they think that fair",
            "And is that fair, do you reckon?",
            End("harvest_fair_end", 3,
                "Fair's not a word that's much use out here. But I'll say it to you, since you asked straight: no.",
                "Careful. That's the sort of question that gets a person talked about.")),

        new PlayerOption("harvest_talk_prices", "ask what it fetches when they do sell",
            "And what does it fetch, when you take it to sell?",
            End("harvest_prices_end", 3,
                "{npc:opinion_trade} You've a head for this. I'll talk shop with you any day.",
                "Prices. Now you sound like every buyer who ever tried to skin me.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Water — short
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode WaterTopic() => new(
        nodeId:  "water_topic",
        replica: "It has. {npc:opinion_water}",

        new PlayerOption("water_agree", "agree that water is never obliging",
            "It gives too much or too little and never the middle.",
            End("water_agree_end", 2,
                "Never the middle. That's it exactly. You've been paying attention.",
                "Everyone says that. It's not clever the fiftieth time.")),

        new PlayerOption("water_ask_flood", "ask whether it has ever come up badly",
            "Has it ever come up over where it shouldn't?",
            WaterFlood()),

        new PlayerOption("water_ask_drink", "ask where the good water is hereabouts",
            "Where's the good water here? I'd rather ask than find out wrong.",
            End("water_drink_end", 2,
                "The spring above the old wall. Not the low well — that's fine for beasts and nothing else. Now you know.",
                "Find out wrong, then. It'll teach you faster than I would.")));

    private static NpcLineNode WaterFlood() => new(
        nodeId:  "water_flood",
        replica: "Twice in my life. The second time it came at night, and we were carrying children out in the dark with the water at our knees.",

        new PlayerOption("water_ask_after_flood", "ask what was left afterward",
            "What was left of it, after?",
            End("water_after_end", 3,
                "Mud in everything, and everyone alive. You learn what order to want things in.",
                "Mud. Ruin. I'd rather not walk through it again for your curiosity.")),

        new PlayerOption("water_say_sorry", "say plainly that must have been a terrible night",
            "That's a night nobody should have had.",
            End("water_sorry_end", 3,
                "No. But it was had, and we're still here. Thank you for saying so.",
                "It was a long time ago. I don't need consoling over it now.")));
}
