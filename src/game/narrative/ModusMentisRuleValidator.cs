using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Cathedral.Fight;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Launch-time validation of the modus-mentis content hard rules, plus the
/// <c>--mm-audit</c> report that also tracks the soft statistical targets.
///
/// Hard rules (violations throw at startup, see <see cref="ValidateOrThrow"/>):
///   R1  no MM has both Thinking and Action (except exempt special first skills, see below)
///   R2  every MM has at least one of Observation / Thinking / Action
///   R3  every MM has at most 3 functions, without duplicates
///   R4  Semantic memory requires Thinking; Sensory requires Observation; Procedural requires Action
///   R5  Organs is exactly 1 body-region id XOR exactly 2 distinct organ ids, all ids canonical
///   R6  every organ and every body region is related to at least 5 MMs
///   R7  the organ mediums of all skills whose MAIN MM this is are a subset of the MM's Organs
///   R8  no fighting skill has more than 2 organ mediums
///   R9  MM has Fighting ⇔ it is referenced by at least one fighting skill (main or secondary)
///   R10 every organ / region has exactly one correctly-scoped IMaxLevelContributionStat
///   R11 every organ / region of every anatomy has at least 3 MMs that anatomy can learn
///   R12 every MM is learnable by at least one anatomy
///   R13 a MM with neither Thinking nor Action is MoralLevel.Medium — nothing reads its morality
///
/// Soft targets (reported by the audit, never fatal):
///   ~80% two-organ MMs vs ~20% one-region MMs
///   ~20% Low / 60% Medium / 20% High morality
///   ~10% discrete MMs
///   ~33% / 33% / 33% memory-type split
/// </summary>
public static class ModusMentisRuleValidator
{
    private const int MinRelatedModiMentis = 5;

    /// <summary>
    /// R11's floor: how many modi mentis an anatomy must be able to learn for each of its own organs
    /// and regions. Lower than <see cref="MinRelatedModiMentis"/> because that one counts the whole
    /// catalogue, while this counts only what one body can actually hold — a beast is barred from
    /// every Speaking and every lettered modus mentis, so its cerebrum will never see the numbers a
    /// human's does. Three is the floor at which a source still offers a choice rather than a single
    /// forced skill.
    /// </summary>
    private const int MinPerAnatomy = 3;

    /// <summary>
    /// MMs exempt from R1 (Thinking+Action exclusivity). Childhood Reminescence is the special
    /// temporary first skill of the intro phase and deliberately carries all three of
    /// Observation/Thinking/Action; every other rule still applies to it.
    /// </summary>
    private static readonly HashSet<string> ThinkingActionExemptIds = new() { "childhood_reminescence" };

    // ── Canonical anatomy ids (reflection over Organ / BodyPart subclasses) ──

    private static HashSet<string>? _organIds;
    private static HashSet<string>? _regionIds;

    /// <summary>All canonical organ ids (human + beast; shared ids like "legs" appear once).</summary>
    public static HashSet<string> OrganIds => _organIds ??= DiscoverIds<Organ>(o => o.Id);

    /// <summary>All canonical body-region ids (human + beast; shared ids like "trunk" appear once).</summary>
    public static HashSet<string> RegionIds => _regionIds ??= DiscoverIds<BodyPart>(b => b.Id);

    private static HashSet<string> DiscoverIds<T>(Func<T, string> idOf) where T : class
    {
        var ids = new HashSet<string>();
        var baseType = typeof(T);
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
                     .Where(t => t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t)
                                 && t.GetConstructor(Type.EmptyTypes) != null))
        {
            if (Activator.CreateInstance(type) is T instance)
                ids.Add(idOf(instance));
        }
        return ids;
    }

    // ── Hard rules ────────────────────────────────────────────────────────────

    /// <summary>
    /// Runs every hard rule against the full MM registry + fighting skill registry and throws a
    /// single <see cref="InvalidOperationException"/> listing all violations. Called once at
    /// startup (Program.cs) so a rule-breaking MM can never reach the game.
    /// </summary>
    public static void ValidateOrThrow()
    {
        var violations = CollectHardRuleViolations();
        if (violations.Count > 0)
            throw new InvalidOperationException(
                $"Modus mentis hard-rule validation failed ({violations.Count} violation(s)):\n  - " +
                string.Join("\n  - ", violations));
    }

    /// <summary>All hard-rule violations, one human-readable line each (empty = content is valid).</summary>
    public static List<string> CollectHardRuleViolations()
    {
        var violations = new List<string>();
        var all = ModusMentisRegistry.Instance.GetAllModiMentis();
        var skills = FightingSkillRegistry.Instance.GetAll().ToList();

        foreach (var mm in all)
        {
            var fns = mm.Functions;

            // R3 — at most 3 functions, no duplicates
            if (fns.Length > 3)
                violations.Add($"[R3] {mm.ModusMentisId}: has {fns.Length} functions (max 3)");
            if (fns.Distinct().Count() != fns.Length)
                violations.Add($"[R3] {mm.ModusMentisId}: duplicate entries in Functions");

            // R1 — Thinking and Action are mutually exclusive (special first-skill MMs exempt)
            if (!ThinkingActionExemptIds.Contains(mm.ModusMentisId)
                && fns.Contains(ModusMentisFunction.Thinking) && fns.Contains(ModusMentisFunction.Action))
                violations.Add($"[R1] {mm.ModusMentisId}: has both Thinking and Action");

            // R2 — at least one of Observation / Thinking / Action
            if (!fns.Contains(ModusMentisFunction.Observation)
                && !fns.Contains(ModusMentisFunction.Thinking)
                && !fns.Contains(ModusMentisFunction.Action))
                violations.Add($"[R2] {mm.ModusMentisId}: needs at least one of Observation/Thinking/Action");

            // R4 — memory type must match a function
            switch (mm.MemoryType)
            {
                case Memory.ModusMentisMemoryType.Semantic when !fns.Contains(ModusMentisFunction.Thinking):
                    violations.Add($"[R4] {mm.ModusMentisId}: Semantic memory requires the Thinking function");
                    break;
                case Memory.ModusMentisMemoryType.Sensory when !fns.Contains(ModusMentisFunction.Observation):
                    violations.Add($"[R4] {mm.ModusMentisId}: Sensory memory requires the Observation function");
                    break;
                case Memory.ModusMentisMemoryType.Procedural when !fns.Contains(ModusMentisFunction.Action):
                    violations.Add($"[R4] {mm.ModusMentisId}: Procedural memory requires the Action function");
                    break;
            }

            // R13 — morality is only ever read off a thinking or an action modus mentis: the goal
            // filter asks the thinker, the willingness filter and the impossibility rule ask the
            // actor. A MM that does neither has nowhere for a MoralLevel to be consulted, so a
            // non-Medium one is a claim the engine cannot honour — it reads as character in the
            // memory panel, skews the audit's 20/60/20 distribution, and changes nothing in play.
            if (!fns.Contains(ModusMentisFunction.Thinking)
                && !fns.Contains(ModusMentisFunction.Action)
                && mm.MoralLevel != MoralLevel.Medium)
                violations.Add($"[R13] {mm.ModusMentisId}: is {mm.MoralLevel} morality but has neither "
                               + "Thinking nor Action — nothing would ever read it (must be Medium)");

            // R5 — exactly 1 region XOR exactly 2 distinct organs, canonical ids only
            var organs = mm.Organs;
            if (organs.Length == 1)
            {
                if (!RegionIds.Contains(organs[0]))
                    violations.Add($"[R5] {mm.ModusMentisId}: single anatomy entry '{organs[0]}' is not a body region"
                                   + (OrganIds.Contains(organs[0]) ? " (it is an organ — a lone organ is not allowed)" : " (unknown id)"));
            }
            else if (organs.Length == 2)
            {
                if (organs[0] == organs[1])
                    violations.Add($"[R5] {mm.ModusMentisId}: duplicate organ '{organs[0]}'");
                foreach (var id in organs)
                    if (!OrganIds.Contains(id))
                        violations.Add($"[R5] {mm.ModusMentisId}: '{id}' is not an organ"
                                       + (RegionIds.Contains(id) ? " (regions cannot be mixed with organs)" : " (unknown id)"));
            }
            else
            {
                violations.Add($"[R5] {mm.ModusMentisId}: has {organs.Length} anatomy entries (must be 1 region or 2 organs)");
            }

            // R7 — organ mediums of main skills must all be related organs of the MM
            var mainOrganMediums = skills
                .Where(s => s.RequiredModusMentisId == mm.ModusMentisId)
                .SelectMany(s => s.Mediums)
                .Where(m => m.Type == MediumType.OrganMedium && !string.IsNullOrEmpty(m.OrganId))
                .Select(m => m.OrganId!)
                .Distinct()
                .ToList();
            foreach (var organId in mainOrganMediums.Where(o => !organs.Contains(o)))
                violations.Add($"[R7] {mm.ModusMentisId}: main skill uses organ medium '{organId}' not in its related organs [{string.Join(", ", organs)}]");
        }

        // R8 — a fighting skill has at most 2 organ mediums
        foreach (var skill in skills)
        {
            int organMediums = skill.Mediums.Count(m => m.Type == MediumType.OrganMedium);
            if (organMediums > 2)
                violations.Add($"[R8] skill {skill.SkillId}: has {organMediums} organ mediums (max 2)");
        }

        // R9 — Fighting function ⇔ referenced by a fighting skill (and references must resolve)
        var referencedIds = skills
            .SelectMany(s => s.SecondaryModusMentisIds.Append(s.RequiredModusMentisId))
            .ToHashSet();
        foreach (var mm in all)
        {
            bool hasFighting = mm.Functions.Contains(ModusMentisFunction.Fighting);
            bool referenced = referencedIds.Contains(mm.ModusMentisId);
            if (hasFighting && !referenced)
                violations.Add($"[R9] {mm.ModusMentisId}: has the Fighting function but no fighting skill references it");
            if (referenced && !hasFighting)
                violations.Add($"[R9] {mm.ModusMentisId}: is referenced by fighting skills but lacks the Fighting function");
        }
        var knownIds = all.Select(m => m.ModusMentisId).ToHashSet();
        foreach (var id in referencedIds.Where(id => !knownIds.Contains(id)))
            violations.Add($"[R9] fighting skills reference unknown modus mentis id '{id}'");

        // R10 — every anatomy source a MM can name must own a max-level contribution stat
        violations.AddRange(CollectMaxLevelStatViolations());

        // R6 — coverage: every organ and region related to at least 5 MMs
        var organCounts = CountCoverage(all, OrganIds, organArity: 2);
        var regionCounts = CountCoverage(all, RegionIds, organArity: 1);
        foreach (var (id, count) in organCounts.Where(kv => kv.Value < MinRelatedModiMentis).OrderBy(kv => kv.Key))
            violations.Add($"[R6] organ '{id}': related to only {count} MM(s) (min {MinRelatedModiMentis})");
        foreach (var (id, count) in regionCounts.Where(kv => kv.Value < MinRelatedModiMentis).OrderBy(kv => kv.Key))
            violations.Add($"[R6] region '{id}': related to only {count} MM(s) (min {MinRelatedModiMentis})");

        // R11 / R12 — anatomy reach
        violations.AddRange(CollectAnatomyViolations(all));

        return violations;
    }

    /// <summary>
    /// R11 and R12 — the two rules that keep the anatomy gate honest.
    ///
    /// <para><b>R12</b>: a modus mentis no anatomy can learn is dead content that nothing else
    /// reports. It happens by pairing organs from two anatomies — <c>fangs</c> with <c>teeths</c>,
    /// <c>claws</c> with <c>arms</c> — which reads perfectly well and reaches nobody. Five modi mentis
    /// were in exactly that state when this rule was written.</para>
    ///
    /// <para><b>R11</b>: the counterweight to <see cref="ModusMentis.RequiredCapabilities"/>. Barring a
    /// beast from speech and letters is right, but done freely it would leave a wolf's cerebrum with
    /// one learnable skill and its heart with none — a body part that exists, contributes to level
    /// caps, and has nothing to spend itself on. Three per source, per anatomy, is the floor.</para>
    /// </summary>
    private static List<string> CollectAnatomyViolations(List<ModusMentis> all)
    {
        var violations = new List<string>();

        foreach (var mm in all)
        {
            if (!ModusMentisAnatomy.AllAnatomies.Any(a => ModusMentisAnatomy.IsLearnableBy(mm, a)))
                violations.Add($"[R12] {mm.ModusMentisId}: no anatomy can learn it "
                               + $"(organs [{string.Join(", ", mm.Organs)}], requires {mm.RequiredCapabilities}) "
                               + "— usually organs from two different anatomies in one pair");
        }

        foreach (var anatomy in ModusMentisAnatomy.AllAnatomies)
        {
            foreach (var (source, count) in AnatomyCoverage(all, anatomy).OrderBy(kv => kv.Key))
                if (count < MinPerAnatomy)
                    violations.Add($"[R11] {anatomy} '{source}': only {count} learnable MM(s) "
                                   + $"(min {MinPerAnatomy})");
        }

        return violations;
    }

    /// <summary>
    /// Per anatomy: how many modi mentis a body of that anatomy could learn for each organ and region
    /// it owns. Counts learnability, not mere relation — a modus mentis it is barred from by
    /// capability does not count towards its floor, which is the entire point of R11.
    /// </summary>
    public static Dictionary<string, int> AnatomyCoverage(List<ModusMentis> all, AnatomyType anatomy)
    {
        var counts = ModusMentisAnatomy.SourcesOf(anatomy).ToDictionary(id => id, _ => 0);
        foreach (var mm in all.Where(m => ModusMentisAnatomy.IsLearnableBy(m, anatomy)))
            foreach (var id in mm.Organs.Distinct())
                if (counts.ContainsKey(id))
                    counts[id]++;
        return counts;
    }

    /// <summary>How many MMs relate to each id in <paramref name="ids"/> (via an Organs array of the given arity).</summary>
    private static Dictionary<string, int> CountCoverage(List<ModusMentis> all, HashSet<string> ids, int organArity)
    {
        var counts = ids.ToDictionary(id => id, _ => 0);
        foreach (var mm in all.Where(m => m.Organs.Length == organArity))
            foreach (var id in mm.Organs.Distinct())
                if (counts.ContainsKey(id))
                    counts[id]++;
        return counts;
    }

    /// <summary>
    /// R10 — every organ and every body region must own exactly one correctly-scoped
    /// <see cref="IMaxLevelContributionStat"/>.
    /// <para>
    /// <see cref="PartyMember.GetMaxLevelForModusMentis"/> resolves each <c>Organs</c> entry to the
    /// first contribution stat whose <c>RelatedOrganId</c> / <c>RelatedBodyPartId</c> matches, and
    /// contributes <b>+0</b> when there is none. So an organ added without its stat silently caps
    /// every modus mentis related to it at level 1 — nothing throws, nothing logs, the number in the
    /// memory menu is just wrong — and two stats on one id make which curve applies arbitrary.
    /// This is the same class of silent-typo failure R5 catches for the ids themselves.
    /// </para>
    /// </summary>
    private static List<string> CollectMaxLevelStatViolations()
    {
        var violations = new List<string>();
        var stats = DerivedStat.DiscoverAll().Where(s => s is IMaxLevelContributionStat).ToList();

        foreach (var stat in stats)
        {
            // The resolver matches on the organ / body-part key only, so an organ-part-scoped
            // contribution stat would never be found (paired organs are typed at organ level).
            if (stat.RelatedOrganPartId != null)
                violations.Add($"[R10] stat {stat.Name}: is scoped to organ part '{stat.RelatedOrganPartId}'"
                               + " (max-level stats must be scoped to an organ or a body region)");

            switch (stat.RelatedOrganId, stat.RelatedBodyPartId)
            {
                case (null, null):
                    violations.Add($"[R10] stat {stat.Name}: names neither an organ nor a body region");
                    break;
                case ({ } organId, { } regionId):
                    violations.Add($"[R10] stat {stat.Name}: names both organ '{organId}' and region '{regionId}'");
                    break;
                case ({ } organId, null) when !OrganIds.Contains(organId):
                    violations.Add($"[R10] stat {stat.Name}: '{organId}' is not a canonical organ id"
                                   + (RegionIds.Contains(organId) ? " (it is a region — use RelatedBodyPartId)" : ""));
                    break;
                case (null, { } regionId) when !RegionIds.Contains(regionId):
                    violations.Add($"[R10] stat {stat.Name}: '{regionId}' is not a canonical body-region id"
                                   + (OrganIds.Contains(regionId) ? " (it is an organ — use RelatedOrganId)" : ""));
                    break;
            }
        }

        // Exactly one stat per anatomy source: none means a silent +0, several means an arbitrary pick.
        var byId = stats
            .Select(s => s.RelatedOrganId ?? s.RelatedBodyPartId)
            .Where(id => id != null)
            .GroupBy(id => id!)
            .ToDictionary(g => g.Key, g => g.Count());
        foreach (var id in OrganIds.Concat(RegionIds).OrderBy(id => id))
        {
            int count = byId.GetValueOrDefault(id, 0);
            if (count == 0)
                violations.Add($"[R10] '{id}': has no max-level contribution stat "
                               + "(every modus mentis related to it would be capped at level 1)");
            else if (count > 1)
                violations.Add($"[R10] '{id}': has {count} max-level contribution stats (must be exactly 1)");
        }

        return violations;
    }

    /// <summary>How many of <paramref name="ids"/> own a max-level contribution stat.</summary>
    private static int CountMaxLevelStats(HashSet<string> ids) =>
        DerivedStat.DiscoverAll()
            .Where(s => s is IMaxLevelContributionStat)
            .Select(s => s.RelatedOrganId ?? s.RelatedBodyPartId)
            .Count(id => id != null && ids.Contains(id));

    // ── Audit report (--mm-audit) ─────────────────────────────────────────────

    /// <summary>
    /// Full content-health report: per-MM table, hard-rule violations, per-organ/region coverage,
    /// and soft-target statistics. Informational only — never throws.
    /// </summary>
    public static string BuildAuditReport()
    {
        var sb = new StringBuilder();
        var all = ModusMentisRegistry.Instance.GetAllModiMentis()
            .OrderBy(m => m.ModusMentisId).ToList();
        var skills = FightingSkillRegistry.Instance.GetAll().ToList();

        sb.AppendLine("═══ MODUS MENTIS AUDIT ═══");
        sb.AppendLine($"{all.Count} modi mentis, {skills.Count} fighting skills, "
                      + $"{OrganIds.Count} organ ids, {RegionIds.Count} region ids");
        sb.AppendLine();

        // ── Per-MM table ──
        sb.AppendLine("── modi mentis ──");
        foreach (var mm in all)
        {
            var mainSkills = skills.Where(s => s.RequiredModusMentisId == mm.ModusMentisId).Select(s => s.SkillId);
            var secSkills = skills.Where(s => s.SecondaryModusMentisIds.Contains(mm.ModusMentisId)).Select(s => s.SkillId);
            sb.Append($"{mm.ModusMentisId} | fn={string.Join("+", mm.Functions)} | organs={string.Join(",", mm.Organs)}");
            sb.Append($" | mem={mm.MemoryType} | moral={mm.MoralLevel} | discrete={(mm.ActsDiscretely ? "yes" : "no")}");
            if (mm.RequiredCapabilities != AnatomyCapability.None)
                sb.Append($" | needs={mm.RequiredCapabilities}");
            sb.Append($" | anatomy:[{string.Join(",", ModusMentisAnatomy.AllAnatomies.Where(a => ModusMentisAnatomy.IsLearnableBy(mm, a)))}]");
            if (mainSkills.Any()) sb.Append($" | main:[{string.Join(",", mainSkills)}]");
            if (secSkills.Any()) sb.Append($" | sec:[{string.Join(",", secSkills)}]");
            sb.AppendLine();
        }
        sb.AppendLine();

        // ── Hard-rule violations ──
        var violations = CollectHardRuleViolations();
        sb.AppendLine($"── hard rules: {(violations.Count == 0 ? "OK" : violations.Count + " violation(s)")} ──");
        foreach (var v in violations)
            sb.AppendLine("  " + v);
        sb.AppendLine();

        // ── Coverage ──
        var organCounts = CountCoverage(all, OrganIds, organArity: 2);
        var regionCounts = CountCoverage(all, RegionIds, organArity: 1);
        sb.AppendLine($"── coverage (min {MinRelatedModiMentis} each) ──");
        sb.AppendLine("organs:  " + string.Join("  ", organCounts.OrderBy(kv => kv.Value).ThenBy(kv => kv.Key)
            .Select(kv => $"{kv.Key}={kv.Value}{(kv.Value < MinRelatedModiMentis ? "!" : "")}")));
        sb.AppendLine("regions: " + string.Join("  ", regionCounts.OrderBy(kv => kv.Value).ThenBy(kv => kv.Key)
            .Select(kv => $"{kv.Key}={kv.Value}{(kv.Value < MinRelatedModiMentis ? "!" : "")}")));

        // Per-anatomy reach: what a body of each anatomy can actually learn, source by source. The
        // catalogue-wide numbers above say nothing about this — a wolf is barred from every Speaking
        // and every lettered modus mentis, so its counts are a different (much smaller) table.
        sb.AppendLine();
        sb.AppendLine($"── learnable per anatomy (min {MinPerAnatomy} each, R11) ──");
        foreach (var anatomy in ModusMentisAnatomy.AllAnatomies)
        {
            int learnable = all.Count(m => ModusMentisAnatomy.IsLearnableBy(m, anatomy));
            sb.AppendLine($"{anatomy,-6} {learnable}/{all.Count} modi mentis");
            sb.AppendLine("   " + string.Join("  ", AnatomyCoverage(all, anatomy)
                .OrderBy(kv => kv.Value).ThenBy(kv => kv.Key)
                .Select(kv => $"{kv.Key}={kv.Value}{(kv.Value < MinPerAnatomy ? "!" : "")}")));
        }
        sb.AppendLine();

        // Max-level contribution stats: a source without one grants +0 and caps its MMs at level 1.
        int organStats  = CountMaxLevelStats(OrganIds);
        int regionStats = CountMaxLevelStats(RegionIds);
        sb.AppendLine($"max-level stats: {organStats}/{OrganIds.Count} organs, "
                      + $"{regionStats}/{RegionIds.Count} regions"
                      + (organStats == OrganIds.Count && regionStats == RegionIds.Count ? "" : "   ← see R10 above"));
        sb.AppendLine();

        // ── Soft targets ──
        sb.AppendLine("── soft targets ──");
        int n = all.Count;
        int twoOrgan = all.Count(m => m.Organs.Length == 2);
        int oneRegion = all.Count(m => m.Organs.Length == 1);
        sb.AppendLine($"anatomy:  2-organ {Pct(twoOrgan, n)} vs 1-region {Pct(oneRegion, n)}   (target ~80% / ~20%)");
        sb.AppendLine($"morality: Low {Pct(all.Count(m => m.MoralLevel == MoralLevel.Low), n)}, "
                      + $"Medium {Pct(all.Count(m => m.MoralLevel == MoralLevel.Medium), n)}, "
                      + $"High {Pct(all.Count(m => m.MoralLevel == MoralLevel.High), n)}   (target ~20/60/20)");
        sb.AppendLine($"discrete: {Pct(all.Count(m => m.ActsDiscretely), n)}   (target ~10%)");
        sb.AppendLine($"memory:   Procedural {Pct(all.Count(m => m.MemoryType == Memory.ModusMentisMemoryType.Procedural), n)}, "
                      + $"Sensory {Pct(all.Count(m => m.MemoryType == Memory.ModusMentisMemoryType.Sensory), n)}, "
                      + $"Semantic {Pct(all.Count(m => m.MemoryType == Memory.ModusMentisMemoryType.Semantic), n)}   (target ~33/33/33)");

        return sb.ToString();
    }

    private static string Pct(int count, int total) =>
        total == 0 ? "0%" : $"{count} ({100.0 * count / total:0.#}%)";
}
