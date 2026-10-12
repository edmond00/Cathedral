using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;

namespace Cathedral.Game.Npc.Traits;

/// <summary>Reserved traits for the people of a dense city: cobbler, tailor, chandler, butcher, tanner, potter, apothecary, barber, porter, laundress, water-carrier, beggar.</summary>
public sealed partial class PersonalityTraitRegistry
{
    private void RegisterCityTraits()
    {
        // ── Cobbler ───────────────────────────────────────────────────────────────
        Add("cobbler",
            new PersonalityTrait
            {
                TraitId     = "cobbler_reads_heels",
                DisplayName = "Reads Heels",
                ModiMentis  = new[] { "wear_reading", "physiognomy", "scrutiny" },
                Appearance  = "looks at your feet before your face",
                Persona     = "You can tell a man's whole life from the wear on his heels - his gait, his trade, his drinking - and you cannot stop yourself saying so.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "the left heel worn to nothing, the right untouched. He limps to hide something. I know what") },
            },
            new PersonalityTrait
            {
                TraitId     = "cobbler_hunched",
                DisplayName = "Bent at the Bench",
                ModiMentis  = new[] { "diligence", "endurance" },
                Wounds      = new Func<Wound>[] { () => new BrokenBackboneWound() },
                Appearance  = "a back bent like a hook from thirty years at the low bench",
                Persona     = "Your back has set in the shape of your work and you can no longer stand fully straight. You do not complain; you sit down.",
                Opinions    = new[] { (DialogueTopic.Health, "I have not stood straight since my apprenticeship. The bench takes its due") },
            },
            new PersonalityTrait
            {
                TraitId     = "cobbler_old_soles",
                DisplayName = "Sells Old Soles as New",
                ModiMentis  = new[] { "sharp_practice", "masquerade", "thrift" },
                Items       = new Func<Item>[] { () => new Hobnails() },
                Appearance  = "a pile of old boots in the back that never seems to get smaller",
                Persona     = "You buy dead men's boots for coppers, black them, re-heel them and sell them as new. Nobody has caught you yet.",
                Opinions    = new[] { (DialogueTopic.Trade, "new, of course they are new. Smell the blacking") },
            },
            new PersonalityTrait
            {
                TraitId     = "cobbler_fine_work",
                DisplayName = "Makes for the Gentry",
                ModiMentis  = new[] { "cobbling", "hallmark", "pride" },
                Items       = new Func<Item>[] { () => new BespokeShoes() },
                Appearance  = "a pair of exquisite shoes on display that are not for sale",
                Persona     = "You make shoes for the best houses in the town and you know you are the best cobbler in the province. You make sure everyone else knows it too.",
                Opinions    = new[] { (DialogueTopic.Work, "the lord's steward sends for me twice a year. Twice a year, mind") },
            },
            new PersonalityTrait
            {
                TraitId     = "cobbler_former_soldier",
                DisplayName = "Marched in the Wars",
                ModiMentis  = new[] { "soldiery", "war_weariness", "endurance" },
                Appearance  = "an old army boot nailed over the door",
                Persona     = "You marched a thousand miles in bad boots in the last war and swore you would never let anyone suffer that again. You still give soldiers a discount.",
                Opinions    = new[] { (DialogueTopic.Roads, "I walked to the border and back in boots that split on the first day. Never again, for anyone") },
            },
            new PersonalityTrait
            {
                TraitId     = "cobbler_philosopher",
                DisplayName = "Bench Philosopher",
                ModiMentis  = new[] { "philosophy", "disputation", "patience" },
                Appearance  = "argues about the gods while hammering in a heel",
                Persona     = "Long hours of quiet work have made you a thinker, and you will argue about the nature of the soul with anyone who stands still long enough.",
                Opinions    = new[] { (DialogueTopic.Stories, "every man is a shoe. Made for one foot, worn by another, mended until there is nothing of the first left") },
            });

        // ── Tailor ────────────────────────────────────────────────────────────────
        Add("tailor",
            new PersonalityTrait
            {
                TraitId     = "tailor_snob",
                DisplayName = "Dresses Above Their Station",
                ModiMentis  = new[] { "high_society_manners", "vanitas", "disdain" },
                Items       = new Func<Item>[] { () => new Waistcoat() },
                Appearance  = "dressed better than most of the customers",
                Persona     = "You dress like a gentleman and speak like one, and you look down on anyone who does not, which is nearly everyone who comes through the door.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "half this street dresses like it has never seen a mirror. The other half, worse") },
            },
            new PersonalityTrait
            {
                TraitId     = "tailor_skims_cloth",
                DisplayName = "Keeps the Remnants",
                ModiMentis  = new[] { "sharp_practice", "thrift", "tailoring" },
                Items       = new Func<Item>[] { () => new DyedCloth() },
                Appearance  = "a chest of fine remnants under the table",
                Persona     = "You always ask for a yard more cloth than the work needs and keep the difference. Your own children dress very well.",
                Opinions    = new[] { (DialogueTopic.Trade, "three yards for a coat? No, four, to be safe. Four") },
            },
            new PersonalityTrait
            {
                TraitId     = "tailor_gossip",
                DisplayName = "Knows Every Measure",
                ModiMentis  = new[] { "gossip", "eavesdropping", "physiognomy" },
                Appearance  = "talks about other customers with pins in their mouth",
                Persona     = "Everyone undresses in front of the tailor, and everyone talks while they do. You remember all of it and repeat most of it.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "the merchant's wife has let her gowns out twice since spring. Draw your own conclusions") },
            },
            new PersonalityTrait
            {
                TraitId     = "tailor_short_sighted",
                DisplayName = "Needle-Blind",
                ModiMentis  = new[] { "steady_hand", "patience" },
                Items       = new Func<Item>[] { () => new ReadingLenses() },
                Appearance  = "peers at faces from a hand's breadth away",
                Persona     = "Forty years of fine stitching have ruined your eyes for anything further off than your needle. You recognise people by their coats.",
                Opinions    = new[] { (DialogueTopic.Health, "I can thread a needle in candlelight and I cannot tell you who is standing at the door") },
            },
            new PersonalityTrait
            {
                TraitId     = "tailor_fashion",
                DisplayName = "Follows the Capital's Fashions",
                ModiMentis  = new[] { "aesthetic", "ambition", "worldliness" },
                Appearance  = "talks constantly of what they are wearing in the capital",
                Persona     = "You copy every fashion that reaches the town from the capital, a year late, and you believe that makes you the height of taste.",
                Opinions    = new[] { (DialogueTopic.Stories, "in the capital they slash the sleeves to show the lining. It will come here. I will be ready") },
            },
            new PersonalityTrait
            {
                TraitId     = "tailor_shrouds",
                DisplayName = "Sews Shrouds Too",
                ModiMentis  = new[] { "obsequies", "condolence", "threadwork" },
                Appearance  = "a stack of plain linen folded apart from the rest",
                Persona     = "You sew wedding clothes and shrouds and you take the same care over both. You have been at more deathbeds than the priest.",
                Opinions    = new[] { (DialogueTopic.Kin, "I made her christening gown and her wedding dress and her shroud. Same hands, same care") },
            });

        // ── Chandler ──────────────────────────────────────────────────────────────
        Add("chandler",
            new PersonalityTrait
            {
                TraitId     = "chandler_church_supplier",
                DisplayName = "Supplies the Church",
                ModiMentis  = new[] { "piety", "liturgy", "chandlery" },
                Items       = new Func<Item>[] { () => new Beeswax() },
                Appearance  = "a beeswax taper behind one ear, as if it were a pen",
                Persona     = "You make the church's candles and you are prouder of that contract than of anything else in your life. You go to every office to see them lit.",
                Opinions    = new[] { (DialogueTopic.Work, "the tapers on the high altar are mine. Every one. Pure wax, not a drop of tallow") },
            },
            new PersonalityTrait
            {
                TraitId     = "chandler_cut_tallow",
                DisplayName = "Cuts the Tallow",
                ModiMentis  = new[] { "sharp_practice", "thrift" },
                Items       = new Func<Item>[] { () => new Tallow() },
                Appearance  = "candles that smoke rather more than they should",
                Persona     = "You stretch the good tallow with whatever fat the butcher throws out. Your candles stink, but they are cheap, and the poor buy them.",
                Opinions    = new[] { (DialogueTopic.Trade, "a candle is a candle. If you want it not to smoke, buy wax") },
            },
            new PersonalityTrait
            {
                TraitId     = "chandler_burned",
                DisplayName = "Burned by the Vat",
                ModiMentis  = new[] { "caution", "firecraft" },
                Wounds      = new Func<Wound>[] { () => new ScarWound() },
                Appearance  = "a shiny scar down one forearm from spilled fat",
                Persona     = "You upset a vat of boiling tallow as an apprentice and the scar never let you forget it. You are careful now - painfully, slowly careful.",
                Opinions    = new[] { (DialogueTopic.Health, "hot fat clings. Water does nothing. Remember it") },
            },
            new PersonalityTrait
            {
                TraitId     = "chandler_night_owl",
                DisplayName = "Keeps Late Hours",
                ModiMentis  = new[] { "night_ear", "vigil", "stillness" },
                Appearance  = "a candle always burning in the window long after dark",
                Persona     = "You work best at night with your own light, when the street is quiet. You have seen a great many things pass your window after curfew.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "I see who goes where after the bell. I am the only one awake to see it") },
            },
            new PersonalityTrait
            {
                TraitId     = "chandler_soap_maker",
                DisplayName = "Makes Soap Too",
                ModiMentis  = new[] { "perfumery", "enterprise", "chandlery" },
                Items       = new Func<Item>[] { () => new Soap() },
                Appearance  = "smells, under the tallow, of lavender",
                Persona     = "You have found that ash and fat make soap as well as candles, and you are trying perfumes on the gentry with some success.",
                Opinions    = new[] { (DialogueTopic.Trade, "lavender soap for the merchants' wives. Same fat as the candles. Ten times the price") },
            },
            new PersonalityTrait
            {
                TraitId     = "chandler_gloomy",
                DisplayName = "Candles for the Dead",
                ModiMentis  = new[] { "mourning", "superstition", "elegy" },
                Items       = new Func<Item>[] { () => new VotiveCandle() },
                Appearance  = "talks about death as if it were the weather",
                Persona     = "Most of your candles light deathbeds and wakes, and you have grown gloomy and fond of the dead. You are oddly good company at a funeral.",
                Opinions    = new[] { (DialogueTopic.Omens, "a candle that gutters with no draught - I sold three for a wake the next day. Every time") },
            });

        // ── Butcher ───────────────────────────────────────────────────────────────
        Add("butcher",
            new PersonalityTrait
            {
                TraitId     = "butcher_big_heart",
                DisplayName = "Feeds the Poor",
                ModiMentis  = new[] { "almsgiving", "largesse", "empathy" },
                Items       = new Func<Item>[] { () => new Tripe() },
                Appearance  = "a crowd of children and beggars at the back door every evening",
                Persona     = "What does not sell by evening goes out the back door to whoever is hungry. You tell everyone it is to keep the rats down.",
                Opinions    = new[] { (DialogueTopic.Food, "nobody starves on my street. Not while I have offal") },
            },
            new PersonalityTrait
            {
                TraitId     = "butcher_thumb_on_scale",
                DisplayName = "Thumb on the Scale",
                ModiMentis  = new[] { "sharp_practice", "cutpurse", "avarice" },
                Items       = new Func<Item>[] { () => new Scales() },
                Appearance  = "a big thumb that rests on the scale pan while it weighs",
                Persona     = "You have weighed with your thumb for so long you no longer notice you do it. Your customers notice.",
                Opinions    = new[] { (DialogueTopic.Trade, "a pound is a pound, near enough. Who is counting?") },
            },
            new PersonalityTrait
            {
                TraitId     = "butcher_nine_fingers",
                DisplayName = "Nine Fingers",
                ModiMentis  = new[] { "butchery", "clenched_grit" },
                Wounds      = new Func<Wound>[] { () => new FingersAmputeeLeftWound() },
                Appearance  = "a hand short of a finger, held up proudly as a warning to apprentices",
                Persona     = "You lost a finger to the cleaver as a young man and you show the stump to every apprentice on their first day. None of them has lost one since.",
                Opinions    = new[] { (DialogueTopic.Work, "look at this hand. That is what hurrying costs. Now go slowly") },
            },
            new PersonalityTrait
            {
                TraitId     = "butcher_slaughterman",
                DisplayName = "Quick Killer",
                ModiMentis  = new[] { "anatomy_lore", "cold_blood", "beast_sense" },
                Appearance  = "beasts go quiet when they hear the voice",
                Persona     = "You kill a beast in one stroke with no fuss, and you think that a matter of honour. You despise anyone who makes an animal suffer.",
                Opinions    = new[] { (DialogueTopic.Beasts, "one stroke, no fear, no pain. Anything else is cruelty and I will not have it in my yard") },
            },
            new PersonalityTrait
            {
                TraitId     = "butcher_brawler",
                DisplayName = "Shambles Brawler",
                ModiMentis  = new[] { "brawling", "brute_force", "temerity" },
                Wounds      = new Func<Wound>[] { () => new BrokenNoseWound() },
                Appearance  = "a nose broken flat and forearms like hams",
                Persona     = "The butchers' street fights the tanners' street every feast day and you have led the charge for twenty years. You are proud of every scar.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "the tanners think they are hard. They are not. We have the cleavers") },
            },
            new PersonalityTrait
            {
                TraitId     = "butcher_sausage_maker",
                DisplayName = "Famous for Sausage",
                ModiMentis  = new[] { "cookery", "gourmandise", "pride" },
                Items       = new Func<Item>[] { () => new BloodPudding() },
                Appearance  = "strings of sausage hanging from every beam",
                Persona     = "Your sausages are known across the quarter and the recipe is a family secret three generations old. You will not even tell your apprentice.",
                Opinions    = new[] { (DialogueTopic.Food, "pepper, sage and something else. The something else is mine") },
            });

        // ── Tanner ────────────────────────────────────────────────────────────────
        Add("tanner",
            new PersonalityTrait
            {
                TraitId     = "tanner_rich_and_shunned",
                DisplayName = "Rich and Shunned",
                ModiMentis  = new[] { "avarice", "misanthropy", "pride" },
                Items       = new Func<Item>[] { () => new CoinPurse() },
                Appearance  = "a heavy purse and nobody to spend it with",
                Persona     = "You are richer than most merchants and no decent family will have you to dinner. You have stopped caring, mostly.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "they hold their noses when I pass and come to me when they need a loan") },
            },
            new PersonalityTrait
            {
                TraitId     = "tanner_lime_hands",
                DisplayName = "Lime-Eaten Hands",
                ModiMentis  = new[] { "tanning", "endurance", "dirty_labor" },
                Items       = new Func<Item>[] { () => new LeatherGloves() },
                Appearance  = "hands cracked and white with lime",
                Persona     = "The lime has eaten your hands to leather and you feel almost nothing through them now. You can pick up a hot coal and not notice for a moment.",
                Opinions    = new[] { (DialogueTopic.Health, "feel these. Like old boots. The lime does that, and worse to the lungs") },
            },
            new PersonalityTrait
            {
                TraitId     = "tanner_dog_dung",
                DisplayName = "Buys Dog Dung",
                ModiMentis  = new[] { "dirty_labor", "thrift", "streetwise" },
                Appearance  = "children bring buckets to the yard gate for coppers",
                Persona     = "You pay the street children to collect dog dung for the bating pits, and so every urchin in town knows you and tells you everything.",
                Opinions    = new[] { (DialogueTopic.Trade, "a copper a bucket. The children love me. Their mothers do not") },
            },
            new PersonalityTrait
            {
                TraitId     = "tanner_saddle_leather",
                DisplayName = "Best Leather in the Province",
                ModiMentis  = new[] { "hallmark", "patience", "tanning" },
                Items       = new Func<Item>[] { () => new TannedLeather() },
                Appearance  = "runs a thumb over every hide that leaves the yard",
                Persona     = "Your leather is a year in the pits and not a day less, and the saddlers of three towns buy nothing else. You will not hurry for any price.",
                Opinions    = new[] { (DialogueTopic.Work, "a year in the pits. Not eleven months. A year") },
            },
            new PersonalityTrait
            {
                TraitId     = "tanner_feud",
                DisplayName = "Feuds with the Butchers",
                ModiMentis  = new[] { "grudgekeeping", "brawling", "invective" },
                Appearance  = "spits whenever the butchers' street is mentioned",
                Persona     = "The butchers cheat you on hides and their street fights yours every feast day. The feud is older than you are and you mean to win it.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "the butchers sell me hides full of cuts and charge me for whole ones. One day") },
            },
            new PersonalityTrait
            {
                TraitId     = "tanner_cough",
                DisplayName = "Pit Cough",
                ModiMentis  = new[] { "iron_stomach", "clenched_grit" },
                Wounds      = new Func<Wound>[] { () => new ContusionWound() },
                Appearance  = "a rattling cough that never quite stops",
                Persona     = "The fumes of the pits have got into your chest and you cough day and night. You expect it to kill you and are not in a hurry about it.",
                Opinions    = new[] { (DialogueTopic.Health, "every tanner coughs. The ones who stop coughing are in the churchyard") },
            });

        // ── Potter ────────────────────────────────────────────────────────────────
        Add("potter",
            new PersonalityTrait
            {
                TraitId     = "potter_rings_true",
                DisplayName = "Hears the Crack",
                ModiMentis  = new[] { "keen_ear", "potcraft", "hallmark" },
                Appearance  = "taps every pot and listens with head cocked",
                Persona     = "You can hear a hairline crack in a jug across the room and you will not let a flawed pot leave the shop, however good it looks.",
                Opinions    = new[] { (DialogueTopic.Work, "listen. Hear that? Dull. Cracked inside. It goes on the heap") },
            },
            new PersonalityTrait
            {
                TraitId     = "potter_kiln_keeper",
                DisplayName = "Master of the Kiln",
                ModiMentis  = new[] { "firecraft", "smoke_reading", "vigil" },
                Appearance  = "eyebrows singed off, eyes red from watching flames",
                Persona     = "You sit up three nights with every firing, watching the colour of the flame, and you have never lost a kiln-load. The others say you talk to the fire.",
                Opinions    = new[] { (DialogueTopic.Seasons, "the big firing is next week. I will not sleep for three nights. I never do") },
            },
            new PersonalityTrait
            {
                TraitId     = "potter_artist",
                DisplayName = "Paints the Ware",
                ModiMentis  = new[] { "aesthetic", "iconography", "illumination" },
                Items       = new Func<Item>[] { () => new GlazedBowl() },
                Appearance  = "bowls painted with birds and vines among the plain ware",
                Persona     = "You paint birds and vines on your best ware and you would rather do that than throw a hundred plain jugs. Your accounts suffer for it.",
                Opinions    = new[] { (DialogueTopic.Trade, "they buy the plain jugs. They keep the painted bowls. I know which matters") },
            },
            new PersonalityTrait
            {
                TraitId     = "potter_calm",
                DisplayName = "Calm as Clay",
                ModiMentis  = new[] { "meditation", "patience", "stillness" },
                Appearance  = "moves slowly and never raises their voice",
                Persona     = "The wheel has made you calm. Nothing hurries you and nothing angers you, and people come to sit in the shop just to be near it.",
                Opinions    = new[] { (DialogueTopic.Rest, "sit at a wheel for an hour. You will forget whatever was troubling you") },
            },
            new PersonalityTrait
            {
                TraitId     = "potter_clay_secret",
                DisplayName = "Secret Clay Bed",
                ModiMentis  = new[] { "spadework", "bushcraft", "covetousness" },
                Items       = new Func<Item>[] { () => new Clay() },
                Appearance  = "mud on the boots from somewhere outside the walls",
                Persona     = "You dig your clay from a bank outside town that nobody else knows about, and you go there before dawn so nobody will follow you.",
                Opinions    = new[] { (DialogueTopic.Wilds, "the best clay is out past the walls. Where exactly is my business") },
            },
            new PersonalityTrait
            {
                TraitId     = "potter_broken_pots",
                DisplayName = "Collects Shards",
                ModiMentis  = new[] { "archeology", "curiosity", "provenance" },
                Items       = new Func<Item>[] { () => new Potsherd() },
                Appearance  = "a shelf of ancient broken pottery dug up around the town",
                Persona     = "You collect old shards that turn up in gardens and ditches and you can tell which are a hundred years old and which a thousand.",
                Opinions    = new[] { (DialogueTopic.Stories, "this shard is older than the empire. Someone drank from it before there was a town here") },
            });

        // ── Apothecary ────────────────────────────────────────────────────────────
        Add("apothecary",
            new PersonalityTrait
            {
                TraitId     = "apothecary_quack",
                DisplayName = "Sells Coloured Water",
                ModiMentis  = new[] { "sharp_practice", "rhetoric", "masquerade" },
                Items       = new Func<Item>[] { () => new Theriac() },
                Appearance  = "a great many very impressive bottles",
                Persona     = "Half your remedies are coloured water and confident talk, and you have found that the talk cures about as many people as the medicine.",
                Opinions    = new[] { (DialogueTopic.Health, "belief is the greatest part of any remedy. I simply supply the belief") },
            },
            new PersonalityTrait
            {
                TraitId     = "apothecary_poisoner",
                DisplayName = "Knows the Dark Simples",
                ModiMentis  = new[] { "poisoning", "herblore", "intrigue" },
                Appearance  = "a locked cabinet that is never opened in front of customers",
                Persona     = "You know exactly what kills and in what dose, and you have sold that knowledge more than once to people who said it was for rats.",
                Opinions    = new[] { (DialogueTopic.Trade, "arsenic for the rats. Of course. Two-legged or four, I do not ask") },
            },
            new PersonalityTrait
            {
                TraitId     = "apothecary_scholar",
                DisplayName = "Corresponds with Physicians",
                ModiMentis  = new[] { "scientific_research", "diagnosis", "scholarship" },
                Items       = new Func<Item>[] { () => new Parchment() },
                Appearance  = "ink-stained fingers and letters from the university",
                Persona     = "You write to physicians in the capital about every unusual case and you believe you are on the edge of a great discovery about fevers.",
                Opinions    = new[] { (DialogueTopic.Health, "the fevers come from bad air, the physicians say. I think it is the water. I am writing to them") },
            },
            new PersonalityTrait
            {
                TraitId     = "apothecary_alchemist",
                DisplayName = "Secret Alchemist",
                ModiMentis  = new[] { "alchemy", "gold_fever", "curiosity" },
                Appearance  = "a smell of sulphur from the back room",
                Persona     = "Behind the shop you have a furnace and an alembic and you are trying to make gold. You have spent your daughter's dowry on it.",
                Opinions    = new[] { (DialogueTopic.Trade, "one day I will not need to sell powders to fools. One day soon") },
            },
            new PersonalityTrait
            {
                TraitId     = "apothecary_kind",
                DisplayName = "Treats the Poor for Nothing",
                ModiMentis  = new[] { "mercy", "physic", "almsgiving" },
                Items       = new Func<Item>[] { () => new Herb() },
                Appearance  = "a queue of the poor at the back door on market days",
                Persona     = "You treat those who cannot pay one morning a week and lose money on it every time. It is the only part of the trade you are proud of.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "the rich pay for the poor here, though they do not know it") },
            },
            new PersonalityTrait
            {
                TraitId     = "apothecary_spice_trade",
                DisplayName = "Deals in Spices",
                ModiMentis  = new[] { "apothecary_nose", "brokerage", "appraisal" },
                Items       = new Func<Item>[] { () => new SpiceBundle() },
                Appearance  = "smells of pepper and cinnamon",
                Persona     = "The spice trade pays better than physic and you have quietly become a merchant in pepper and saffron, under the guise of medicine.",
                Opinions    = new[] { (DialogueTopic.Trade, "saffron is a medicine. It is also worth its weight in silver. Both things are true") },
            });

        // ── Barber ────────────────────────────────────────────────────────────────
        Add("barber",
            new PersonalityTrait
            {
                TraitId     = "barber_never_stops_talking",
                DisplayName = "Never Stops Talking",
                ModiMentis  = new[] { "gossip", "banter", "gregariousness" },
                Appearance  = "talking before the customer has sat down",
                Persona     = "You talk from the moment a customer sits until they leave, and you pass on every piece of news you heard from the last one.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "you heard about the guard captain's wife? No? Sit down, sit down, I will tell you") },
            },
            new PersonalityTrait
            {
                TraitId     = "barber_war_surgeon",
                DisplayName = "Army Sawbones",
                ModiMentis  = new[] { "surgery", "sangfroid", "war_weariness" },
                Items       = new Func<Item>[] { () => new Saw() },
                Appearance  = "a saw on the wall that is not for wood",
                Persona     = "You took off legs on a battlefield for six years and nothing in a town barber's shop can shock you. You do not speak of the war.",
                Opinions    = new[] { (DialogueTopic.Health, "a clean cut and hot pitch. I have done it in the rain with a man screaming. A tooth is nothing") },
            },
            new PersonalityTrait
            {
                TraitId     = "barber_bonesetter",
                DisplayName = "Sets Bones",
                ModiMentis  = new[] { "bonesetting", "anatomy_lore", "brute_force" },
                Appearance  = "big hands that can pull a shoulder back into place in one jerk",
                Persona     = "The farmhands and porters come to you when something is out of joint, and you put it back with one pull and a joke.",
                Opinions    = new[] { (DialogueTopic.Work, "a shoulder out is one pull. You will scream, and then you will thank me") },
            },
            new PersonalityTrait
            {
                TraitId     = "barber_teeth_collector",
                DisplayName = "Strings of Teeth",
                ModiMentis  = new[] { "tooth_drawing", "pride", "boasting" },
                Items       = new Func<Item>[] { () => new Bone() },
                Appearance  = "a string of drawn teeth hung in the window like a necklace",
                Persona     = "You keep every tooth you have ever drawn and string them in the window as proof of your skill. You are very proud of the string.",
                Opinions    = new[] { (DialogueTopic.Stories, "eleven hundred and four teeth on that string. The big one was a smith's") },
            },
            new PersonalityTrait
            {
                TraitId     = "barber_unsteady",
                DisplayName = "Drinks Before Noon",
                ModiMentis  = new[] { "carousal", "recklessness", "self_preservation" },
                Items       = new Func<Item>[] { () => new Beer() },
                Appearance  = "a hand that trembles until the second drink",
                Persona     = "You need a drink to steady your hand and two to steady your nerves. By the afternoon your work is excellent. In the morning, customers are wary.",
                Opinions    = new[] { (DialogueTopic.Rest, "a little something to steady the hand. Purely professional") },
            },
            new PersonalityTrait
            {
                TraitId     = "barber_leech_keeper",
                DisplayName = "Keeps Leeches",
                ModiMentis  = new[] { "physic", "creature_lore", "patience" },
                Appearance  = "a jar of live leeches on the counter",
                Persona     = "You keep a jar of leeches and swear by them over the lancet for the gentle cases. You have names for the oldest ones.",
                Opinions    = new[] { (DialogueTopic.Health, "a lancet for the strong, leeches for the delicate. This one is called Brother Thomas") },
            });

        // ── Porter ────────────────────────────────────────────────────────────────
        Add("porter",
            new PersonalityTrait
            {
                TraitId     = "porter_strongest",
                DisplayName = "Strongest Back in Town",
                ModiMentis  = new[] { "haulage", "brute_force", "boasting" },
                Appearance  = "shoulders as wide as a doorway",
                Persona     = "You can carry what two other porters cannot and you never let anyone forget it. You win wagers lifting barrels at the inn.",
                Opinions    = new[] { (DialogueTopic.Work, "two barrels at once, up the custom-house stair. Ask anyone. Ask them") },
            },
            new PersonalityTrait
            {
                TraitId     = "porter_ruined_back",
                DisplayName = "Back Going",
                ModiMentis  = new[] { "endurance", "clenched_grit", "dread" },
                Wounds      = new Func<Wound>[] { () => new BrokenBackboneWound() },
                Appearance  = "winces every time a load comes off the shoulder",
                Persona     = "Your back is going, as every porter's does, and you are terrified of the day you cannot lift. You hide the pain from anyone who might hire you.",
                Opinions    = new[] { (DialogueTopic.Health, "it is nothing. A twinge. I can carry anything you have. Anything") },
            },
            new PersonalityTrait
            {
                TraitId     = "porter_light_fingers",
                DisplayName = "Loads Grow Lighter",
                ModiMentis  = new[] { "petty_thief", "snatch", "masquerade" },
                Appearance  = "always a little something in the shirt at the end of a job",
                Persona     = "Nobody counts a sack of nuts or a crate of figs carefully, and a handful from each finds its way home with you. It adds up.",
                Opinions    = new[] { (DialogueTopic.Trade, "a load always arrives a little lighter than it left. The road takes its share") },
            },
            new PersonalityTrait
            {
                TraitId     = "porter_knows_every_stair",
                DisplayName = "Knows Every Shortcut",
                ModiMentis  = new[] { "throng", "rooftops", "topographia" },
                Appearance  = "vanishes into a passage nobody else noticed",
                Persona     = "You know every alley, passage and stair in the town and you can get a load from the gate to the quay faster than any cart.",
                Opinions    = new[] { (DialogueTopic.Roads, "the carts go round. I go through. There is always a through, if you know the town") },
            },
            new PersonalityTrait
            {
                TraitId     = "porter_guild_man",
                DisplayName = "Porters' Fraternity",
                ModiMentis  = new[] { "esprit_de_corps", "loyalty", "muster" },
                Appearance  = "a fraternity badge sewn on the cap",
                Persona     = "You belong to the porters' fraternity and you would fight anyone who undercuts its rates. When one of you is hurt, the rest feed his family.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "touch one porter and you will have all of us at your door") },
            },
            new PersonalityTrait
            {
                TraitId     = "porter_singer",
                DisplayName = "Sings Under Load",
                ModiMentis  = new[] { "shanty", "sense_of_humor", "hard_labor" },
                Appearance  = "always singing something rude while carrying",
                Persona     = "You sing while you carry - work songs, filthy songs, songs you make up about the customers - and it makes the loads lighter, you swear.",
                Opinions    = new[] { (DialogueTopic.Work, "sing and the load carries itself. Halfway, anyway") },
            });

        // ── Laundress ─────────────────────────────────────────────────────────────
        Add("laundress",
            new PersonalityTrait
            {
                TraitId     = "laundress_knows_secrets",
                DisplayName = "Reads the Washing",
                ModiMentis  = new[] { "wear_reading", "gossip", "scrutiny" },
                Appearance  = "holds every sheet up to the light before it goes in the tub",
                Persona     = "A household's linen tells you who is sick, who is with child, who has been fighting and who sleeps where. You know more than the priest.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "blood on a shirt that was not from cutting meat. I said nothing. I say nothing. Mostly") },
            },
            new PersonalityTrait
            {
                TraitId     = "laundress_raw_hands",
                DisplayName = "Lye-Raw Hands",
                ModiMentis  = new[] { "laundering", "endurance", "dirty_labor" },
                Items       = new Func<Item>[] { () => new Lye() },
                Appearance  = "hands red, cracked and bleeding at the knuckles",
                Persona     = "The lye has eaten your hands raw and they never heal. You work through it because there is nothing else to do.",
                Opinions    = new[] { (DialogueTopic.Health, "they bleed in winter. Every winter. You put your hands back in the tub anyway") },
            },
            new PersonalityTrait
            {
                TraitId     = "laundress_matchmaker",
                DisplayName = "Matchmaker of the Wash House",
                ModiMentis  = new[] { "intrigue", "empathy", "wheedling" },
                Appearance  = "always asking who is courting whom",
                Persona     = "You have made half the marriages in the quarter from the wash house, steering likely girls and lads together, and you are very proud of it.",
                Opinions    = new[] { (DialogueTopic.Kin, "the baker's boy and the cooper's girl. That was mine. And the two before") },
            },
            new PersonalityTrait
            {
                TraitId     = "laundress_widow",
                DisplayName = "Widowed Young",
                ModiMentis  = new[] { "mourning", "resolve", "thrift" },
                Appearance  = "a black ribbon tied at the wrist",
                Persona     = "Your husband drowned in the harbour when the children were small and you have raised them on washing alone. You will not marry again.",
                Opinions    = new[] { (DialogueTopic.Kin, "four children on washing money. People said it could not be done") },
            },
            new PersonalityTrait
            {
                TraitId     = "laundress_sharp_tongue",
                DisplayName = "Tongue Like a Wash Beetle",
                ModiMentis  = new[] { "invective", "insolence", "banter" },
                Items       = new Func<Item>[] { () => new WashBeetle() },
                Appearance  = "a laugh you can hear across the square",
                Persona     = "You have a tongue sharper than anything in the wash house and you use it on anyone who looks down on you, guards and merchants included.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "the clerk thinks he is a lord because he can write. I have seen his shirts") },
            },
            new PersonalityTrait
            {
                TraitId     = "laundress_cleans_blood",
                DisplayName = "Asks No Questions",
                ModiMentis  = new[] { "stoneface", "laundering", "self_preservation" },
                Appearance  = "takes any bundle without unrolling it in front of the customer",
                Persona     = "For an extra coin you wash anything and ask nothing, and some of what comes to you at night would hang its owner.",
                Opinions    = new[] { (DialogueTopic.Trade, "double for the night bundles. And I never saw the night bundles") },
            });

        // ── Water-carrier ─────────────────────────────────────────────────────────
        Add("water_carrier",
            new PersonalityTrait
            {
                TraitId     = "water_carrier_well_lore",
                DisplayName = "Knows Every Well",
                ModiMentis  = new[] { "water_bearing", "taint_sense", "water_voice" },
                Appearance  = "tastes the water from every well before drawing",
                Persona     = "You know every well in the town, which is sweet, which is foul and which goes bad in summer, and you are always right.",
                Opinions    = new[] { (DialogueTopic.Water, "taste it. Iron. And something else. Do not drink from that one after midsummer") },
            },
            new PersonalityTrait
            {
                TraitId     = "water_carrier_uneven_shoulders",
                DisplayName = "Yoke-Crooked",
                ModiMentis  = new[] { "endurance", "athletics" },
                Wounds      = new Func<Wound>[] { () => new ShoulderDislocationRightWound() },
                Appearance  = "one shoulder higher than the other from years under the yoke",
                Persona     = "Years under the yoke have set your shoulders crooked and you walk with a little sideways lean even without the buckets.",
                Opinions    = new[] { (DialogueTopic.Health, "the yoke shapes you. Look at any old water-carrier. We all lean the same way") },
            },
            new PersonalityTrait
            {
                TraitId     = "water_carrier_message_runner",
                DisplayName = "Carries More Than Water",
                ModiMentis  = new[] { "intrigue", "eavesdropping", "receiving" },
                Appearance  = "slips a folded note into a kitchen along with the water",
                Persona     = "You go into every kitchen in town and you carry letters for lovers and conspirators for a coin apiece. You have never read one. Nearly never.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "a note under the bucket, a coin on the step. I see nothing. I carry water") },
            },
            new PersonalityTrait
            {
                TraitId     = "water_carrier_drought_dread",
                DisplayName = "Fears the Dry Year",
                ModiMentis  = new[] { "dread", "sky_reading", "foresight" },
                Appearance  = "always looking at the sky for rain",
                Persona     = "You lived through the dry year when the wells failed and people died at the cisterns, and you watch the sky every day for it coming again.",
                Opinions    = new[] { (DialogueTopic.Weather, "no rain for three weeks. The well is a foot lower. I have seen this before") },
            },
            new PersonalityTrait
            {
                TraitId     = "water_carrier_waters_wine",
                DisplayName = "Sells to the Taverns",
                ModiMentis  = new[] { "sharp_practice", "tapstering", "banter" },
                Appearance  = "on very good terms with every innkeeper",
                Persona     = "The taverns pay you extra to bring water quietly through the back for the wine and the ale. You keep their secrets and drink free.",
                Opinions    = new[] { (DialogueTopic.Food, "where do you think the wine gets its strength? From me. Its weakness, I mean") },
            },
            new PersonalityTrait
            {
                TraitId     = "water_carrier_cheerful",
                DisplayName = "Cheerful on the Round",
                ModiMentis  = new[] { "gregariousness", "sense_of_humor", "friendship" },
                Appearance  = "greets everyone on the street by name",
                Persona     = "You know everyone on your round and you have a word for each of them. You are, without knowing it, the most liked person in the quarter.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "good people, every one. Well. Nearly every one") },
            });

        // ── Beggar ────────────────────────────────────────────────────────────────
        Add("beggar",
            new PersonalityTrait
            {
                TraitId     = "beggar_false_cripple",
                DisplayName = "False Cripple",
                ModiMentis  = new[] { "masquerade", "beggary", "dramaturgy" },
                Items       = new Func<Item>[] { () => new Crutch() },
                Appearance  = "a crutch that is not always on the same side",
                Persona     = "Your leg is perfectly sound, but a crutch doubles the alms. You have been caught running twice and talked your way out both times.",
                Opinions    = new[] { (DialogueTopic.Health, "my leg? A war wound. Or a fall. Which one did I tell you?") },
            },
            new PersonalityTrait
            {
                TraitId     = "beggar_fallen_merchant",
                DisplayName = "Was Once Rich",
                ModiMentis  = new[] { "high_society_manners", "elegy", "bookkeeping" },
                Appearance  = "speaks like a gentleman despite the rags",
                Persona     = "You were a merchant until a ship went down with everything you owned. You still speak and bow like one, and it embarrasses the people you beg from.",
                Opinions    = new[] { (DialogueTopic.Trade, "I had three ships once. The sea took two and my partner took the third") },
            },
            new PersonalityTrait
            {
                TraitId     = "beggar_eyes_of_the_street",
                DisplayName = "Eyes of the Street",
                ModiMentis  = new[] { "streetwise", "eavesdropping", "vigilance" },
                Appearance  = "watches every face that passes without seeming to",
                Persona     = "Nobody sees a beggar, so you see everybody, and for a coin you will tell anyone who went where with whom. The guards pay best.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "a copper for the poor? Two, and I will tell you who left the merchant's house at midnight") },
            },
            new PersonalityTrait
            {
                TraitId     = "beggar_holy_fool",
                DisplayName = "Holy Fool",
                ModiMentis  = new[] { "mysticism", "humility", "augury" },
                Appearance  = "talks to people nobody else can see",
                Persona     = "People say you are touched by the gods and they give to you for luck. Sometimes you say things about their futures that come true.",
                Opinions    = new[] { (DialogueTopic.Omens, "the crows told me. Three crows. You will travel, and lose something, and be glad of it") },
            },
            new PersonalityTrait
            {
                TraitId     = "beggar_old_soldier",
                DisplayName = "Discharged Soldier",
                ModiMentis  = new[] { "soldiery", "war_weariness", "grudgekeeping" },
                Wounds      = new Func<Wound>[] { () => new KneeFractureRightWound() },
                Appearance  = "a ruined knee and an old army coat",
                Persona     = "You gave the empire your knee and twenty years and it gave you a discharge paper and the street. You are bitter about it to anyone who will listen.",
                Opinions    = new[] { (DialogueTopic.Kin, "my regiment was my kin. Most of them are dead, and the rest are here begging like me") },
            },
            new PersonalityTrait
            {
                TraitId     = "beggar_cutpurse",
                DisplayName = "Begs with One Hand",
                ModiMentis  = new[] { "cutpurse", "snatch", "petty_thief" },
                Items       = new Func<Item>[] { () => new Knife() },
                Appearance  = "presses close when asking, a little too close",
                Persona     = "One hand holds out the bowl and the other finds the purse. You take what you are given and a little of what you are not.",
                Opinions    = new[] { (DialogueTopic.Trade, "charity is a fine thing. Sometimes people need help being charitable") },
            });
    }
}
