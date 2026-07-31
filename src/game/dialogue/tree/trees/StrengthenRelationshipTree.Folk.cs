namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// Small-talk subjects about work and the world around it: their trade, the beasts, the wild country
/// past the fields, and the folk hereabouts. The wild country runs deep — it is where the old stories
/// live — and the neighbours run short, because gossip is cheap.
/// See <see cref="StrengthenRelationshipTree"/> for the shape rules, and "Authoring the neutral
/// text" on <see cref="DialogueTree"/> for what a replica may and may not carry.
/// </summary>
public partial class StrengthenRelationshipTree
{
    // ══════════════════════════════════════════════════════════════════════════
    //  Work — rich
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode WorkTopic() => new(
        nodeId:          "work_topic",
        replica:         "The work? {npc:opinion_work}",
        replicaIndirect: "I tell {you:name} that of the work I think {npc:opinion_work}.",
        replicaHeard:    "{npc:name} tells me what they think of their work.",

        new PlayerOption("work_respect", "say plainly that it is honest work",
            "It is honest work, and there is not enough of that about.",
            "I tell {npc:name} it is honest work and there is not enough of that about.",
            End("work_respect_end", 2,
                "There is not. And it is a rare person who says so without wanting something. Good day to you.",
                "I tell {you:name} few say so without wanting something, and bid them good day.",
                "Everyone praises work they would never do themselves.",
                "I tell {you:name} everyone praises work they would never do.")),

        new PlayerOption("work_ask_hardest", "ask what the hardest part of it is",
            "What is the worst part of it, honestly?",
            "I ask {npc:name} what the worst part of it is.",
            WorkHardest()),

        new PlayerOption("work_ask_pride", "ask what part of it they are proudest of",
            "What part of it are you proudest of?",
            "I ask {npc:name} what part of it they are proudest of.",
            WorkPride()));

    private static NpcLineNode WorkHardest() => new(
        nodeId:          "work_hardest",
        replica:         "Not the labour. It is that there is no end to it. You finish and it has already begun again. {npc:labour}. Every day.",
        replicaIndirect: "I tell {you:name} it is not the labour but that there is no end to it, and that my day is {npc:labour}.",
        replicaHeard:    "{npc:name} says it is not the labour but that there is no end to it, and describes their working day.",

        new PlayerOption("work_ask_stop", "ask whether they ever think of doing something else",
            "Do you ever think of doing something else entirely?",
            "I ask {npc:name} whether they ever think of doing something else.",
            End("work_stop_end", 3,
                "Every winter. Then spring comes and my hands start before I have decided anything. I have said too much.",
                "I admit I do every winter, and that come spring my hands start before I have decided anything.",
                "And do what? That is a question for people with choices.",
                "I ask {you:name} what else I would do, since that is a question for people with choices.")),

        new PlayerOption("work_say_seen", "say you had not thought about it that way",
            "I had not thought of the not-ending as the hard part.",
            "I tell {npc:name} I had not thought of the not-ending as the hard part.",
            End("work_seen_end", 3,
                "Most do not. They see the sweat and think that is all of it. You listened properly.",
                "I tell {you:name} most see the sweat and think that is all of it, and that they listened properly.",
                "Well, now you have. There is your lesson for the day.",
                "I tell {you:name} that is their lesson for the day.")));

    private static NpcLineNode WorkPride() => new(
        nodeId:          "work_pride",
        replica:         "There are things I have made, or kept standing, that will outlast me. Nobody will know they were mine. I will know.",
        replicaIndirect: "I tell {you:name} there are things I have made that will outlast me, and that nobody but me will know they were mine.",
        replicaHeard:    "{npc:name} says there are things they have made that will outlast them, and that nobody but them will know they were theirs.",

        new PlayerOption("work_ask_show", "ask to be shown one of them",
            "Show me one. I would like to see it.",
            "I ask {npc:name} to show me one.",
            End("work_show_end", 3,
                "Come by {npc:workplace} and I will. Nobody has ever asked before.",
                "I tell {you:name} to come by {npc:workplace}, and that nobody has ever asked before.",
                "It is not for showing off. Look about you and you will walk past it.",
                "I tell {you:name} it is not for showing off, and they will walk past it.")),

        new PlayerOption("work_ask_taught", "ask who taught them",
            "Who taught you to do it that well?",
            "I ask {npc:name} who taught them to do it that well.",
            End("work_taught_end", 3,
                "Someone who is dead now, and who never had a kind word for my work. I still hear them.",
                "I tell {you:name} it was someone dead now who never had a kind word for my work.",
                "That is mine. I will not hand it over for the asking.",
                "I tell {you:name} that is mine and I will not hand it over for the asking.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Beasts — rich
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode BeastsTopic() => new(
        nodeId:          "beasts_topic",
        replica:         "The animals. {npc:opinion_beasts}",
        replicaIndirect: "I tell {you:name} that of the animals I think {npc:opinion_beasts}.",
        replicaHeard:    "{npc:name} tells me what they think of the animals.",

        new PlayerOption("beasts_agree", "agree that animals are easier to read than people",
            "They are easier to read than people. They do not pretend.",
            "I tell {npc:name} animals are easier to read than people, since they do not pretend.",
            End("beasts_agree_end", 2,
                "No, they do not. I think you and I will get on.",
                "I tell {you:name} I think we will get on.",
                "They bite, they kick and they run off. Do not make them into something they are not.",
                "I tell {you:name} they bite and kick and run off, and not to make them into something else.")),

        new PlayerOption("beasts_ask_favourite", "ask whether there is one they are fond of",
            "Is there one of them you are fond of? There usually is.",
            "I ask {npc:name} whether there is one of them they are fond of.",
            BeastsFavourite()),

        new PlayerOption("beasts_ask_trouble", "ask what trouble the beasts give them",
            "What trouble do they give you?",
            "I ask {npc:name} what trouble the animals give them.",
            BeastsTrouble()));

    private static NpcLineNode BeastsFavourite() => new(
        nodeId:          "beasts_favourite",
        replica:         "There is one. I will not say which, and I would deny it under oath. But there is one.",
        replicaIndirect: "I admit to {you:name} there is one, and that I will not say which and would deny it under oath.",
        replicaHeard:    "{npc:name} admits there is one, and says they will not tell me which and would deny it under oath.",

        new PlayerOption("beasts_press_kindly", "press them, kindly, to say more",
            "Go on. I will not tell anyone.",
            "I press {npc:name} to go on, and promise not to tell anyone.",
            End("beasts_press_end", 3,
                "The old one, that should have died last winter and did not. I have kept it going out of stubbornness.",
                "I tell {you:name} it is the old one that should have died last winter, kept going out of stubbornness.",
                "No. Some things stay mine.",
                "I tell {you:name} some things stay mine.")),

        new PlayerOption("beasts_let_be", "let them keep it to themselves",
            "Then I will not ask which. Some things are yours.",
            "I tell {npc:name} I will not ask which, since some things are theirs.",
            End("beasts_let_be_end", 3,
                "You are the first to leave a thing alone when I asked. That is worth more than the answer.",
                "I tell {you:name} they are the first to leave a thing alone when I asked.",
                "Then there is nothing more to say.",
                "I tell {you:name} there is nothing more to say, then.")));

    private static NpcLineNode BeastsTrouble() => new(
        nodeId:          "beasts_trouble",
        replica:         "They get out. They fall sick at the worst moment. They know when you are in a hurry and choose that hour to be stupid.",
        replicaIndirect: "I tell {you:name} they get out, fall sick at the worst moment, and choose the hour you are in a hurry.",
        replicaHeard:    "{npc:name} says the animals get out, fall sick at the worst moment, and choose the hour you are in a hurry.",

        new PlayerOption("beasts_laugh", "laugh and say they clearly know you too well",
            "They have your measure, then.",
            "I tell {npc:name} the animals have their measure.",
            End("beasts_laugh_end", 3,
                "They do. Forty years and they are still ahead of me. Good to laugh about it with someone.",
                "I tell {you:name} forty years on they are still ahead of me, and that it is good to laugh about it.",
                "It is not a joke when it is your year they are ruining.",
                "I tell {you:name} it is not a joke when it is your year they are ruining.")),

        new PlayerOption("beasts_ask_loss", "ask whether they have lost many",
            "Have you lost many of them over the years?",
            "I ask {npc:name} whether they have lost many over the years.",
            End("beasts_loss_end", 3,
                "Enough that I stopped naming them, and then started again, because not naming them did not help.",
                "I tell {you:name} I stopped naming them, then started again because it did not help.",
                "That is a cold thing to ask. Every one of them cost me.",
                "I tell {you:name} that is a cold thing to ask, and every one of them cost me.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Wilds — deep
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode WildsTopic() => new(
        nodeId:          "wilds_topic",
        replica:         "Past the last field. {npc:opinion_wilds}",
        replicaIndirect: "I tell {you:name} that of the country past the last field I think {npc:opinion_wilds}.",
        replicaHeard:    "{npc:name} tells me what they think of the country past the last field.",

        new PlayerOption("wilds_agree", "say you have felt the same about wild country",
            "I have stood at that edge myself. It is different past it.",
            "I tell {npc:name} I have stood at that edge myself and it is different past it.",
            End("wilds_agree_end", 2,
                "It is. Most people here have never gone and never wondered. You wondered.",
                "I tell {you:name} most here have never gone and never wondered, but they wondered.",
                "It is trees and weather. You are making more of it than there is.",
                "I tell {you:name} it is trees and weather.")),

        new PlayerOption("wilds_ask_far", "ask how far out they have been",
            "How far out have you actually gone?",
            "I ask {npc:name} how far out they have gone.",
            WildsFar()),

        new PlayerOption("wilds_ask_stories", "ask what is said about what lives out there",
            "What do people here say lives out there?",
            "I ask {npc:name} what people here say lives out there.",
            WildsStories()));

    private static NpcLineNode WildsFar() => new(
        nodeId:          "wilds_far",
        replica:         "Further than I tell people. There is a place half a day out where the ground changes and the birds stop. I have stood in it twice and not gone back.",
        replicaIndirect: "I tell {you:name} there is a place half a day out where the ground changes and the birds stop, and that I have stood in it twice.",
        replicaHeard:    "{npc:name} says there is a place half a day out where the ground changes and the birds stop, and that they have stood in it twice.",

        new PlayerOption("wilds_ask_why_not", "ask why they never went back",
            "Why not a third time?",
            "I ask {npc:name} why they did not go a third time.",
            End("wilds_why_not_end", 3,
                "Because both times I came home wanting to go further, and I have a household. That is reason enough.",
                "I tell {you:name} both times I came home wanting to go further, and that I have a household.",
                "Because I am not a fool. I had hoped you were not either.",
                "I tell {you:name} it is because I am not a fool, and I had hoped they were not either.")),

        new PlayerOption("wilds_offer_go", "offer to walk out that way together sometime",
            "Then walk out that way with me sometime. Two can go further than one.",
            "I offer {npc:name} to walk out that way with me, since two go further than one.",
            End("wilds_offer_end", 3,
                "Ask me again when the work is slack. I might say yes, and that unsettles me.",
                "I tell {you:name} to ask again when the work is slack, and that I might say yes.",
                "With you? I have known you five minutes.",
                "I tell {you:name} I have known them five minutes.")));

    private static NpcLineNode WildsStories() => new(
        nodeId:          "wilds_stories",
        replica:         "{npc:opinion_stories} And there are the ones nobody tells in daylight, which are the ones you would want.",
        replicaIndirect: "I tell {you:name} that {npc:opinion_stories}, and that the ones nobody tells in daylight are the ones they would want.",
        replicaHeard:    "{npc:name} tells me the stories that are told, and says the ones nobody tells in daylight are the ones I would want.",

        new PlayerOption("wilds_ask_daylight", "ask for one of the daylight ones",
            "Give me one of the daylight ones, then.",
            "I ask {npc:name} for one of the daylight ones.",
            End("wilds_daylight_end", 3,
                "There is a track out there older than any village, and it goes somewhere nobody living has been. That is the tame version.",
                "I tell {you:name} there is a track out there older than any village that goes where nobody living has been.",
                "You will get none of them from me. Ask the children, they will tell you anything.",
                "I tell {you:name} to ask the children, who will tell them anything.")),

        new PlayerOption("wilds_ask_night", "ask for one of the ones told after dark",
            "And the ones nobody tells in daylight?",
            "I ask {npc:name} for one of the ones nobody tells in daylight.",
            WildsNight()));

    private static NpcLineNode WildsNight() => new(
        nodeId:          "wilds_night",
        replica:         "You would have to be someone I trusted. Those are not tales. They are things people saw and were laughed at for saying.",
        replicaIndirect: "I tell {you:name} those are not tales but things people saw and were laughed at for saying, and that I would have to trust them first.",
        replicaHeard:    "{npc:name} says those are not tales but things people saw and were laughed at for saying, and that they would have to trust me first.",

        new PlayerOption("wilds_say_believe", "say you would not laugh",
            "I would not laugh. I have seen things I stopped mentioning.",
            "I tell {npc:name} I would not laugh, since I have seen things I stopped mentioning.",
            End("wilds_believe_end", 4,
                "Then come after dark, bring nothing to drink, and I will tell you what my father saw. You would be the third to hear it.",
                "I tell {you:name} to come after dark, and that they would be the third person to hear what my father saw.",
                "Everyone says that, and then it is round the village by the week's end.",
                "I tell {you:name} it would be round the village by the week's end.")),

        new PlayerOption("wilds_wait", "say you will wait until they trust you enough",
            "Then I will wait until you trust me. It will keep.",
            "I tell {npc:name} I will wait until they trust me, since it will keep.",
            End("wilds_wait_end", 4,
                "That was the right answer. You will hear it, though not today.",
                "I tell {you:name} that was the right answer, and that they will hear it, though not today.",
                "You will be waiting a long time.",
                "I tell {you:name} they will be waiting a long time.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Neighbours — short
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode NeighboursTopic() => new(
        nodeId:          "neighbours_topic",
        replica:         "The people here. {npc:opinion_neighbours}",
        replicaIndirect: "I tell {you:name} that of the people here I think {npc:opinion_neighbours}.",
        replicaHeard:    "{npc:name} tells me what they think of the people here.",

        new PlayerOption("neighbours_agree", "say that sounds like every village you have known",
            "That is much the same as every place I have stopped in.",
            "I tell {npc:name} that is much the same as every place I have stopped in.",
            End("neighbours_agree_end", 2,
                "It is. People are people wherever you put them. Farewell.",
                "I tell {you:name} people are people wherever you put them, and bid them farewell.",
                "Then you have not been paying attention. This place is not like others.",
                "I tell {you:name} they have not been paying attention.")),

        new PlayerOption("neighbours_ask_quarrel", "ask whether there is a quarrel running",
            "Is there a quarrel running that I should know about?",
            "I ask {npc:name} whether there is a quarrel running I should know about.",
            NeighboursQuarrel()),

        new PlayerOption("neighbours_refuse_gossip", "say you would rather not hear tales about people",
            "On second thought, I would rather not hear tales about people behind their backs.",
            "I tell {npc:name} I would rather not hear tales about people behind their backs.",
            End("neighbours_refuse_end", 2,
                "That is a rarer answer than you would think, and a better one. I will remember it.",
                "I tell {you:name} that is a rarer answer than they would think, and that I will remember it.",
                "Then why did you ask?",
                "I ask {you:name} why they asked, then.")));

    private static NpcLineNode NeighboursQuarrel() => new(
        nodeId:          "neighbours_quarrel",
        replica:         "There is always one. Two households that have been sour since before either could tell you why, and everyone else taking sides at market.",
        replicaIndirect: "I tell {you:name} two households have been sour since before either could say why, and everyone else takes sides at market.",
        replicaHeard:    "{npc:name} says two households have been sour since before either could say why, and everyone else takes sides at market.",

        new PlayerOption("neighbours_ask_side", "ask which side they take",
            "Which side do you take?",
            "I ask {npc:name} which side they take.",
            End("neighbours_side_end", 3,
                "Neither, aloud. Both, depending who is asking. That is how a person survives a small place.",
                "I tell {you:name} it is neither aloud and both depending who asks, and that this is how a person survives here.",
                "That is exactly the question that starts a third quarrel. No.",
                "I tell {you:name} that is the question that starts a third quarrel.")),

        new PlayerOption("neighbours_ask_mend", "ask whether it could be mended",
            "Could it be mended, do you think? By someone from outside it?",
            "I ask {npc:name} whether an outsider could mend it.",
            End("neighbours_mend_end", 3,
                "Perhaps. Nobody has tried in twenty years. It would take someone with nothing to lose by it.",
                "I tell {you:name} nobody has tried in twenty years, and it would take someone with nothing to lose.",
                "By an outsider? That is the fastest way to make it worse. Leave it alone.",
                "I tell {you:name} an outsider is the fastest way to make it worse.")));
}
