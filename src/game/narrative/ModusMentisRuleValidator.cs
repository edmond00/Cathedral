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

        // R6 — coverage: every organ and region related to at least 5 MMs
        var organCounts = CountCoverage(all, OrganIds, organArity: 2);
        var regionCounts = CountCoverage(all, RegionIds, organArity: 1);
        foreach (var (id, count) in organCounts.Where(kv => kv.Value < MinRelatedModiMentis).OrderBy(kv => kv.Key))
            violations.Add($"[R6] organ '{id}': related to only {count} MM(s) (min {MinRelatedModiMentis})");
        foreach (var (id, count) in regionCounts.Where(kv => kv.Value < MinRelatedModiMentis).OrderBy(kv => kv.Key))
            violations.Add($"[R6] region '{id}': related to only {count} MM(s) (min {MinRelatedModiMentis})");

        return violations;
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
