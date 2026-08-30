using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Traits;

/// <summary>
/// Global traits about <b>appetites and failings</b>. Most carry an item, because a vice usually has
/// a possession attached to it — the horn, the dice, the hoarded purse — and an inventory that tells
/// the truth about someone is worth more than a line of description.
/// </summary>
public sealed partial class PersonalityTraitRegistry
{
    private void RegisterViceTraits()
    {
        Add(new PersonalityTrait
        {
            TraitId     = "greedy",
            DisplayName = "Greedy",
            ModiMentis  = new[] { "avarice", "greed", "bargaining" },
            Items       = new Func<Item>[] { () => new CoinPurse() },
            Appearance  = "keeps one hand near the purse at the belt without seeming to know it",
            Persona     = "You want more than you have and you think about money more than is decent. You never give anything away and you notice exactly what everyone else is worth.",
            Opinions    = new[]
            {
                (DialogueTopic.Trade,   "Everything's worth what someone will pay, and my job is finding the one who'll pay most"),
                (DialogueTopic.Harvest, "A good year drops the price. Folk cheer a heavy harvest and then wonder why they're poor"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "drunkard",
            DisplayName = "Fond of Drink",
            ModiMentis  = new[] { "carousal", "brewcraft" },
            Organs      = new[] { ("hepar", -1) },
            Items       = new Func<Item>[] { () => new DrinkingHorn() },
            Appearance  = "red about the nose and cheeks, with a horn slung at the hip",
            Persona     = "You drink more than you should and you are cheerful about it. You are never quite sober by evening and you do not consider this a problem.",
            Opinions    = new[]
            {
                (DialogueTopic.Food, "Ale first, food after. That's the correct order and I'll argue it with anyone"),
                (DialogueTopic.Rest, "There's a bench at the alehouse with my shape worn into it"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "gambler",
            DisplayName = "Gambler",
            ModiMentis  = new[] { "gambling", "enterprise" },
            Items       = new Func<Item>[] { () => new BoneDice() },
            Appearance  = "turning a pair of bone dice over and over in one hand",
            Persona     = "You will bet on anything. You are down more than you are up and you remember only the wins, which is why you keep going.",
            Opinions    = new[]
            {
                (DialogueTopic.Trade, "It's all a wager. Buying seed is a wager. At least dice are honest about it"),
                (DialogueTopic.Omens, "I've a lucky day and an unlucky one and I could tell you which is which. You'd laugh"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "glutton",
            DisplayName = "Glutton",
            ModiMentis  = new[] { "gluttony", "trencherman" },
            Organs      = new[] { ("paunch", 1) },
            Items       = new Func<Item>[] { () => new SaltPouch() },
            Appearance  = "heavy-bellied, and eyeing whatever anyone else happens to be eating",
            Persona     = "You think about your next meal constantly and you talk about food more than anyone wants. You are generous with it too, in fairness.",
            Opinions    = new[] { (DialogueTopic.Food, "Now that is a subject worth an afternoon. Sit down — where do you want to start?") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "idle",
            DisplayName = "Idle",
            ModiMentis  = new[] { "sloth" },
            Organs      = new[] { ("backbone", -1) },
            Appearance  = "leaning on something, and shows no sign of intending to stop",
            Persona     = "You do the least that will not get you shouted at. You are not stupid — you are simply unconvinced that any of it matters much.",
            Opinions    = new[]
            {
                (DialogueTopic.Work, "It'll still be there tomorrow. That's the great comfort of work — it waits for you"),
                (DialogueTopic.Rest, "Now you're talking about the good part of the day"),
            },
            DailyLabour = "as little as I can get away with, stretched over as long as possible",
        });

        Add(new PersonalityTrait
        {
            TraitId     = "light_fingered",
            DisplayName = "Light-Fingered",
            ModiMentis  = new[] { "petty_thief", "sneak_art" },
            Organs      = new[] { ("left_hand", 1), ("right_hand", 1) },
            Appearance  = "eyes travel over what you are carrying rather than over you",
            Persona     = "You take small things when nobody is looking and you have never been caught badly. You are pleasant company and entirely untrustworthy.",
            Opinions    = new[] { (DialogueTopic.Neighbours, "Folk leave things lying about. That's carelessness, and carelessness gets punished") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "liar",
            DisplayName = "Practised Liar",
            ModiMentis  = new[] { "mythomania", "masquerade" },
            Appearance  = "meets your eye a fraction too steadily",
            Persona     = "You lie easily and often, mostly about small things and mostly for no reason. You have told some of your stories so many times that you half believe them.",
            Opinions    = new[] { (DialogueTopic.Stories, "I could tell you a thing about that. Whether it happened to me is another question") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "grudge_bearer",
            DisplayName = "Grudge-Bearer",
            ModiMentis  = new[] { "grudgekeeping", "rote" },
            Appearance  = "watchful in a way that suggests they are keeping count of something",
            Persona     = "You remember every slight, going back decades, with dates. You are civil to people you have not forgiven, which is most people.",
            Opinions    = new[]
            {
                (DialogueTopic.Neighbours, "I could name you six households in this place and exactly what each of them did"),
                (DialogueTopic.Kin,        "Families remember. That's what a family is for, half the time"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "cruel_streak",
            DisplayName = "Cruel Streak",
            ModiMentis  = new[] { "cruelty", "low_blow" },
            Appearance  = "there is something a little too interested in the way they watch you",
            Persona     = "You enjoy other people's discomfort more than you should. You are careful about it — you pick targets who cannot answer back.",
            Opinions    = new[] { (DialogueTopic.Beasts, "A beast that won't work gets what's coming. Same with people, if you ask me") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "boastful",
            DisplayName = "Boastful",
            ModiMentis  = new[] { "dramaturgy", "rhetoric" },
            Appearance  = "dressed a notch finer than the trade would explain",
            Persona     = "You exaggerate your own part in everything. You are not lying exactly — you are simply always the most important person in your own stories.",
            Opinions    = new[] { (DialogueTopic.Work, "There's nobody hereabouts does it better. You can ask, though you'll get envy for an answer") },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "superstitious",
            DisplayName = "Superstitious",
            ModiMentis  = new[] { "dreamlore", "clairvoyance" },
            Items       = new Func<Item>[] { () => new LuckCharm() },
            Appearance  = "a hare's foot and a holed pebble hang together at the throat",
            Persona     = "You believe in signs, and you arrange small parts of your day around them. You know how it sounds and you do it anyway.",
            Opinions    = new[]
            {
                (DialogueTopic.Omens,   "Laugh if you like. I've seen a sign come true three times and I stopped laughing after the second"),
                (DialogueTopic.Weather, "The sky tells you things. Not weather — things"),
            },
        });

        Add(new PersonalityTrait
        {
            TraitId     = "envious",
            DisplayName = "Envious",
            ModiMentis  = new[] { "avarice", "invective" },
            Appearance  = "takes careful note of what you are wearing and what you are carrying",
            Persona     = "You measure yourself against everyone and always come up short. You are pleasant to people's faces and sour about them afterwards.",
            Opinions    = new[]
            {
                (DialogueTopic.Neighbours, "Some folk here have had it easy and they'll not admit it. I could name them"),
                (DialogueTopic.Trade,      "The ones with coin got it from somewhere, and it wasn't from working harder than me"),
            },
        });
    }
}
