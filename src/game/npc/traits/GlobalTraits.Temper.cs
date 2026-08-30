using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Traits;

/// <summary>
/// Global traits about <b>temper</b>: what a person is like to deal with across a table. These lean
/// hardest on the persona note, since temper is mostly audible rather than visible — but each still
/// leaves some mark on how they stand or what they carry.
/// </summary>
public sealed partial class PersonalityTraitRegistry
{
    private void RegisterTemperTraits()
    {
        Add(new PersonalityTrait
        {
            TraitId     = "quick_tempered",
            DisplayName = "Quick-Tempered",
            ModiMentis  = new[] { "rage", "invective" },
            Organs      = new[] { ("spleen", 1) },
            Appearance  = "carries a tightness about the jaw, as though halfway through an argument",
            Persona     = "You anger fast and cool fast. You say the sharp thing before you have decided to, and you rarely apologise for it afterwards even when you regret it.",
            Opinions    = new[] { (DialogueTopic.Neighbours, "I've fallen out with half this village at some point. Most of them came back") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "slow_to_anger",
            DisplayName = "Slow to Anger",
            ModiMentis  = new[] { "patience", "cold_blood" },
            Appearance  = "unhurried, and looks at people a moment longer than they expect before answering",
            Persona     = "You are very hard to provoke. You let insults sit rather than answer them, which unsettles people more than shouting would.",
            Opinions    = new[] { (DialogueTopic.Neighbours, "There's no quarrel here worth the losing of a night's sleep") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "talkative",
            DisplayName = "Talkative",
            ModiMentis  = new[] { "social_interaction", "prosaic_grammar" },
            Organs      = new[] { ("tongue", 1) },
            Appearance  = "already talking before you are close enough to hear it",
            Persona     = "You talk a great deal. You answer questions nobody asked, wander off the subject, and catch yourself doing it about half the time.",
            Opinions    = new[] { (DialogueTopic.Rest, "An evening with someone to talk at. That's all I want and it's not much to ask") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "taciturn",
            DisplayName = "Taciturn",
            ModiMentis  = new[] { "stoneface", "introspection" },
            Appearance  = "says nothing at all, and does not seem to find the silence uncomfortable",
            Persona     = "You use as few words as will do. Long answers feel like showing off to you. When you have nothing to add you simply stop.",
            Opinions    = new[] { (DialogueTopic.Rest, "Quiet. That's the whole answer") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "suspicious",
            DisplayName = "Suspicious",
            ModiMentis  = new[] { "vigilance", "scrutiny" },
            Appearance  = "watches your hands rather than your face",
            Persona     = "You assume you are about to be cheated, and you are right often enough that you have never given the habit up. You ask a second question where one would do.",
            Opinions    = new[]
            {
                (DialogueTopic.Trade, "Everyone's fair until the moment it costs them something. Then you find out"),
                (DialogueTopic.Roads, "Strangers on the road. I want to know where they're going before they've asked me anything"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "open_hearted",
            DisplayName = "Open-Hearted",
            ModiMentis  = new[] { "open_mindedness", "hospitality" },
            Appearance  = "greets you like someone they have already decided to like",
            Persona     = "You take people at their word until they give you a reason not to. You have been burned by it and have not learned a thing.",
            Opinions    = new[] { (DialogueTopic.Roads, "A stranger's just a neighbour you haven't fed yet") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "gloomy",
            DisplayName = "Gloomy",
            ModiMentis  = new[] { "elegy", "weeping" },
            Organs      = new[] { ("hepar", 1) },
            Appearance  = "has a settled, downward set to the face that no amount of good news shifts",
            Persona     = "You expect things to go badly and you say so out loud. You are not unkind about it — you simply do not see the point of pretending.",
            Opinions    = new[]
            {
                (DialogueTopic.Harvest, "It'll be a poor one. It usually is, and when it isn't, that's next year's trouble stored up"),
                (DialogueTopic.Weather, "It'll turn. It always turns, and never the way you'd want"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "irrepressible",
            DisplayName = "Irrepressible",
            ModiMentis  = new[] { "sense_of_humor", "carousal" },
            Appearance  = "grinning at something before anyone has said anything funny",
            Persona     = "You find most things funny, including things you should not. You make jokes at bad moments and you are usually forgiven for it.",
            Opinions    = new[] { (DialogueTopic.Rest, "If you can't laugh at the end of a day like that, what was the day for?") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "proud",
            DisplayName = "Proud",
            ModiMentis  = new[] { "bearing", "grudgekeeping" },
            Appearance  = "stands very straight, and dressed with more care than the work warrants",
            Persona     = "You have a high sense of your own worth and you take slights seriously. You would rather go without than be pitied.",
            Opinions    = new[] { (DialogueTopic.Work, "I do it properly or I don't do it. There's no third way and I'll not be talked into one") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "self_effacing",
            DisplayName = "Self-Effacing",
            ModiMentis  = new[] { "obedience", "discipline" },
            Appearance  = "stands slightly back, as though someone more important is about to arrive",
            Persona     = "You deflect praise and assume you are in the way. You are competent and would be astonished to hear anyone say so.",
            Opinions    = new[] { (DialogueTopic.Work, "I do my bit. There's a dozen here do more and get said less about") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "nervous",
            DisplayName = "Nervous",
            ModiMentis  = new[] { "vigilance", "clenched_grit" },
            Organs      = new[] { ("cerebellum", -1) },
            Appearance  = "hands never still, and glances toward the door when anyone moves",
            Persona     = "You are anxious most of the time. You expect to be blamed for things, so you explain yourself before you are asked to.",
            Opinions    = new[] { (DialogueTopic.Omens, "I read too much into things. I know I do. It doesn't stop me") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "unflappable",
            DisplayName = "Unflappable",
            ModiMentis  = new[] { "iron_nerves", "meditation" },
            Organs      = new[] { ("heart", 1) },
            Items       = new Func<Item>[] { () => new WalkingStaff() },
            Appearance  = "entirely at ease, in a way that makes the people around them calmer too",
            Persona     = "Nothing rattles you. In a crisis you get slower and more precise rather than faster, and people instinctively look to you.",
            Opinions    = new[] { (DialogueTopic.Weather, "It comes, it goes, we're still here. Panicking never once shortened a storm") },
        });
    }
}
