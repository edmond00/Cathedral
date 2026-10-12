using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative.Work;

/// <summary>
/// Singleton catalogue of all <see cref="Job"/>s and the map from a job-giving archetype
/// (blacksmith, carpenter, …, reeve, farmer, hayward) to the jobs it can offer. Transcribed from
/// <c>design/turnips_and_radishes/economy.md</c>.
///
/// Job sampling is deterministic per NPC: <see cref="SampleJobs"/> seeds its RNG from the NPC id
/// (process-stable FNV-1a, mirroring <c>NpcTradeCatalog</c>), so the same NPC always proposes the
/// same set of jobs.
/// </summary>
public sealed class JobRegistry
{
    private static JobRegistry? _instance;
    public static JobRegistry Instance => _instance ??= new JobRegistry();

    private readonly Dictionary<string, Job>          _jobsById       = new();
    private readonly Dictionary<string, List<string>> _jobsByArchetype = new();

    private JobRegistry()
    {
        BuildCatalogue();
        ValidateModusMentisReferences();
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>True when the given archetype id has any jobs to offer.</summary>
    public bool HasJobs(string archetypeId) => _jobsByArchetype.ContainsKey(archetypeId);

    /// <summary>The job with the given id, or null.</summary>
    public Job? GetById(string id) => _jobsById.GetValueOrDefault(id);

    /// <summary>Every job in the catalogue. What the audits read to see which lessons work buys.</summary>
    public IEnumerable<Job> All => _jobsById.Values;

    /// <summary>
    /// Deterministically samples up to <paramref name="count"/> distinct jobs the given archetype
    /// offers, seeded by <paramref name="npcId"/> so the same NPC always yields the same set.
    /// </summary>
    public IReadOnlyList<Job> SampleJobs(string npcId, string archetypeId, int count = 3)
    {
        if (!_jobsByArchetype.TryGetValue(archetypeId, out var pool) || pool.Count == 0)
            return Array.Empty<Job>();

        var rng = new Random(StableSeed(npcId));
        return pool.OrderBy(_ => rng.Next())
                   .Take(Math.Min(count, pool.Count))
                   .Select(id => _jobsById[id])
                   .ToList();
    }

    // ── Catalogue ───────────────────────────────────────────────────────────────

    private void BuildCatalogue()
    {
        // Every wage is paid in copper, like every price — denominations never convert, so a wage
        // in silver would buy nothing. Village masters pay the better rates (a copper per 6–15
        // days) against the field and wilderness work below.
        AddArchetype("blacksmith",
            J("bellows_hand",   "bellows-hand",   CoinType.Copper, 12f, "firecraft", "hard_labor", "patience"),
            J("coal_breaker",   "coal-breaker",   CoinType.Copper, 15f, "hard_labor", "firecraft", "dirty_labor"),
            J("quench_hand",    "quench-hand",    CoinType.Copper,  6f, "metalcraft", "firecraft", "steady_hand"),
            J("scrap_sorter",   "scrap-sorter",   CoinType.Copper,  9f, "metalcraft", "scrutiny", "tallycraft"));

        AddArchetype("carpenter",
            J("sawyer",         "sawyer",         CoinType.Copper,  9f, "woodcraft", "hard_labor", "steady_hand"),
            J("plank_stacker",  "plank-stacker",  CoinType.Copper, 12f, "haulage", "woodcraft", "hard_labor"),
            J("peg_whittler",   "peg-whittler",   CoinType.Copper,  6f, "whittlecraft", "woodcraft", "finesse"),
            J("shaving_sweeper","shaving-sweeper",CoinType.Copper, 15f, "dirty_labor", "woodcraft", "obedience"));

        AddArchetype("cooper",
            J("stave_shaver",   "stave-shaver",   CoinType.Copper,  7.5f, "woodcraft", "whittlecraft", "steady_hand"),
            J("hoop_holder",    "hoop-holder",    CoinType.Copper,  9f, "steady_hand", "metalcraft", "patience"),
            J("barrel_hauler",  "barrel-hauler",  CoinType.Copper, 12f, "haulage", "cellarcraft", "woodcraft"));

        AddArchetype("weaver",
            J("wool_carder",    "wool-carder",    CoinType.Copper, 12f, "threadwork", "patience", "dirty_labor"),
            J("spindle_hand",   "spindle-hand",   CoinType.Copper,  7.5f, "threadwork", "finesse", "patience"),
            J("warp_threader",  "warp-threader",  CoinType.Copper,  6f, "threadwork", "finesse", "steady_hand"),
            J("fleece_picker",  "fleece-picker",  CoinType.Copper, 15f, "threadwork", "scrutiny", "dirty_labor"));

        AddArchetype("miller",
            J("sack_carrier",   "sack-carrier",   CoinType.Copper, 15f, "haulage", "hard_labor", "peasantry"),
            J("grist_sifter",   "grist-sifter",   CoinType.Copper,  7.5f, "threshery", "millcraft", "scrutiny"),
            J("hopper_feeder",  "hopper-feeder",  CoinType.Copper,  9f, "millcraft", "haulage", "patience"),
            J("toll_tallier",   "toll-tallier",   CoinType.Copper,  6f, "tallycraft", "bargaining", "scrutiny"));

        AddArchetype("baker",
            J("oven_firer",     "oven-firer",     CoinType.Copper,  7.5f, "firecraft", "doughcraft", "patience"),
            J("dough_kneader",  "dough-kneader",  CoinType.Copper,  9f, "doughcraft", "hard_labor", "patience"),
            J("bread_runner",   "bread-runner",   CoinType.Copper, 15f, "haulage", "peasantry", "obedience"),
            J("wood_fetcher",   "wood-fetcher",   CoinType.Copper, 15f, "haulage", "hard_labor", "obedience"));

        AddArchetype("brewer",
            J("malt_turner",    "malt-turner",    CoinType.Copper,  9f, "brewcraft", "patience", "hard_labor"),
            J("mash_stirrer",   "mash-stirrer",   CoinType.Copper,  7.5f, "brewcraft", "firecraft", "hard_labor"),
            J("ale_pourer",     "ale-pourer",     CoinType.Copper,  6f, "enterprise", "bargaining", "brewcraft"),
            J("cask_roller",    "cask-roller",    CoinType.Copper, 12f, "haulage", "steady_hand", "cellarcraft"));

        // Field reeve pays in copper: one copper every 5–15 days.
        AddArchetype("reeve",
            J("plowman",        "plowman",        CoinType.Copper,  5f,  "tillage", "hard_labor", "beast_sense"),
            J("sower",          "sower",          CoinType.Copper,  6f,  "seed_lore", "fieldcraft", "peasantry"),
            J("reaper",         "reaper",         CoinType.Copper,  6f,  "harvestry", "hard_labor", "patience"),
            J("thresher_binder","thresher",       CoinType.Copper,  7.5f,"threshery", "hard_labor", "harvestry"),
            J("weed_puller",    "weed-puller",    CoinType.Copper, 15f,  "fieldcraft", "peasantry", "patience"),
            J("root_digger",    "root-digger",    CoinType.Copper, 10f,  "harvestry", "hard_labor", "dirty_labor"),
            J("herb_gatherer",  "herb-gatherer",  CoinType.Copper, 10f,  "herblore", "forage_lore", "scrutiny"),
            J("ditch_clearer",  "ditch-clearer",  CoinType.Copper, 10f,  "drainage", "dirty_labor", "hard_labor"),
            J("reed_cutter",    "reed-cutter",    CoinType.Copper, 15f,  "drainage", "bushcraft", "harvestry"),
            J("stone_picker",   "stone-picker",   CoinType.Copper, 15f,  "stonework", "hard_labor", "obedience"),
            J("margin_watch",   "margin-watch",   CoinType.Copper,  7.5f,"hedgecraft", "vigilance", "fieldcraft"));

        // Farm reeve pays in copper: one copper every 5–15 days.
        AddArchetype("farmer",
            J("farmhand",       "farmhand",       CoinType.Copper, 10f,  "peasantry", "hard_labor", "husbandry"),
            J("shepherd",       "shepherd",       CoinType.Copper,  6f,  "husbandry", "beast_sense", "fieldcraft"),
            J("shearer",        "shearer",        CoinType.Copper,  7.5f,"husbandry", "threadwork", "steady_hand"),
            J("swineherd",      "swineherd",      CoinType.Copper, 10f,  "husbandry", "dirty_labor", "beast_sense"),
            J("poultry_hand",   "poultry-hand",   CoinType.Copper, 15f,  "husbandry", "forage_lore", "peasantry"),
            J("milker",         "milker",         CoinType.Copper,  6f,  "dairycraft", "husbandry", "patience"),
            J("churn_hand",     "churn-hand",     CoinType.Copper,  7.5f,"dairycraft", "hard_labor", "patience"),
            J("orchard_picker", "orchard-picker", CoinType.Copper, 10f,  "forage_lore", "athletics", "herblore"),
            J("garden_hand",    "garden-hand",    CoinType.Copper, 10f,  "herblore", "fieldcraft", "peasantry"),
            J("hay_hauler",     "hay-hauler",     CoinType.Copper, 10f,  "haulage", "cellarcraft", "hard_labor"),
            J("pig_fattener",   "pig-fattener",   CoinType.Copper, 10f,  "swine_lore", "dunging", "husbandry"),
            J("breeder_hand",   "breeder's hand", CoinType.Copper,  7.5f,"bloodlines", "husbandry", "beast_sense"));

        // The hayward patrols the field margin: a small themed pool reusing the boundary jobs.
        LinkArchetype("hayward", "margin_watch", "reed_cutter", "stone_picker");

        // ── The settled country ──────────────────────────────────────────────
        // Groves and vineyards pay like the field; plantations a little worse, because there are
        // always more hands than rows. The great houses and the towns pay like village masters.
        AddArchetype("orchardist",
            J("fruit_picker",   "fruit-picker",   CoinType.Copper, 10f,  "pomology", "athletics", "harvestry"),
            J("tree_pruner",    "pruner",         CoinType.Copper,  7.5f,"pruning", "steady_hand", "pomology"),
            J("grafting_hand",  "grafting-hand",  CoinType.Copper,  6f,  "grafting", "finesse", "patience"),
            J("windfall_gleaner","windfall-gleaner",CoinType.Copper,15f, "gleaning", "forage_lore", "peasantry"));

        AddArchetype("vintner",
            J("grape_picker",   "grape-picker",   CoinType.Copper, 10f,  "viniculture", "harvestry", "hard_labor"),
            J("vine_dresser",   "vine-dresser",   CoinType.Copper,  7.5f,"pruning", "viniculture", "patience"),
            J("treader",        "treader",        CoinType.Copper, 12f,  "hard_labor", "endurance", "bouquet"),
            J("press_hand",     "press-hand",     CoinType.Copper,  9f,  "oil_pressing", "haulage", "cellarcraft"));

        AddArchetype("planter",
            J("cane_cutter",    "cane-cutter",    CoinType.Copper, 15f,  "canecraft", "hard_labor", "endurance"),
            J("tea_plucker",    "tea-plucker",    CoinType.Copper, 12f,  "tea_lore", "finesse", "patience"),
            J("paddy_hand",     "paddy-hand",     CoinType.Copper, 12f,  "paddycraft", "dirty_labor", "hard_labor"),
            J("weigh_tallier",  "weigh-tallier",  CoinType.Copper,  6f,  "bookkeeping", "tallycraft", "scrutiny"),
            J("water_carrier",  "water-carrier",  CoinType.Copper, 15f,  "sluicecraft", "haulage", "obedience"),
            J("terrace_builder","terrace-builder",CoinType.Copper, 12f,  "terracing", "stonework", "hard_labor"));

        AddArchetype("captain",
            J("sentry",         "sentry",         CoinType.Copper,  7.5f,"watchkeeping", "vigilance", "discipline"),
            J("wall_mender",    "wall-mender",    CoinType.Copper,  9f,  "fortification", "stonework", "hard_labor"),
            J("armoury_hand",   "armoury-hand",   CoinType.Copper,  9f,  "arms_care", "fletching", "armoury_lore"),
            J("runner",         "runner",         CoinType.Copper, 12f,  "signalling", "athletics", "obedience"),
            J("turnkey",        "turnkey",        CoinType.Copper,  9f,  "jailcraft", "watchkeeping", "vigilance"),
            J("gate_ward",      "gate-ward",      CoinType.Copper,  9f,  "gatekeeping", "billeting", "bastion_eye"),
            J("scout",          "scout",          CoinType.Copper,  7.5f,"manhunt", "ambuscade", "patrol"),
            J("questioner_hand","questioner's hand",CoinType.Copper, 7.5f,"interrogation", "clerkship", "scrutiny"),
            J("camp_scrounger", "camp-scrounger", CoinType.Copper, 12f,  "looting", "provisioning", "self_preservation"));

        AddArchetype("steward",
            J("scullion",       "scullion",       CoinType.Copper, 15f,  "dirty_labor", "cookery", "obedience"),
            J("serving_hand",   "serving-hand",   CoinType.Copper, 10f,  "precedence", "courtesy", "deference"),
            J("store_tallier",  "store-tallier",  CoinType.Copper,  7.5f,"provisioning", "tallycraft", "bookkeeping"),
            J("stable_lad",     "stable-lad",     CoinType.Copper, 12f,  "horsemanship", "grooming", "dirty_labor"),
            J("falconer_boy",   "falconer's boy", CoinType.Copper, 10f,  "falconry", "beast_sense", "patience"),
            J("hall_musician",  "hall musician",  CoinType.Copper,  7.5f,"lute_playing", "solfege", "courtesy"));

        AddArchetype("merchant",
            J("porter",         "porter",         CoinType.Copper, 12f,  "haulage", "stevedoring", "hard_labor"),
            J("counting_clerk", "counting-clerk", CoinType.Copper,  6f,  "bookkeeping", "arithmetic_logic", "scrutiny"),
            J("stall_crier",    "stall-crier",    CoinType.Copper,  9f,  "hawking", "bargaining", "banter"),
            J("goods_appraiser","appraiser",      CoinType.Copper,  6f,  "appraisal", "coin_eye", "hallmark"),
            J("carter",         "carter",         CoinType.Copper, 10f,  "carting", "haulage", "horsemanship"),
            J("back_runner",    "back-door runner",CoinType.Copper,  9f, "throng", "rooftops", "receiving"));

        AddArchetype("innkeeper",
            J("potboy",         "potboy",         CoinType.Copper, 12f,  "tapstering", "haulage", "obedience"),
            J("kitchen_hand",   "kitchen-hand",   CoinType.Copper, 10f,  "cookery", "firecraft", "dirty_labor"),
            J("ostler",         "ostler",         CoinType.Copper, 12f,  "grooming", "horse_sense", "dirty_labor"),
            J("bouncer",        "bouncer",        CoinType.Copper,  9f,  "brawling", "iron_nerves", "vigilance"),
            J("card_dealer",    "card-dealer",    CoinType.Copper,  9f,  "card_sharping", "gambling", "sharp_practice"));

        AddArchetype("priest",
            J("altar_server",   "altar-server",   CoinType.Copper, 12f,  "liturgy", "incensing", "reverence"),
            J("bell_hand",      "bell-ringer",    CoinType.Copper, 10f,  "bell_ringing", "athletics", "obedience"),
            J("grave_hand",     "grave-hand",     CoinType.Copper, 12f,  "sextonry", "hard_labor", "dirty_labor"),
            J("door_keeper",    "door-keeper",    CoinType.Copper, 12f,  "asylum", "vigil", "liturgy"),
            J("hired_mourner",  "hired mourner",  CoinType.Copper, 10f,  "obsequies", "mourning", "elegy"),
            J("almoner",        "almoner's hand", CoinType.Copper,  9f,  "almsgiving", "tithing", "humility"),
            J("exorcist_acolyte","exorcist's acolyte",CoinType.Copper, 9f,"exorcism", "superstition", "dread"));

        AddArchetype("drover",
            J("night_drover",   "night drover",   CoinType.Copper,  9f,  "rustling", "droving", "night_ear"),
            J("brand_hand",     "brand-hand",     CoinType.Copper, 10f,  "branding", "roping", "hard_labor"));

        AddArchetype("monk",
            J("copyist",        "copyist",        CoinType.Copper,  9f,  "copying", "calligraphy", "patience"),
            J("garden_brother", "garden-hand",    CoinType.Copper, 12f,  "herblore", "tillage", "cloister_silence"),
            J("infirmary_hand", "infirmary-hand", CoinType.Copper, 10f,  "physic", "empathy", "dirty_labor"),
            J("illuminator",    "illuminator",    CoinType.Copper,  6f,  "illumination", "calligraphy", "patience"));

        // The trades and callings of a dense city.
        AddArchetype("cobbler",
            J("heel_mender",    "heel-mender",    CoinType.Copper, 10f,  "cobbling", "thrift", "diligence"),
            J("upper_cutter",   "upper-cutter",   CoinType.Copper,  9f,  "cobbling", "steady_hand", "appraisal"),
            J("boot_blacker",   "boot-blacker",   CoinType.Copper, 15f,  "dirty_labor", "courtesy", "deference"),
            J("shoe_runner",    "shoe-runner",    CoinType.Copper, 12f,  "throng", "athletics", "obedience"));

        AddArchetype("tailor",
            J("seamster",       "seamster",       CoinType.Copper,  9f,  "tailoring", "threadwork", "patience"),
            J("buttonholer",    "buttonholer",    CoinType.Copper, 10f,  "steady_hand", "tailoring", "diligence"),
            J("cloth_presser",  "cloth-presser",  CoinType.Copper, 12f,  "firecraft", "hard_labor", "obedience"),
            J("fitting_hand",   "fitting-hand",   CoinType.Copper,  9f,  "courtesy", "physiognomy", "flattery"));

        AddArchetype("chandler",
            J("wick_dipper",    "wick-dipper",    CoinType.Copper, 12f,  "chandlery", "patience", "obedience"),
            J("tallow_renderer","tallow-renderer",CoinType.Copper, 10f,  "chandlery", "iron_stomach", "dirty_labor"),
            J("soap_boiler",    "soap-boiler",    CoinType.Copper, 10f,  "perfumery", "firecraft", "hard_labor"),
            J("lamp_seller",    "lamp-seller",    CoinType.Copper, 12f,  "hawking", "bargaining", "banter"));

        AddArchetype("butcher",
            J("slaughter_hand", "slaughter-hand", CoinType.Copper, 10f,  "butchery", "cold_blood", "hard_labor"),
            J("offal_boy",      "offal-boy",      CoinType.Copper, 15f,  "dirty_labor", "iron_stomach", "obedience"),
            J("sausage_stuffer","sausage-stuffer",CoinType.Copper, 12f,  "cookery", "butchery", "diligence"),
            J("meat_crier",     "meat-crier",     CoinType.Copper, 12f,  "hawking", "bargaining", "banter"));

        AddArchetype("tanner",
            J("flesher",        "flesher",        CoinType.Copper,  9f,  "tanning", "hard_labor", "iron_stomach"),
            J("pit_turner",     "pit-turner",     CoinType.Copper, 10f,  "tanning", "dirty_labor", "endurance"),
            J("bark_grinder",   "bark-grinder",   CoinType.Copper, 12f,  "millcraft", "hard_labor", "obedience"),
            J("dung_gatherer",  "dung-gatherer",  CoinType.Copper, 15f,  "dirty_labor", "streetwise", "thrift"));

        AddArchetype("potter",
            J("clay_wedger",    "clay-wedger",    CoinType.Copper, 12f,  "potcraft", "hard_labor", "patience"),
            J("kiln_stoker",    "kiln-stoker",    CoinType.Copper, 10f,  "firecraft", "vigil", "endurance"),
            J("clay_digger",    "clay-digger",    CoinType.Copper, 12f,  "digging", "haulage", "hard_labor"),
            J("glaze_painter",  "glaze-painter",  CoinType.Copper,  7.5f,"aesthetic", "potcraft", "steady_hand"));

        AddArchetype("apothecary",
            J("pill_roller",    "pill-roller",    CoinType.Copper, 10f,  "physic", "steady_hand", "diligence"),
            J("simples_runner", "simples-runner", CoinType.Copper, 12f,  "simpling", "herblore", "forage_lore"),
            J("still_watcher",  "still-watcher",  CoinType.Copper,  9f,  "alchemy", "vigil", "patience"),
            J("powder_grinder", "powder-grinder", CoinType.Copper, 12f,  "apothecary_nose", "hard_labor", "obedience"));

        AddArchetype("barber",
            J("lather_boy",     "lather-boy",     CoinType.Copper, 15f,  "courtesy", "banter", "obedience"),
            J("patient_holder", "patient-holder", CoinType.Copper, 12f,  "brute_force", "iron_nerves", "surgery"),
            J("strop_hand",     "strop-hand",     CoinType.Copper, 12f,  "arms_care", "steady_hand", "diligence"),
            J("tooth_puller",   "tooth-puller",   CoinType.Copper,  7.5f,"tooth_drawing", "anatomy_lore", "sangfroid"));

        AddArchetype("laundress",
            J("wash_hand",      "wash-hand",      CoinType.Copper, 15f,  "laundering", "hard_labor", "endurance"),
            J("linen_wringer",  "linen-wringer",  CoinType.Copper, 15f,  "laundering", "brute_force", "obedience"),
            J("bundle_carrier", "bundle-carrier", CoinType.Copper, 12f,  "haulage", "throng", "receiving"));

        AddArchetype("water_carrier",
            J("well_drawer",    "well-drawer",    CoinType.Copper, 15f,  "water_bearing", "hard_labor", "endurance"),
            J("round_runner",   "round-runner",   CoinType.Copper, 12f,  "water_bearing", "surefoot", "throng"));
    }

    // ── Building helpers ───────────────────────────────────────────────────────

    private static Job J(string id, string title, CoinType coin, float daysPerCoin, params string[] mmIds)
        => new(id, title, coin, daysPerCoin, mmIds);

    private void AddArchetype(string archetypeId, params Job[] jobs)
    {
        var ids = new List<string>();
        foreach (var job in jobs)
        {
            _jobsById[job.Id] = job;
            ids.Add(job.Id);
        }
        _jobsByArchetype[archetypeId] = ids;
    }

    /// <summary>Points an archetype at jobs already defined elsewhere (shared job objects).</summary>
    private void LinkArchetype(string archetypeId, params string[] jobIds)
        => _jobsByArchetype[archetypeId] = jobIds.ToList();

    private void ValidateModusMentisReferences()
    {
        foreach (var job in _jobsById.Values)
        {
            if (job.ModusMentisIds.Length != 3)
                Console.WriteLine($"JobRegistry: job '{job.Id}' has {job.ModusMentisIds.Length} modi mentis (expected 3).");
            foreach (var mmId in job.ModusMentisIds)
                if (ModusMentisRegistry.Instance.GetModusMentis(mmId) == null)
                    Console.WriteLine($"JobRegistry: job '{job.Id}' references unknown modus mentis '{mmId}'.");
        }
    }

    /// <summary>Process-stable 32-bit FNV-1a hash of the npc id (String.GetHashCode is randomized).</summary>
    private static int StableSeed(string npcId)
    {
        unchecked
        {
            const uint offset = 2166136261;
            const uint prime  = 16777619;
            uint hash = offset;
            foreach (char c in npcId)
            {
                hash ^= c;
                hash *= prime;
            }
            return (int)hash;
        }
    }
}
