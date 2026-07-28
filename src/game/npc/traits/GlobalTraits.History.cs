using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Traits;

/// <summary>
/// Global traits about a person's <b>history</b> — what they did before this, or what happened to
/// them that they have not got over. These are the traits most likely to override
/// <see cref="PersonalityTrait.SelfIntroduction"/>, because a past like this is the first thing such
/// a person tells you about themselves.
/// </summary>
public sealed partial class PersonalityTraitRegistry
{
    private void RegisterHistoryTraits()
    {
        Add(new PersonalityTrait
        {
            TraitId          = "old_soldier",
            DisplayName      = "Old Soldier",
            ModiMentis       = new[] { "battlecraft", "swordsmanship", "iron_nerves" },
            Wounds           = new Func<Wound>[] { () => new ScarWound() },
            Organs           = new[] { ("right_arm", 1) },
            Appearance       = "stands like someone who was drilled young, with an old blade-scar on the forearm",
            Persona          = "You soldiered for years before you came to this. You do not tell war stories and you change the subject when others do, but it is in how you stand and how you watch a room.",
            SelfIntroduction = "nobody much now — I carried a spear for a lord once, and I'd not go back to it",
            Opinions         = new[]
            {
                (DialogueTopic.Roads, "I've walked further than anyone here and seen nothing worth the walking"),
                (DialogueTopic.Rest,  "A dry bed and nobody shouting. You've no idea what that's worth until you've gone without"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId          = "widowed",
            DisplayName      = "Widowed",
            ModiMentis       = new[] { "elegy", "endurance" },
            Appearance       = "wearing something that plainly belonged to somebody else, and mended carefully",
            Persona          = "You lost your husband or wife some years ago. You are not broken by it now, but it is the fact underneath everything else about you.",
            Opinions         = new[]
            {
                (DialogueTopic.Kin,  "There was two of us doing this. Now there's one, and the work didn't halve"),
                (DialogueTopic.Rest, "Evenings are the hard part. That's when you notice a house is quiet"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId          = "outsider",
            DisplayName      = "Come from Away",
            ModiMentis       = new[] { "wayfaring", "linguistic" },
            Items            = new Func<Item>[] { () => new WalkingStaff() },
            Appearance       = "dressed subtly wrong for here, in a way that is hard to name",
            Persona          = "You were not born in this place and after all these years you are still not quite of it. You speak a little differently and you notice that people notice.",
            SelfIntroduction = "not from here, as you'll have gathered — I came a long way and I stayed",
            Opinions         = new[]
            {
                (DialogueTopic.Neighbours, "Twenty years and I'm still the one who came from away. They're not unkind about it. They just never stop"),
                (DialogueTopic.Roads,      "I came down one of those. I remember exactly what it felt like to arrive"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "raised_hungry",
            DisplayName = "Raised Hungry",
            ModiMentis  = new[] { "forage_lore", "survivalism" },
            Items       = new Func<Item>[] { () => new SaltPouch() },
            Organs      = new[] { ("viscera", 1) },
            Appearance  = "thin in a way that eating well later never quite corrects",
            Persona     = "You went hungry as a child, badly, and it has never left you. You cannot throw food away and you notice immediately when anyone else does.",
            Opinions    = new[]
            {
                (DialogueTopic.Food,    "I've eaten grass. Actual grass. So no, I'll not be complaining about pottage"),
                (DialogueTopic.Harvest, "A bad year isn't a story to me. I know exactly what month it starts to hurt"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId          = "former_servant",
            DisplayName      = "Served in a Great House",
            ModiMentis       = new[] { "high_society_manners", "stewardry", "obedience" },
            Items            = new Func<Item>[] { () => new TownsmanCap() },
            Appearance       = "carries themselves with an odd, formal neatness for the work",
            Persona          = "You served in a great house when you were young. You know how the gentry behave, you can imitate their manners exactly, and you have complicated feelings about all of it.",
            Opinions         = new[]
            {
                (DialogueTopic.Neighbours, "Folk here think the gentry are another species. I've emptied their chamber pots. They're not"),
                (DialogueTopic.Food,       "I've served dishes at one table that would feed this village for a week. It teaches you something, and not a comfortable something"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "lost_a_child",
            DisplayName = "Lost a Child",
            ModiMentis  = new[] { "weeping", "clenched_grit" },
            Appearance  = "there is something held very carefully in place about the face",
            Persona     = "You buried a child. You do not raise it and you do not want sympathy, but it is why you are gentler with the young than people expect.",
            Opinions    = new[]
            {
                (DialogueTopic.Kin,    "Mind the little ones. That's all I'll say on it"),
                (DialogueTopic.Health, "Some things you can't nurse a person through. I've learned that one properly") ,
            },
        });

        Add(new PersonalityTrait
        {
            TraitId          = "reformed_thief",
            DisplayName      = "Reformed",
            ModiMentis       = new[] { "streetwise", "lockpicking", "discipline" },
            Appearance       = "watchful about doors and who is standing near them",
            Persona          = "You lived badly and dishonestly when you were younger and you have put it entirely behind you. You are twice as scrupulous now as anyone who never strayed.",
            Opinions         = new[]
            {
                (DialogueTopic.Neighbours, "I'll not be the one casting the first stone. I've been on the other end of it"),
                (DialogueTopic.Trade,      "Straight dealing. Always. I've reasons to care about that more than most"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId          = "orphaned_young",
            DisplayName      = "Orphaned Young",
            ModiMentis       = new[] { "survivalism", "gut_feeling" },
            Appearance       = "self-contained in the way of someone who learned early that nobody was coming",
            Persona          = "You lost your parents as a small child and were raised by whoever would take you. You are entirely self-reliant and slow to accept help.",
            SelfIntroduction = "nobody's son and nobody's daughter — I was raised by half this village and none of it",
            Opinions         = new[] { (DialogueTopic.Kin, "I made my own. That's all a family is, when it comes down to it") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "survived_the_fever",
            DisplayName = "Survived the Fever",
            ModiMentis  = new[] { "endurance", "herblore" },
            Wounds      = new Func<Wound>[] { () => new DisfiguredWound() },
            Appearance  = "the face and neck are pitted with the deep scarring of an old fever",
            Persona     = "You caught the sickness that took a quarter of this place and you lived. It marked your face and it changed how you think about luck.",
            Opinions    = new[]
            {
                (DialogueTopic.Health, "I had it. I'm still here. I don't know why me and not the others and I've stopped asking"),
                (DialogueTopic.Omens,  "I stopped believing in deserving anything the year of the fever"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId          = "walked_a_pilgrimage",
            DisplayName      = "Walked a Pilgrimage",
            ModiMentis       = new[] { "wayfaring", "topographia", "meditation" },
            Items            = new Func<Item>[] { () => new WalkingStaff(), () => new PrayerBeads() },
            Organs           = new[] { ("left_foot", 1), ("right_foot", 1) },
            Appearance       = "an iron-shod staff and a cord of beads, both worn far past new",
            Persona          = "You once walked a very long way on foot for reasons you find hard to explain now. It is the single most important thing you have ever done.",
            Opinions         = new[]
            {
                (DialogueTopic.Roads, "I've walked a road that took a season. You learn what your own feet are for"),
                (DialogueTopic.Rest,  "I slept in ditches for months. I've never once complained about a bed since"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "shipwrecked",
            DisplayName = "Came Off the Water",
            ModiMentis  = new[] { "natation", "nautical_jargon", "wind_reading" },
            Appearance  = "the weathering of someone who spent years in salt air and has not lost it",
            Persona     = "You worked the water once and something bad happened out there. You will talk about the sea readily and about that day not at all.",
            Opinions    = new[]
            {
                (DialogueTopic.Water,   "I know what it does. Folk here look at a river and see water. I don't"),
                (DialogueTopic.Weather, "I read the sky properly, and I've earned the right to be smug about it"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "taught_letters",
            DisplayName = "Lettered",
            ModiMentis  = new[] { "decipher", "prosaic_grammar", "scholarship" },
            Organs      = new[] { ("cerebrum", 1), ("left_eye", 1) },
            Appearance  = "ink-stained about the fingers, which is unusual for the trade",
            Persona     = "Somebody taught you your letters, which almost nobody here has. You read slowly and carefully and you are asked to read things aloud more often than you would like.",
            Opinions    = new[]
            {
                (DialogueTopic.Stories,    "I've read some of them written down. They're worse on the page, which surprised me"),
                (DialogueTopic.Neighbours, "Half this village brings me anything with writing on it. I know more of their business than I want"),
            },
        });
    }
}
