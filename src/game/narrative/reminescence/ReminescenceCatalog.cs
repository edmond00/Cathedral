using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Narrative.Reminescence;

/// <summary>
/// One-shot builder that populates <see cref="ReminescenceRegistry"/> with the full
/// catalog of childhood reminescences. Each fragment carries two texts:
///   ObservationText — vague/sensory impression shown during the observation phase.
///   OutcomeText     — concrete memory revealed only after REMEMBER fires.
/// </summary>
internal static class ReminescenceCatalog
{
    /// <summary>The wandering-supplies backpack used by several terminal reminescences.</summary>
    private static Item BuildTravelersBackpack()
    {
        var pack = new TravelersBackpack();
        pack.TryAdd(new Bread());
        pack.TryAdd(new Cheese());
        pack.TryAdd(new Sausage());
        var canteen = new LeatherCanteen();
        canteen.TryAdd(new WaterDraught());
        pack.TryAdd(canteen);
        return pack;
    }

    private static FragmentOutcome End(
        IEnumerable<string>? skills = null,
        IEnumerable<Func<Item>>? items = null)
        => new(
            skillIds:           skills == null ? Array.Empty<string>() : new List<string>(skills),
            items:              items == null  ? Array.Empty<Func<Item>>() : new List<Func<Item>>(items),
            nextReminescenceId: ReminescenceRegistry.EndSentinel);

    private static FragmentOutcome To(
        string nextId,
        IEnumerable<string>? skills = null,
        IEnumerable<Func<Item>>? items = null,
        string? setLocation = null)
        => new(
            skillIds:             skills == null ? Array.Empty<string>() : new List<string>(skills),
            items:                items == null  ? Array.Empty<Func<Item>>() : new List<Func<Item>>(items),
            setChildhoodLocation: setLocation,
            nextReminescenceId:   nextId);

    public static void Build(Dictionary<string, ReminescenceData> dst)
    {
        // ── sound_in_the_dark ─────────────────────────────────────────────────
        dst["sound_in_the_dark"] = new ReminescenceData(
            id: "sound_in_the_dark",
            contentLines: new List<string>
            {
                "you are sitting exhausted at the foot of a tree",
                "you remember what brought you here",
                "you remember your childhood",
                "sounds in the distant darkness of your early childhood",
            },
            fragments: new List<FragmentData>
            {
                new("a laugh",
                    observationText: "a warm, distant laugh dissolving into darkness",
                    outcomeText:     "the laugh of your father at the stable where you spent your childhood",
                    outcome: To("stable_childhood",
                        skills: new[] { "sense_of_humor", "beast_sense" },
                        items:  new Func<Item>[] {
                            () => new StableChildSmock(),
                            () => new StableChildBreeches(),
                            () => new StableChildClogs(),
                        },
                        setLocation: "the stable")),

                new("sobs",
                    observationText: "a muffled weeping, a woman's voice somewhere in the dark",
                    outcomeText:     "the sobs of an unknown lady on the dock of the port city where you spent your childhood",
                    outcome: To("port_city_childhood",
                        skills: new[] { "empathy", "nautical_jargon" },
                        items:  new Func<Item>[] {
                            () => new TownsmanCloak(),
                            () => new TownsmanTunic(),
                            () => new TownsmanBreeches(),
                            () => new TownsmanCap(),
                        },
                        setLocation: "the port city")),

                new("a voice",
                    observationText: "a stern, clipped voice reciting something you could not follow",
                    outcomeText:     "the severe voice of your tutor at the orphanage where you spent your childhood",
                    outcome: To("orphanage_childhood",
                        skills: new[] { "discipline", "obedience" },
                        items:  new Func<Item>[] { () => new PlainRobe() },
                        setLocation: "the orphanage")),

                new("a scream",
                    observationText: "a raw, animal scream cutting through the night",
                    outcomeText:     "the pained scream of a slaughtered pig at the farm where you spent your childhood",
                    outcome: To("farm_childhood",
                        skills: new[] { "butchery", "peasantry" },
                        items:  new Func<Item>[] {
                            () => new FarmerSmock(),
                            () => new FarmerBreeches(),
                            () => new FarmerStrawHat(),
                            () => new FarmerClogs(),
                        },
                        setLocation: "the farm")),

                new("a whisper",
                    observationText: "a low, murmured chant winding under a closed door",
                    outcomeText:     "the whispered recitation of a monk at the temple where you spent your childhood",
                    outcome: To("temple_childhood",
                        skills: new[] { "meditation", "murmur" },
                        items:  new Func<Item>[] { () => new PlainRobe() },
                        setLocation: "the temple")),

                new("a lullaby",
                    observationText: "a soft melody, half-heard and warm, pulling you toward sleep",
                    outcomeText:     "the lullaby sung by your mother in your bedroom at the castle where you spent your childhood",
                    outcome: To("castle_childhood",
                        skills: new[] { "lullaby", "aristocracy" },
                        items:  new Func<Item>[] {
                            () => new SilkStockings(),
                            () => new KneeLengthCoat(),
                            () => new NobleUndertunic(),
                            () => new SoftLeatherShoes(),
                        },
                        setLocation: "the castle")),
            });

        // ── farm_childhood ────────────────────────────────────────────────────
        dst["farm_childhood"] = new ReminescenceData(
            id: "farm_childhood",
            contentLines: new List<string>
            {
                "you remember your life in an old and isolated farm",
                "you remember the cry of a slaughtered pig hung by its feet",
                "you remember the ripe peaches in the orchard",
                "you remember the rooster's crow in the morning",
            },
            fragments: new List<FragmentData>
            {
                new("manure smelt",
                    observationText: "a heavy, pungent smell — earthy and animal and inescapable",
                    outcomeText:     "the smell of manure as you pushed wheelbarrows of it and handled pig carcasses",
                    outcome: To("work", skills: new[] { "dirty_labor" })),

                new("apple pie smelt",
                    observationText: "a sweet, drifting warmth — something baking somewhere inside",
                    outcomeText:     "the smell of apple pie, and the taste of sausage, stew and water from the leather canteen",
                    outcome: To("comfort", skills: new[] { "gluttony" })),
            });

        // ── stable_childhood ──────────────────────────────────────────────────
        dst["stable_childhood"] = new ReminescenceData(
            id: "stable_childhood",
            contentLines: new List<string>
            {
                "you remember your life in a humble stable when you were a young child",
                "you remember the laughs and joy of your father",
                "you remember the horses running in their enclosure",
                "you remember the travellers and knights visiting the stable",
            },
            fragments: new List<FragmentData>
            {
                new("a small black donkey",
                    observationText: "a stubborn, round-eyed creature watching you with suspicion in the half-dark",
                    outcomeText:     "a small black donkey you tormented as a child — annoying and tormenting the poor creature",
                    outcome: To("rascal", skills: new[] { "foul_play" })),

                new("an old grey mule",
                    observationText: "a tired, patient grey shape standing still in dim light",
                    outcomeText:     "an old grey mule you cared for — feeding and brushing it every day",
                    outcome: To("work", skills: new[] { "hard_labor" })),
            });

        // ── port_city_childhood ───────────────────────────────────────────────
        dst["port_city_childhood"] = new ReminescenceData(
            id: "port_city_childhood",
            contentLines: new List<string>
            {
                "you remember your life in an old port city when you were a young child",
                "you remember the sobs of an unknown old woman late on the docks",
                "you remember the sound of waves and the smell of salt",
                "you remember the dockers and sailors",
            },
            fragments: new List<FragmentData>
            {
                new("a laughing gull",
                    observationText: "a flash of white wings and a cackling cry over narrow sun-bleached rooftops",
                    outcomeText:     "a laughing gull you chased through the narrow streets of the port city, living by your wits on the street",
                    outcome: To("rascal", skills: new[] { "streetwise" })),

                new("the sound of waves and smell of sweat",
                    observationText: "a low rhythmic thunder and a sour, salt-and-sweat smell rising from below",
                    outcomeText:     "the sound of waves and the smell of sweat on the docks, loading and unloading heavy bundles from ships",
                    outcome: To("work", skills: new[] { "hard_labor" })),
            });

        // ── orphanage_childhood ───────────────────────────────────────────────
        dst["orphanage_childhood"] = new ReminescenceData(
            id: "orphanage_childhood",
            contentLines: new List<string>
            {
                "you remember the severe voice of your tutors at the orphanage",
                "you remember the pain of the punishments",
                "you remember the long hallway and cold dormitory",
            },
            fragments: new List<FragmentData>
            {
                new("a small hairpin",
                    observationText: "something small and metallic, cold against the floorstone — easy to miss",
                    outcomeText:     "a small hairpin you found on the dormitory floor and used to lockpick the door the night you ran away",
                    outcome: To("runaway",
                        skills: new[] { "lockpicking" },
                        items:  new Func<Item>[] { () => new Hairpin() })),

                new("chair, table, paper, inkwell",
                    observationText: "a row of still shapes in lamplight — ordered, patient, waiting",
                    outcomeText:     "a chair, a table, paper and an inkwell — the tools of your lessons at the orphanage",
                    outcome: To("study", skills: new[] { "scholarship" })),
            });

        // ── temple_childhood ──────────────────────────────────────────────────
        dst["temple_childhood"] = new ReminescenceData(
            id: "temple_childhood",
            contentLines: new List<string>
            {
                "you remember your life in an old and cold temple when you were a young child",
                "you remember the strange ritual sounds during the nights",
                "you remember the priests whispering unknown litanies",
                "you remember the terrifying statues of unknown deities",
            },
            fragments: new List<FragmentData>
            {
                new("strange light in the night",
                    observationText: "a pale glow seeping under a doorway in the deep of the night, where there should have been none",
                    outcomeText:     "a strange light you chose to follow — running away from the temple that same night",
                    outcome: To("comfort", skills: new[] { "clairvoyance" })),

                new("candlelight",
                    observationText: "the wavering halo of a single candle, late in the night, very quiet",
                    outcomeText:     "candlelight over an old manuscript, studying late into the night at the temple",
                    outcome: To("study", skills: new[] { "decipher" })),
            });

        // ── castle_childhood ──────────────────────────────────────────────────
        dst["castle_childhood"] = new ReminescenceData(
            id: "castle_childhood",
            contentLines: new List<string>
            {
                "you remember your life in a noble castle when you were a young child",
                "you remember the lullaby your mother sang to help you sleep",
                "you remember the castle courtyard, the knights, courtisans, nobles and servants",
                "you remember the velvet, the rich fineries, the banquets and the feasts",
            },
            fragments: new List<FragmentData>
            {
                new("a pillow, silk sheets, fruits and cheese",
                    observationText: "a softness, a warmth, and something cool and sweet within reach — no need to move",
                    outcomeText:     "silk sheets and a pillow, fruits and cheese brought by a servant — days of idle comfort in bed",
                    outcome: To("comfort", skills: new[] { "sloth" })),

                new("smell of old books, voice of an old man",
                    observationText: "the dry smell of old paper and a measured, deliberate voice reading aloud",
                    outcomeText:     "the smell of old books and the voice of your preceptor, following your lessons at the castle",
                    outcome: To("study", skills: new[] { "scholarship" })),
            });

        // ── rascal ────────────────────────────────────────────────────────────
        dst["rascal"] = new ReminescenceData(
            id: "rascal",
            contentLines: new List<string>
            {
                "you remember the life of a scoundrel you led during your childhood",
                "you remember the tricks and crimes you committed",
            },
            fragments: new List<FragmentData>
            {
                new("a broken tooth",
                    observationText: "the sharp shock of a blow and a hard white shard on the ground",
                    outcomeText:     "a broken tooth from a fight with another child — you remember breaking their jaw",
                    outcome: To("pillage", skills: new[] { "pugilatus" })),

                new("a gold coin",
                    observationText: "a bright disc, warm and heavy in a palm that was not yours",
                    outcomeText:     "a gold coin you stole from a rich traveller at a fair",
                    outcome: To("gold_thirst",
                        skills: new[] { "petty_thief" },
                        items:  new Func<Item>[] { () => new GoldCoin() })),

                new("a glass of wine",
                    observationText: "a cloying sweetness, red lips and laughter in a brightly lit room you had no right to enter",
                    outcomeText:     "a glass of wine you drank at a villa, pretending to be of noble lineage — too much wine, and vomiting on the floor",
                    outcome: To("pillage", skills: new[] { "mythomania" })),
            });

        // ── runaway ───────────────────────────────────────────────────────────
        dst["runaway"] = new ReminescenceData(
            id: "runaway",
            contentLines: new List<string>
            {
                "you remember the night you escaped the building",
                "you remember the fear of being caught and punished",
            },
            fragments: new List<FragmentData>
            {
                new("dark shadows",
                    observationText: "thick darkness pressing against the walls — no lamp, no stars, nowhere to go",
                    outcomeText:     "hiding in shadow so as not to be seen the night you ran away",
                    outcome: To("survive", skills: new[] { "stealth" })),

                new("a small flame",
                    observationText: "a tiny orange finger of fire trembling in the dark, far too small to warm anything",
                    outcomeText:     "a candle you lit as a diversion — and the whole building burning behind you that night",
                    outcome: To("survive", skills: new[] { "arson_fire" })),

                new("sound of glass breaking",
                    observationText: "a sharp, splintering crack splitting the night silence — then running footsteps",
                    outcomeText:     "the sound of a window breaking as you smashed it to escape, then running away across the rooftops",
                    outcome: To("survive", skills: new[] { "acrobatics" })),
            });

        // ── study ─────────────────────────────────────────────────────────────
        dst["study"] = new ReminescenceData(
            id: "study",
            contentLines: new List<string>
            {
                "you remember a studious childhood",
                "you remember listening to lessons",
                "you remember studying old books",
            },
            fragments: new List<FragmentData>
            {
                new("triangles and circles",
                    observationText: "lines and curves drawn in dust — shapes that kept insisting on a hidden pattern",
                    outcomeText:     "triangles and circles from your geometry lessons",
                    outcome: To("curiosity", skills: new[] { "geometric_scheme" })),

                new("numbers and symbols",
                    observationText: "columns of marks on a slate, arranged in a logic you were beginning to see",
                    outcomeText:     "numbers and symbols from your arithmetic lessons",
                    outcome: To("curiosity", skills: new[] { "arithmetic_logic" })),

                new("letters and words",
                    observationText: "marks on a page that were beginning to mean something, slowly, one by one",
                    outcomeText:     "letters and words — you remember the day you learnt to read",
                    outcome: To("curiosity", skills: new[] { "prosaic_grammar" })),
            });

        // ── work ──────────────────────────────────────────────────────────────
        dst["work"] = new ReminescenceData(
            id: "work",
            contentLines: new List<string>
            {
                "you remember the effort and fatigue of your difficult labour",
                "you remember the first silver coin you received for your work",
            },
            fragments: new List<FragmentData>
            {
                new("something unlucky",
                    observationText: "the clatter of dice on wood, and a silence that meant you had lost",
                    outcomeText:     "gambling your first silver coin and losing it in one throw",
                    outcome: To("gold_thirst", skills: new[] { "gambling" })),

                new("something sharp",
                    observationText: "a gleam of metal behind a trader's stall, the faint smell of oil and iron",
                    outcomeText:     "a sword you bought with your first earned coin from a market trader",
                    outcome: To("dream",
                        skills: new[] { "bargaining" },
                        items:  new Func<Item>[] { () => new ShortSword() })),

                new("nothing",
                    observationText: "a closed fist, and the plain satisfaction of keeping it closed",
                    outcomeText:     "choosing to keep your silver coin rather than spend or gamble it",
                    outcome: To("gold_thirst",
                        skills: new[] { "avarice" },
                        items:  new Func<Item>[] { () => new SilverCoin() })),
            });

        // ── comfort ───────────────────────────────────────────────────────────
        dst["comfort"] = new ReminescenceData(
            id: "comfort",
            contentLines: new List<string>
            {
                "you remember a happy childhood",
                "you remember days spent playing innocently",
                "you remember your boundless imagination",
            },
            fragments: new List<FragmentData>
            {
                new("a company of knights",
                    observationText: "the noise of children shouting, playing at something urgent in the sun",
                    outcomeText:     "playing knights with other children — your old friends from those years",
                    outcome: To("pillage", skills: new[] { "friendship" })),

                new("a magic sword",
                    observationText: "a smooth stick, light in the hand, that your mind kept turning into something more",
                    outcomeText:     "a wooden stick you imagined as a magic sword",
                    outcome: To("dream",
                        skills: new[] { "social_interaction" },
                        items:  new Func<Item>[] { () => new WoodenStick() })),

                new("a sleeping princess",
                    observationText: "a small painted face of wood, blank-eyed and still in your hands",
                    outcomeText:     "a wooden doll you imagined as a sleeping princess",
                    outcome: To("dream",
                        skills: new[] { "puppet_theather" },
                        items:  new Func<Item>[] { () => new WoodenDoll() })),

                new("an old magician",
                    observationText: "an old face in dim light and a storytelling voice, slow and warm, always one more tale",
                    outcomeText:     "your grandfather telling you stories to fall asleep — always one more, always one more",
                    outcome: To("pillage", skills: new[] { "fables_and_tales" })),
            });

        // ── curiosity ─────────────────────────────────────────────────────────
        dst["curiosity"] = new ReminescenceData(
            id: "curiosity",
            contentLines: new List<string>
            {
                "you remember your insatiable childhood curiosity",
                "you remember reading many books to satisfy it",
            },
            fragments: new List<FragmentData>
            {
                new("a large, heavy book",
                    observationText: "a great weight of pages, the spine pressing into your palms — too many words to count",
                    outcomeText:     "a great encyclopedia full of concepts you could not understand, until you left to find a library that could explain them",
                    outcome: To("travel",
                        skills: new[] { "scientific_research" },
                        items:  new Func<Item>[] { BuildTravelersBackpack })),

                new("an old dusty manuscript",
                    observationText: "crumbling edges and small faded letters beneath your finger — someone else's journey",
                    outcomeText:     "an old dusty manuscript telling of a traveller crossing the world, until you left to do the same",
                    outcome: To("travel",
                        skills: new[] { "voyage" },
                        items:  new Func<Item>[] { BuildTravelersBackpack })),

                new("a black book with indecipherable symbols",
                    observationText: "a dark cover marked with signs your eye kept sliding off — impossible to hold in mind",
                    outcomeText:     "a black book of indecipherable symbols, until you left to find someone who could teach you the language",
                    outcome: To("travel",
                        skills: new[] { "linguistic" },
                        items:  new Func<Item>[] { BuildTravelersBackpack })),
            });

        // ── dream ─────────────────────────────────────────────────────────────
        dst["dream"] = new ReminescenceData(
            id: "dream",
            contentLines: new List<string>
            {
                "you remember a strange and vivid dream you had as a child",
                "you remember having the same dream many times",
            },
            fragments: new List<FragmentData>
            {
                new("dream of a golden arch",
                    observationText: "a great curve of gold in a dream sky — ancient, enormous, and impossibly bright",
                    outcomeText:     "a dream of a golden arch rising over the ruins of an ancient city, until you left to find it",
                    outcome: To("travel",
                        skills: new[] { "archeology" },
                        items:  new Func<Item>[] { BuildTravelersBackpack })),

                new("dream of scaled wings",
                    observationText: "enormous dark wings folding through a dream sky — something reptilian, something breathing fire",
                    outcomeText:     "a dream of a great lizard with scaled wings breathing fire, until you left to find that creature",
                    outcome: To("travel",
                        skills: new[] { "clairvoyance" },
                        items:  new Func<Item>[] { BuildTravelersBackpack })),

                new("dream of purple rubies",
                    observationText: "a deep red gleam, like stained glass, in a dark dreaming space — a chest, a dungeon, a promise",
                    outcomeText:     "a dream of a chest of purple rubies in a dark dungeon, until you left to find that treasure",
                    outcome: To("travel",
                        skills: new[] { "greed" },
                        items:  new Func<Item>[] { BuildTravelersBackpack })),
            });

        // ── gold_thirst ───────────────────────────────────────────────────────
        dst["gold_thirst"] = new ReminescenceData(
            id: "gold_thirst",
            contentLines: new List<string>
            {
                "you remember your gold thirst",
                "you remember your desire to be rich",
            },
            fragments: new List<FragmentData>
            {
                new("a dirty traveller",
                    observationText: "a ragged stranger with something heavy in bulging, reeking bags",
                    outcomeText:     "a dirty traveller carrying bags of gold nuggets, until you left to find a gold mine of your own",
                    outcome: To("travel",
                        skills: new[] { "treasure_hunting" },
                        items:  new Func<Item>[] { BuildTravelersBackpack })),

                new("a perfumed traveller",
                    observationText: "a figure stepping into the light with fine cloth and a scent you had never smelled before",
                    outcomeText:     "a perfumed traveller from a great bustling city, until you left to make your fortune there",
                    outcome: To("travel",
                        skills: new[] { "high_society_manners" },
                        items:  new Func<Item>[] { BuildTravelersBackpack })),

                new("an eccentric foreigner",
                    observationText: "a strange voice, strange goods spread on a cloth, a smell of distant places you had no name for",
                    outcomeText:     "an eccentric foreign merchant with rare goods from a distant land, until you left to trade in those countries",
                    outcome: To("travel",
                        skills: new[] { "enterprise" },
                        items:  new Func<Item>[] { BuildTravelersBackpack })),
            });

        // ── pillage ───────────────────────────────────────────────────────────
        dst["pillage"] = new ReminescenceData(
            id: "pillage",
            contentLines: new List<string>
            {
                "you remember the day looters attacked and raided the place where you lived",
                "you remember everything burning around you",
                "you remember everyone you knew being slaughtered",
            },
            fragments: new List<FragmentData>
            {
                new("a shady corner",
                    observationText: "a dark recess in a wall, barely large enough to press yourself into",
                    outcomeText:     "a shady corner where you hid while the looters ransacked everything around you",
                    outcome: To("survive", skills: new[] { "stealth" })),

                new("a pile of corpses",
                    observationText: "a shapeless heap in the shadows, utterly still while chaos moved around it",
                    outcomeText:     "a pile of corpses — you lay among them, pretending to be dead until the looters left",
                    outcome: To("survive", skills: new[] { "masquerade" })),

                new("pain in the leg",
                    observationText: "a burning ache spreading up from the knees, the ground blurring below you",
                    outcomeText:     "the pain in your legs from running as fast as you could away from the pillage",
                    outcome: To("survive", skills: new[] { "athletics" })),
            });

        // ── survive (terminal) ────────────────────────────────────────────────
        dst["survive"] = new ReminescenceData(
            id: "survive",
            contentLines: new List<string>
            {
                "you remember having to survive alone in the wild",
                "you remember the fear, the hunger and the cold",
            },
            fragments: new List<FragmentData>
            {
                new("worms",
                    observationText: "soft, pale coils in the earth beneath your palm — still moving",
                    outcomeText:     "worms you ate from the dirt to survive, until you collapsed exhausted at the foot of a tree",
                    outcome: End(
                        skills: new[] { "survivalism" },
                        items:  new Func<Item>[] { () => new Worm() })),

                new("mice and squirrels",
                    observationText: "small quick shapes darting at the edge of your vision, there and gone",
                    outcomeText:     "mice and squirrels you hunted and ate to survive, until you collapsed exhausted at the foot of a tree",
                    outcome: End(
                        skills: new[] { "hunt" },
                        items:  new Func<Item>[] { () => new MouseMeat(), () => new SquirrelMeat() })),

                new("mushrooms",
                    observationText: "pale caps huddled in the shadow of a root, wet with morning damp",
                    outcomeText:     "mushrooms you gathered and ate to survive, until you collapsed exhausted at the foot of a tree",
                    outcome: End(
                        skills: new[] { "mycology" },
                        items:  new Func<Item>[] { () => new Mushroom() })),
            });

        // ── travel (terminal) ─────────────────────────────────────────────────
        dst["travel"] = new ReminescenceData(
            id: "travel",
            contentLines: new List<string>
            {
                "you remember travelling for days and days",
                "you remember the fatigue and the loneliness",
                "you remember being caught in a violent storm one day",
            },
            fragments: new List<FragmentData>
            {
                new("rain and winds",
                    observationText: "a wall of water and roaring air, the ground slipping underfoot, no shelter in any direction",
                    outcomeText:     "rain and wind you pushed through until your legs gave out — losing consciousness at the foot of a tree",
                    outcome: End(skills: new[] { "brute_force" })),

                new("wet wood",
                    observationText: "a pile of dark soaked sticks and no smoke, no heat — only the cold spreading inward",
                    outcomeText:     "wet wood that would not catch fire, and being too exhausted to try again — losing consciousness at the foot of a tree",
                    outcome: End(skills: new[] { "bushcraft" })),

                new("shelter",
                    observationText: "a dark hillside with no door, no overhang, no gap anywhere — nothing to crawl into",
                    outcomeText:     "searching for shelter from the storm and finding nothing — losing consciousness at the foot of a tree",
                    outcome: End(skills: new[] { "exploration" })),
            });
    }
}
