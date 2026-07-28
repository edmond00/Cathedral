namespace Cathedral.Game.Dialogue.Tree.Trees;

/// <summary>
/// Small-talk subjects about work and the world around it: their trade, the beasts, the wild country
/// past the fields, and the folk hereabouts. The wild country runs deep — it is where the old stories
/// live — and the neighbours run short, because gossip is cheap.
/// See <see cref="StrengthenRelationshipTree"/> for the shape rules.
/// </summary>
public partial class StrengthenRelationshipTree
{
    // ══════════════════════════════════════════════════════════════════════════
    //  Work — rich
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode WorkTopic() => new(
        nodeId:  "work_topic",
        replica: "The work? {npc:opinion_work}",

        new PlayerOption("work_respect", "say plainly that it is honest work",
            "It's honest work, and there's not enough of that about.",
            End("work_respect_end", 2,
                "There isn't. And it's a rare person who says so without wanting something. Good day to you.",
                "Honest. Aye. Everyone's full of praise for work they'd never do.")),

        new PlayerOption("work_ask_hardest", "ask what the hardest part of it is",
            "What's the worst part of it, if you're honest?",
            WorkHardest()),

        new PlayerOption("work_ask_pride", "ask what part of it they are proudest of",
            "And what part of it are you proudest of?",
            WorkPride()));

    private static NpcLineNode WorkHardest() => new(
        nodeId:  "work_hardest",
        replica: "Not the labour. It's that there's no end to it — you finish and it's already starting again behind you. {npc:labour}. Every day of it.",

        new PlayerOption("work_ask_stop", "ask whether they ever think of doing something else",
            "Do you ever think about doing something else entirely?",
            End("work_stop_end", 3,
                "Every winter. Then spring comes and my hands know what to do before I've decided anything. Aye — I've said too much.",
                "And do what? That's a question for people with choices.")),

        new PlayerOption("work_say_seen", "say you had not thought about it that way",
            "I'd never thought of it as the not-ending being the hard part.",
            End("work_seen_end", 3,
                "Most don't. They see the sweat and think that's the whole of it. You listened properly.",
                "Well, now you have. There's your lesson for the day.")));

    private static NpcLineNode WorkPride() => new(
        nodeId:  "work_pride",
        replica: "There's things I've made — or done, or kept standing — that'll outlast me. Nobody will know it was mine. I'll know.",

        new PlayerOption("work_ask_show", "ask to be shown one of them",
            "Show me one. I'd like to see it.",
            End("work_show_end", 3,
                "...Alright. Come by {npc:workplace} and I will. Nobody's ever asked before.",
                "It's not a thing to be paraded. Look about you, you'll walk past it.")),

        new PlayerOption("work_ask_taught", "ask who taught them",
            "Who taught you to do it that well?",
            End("work_taught_end", 3,
                "Someone who's dead now, and who'd not have said a kind word about my work if I'd begged for one. I still hear them.",
                "That's mine. I'll not hand it over for the asking.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Beasts — rich
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode BeastsTopic() => new(
        nodeId:  "beasts_topic",
        replica: "The beasts? {npc:opinion_beasts}",

        new PlayerOption("beasts_agree", "agree that animals are easier to read than people",
            "They're easier to read than people, at least. They don't pretend.",
            End("beasts_agree_end", 2,
                "Ha! No, they don't. You and I are going to get on, I think.",
                "They bite, they kick and they run off. Don't romanticise it.")),

        new PlayerOption("beasts_ask_favourite", "ask whether there is one they are fond of",
            "Is there one of them you're soft about? There always is.",
            BeastsFavourite()),

        new PlayerOption("beasts_ask_trouble", "ask what trouble the beasts give them",
            "And what trouble do they give you?",
            BeastsTrouble()));

    private static NpcLineNode BeastsFavourite() => new(
        nodeId:  "beasts_favourite",
        replica: "...There's one. I'll not say which, and I'd deny it under oath. But there's one.",

        new PlayerOption("beasts_press_kindly", "press them, kindly, to say more",
            "Go on. I'll not tell anyone.",
            End("beasts_press_end", 3,
                "The old one, that should have gone last winter and didn't. I've been keeping it going out of pure stubbornness. There. Now you know.",
                "No. Some things stay mine.")),

        new PlayerOption("beasts_let_be", "let them keep it to themselves",
            "Then I'll not ask which. Some things are yours.",
            End("beasts_let_be_end", 3,
                "...You're the first person to leave a thing alone when I asked. That's worth more than the answer would have been.",
                "Aye. Fine. Then there's nothing more to say, is there.")));

    private static NpcLineNode BeastsTrouble() => new(
        nodeId:  "beasts_trouble",
        replica: "They get out. They get sick at the worst moment. They know exactly when you're in a hurry and they choose that hour to be stupid.",

        new PlayerOption("beasts_laugh", "laugh and say they clearly know you too well",
            "They've got your measure, then. That's the worst of it.",
            End("beasts_laugh_end", 3,
                "They have. Forty years and they're still ahead of me. Good to laugh about it with someone.",
                "It's not a joke when it's your year they're ruining.")),

        new PlayerOption("beasts_ask_loss", "ask whether they have lost many",
            "Have you lost many of them, over the years?",
            End("beasts_loss_end", 3,
                "Enough that I stopped naming them, and then started again, because not naming them didn't help either.",
                "That's a cold thing to ask. Every one of them cost me.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Wilds — deep
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode WildsTopic() => new(
        nodeId:  "wilds_topic",
        replica: "Out past the last field? {npc:opinion_wilds}",

        new PlayerOption("wilds_agree", "say you have felt the same about wild country",
            "I've stood at that edge myself. It's a different air past it.",
            End("wilds_agree_end", 2,
                "It is. Most folk here have never gone and never wondered. You've wondered. That's something.",
                "It's trees and weather. You're making a poem of it.")),

        new PlayerOption("wilds_ask_far", "ask how far out they have been",
            "How far out have you actually gone?",
            WildsFar()),

        new PlayerOption("wilds_ask_stories", "ask what is said about what lives out there",
            "And what do folk here say lives out there?",
            WildsStories()));

    private static NpcLineNode WildsFar() => new(
        nodeId:  "wilds_far",
        replica: "Further than I tell people. There's a place a half-day out where the ground changes and the birds stop. I've stood in it twice and not gone back.",

        new PlayerOption("wilds_ask_why_not", "ask why they never went back",
            "Why not a third time?",
            End("wilds_why_not_end", 3,
                "Because both times I came home wanting to go further, and I've a household. That's the whole reason and it's reason enough.",
                "Because I'm not a fool. Neither are you, I'd hoped.")),

        new PlayerOption("wilds_offer_go", "offer to walk out that way together sometime",
            "Then walk out that way with me sometime. Two go further than one.",
            End("wilds_offer_end", 3,
                "...Ask me again when the work's slack. I might say yes, and that frightens me a little.",
                "With you? I've known you five minutes and a river of trouble.")));

    private static NpcLineNode WildsStories() => new(
        nodeId:  "wilds_stories",
        replica: "{npc:opinion_stories} And there's the ones nobody tells in daylight, which are the ones you'd actually want.",

        new PlayerOption("wilds_ask_daylight", "ask for one of the daylight ones",
            "Give me one of the daylight ones, then. I'll take what I'm allowed.",
            End("wilds_daylight_end", 3,
                "Alright — there's a track out there that's older than any village, and it goes somewhere nobody living has been. That's the tame version.",
                "You'll get none of them from me. Go ask the children, they'll tell you anything.")),

        new PlayerOption("wilds_ask_night", "ask for one of the ones told after dark",
            "And the ones nobody tells in daylight?",
            WildsNight()));

    private static NpcLineNode WildsNight() => new(
        nodeId:  "wilds_night",
        replica: "...You'd have to be someone I trusted. Those aren't tales, exactly. They're things people saw and were laughed at for saying.",

        new PlayerOption("wilds_say_believe", "say you would not laugh",
            "I'd not laugh. I've seen a thing or two I stopped mentioning.",
            End("wilds_believe_end", 4,
                "...Then come by after dark, and bring nothing to drink, and I'll tell you what my own father saw. I've told two people. You'd be the third.",
                "Everyone says that. Then it's round the village by the week's end.")),

        new PlayerOption("wilds_wait", "say you will wait until they trust you enough",
            "Then I'll wait until I'm someone you trust. It'll keep.",
            End("wilds_wait_end", 4,
                "...That was the right answer. You'll hear it. Not today — but you'll hear it.",
                "You'll be waiting a long while.")));

    // ══════════════════════════════════════════════════════════════════════════
    //  Neighbours — short
    // ══════════════════════════════════════════════════════════════════════════

    private static NpcLineNode NeighboursTopic() => new(
        nodeId:  "neighbours_topic",
        replica: "The folk here? {npc:opinion_neighbours}",

        new PlayerOption("neighbours_agree", "say that sounds like every village you have known",
            "That's every place I've ever stopped in, near enough.",
            End("neighbours_agree_end", 2,
                "It is, isn't it. Folk are folk wherever you set them down. Fare well.",
                "Then you've not been paying attention. This place isn't like others.")),

        new PlayerOption("neighbours_ask_quarrel", "ask whether there is a quarrel running",
            "Is there a quarrel running I ought to know about?",
            NeighboursQuarrel()),

        new PlayerOption("neighbours_refuse_gossip", "say you would rather not hear tales about people",
            "Come to think of it — I'd rather not hear tales about folk behind their backs.",
            End("neighbours_refuse_end", 2,
                "...Hm. That's a rarer answer than you'd think, and a better one. I'll remember it of you.",
                "Then why in the world did you ask?")));

    private static NpcLineNode NeighboursQuarrel() => new(
        nodeId:  "neighbours_quarrel",
        replica: "There's always one running. Two households that have been sour since before either of them could tell you why, and everyone else picking a side at market.",

        new PlayerOption("neighbours_ask_side", "ask which side they take",
            "And which side do you stand on?",
            End("neighbours_side_end", 3,
                "Neither, out loud. Both, depending who's asking. That's how a person survives a small place.",
                "That's exactly the question that starts a third quarrel. No.")),

        new PlayerOption("neighbours_ask_mend", "ask whether it could be mended",
            "Could it be mended, do you think? By someone from outside it?",
            End("neighbours_mend_end", 3,
                "...Maybe. Nobody's tried in twenty years. It'd take someone with nothing to lose by it. Interesting that you'd ask.",
                "By an outsider? That's the fastest way to make it worse. Leave it alone.")));
}
