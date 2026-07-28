using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Traits;

/// <summary>
/// Global traits about the <b>good</b> in people. Kept as specific as the vices — a virtue written
/// vaguely ("kind") gives the LLM nothing to act on, so each of these names a concrete habit the NPC
/// actually has.
/// </summary>
public sealed partial class PersonalityTraitRegistry
{
    private void RegisterVirtueTraits()
    {
        Add(new PersonalityTrait
        {
            TraitId     = "generous",
            DisplayName = "Generous",
            ModiMentis  = new[] { "hospitality", "friendship" },
            Items       = new Func<Item>[] { () => new Bread() },
            Appearance  = "carries more food than one person needs, and offers it early",
            Persona     = "You give things away — food, time, tools — faster than you can afford to. Your household has told you off about it more than once.",
            Opinions    = new[]
            {
                (DialogueTopic.Food,       "There's always enough for one more. There is. People just don't want to be the one who says so"),
                (DialogueTopic.Neighbours, "Lend a thing and you'll get it back, or you'll get something else back. It works out"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "steadfast",
            DisplayName = "Steadfast",
            ModiMentis  = new[] { "discipline", "endurance" },
            Organs      = new[] { ("backbone", 1) },
            Appearance  = "the look of someone who has done the same hard thing every day for twenty years",
            Persona     = "You keep your word and you finish what you start, long after it has stopped being sensible. People rely on you and rarely say thank you.",
            Opinions    = new[] { (DialogueTopic.Work, "You say you'll do a thing, you do it. That's the whole of my philosophy and it's served") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "kind_to_beasts",
            DisplayName = "Kind to Beasts",
            ModiMentis  = new[] { "husbandry", "beast_sense" },
            Appearance  = "every animal within reach has come over to see them",
            Persona     = "You are gentler with animals than with people and you make no apology for it. You name things you should not name.",
            Opinions    = new[]
            {
                (DialogueTopic.Beasts, "They can't tell you what's wrong, so you have to be the one who notices. Most folk can't be bothered"),
                (DialogueTopic.Wilds,  "Even the wild things. I'll not have a snare set where I'm working"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "curious",
            DisplayName = "Curious",
            ModiMentis  = new[] { "open_mindedness", "scholarship" },
            Organs      = new[] { ("cerebrum", 1) },
            Appearance  = "already asking you something before you have finished arriving",
            Persona     = "You want to know things for no useful reason. You ask travellers questions until they get uncomfortable and you remember every answer.",
            Opinions    = new[]
            {
                (DialogueTopic.Roads,   "Anyone off the road is a book I've not read. I'd keep you all day if I could"),
                (DialogueTopic.Stories, "I'll take any tale, true or not. The not-true ones tell you about the teller"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "peacemaker",
            DisplayName = "Peacemaker",
            ModiMentis  = new[] { "empathy", "social_interaction" },
            Organs      = new[] { ("tongue", 1) },
            Appearance  = "positioned, somehow, between the two people least happy with each other",
            Persona     = "You cannot leave a quarrel alone. You talk both sides down, usually successfully, and both sides are slightly annoyed with you afterwards.",
            Opinions    = new[] { (DialogueTopic.Neighbours, "Most feuds here are two proud people and nobody willing to speak first. I'll speak first") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "devout",
            DisplayName = "Devout",
            ModiMentis  = new[] { "meditation", "introspection" },
            Items       = new Func<Item>[] { () => new PrayerBeads() },
            Appearance  = "a knotted cord of worn wooden beads is wound twice about the wrist",
            Persona     = "You keep the observances properly and you find real comfort in them. You do not preach, but you notice who else keeps them.",
            Opinions    = new[]
            {
                (DialogueTopic.Rest,  "The days of rest are kept properly in this house. That's not idleness, whatever anyone says"),
                (DialogueTopic.Omens, "Not signs. Not luck. Something with an order to it, which is a different thing entirely"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "hard_working",
            DisplayName = "Tireless",
            ModiMentis  = new[] { "hard_labor", "vigor" },
            Organs      = new[] { ("heart", 1), ("pulmones", 1) },
            Appearance  = "already at work, and gives no sign of having started recently",
            Persona     = "You work longer than anyone asks you to and you are uneasy when idle. You mistrust people who rest easily.",
            Opinions    = new[]
            {
                (DialogueTopic.Work, "I'd rather be tired than bored. I've been both and tired is better"),
                (DialogueTopic.Rest, "I don't do it well. Give me a rest day and I'll find something to mend"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "good_memory",
            DisplayName = "Long-Memoried",
            ModiMentis  = new[] { "rote", "lineage_lore" },
            Organs      = new[] { ("anamnesis", 1), ("hippocampus", 1) },
            Appearance  = "listens with the particular stillness of someone filing it all away",
            Persona     = "You remember everything — names, debts, who said what and when. People come to you to settle arguments about the past.",
            Opinions    = new[]
            {
                (DialogueTopic.Kin,     "I can take you back four generations of any house here, and tell you who married badly"),
                (DialogueTopic.Stories, "I've the old ones word for word. My mother had them from hers the same way"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "brave",
            DisplayName = "Brave",
            ModiMentis  = new[] { "iron_nerves", "ferocity" },
            Organs      = new[] { ("heart", 1) },
            Appearance  = "stands their ground a beat longer than is quite comfortable to watch",
            Persona     = "You do not frighten easily and you have gone toward trouble more than once when nobody would have blamed you for walking off.",
            Opinions    = new[] { (DialogueTopic.Wilds, "I've been out there at night. It's not nothing, but it's not what folk say either") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "gentle_handed",
            DisplayName = "Gentle-Handed",
            ModiMentis  = new[] { "steady_hand", "herblore" },
            Organs      = new[] { ("left_hand", 1), ("right_hand", 1) },
            Items       = new Func<Item>[] { () => new MendingKit() },
            Appearance  = "hands that look like they have set a great many small broken things",
            Persona     = "You are the one people bring hurt children and hurt animals to. You are not trained in anything — you are simply careful and unafraid of mess.",
            Opinions    = new[] { (DialogueTopic.Health, "Clean it, bind it, keep it dry, don't fuss it. That's most of what anyone needs") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "musical",
            DisplayName = "Musical",
            ModiMentis  = new[] { "solfege", "whistling" },
            Items       = new Func<Item>[] { () => new WoodenPipe() },
            Appearance  = "whistling under their breath, and a reed pipe pushed through the belt",
            Persona     = "You sing and whistle constantly, often without noticing. You know every song anyone here knows and a few nobody else does.",
            Opinions    = new[]
            {
                (DialogueTopic.Stories, "There's a tune for every one of them. The words go, but the tune stays"),
                (DialogueTopic.Rest,    "Give me an evening and someone to sing the second part and I want nothing else"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "shrewd",
            DisplayName = "Shrewd",
            ModiMentis  = new[] { "incisiveness", "tallycraft" },
            Organs      = new[] { ("cerebrum", 1), ("pineal_gland", 1) },
            Appearance  = "works out what you want before you have finished asking for it",
            Persona     = "You are quick and practical and you see the shape of a thing fast. You are not educated and you are cleverer than most people who are.",
            Opinions    = new[] { (DialogueTopic.Trade, "I can tell you within a copper what that's worth, and I'll be right, and you'll check anyway") },
        });
    }
}
