using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Traits;

/// <summary>Reserved traits for the farm roles: farmer, farmhand, shepherd, swineherd, dairymaid, poultry keeper.</summary>
public sealed partial class PersonalityTraitRegistry
{
    private void RegisterFarmTraits()
    {
        // ── Farmer ─────────────────────────────────────────────────────────────
        Add("farmer",
            new PersonalityTrait
            {
                TraitId     = "farmer_one_bad_year_from_ruin",
                DisplayName = "One Bad Year from Ruin",
                ModiMentis  = new[] { "tallycraft", "vigilance", "enterprise" },
                Organs      = new[] { ("paunch", -1) },
                Appearance  = "thinner than a landholder ought to be, and watching the sky more than the visitor",
                Persona     = "You borrowed against this year's crop and it is not standing well. You are outwardly solid and privately terrified.",
                Opinions    = new[]
                {
                    (DialogueTopic.Harvest, "It needs to come in heavy. I'll not tell you what's riding on it, but it needs to come in heavy"),
                    (DialogueTopic.Trade,   "I owe. Most holdings owe. The ones who say otherwise owe more") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "farmer_improver",
                DisplayName = "Always Trying Something New",
                ModiMentis  = new[] { "seed_lore", "scientific_research", "drainage" },
                Appearance  = "eager to show you a corner of the ground they have done something unusual to",
                Persona     = "You experiment — new crops, new rotations, ditching schemes. Half fail, the neighbours mock you, and you keep going.",
                Opinions    = new[]
                {
                    (DialogueTopic.Harvest, "I've a corner sown with something nobody here has tried. It may come to nothing. Most of them do"),
                    (DialogueTopic.Work,    "They laugh. Then one of my notions works and they copy it inside two seasons"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "farmer_no_heir",
                DisplayName = "Nobody to Leave It To",
                ModiMentis  = new[] { "elegy", "stewardry", "introspection" },
                Appearance  = "looks over their own fields the way a person looks at something already lost",
                Persona     = "You have no child to inherit and the holding will go to a cousin you dislike. It has taken the point out of the work.",
                Opinions    = new[]
                {
                    (DialogueTopic.Kin,     "There's nobody after me. It goes to a cousin I've no time for. That's the whole shape of it"),
                    (DialogueTopic.Seasons, "You work a place for forty years for whoever comes next. When there's nobody, the years read differently"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "farmer_hard_master",
                DisplayName = "Hard on the Hands",
                ModiMentis  = new[] { "cruelty", "invective", "stewardry" },
                Appearance  = "watches the hired hands rather than the land",
                Persona     = "You work your labourers to the edge and pay them late. You consider this ordinary and would be amazed to hear it called cruel.",
                Opinions    = new[]
                {
                    (DialogueTopic.Work,       "I pay for a day and I'll have a day. Not most of one"),
                    (DialogueTopic.Neighbours, "Hands come and go here. That says more about hands than it does about me"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "farmer_lends_the_team",
                DisplayName = "Lends the Team to Anyone",
                ModiMentis  = new[] { "hospitality", "friendship", "husbandry" },
                Appearance  = "greets you as though you have already been done a favour by them",
                Persona     = "You lend your oxen and your gear to any neighbour who asks. It costs you working days and you consider it simply how a village stands up.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "I lend the team and I get a team lent back. That's not charity, that's how anybody here survives") },
            },
            new PersonalityTrait
            {
                TraitId     = "farmer_lost_the_herd",
                DisplayName = "Lost the Herd to Murrain",
                ModiMentis  = new[] { "husbandry", "herblore", "grudgekeeping" },
                Appearance  = "a wariness that comes out whenever livestock are mentioned",
                Persona     = "A cattle sickness took nearly all your beasts three years ago. You are still rebuilding and you are savage about anyone bringing an unchecked animal onto your land.",
                Opinions    = new[]
                {
                    (DialogueTopic.Beasts, "I lost nine in one month. Nine. You do not bring a strange beast onto my ground, not for any reason"),
                    (DialogueTopic.Health, "It goes through a herd like fire through straw and there's not a thing you can do but burn what's left"),
                },
            });

        // ── Farmhand ───────────────────────────────────────────────────────────
        Add("farmhand",
            new PersonalityTrait
            {
                TraitId     = "farmhand_sweet_on_someone",
                DisplayName = "Sweet on Someone Here",
                ModiMentis  = new[] { "comeliness", "seduction", "grooming" },
                Items       = new Func<Item>[] { () => new Hairpin() },
                Appearance  = "washed and tidied to a standard the work does not require",
                Persona     = "You are in love with someone on this farm and everyone knows except, you devoutly hope, them. You are transparently distracted.",
                Opinions    = new[]
                {
                    (DialogueTopic.Rest, "There's a reason I don't mind the evening milking. I'll not be saying more than that"),
                    (DialogueTopic.Kin,  "I'd like a house of my own one day. With — well. With someone in it"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "farmhand_underpaid_and_knows_it",
                DisplayName = "Knows Exactly What He's Owed",
                ModiMentis  = new[] { "tallycraft", "invective", "bargaining" },
                Items       = new Func<Item>[] { () => new TallyStick() },
                Appearance  = "notching a stick that plainly records something other than the farm's business",
                Persona     = "You keep a private tally of every day worked and every copper short. You are polite about it and you are absolutely going to raise it.",
                Opinions    = new[]
                {
                    (DialogueTopic.Trade, "I know what I'm owed to the day. He knows I know. We're both waiting to see who says it first"),
                    (DialogueTopic.Work,  "I'll do the work. I'll not pretend I'm grateful for the rate"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "farmhand_strong_as_two",
                DisplayName = "Does the Work of Two",
                ModiMentis  = new[] { "hard_labor", "haulage", "brute_force" },
                Organs      = new[] { ("left_arm", 1), ("right_arm", 1), ("backbone", 1) },
                Appearance  = "carrying something that ought to take two people, and not obviously struggling",
                Persona     = "You are extraordinarily strong and it is the only thing anyone values you for. You are beginning to resent that.",
                Opinions    = new[] { (DialogueTopic.Work, "They give me the heavy end of everything. Nobody's ever asked whether I've a head on me as well") },
            },
            new PersonalityTrait
            {
                TraitId     = "farmhand_orchard_thief",
                DisplayName = "Always in the Orchard",
                ModiMentis  = new[] { "petty_thief", "clambering", "forage_lore" },
                Items       = new Func<Item>[] { () => new Apple() },
                Appearance  = "eating an apple that did not obviously come from anywhere legitimate",
                Persona     = "You steal fruit constantly and consider it a right rather than a theft. You are cheerfully unrepentant if caught.",
                Opinions    = new[] { (DialogueTopic.Food, "There's an apple in my hand most of the year. Where from? Trees. Next question") },
            },
            new PersonalityTrait
            {
                TraitId     = "farmhand_wants_the_daughter",
                DisplayName = "Aiming Above His Station",
                ModiMentis  = new[] { "enterprise", "high_society_manners", "bearing" },
                Appearance  = "wearing a decent cap that does not match the rest of the clothes at all",
                Persona     = "You intend to marry into the holding and you are working openly toward it. The farmer knows. It has made things tense.",
                Opinions    = new[]
                {
                    (DialogueTopic.Kin,  "I'll not be a hired hand at forty. There's a way up out of this and I mean to take it"),
                    (DialogueTopic.Work, "I do more than I'm paid for, and I make sure it's noticed. That's not shame, that's a plan"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "farmhand_slept_in_the_barn_all_winter",
                DisplayName = "Sleeps with the Beasts",
                ModiMentis  = new[] { "husbandry", "endurance", "beast_sense" },
                Appearance  = "straw in the clothes that has been there a while, and smells of byre",
                Persona     = "You sleep in the byre for the warmth and because the animals settle you. You have no room of your own and have stopped expecting one.",
                Opinions    = new[]
                {
                    (DialogueTopic.Rest,   "I sleep in with the beasts. It's warmer than the loft and they're better company than the loft was"),
                    (DialogueTopic.Beasts, "You learn a herd properly when you sleep among them. I know which one's off before the farmer does"),
                },
            });

        // ── Shepherd ───────────────────────────────────────────────────────────
        Add("shepherd",
            new PersonalityTrait
            {
                TraitId     = "shepherd_wolf_killer",
                DisplayName = "Killed the Wolf",
                ModiMentis  = new[] { "hunt", "ferocity", "vigilance" },
                Wounds      = new Func<Wound>[] { () => new ScarWound() },
                Items       = new Func<Item>[] { () => new HuntingSpear() },
                Appearance  = "a spear kept within reach, and a torn white scar across the forearm",
                Persona     = "You killed a wolf that was taking your flock, alone, and it marked you. You do not boast about it and you will confirm it if asked.",
                Opinions    = new[]
                {
                    (DialogueTopic.Wilds,  "I've had one at close quarters. It's not like the stories. It's fast and it's quiet and then it's on you"),
                    (DialogueTopic.Beasts, "I lost four before I got it. Four. That's a family's year in wool") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "shepherd_names_every_ewe",
                DisplayName = "Names Every Ewe",
                ModiMentis  = new[] { "husbandry", "rote", "empathy" },
                Appearance  = "addressing individual sheep by name, without any self-consciousness at all",
                Persona     = "You know every animal in the flock by name and temperament. You grieve properly when one dies and you know how that sounds.",
                Opinions    = new[]
                {
                    (DialogueTopic.Beasts, "Every one of them has a name and I'll not be laughed at for it. I know which ewe is which at two hundred paces"),
                    (DialogueTopic.Kin,    "The flock's most of my company. Say what you like about that") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "shepherd_star_reader",
                DisplayName = "Reads the Sky",
                ModiMentis  = new[] { "wind_reading", "cartography", "clairvoyance" },
                Organs      = new[] { ("left_eye", 1), ("right_eye", 1) },
                Appearance  = "glances up at the sky mid-sentence, and appears to get something from it",
                Persona     = "You have spent thousands of nights under an open sky and you know the stars and the weather better than anyone here. You are quietly certain about the future weather and usually right.",
                Opinions    = new[]
                {
                    (DialogueTopic.Weather, "Three days out I'll tell you, and I'll be right four times in five. It's not a gift, it's ten thousand nights of looking"),
                    (DialogueTopic.Omens,   "The sky says things. Not fortunes — weather. But folk hear fortunes when I say it") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "shepherd_half_wild",
                DisplayName = "Half Gone Wild",
                ModiMentis  = new[] { "survivalism", "bushcraft", "stoneface" },
                Appearance  = "unkempt to a degree that suggests months without seeing anyone",
                Persona     = "You spend so long alone on the high grazing that you have half forgotten how to talk to people. You are not unfriendly, just badly out of practice.",
                Opinions    = new[]
                {
                    (DialogueTopic.Neighbours, "I'm down here twice a season. It takes me a day to get my talking back"),
                    (DialogueTopic.Rest,       "Alone on the hill with nothing needing me. That's rest. A room full of people isn't") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "shepherd_lost_the_dog",
                DisplayName = "Lost the Dog",
                ModiMentis  = new[] { "elegy", "beast_sense", "whistling" },
                Appearance  = "whistles a signal out of habit and then remembers there is nothing to answer it",
                Persona     = "Your working dog died recently and you have not replaced it. You still whistle for it without meaning to and it undoes you every time.",
                Opinions    = new[]
                {
                    (DialogueTopic.Beasts, "I had a dog fifteen years. I still whistle for her. Every day I still whistle for her"),
                    (DialogueTopic.Work,   "It takes me twice as long now. That's not the part I mind") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "shepherd_carves_while_watching",
                DisplayName = "Carves While Watching",
                ModiMentis  = new[] { "whittlecraft", "aesthetic", "patience" },
                Items       = new Func<Item>[] { () => new Knife(), () => new WoodenDoll() },
                Appearance  = "whittling something small and detailed without appearing to look at it",
                Persona     = "The hours of watching gave you a craft. You carve constantly and give the results away, and some of them are genuinely beautiful.",
                Opinions    = new[] { (DialogueTopic.Rest, "Ten hours a day watching. Your hands want something. Mine took up a knife and a bit of wood") },
            });

        // ── Swineherd ──────────────────────────────────────────────────────────
        Add("swineherd",
            new PersonalityTrait
            {
                TraitId     = "swineherd_mast_walker",
                DisplayName = "Knows Every Oak",
                ModiMentis  = new[] { "forage_lore", "topographia", "bushcraft" },
                Appearance  = "burrs and leaf-litter well up the legs, from somewhere further out than the pens",
                Persona     = "You take the drove deep into the wood for acorns every autumn and you know which oaks bear well years in advance.",
                Opinions    = new[]
                {
                    (DialogueTopic.Wilds,   "I know every oak within a half-day and which of them will drop. That's my whole autumn"),
                    (DialogueTopic.Seasons, "Mast-time. Six weeks in the wood with the drove. Best of the year and nobody believes me"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "swineherd_savaged",
                DisplayName = "Savaged by the Boar",
                Wounds      = new Func<Wound>[] { () => new TibiaFractureLeftWound() },
                ModiMentis  = new[] { "clenched_grit", "beast_sense" },
                Appearance  = "the left leg is a mess of old scarring below the knee, and takes weight badly",
                Persona     = "The boar went for you once and nearly took the leg. You still work the pens and you never turn your back on the male.",
                Opinions    = new[] { (DialogueTopic.Beasts, "The boar had me down. I've the leg to show for it and I count myself lucky it was only the leg") },
            },
            new PersonalityTrait
            {
                TraitId     = "swineherd_butcher_too",
                DisplayName = "Does the Killing Too",
                ModiMentis  = new[] { "butchery", "cold_blood", "steady_hand" },
                Items       = new Func<Item>[] { () => new HuntingKnife(), () => new SaltPouch() },
                Appearance  = "a heavy knife at the belt and a practical, unsentimental way of looking at livestock",
                Persona     = "You do the autumn killing for half the households here. You are matter-of-fact about it and you are aware that unsettles people.",
                Opinions    = new[]
                {
                    (DialogueTopic.Beasts, "I raise them and I kill them. Both properly. Doing one well is what earns you the right to the other"),
                    (DialogueTopic.Food,   "There's a right way to take a pig apart so nothing's wasted. I'll show you if you've the stomach"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "swineherd_outcast",
                DisplayName = "Nobody Sits Near Him",
                ModiMentis  = new[] { "stoneface", "sense_of_humor", "endurance" },
                Appearance  = "an unmistakable smell, and a settled indifference to the space it creates",
                Persona     = "People stand upwind of you and always have. You have made it into a joke you tell first, which is its own kind of armour.",
                Opinions    = new[]
                {
                    (DialogueTopic.Neighbours, "Nobody sits by me at a feast and they all eat my bacon in February. I find that funnier than they do"),
                    (DialogueTopic.Rest,       "I've plenty of room around me wherever I sit. There's worse arrangements") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "swineherd_pig_favourite",
                DisplayName = "Cannot Kill That One",
                ModiMentis  = new[] { "husbandry", "empathy", "mythomania" },
                Appearance  = "one particular pig is following them about with obvious familiarity",
                Persona     = "There is one animal you cannot bring yourself to slaughter and you have been inventing reasons to spare it for two years. You know how absurd it is.",
                Opinions    = new[] { (DialogueTopic.Beasts, "There's one I've a soft spot for. I've a fresh reason every year for why she's not for the block. Don't tell the farmer") },
            },
            new PersonalityTrait
            {
                TraitId     = "swineherd_truffle_finder",
                DisplayName = "Finds What the Pigs Find",
                ModiMentis  = new[] { "mycology", "scenting", "enterprise" },
                Items       = new Func<Item>[] { () => new Mushroom() },
                Appearance  = "a cloth pouch of something dark and pungent tucked well out of sight",
                Persona     = "Your pigs root up things worth real money and you sell them quietly to people passing through. Nobody local knows what they are worth.",
                Opinions    = new[] { (DialogueTopic.Trade, "The pigs find things in the wood. What things is my business, and what they fetch is very much my business") },
            });

        // ── Dairymaid ──────────────────────────────────────────────────────────
        Add("dairymaid",
            new PersonalityTrait
            {
                TraitId     = "dairymaid_famous_cheese",
                DisplayName = "Her Cheese Travels",
                ModiMentis  = new[] { "dairycraft", "aesthetic", "enterprise" },
                Items       = new Func<Item>[] { () => new Cheese() },
                Appearance  = "carries a wrapped cheese with the care of something valuable",
                Persona     = "Your cheese is sought after well beyond this holding and you have quietly built a small trade of your own out of it.",
                Opinions    = new[]
                {
                    (DialogueTopic.Food,  "There's men come from two days off for my cheese. I'll not pretend that isn't the pride of my life"),
                    (DialogueTopic.Trade, "I sell my own, separate from the farm's. It's an arrangement and it took some getting"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "dairymaid_ruined_hands",
                DisplayName = "Hands Gone to the Cold",
                Wounds      = new Func<Wound>[] { () => new BrokenHandRightWound() },
                ModiMentis  = new[] { "clenched_grit", "patience" },
                Appearance  = "hands swollen, red-raw and cracked, held slightly curled",
                Persona     = "Cold water and endless milking have wrecked your hands. They ache constantly and you can no longer straighten the right one fully.",
                Opinions    = new[] { (DialogueTopic.Health, "My hands. Cold water twice a day for twenty years. Nobody warns you about the hands") },
            },
            new PersonalityTrait
            {
                TraitId     = "dairymaid_never_took_the_pox",
                DisplayName = "Never Took the Pox",
                ModiMentis  = new[] { "iron_stomach", "herblore", "gut_feeling" },
                Organs      = new[] { ("viscera", 1), ("spleen", 1) },
                Appearance  = "an unmarked face in a place where most faces are not",
                Persona     = "The sickness that scarred half the village passed you by entirely, as it did the other dairy women. People think you are lucky or blessed; you have your own quiet theory about the cows.",
                Opinions    = new[]
                {
                    (DialogueTopic.Health, "It went through this place and not one of us in the dairy took it. Say what you like — I've a notion it's the cows"),
                    (DialogueTopic.Omens,  "Folk called it a blessing. I don't think it was a blessing. I think it was the byre"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "dairymaid_sings_to_the_herd",
                DisplayName = "Sings the Cows Down",
                ModiMentis  = new[] { "lullaby", "solfege", "husbandry" },
                Appearance  = "singing quietly and continuously, in a way that seems aimed at the animals",
                Persona     = "You sing at every milking because the herd lets down better for it. You have a genuinely lovely voice and are embarrassed to use it for people.",
                Opinions    = new[]
                {
                    (DialogueTopic.Beasts,  "They milk better for a voice. Any voice. Mine's had a lot of practice"),
                    (DialogueTopic.Stories, "I know the songs. I'll sing them to a cow all day and not to a room, and I couldn't tell you why"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "dairymaid_wants_her_own_two_cows",
                DisplayName = "Saving for Two Cows",
                ModiMentis  = new[] { "enterprise", "tallycraft", "discipline" },
                Items       = new Func<Item>[] { () => new CoinPurse() },
                Appearance  = "there is a purpose about them that the work alone does not explain",
                Persona     = "You intend to have a house and two cows of your own and you have a plan with a number in it. You will tell anyone who asks and several who do not.",
                Opinions    = new[] { (DialogueTopic.Kin, "A house of my own with two cows in it and nobody's schedule but mine. I know exactly what it costs, to the copper") },
            },
            new PersonalityTrait
            {
                TraitId     = "dairymaid_the_butter_wont_come",
                DisplayName = "Reads the Churn",
                ModiMentis  = new[] { "dreamlore", "clairvoyance", "gut_feeling" },
                Items       = new Func<Item>[] { () => new LuckCharm() },
                Appearance  = "a holed pebble on a thong knotted to the wrist, near where the churn would be gripped",
                Persona     = "You believe a quarrel in the house stops the butter coming and you have seen it hold true too often to laugh at. You police the mood of the dairy carefully.",
                Opinions    = new[]
                {
                    (DialogueTopic.Omens, "The butter won't come if there's bad feeling in the house. Laugh. I've watched it happen and I've stopped laughing"),
                    (DialogueTopic.Kin,   "I keep the peace in that dairy for a reason, and it isn't only that I like a quiet room"),
                },
            });

        // ── Poultry keeper ─────────────────────────────────────────────────────
        Add("poultry_keeper",
            new PersonalityTrait
            {
                TraitId     = "poultry_fox_war",
                DisplayName = "At War with the Fox",
                ModiMentis  = new[] { "hunt", "vigilance", "spoor_reading" },
                Items       = new Func<Item>[] { () => new Rope() },
                Appearance  = "hollow-eyed, and glances toward the treeline mid-conversation",
                Persona     = "A fox is taking your birds and you have not slept properly in weeks. It has become intensely personal.",
                Opinions    = new[]
                {
                    (DialogueTopic.Wilds,  "There's one particular fox. Don't tell me they're all alike — I know this one and this one knows me"),
                    (DialogueTopic.Rest,   "I don't sleep through. I'm up at every sound and half the sounds are nothing") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "poultry_talks_to_the_birds",
                DisplayName = "Talks to the Birds",
                ModiMentis  = new[] { "beast_sense", "whistling", "empathy" },
                Appearance  = "conducting a genuine-sounding conversation with a hen",
                Persona     = "You talk to the flock all day and hold up both sides of it. You find birds better company than most people and are entirely open about that.",
                Opinions    = new[] { (DialogueTopic.Beasts, "They've more sense than they're credited with. And more temper. I'd take their company over a room of folk") },
            },
            new PersonalityTrait
            {
                TraitId     = "poultry_egg_counter",
                DisplayName = "Counts Every Egg",
                ModiMentis  = new[] { "tallycraft", "scrutiny", "grudgekeeping" },
                Items       = new Func<Item>[] { () => new TallyStick(), () => new Egg() },
                Appearance  = "counting something under their breath while you talk",
                Persona     = "You know exactly how many eggs the flock produced every day for years back. You notice a single one missing and you will pursue it.",
                Opinions    = new[]
                {
                    (DialogueTopic.Neighbours, "Three went last month. Not the fox — a fox doesn't take three neatly and leave the birds. I know what I know"),
                    (DialogueTopic.Trade,      "I can tell you what this flock laid any week these four years. Try me") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "poultry_goose_bitten",
                DisplayName = "Frightened of the Geese",
                Wounds      = new Func<Wound>[] { () => new ContusionWound() },
                ModiMentis  = new[] { "sense_of_humor", "clenched_grit" },
                Appearance  = "a set of angry bruises up one forearm and a wary eye on anything with a long neck",
                Persona     = "The geese have you thoroughly beaten and everyone finds it funny including, mostly, you. You still have to feed them.",
                Opinions    = new[] { (DialogueTopic.Beasts, "The geese run this yard and I only work here. Laugh — everyone does. Then they meet the geese") },
            },
            new PersonalityTrait
            {
                TraitId     = "poultry_child_worker",
                DisplayName = "Barely Grown",
                ModiMentis  = new[] { "obedience", "surefoot", "forage_lore" },
                Organs      = new[] { ("left_foot", 1), ("right_foot", 1) },
                Appearance  = "very young for the work, and moving at a run out of habit",
                Persona     = "You are young enough that this is your first real post. You take it extremely seriously and are anxious about doing it wrong.",
                Opinions    = new[]
                {
                    (DialogueTopic.Work, "It's my first proper charge. I've not lost a bird yet. Not one"),
                    (DialogueTopic.Kin,  "My mother got me the place. I'd not want to go home and say I'd lost it"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "poultry_sells_on_the_side",
                DisplayName = "Eggs Out the Back Door",
                ModiMentis  = new[] { "petty_thief", "bargaining", "masquerade" },
                Items       = new Func<Item>[] { () => new Egg(), () => new CoinPurse() },
                Appearance  = "a basket held slightly behind them, and a smile arriving a little early",
                Persona     = "A steady handful of eggs never reaches the household and goes out for your own coin instead. You are good at it and quietly terrified of the day you are not.",
                Opinions    = new[] { (DialogueTopic.Trade, "A few here and there. Nobody counts to the last egg. Well — nobody has yet") },
            });
    }
}
