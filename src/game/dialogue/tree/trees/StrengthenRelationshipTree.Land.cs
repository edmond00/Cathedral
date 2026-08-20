using Cathedral.Game.Narrative.ModiMentis;

namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// Small-talk subjects about the land and the year: the weather, the turning seasons, the harvest,
/// and the water. Weather and harvest run rich (four ways to follow them); seasons and water run
/// short. See <see cref="StrengthenRelationshipTree"/> for the shape rules, and "Authoring the
/// neutral text" on <see cref="DialogueTree"/> for what a replica may and may not carry.
/// </summary>
public partial class StrengthenRelationshipTree
{
    // ══════════════════════════════════════════════════════════════════════════
    //  Weather — rich
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode WeatherTopic() => new(
        nodeId:          "weather_topic",
        replica:         "The weather. {npc:opinion_weather}",
        replicaIndirect: "I tell {you:name} that of the weather I think {npc:opinion_weather}.",
        replicaHeard:    "{npc:name} tells me what they think of the weather.",

        new PlayerOption("weather_agree", "agree and let it rest there",
            "That is true. There is no arguing with the sky.",
            "I tell {npc:name} there is no arguing with the sky.",
            End("weather_agree_end", 2,
                "There is not. It is good to talk to someone who knows that.",
                "I tell {you:name} it is good to talk to someone who knows that.",
                "Yes. Was that all you wanted?",
                "I ask {you:name} whether that was all they wanted.",
                typeof(WeatherEarModusMentis))),

        new PlayerOption("weather_ask_worst", "ask about the worst weather they remember",
            "What is the worst weather you have seen?",
            "I ask {npc:name} what the worst weather is they have seen.",
            WeatherWorst()),

        new PlayerOption("weather_ask_reading", "ask how they tell what is coming",
            "How do you tell what is coming before it arrives?",
            "I ask {npc:name} how they tell what is coming before it arrives.",
            WeatherReading()));

    private static NpcLineNode WeatherWorst() => new(
        nodeId:          "weather_worst",
        replica:         "A winter some years ago. It took the roofs off two houses, and we spent three weeks digging out.",
        replicaIndirect: "I tell {you:name} about a winter that took the roofs off two houses and left us three weeks digging out.",
        replicaHeard:    "{npc:name} tells me about a winter that took the roofs off two houses and left them three weeks digging out.",

        new PlayerOption("weather_ask_after", "ask how they came through it",
            "How did you come through it?",
            "I ask {npc:name} how they came through it.",
            End("weather_after_end", 3,
                "Badly, and together. That is how anyone comes through anything here.",
                "I tell {you:name} we came through it badly and together, as anyone does here.",
                "We came through it. That is all there is to say.",
                "I tell {you:name} we came through it, and that is all there is to say.",
                typeof(PetrichorModusMentis))),

        new PlayerOption("weather_share_own", "offer a hard season of your own",
            "I have had a season or two like that. You learn what you can do without.",
            "I tell {npc:name} I have had seasons like that, and you learn what you can do without.",
            End("weather_share_end", 3,
                "You do. It is a comfort to hear it from someone who has been through the same.",
                "I tell {you:name} it is a comfort to hear that from someone who has been through the same.",
                "Everybody has had a hard winter. It is not something to trade.",
                "I tell {you:name} everybody has had a hard winter.")));

    private static NpcLineNode WeatherReading() => new(
        nodeId:          "weather_reading",
        replica:         "You watch. The way the smoke leans, the way the animals stand, whether the light turns green before a storm. Nobody teaches it.",
        replicaIndirect: "I tell {you:name} you watch the smoke, the animals and the light, and that nobody teaches it.",
        replicaHeard:    "{npc:name} says you watch the smoke, the animals and the light, and that nobody teaches it.",

        new PlayerOption("weather_ask_teach", "ask them to teach you one sign",
            "Teach me one sign I can use.",
            "I ask {npc:name} to teach me one sign I can use.",
            End("weather_teach_end", 3,
                "If the birds go quiet all at once, get under cover. That is the one I would give away.",
                "I tell {you:name} that if the birds go quiet all at once they should get under cover.",
                "It is not something to hand over. Watch the sky for twenty years as I did.",
                "I tell {you:name} to watch the sky for twenty years as I did.",
                typeof(SkyReadingModusMentis))),

        new PlayerOption("weather_mention_omens", "ask whether they hold with signs and omens",
            "Do you believe in the old signs that people swear by?",
            "I ask {npc:name} whether they believe the old signs.",
            End("weather_omens_end", 3,
                "{npc:opinion_omens} Now you know something about me that most do not.",
                "I tell {you:name} that of the old signs I think {npc:opinion_omens}.",
                "That is a question for someone with time to waste. I have none today.",
                "I tell {you:name} that is a question for someone with time to waste.",
                typeof(WindReadingModusMentis))));

    // ══════════════════════════════════════════════════════════════════════════
    //  Seasons — short
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode SeasonsTopic() => new(
        nodeId:          "seasons_topic",
        replica:         "It does. {npc:opinion_seasons}",
        replicaIndirect: "I tell {you:name} that of the turning year I think {npc:opinion_seasons}.",
        replicaHeard:    "{npc:name} tells me what they think of the turning year.",

        new PlayerOption("seasons_favourite", "ask which part of the year they like best",
            "Which part of the year do you like best?",
            "I ask {npc:name} which part of the year they like best.",
            End("seasons_favourite_end", 2,
                "The turn into the long days. Everything is still ahead and nothing has gone wrong yet.",
                "I tell {you:name} it is the turn into the long days, when nothing has gone wrong yet.",
                "The part where people leave me to get on with it.",
                "I tell {you:name} it is the part where people leave me to get on with it.",
                typeof(QuickeningModusMentis))),

        new PlayerOption("seasons_hardest", "ask which part they dread",
            "Which part do you dread?",
            "I ask {npc:name} which part of the year they dread.",
            SeasonsHardest()),

        new PlayerOption("seasons_agree", "say the year moves faster every time round",
            "The year comes round quicker each time. Or I am slower.",
            "I tell {npc:name} the year comes round quicker each time.",
            End("seasons_quicker_end", 2,
                "That is exactly it, and I have never heard it put so plainly. You are good company.",
                "I tell {you:name} I have never heard it put so plainly, and that they are good company.",
                "Speak for yourself. My years are long enough.",
                "I tell {you:name} to speak for themselves, since my years are long enough.",
                typeof(ForesightModusMentis))));

    private static NpcLineNode SeasonsHardest() => new(
        nodeId:          "seasons_hardest",
        replica:         "The dark half. Not the cold, the dark. You go out in it and come back in it and never see the day you worked through.",
        replicaIndirect: "I tell {you:name} it is the dark half, since you go out in it and come back in it and never see the day.",
        replicaHeard:    "{npc:name} says it is the dark half, since they go out in it and come back in it and never see the day.",

        new PlayerOption("seasons_ask_endure", "ask what gets them through it",
            "What gets you through that part of the year?",
            "I ask {npc:name} what gets them through it.",
            End("seasons_endure_end", 3,
                "Company, mostly. Which is my way of thanking you for stopping.",
                "I tell {you:name} it is company, and thank them for stopping.",
                "I get through it. I do not need it examined.",
                "I tell {you:name} I get through it and do not need it examined.",
                typeof(EnduranceModusMentis))),

        new PlayerOption("seasons_offer_company", "offer to come by more often through the dark months",
            "Then I will come by more often through the dark months, if you do not mind.",
            "I offer {npc:name} to come by more often through the dark months.",
            End("seasons_company_end", 3,
                "I would not mind at all. See that you hold to it.",
                "I tell {you:name} I would not mind at all, and to hold to it.",
                "Do not make promises about winter in the summer. Nobody keeps them.",
                "I tell {you:name} not to make promises about winter in the summer.",
                typeof(GregariousnessModusMentis))));

    // ══════════════════════════════════════════════════════════════════════════
    //  Harvest — rich
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode HarvestTopic() => new(
        nodeId:          "harvest_topic",
        replica:         "The crop. {npc:opinion_harvest}",
        replicaIndirect: "I tell {you:name} that of this year's crop I think {npc:opinion_harvest}.",
        replicaHeard:    "{npc:name} tells me what they think of this year's crop.",

        new PlayerOption("harvest_wish_well", "wish them a good year of it",
            "I hope it is a heavy crop and the weather stays dry for it.",
            "I wish {npc:name} a heavy crop and dry weather to carry it.",
            End("harvest_wish_end", 2,
                "Say that again in three months and I will buy you a drink on it.",
                "I tell {you:name} to say that again in three months and I will buy them a drink.",
                "Wishing does not fill a barn. But thank you.",
                "I tell {you:name} wishing does not fill a barn.",
                typeof(HarvestryModusMentis))),

        new PlayerOption("harvest_ask_lean", "ask what happens in a lean year",
            "What happens in a bad year?",
            "I ask {npc:name} what happens in a bad year.",
            HarvestLean()),

        new PlayerOption("harvest_ask_share", "ask how much of it they actually keep",
            "How much of it is yours, once it is all counted?",
            "I ask {npc:name} how much of it is theirs once it is counted.",
            HarvestShare()));

    private static NpcLineNode HarvestLean() => new(
        nodeId:          "harvest_lean",
        replica:         "Then you eat what you meant to sow, and hope for the rest. I have seen two of those. I would rather not see a third.",
        replicaIndirect: "I tell {you:name} you eat what you meant to sow, that I have seen two of those, and would rather not see a third.",
        replicaHeard:    "{npc:name} says you eat what you meant to sow, that they have seen two of those, and would rather not see a third.",

        new PlayerOption("harvest_ask_survive", "ask how a household survives it",
            "How does a household come through that?",
            "I ask {npc:name} how a household comes through that.",
            End("harvest_survive_end", 3,
                "Barely, and by borrowing, which is why I am careful whom I refuse.",
                "I tell {you:name} it is barely, and by borrowing, which is why I am careful whom I refuse.",
                "You have never gone hungry. It shows.",
                "I tell {you:name} they have never gone hungry and it shows.",
                typeof(ThriftModusMentis))),

        new PlayerOption("harvest_offer_help", "say you would help if it came to that",
            "If a third year comes, you will not carry it alone. I will see to that.",
            "I promise {npc:name} they will not carry a third such year alone.",
            End("harvest_help_end", 3,
                "That is a serious thing to say to someone. I will remember you said it.",
                "I tell {you:name} that is a serious thing to say, and that I will remember they said it.",
                "Easy words. Everyone is generous in a good year.",
                "I tell {you:name} everyone is generous in a good year.",
                typeof(HardLaborModusMentis))));

    private static NpcLineNode HarvestShare() => new(
        nodeId:          "harvest_share",
        replica:         "Less than you would hope and more than some get. There is the tithe, the toll, and what is owed from last year. What is left is mine.",
        replicaIndirect: "I tell {you:name} there is the tithe, the toll and last year's debt, and that what is left is mine.",
        replicaHeard:    "{npc:name} says there is the tithe, the toll and last year's debt, and that what is left is theirs.",

        new PlayerOption("harvest_ask_fair", "ask whether they think that fair",
            "Do you think that is fair?",
            "I ask {npc:name} whether they think that is fair.",
            End("harvest_fair_end", 3,
                "Fair is not a useful word out here. But since you asked straight: no.",
                "I tell {you:name} fair is not a useful word out here, but that since they asked straight, no.",
                "Be careful. That is the sort of question that gets a person talked about.",
                "I warn {you:name} that this is the sort of question that gets a person talked about.",
                typeof(PlainDealingModusMentis))),

        new PlayerOption("harvest_talk_prices", "ask what it fetches when they do sell",
            "What does it fetch when you take it to sell?",
            "I ask {npc:name} what it fetches when they sell.",
            End("harvest_prices_end", 3,
                "{npc:opinion_trade} You have a head for this. I will talk trade with you any day.",
                "I tell {you:name} that of trade I think {npc:opinion_trade}, and that they have a head for it.",
                "Prices. Now you sound like every buyer who has tried to cheat me.",
                "I tell {you:name} they sound like every buyer who has tried to cheat me.",
                typeof(BargainingModusMentis))));

    // ══════════════════════════════════════════════════════════════════════════
    //  Water — short
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode WaterTopic() => new(
        nodeId:          "water_topic",
        replica:         "It has. {npc:opinion_water}",
        replicaIndirect: "I tell {you:name} that of the water I think {npc:opinion_water}.",
        replicaHeard:    "{npc:name} tells me what they think of the water.",

        new PlayerOption("water_agree", "agree that water is never obliging",
            "It gives too much or too little and never the right amount.",
            "I tell {npc:name} it gives too much or too little and never the right amount.",
            End("water_agree_end", 2,
                "Never the right amount. That is exactly it. You have been paying attention.",
                "I tell {you:name} that is exactly it, and that they have been paying attention.",
                "Everyone says that. It is not clever the fiftieth time.",
                "I tell {you:name} it is not clever the fiftieth time.",
                typeof(DrainageModusMentis))),

        new PlayerOption("water_ask_flood", "ask whether it has ever come up badly",
            "Has it ever risen where it should not?",
            "I ask {npc:name} whether it has ever risen where it should not.",
            WaterFlood()),

        new PlayerOption("water_ask_drink", "ask where the good water is hereabouts",
            "Where is the good water here? I would rather ask than guess wrong.",
            "I ask {npc:name} where the good water is here.",
            End("water_drink_end", 2,
                "The spring above the old wall. Not the low well, which is fit for animals only.",
                "I tell {you:name} it is the spring above the old wall, not the low well.",
                "Guess wrong, then. It will teach you faster than I would.",
                "I tell {you:name} to guess wrong, since it will teach them faster than I would.",
                typeof(TaintSenseModusMentis))));

    private static NpcLineNode WaterFlood() => new(
        nodeId:          "water_flood",
        replica:         "Twice in my life. The second time it came at night, and we carried children out in the dark with the water at our knees.",
        replicaIndirect: "I tell {you:name} it happened twice, and that the second time we carried children out in the dark.",
        replicaHeard:    "{npc:name} says it happened twice, and that the second time they carried children out in the dark.",

        new PlayerOption("water_ask_after_flood", "ask what was left afterward",
            "What was left afterwards?",
            "I ask {npc:name} what was left afterwards.",
            End("water_after_end", 3,
                "Mud in everything, and everyone alive. You learn what to want first.",
                "I tell {you:name} there was mud in everything and everyone alive, and that you learn what to want first.",
                "Mud and ruin. I would rather not go through it again for your curiosity.",
                "I tell {you:name} I would rather not go through it again for their curiosity.",
                typeof(WaterVoiceModusMentis))),

        new PlayerOption("water_say_sorry", "say plainly that must have been a terrible night",
            "That is a night nobody should have had.",
            "I tell {npc:name} that is a night nobody should have had.",
            End("water_sorry_end", 3,
                "No. But we had it, and we are still here. Thank you for saying so.",
                "I tell {you:name} we had it and are still here, and thank them for saying so.",
                "It was a long time ago. I do not need consoling now.",
                "I tell {you:name} it was long ago and I do not need consoling.",
                typeof(EmpathyModusMentis))));
}
