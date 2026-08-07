using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Traits;

/// <summary>Reserved traits for the trades that work outside the village: woodcutter, charcoal burner, fisherman, miner.</summary>
public sealed partial class PersonalityTraitRegistry
{
    private void RegisterWildernessTraits()
    {
        // ── Woodcutter ─────────────────────────────────────────────────────────
        Add("woodcutter",
            new PersonalityTrait
            {
                TraitId     = "woodcutter_crushed_by_a_widowmaker",
                DisplayName = "Caught by a Falling Limb",
                Wounds      = new Func<Wound>[] { () => new ShoulderDislocationRightWound() },
                ModiMentis  = new[] { "vigilance", "clenched_grit" },
                Appearance  = "the right shoulder sits low and the arm is used carefully below the height of the head",
                Persona     = "A dead limb came down on you and the shoulder never set right. You look up constantly now, and you make everyone around you do the same.",
                Opinions    = new[]
                {
                    (DialogueTopic.Work,  "Look up. That's the whole of the trade's wisdom and I learned it the slow way"),
                    (DialogueTopic.Wilds, "It's not the tree you're cutting that kills you. It's the dead one next to it") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "woodcutter_talks_to_trees",
                DisplayName = "Asks Before Felling",
                ModiMentis  = new[] { "iconography", "dreamlore", "woodcraft" },
                Appearance  = "puts a hand flat against a trunk and pauses before doing anything else",
                Persona     = "You say a few words to a tree before you fell it. You would not call it a prayer and you would not stop doing it either.",
                Opinions    = new[]
                {
                    (DialogueTopic.Wilds, "I say a word to it first. No, I'll not explain that, and no, I'll not stop"),
                    (DialogueTopic.Omens, "There's trees I've walked away from. Couldn't tell you why. Still won't touch them"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "woodcutter_poacher",
                DisplayName = "Poaches on the Side",
                ModiMentis  = new[] { "hunt", "stalking", "foul_play" },
                Items       = new Func<Item>[] { () => new Rope(), () => new DriedMeat() },
                Appearance  = "carrying rather more meat than a woodcutter's wage accounts for",
                Persona     = "You take deer that are not yours and you are very good at it. You are alert to anyone asking questions about the wood.",
                Opinions    = new[]
                {
                    (DialogueTopic.Wilds,  "The wood feeds whoever knows it. That's how I see it and the lord sees it differently"),
                    (DialogueTopic.Food,   "There's meat on my table more weeks than there ought to be. I'll leave it there") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "woodcutter_axe_from_his_father",
                DisplayName = "His Father's Axe",
                ModiMentis  = new[] { "lineage_lore", "woodcraft", "steady_hand" },
                Items       = new Func<Item>[] { () => new Hatchet(), () => new Whetstone() },
                Appearance  = "an axe far older than they are, with a head reground almost thin",
                Persona     = "You work with your father's axe and you have replaced the haft four times. It matters to you more than you would say out loud.",
                Opinions    = new[] { (DialogueTopic.Kin, "Four hafts and one head. It was his and his father's. Folk say that makes it a different axe. It doesn't") },
            },
            new PersonalityTrait
            {
                TraitId     = "woodcutter_afraid_of_the_deep_wood",
                DisplayName = "Will Not Go Past the Ridge",
                ModiMentis  = new[] { "vigilance", "survivalism", "gut_feeling" },
                Appearance  = "goes still and listens when the conversation turns to the deep wood",
                Persona     = "Something happened to you out beyond the ridge and you will not work past it any more. You lose money over this and you will not be argued round.",
                Opinions    = new[]
                {
                    (DialogueTopic.Wilds, "I don't go past the ridge. There's good timber out there and I don't go past the ridge"),
                    (DialogueTopic.Omens, "I'll not say what. I'll say I was wrong to think it was nothing at the time") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "woodcutter_strongest_man_here",
                DisplayName = "Strongest Man in the District",
                ModiMentis  = new[] { "brute_force", "haulage", "bearing" },
                Organs      = new[] { ("left_arm", 1), ("right_arm", 1), ("backbone", 1) },
                Appearance  = "built on a scale that makes the surroundings look temporary",
                Persona     = "You are enormous and everyone knows it. You are gentle in the way that very strong people learn to be, and you enjoy the reputation.",
                Opinions    = new[] { (DialogueTopic.Work, "I carry what two carry. It's not cleverness. It's the one thing I was given and I've made a living of it") },
            });

        // ── Charcoal burner ────────────────────────────────────────────────────
        Add("charcoal_burner",
            new PersonalityTrait
            {
                TraitId     = "charcoal_lost_a_clamp",
                DisplayName = "Lost a Clamp to the Wind",
                ModiMentis  = new[] { "firecraft", "vigilance", "weeping" },
                Appearance  = "a burnt-through patch on one sleeve, and something defeated about the shoulders",
                Persona     = "A week's burn went up in a night and you watched it. You are ruinously behind and you check the mound obsessively.",
                Opinions    = new[]
                {
                    (DialogueTopic.Work,    "A week awake, and the wind found a hole in the turf, and it was gone by morning. All of it"),
                    (DialogueTopic.Weather, "Wind. Everything else I can work in. Wind and I'm out there all night with a shovel"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "charcoal_black_lung",
                DisplayName = "Black in the Chest",
                ModiMentis  = new[] { "endurance" },
                Organs      = new[] { ("pulmones", -1) },
                Appearance  = "coughs black into a rag and folds it away without comment",
                Persona     = "The smoke has ruined your chest. You cough badly and you know exactly what it means, and you keep going because the clamp does not wait.",
                Opinions    = new[] { (DialogueTopic.Health, "It's in my chest and it's not coming out. I've seen how it finishes. I'd sooner not discuss it") },
            },
            new PersonalityTrait
            {
                TraitId          = "charcoal_hermit_by_habit",
                DisplayName      = "Alone Too Long",
                ModiMentis       = new[] { "introspection", "meditation", "survivalism" },
                Appearance       = "talks a great deal, then stops abruptly as though catching themselves",
                Persona          = "You go a week or more without seeing anyone. When you do meet someone you talk far too much, then notice and go silent.",
                SelfIntroduction = "the one who burns the coal — sorry, I've not spoken to a soul in nine days, I'll slow down",
                Opinions         = new[] { (DialogueTopic.Neighbours, "Nine days since I saw a face. Then I meet one and can't stop talking. Stop me when you've had enough") },
            },
            new PersonalityTrait
            {
                TraitId     = "charcoal_night_watcher",
                DisplayName = "Sees Things in the Wood at Night",
                ModiMentis  = new[] { "clairvoyance", "dreamlore", "vigilance" },
                Items       = new Func<Item>[] { () => new LuckCharm() },
                Appearance  = "eyes red-rimmed from smoke and sleeplessness, and watchful past your shoulder",
                Persona     = "Alone by the clamp at night you have seen things you cannot account for. You are not certain whether it was the smoke, and you have stopped needing to be.",
                Opinions    = new[]
                {
                    (DialogueTopic.Omens, "I've seen smoke lean against the wind. Twice. I don't know what it means and I don't like it"),
                    (DialogueTopic.Wilds, "There's things move out there at night. Mostly deer. Mostly"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "charcoal_smith_creditor",
                DisplayName = "The Smith Owes Him",
                ModiMentis  = new[] { "tallycraft", "bargaining", "grudgekeeping" },
                Items       = new Func<Item>[] { () => new TallyStick() },
                Appearance  = "produces a notched stick the moment money is mentioned",
                Persona     = "The blacksmith owes you a season's coal and cannot pay. You need the money and you also need the smith to stay in business, which is an awkward place to stand.",
                Opinions    = new[] { (DialogueTopic.Trade, "The smith owes me a season. I can't press him — if the forge shuts I've no customer at all. So we both pretend") },
            },
            new PersonalityTrait
            {
                TraitId     = "charcoal_reads_the_smoke",
                DisplayName = "Reads the Smoke",
                ModiMentis  = new[] { "firecraft", "ripelore", "thermodynamics" },
                Organs      = new[] { ("nose", 1) },
                Appearance  = "sniffs the air twice and appears to learn something from it",
                Persona     = "You can tell the state of a burn from the colour and smell of the smoke at fifty paces. It is a real and unteachable skill and you are proud of it.",
                Opinions    = new[] { (DialogueTopic.Work, "I can smell what a clamp is doing from the far side of the clearing. Forty years buys you that and not much else") },
            });

        // ── Fisherman ──────────────────────────────────────────────────────────
        Add("fisherman",
            new PersonalityTrait
            {
                TraitId     = "fisherman_drowned_crew",
                DisplayName = "The Only One Who Came Back",
                ModiMentis  = new[] { "natation", "iron_nerves", "elegy" },
                Appearance  = "looks at the water rather than at you, whenever it is in sight",
                Persona     = "You went out with others and came back alone. You still fish the same water and you have never explained the day to anyone.",
                Opinions    = new[]
                {
                    (DialogueTopic.Water, "It took the others and left me. I go out on it still. What else would I do?"),
                    (DialogueTopic.Kin,   "You say the important things before you go out. I didn't, that time") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "fisherman_net_mender",
                DisplayName = "Mends for the Whole Shore",
                ModiMentis  = new[] { "knotwork", "threadwork", "patience" },
                Items       = new Func<Item>[] { () => new FishingNet(), () => new MendingKit() },
                Appearance  = "fingers working at a knot even while talking about something else",
                Persona     = "You mend nets faster and better than anyone and you do it for others as a matter of course. Your hands never stop.",
                Opinions    = new[] { (DialogueTopic.Work, "I mend for half this shore. It's not generosity — a bad net near mine catches nothing and we all eat less") },
            },
            new PersonalityTrait
            {
                TraitId     = "fisherman_knows_the_weather",
                DisplayName = "Never Been Caught Out",
                ModiMentis  = new[] { "wayfaring", "voyage", "gut_feeling" },
                Appearance  = "checks the sky the way other people check a purse",
                Persona     = "You have never once been surprised by weather on the water and you are quietly, insufferably proud of it. Others wait to see whether you launch.",
                Opinions    = new[]
                {
                    (DialogueTopic.Weather, "I've never once been caught out. Not once in thirty years. Half this shore won't launch till they've seen whether I do"),
                    (DialogueTopic.Omens,   "It's not a gift. It's looking at the same sky every day since I was six") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "fisherman_taboo_keeper",
                DisplayName = "Keeps the Old Prohibitions",
                ModiMentis  = new[] { "dreamlore", "fables_and_tales", "nautical_jargon" },
                Items       = new Func<Item>[] { () => new LuckCharm() },
                Appearance  = "a charm knotted into the belt, and a wariness about certain words",
                Persona     = "There are words you will not say and things you will not bring on the water. You are entirely serious about this and will leave a man ashore over it.",
                Opinions    = new[]
                {
                    (DialogueTopic.Omens, "There's words I'll not say aboard. Call it foolish. I've buried men who called it foolish"),
                    (DialogueTopic.Water, "You show it respect or it teaches you to. Those are the two ways it goes"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "fisherman_salt_hands",
                DisplayName = "Hands Cracked to the Bone",
                Wounds      = new Func<Wound>[] { () => new BrokenHandLeftWound() },
                ModiMentis  = new[] { "clenched_grit", "herblore" },
                Appearance  = "hands split open in a dozen places, greased and bound with rag",
                Persona     = "Salt and cold have opened your hands up permanently. They bleed most days and you bind them and go out anyway.",
                Opinions    = new[] { (DialogueTopic.Health, "Salt gets in every crack and keeps it open. I grease them, I bind them, I go out. That's the whole treatment") },
            },
            new PersonalityTrait
            {
                TraitId     = "fisherman_boat_is_his_house",
                DisplayName = "Lives Aboard",
                ModiMentis  = new[] { "survivalism", "knotwork", "stewardry" },
                Items       = new Func<Item>[] { () => new LeatherCanteen(), () => new WoodenBowl() },
                Appearance  = "everything they own is on them, stowed with obsessive neatness",
                Persona     = "You have no house — you sleep aboard, in all weathers. Everything you own is stowed and lashed and you cannot bear disorder.",
                Opinions    = new[]
                {
                    (DialogueTopic.Rest, "I sleep aboard. Rocking. I can't sleep on ground that stays still any more, and that's the truth"),
                    (DialogueTopic.Kin,  "No house, no household. The boat's both and it asks less") ,
                },
            });

        // ── Miner ──────────────────────────────────────────────────────────────
        Add("miner",
            new PersonalityTrait
            {
                TraitId     = "miner_buried_once",
                DisplayName = "Dug Out Alive",
                Wounds      = new Func<Wound>[] { () => new BrokenRibsWound() },
                ModiMentis  = new[] { "iron_nerves", "clenched_grit", "survivalism" },
                Appearance  = "breathes shallowly, and does not stand under anything overhanging",
                Persona     = "A fall buried you for most of a day and they dug you out. Your ribs never set right and neither did your nerve, though you still go down.",
                Opinions    = new[]
                {
                    (DialogueTopic.Work,   "I was under it the best part of a day. They got me out. I went back down the following week and I'd not recommend it"),
                    (DialogueTopic.Health, "My ribs never set right. I breathe shallow. You get used to breathing shallow"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "miner_never_whistles",
                DisplayName = "Keeps the Shaft's Rules",
                ModiMentis  = new[] { "dreamlore", "discipline", "vigilance" },
                Items       = new Func<Item>[] { () => new LuckCharm() },
                Appearance  = "a holed pebble on a thong, and a visible discomfort at certain sounds",
                Persona     = "You keep every superstition of the shaft absolutely — no whistling, no sitting on the ore-pile — and you will walk out on anyone who breaks them.",
                Opinions    = new[]
                {
                    (DialogueTopic.Omens, "You don't whistle down there and you don't sit on the pile. I've buried men who laughed at both"),
                    (DialogueTopic.Wilds, "Underground listens back. That's not a story. Go down and stand still and you'll know what I mean"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "miner_found_something_good",
                DisplayName = "Struck a Rich Seam",
                ModiMentis  = new[] { "treasure_hunting", "masquerade", "enterprise" },
                Items       = new Func<Item>[] { () => new CoinPurse() },
                Appearance  = "dressed no better than anyone, and rather too careful about seeming so",
                Persona     = "You have hit something genuinely valuable and you are hiding it. You are terrified of being followed and of being asked directly.",
                Opinions    = new[]
                {
                    (DialogueTopic.Trade, "It's a poor seam. Barely worth the working. I'd not go up there if I were you"),
                    (DialogueTopic.Roads, "Strangers asking where I work. That happens more than I'd like just now") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "miner_night_eyes",
                DisplayName = "Sees in the Dark",
                ModiMentis  = new[] { "scrutiny", "stealth", "topographia" },
                Organs      = new[] { ("left_eye", 1), ("right_eye", 1) },
                Items       = new Func<Item>[] { () => new MinersLamp() },
                Appearance  = "squints painfully in daylight and is entirely at ease in the dark",
                Persona     = "Years underground have made daylight actively unpleasant to you. You work by touch and by a lamp turned low and you find the surface too bright and too loud.",
                Opinions    = new[] { (DialogueTopic.Weather, "Sun's the worst of it. I come up and I can't see for an hour. Give me a wet grey day") },
            },
            new PersonalityTrait
            {
                TraitId     = "miner_lost_his_partner",
                DisplayName = "Works Alone Now",
                ModiMentis  = new[] { "elegy", "endurance", "stoneface" },
                Appearance  = "carries gear for two and uses one set",
                Persona     = "You worked the seam with someone for twenty years and they died in it. You still carry both sets of tools and you will not take on anyone new.",
                Opinions    = new[]
                {
                    (DialogueTopic.Kin,  "Twenty years the two of us on that seam. I still take both picks down. I know how that looks"),
                    (DialogueTopic.Work, "Alone now. I'll not take another partner, so don't offer") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "miner_hears_the_rock",
                DisplayName = "Listens to the Rock",
                ModiMentis  = new[] { "keen_ear", "gut_feeling", "stonework" },
                Organs      = new[] { ("left_ear", 1), ("right_ear", 1) },
                Appearance  = "goes quiet mid-sentence and tilts the head as though checking something",
                Persona     = "You can hear a shaft about to go. It has saved you twice and saved others more. Nobody can explain it, least of all you.",
                Opinions    = new[]
                {
                    (DialogueTopic.Work,  "It talks before it goes. Little sounds. I've walked men out of a shaft on nothing but that and been right both times"),
                    (DialogueTopic.Omens, "Not a gift. Ears and thirty years. Though I'll not swear to that at two in the morning") ,
                },
            });
    }
}
