using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Traits;

/// <summary>
/// Reserved traits for the three who live outside the village entirely: druid, hermit, savage.
/// These pools lean harder on persona than the trades do — what makes one hermit different from
/// another is almost entirely what they believe.
/// </summary>
public sealed partial class PersonalityTraitRegistry
{
    private void RegisterSolitaryTraits()
    {
        // ── Druid ──────────────────────────────────────────────────────────────
        Add("druid",
            new PersonalityTrait
            {
                TraitId     = "druid_healer_of_last_resort",
                DisplayName = "Sent For When It Is Bad",
                ModiMentis  = new[] { "herblore", "steady_hand", "empathy" },
                Items       = new Func<Item>[] { () => new Herb(), () => new Herb() },
                Appearance  = "a satchel of dried plants that has plainly been opened in a hurry many times",
                Persona     = "The village fears you and fetches you anyway when a birth or a wound goes wrong. You come every time and you never mention the contradiction.",
                Opinions    = new[]
                {
                    (DialogueTopic.Health,     "They come up the path at midnight and they are always sorry and always frightened. I go. I always go"),
                    (DialogueTopic.Neighbours, "They will not sit with me at a feast and they will wake me at three for a child that will not come. Both are true"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "druid_hates_the_axe",
                DisplayName = "Will Not Suffer the Axe",
                ModiMentis  = new[] { "invective", "brute_force", "woodcraft" },
                Appearance  = "an anger held very still, that surfaces the moment cutting is mentioned",
                Persona     = "You have gone beyond distrust of woodcutters into open hostility. You have interfered with their work and you would do it again.",
                Opinions    = new[]
                {
                    (DialogueTopic.Wilds, "They take the old ones because the old ones are the biggest. There is no argument they can make to me about it"),
                    (DialogueTopic.Work,  "I have pulled marks off trees so they could not find them again. I would do it tomorrow") ,
                },
            },
            new PersonalityTrait
            {
                TraitId          = "druid_last_of_the_line",
                DisplayName      = "The Last of It",
                ModiMentis       = new[] { "elegy", "lineage_lore", "rote" },
                Appearance       = "carries themselves like the keeper of something that will not outlast them",
                Persona          = "You are the last person who knows what you know and there is nobody to teach. It is the grief underneath everything you say.",
                SelfIntroduction = "the last one who keeps these woods — and I mean the last, in the plainest sense",
                Opinions         = new[]
                {
                    (DialogueTopic.Stories, "When I am gone, these go. I have no one. That is not self-pity, it is simply the position"),
                    (DialogueTopic.Seasons, "I have watched this wood turn sixty times. Nobody will watch it the way I have again"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "druid_poisoner",
                DisplayName = "Knows the Other Half of Herblore",
                ModiMentis  = new[] { "mycology", "cold_blood", "scrutiny" },
                Items       = new Func<Item>[] { () => new Mushroom() },
                Appearance  = "sorting something into two piles, and keeping one of them well separated",
                Persona     = "You know exactly which plants kill and in what quantity. You have used that knowledge at least once and you are entirely at peace about it.",
                Opinions    = new[]
                {
                    (DialogueTopic.Wilds,  "Every plant that heals will kill at the wrong measure. There is no separate kind"),
                    (DialogueTopic.Health, "I can end a thing as easily as mend it. Both are asked of me. Only one is spoken of") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "druid_took_an_apprentice",
                DisplayName = "Has Someone Learning",
                ModiMentis  = new[] { "patience", "rote", "hospitality" },
                Appearance  = "keeps glancing back toward the trees as though expecting somebody",
                Persona     = "A village child comes to you in secret to learn. Their family would forbid it. You are careful, hopeful and quietly delighted.",
                Opinions    = new[]
                {
                    (DialogueTopic.Kin,     "There is one who comes. Not mine, and their people would stop it if they knew. So we do not tell their people"),
                    (DialogueTopic.Stories, "For the first time in thirty years there is somebody to hand them to. You have no idea what that is worth"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "druid_keeps_a_beast",
                DisplayName = "Something Follows Her",
                ModiMentis  = new[] { "beast_sense", "husbandry", "beast_sense" },
                Appearance  = "there is an animal at the edge of sight that has not run off, and does not intend to",
                Persona     = "A wild animal attached itself to you years ago and has never left. You do not call it tame and you do not call it yours.",
                Opinions    = new[]
                {
                    (DialogueTopic.Beasts, "It is not mine and it is not tame. It stays. That is the whole of the arrangement and it suits us both"),
                    (DialogueTopic.Kin,    "I am not alone up here. That is as much as I will say") ,
                },
            });

        // ── Hermit ─────────────────────────────────────────────────────────────
        Add("hermit",
            new PersonalityTrait
            {
                TraitId          = "hermit_fled_a_killing",
                DisplayName      = "Came Up Here After Something",
                ModiMentis       = new[] { "cold_blood", "stoneface", "iron_nerves" },
                Wounds           = new Func<Wound>[] { () => new ScarWound() },
                Appearance       = "old marks on the hands of a kind that were not got from rock or weather",
                Persona          = "You did something down there that you came up here to be away from. You will not be drawn on it, and you watch the path more than the view.",
                SelfIntroduction = "nobody you want to have met. Leave it there",
                Opinions         = new[]
                {
                    (DialogueTopic.Roads,      "Every road goes back. That is exactly what is wrong with them"),
                    (DialogueTopic.Neighbours, "I came up here to be away from people knowing my name. You can see the difficulty"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "hermit_reads_the_stone",
                DisplayName = "Reads the Old Carvings",
                ModiMentis  = new[] { "decipher", "archeology", "scholarship" },
                Organs      = new[] { ("cerebrum", 1), ("anamnesis", 1) },
                Appearance  = "fingertips worn smooth, as though from tracing the same lines over and over",
                Persona     = "There are marks cut into the rock up here older than any village and you have spent years working at them. You have got further than you tell people.",
                Opinions    = new[]
                {
                    (DialogueTopic.Stories, "There are marks in the stone up here older than any tale still told below. Nobody comes to read them but me"),
                    (DialogueTopic.Omens,   "Somebody stood where I stand and cut those. That is not a portent. It is better than a portent") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "hermit_starving",
                DisplayName = "Not Eating Enough",
                ModiMentis  = new[] { "forage_lore", "endurance", "meditation" },
                Organs      = new[] { ("paunch", -1), ("viscera", -1) },
                Appearance  = "gaunt past thinness, with the skin drawn tight over the face",
                Persona     = "You are slowly starving and you have made a virtue of it. You will refuse food offered too directly and accept it if it is left.",
                Opinions    = new[]
                {
                    (DialogueTopic.Food,   "What the rock gives. Less than you would think. Enough, so far — and I have said that for three winters"),
                    (DialogueTopic.Health, "The body wants less than it claims. I have tested that further than most") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "hermit_talks_to_the_dead",
                DisplayName = "Holds Conversations",
                ModiMentis  = new[] { "clairvoyance", "dreamlore", "weeping" },
                Appearance  = "answering somebody, quietly and quite naturally, who is not there",
                Persona     = "You talk with someone who died. You are not mad about it — it is simply the arrangement you have arrived at, and you see no reason to defend it.",
                Opinions    = new[]
                {
                    (DialogueTopic.Kin,   "I still talk with her. You may make of that what you like — you asked and I have answered"),
                    (DialogueTopic.Omens, "The dead are not far off up here. Down there they are. That is the difference in the two places"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "hermit_was_a_rich_man",
                DisplayName = "Gave It All Away",
                ModiMentis  = new[] { "high_society_manners", "philosophy", "introspection" },
                Items       = new Func<Item>[] { () => new NobleUndertunic() },
                Appearance  = "the rags are patched over something that was once very fine cloth indeed",
                Persona     = "You had money and standing and you gave up every part of it deliberately. You do not preach about it and you have never once regretted it out loud.",
                Opinions    = new[]
                {
                    (DialogueTopic.Trade, "I had a great deal. I put it down. No, it did not feel like sacrifice — that is the part nobody believes"),
                    (DialogueTopic.Rest,  "Down there I owned a house I could not sleep in. Up here I sleep on rock. Work that one out") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "hermit_tests_visitors",
                DisplayName = "Sets Them a Test",
                ModiMentis  = new[] { "philosophy", "incisiveness", "patience" },
                Appearance  = "watching you with the specific attention of somebody scoring something",
                Persona     = "You put visitors through a small deliberate test before you decide whether to speak properly. Most fail it and never notice there was one.",
                Opinions    = new[]
                {
                    (DialogueTopic.Roads,      "People come up. Most come up wanting something. I find out which sort quickly"),
                    (DialogueTopic.Neighbours, "You have asked me three questions and listened to two answers. That is better than most manage"),
                },
            });

        // ── Savage ─────────────────────────────────────────────────────────────
        Add("savage",
            new PersonalityTrait
            {
                TraitId          = "savage_was_taken_as_a_child",
                DisplayName      = "Taken Young",
                ModiMentis       = new[] { "linguistic", "survivalism", "introspection" },
                Appearance       = "there is something in the face that does not match the life",
                Persona          = "You were born in a village and taken young. Fragments of that language still surface in you and it disturbs you when they do.",
                SelfIntroduction = "I was — from a place. Like yours. Long ago. I do not think about it",
                Opinions         = new[]
                {
                    (DialogueTopic.Kin,   "Had people. In a house. I remember a door. Nothing else. Do not ask more"),
                    (DialogueTopic.Roads, "Your roads. I walked one once. Small. I was small") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "savage_wolf_scarred",
                DisplayName = "Marked by the Pack",
                Wounds      = new Func<Wound>[] { () => new ScarWound(), () => new FingersAmputeeLeftWound() },
                ModiMentis  = new[] { "brawling", "hunt", "clenched_grit" },
                Appearance  = "torn scarring across the shoulder and two fingers gone from the left hand",
                Persona     = "Wolves had you and you got away. You are not frightened of them now, which everyone else finds the alarming part.",
                Opinions    = new[]
                {
                    (DialogueTopic.Beasts, "Wolf had me. I am here. It is not. That is the story"),
                    (DialogueTopic.Wilds,  "Not afraid of the wood. The wood knows me now") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "savage_trades_at_the_edge",
                DisplayName = "Trades at the Treeline",
                ModiMentis  = new[] { "bargaining", "forage_lore", "enterprise" },
                Items       = new Func<Item>[] { () => new Hide(), () => new Mushroom() },
                Appearance  = "carrying hides and gathered things, bundled as though for someone else",
                Persona     = "You leave hides and forage at the wood's edge and take what is left in return. You have never spoken to the people you trade with.",
                Opinions    = new[]
                {
                    (DialogueTopic.Trade, "I leave. They take. They leave. I take. No talking. Good arrangement"),
                    (DialogueTopic.Food,  "Salt. That is what I want from you people. Only salt") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "savage_mourning",
                DisplayName = "Was Not Always Alone",
                ModiMentis  = new[] { "elegy", "elegy", "endurance" },
                Appearance  = "there are two of everything in the shelter and only one of them is used",
                Persona     = "There was somebody else out here with you and there is not now. You do not have words for it in any language and you do not try.",
                Opinions    = new[]
                {
                    (DialogueTopic.Kin,  "Was two. Now one. That is all"),
                    (DialogueTopic.Rest, "I do not sleep well. Too quiet now") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "savage_watches_the_village",
                DisplayName = "Watches Your Fires",
                ModiMentis  = new[] { "stealth", "stalking", "scrutiny" },
                Organs      = new[] { ("left_eye", 1), ("right_eye", 1) },
                Appearance  = "knows things about you that nothing in this conversation explains",
                Persona     = "You have watched the village from the treeline for years. You know their routines, their names, their quarrels. They have no idea.",
                Opinions    = new[]
                {
                    (DialogueTopic.Neighbours, "I know your people. Names. Who fights. Who steals. They do not know I know"),
                    (DialogueTopic.Rest,       "I watch your fires at night. Warm-looking. I do not come down") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "savage_healer_of_beasts",
                DisplayName = "Mends What He Finds",
                ModiMentis  = new[] { "beast_sense", "herblore", "husbandry" },
                Items       = new Func<Item>[] { () => new Herb() },
                Appearance  = "moves slowly and quietly, in the manner of somebody used to not frightening things",
                Persona     = "You find injured animals and mend them, which sits oddly with everything else about you. You would not describe it as kindness.",
                Opinions    = new[]
                {
                    (DialogueTopic.Beasts, "Find them hurt. Mend them. They go. Not kindness. Just — I do it"),
                    (DialogueTopic.Wilds,  "The wood takes enough. Sometimes I take something back off it") ,
                },
            });
    }
}
