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
            J("hay_hauler",     "hay-hauler",     CoinType.Copper, 10f,  "haulage", "cellarcraft", "hard_labor"));

        // The hayward patrols the field margin: a small themed pool reusing the boundary jobs.
        LinkArchetype("hayward", "margin_watch", "reed_cutter", "stone_picker");
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
