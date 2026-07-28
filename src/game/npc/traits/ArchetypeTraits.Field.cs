using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Traits;

/// <summary>Reserved traits for the open-field roles: reeve, hayward, plowman, reaper, bondman.</summary>
public sealed partial class PersonalityTraitRegistry
{
    private void RegisterFieldTraits()
    {
        // ── Reeve ──────────────────────────────────────────────────────────────
        Add("reeve",
            new PersonalityTrait
            {
                TraitId     = "reeve_skims_the_tally",
                DisplayName = "Skims the Tally",
                ModiMentis  = new[] { "avarice", "tallycraft", "masquerade" },
                Items       = new Func<Item>[] { () => new CoinPurse() },
                Appearance  = "keeps the tally stick turned away from anyone who might read it",
                Persona     = "You take a little off the top of every account and you have done it so long it feels like wages. You are terrified of an audit.",
                Opinions    = new[] { (DialogueTopic.Trade, "The accounts balance. They've always balanced. I'd not have held this post twenty years otherwise") },
            },
            new PersonalityTrait
            {
                TraitId     = "reeve_hated",
                DisplayName = "Hated by the Field",
                ModiMentis  = new[] { "stoneface", "grudgekeeping", "iron_nerves" },
                Appearance  = "stands slightly apart from everyone, and does not seem to expect otherwise",
                Persona     = "The people you oversee dislike you and you have accepted it as the price of the post. You are lonelier than you admit.",
                Opinions    = new[]
                {
                    (DialogueTopic.Neighbours, "They'd not sit with me at a feast. I set them the work, so I'm not one of them any more"),
                    (DialogueTopic.Rest,       "I take my rest alone. It's simpler for everyone"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "reeve_soft_hearted",
                DisplayName = "Soft on the Bondmen",
                ModiMentis  = new[] { "empathy", "hospitality" },
                Appearance  = "makes a point of not seeing someone resting in the shade",
                Persona     = "You cannot bring yourself to work people as hard as you are supposed to. Your yields are down and you are answering for it upward.",
                Opinions    = new[]
                {
                    (DialogueTopic.Work, "I set what a person can actually do. It costs me with the lord and I'll wear that"),
                    (DialogueTopic.Kin,  "They've children. Every one of them has children. You can't look at that and then shout"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "reeve_boundary_lawyer",
                DisplayName = "Knows Every Boundary Stone",
                ModiMentis  = new[] { "topographia", "rote", "heraldry" },
                Items       = new Func<Item>[] { () => new TallyStick() },
                Appearance  = "gives the impression of mentally re-measuring the ground you are standing on",
                Persona     = "You know the position and history of every boundary marker for miles and you have won three disputes on it. You bring it up often.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "There's a stone on the west margin been moved twice in my lifetime. I know exactly who by, both times") },
            },
            new PersonalityTrait
            {
                TraitId     = "reeve_risen_from_below",
                DisplayName = "Risen from the Furrow",
                ModiMentis  = new[] { "tillage", "peasantry", "bearing" },
                Appearance  = "hands that plainly did the work long before they held the tally",
                Persona     = "You were a bondman yourself and rose to this. You are proud of it and it makes you harsher, not softer, on people who remind you of your younger self.",
                Opinions    = new[]
                {
                    (DialogueTopic.Work, "I've done every job I set. Every one. So I know exactly when a man is telling me it can't be done"),
                    (DialogueTopic.Kin,  "My father died in that field. I hold the stick now. I'd say that's worth something"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "reeve_dreads_the_reckoning",
                DisplayName = "Dreads the Reckoning",
                ModiMentis  = new[] { "arithmetic_logic", "vigilance", "weeping" },
                Organs      = new[] { ("paunch", -1) },
                Appearance  = "there is a permanent unease about them that no amount of good weather settles",
                Persona     = "The accounting is coming and the numbers will not do. You are sleepless with it and you cannot tell anyone.",
                Opinions    = new[]
                {
                    (DialogueTopic.Harvest, "It has to be a good one. It has to be. I'll not discuss what happens if it isn't"),
                    (DialogueTopic.Rest,    "I lie down. I wouldn't call it resting"),
                },
            });

        // ── Hayward ────────────────────────────────────────────────────────────
        Add("hayward",
            new PersonalityTrait
            {
                TraitId     = "hayward_night_walker",
                DisplayName = "Walks the Margin at Night",
                ModiMentis  = new[] { "prowl", "vigilance", "stalking" },
                Organs      = new[] { ("left_eye", 1), ("right_eye", 1) },
                Appearance  = "grey and hollow-eyed, like someone who keeps the wrong hours on purpose",
                Persona     = "You patrol the boundary after dark because that is when things are taken. You sleep in the afternoon and you see a great deal nobody knows you see.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "I'm out there at two in the morning. You'd be amazed who else is") },
            },
            new PersonalityTrait
            {
                TraitId     = "hayward_hedge_artist",
                DisplayName = "Lays a Beautiful Hedge",
                ModiMentis  = new[] { "hedgecraft", "aesthetic", "patience" },
                Items       = new Func<Item>[] { () => new LeatherGloves() },
                Appearance  = "hands criss-crossed with thorn scratches, old and new together",
                Persona     = "Your laid hedges are genuinely fine work and people come to look at them. It is the only thing about the post you actually love.",
                Opinions    = new[] { (DialogueTopic.Work, "A hedge properly laid will still be stock-proof in thirty years. That's a thing to leave behind") },
            },
            new PersonalityTrait
            {
                TraitId     = "hayward_gored",
                DisplayName = "Gored Once",
                Wounds      = new Func<Wound>[] { () => new PiercedPaunchWound() },
                ModiMentis  = new[] { "clenched_grit", "beast_sense" },
                Appearance  = "moves stiffly through the middle, and gives cattle a wide berth",
                Persona     = "A loose bull put a horn through you and you nearly died of it. You are still frightened of cattle and you hide it badly.",
                Opinions    = new[] { (DialogueTopic.Beasts, "One got me. Through and out. I'll turn a loose beast back from a good distance now, and I'll not apologise for the distance") },
            },
            new PersonalityTrait
            {
                TraitId     = "hayward_takes_bribes",
                DisplayName = "Looks the Other Way",
                ModiMentis  = new[] { "foul_play", "bargaining", "masquerade" },
                Items       = new Func<Item>[] { () => new CoinPurse() },
                Appearance  = "a certain easiness about them for someone whose whole job is suspicion",
                Persona     = "You take small payments to not report damage. It is never much, and it has become how you actually make a living.",
                Opinions    = new[] { (DialogueTopic.Trade, "There's arrangements. Everybody has arrangements. Mine just happen to be about hedges") },
            },
            new PersonalityTrait
            {
                TraitId     = "hayward_incorruptible",
                DisplayName = "Reports Everything",
                ModiMentis  = new[] { "discipline", "obedience", "grudgekeeping" },
                Appearance  = "notes you, notes where you have walked, and says nothing about either",
                Persona     = "You report every infraction without exception, including from friends and family. It has cost you nearly every relationship you had.",
                Opinions    = new[]
                {
                    (DialogueTopic.Neighbours, "I reported my own brother's beasts in the corn. He's not spoken to me in four years. I'd do it again"),
                    (DialogueTopic.Work,       "The post is the post. You do it for everyone or you do it for nobody"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "hayward_deer_war",
                DisplayName = "At War with the Deer",
                ModiMentis  = new[] { "hunt", "spoor_reading", "marksman" },
                Items       = new Func<Item>[] { () => new HuntingBow() },
                Appearance  = "carries a bow on a round of the hedges, which is not strictly their business",
                Persona     = "The deer coming out of the wood are your personal enemy. You hunt them beyond what the post allows and you are cagey about it.",
                Opinions    = new[]
                {
                    (DialogueTopic.Wilds,  "They come out at dusk and take a family's year in one night. Somebody has to answer that"),
                    (DialogueTopic.Beasts, "Don't talk to me about deer. Not unless you've a few hours") ,
                },
            });

        // ── Plowman ────────────────────────────────────────────────────────────
        Add("plowman",
            new PersonalityTrait
            {
                TraitId     = "plowman_ox_whisperer",
                DisplayName = "The Team Follows Him",
                ModiMentis  = new[] { "beast_sense", "husbandry", "lullaby" },
                Appearance  = "talks continuously and quietly to nobody, in the cadence used on animals",
                Persona     = "Your oxen work for you as they do for nobody else. You talk to them all day and you are more comfortable doing that than talking to people.",
                Opinions    = new[]
                {
                    (DialogueTopic.Beasts, "They know my voice before they know the goad. That took years and it's worth every one"),
                    (DialogueTopic.Kin,    "I talk more to the team than to my own household. That's been said to me and it's fair"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "plowman_ruined_back",
                DisplayName = "Back Gone at Forty",
                Wounds      = new Func<Wound>[] { () => new BrokenBackboneWound() },
                ModiMentis  = new[] { "clenched_grit", "endurance" },
                Appearance  = "straightens up in stages, with a hand pressed to the base of the spine",
                Persona     = "Your back is finished and you are not old. You can still do the day but every hour of it hurts and you know how few years are left in you.",
                Opinions    = new[] { (DialogueTopic.Health, "The stilts did it. Everyone who holds them long enough ends the same. I just got there early") },
            },
            new PersonalityTrait
            {
                TraitId     = "plowman_straightest_furrow",
                DisplayName = "Straightest Furrow in the Field",
                ModiMentis  = new[] { "tillage", "geometric_scheme", "bearing" },
                Appearance  = "glances back along the ground behind them with obvious satisfaction",
                Persona     = "You cut the straightest furrow anyone here has seen and it is the one thing in your life you are unashamedly vain about.",
                Opinions    = new[] { (DialogueTopic.Work, "Go and look at the east strip. Go on. Then come back and tell me who ploughs here") },
            },
            new PersonalityTrait
            {
                TraitId     = "plowman_turned_up_bones",
                DisplayName = "Turned Something Up",
                ModiMentis  = new[] { "archeology", "dreamlore", "clairvoyance" },
                Items       = new Func<Item>[] { () => new LuckCharm() },
                Appearance  = "wears a charm at the throat and touches it while talking",
                Persona     = "You ploughed up human bones and something worked in metal in the west field. You put them back. You have not been easy about that ground since.",
                Opinions    = new[]
                {
                    (DialogueTopic.Omens, "I turned something up out there. I put it back and I've not ploughed that corner since. Ask me no more"),
                    (DialogueTopic.Wilds, "There's older things under this field than there are on it") ,
                },
            },
            new PersonalityTrait
            {
                TraitId     = "plowman_sings_the_team",
                DisplayName = "Sings the Team Along",
                ModiMentis  = new[] { "solfege", "lullaby", "endurance" },
                Items       = new Func<Item>[] { () => new WoodenPipe() },
                Appearance  = "audible from two fields away, and not unpleasantly",
                Persona     = "You sing the ploughing songs the whole day through, badly and loudly. The team steps to it and so, by now, does half the field.",
                Opinions    = new[] { (DialogueTopic.Stories, "There's a song for the ploughing that's older than the village. I sing it badly and the oxen don't mind") },
            },
            new PersonalityTrait
            {
                TraitId     = "plowman_landless_dream",
                DisplayName = "Saving for a Strip",
                ModiMentis  = new[] { "enterprise", "tallycraft", "discipline" },
                Items       = new Func<Item>[] { () => new CoinPurse() },
                Appearance  = "wearing everything a season past when it should have been replaced",
                Persona     = "You are saving, copper by copper, to hold land of your own. You go without to do it and you talk about it to anyone who will listen.",
                Opinions    = new[]
                {
                    (DialogueTopic.Work,  "Every day of this is a day nearer a strip with my name on it. That's how I get through it"),
                    (DialogueTopic.Trade, "I don't spend. Not on ale, not on anything. There's a reason and it's worth it"),
                },
            });

        // ── Reaper ─────────────────────────────────────────────────────────────
        Add("reaper",
            new PersonalityTrait
            {
                TraitId     = "reaper_fastest_blade",
                DisplayName = "Fastest Sickle in the Field",
                ModiMentis  = new[] { "harvestry", "vigor", "finesse" },
                Items       = new Func<Item>[] { () => new Sickle(), () => new Whetstone() },
                Organs      = new[] { ("right_arm", 1) },
                Appearance  = "a sickle kept far sharper than the others in the row",
                Persona     = "You cut faster than anyone and you race the row without being asked. It is competitive, exhausting, and the high point of your year.",
                Opinions    = new[] { (DialogueTopic.Harvest, "I take the outside of the row and I'm three lengths clear by noon. Ask anyone who's tried to hold my pace") },
            },
            new PersonalityTrait
            {
                TraitId     = "reaper_cut_himself",
                DisplayName = "Cut to the Bone",
                Wounds      = new Func<Wound>[] { () => new CutWound() },
                ModiMentis  = new[] { "herblore", "clenched_grit" },
                Appearance  = "a long, badly-healed gash up the inside of the left forearm",
                Persona     = "You opened your arm on your own blade in a tired moment and nearly bled out in a field. You are careful now to the point of slowness.",
                Opinions    = new[] { (DialogueTopic.Health, "Opened myself up to the bone at the end of a long day. Tiredness did that, not the blade") },
            },
            new PersonalityTrait
            {
                TraitId     = "reaper_last_sheaf",
                DisplayName = "Keeps the Last Sheaf",
                ModiMentis  = new[] { "fables_and_tales", "iconography", "dreamlore" },
                Items       = new Func<Item>[] { () => new Straw() },
                Appearance  = "a plaited figure of straw tucked carefully into the belt",
                Persona     = "You keep the customs of the last sheaf exactly and you take them seriously. You get quietly furious when the young treat it as a joke.",
                Opinions    = new[]
                {
                    (DialogueTopic.Harvest, "The last sheaf is cut a particular way and kept a particular way. It matters. It has always mattered"),
                    (DialogueTopic.Omens,   "You do it right or the year notices. Laugh — everyone laughs until a bad year"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "reaper_travels_for_work",
                DisplayName = "Follows the Harvest",
                ModiMentis  = new[] { "wayfaring", "topographia", "survivalism" },
                Items       = new Func<Item>[] { () => new TravelersBackpack() },
                Appearance  = "carrying everything they own, which is not much and is well packed",
                Persona     = "You walk from harvest to harvest and are only here for the season. You know the roads and every farm's reputation for miles.",
                SelfIntroduction = "here for the cutting and gone after — I follow the harvest and I've done it fifteen years",
                Opinions    = new[] { (DialogueTopic.Roads, "I've cut in six counties. You learn quick which farms feed you properly and which ones don't") },
            },
            new PersonalityTrait
            {
                TraitId     = "reaper_sunstruck",
                DisplayName = "Struck by the Sun",
                Wounds      = new Func<Wound>[] { () => new ConcussionsWound() },
                ModiMentis  = new[] { "wind_reading" },
                Appearance  = "keeps to the shade oddly deliberately, with a rag tied over the head",
                Persona     = "You collapsed in a field one August and were not right for a month. Your head still swims in the heat and you are frightened of it happening again.",
                Opinions    = new[]
                {
                    (DialogueTopic.Weather, "Heat. Everyone worries about the rain. It's the heat that put me on my back for a month"),
                    (DialogueTopic.Health,  "My head's not been quite right since. I lose words sometimes. It comes back"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "reaper_gleaner_friend",
                DisplayName = "Leaves Enough for the Gleaners",
                ModiMentis  = new[] { "empathy", "hospitality", "peasantry" },
                Appearance  = "works the row a little less thoroughly than the reeve would like",
                Persona     = "You deliberately cut a little carelessly so the gleaning widows behind you find something. You have been warned about it twice.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "There's women come behind us with nothing. I leave what I leave. The reeve knows and we don't discuss it") },
            });

        // ── Bondman ────────────────────────────────────────────────────────────
        Add("bondman",
            new PersonalityTrait
            {
                TraitId          = "bondman_dreams_of_freedom",
                DisplayName      = "Counting Toward Freedom",
                ModiMentis       = new[] { "tallycraft", "enterprise", "discipline" },
                Items            = new Func<Item>[] { () => new TallyStick(), () => new CoinPurse() },
                Appearance       = "a notched stick carried in a pocket that is plainly not for the reeve's use",
                Persona          = "You are saving to buy yourself free and you keep a private count of every copper. It is the entire organising fact of your life.",
                SelfIntroduction = "bound, for now — I'll say it plain, and I'll not be bound forever",
                Opinions         = new[]
                {
                    (DialogueTopic.Work,  "Every day of this has a number attached to it. I know exactly how many are left"),
                    (DialogueTopic.Trade, "I don't spend. Not one copper. You'd not either, if you were counting toward what I'm counting toward"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "bondman_broken_in",
                DisplayName = "Long Since Given Up",
                ModiMentis  = new[] { "obedience", "sloth", "stoneface" },
                Appearance  = "does the work with a completeness that has nothing behind it",
                Persona     = "You stopped hoping a long time ago. You are obedient, competent and entirely absent, and it takes people a while to notice.",
                Opinions    = new[]
                {
                    (DialogueTopic.Work, "It gets done. Same tomorrow. There's no more to say about it than that"),
                    (DialogueTopic.Rest, "I sleep. That's the good part and it isn't much of one"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "bondman_quiet_rebel",
                DisplayName = "Quietly Ungovernable",
                ModiMentis  = new[] { "foul_play", "sneak_art", "invective" },
                Appearance  = "agrees very readily, and there is something wrong with how readily",
                Persona     = "You obey to the letter and sabotage in tiny ways nobody can prove. Tools go missing. Gates are left open. You have never once been caught.",
                Opinions    = new[] { (DialogueTopic.Neighbours, "Things go wrong on this field more than they do elsewhere. Strange, that") },
            },
            new PersonalityTrait
            {
                TraitId     = "bondman_field_healer",
                DisplayName = "Binds the Field's Hurts",
                ModiMentis  = new[] { "herblore", "steady_hand", "empathy" },
                Items       = new Func<Item>[] { () => new Herb(), () => new MendingKit() },
                Appearance  = "a pouch of dried leaves at the belt and a rag roll of bindings beside it",
                Persona     = "You are the one who binds cuts and sets small breaks out in the field, an hour from anyone better. You learned it from watching and you are better than you think.",
                Opinions    = new[] { (DialogueTopic.Health, "Out here you're an hour from anyone who knows better. So you learn, or people bleed") },
            },
            new PersonalityTrait
            {
                TraitId     = "bondman_whole_family_bound",
                DisplayName = "Three Generations Bound",
                ModiMentis  = new[] { "lineage_lore", "rote", "endurance" },
                Appearance  = "belongs to this ground so completely that they look like part of it",
                Persona     = "Your grandparents worked this same strip. You know every stone in it. The land is not yours and it is more yours than anyone's.",
                Opinions    = new[]
                {
                    (DialogueTopic.Kin,     "My grandfather worked this strip. My father. Me. It isn't ours and there's nobody knows it better"),
                    (DialogueTopic.Harvest, "I could tell you what this ground did in every year since before I was born"),
                },
            },
            new PersonalityTrait
            {
                TraitId     = "bondman_took_the_blame",
                DisplayName = "Took Another's Blame",
                Wounds      = new Func<Wound>[] { () => new ScarWound() },
                ModiMentis  = new[] { "clenched_grit", "friendship", "grudgekeeping" },
                Appearance  = "old marks across the back and shoulders, of a kind that were given deliberately",
                Persona     = "You took a whipping for something a friend did and never said. You are proud of it and bitter about it in roughly equal measure.",
                Opinions    = new[]
                {
                    (DialogueTopic.Neighbours, "I took what was coming to someone else once. He knows. We've never spoken of it and we never will"),
                    (DialogueTopic.Work,       "There's a kind of loyalty out here that costs skin. I've paid it"),
                },
            });
    }
}
