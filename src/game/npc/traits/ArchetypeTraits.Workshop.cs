using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Traits;

/// <summary>Reserved traits for the village workshop trades.</summary>
public sealed partial class PersonalityTraitRegistry
{
    private void RegisterWorkshopTraits()
    {
        // ── Blacksmith ─────────────────────────────────────────────────────────
        Add("blacksmith",
            new PersonalityTrait
            {
                TraitId     = "smith_burnt_hands",
                DisplayName = "Burnt Hands",
                Wounds      = new Func<Wound>[] { () => new BrokenHandLeftWound() },
                ModiMentis  = new[] { "firecraft", "clenched_grit" },
                Appearance  = "both hands are a map of old burn scars, shiny and hairless across the backs",
                Persona     = "Your hands are ruined with burns and you have long since stopped feeling small ones. You handle hot metal more carelessly than you should.",
                Opinions    = new[] { (DialogueTopic.Work, "You stop noticing the small burns. That's the day the trade has you") },
            },
            new PersonalityTrait
            {
                TraitId     = "smith_swordmaker",
                DisplayName = "Made Blades Once",
                ModiMentis  = new[] { "swordsmanship", "metalcraft", "battlecraft" },
                Items       = new Func<Item>[] { () => new IronDagger(), () => new Whetstone() },
                Appearance  = "a well-kept dagger at the belt, plainly of their own making and better than it needs to be",
                Persona     = "You made weapons for a lord's household once, and hoes and hinges are a comedown you feel every day. You will talk about steel at length.",
                SelfIntroduction = "the smith here — though I made blades for better men than this village holds, once",
                Opinions    = new[] { (DialogueTopic.Work, "Ploughshares. I once put an edge on a thing a man would stake his life on, and now: ploughshares") },
            },
            new PersonalityTrait
            {
                TraitId     = "smith_deaf_from_forge",
                DisplayName = "Forge-Deafened",
                Wounds      = new Func<Wound>[] { () => new PerforatedEardrumLeftWound() },
                Appearance  = "leans in close and still asks you to say it again",
                Persona     = "Thirty years beside the anvil has taken most of your hearing. You shout without meaning to and you miss half of what is said to you.",
                Opinions    = new[] { (DialogueTopic.Health, "The hammer took my ears. Everyone said it would and I couldn't hear them saying it") },
            },
            new PersonalityTrait
            {
                TraitId     = "smith_charcoal_debt",
                DisplayName = "In Debt for Charcoal",
                ModiMentis  = new[] { "tallycraft", "bargaining" },
                Appearance  = "a worry sits behind the eyes that the work does not account for",
                Persona     = "You owe the charcoal burner more than you can pay and the forge cannot run without them. It colours every price you quote.",
                Opinions    = new[]
                {
                    (DialogueTopic.Trade, "I'd charge less if I could. I can't. There's a man up the wood I owe and he's not patient"),
                    (DialogueTopic.Work,  "Fire eats money. Nobody sees that part — they see the hammer and think that's the trade"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "smith_perfectionist",
                DisplayName = "Will Not Sell a Flawed Piece",
                ModiMentis  = new[] { "aesthetic", "scrutiny", "patience" },
                Appearance  = "turning a finished piece over and over, plainly unhappy with it",
                Persona     = "You break up work that does not satisfy you rather than sell it. It costs you money and you would do it again tomorrow.",
                Opinions    = new[] { (DialogueTopic.Trade, "I've put good iron back in the fire rather than let it out with my mark on it. Twice this month") },
            },
            new PersonalityTrait
            {
                TraitId     = "smith_horse_doctor",
                DisplayName = "Doctors the Beasts",
                ModiMentis  = new[] { "husbandry", "beast_sense", "herblore" },
                Appearance  = "smells of hot iron and, oddly, of liniment",
                Persona     = "You shoe the oxen and, because you are the nearest thing to a doctor for them, you have ended up treating them too. Farmers fetch you at all hours.",
                Opinions    = new[] { (DialogueTopic.Beasts, "I shoe them, so I end up physicking them. Nobody asked me to. They just started knocking") },
            });

        // ── Baker ──────────────────────────────────────────────────────────────
        Add("baker",
            new PersonalityTrait
            {
                TraitId     = "baker_feeds_the_poor",
                DisplayName = "Feeds the Poor",
                ModiMentis  = new[] { "hospitality", "empathy" },
                Items       = new Func<Item>[] { () => new Bread(), () => new Bread() },
                Appearance  = "carrying more loaves than a morning's sales would explain",
                Persona     = "You quietly put aside bread for the households you know are short. You never mention it and you get sharp with anyone who does.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "There's four houses here that would go without if I kept proper accounts. So I don't keep proper accounts") },
            },
            new PersonalityTrait
            {
                TraitId     = "baker_scorched_arms",
                DisplayName = "Oven-Scarred",
                Wounds      = new Func<Wound>[] { () => new ScarWound() },
                Organs      = new[] { ("right_arm", 1) },
                Appearance  = "both forearms are banded with the parallel burn-scars of an oven mouth",
                Persona     = "Your arms are striped with burns from reaching into the oven. You show them off when drunk and are embarrassed by them sober.",
                Opinions    = new[] { (DialogueTopic.Work, "The oven marks you. Every baker I've known has these same stripes up the arm") },
            },
            new PersonalityTrait
            {
                TraitId     = "baker_short_weight",
                DisplayName = "Short of Weight",
                ModiMentis  = new[] { "avarice", "tallycraft", "masquerade" },
                Appearance  = "a little too quick to wrap the loaf and hand it over",
                Persona     = "Your loaves run light and you know exactly by how much. You are charming about it and you have never been caught properly.",
                Opinions    = new[]
                {
                    (DialogueTopic.Trade,      "Weights are weights. If someone thinks otherwise they can bring a scale and we'll all look foolish"),
                    (DialogueTopic.Neighbours, "There's talk. There's always talk about the baker and the miller both. It's tradition") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "baker_sleepless",
                DisplayName = "Never Properly Asleep",
                ModiMentis  = new[] { "endurance", "meditation" },
                Organs      = new[] { ("pineal_gland", -1) },
                Appearance  = "grey about the eyes, and blinks slowly like someone half in a dream",
                Persona     = "You have not had a full night's sleep in twenty years. You are gentle and vague and you lose the thread of sentences.",
                Opinions    = new[] { (DialogueTopic.Rest, "I sleep in pieces. An hour here, two while the oven cools. I've forgotten what the other kind is like") },
                DailyLabour = "firing the oven at an hour that has no name, and dozing upright between batches",
            },
            new PersonalityTrait
            {
                TraitId     = "baker_famous_loaf",
                DisplayName = "Known for the Loaf",
                ModiMentis  = new[] { "doughcraft", "aesthetic", "enterprise" },
                Appearance  = "there is a proprietary way they handle the bread, like a craftsman with a signature",
                Persona     = "Your bread is genuinely better than it needs to be and people come from the next holdings for it. You are quietly, enormously proud.",
                Opinions    = new[] { (DialogueTopic.Food, "Folk walk from three holdings over for my loaf. I'll not pretend I don't like hearing it") },
            },
            new PersonalityTrait
            {
                TraitId     = "baker_hoards_grain",
                DisplayName = "Hoards Against the Winter",
                ModiMentis  = new[] { "cellarcraft", "avarice", "seed_lore" },
                Items       = new Func<Item>[] { () => new Grain() },
                Appearance  = "sizing up every sack in the room without appearing to",
                Persona     = "You lived through a famine and you now keep far more grain back than you need. You are secretive about how much.",
                Opinions    = new[] { (DialogueTopic.Harvest, "I keep more back than I'll use. I've been on the wrong side of a thin spring and I'll not be there twice") },
            });

        // ── Brewer ─────────────────────────────────────────────────────────────
        Add("brewer",
            new PersonalityTrait
            {
                TraitId     = "brewer_keeps_the_gossip",
                DisplayName = "Keeper of the Gossip",
                ModiMentis  = new[] { "streetwise", "rote", "social_interaction" },
                Appearance  = "listening to two conversations that are not theirs",
                Persona     = "You hear everything said across your benches and you forget none of it. You trade information more carefully than you trade ale.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "I know what this village did last night and most of what it means to do tomorrow. I'll not be repeating it cheap") },
            },
            new PersonalityTrait
            {
                TraitId     = "brewer_never_drinks",
                DisplayName = "Never Touches It",
                ModiMentis  = new[] { "discipline", "continence" },
                Appearance  = "the only person in the room with nothing in front of them",
                Persona     = "You brew and you never drink a drop. You watched drink ruin someone and you took a decision about it that you have never once broken.",
                Opinions    = new[]
                {
                    (DialogueTopic.Food, "I taste it and spit it, and that's the whole of my drinking. There's a reason and I'll not go into it"),
                    (DialogueTopic.Rest, "I watch other people rest. That's near enough for me") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "brewer_secret_recipe",
                DisplayName = "Keeps a Secret Gyle",
                ModiMentis  = new[] { "brewcraft", "herblore", "masquerade" },
                Items       = new Func<Item>[] { () => new Herb() },
                Appearance  = "a pouch of something dried at the belt that is plainly not for sharing",
                Persona     = "You put something in your ale that nobody else does and you will die before you say what. You enjoy being asked.",
                Opinions    = new[] { (DialogueTopic.Work, "There's a thing I add. No, I'll not tell you, and no, it isn't what you're thinking") },
            },
            new PersonalityTrait
            {
                TraitId     = "brewer_breaks_up_fights",
                DisplayName = "Breaks Up the Fights",
                ModiMentis  = new[] { "brawling", "iron_fist", "bearing" },
                Organs      = new[] { ("left_arm", 1), ("right_arm", 1) },
                Wounds      = new Func<Wound>[] { () => new BlackEyeRightWound() },
                Appearance  = "thick-armed, with a fresh bruise about one eye and no apparent concern about it",
                Persona     = "You throw people out of your own house most weeks and you are good at it. You are friendly right up until you are not.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "Two or three a month need carrying out. It's not personal and they're back by the week's end") },
            },
            new PersonalityTrait
            {
                TraitId     = "brewer_soured_batch",
                DisplayName = "Lost a Batch",
                ModiMentis  = new[] { "grudgekeeping", "scrutiny" },
                Appearance  = "checking and rechecking something in a way that suggests it went wrong once",
                Persona     = "A whole season's brewing went sour on you last year and nearly finished you. You are superstitious and controlling about the process now.",
                Opinions    = new[]
                {
                    (DialogueTopic.Work,    "I lost a full gyle to the heat last summer. I've not slept easy in a warm spell since"),
                    (DialogueTopic.Weather, "A close, thundery week and I'm at that tub every hour like a man watching a sick child"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "brewer_sings",
                DisplayName = "Leads the Singing",
                ModiMentis  = new[] { "solfege", "carousal", "fables_and_tales" },
                Organs      = new[] { ("tongue", 1) },
                Appearance  = "the loudest thing in the room, and the room seems glad of it",
                Persona     = "You start the songs and you know every verse of all of them, including the ones people pretend to have forgotten.",
                Opinions    = new[] { (DialogueTopic.Stories, "I know verses to some of those that would turn your ears red. Come back after dark") },
            });

        // ── Carpenter ──────────────────────────────────────────────────────────
        Add("carpenter",
            new PersonalityTrait
            {
                TraitId     = "carpenter_coffin_maker",
                DisplayName = "Makes the Coffins",
                ModiMentis  = new[] { "elegy", "woodcraft", "empathy" },
                Appearance  = "there is a gravity to them that ordinary joinery would not produce",
                Persona     = "You make the coffins for this place. You have measured most of the people you know, eventually. It has given you a particular kind of calm.",
                Opinions    = new[]
                {
                    (DialogueTopic.Kin,    "I've built the box for near everyone here who's gone. You end up knowing a family very well"),
                    (DialogueTopic.Health, "I see how it ends more often than most. It's made me gentler, I think, not harder"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "carpenter_lost_thumb",
                DisplayName = "Lost to the Saw",
                Wounds      = new Func<Wound>[] { () => new FingersAmputeeRightWound() },
                ModiMentis  = new[] { "steady_hand" },
                Appearance  = "the right hand is short of a thumb and most of a finger",
                Persona     = "The saw took your thumb years ago. You have relearned the entire trade left-handed and you are better than you were.",
                Opinions    = new[] { (DialogueTopic.Work, "Took my thumb at twenty-six. Learned it all again the other way round. Took four years") },
            },
            new PersonalityTrait
            {
                TraitId     = "carpenter_roof_climber",
                DisplayName = "Goes Up on the Roofs",
                ModiMentis  = new[] { "clambering", "surefoot", "vaulting" },
                Organs      = new[] { ("left_foot", 1), ("right_foot", 1) },
                Appearance  = "moves lightly, with the balance of someone habitually somewhere high",
                Persona     = "You do all the roof work, at any height, in any weather, and nobody else in the village will. You are quietly contemptuous of people who will not climb.",
                Opinions    = new[] { (DialogueTopic.Weather, "Wind. Everything else you can work in. Wind on a roof and you come down") },
            },
            new PersonalityTrait
            {
                TraitId     = "carpenter_carves_for_pleasure",
                DisplayName = "Carves for the Pleasure of It",
                ModiMentis  = new[] { "whittlecraft", "aesthetic", "iconography" },
                Items       = new Func<Item>[] { () => new WoodenDoll() },
                Appearance  = "a half-finished carving of something small and unnecessary in one pocket",
                Persona     = "You carve little useless beautiful things in the evenings and give them away. It is the part of the trade you actually love.",
                Opinions    = new[] { (DialogueTopic.Rest, "I carve. Nothing useful — birds, faces, a doll for someone's child. That's my evening") },
            },
            new PersonalityTrait
            {
                TraitId     = "carpenter_knows_every_roof",
                DisplayName = "Knows Every Roof",
                ModiMentis  = new[] { "architecture", "rote", "scrutiny" },
                Appearance  = "looking at the beams above rather than at the people below",
                Persona     = "You have been inside every building here and you know exactly which are sound and which are one storm from the ground. It worries you.",
                Opinions    = new[]
                {
                    (DialogueTopic.Neighbours, "I could tell you which four houses in this village won't see another bad winter. I've told them, too"),
                    (DialogueTopic.Weather,    "A hard gale and I lie awake going through the roofs in my head"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "carpenter_hates_nails",
                DisplayName = "Will Not Use Nails",
                ModiMentis  = new[] { "woodcraft", "patience", "grudgekeeping" },
                Items       = new Func<Item>[] { () => new WoodChisel() },
                Appearance  = "a chisel and a mallet to hand, and not an iron nail anywhere in sight",
                Persona     = "You consider nails a cheat and you will not use them. Every joint you cut is pegged. It doubles your work and you do not care.",
                Opinions    = new[] { (DialogueTopic.Work, "Pegged. Always pegged. Iron rusts out and takes the timber with it, and then they blame the carpenter") },
            });

        // ── Cooper ─────────────────────────────────────────────────────────────
        Add("cooper",
            new PersonalityTrait
            {
                TraitId     = "cooper_water_tight_pride",
                DisplayName = "Never Made a Leaker",
                ModiMentis  = new[] { "aesthetic", "patience", "bearing" },
                Appearance  = "an unhurried certainty about the hands that is almost annoying to watch",
                Persona     = "No barrel of yours has ever leaked and you will tell anyone this within a minute of meeting them.",
                Opinions    = new[] { (DialogueTopic.Work, "Not one. Forty years and not one cask of mine has ever wept. Ask anybody") },
            },
            new PersonalityTrait
            {
                TraitId     = "cooper_cellar_rat",
                DisplayName = "In Everyone's Cellar",
                ModiMentis  = new[] { "cellarcraft", "streetwise", "scrutiny" },
                Appearance  = "smells faintly of cold stone and old wood",
                Persona     = "Your work takes you into every cellar in the district, so you know exactly what everyone has laid by and what they are pretending not to have.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "I've been under every house here. You'd be amazed what people keep down there and don't mention") },
            },
            new PersonalityTrait
            {
                TraitId     = "cooper_crushed_hand",
                DisplayName = "Crushed by a Cask",
                Wounds      = new Func<Wound>[] { () => new WristFractureLeftWound() },
                ModiMentis  = new[] { "clenched_grit" },
                Appearance  = "the left wrist sits at a wrong angle and is used carefully",
                Persona     = "A full barrel came off a stack onto your wrist. It healed crooked. You work around it and you never stand under a stack now.",
                Opinions    = new[] { (DialogueTopic.Health, "A full cask went over on my wrist. It set how it set. I work round it") },
            },
            new PersonalityTrait
            {
                TraitId     = "cooper_oak_snob",
                DisplayName = "Oak or Nothing",
                ModiMentis  = new[] { "woodcraft", "invective", "ripelore" },
                Appearance  = "running a thumb along a piece of timber with visible disapproval",
                Persona     = "You have violent opinions about wood and you share them unprompted. Anything but seasoned oak insults you personally.",
                Opinions    = new[] { (DialogueTopic.Wilds, "Oak. Anything else in a cask is a lie you're telling the man who buys it") },
            },
            new PersonalityTrait
            {
                TraitId     = "cooper_barrel_bed",
                DisplayName = "Sleeps in the Yard",
                ModiMentis  = new[] { "survivalism", "endurance" },
                Appearance  = "wood shavings in the hair that have plainly been slept in",
                Persona     = "You mostly sleep in the cooperage rather than go home. Whether there is a home to go to is something you do not discuss.",
                Opinions    = new[] { (DialogueTopic.Kin, "The yard suits me. It's warm enough and nobody in it wants anything from me") },
            },
            new PersonalityTrait
            {
                TraitId     = "cooper_measures_everything",
                DisplayName = "Measures Everything",
                ModiMentis  = new[] { "geometric_scheme", "arithmetic_logic", "tallycraft" },
                Items       = new Func<Item>[] { () => new TallyStick() },
                Appearance  = "notching something onto a tally stick before the conversation has even ended",
                Persona     = "You measure and count compulsively — barrels, days, people in a room. It calms you and other people find it unnerving.",
                Opinions    = new[] { (DialogueTopic.Trade, "Numbers don't argue. That's why I like them and why nobody likes doing business with me") },
            });

        // ── Miller ─────────────────────────────────────────────────────────────
        Add("miller",
            new PersonalityTrait
            {
                TraitId     = "miller_thumb_on_the_scale",
                DisplayName = "Thumb on the Scale",
                ModiMentis  = new[] { "avarice", "tallycraft", "masquerade" },
                Items       = new Func<Item>[] { () => new CoinPurse() },
                Appearance  = "an over-friendliness that arrives a moment too fast",
                Persona     = "You take a heavier toll than you are owed and you are extremely good at it. You are warm and jovial and completely dishonest.",
                Opinions    = new[]
                {
                    (DialogueTopic.Trade,      "I take the toll I'm owed. If anyone thinks different they're welcome to grind it by hand"),
                    (DialogueTopic.Neighbours, "They've called the miller a thief since before there were mills. I don't take it personally"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "miller_scrupulous",
                DisplayName = "Weighs It Twice",
                ModiMentis  = new[] { "discipline", "tallycraft", "stoneface" },
                Appearance  = "weighing something in front of you and inviting you to watch",
                Persona     = "Because every miller is assumed to cheat, you have become almost tediously honest, and you make a performance of it.",
                Opinions    = new[] { (DialogueTopic.Trade, "I weigh it in front of you, both ends. I've had enough of that particular joke for one lifetime") },
            },
            new PersonalityTrait
            {
                TraitId     = "miller_white_lung",
                DisplayName = "White Lung",
                ModiMentis  = new[] { "endurance" },
                Organs      = new[] { ("pulmones", -1) },
                Appearance  = "flour-pale to the eyebrows, and coughs in a way that stops the conversation",
                Persona     = "Thirty years of meal dust has ruined your chest. You cough badly, especially indoors, and you know exactly where it ends.",
                Opinions    = new[] { (DialogueTopic.Health, "The dust. It goes in and it stays in. Every miller I've known coughed like this at the end") },
            },
            new PersonalityTrait
            {
                TraitId     = "miller_water_feud",
                DisplayName = "At War Over the Water",
                ModiMentis  = new[] { "grudgekeeping", "drainage", "invective" },
                Appearance  = "bristles at the mention of the river",
                Persona     = "You are in a long, bitter dispute with someone upstream about the water and you will bring it up unprompted.",
                Opinions    = new[]
                {
                    (DialogueTopic.Water, "There's a man upstream who thinks the race is his to dam. It is not. We'll be at law over it yet"),
                    (DialogueTopic.Work,  "I could grind twice what I do if I got the water I'm owed"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "miller_deaf_to_the_wheel",
                DisplayName = "Shouts Over the Wheel",
                Wounds      = new Func<Wound>[] { () => new PerforatedEardrumRightWound() },
                Appearance  = "speaks at a volume suited to a room that is not this one",
                Persona     = "The mill is deafening and you have been in it your whole life. You shout at everyone, everywhere, and are baffled when told so.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "They say I shout. The mill shouts. I'm only keeping up with it") },
            },
            new PersonalityTrait
            {
                TraitId     = "miller_knows_the_shortfall",
                DisplayName = "Knows Who Will Starve",
                ModiMentis  = new[] { "tallycraft", "empathy", "scrutiny" },
                Appearance  = "watches the sacks coming in with something more careful than commerce in it",
                Persona     = "Every sack in the district passes through your hands, so you know months before anyone else which households will not last the winter. It sits on you.",
                Opinions    = new[]
                {
                    (DialogueTopic.Harvest,    "I see it first. I know by the turn of the year who'll be short, and I've not worked out what to do with knowing"),
                    (DialogueTopic.Neighbours, "There's two houses bringing less each month. Nobody's noticed but me, and I'll not be the one to say it aloud"),
                },
            });

        // ── Weaver ─────────────────────────────────────────────────────────────
        Add("weaver",
            new PersonalityTrait
            {
                TraitId     = "weaver_going_blind",
                DisplayName = "Losing the Eyes",
                Wounds      = new Func<Wound>[] { () => new BlackEyeLeftWound() },
                Organs      = new[] { ("left_hand", 1), ("right_hand", 1) },
                ModiMentis  = new[] { "steady_hand", "clenched_grit" },
                Appearance  = "holds the work close enough to touch the nose, and squints at everything further off",
                Persona     = "Your sight is going and the loom is why. You are terrified of the day you cannot work and you tell nobody.",
                Opinions    = new[]
                {
                    (DialogueTopic.Health, "My eyes. I'll not say more than that, and I'd thank you not to ask again"),
                    (DialogueTopic.Work,   "I can do most of it by feel now. Most of it"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "weaver_dyer",
                DisplayName = "Dyes as Well",
                ModiMentis  = new[] { "herblore", "aesthetic", "mycology" },
                Items       = new Func<Item>[] { () => new Herb() },
                Appearance  = "the hands are stained blue-black to the wrist and will not come clean",
                Persona     = "You dye as well as weave, which almost nobody here can do. Your hands are permanently coloured and you are vain about your reds.",
                Opinions    = new[] { (DialogueTopic.Wilds, "Half of what I need grows wild if you know where. Madder, weld, woad. I'll not say where") },
            },
            new PersonalityTrait
            {
                TraitId     = "weaver_pattern_keeper",
                DisplayName = "Keeps the Old Patterns",
                ModiMentis  = new[] { "rote", "lineage_lore", "iconography" },
                Appearance  = "the cloth on the loom carries a figure far more elaborate than the village needs",
                Persona     = "You know patterns handed down four generations and you can weave the mark of every household here. You consider it a sacred trust.",
                Opinions    = new[]
                {
                    (DialogueTopic.Stories, "Every one of these patterns is older than the village. I know which house each belongs to and why"),
                    (DialogueTopic.Kin,     "My grandmother taught me these at seven. I'll teach them on, if anyone will sit still long enough"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "weaver_bent_back",
                DisplayName = "Bent to the Loom",
                Wounds      = new Func<Wound>[] { () => new BrokenBackboneWound() },
                Appearance  = "permanently curved forward, as though still sitting at the treadles while standing up",
                Persona     = "Your back has set into the shape of the loom. Standing straight hurts and you have stopped trying.",
                Opinions    = new[] { (DialogueTopic.Health, "The chair did it. Forty years in the same chair and now I'm the shape of it") },
            },
            new PersonalityTrait
            {
                TraitId     = "weaver_hears_everything",
                DisplayName = "Hears It All Anyway",
                ModiMentis  = new[] { "scrutiny", "streetwise", "rote" },
                Appearance  = "eyes on the warp, and quite obviously listening to every word in the room",
                Persona     = "You sit still and quiet all day while the village walks past your door. You know a great deal and you say very little of it.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "I sit here and they talk past me as if the loom were doing the listening. It isn't") },
            },
            new PersonalityTrait
            {
                TraitId     = "weaver_undersells",
                DisplayName = "Cannot Ask a Fair Price",
                ModiMentis  = new[] { "empathy", "aesthetic" },
                Appearance  = "flinches slightly at the moment money is mentioned",
                Persona     = "You are a fine weaver and a hopeless seller. You drop your price the instant anyone hesitates and your household despairs of you.",
                Opinions    = new[]
                {
                    (DialogueTopic.Trade, "I ask too little. I know I do. I can't stand the moment where they think about it"),
                    (DialogueTopic.Work,  "The cloth's good. It's the selling of it I've never learned"),
                },
            });

        // ── Apprentice ─────────────────────────────────────────────────────────
        Add("apprentice",
            new PersonalityTrait
            {
                TraitId     = "apprentice_beaten",
                DisplayName = "Beaten by the Master",
                Wounds      = new Func<Wound>[] { () => new ContusionWound() },
                ModiMentis  = new[] { "obedience", "clenched_grit" },
                Appearance  = "a fading bruise along the jaw and a habit of flinching at sudden movement",
                Persona     = "Your master beats you. You are careful, watchful and quick to agree, and you would deny all of it if asked directly.",
                Opinions    = new[]
                {
                    (DialogueTopic.Work, "He's hard. That's how it's done and I'll not be the one complaining about it"),
                    (DialogueTopic.Kin,  "My people bound me here. They knew what he was like. I think about that") ,
                },
            },
            new PersonalityTrait
            {
                TraitId          = "apprentice_prodigy",
                DisplayName      = "Better Than the Master",
                ModiMentis       = new[] { "finesse", "steady_hand", "aesthetic" },
                Appearance       = "young hands moving with a confidence that does not match the years",
                Persona          = "You are already better at the craft than the man teaching you, and you both know it. You are careful never to show it, and not careful enough.",
                SelfIntroduction = "only the apprentice here — though you might ask who actually made the piece you're looking at",
                Opinions         = new[] { (DialogueTopic.Work, "I'll say this quietly: there's work goes out under his mark that came off my bench") },
            },
            new PersonalityTrait
            {
                TraitId          = "apprentice_runaway",
                DisplayName      = "Means to Run",
                ModiMentis       = new[] { "wayfaring", "sneak_art", "survivalism" },
                Items            = new Func<Item>[] { () => new DriedMeat(), () => new LeatherCanteen() },
                Appearance        = "carrying rather more food than a day in a workshop requires",
                Persona          = "You are planning to break your indenture and go. You have been saving and hiding provisions. You are desperate for news of the roads.",
                Opinions         = new[]
                {
                    (DialogueTopic.Roads, "Where does that road go? No reason. I only wondered"),
                    (DialogueTopic.Rest,  "I don't rest much. I'm — I've things to think about"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "apprentice_masters_child",
                DisplayName = "The Master's Own",
                ModiMentis  = new[] { "lineage_lore", "bearing", "stewardry" },
                Appearance  = "better fed and better dressed than an apprentice has any business being",
                Persona     = "You are the master's own child, learning the trade you will inherit. The other hands resent you and you have never quite worked out how to fix that.",
                Opinions    = new[]
                {
                    (DialogueTopic.Kin,  "It's my father's shop and it'll be mine. That's not a boast — most days it feels more like a sentence"),
                    (DialogueTopic.Work, "The others think I've it easy. I've never once been let off a thing, but I'll not win that argument"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "apprentice_talks_too_much",
                DisplayName = "Cannot Keep a Secret",
                ModiMentis  = new[] { "social_interaction", "mythomania" },
                Appearance  = "visibly delighted that someone is talking to them",
                Persona     = "You are lonely and starved of conversation, so you tell anyone friendly far more about the workshop's business than you should.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "You want to know who's behind on payments? I shouldn't. But I know, and it's been a long week") },
            },
            new PersonalityTrait
            {
                TraitId     = "apprentice_worships_master",
                DisplayName = "Worships the Master",
                ModiMentis  = new[] { "obedience", "rote", "discipline" },
                Appearance  = "glancing constantly toward whoever is in charge for approval",
                Persona     = "You think your master is the finest craftsman living and you defend them furiously. You quote them constantly, often mid-sentence.",
                Opinions    = new[] { (DialogueTopic.Work, "There's nobody in the county touches the master's work. Nobody. I'll argue it with anyone") },
            });
    }
}
