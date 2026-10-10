using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;

namespace Cathedral.Game.Npc.Traits;

/// <summary>Reserved traits for the people of the settled country: orchardist, vintner, planter, picker, groom, drover, gravedigger, guard, captain, priest, monk, scholar, steward, lord, merchant, sailor, innkeeper, clerk, quartermaster, sacristan, cellarer.</summary>
public sealed partial class PersonalityTraitRegistry
{
    private void RegisterSettledTraits()
    {
        // ── Orchardist ────────────────────────────────────────────────────────────
        Add("orchardist",
            new PersonalityTrait
            {
                TraitId     = "orchardist_talks_to_trees",
                DisplayName = "Talks to the Trees",
                ModiMentis  = new[] { "pomology", "reverence", "patience" },
                Appearance  = "murmurs something to a trunk before laying a hand on it",
                Persona     = "You speak to your trees aloud and believe they grow better for it. You are slightly embarrassed to be caught at it and do it anyway.",
                Opinions    = new[] { (DialogueTopic.Work, "Laugh if you like. The ones I talk to bear heavier. I've kept count") },
            },
            new PersonalityTrait
            {
                TraitId     = "orchardist_master_grafter",
                DisplayName = "Master Grafter",
                ModiMentis  = new[] { "grafting", "steady_hand", "finesse" },
                Items       = new Func<Item>[] { () => new GraftingKnife() },
                Appearance  = "fingers nicked with a hundred tiny healed cuts",
                Persona     = "You can make anything take to anything - pear on quince, apple on crab. Other orchards send for you. You are quietly very proud of it.",
                Opinions    = new[] { (DialogueTopic.Work, "Three varieties on one tree, that one by the gate. People think it is a miracle. It is a sharp knife and a steady hand") },
            },
            new PersonalityTrait
            {
                TraitId     = "orchardist_fell_from_ladder",
                DisplayName = "Fell from the Ladder",
                ModiMentis  = new[] { "caution", "clenched_grit" },
                Wounds      = new Func<Wound>[] { () => new KneeFractureRightWound() },
                Appearance  = "walks with a stiff hip and looks up at ladders with mistrust",
                Persona     = "A rung broke under you at the top of the old pear and you have not climbed above halfway since. You send the young ones up and hate yourself for it.",
                Opinions    = new[]
                {
                    (DialogueTopic.Health, "my hip tells me the weather two days before the sky does"),
                    (DialogueTopic.Work, "I pick from the ground now and the ladder rungs. Somebody younger goes up top"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "orchardist_cider_drinker",
                DisplayName = "Fond of the Cider",
                ModiMentis  = new[] { "carousal", "bouquet" },
                Items       = new Func<Item>[] { () => new Cider() },
                Appearance  = "a red nose and a contented smile at all hours",
                Persona     = "The windfalls go to the press and a good share of the press goes to you. You are never drunk, exactly. You are never entirely sober either.",
                Opinions    = new[]
                {
                    (DialogueTopic.Food, "windfalls are no good for eating, but they make a cider you would sell your boots for"),
                    (DialogueTopic.Rest, "a jug in the shade of the old tree after the picking. That is the whole of what I want"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "orchardist_wasp_stung",
                DisplayName = "Swollen with Stings",
                ModiMentis  = new[] { "iron_nerves", "swarm_sense" },
                Organs      = new[] { ("nose", -1) },
                Appearance  = "a face puffy on one side from a fresh sting, and not a care about it",
                Persona     = "You have been stung so many times that you barely notice. You take a strange pride in reaching into a wasp-filled windfall bare-handed.",
                Opinions    = new[] { (DialogueTopic.Wilds, "the wasps want the fruit as much as I do. We've come to an arrangement - mostly") },
            },
            new PersonalityTrait
            {
                TraitId     = "orchardist_keeps_old_varieties",
                DisplayName = "Keeps the Old Varieties",
                ModiMentis  = new[] { "seed_lore", "lineage_lore", "pomology" },
                Appearance  = "knows the name of every tree in the row and says them like the names of the dead",
                Persona     = "You keep trees nobody plants any more, old varieties with odd names and odd tastes, and you will take the cuttings to your grave rather than let them die out.",
                Opinions    = new[] { (DialogueTopic.Stories, "that one is a Lady-of-the-Ford. There are three left in the whole country and two of them are mine") },
            });

        // ── Vintner ───────────────────────────────────────────────────────────────
        Add("vintner",
            new PersonalityTrait
            {
                TraitId     = "vintner_golden_nose",
                DisplayName = "The Golden Nose",
                ModiMentis  = new[] { "bouquet", "tea_lore", "apothecary_nose" },
                Organs      = new[] { ("nose", 1) },
                Appearance  = "sniffs at everything - food, people, the air - before engaging with it",
                Persona     = "Your nose is famous for three valleys and you know it. You can tell a vintage, a row and a week from a single sniff, and you never let anyone forget it.",
                Opinions    = new[] { (DialogueTopic.Food, "smell it first. Always. The tongue only confirms what the nose already knows") },
            },
            new PersonalityTrait
            {
                TraitId     = "vintner_ruined_vintage",
                DisplayName = "Lost a Vintage",
                ModiMentis  = new[] { "grudgekeeping", "almanac", "vigilance" },
                Appearance  = "glances at the sky constantly, as if expecting it to betray them",
                Persona     = "Hail took one whole vintage in a single afternoon and the debts are not paid yet. You watch every cloud like an enemy.",
                Opinions    = new[] { (DialogueTopic.Weather, "twenty minutes. That is how long it took. Twenty minutes and a year of work was mud on the ground") },
            },
            new PersonalityTrait
            {
                TraitId     = "vintner_waters_the_wine",
                DisplayName = "Waters the Wine",
                ModiMentis  = new[] { "sharp_practice", "masquerade", "bargaining" },
                Items       = new Func<Item>[] { () => new Wine() },
                Appearance  = "smiles a little too readily when the wine is praised",
                Persona     = "You stretch your wine with water and sometimes with worse, and you sell it as the good stuff. You have never been caught and you are beginning to think you never will be.",
                Opinions    = new[] { (DialogueTopic.Trade, "it is the same wine. More or less. Nobody has ever complained") },
            },
            new PersonalityTrait
            {
                TraitId     = "vintner_old_family",
                DisplayName = "Old Vineyard Family",
                ModiMentis  = new[] { "lineage_lore", "pride", "heraldry" },
                Appearance  = "speaks of the vines as an inheritance rather than a property",
                Persona     = "Your family has had these slopes for nine generations and you will not be the one who loses them. Every decision is measured against the dead.",
                Opinions    = new[] { (DialogueTopic.Kin, "my great-grandfather planted those rows. I am just the one looking after them at the moment") },
            },
            new PersonalityTrait
            {
                TraitId     = "vintner_drinks_his_stock",
                DisplayName = "Drinks the Stock",
                ModiMentis  = new[] { "carousal", "gluttony" },
                Organs      = new[] { ("hepar", -1) },
                Appearance  = "a broken-veined face and an easy, too-loud laugh",
                Persona     = "You drink more of your own wine than you sell, and the accounts show it. You are excellent company and a disastrous businessman.",
                Opinions    = new[]
                {
                    (DialogueTopic.Rest, "a vintner who does not drink his own wine is a vintner who does not trust it"),
                    (DialogueTopic.Trade, "the accounts are... the accounts are complicated this year"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "vintner_pruning_zealot",
                DisplayName = "Prunes Hard",
                ModiMentis  = new[] { "pruning", "severity", "discipline" },
                Items       = new Func<Item>[] { () => new PruningHook() },
                Appearance  = "looks at overgrown things - hedges, beards, people - with visible disapproval",
                Persona     = "You prune harder than anyone in the valley and your yields are low and your wine is the best for miles. You apply the same principle to everything, including your children.",
                Opinions    = new[] { (DialogueTopic.Work, "cut it back to almost nothing. It will hate you for a season and thank you for twenty") },
            });

        // ── Planter ───────────────────────────────────────────────────────────────
        Add("planter",
            new PersonalityTrait
            {
                TraitId     = "planter_hard_driver",
                DisplayName = "Drives the Gangs Hard",
                ModiMentis  = new[] { "plantership", "severity", "iron_fist" },
                Appearance  = "a cane under the arm that has not been carried for walking",
                Persona     = "You work your people to the edge and past it when the price is high. You tell yourself it is the price and not you.",
                Opinions    = new[] { (DialogueTopic.Work, "they can rest in the wet season. The crop cannot wait for anyone") },
            },
            new PersonalityTrait
            {
                TraitId     = "planter_fever_survivor",
                DisplayName = "Survived the Fever",
                ModiMentis  = new[] { "endurance", "second_wind" },
                Organs      = new[] { ("hepar", -1) },
                Appearance  = "yellowed eyes and a slight tremor in the hands",
                Persona     = "The fever nearly killed you your first year in the hot country, and it comes back every wet season. You drink quinine bark and pretend it does not.",
                Opinions    = new[] { (DialogueTopic.Health, "it comes back every year. One year it will win. Not this one") },
            },
            new PersonalityTrait
            {
                TraitId     = "planter_in_debt",
                DisplayName = "Deep in Debt",
                ModiMentis  = new[] { "bookkeeping", "dread", "masquerade" },
                Appearance  = "good clothes going thin at the elbows",
                Persona     = "The plantation is mortgaged to a coast merchant and a bad year will take it. Nobody knows. Everyone suspects.",
                Opinions    = new[] { (DialogueTopic.Trade, "business is excellent. Excellent. Why do you ask?") },
            },
            new PersonalityTrait
            {
                TraitId     = "planter_decent_master",
                DisplayName = "A Decent Master",
                ModiMentis  = new[] { "empathy", "stewardry", "mercy" },
                Appearance  = "knows the names of the workers and uses them",
                Persona     = "You feed your people properly and let the sick rest, and your neighbours think you are soft. Your yields are as good as theirs and your people do not run.",
                Opinions    = new[] { (DialogueTopic.Work, "a fed hand cuts twice the cane of a starved one. It is not kindness. Well. Not only kindness") },
            },
            new PersonalityTrait
            {
                TraitId     = "planter_botanist",
                DisplayName = "Collects Plants",
                ModiMentis  = new[] { "herblore", "scientific_research", "curiosity" },
                Appearance  = "a pressed flower sticking out of the ledger",
                Persona     = "You collect and press every plant you find and send them to scholars across the sea. You care more about the specimens than the crop.",
                Opinions    = new[] { (DialogueTopic.Wilds, "there is a flower in the far ravine nobody has named. I intend it to carry mine") },
            },
            new PersonalityTrait
            {
                TraitId     = "planter_homesick_planter",
                DisplayName = "Misses the Old Country",
                ModiMentis  = new[] { "homesickness", "elegy" },
                Appearance  = "keeps a faded print of a cold, green landscape by the door",
                Persona     = "You came out to the hot country to make a fortune and you have, nearly, and you hate every day of the heat. You will go home rich or not at all.",
                Opinions    = new[]
                {
                    (DialogueTopic.Weather, "I would give a year of profit for one grey drizzling morning"),
                    (DialogueTopic.Kin, "they are all back home. I write. They write less"),
                },
            });

        // ── Picker ────────────────────────────────────────────────────────────────
        Add("picker",
            new PersonalityTrait
            {
                TraitId     = "picker_fastest_hands",
                DisplayName = "Fastest Hands in the Row",
                ModiMentis  = new[] { "canecraft", "harvestry", "athletics" },
                Appearance  = "hands moving even while standing still, as if still picking",
                Persona     = "You fill more baskets than anyone and the master knows your name for it. The others resent you and depend on you.",
                Opinions    = new[] { (DialogueTopic.Work, "I fill twelve baskets. The others fill eight and say I make them look slow") },
            },
            new PersonalityTrait
            {
                TraitId     = "picker_runaway",
                DisplayName = "Ran Once",
                ModiMentis  = new[] { "vagabondage", "stealth", "self_preservation" },
                Wounds      = new Func<Wound>[] { () => new ScarWound() },
                Appearance  = "scarred ankles, and a habit of noting every exit",
                Persona     = "You ran from a worse master once and were brought back and then sold on. You are always watching the road.",
                Opinions    = new[] { (DialogueTopic.Roads, "I know where every road out of here goes. I have walked two of them") },
            },
            new PersonalityTrait
            {
                TraitId     = "picker_singer_in_rows",
                DisplayName = "Sings in the Rows",
                ModiMentis  = new[] { "shanty", "solfege" },
                Appearance  = "humming under the breath even while talking",
                Persona     = "You lead the songs that keep the pickers in rhythm and you know two hundred of them. It is the one thing nobody can take from you.",
                Opinions    = new[] { (DialogueTopic.Stories, "every song is a story. The old ones are about masters who got what they deserved") },
            },
            new PersonalityTrait
            {
                TraitId     = "picker_saving_coins",
                DisplayName = "Saving to Buy Free",
                ModiMentis  = new[] { "thrift", "tallycraft", "patience" },
                Items       = new Func<Item>[] { () => new CoinPurse() },
                Appearance  = "a small purse hidden somewhere about the clothing, touched often",
                Persona     = "You have been saving coppers for eleven years to buy your way out. You are a little over halfway.",
                Opinions    = new[] { (DialogueTopic.Trade, "every copper goes in the purse. Every single one. I know to the coin how far I have to go") },
            },
            new PersonalityTrait
            {
                TraitId     = "picker_snakebitten",
                DisplayName = "Snakebitten",
                ModiMentis  = new[] { "caution", "creature_lore" },
                Organs      = new[] { ("right_hand", -1) },
                Appearance  = "a withered patch on one hand and a sharp eye on the undergrowth",
                Persona     = "A snake in the rows bit you three years ago and you nearly died of it. You check every step now and you will not reach into shade.",
                Opinions    = new[] { (DialogueTopic.Wilds, "they lie in the cool under the leaves at noon. Never reach in where you cannot see") },
            },
            new PersonalityTrait
            {
                TraitId     = "picker_rows_gossip",
                DisplayName = "Knows Everyone's Business",
                ModiMentis  = new[] { "gossip", "crowd_murmur", "eavesdropping" },
                Appearance  = "eyes always moving between the other pickers",
                Persona     = "Nothing happens on the plantation that you do not know by nightfall. You trade gossip like other people trade coin.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "the overseer is sweet on the cook's daughter, and the cook does not know. Yet") },
            });

        // ── Groom ─────────────────────────────────────────────────────────────────
        Add("groom",
            new PersonalityTrait
            {
                TraitId     = "groom_horse_whisperer",
                DisplayName = "Gentles Wild Horses",
                ModiMentis  = new[] { "horse_sense", "beast_sense", "patience" },
                Appearance  = "moves slowly and softly, and horses turn their heads toward them",
                Persona     = "You can quiet a horse nobody else can touch, and you never use force. Some people think it is witchcraft. You think it is listening.",
                Opinions    = new[] { (DialogueTopic.Beasts, "you do not break a horse. You ask it, and you wait until it says yes") },
            },
            new PersonalityTrait
            {
                TraitId     = "groom_kicked",
                DisplayName = "Kicked Once",
                ModiMentis  = new[] { "caution", "clenched_grit" },
                Wounds      = new Func<Wound>[] { () => new ConcussionsWound() },
                Appearance  = "a dent in the forehead under the hair, and a habit of standing to the side",
                Persona     = "A frightened mare caught you full in the head and you were a week waking up. You still love them. You stand differently now.",
                Opinions    = new[] { (DialogueTopic.Beasts, "never stand behind one you do not know. I learned that the hard way and I nearly did not learn anything again") },
            },
            new PersonalityTrait
            {
                TraitId     = "groom_loves_one_horse",
                DisplayName = "Has a Favourite",
                ModiMentis  = new[] { "friendship", "horse_sense" },
                Items       = new Func<Item>[] { () => new Bread() },
                Appearance  = "keeps a crust of bread in a pocket for one particular horse",
                Persona     = "There is one old horse in the stable you love more than any person. You know you will grieve him like a brother when he goes.",
                Opinions    = new[] { (DialogueTopic.Beasts, "the grey in the end stall. He is older than me in horse years. He knows everything") },
            },
            new PersonalityTrait
            {
                TraitId     = "groom_sells_tips",
                DisplayName = "Sells Racing Tips",
                ModiMentis  = new[] { "gambling", "bloodlines", "sharp_practice" },
                Appearance  = "a knowing look whenever horses are discussed",
                Persona     = "You know which horse will win before the race and you sell the knowledge to anyone with coin. Sometimes you make it true.",
                Opinions    = new[] { (DialogueTopic.Trade, "a quiet word about which horse is lame this morning is worth a silver to the right man") },
            },
            new PersonalityTrait
            {
                TraitId     = "groom_farrier_trained",
                DisplayName = "Trained at the Forge",
                ModiMentis  = new[] { "farriery", "metalcraft" },
                Items       = new Func<Item>[] { () => new HoofPick() },
                Appearance  = "burn marks on the forearms and a hammer at the belt",
                Persona     = "You trained as a farrier before you came to the stable, and you shoe the horses yourself and do it better than the smith.",
                Opinions    = new[] { (DialogueTopic.Work, "the smith shoes them like he is shoeing a cart. I shoe them like they are going to walk on it") },
            },
            new PersonalityTrait
            {
                TraitId     = "groom_rides_at_night",
                DisplayName = "Rides at Night",
                ModiMentis  = new[] { "horsemanship", "night_ear", "wanderlust" },
                Appearance  = "tired-eyed and windburned, as if from long rides",
                Persona     = "When everyone is asleep you take the best horse out and ride as far as you can and back before dawn. The master does not know.",
                Opinions    = new[] { (DialogueTopic.Roads, "the roads are empty at night. Just me and the horse and the moon. That is the only freedom I have") },
            });

        // ── Drover ────────────────────────────────────────────────────────────────
        Add("drover",
            new PersonalityTrait
            {
                TraitId     = "drover_lost_herd",
                DisplayName = "Lost a Herd",
                ModiMentis  = new[] { "grudgekeeping", "herd_eye", "vigilance" },
                Appearance  = "counts the herd compulsively, lips moving",
                Persona     = "You lost a whole herd to a stampede in a storm once, and a man with it. You count your animals ten times a day now.",
                Opinions    = new[] { (DialogueTopic.Beasts, "I count them. Then I count them again. Then once more before I sleep") },
            },
            new PersonalityTrait
            {
                TraitId     = "drover_dog_bond",
                DisplayName = "Inseparable from the Dog",
                ModiMentis  = new[] { "beast_call", "whistling", "friendship" },
                Appearance  = "whistles softly through the teeth as a habit",
                Persona     = "Your dog is the best on the range and the only creature you trust entirely. You would leave the herd before you left the dog.",
                Opinions    = new[] { (DialogueTopic.Beasts, "that dog does the work of three men and does not drink") },
            },
            new PersonalityTrait
            {
                TraitId     = "drover_cattle_thief",
                DisplayName = "Takes a Few Head",
                ModiMentis  = new[] { "rustling", "branding", "masquerade" },
                Appearance  = "very relaxed about other people's brands",
                Persona     = "You lift a few beasts from other herds every season and change the marks. Everyone does it. You do it better.",
                Opinions    = new[] { (DialogueTopic.Trade, "a stray calf belongs to whoever finds it. That is the law of the range. My law, anyway") },
            },
            new PersonalityTrait
            {
                TraitId     = "drover_trampled",
                DisplayName = "Trampled in a Run",
                ModiMentis  = new[] { "endurance", "balance" },
                Wounds      = new Func<Wound>[] { () => new BrokenRibsWound() },
                Appearance  = "walks with a pronounced roll and a cracked laugh",
                Persona     = "You went under a running herd once and came out with half your ribs broken. You were back in the saddle in a month and you are proud of it.",
                Opinions    = new[] { (DialogueTopic.Health, "my ribs grew back crooked. I can tell rain is coming from how they ache") },
            },
            new PersonalityTrait
            {
                TraitId     = "drover_star_reader",
                DisplayName = "Steers by the Stars",
                ModiMentis  = new[] { "astronomy", "sky_reading", "homing" },
                Appearance  = "looks up often, even in daylight",
                Persona     = "You find your way across open country at night by the stars, and you know their names - your own names, not the scholars'.",
                Opinions    = new[] { (DialogueTopic.Omens, "the herdsman's star rose late this year. That means a hard winter, mark me") },
            },
            new PersonalityTrait
            {
                TraitId     = "drover_loner",
                DisplayName = "Hates Towns",
                ModiMentis  = new[] { "misanthropy", "survivalism" },
                Appearance  = "restless and twitchy indoors, eyes on the door",
                Persona     = "You cannot bear walls and crowds. A night under a roof makes you short-tempered and you leave market the moment the beasts are sold.",
                Opinions    = new[]
                {
                    (DialogueTopic.Neighbours, "too many people. Too close. Too loud. Give me the range"),
                    (DialogueTopic.Rest, "I sleep out, even in town. Roofs press on me"),
                },
            });

        // ── Gravedigger ───────────────────────────────────────────────────────────
        Add("gravedigger",
            new PersonalityTrait
            {
                TraitId     = "gravedigger_dark_humour",
                DisplayName = "Laughs at Death",
                ModiMentis  = new[] { "sense_of_humor", "sangfroid", "stoneface" },
                Appearance  = "a grin that sits oddly on a face so often near graves",
                Persona     = "You make jokes about the dead constantly, gently, and it makes the bereaved either laugh or hate you. You think both help.",
                Opinions    = new[] { (DialogueTopic.Stories, "old Wenna asked to be buried facing the alehouse. I obliged. She looks content") },
            },
            new PersonalityTrait
            {
                TraitId     = "gravedigger_robs_graves",
                DisplayName = "Robs the Graves",
                ModiMentis  = new[] { "grave_robbing", "treasure_hunting", "stealth" },
                Items       = new Func<Item>[] { () => new GraveRing() },
                Appearance  = "oddly fine rings on dirty fingers",
                Persona     = "You take what the rich are buried with, a little at a time, and fill the graves back so well no one has ever noticed.",
                Opinions    = new[] { (DialogueTopic.Trade, "the dead do not need gold. I have never understood burying it") },
            },
            new PersonalityTrait
            {
                TraitId     = "gravedigger_fears_the_dark",
                DisplayName = "Afraid of the Dark",
                ModiMentis  = new[] { "dread", "superstition" },
                Items       = new Func<Item>[] { () => new Lantern() },
                Appearance  = "carries a lantern even at dusk, and keeps it lit",
                Persona     = "You are terrified of the graveyard at night and have been all your life. You took the job anyway, because it was the only job.",
                Opinions    = new[] { (DialogueTopic.Omens, "I do not dig after dark. Ever. I do not care who has died") },
            },
            new PersonalityTrait
            {
                TraitId     = "gravedigger_keeps_register",
                DisplayName = "Keeps the Register",
                ModiMentis  = new[] { "clerkship", "recollection", "lineage_lore" },
                Appearance  = "ink-stained fingers to go with the dirt",
                Persona     = "You keep a perfect register of every burial for forty years, with notes. Families come to you to settle inheritances.",
                Opinions    = new[] { (DialogueTopic.Kin, "I can tell you who your great-grandmother married and who she should have married. It is all in the book") },
            },
            new PersonalityTrait
            {
                TraitId     = "gravedigger_bad_back",
                DisplayName = "Spade-Bent",
                ModiMentis  = new[] { "hard_labor", "endurance" },
                Wounds      = new Func<Wound>[] { () => new BrokenBackboneWound() },
                Appearance  = "bent nearly double even when standing still",
                Persona     = "Thirty years of digging have bent your back for good. You cannot stand straight and you dig anyway.",
                Opinions    = new[] { (DialogueTopic.Health, "every grave takes a little more out of the back. I will dig my own one day, if I can still bend") },
            },
            new PersonalityTrait
            {
                TraitId     = "gravedigger_talks_to_dead",
                DisplayName = "Talks to the Dead",
                ModiMentis  = new[] { "mourning", "elegy", "vigil" },
                Appearance  = "murmurs to the graves as they pass",
                Persona     = "You talk to the people you buried, especially the ones nobody visits. You tell them the news and you are certain they listen.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "my neighbours are all under the grass. Better company than most above it") },
            });

        // ── Guard ─────────────────────────────────────────────────────────────────
        Add("guard",
            new PersonalityTrait
            {
                TraitId     = "guard_takes_bribes",
                DisplayName = "Can Be Bought",
                ModiMentis  = new[] { "bribery", "gatekeeping", "avarice" },
                Items       = new Func<Item>[] { () => new CoinPurse() },
                Appearance  = "a hand that drifts toward an open palm when strangers approach",
                Persona     = "Every cart through your gate pays a little extra to you. You think of it as a fee. Your captain thinks of it as your pay.",
                Opinions    = new[] { (DialogueTopic.Trade, "the toll is the toll. The rest is between you and me") },
            },
            new PersonalityTrait
            {
                TraitId     = "guard_veteran_of_war",
                DisplayName = "Old Campaigner",
                ModiMentis  = new[] { "battlecraft", "war_weariness", "bastion_eye" },
                Wounds      = new Func<Wound>[] { () => new ScarWound() },
                Appearance  = "scarred forearms and the eyes of someone who has seen a breach",
                Persona     = "You fought in the last war and came back with half your company. You do not talk about it and you do not sleep well.",
                Opinions    = new[] { (DialogueTopic.Stories, "there was a breach at the river fort. I was in it. That is all I will say") },
            },
            new PersonalityTrait
            {
                TraitId     = "guard_by_the_book",
                DisplayName = "Goes by the Book",
                ModiMentis  = new[] { "discipline", "watchkeeping", "obedience" },
                Appearance  = "kit polished and correct to the last buckle",
                Persona     = "You follow every order exactly and expect everyone else to. You have never taken a coin, and you report those who do.",
                Opinions    = new[] { (DialogueTopic.Work, "the order says no one through after dark. So no one goes through after dark. Not even you") },
            },
            new PersonalityTrait
            {
                TraitId     = "guard_gambler",
                DisplayName = "Plays Dice on Watch",
                ModiMentis  = new[] { "gambling", "barrack_wit", "card_sharping" },
                Items       = new Func<Item>[] { () => new Dice() },
                Appearance  = "rattles something in a pocket constantly",
                Persona     = "You play dice on every watch and you owe half the garrison money. You are always one throw from getting even.",
                Opinions    = new[] { (DialogueTopic.Rest, "a watch goes faster with a game. Want to play? Copper a throw") },
            },
            new PersonalityTrait
            {
                TraitId     = "guard_lost_eye",
                DisplayName = "Lost an Eye",
                ModiMentis  = new[] { "clenched_grit", "vigilance" },
                Wounds      = new Func<Wound>[] { () => new PiercedEyeLeftWound() },
                Appearance  = "a patch over one eye, and a way of turning the whole head to look",
                Persona     = "An arrow took your eye in a skirmish and you were kept on because you refused to leave. You watch harder than anyone with two.",
                Opinions    = new[] { (DialogueTopic.Health, "one eye sees well enough. Better than some with two that never look") },
            },
            new PersonalityTrait
            {
                TraitId     = "guard_hopes_for_glory",
                DisplayName = "Wants a War",
                ModiMentis  = new[] { "valor", "temerity", "boasting" },
                Appearance  = "young, restless, and touching the sword hilt too often",
                Persona     = "You joined for glory and have found only gate duty. You secretly pray for an attack, any attack, and are ashamed of it.",
                Opinions    = new[] { (DialogueTopic.Stories, "one day something will come up that road worth fighting. I will be ready") },
            });

        // ── Captain ───────────────────────────────────────────────────────────────
        Add("captain",
            new PersonalityTrait
            {
                TraitId     = "captain_cautious_commander",
                DisplayName = "Never Takes a Risk",
                ModiMentis  = new[] { "caution", "fortification", "provisioning" },
                Appearance  = "checks the walls personally every evening",
                Persona     = "You have never lost a fort because you have never taken a chance. Your men think you are cowardly. They are all still alive.",
                Opinions    = new[] { (DialogueTopic.Work, "brave captains get statues. Careful ones get to retire") },
            },
            new PersonalityTrait
            {
                TraitId     = "captain_cruel_discipline",
                DisplayName = "Harsh Disciplinarian",
                ModiMentis  = new[] { "severity", "cruelty", "drill" },
                Appearance  = "a whip hangs on the wall behind the desk and is not decorative",
                Persona     = "You flog for small offences and hang for large ones, and your garrison is the best-drilled in the province and hates you.",
                Opinions    = new[] { (DialogueTopic.Work, "fear keeps a wall manned. Love does not") },
            },
            new PersonalityTrait
            {
                TraitId     = "captain_political",
                DisplayName = "Plays Politics",
                ModiMentis  = new[] { "intrigue", "petitioning", "precedence" },
                Appearance  = "more letters on the desk than maps",
                Persona     = "You care more about promotion than defence and spend your days writing to people who matter. The fort runs itself, mostly.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "I have friends at court. That is worth more than walls") },
            },
            new PersonalityTrait
            {
                TraitId     = "captain_loves_his_men",
                DisplayName = "Father of the Garrison",
                ModiMentis  = new[] { "esprit_de_corps", "loyalty", "empathy" },
                Appearance  = "greets every soldier by name",
                Persona     = "You know every soldier, their families and their troubles, and they would die for you. You worry that one day they will.",
                Opinions    = new[] { (DialogueTopic.Kin, "two hundred men. Every one of them is mine to bring home") },
            },
            new PersonalityTrait
            {
                TraitId     = "captain_old_wound",
                DisplayName = "The Old Wound",
                ModiMentis  = new[] { "iron_nerves", "endurance" },
                Wounds      = new Func<Wound>[] { () => new TibiaFractureLeftWound() },
                Appearance  = "favours one leg and hides it badly",
                Persona     = "A spear through the thigh at a siege years ago never healed right. You refuse to sit down in front of your men.",
                Opinions    = new[] { (DialogueTopic.Health, "it aches in the cold. I do not sit. A captain who sits is a captain who is old") },
            },
            new PersonalityTrait
            {
                TraitId     = "captain_drinks_alone",
                DisplayName = "Drinks Alone",
                ModiMentis  = new[] { "carousal", "introspection", "war_weariness" },
                Items       = new Func<Item>[] { () => new Wine() },
                Appearance  = "a heavy, careful steadiness that is not quite sober",
                Persona     = "Every night after the watch is set you drink alone in the command room until you can sleep.",
                Opinions    = new[] { (DialogueTopic.Rest, "there is no rest for a captain. There is wine, which is close enough") },
            });

        // ── Priest ────────────────────────────────────────────────────────────────
        Add("priest",
            new PersonalityTrait
            {
                TraitId     = "priest_hellfire",
                DisplayName = "Preaches Hellfire",
                ModiMentis  = new[] { "zeal", "homily", "severity" },
                Appearance  = "a voice that does not drop below a pulpit's volume",
                Persona     = "Your sermons are terrifying and the temple is full every holy day. You are not sure if they come for the faith or the spectacle.",
                Opinions    = new[] { (DialogueTopic.Omens, "the end is closer than they think. I tell them every week") },
            },
            new PersonalityTrait
            {
                TraitId     = "priest_secret_doubter",
                DisplayName = "Has Lost the Faith",
                ModiMentis  = new[] { "heresy", "philosophy", "masquerade" },
                Appearance  = "says the prayers perfectly and does not seem to listen to them",
                Persona     = "You stopped believing years ago and go on saying the offices because the people need them and you need the stipend.",
                Opinions    = new[] { (DialogueTopic.Stories, "the old stories are beautiful. Whether they are true is another matter. I am told") },
            },
            new PersonalityTrait
            {
                TraitId     = "priest_sells_pardons",
                DisplayName = "Sells Pardons",
                ModiMentis  = new[] { "simony", "bargaining", "sharp_practice" },
                Items       = new Func<Item>[] { () => new Signet() },
                Appearance  = "a fine ring on a priest's hand",
                Persona     = "Forgiveness is free, but a donation shows sincerity, and you have grown comfortable on sincerity.",
                Opinions    = new[] { (DialogueTopic.Trade, "the temple does not sell. It accepts. Generously") },
            },
            new PersonalityTrait
            {
                TraitId     = "priest_healer_priest",
                DisplayName = "Tends the Sick",
                ModiMentis  = new[] { "physic", "anointing", "mercy" },
                Items       = new Func<Item>[] { () => new Poultice() },
                Appearance  = "herb-stained fingers and a smell of poultices",
                Persona     = "You spend more time nursing the sick than saying offices, and the bishop disapproves. The valley adores you.",
                Opinions    = new[] { (DialogueTopic.Health, "a prayer and a poultice. Both help. One helps faster") },
            },
            new PersonalityTrait
            {
                TraitId     = "priest_scholarly_priest",
                DisplayName = "Buried in Books",
                ModiMentis  = new[] { "theology", "hagiography", "scholarship" },
                Items       = new Func<Item>[] { () => new ReadingLenses() },
                Appearance  = "reading glasses pushed up on the forehead and forgotten",
                Persona     = "You have read every book in the temple twice and are writing one of your own. You forget to eat.",
                Opinions    = new[] { (DialogueTopic.Stories, "the lives of the saints are more interesting than anything that happens in the village") },
            },
            new PersonalityTrait
            {
                TraitId     = "priest_exorcist",
                DisplayName = "Drives Out Spirits",
                ModiMentis  = new[] { "exorcism", "dread", "superstition" },
                Items       = new Func<Item>[] { () => new SaltPouch() },
                Appearance  = "salt in every pocket and a nervous glance at shadows",
                Persona     = "You have performed three exorcisms and believe you succeeded in two. You see the devil's work where others see bad luck.",
                Opinions    = new[] { (DialogueTopic.Omens, "that cow did not sicken by itself. Something touched it. I will see to it") },
            });

        // ── Monk ──────────────────────────────────────────────────────────────────
        Add("monk",
            new PersonalityTrait
            {
                TraitId     = "monk_illuminator",
                DisplayName = "Paints the Books",
                ModiMentis  = new[] { "illumination", "aesthetic", "patience" },
                Appearance  = "gold dust in the creases of the fingers",
                Persona     = "You paint the capitals of the great books and have spent three years on one page. You believe it is the best thing you will ever do.",
                Opinions    = new[] { (DialogueTopic.Work, "a hare in the margin, chasing a snail. It took me a month. Nobody will notice it for a hundred years") },
            },
            new PersonalityTrait
            {
                TraitId     = "monk_broke_silence",
                DisplayName = "Talks Too Much",
                ModiMentis  = new[] { "gossip", "banter", "curiosity" },
                Appearance  = "leans in eagerly whenever anyone outside the house speaks",
                Persona     = "You cannot keep the silence and are always being punished for it. Visitors are a joy to you, and a dangerous one.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "tell me everything. What is happening out there? We hear nothing") },
            },
            new PersonalityTrait
            {
                TraitId     = "monk_former_soldier",
                DisplayName = "Was a Soldier",
                ModiMentis  = new[] { "contrition", "battlecraft", "mortification" },
                Wounds      = new Func<Wound>[] { () => new ScarWound() },
                Appearance  = "a soldier's shoulders under the habit",
                Persona     = "You killed men in the war and took the vows to atone. You still wake at night reaching for a sword that is not there.",
                Opinions    = new[] { (DialogueTopic.Stories, "I was someone else before. He did things I am still praying for") },
            },
            new PersonalityTrait
            {
                TraitId     = "monk_cellarer",
                DisplayName = "Keeps the Cellar",
                ModiMentis  = new[] { "cellarcraft", "brewcraft", "gluttony" },
                Items       = new Func<Item>[] { () => new Wine() },
                Appearance  = "a comfortable belly and a red face",
                Persona     = "You keep the monastery's cellar and its wine and ale are famous. You taste everything, for quality.",
                Opinions    = new[] { (DialogueTopic.Food, "the brothers' ale is the best in the province. I would know") },
            },
            new PersonalityTrait
            {
                TraitId     = "monk_mystic_monk",
                DisplayName = "Has Visions",
                ModiMentis  = new[] { "mysticism", "vigil", "clairvoyance" },
                Appearance  = "eyes that seem to look a little past people",
                Persona     = "You have seen things at prayer in the night that you cannot describe. The abbot is worried about you.",
                Opinions    = new[] { (DialogueTopic.Omens, "there was a light in the chapel at the third hour. Not a candle. Something else") },
            },
            new PersonalityTrait
            {
                TraitId     = "monk_herbalist_monk",
                DisplayName = "Tends the Herb Garden",
                ModiMentis  = new[] { "herblore", "simpling", "physic" },
                Items       = new Func<Item>[] { () => new Valerian() },
                Appearance  = "smells of rosemary and earth",
                Persona     = "You grow every healing herb in the monastery garden and treat the whole valley for free.",
                Opinions    = new[] { (DialogueTopic.Health, "feverfew for the head, comfrey for the bones, valerian for sleep. The garden knows more than any physician") },
            });

        // ── Scholar ───────────────────────────────────────────────────────────────
        Add("scholar",
            new PersonalityTrait
            {
                TraitId     = "scholar_stargazer",
                DisplayName = "Watches the Stars",
                ModiMentis  = new[] { "astronomy", "geometric_scheme", "algebraic_analysis" },
                Items       = new Func<Item>[] { () => new Astrolabe() },
                Appearance  = "red-eyed from nights on the roof",
                Persona     = "You spend every clear night on the roof with your instruments and are certain you have found a new moon.",
                Opinions    = new[] { (DialogueTopic.Omens, "the sky is not an omen. It is a clock. A very large, very precise clock") },
            },
            new PersonalityTrait
            {
                TraitId     = "scholar_cruel_master",
                DisplayName = "Beats the Pupils",
                ModiMentis  = new[] { "severity", "pedagogy", "discipline" },
                Items       = new Func<Item>[] { () => new Rod() },
                Appearance  = "a birch rod tucked into the belt",
                Persona     = "You believe learning is driven in through the hand, and your pupils are terrified of you and very good at grammar.",
                Opinions    = new[] { (DialogueTopic.Work, "the rod is the oldest teacher there is. It has never failed me") },
            },
            new PersonalityTrait
            {
                TraitId     = "scholar_heretical_scholar",
                DisplayName = "Holds Dangerous Ideas",
                ModiMentis  = new[] { "heresy", "disputation", "scientific_research" },
                Appearance  = "glances at the door before saying anything interesting",
                Persona     = "You have reached conclusions about the world that the temple would burn you for, and you write them in cipher.",
                Opinions    = new[] { (DialogueTopic.Stories, "there are things in the old books they would rather we did not read. I have read them") },
            },
            new PersonalityTrait
            {
                TraitId     = "scholar_law_master",
                DisplayName = "Master of Law",
                ModiMentis  = new[] { "jurisprudence", "rhetoric", "rote" },
                Appearance  = "quotes the codes in ordinary conversation",
                Persona     = "You are the foremost authority on the empire's law in the province and you are consulted by judges. You are insufferable about it.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "every quarrel in this town is a lawsuit that has not yet found its lawyer") },
            },
            new PersonalityTrait
            {
                TraitId     = "scholar_poor_scholar",
                DisplayName = "Poor as a Mouse",
                ModiMentis  = new[] { "thrift", "beggary", "scholarship" },
                Appearance  = "a gown worn thin and patched with a different black",
                Persona     = "Every coin you have goes on books. You eat bread and onions and are perfectly happy.",
                Opinions    = new[] { (DialogueTopic.Food, "bread and an onion. What else does a mind need?") },
            },
            new PersonalityTrait
            {
                TraitId     = "scholar_alchemist_scholar",
                DisplayName = "Practises Alchemy",
                ModiMentis  = new[] { "alchemy", "thermodynamics", "obduracy" },
                Appearance  = "one eyebrow singed off and the other raised",
                Persona     = "You are secretly pursuing the great work in the cellar of the school. You have nearly burned it down twice.",
                Opinions    = new[] { (DialogueTopic.Work, "the black, the white, the red. I reached the white last spring. The red is next") },
            });

        // ── Steward ───────────────────────────────────────────────────────────────
        Add("steward",
            new PersonalityTrait
            {
                TraitId     = "steward_embezzler",
                DisplayName = "Skims the Accounts",
                ModiMentis  = new[] { "bookkeeping", "sharp_practice", "masquerade" },
                Appearance  = "a better coat than a steward's wages should buy",
                Persona     = "You have been skimming from the household accounts for years and keep two sets of books. Your lord has never looked.",
                Opinions    = new[] { (DialogueTopic.Trade, "the accounts are in perfect order. I keep them myself") },
            },
            new PersonalityTrait
            {
                TraitId     = "steward_devoted",
                DisplayName = "Devoted to the House",
                ModiMentis  = new[] { "fealty", "loyalty", "stewardry" },
                Appearance  = "speaks of the family as \"we\" without noticing",
                Persona     = "You served the old lord and now the young one, and you would die for the house. You privately think the young lord a fool.",
                Opinions    = new[] { (DialogueTopic.Kin, "I carried the young master on my shoulders. Now I bow to him. It is the proper order of things") },
            },
            new PersonalityTrait
            {
                TraitId     = "steward_intriguer_steward",
                DisplayName = "Knows Every Secret",
                ModiMentis  = new[] { "intrigue", "eavesdropping", "gossip" },
                Appearance  = "appears silently in doorways",
                Persona     = "You know every secret in the house and use them carefully. Nobody in the household dares cross you.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "I know who visits whom at night. I write nothing down. I need not") },
            },
            new PersonalityTrait
            {
                TraitId     = "steward_perfectionist",
                DisplayName = "Perfectionist",
                ModiMentis  = new[] { "precedence", "high_society_manners", "scrutiny" },
                Appearance  = "straightens things as they pass",
                Persona     = "A misplaced spoon ruins your day. The house is immaculate and the servants live in fear of your white glove.",
                Opinions    = new[] { (DialogueTopic.Work, "the forks go there. Not there. There") },
            },
            new PersonalityTrait
            {
                TraitId     = "steward_former_servant",
                DisplayName = "Rose from the Scullery",
                ModiMentis  = new[] { "dirty_labor", "humility", "ambition" },
                Appearance  = "chapped hands under good cuffs",
                Persona     = "You started as a kitchen child and rose to steward, and you have never forgotten it. You are kind to the scullions and hard on the gentry.",
                Opinions    = new[] { (DialogueTopic.Work, "I scrubbed those flags once. I know exactly how long it takes") },
            },
            new PersonalityTrait
            {
                TraitId     = "steward_gout",
                DisplayName = "Gouty",
                ModiMentis  = new[] { "gourmandise", "iron_nerves" },
                Organs      = new[] { ("right_foot", -1) },
                Appearance  = "a bandaged foot and a careful, rolling walk",
                Persona     = "You eat at the high table's leftovers every night and your foot is paying for it. You will not stop.",
                Opinions    = new[]
                {
                    (DialogueTopic.Food, "the leftovers of a great house are better than most men's feasts"),
                    (DialogueTopic.Health, "the foot. Do not mention the foot"),
                },
            });

        // ── Lord ──────────────────────────────────────────────────────────────────
        Add("lord",
            new PersonalityTrait
            {
                TraitId     = "lord_generous_lord",
                DisplayName = "Open-Handed",
                ModiMentis  = new[] { "largesse", "hospitality", "pride" },
                Items       = new Func<Item>[] { () => new CoinPurse() },
                Appearance  = "gives a coin to every beggar at the gate, publicly",
                Persona     = "You are famously generous, and you make sure the generosity is famous. Your table is the best in the province and it is ruining you.",
                Opinions    = new[] { (DialogueTopic.Trade, "a lord who counts coppers is no lord at all") },
            },
            new PersonalityTrait
            {
                TraitId     = "lord_tyrant",
                DisplayName = "A Hard Lord",
                ModiMentis  = new[] { "cruelty", "iron_fist", "disdain" },
                Appearance  = "peasants step off the road when they see the colours",
                Persona     = "You rule by fear and the gallows outside your gate is never empty for long. You believe softness invites rebellion.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "they hate me. They obey me. I know which matters") },
            },
            new PersonalityTrait
            {
                TraitId     = "lord_falconer_lord",
                DisplayName = "Lives for the Hunt",
                ModiMentis  = new[] { "falconry", "hunt", "horsemanship" },
                Appearance  = "a hawk's glove on one hand even indoors",
                Persona     = "You care more for your hawks and hounds than your estates, and you hunt every day the weather allows.",
                Opinions    = new[] { (DialogueTopic.Wilds, "the high moor in autumn with a falcon on the fist. That is the only time I am happy") },
            },
            new PersonalityTrait
            {
                TraitId     = "lord_scholar_lord",
                DisplayName = "Reads Too Much",
                ModiMentis  = new[] { "scholarship", "philosophy", "statecraft" },
                Appearance  = "a library larger than the armoury",
                Persona     = "You prefer books to people and philosophy to war, and your vassals think you weak. You think them illiterate.",
                Opinions    = new[] { (DialogueTopic.Stories, "I have every history of this province ever written. Most of them are lies, beautifully told") },
            },
            new PersonalityTrait
            {
                TraitId     = "lord_last_of_line",
                DisplayName = "Last of the Line",
                ModiMentis  = new[] { "lineage_lore", "elegy", "dread" },
                Appearance  = "a portrait gallery with far more faces than heirs",
                Persona     = "You have no heir and the line dies with you. It is the only thing you think about.",
                Opinions    = new[] { (DialogueTopic.Kin, "nine generations, and I am the last. Do you know what that weighs?") },
            },
            new PersonalityTrait
            {
                TraitId     = "lord_scarred_knight",
                DisplayName = "Scarred in Battle",
                ModiMentis  = new[] { "swordsmanship", "valor", "fealty" },
                Wounds      = new Func<Wound>[] { () => new DisfiguredWound() },
                Appearance  = "a long scar across the face, worn like a medal",
                Persona     = "You won your spurs in the last war and the scar across your face is your proudest possession.",
                Opinions    = new[] { (DialogueTopic.Stories, "I took this at the ford. The man who gave it to me is buried there") },
            });

        // ── Merchant ──────────────────────────────────────────────────────────────
        Add("merchant",
            new PersonalityTrait
            {
                TraitId     = "merchant_cheats_weights",
                DisplayName = "False Weights",
                ModiMentis  = new[] { "sharp_practice", "appraisal", "masquerade" },
                Items       = new Func<Item>[] { () => new Scales() },
                Appearance  = "a hand that rests casually on the scales",
                Persona     = "Your weights are a little light and your measures a little short, and you have made a fortune a grain at a time.",
                Opinions    = new[] { (DialogueTopic.Trade, "every merchant in this town uses the same weights. Mine are simply more... careful") },
            },
            new PersonalityTrait
            {
                TraitId     = "merchant_honest_dealer",
                DisplayName = "Honest as the Day",
                ModiMentis  = new[] { "plain_dealing", "bookkeeping", "courtesy" },
                Appearance  = "a reputation that seems to precede them",
                Persona     = "You have never cheated anyone and it is your whole fortune: people trade with you because they trust you.",
                Opinions    = new[] { (DialogueTopic.Trade, "my word is my bond. It has made me more money than any trick ever made any thief") },
            },
            new PersonalityTrait
            {
                TraitId     = "merchant_ruined_by_sea",
                DisplayName = "Lost a Ship",
                ModiMentis  = new[] { "dread", "navigation", "thrift" },
                Appearance  = "watches the harbour mouth anxiously",
                Persona     = "Your best ship went down with everything you had in her and you are rebuilding from almost nothing. You flinch at storms.",
                Opinions    = new[] { (DialogueTopic.Weather, "the wind is getting up. I have three ships out. Do not talk to me about the weather") },
            },
            new PersonalityTrait
            {
                TraitId     = "merchant_moneylender",
                DisplayName = "Lends at Interest",
                ModiMentis  = new[] { "usury", "avarice", "coin_eye" },
                Items       = new Func<Item>[] { () => new PromissoryNote() },
                Appearance  = "half the town looks away when they pass",
                Persona     = "You lend to everyone, at a tenth a month, and half the town owes you money. It makes you powerful and very lonely.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "they all owe me. Nobody invites me to dinner. Funny, that") },
            },
            new PersonalityTrait
            {
                TraitId     = "merchant_worldly_trader",
                DisplayName = "Has Been Everywhere",
                ModiMentis  = new[] { "worldliness", "pidgin", "wanderlust" },
                Appearance  = "a foreign cut to the coat and a foreign word or two in every sentence",
                Persona     = "You have traded in every port of the empire and beyond, and nothing at home will ever be as interesting.",
                Opinions    = new[] { (DialogueTopic.Stories, "in the islands they pay in shells. In the hot country they pay in salt. Here they pay late") },
            },
            new PersonalityTrait
            {
                TraitId     = "merchant_spice_fortune",
                DisplayName = "Made It in Spices",
                ModiMentis  = new[] { "covetousness", "enterprise", "gourmandise" },
                Items       = new Func<Item>[] { () => new SpiceBundle() },
                Appearance  = "smells faintly of cinnamon and cloves",
                Persona     = "You made your fortune in spices from the hot country and you put them in everything. Your house smells like a bazaar.",
                Opinions    = new[] { (DialogueTopic.Food, "a pinch of clove changes everything. Most people here have never tasted food") },
            });

        // ── Sailor ────────────────────────────────────────────────────────────────
        Add("sailor",
            new PersonalityTrait
            {
                TraitId     = "sailor_superstitious_sailor",
                DisplayName = "Every Superstition",
                ModiMentis  = new[] { "superstition", "augury", "dread" },
                Items       = new Func<Item>[] { () => new LuckCharm() },
                Appearance  = "touches the charm at their neck every few sentences",
                Persona     = "You know every sea-superstition and obey all of them. You once refused to sail because a priest waved at the ship.",
                Opinions    = new[] { (DialogueTopic.Omens, "a cat sneezing means rain. A woman whistling means wind. Do not laugh") },
            },
            new PersonalityTrait
            {
                TraitId     = "sailor_pressed_man",
                DisplayName = "Was Pressed",
                ModiMentis  = new[] { "grudgekeeping", "seamanship", "vagabondage" },
                Appearance  = "flinches at the sight of an officer",
                Persona     = "You were taken drunk from a tavern and woke at sea, and you served six years before you could leave. You hate officers and love the sea.",
                Opinions    = new[] { (DialogueTopic.Kin, "they took me from a tavern at nineteen. My mother thought I was dead for six years") },
            },
            new PersonalityTrait
            {
                TraitId     = "sailor_smuggler_sailor",
                DisplayName = "Runs Contraband",
                ModiMentis  = new[] { "smuggling", "pilotage", "stealth" },
                Appearance  = "knows a great deal about quiet coves",
                Persona     = "Between honest voyages you run brandy and silk into quiet beaches by night. You know every cove on this coast.",
                Opinions    = new[] { (DialogueTopic.Trade, "the tax men watch the harbour. They do not watch the beaches. Not all of them") },
            },
            new PersonalityTrait
            {
                TraitId     = "sailor_shantyman",
                DisplayName = "The Shantyman",
                ModiMentis  = new[] { "shanty", "solfege", "banter" },
                Appearance  = "a voice that carries across a harbour",
                Persona     = "You lead the songs on every ship you sail and you know two hundred of them, some of them unrepeatable.",
                Opinions    = new[] { (DialogueTopic.Stories, "every song is a ship. Some of them sank") },
            },
            new PersonalityTrait
            {
                TraitId     = "sailor_shipwrecked",
                DisplayName = "Survived a Wreck",
                ModiMentis  = new[] { "natation", "second_wind", "salvage" },
                Wounds      = new Func<Wound>[] { () => new ScarWound() },
                Appearance  = "scars on the hands from rocks, and a stare at breaking waves",
                Persona     = "You were wrecked on the rocks and swam half a night to shore. You still dream of it. You still go to sea.",
                Opinions    = new[] { (DialogueTopic.Water, "the sea had me once and let me go. I am not sure why. I keep going back to ask") },
            },
            new PersonalityTrait
            {
                TraitId     = "sailor_topman",
                DisplayName = "Best Man Aloft",
                ModiMentis  = new[] { "rigging", "balance", "acrobatics" },
                Appearance  = "climbs on things without noticing - walls, barrels, people",
                Persona     = "You are the best topman in the port and you go aloft in weather other men will not, laughing.",
                Opinions    = new[] { (DialogueTopic.Work, "sixty feet up on a footrope in a gale. Nothing else feels like it") },
            });

        // ── Innkeeper ─────────────────────────────────────────────────────────────
        Add("innkeeper",
            new PersonalityTrait
            {
                TraitId     = "innkeeper_waters_ale",
                DisplayName = "Waters the Ale",
                ModiMentis  = new[] { "sharp_practice", "brewcraft" },
                Appearance  = "a slightly too generous smile at every complaint",
                Persona     = "Your ale is a third water and your stew a third yesterday's. Nobody has ever complained twice.",
                Opinions    = new[] { (DialogueTopic.Food, "the ale is exactly as good as everyone else's. Well. Nearly") },
            },
            new PersonalityTrait
            {
                TraitId     = "innkeeper_ex_mercenary",
                DisplayName = "Retired Sellsword",
                ModiMentis  = new[] { "brawling", "bastion_eye", "sangfroid" },
                Items       = new Func<Item>[] { () => new Cudgel() },
                Wounds      = new Func<Wound>[] { () => new BrokenNoseWound() },
                Appearance  = "a cudgel under the counter and a nose broken more than once",
                Persona     = "You fought for pay for fifteen years and bought the inn with what was left. No one starts a fight in your house twice.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "trouble in my house goes out the door or out the window. Its choice") },
            },
            new PersonalityTrait
            {
                TraitId     = "innkeeper_information_broker",
                DisplayName = "Sells What They Hear",
                ModiMentis  = new[] { "eavesdropping", "brokerage", "gossip" },
                Appearance  = "leans close to listen, then moves off to talk to someone else",
                Persona     = "You hear everything in the taproom and sell what is worth selling. Half the town's secrets have passed over your counter.",
                Opinions    = new[] { (DialogueTopic.Trade, "ale is a copper a pot. What people say over it is worth a great deal more") },
            },
            new PersonalityTrait
            {
                TraitId     = "innkeeper_motherly",
                DisplayName = "Mothers the Guests",
                ModiMentis  = new[] { "hospitality", "empathy", "cookery" },
                Items       = new Func<Item>[] { () => new Pottage() },
                Appearance  = "presses food on everyone, paid for or not",
                Persona     = "You feed anyone who looks hungry, whether they can pay or not, and your accounts are a disaster. Travellers remember you for years.",
                Opinions    = new[] { (DialogueTopic.Food, "you look starved. Sit down. Eat. We will talk about money later, or not at all") },
            },
            new PersonalityTrait
            {
                TraitId     = "innkeeper_fixes_dice",
                DisplayName = "Runs the Dice",
                ModiMentis  = new[] { "card_sharping", "gambling", "foul_play" },
                Items       = new Func<Item>[] { () => new Dice() },
                Appearance  = "a back room where the games never stop",
                Persona     = "The real money in your house is the back room, where the dice are loaded and the house always wins.",
                Opinions    = new[] { (DialogueTopic.Rest, "a game in the back, if you have the coin. Friendly, of course") },
            },
            new PersonalityTrait
            {
                TraitId     = "innkeeper_brews_famous",
                DisplayName = "Famous Brew",
                ModiMentis  = new[] { "brewcraft", "hop_lore", "pride" },
                Items       = new Func<Item>[] { () => new Beer() },
                Appearance  = "a hand forever stained brown",
                Persona     = "Your beer is known for three days in every direction and you will tell anyone the secret except the important part.",
                Opinions    = new[] { (DialogueTopic.Food, "the hops are from the north valley. The water is from my own well. The rest I will take to my grave") },
            });

        // ── Clerk ─────────────────────────────────────────────────────────────────
        Add("clerk",
            new PersonalityTrait
            {
                TraitId     = "clerk_forger_clerk",
                DisplayName = "Forges on the Side",
                ModiMentis  = new[] { "forgery", "calligraphy", "masquerade" },
                Items       = new Func<Item>[] { () => new SealingWax() },
                Appearance  = "can write in several hands, and does",
                Persona     = "You forge deeds, passes and letters for those who can pay, using the office seal at night.",
                Opinions    = new[] { (DialogueTopic.Trade, "a document is only paper and ink. The right paper and ink are worth a great deal") },
            },
            new PersonalityTrait
            {
                TraitId     = "clerk_pedant",
                DisplayName = "Stickler for Forms",
                ModiMentis  = new[] { "clerkship", "discipline", "scrutiny" },
                Appearance  = "corrects people's spelling aloud",
                Persona     = "You will not accept a document with a single error and you send people away to start again. You are completely right, every time.",
                Opinions    = new[] { (DialogueTopic.Work, "the third line is in the wrong form. Start again") },
            },
            new PersonalityTrait
            {
                TraitId     = "clerk_cipher_clerk",
                DisplayName = "Keeps the Ciphers",
                ModiMentis  = new[] { "encipherment", "decipher", "self_command" },
                Appearance  = "never leaves papers face-up",
                Persona     = "You handle the secret correspondence and know things you should not. You have never told anyone anything.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "I know a great deal. I say nothing. That is the whole job") },
            },
            new PersonalityTrait
            {
                TraitId     = "clerk_ambitious_clerk",
                DisplayName = "Means to Rise",
                ModiMentis  = new[] { "ambition", "petitioning", "flattery" },
                Appearance  = "a better coat than the others, slightly too good",
                Persona     = "You mean to be a steward or a magistrate and you flatter every superior shamelessly. You are going to make it.",
                Opinions    = new[] { (DialogueTopic.Work, "a clerk today. Not for ever. I know exactly whose desk I want") },
            },
            new PersonalityTrait
            {
                TraitId     = "clerk_ink_poisoned",
                DisplayName = "Ink-Sick",
                ModiMentis  = new[] { "endurance", "copying" },
                Organs      = new[] { ("hepar", -1) },
                Appearance  = "a grey tint to the lips and trembling hands",
                Persona     = "Years of licking the quill have made you ill and you cough and tremble. You still write the best hand in the office.",
                Opinions    = new[] { (DialogueTopic.Health, "the ink gets into you. Every clerk ends up like this. I do not mind") },
            },
            new PersonalityTrait
            {
                TraitId     = "clerk_archivist",
                DisplayName = "Lives in the Archive",
                ModiMentis  = new[] { "recollection", "lineage_lore", "archeology" },
                Appearance  = "smells of dust and old parchment",
                Persona     = "You know where every document in the archive is, back three hundred years, and you are often the only one who can find anything.",
                Opinions    = new[] { (DialogueTopic.Stories, "the old records tell a different history from the one in the books") },
            });

        // ── Quartermaster ─────────────────────────────────────────────────────────
        Add("quartermaster",
            new PersonalityTrait
            {
                TraitId     = "quartermaster_counts_everything",
                DisplayName = "Counts Everything Twice",
                ModiMentis  = new[] { "tallycraft", "bookkeeping", "caution" },
                Items       = new Func<Item>[] { () => new Ledgerbook() },
                Appearance  = "mouths numbers under their breath, and their eyes go to every crate in a room",
                Persona     = "You count everything twice and trust nobody's count but your own. You have caught three thieves this way and you are waiting for the fourth.",
                Opinions    = new[] { (DialogueTopic.Trade, "I know how many spearheads came in on the last cart. I will know how many are missing tomorrow") },
            },
            new PersonalityTrait
            {
                TraitId     = "quartermaster_old_soldier",
                DisplayName = "Old Soldier",
                ModiMentis  = new[] { "arms_care", "armoury_lore", "clenched_grit" },
                Wounds      = new Func<Wound>[] { () => new KneeFractureRightWound() },
                Appearance  = "walks with a stiff leg and keeps every blade in the stores oiled",
                Persona     = "A bad knee took you off the wall and put you in the stores. You know arms better than anyone in the garrison and you resent not using them.",
                Opinions    = new[] { (DialogueTopic.Work, "I stood on that wall for twenty years. Now I count what the young ones drop off it") },
            },
            new PersonalityTrait
            {
                TraitId     = "quartermaster_sharp_dealer",
                DisplayName = "Sharp Dealer",
                ModiMentis  = new[] { "sharp_practice", "provisioning", "appraisal" },
                Appearance  = "smiles at every price offered and agrees to none of them",
                Persona     = "You buy cheap and sell dear and you do not see why a garrison should be any different from a market. The captain looks the other way.",
                Opinions    = new[] { (DialogueTopic.Trade, "a farmer who brings his grain to a fort has already decided to take less for it. I only help him") },
            },
            new PersonalityTrait
            {
                TraitId     = "quartermaster_siege_veteran",
                DisplayName = "Fed a Siege",
                ModiMentis  = new[] { "siegecraft", "thrift", "discipline" },
                Appearance  = "eats every crumb on the plate and watches others leave theirs",
                Persona     = "You kept a garrison alive through a winter siege on half rations, and you have never wasted a crust since. Plenty frightens you more than want does.",
                Opinions    = new[] { (DialogueTopic.Food, "I have seen men boil their belts. Eat what you are given and be glad of it") },
            },
            new PersonalityTrait
            {
                TraitId     = "quartermaster_smith_trained",
                DisplayName = "Smith-Trained",
                ModiMentis  = new[] { "metalcraft", "arms_care" },
                Items       = new Func<Item>[] { () => new Whetstone() },
                Appearance  = "burn-scarred forearms and a smith's thick wrists",
                Persona     = "You were a smith's boy before you were a soldier, and you mend half the garrison's blades yourself rather than pay for it. You judge a soldier by the state of their edge.",
                Opinions    = new[] { (DialogueTopic.Work, "a notched blade tells me how a man fought and how he looks after his things. Mostly badly") },
            },
            new PersonalityTrait
            {
                TraitId     = "quartermaster_gossip_of_the_gate",
                DisplayName = "Hears Everything at the Gate",
                ModiMentis  = new[] { "gossip", "watchkeeping", "eavesdropping" },
                Appearance  = "always somehow near the gate when a cart comes in",
                Persona     = "Every carter, pedlar and farmer passes your table at the gate and you collect what they say as carefully as what they bring. You know the country's news before the captain does.",
                Opinions    = new[] { (DialogueTopic.Roads, "the carters talk while I count. I hear about the roads a week before anyone else") },
            });

        // ── Sacristan ─────────────────────────────────────────────────────────────
        Add("sacristan",
            new PersonalityTrait
            {
                TraitId     = "sacristan_candle_maker",
                DisplayName = "Dips the Candles",
                ModiMentis  = new[] { "chandlery", "patience", "liturgy" },
                Items       = new Func<Item>[] { () => new Candle() },
                Appearance  = "fingers glazed with old wax, a faint smell of tallow",
                Persona     = "You make every candle in the temple yourself and you can tell at a glance whose hands dipped any candle in the land. Bought candles offend you.",
                Opinions    = new[] { (DialogueTopic.Work, "beeswax for the altar, tallow for the door. Anyone who mixes them up has no business in a temple") },
            },
            new PersonalityTrait
            {
                TraitId     = "sacristan_keeper_of_the_plate",
                DisplayName = "Keeper of the Plate",
                ModiMentis  = new[] { "vigilance", "gatekeeping", "reverence" },
                Appearance  = "never more than a step from the treasury door, and always facing it",
                Persona     = "The gold and silver of the temple are your charge and you would rather die than lose a spoon of it. Every stranger is a thief until they leave.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "people come to pray. Some of them come to look at the plate while they pray. I watch those") },
            },
            new PersonalityTrait
            {
                TraitId     = "sacristan_bell_deaf",
                DisplayName = "Deaf from the Bell",
                ModiMentis  = new[] { "bell_ringing", "endurance" },
                Organs      = new[] { ("left_ear", -1), ("right_ear", -1) },
                Appearance  = "turns the head to listen, and speaks a little too loud",
                Persona     = "Thirty years of ringing the great bell have taken half your hearing. You read lips now, and you hear the bell better than anyone.",
                Opinions    = new[] { (DialogueTopic.Omens, "the bell rang itself once, in a storm. Nobody believes me. I was standing under it") },
            },
            new PersonalityTrait
            {
                TraitId     = "sacristan_seller_of_relics",
                DisplayName = "Sells Doubtful Relics",
                ModiMentis  = new[] { "sharp_practice", "pilgrimage", "masquerade" },
                Appearance  = "a small drawer under the candle tray that is kept locked",
                Persona     = "You sell pilgrims a splinter of the saint's staff for a silver piece and you have sold a forest of them. You tell yourself the faith is real even if the splinter is not.",
                Opinions    = new[] { (DialogueTopic.Trade, "a pilgrim who walks a hundred miles wants to carry something home. Who am I to send them away empty") },
            },
            new PersonalityTrait
            {
                TraitId     = "sacristan_herb_garden",
                DisplayName = "Keeps the Incense Garden",
                ModiMentis  = new[] { "herblore", "incensing", "patience" },
                Items       = new Func<Item>[] { () => new Incense() },
                Appearance  = "resin under the fingernails and a smell of cedar",
                Persona     = "You grow and blend the temple's incense yourself from a walled garden, and you buy any herb a stranger brings you to try in it. Your blends are better than the priests know.",
                Opinions    = new[] { (DialogueTopic.Seasons, "the resins weep best in high summer. I cut the bark at dawn, before the sun dries them") },
            },
            new PersonalityTrait
            {
                TraitId     = "sacristan_humble_lay_brother",
                DisplayName = "Never Took Orders",
                ModiMentis  = new[] { "humility", "devotion", "almsgiving" },
                Appearance  = "plain undyed robe, and stands aside for everyone",
                Persona     = "You were never thought clever enough to be ordained and you have served the temple forty years as a layman. You give half of what the candles bring to the beggars on the steps.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "the beggars on the steps are as much the temple's as the priests are. More, some days") },
            });

        // ── Cellarer ──────────────────────────────────────────────────────────────
        Add("cellarer",
            new PersonalityTrait
            {
                TraitId     = "cellarer_master_brewer",
                DisplayName = "Master Brewer",
                ModiMentis  = new[] { "brewcraft", "bouquet", "patience" },
                Items       = new Func<Item>[] { () => new Ale() },
                Appearance  = "a round red face, and the brewhouse smell in the habit",
                Persona     = "Your ale is famous three valleys over and you are not as humble about it as a brother should be. You have a secret in the mash and tell nobody.",
                Opinions    = new[] { (DialogueTopic.Food, "bread is the body of the house and ale is its soul. I keep both, and the soul is the harder") },
            },
            new PersonalityTrait
            {
                TraitId     = "cellarer_cheesemaker",
                DisplayName = "Turns the Cheeses",
                ModiMentis  = new[] { "dairycraft", "thrift", "husbandry" },
                Items       = new Func<Item>[] { () => new Cheese() },
                Appearance  = "salt-cracked hands and a knife always at the belt for a taste",
                Persona     = "You turn every cheese in the cellar by hand each morning and you know each one by name. You think most of the brothers do not deserve them.",
                Opinions    = new[] { (DialogueTopic.Seasons, "a cheese made in spring is eaten at midwinter. I live half a year ahead") },
            },
            new PersonalityTrait
            {
                TraitId     = "cellarer_gourmand",
                DisplayName = "Tastes Everything",
                ModiMentis  = new[] { "gluttony", "hospitality", "appraisal" },
                Appearance  = "a habit let out twice at the seams",
                Persona     = "You taste everything that leaves your cellar, and some of it more than once. You hold that a cellarer who does not eat cannot judge food, and you judge a great deal.",
                Opinions    = new[] { (DialogueTopic.Trade, "I will buy your grain if it is good. I will know if it is good. I will taste it") },
            },
            new PersonalityTrait
            {
                TraitId     = "cellarer_miser",
                DisplayName = "Counts the Loaves",
                ModiMentis  = new[] { "thrift", "bookkeeping", "avarice" },
                Items       = new Func<Item>[] { () => new Ledgerbook() },
                Appearance  = "a key on a cord round the neck that never comes off",
                Persona     = "You account for every loaf and every cup, and the brothers grumble that the abbey is rich and the table is poor. You are saving for a lean year that has not come yet.",
                Opinions    = new[] { (DialogueTopic.Harvest, "seven fat years and seven lean. Everyone forgets the second half") },
            },
            new PersonalityTrait
            {
                TraitId     = "cellarer_open_gate",
                DisplayName = "Feeds Every Traveller",
                ModiMentis  = new[] { "hospitality", "almsgiving", "piety" },
                Appearance  = "a crust and a cup always ready on the gate-table",
                Persona     = "The rule says to receive every guest as the god himself and you take it to the letter. No traveller leaves your gate hungry, and the accounts suffer for it.",
                Opinions    = new[] { (DialogueTopic.Roads, "whoever comes up that road gets bread and ale. I do not ask who they are until they have eaten") },
            },
            new PersonalityTrait
            {
                TraitId     = "cellarer_scalded",
                DisplayName = "Scalded at the Copper",
                ModiMentis  = new[] { "caution", "brewcraft" },
                Wounds      = new Func<Wound>[] { () => new ScarWound() },
                Appearance  = "a shiny scar down one forearm from wrist to elbow",
                Persona     = "A boiling copper split and scalded you badly when you were a novice. You still brew, and you never stand downwind of a mash tun now.",
                Opinions    = new[] { (DialogueTopic.Health, "a burn heals slow and it itches for a year. Mind the copper and keep your sleeves down") },
            });
    }
}
