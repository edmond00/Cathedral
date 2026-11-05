using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.Reminescence;

/// <summary>
/// Launch-time validation of the childhood-reminescence outcome rules.
///
/// The protagonist must leave the childhood phase holding at least one modusMentis of each
/// memory type. That guarantee rests on three legs:
///   1. the Childhood Memory MM is always granted, and is Sensory  (structural — not checked here,
///      it is granted unconditionally by the phase rather than by any fragment)
///   2. every fragment of the entry reminescence grants at least one Semantic MM   → [C1]
///   3. every terminal fragment grants at least one Procedural MM                  → [C2]
///
/// And one more, on the offices rather than the memory types: every path grants a general thinker,
/// a Thinking modus mentis that is not a will → [C3]; and every path grants a speaker, a modus mentis
/// with the Speaking function, will or not → [C4].
///
/// Since every playthrough remembers exactly one entry fragment and exactly one terminal
/// fragment, C1 + C2 hold for every possible path through the catalog, whatever the player picks.
///
/// Violations throw at startup (see <see cref="ValidateOrThrow"/>) so unreachable-by-design
/// content can never reach a run.
/// </summary>
public static class ReminescenceRuleValidator
{
    /// <summary>
    /// Runs every reminescence rule and throws a single <see cref="InvalidOperationException"/>
    /// listing all violations. Called once at startup (Program.cs).
    /// </summary>
    public static void ValidateOrThrow()
    {
        var violations = CollectViolations();
        if (violations.Count > 0)
            throw new InvalidOperationException(
                $"Reminescence hard-rule validation failed ({violations.Count} violation(s)):\n  - " +
                string.Join("\n  - ", violations));
    }

    /// <summary>All violations, one human-readable line each (empty = the catalog is valid).</summary>
    public static List<string> CollectViolations()
    {
        var violations = new List<string>();

        // C1 — every entry fragment must grant a Semantic MM.
        var entry = ReminescenceRegistry.GetEntry();
        foreach (var fragment in entry.Fragments)
        {
            if (!Grants(fragment, ModusMentisMemoryType.Semantic))
                violations.Add(
                    $"[C1] entry fragment '{fragment.Name}' (in '{entry.Id}'): grants no Semantic "
                    + $"modusMentis — has [{Describe(fragment)}]");
        }

        // C2 — every terminal fragment must grant a Procedural MM.
        foreach (var (reminescenceId, fragment) in ReachableFragments(violations))
        {
            if (!fragment.Outcome.IsTerminal) continue;

            if (!Grants(fragment, ModusMentisMemoryType.Procedural))
                violations.Add(
                    $"[C2] terminal fragment '{fragment.Name}' (in '{reminescenceId}'): grants no Procedural "
                    + $"modusMentis — has [{Describe(fragment)}]");
        }

        // C3 — every path leaves a general thinker: a modus mentis with the Thinking function that is not
        // a will. A will thinks only toward its own targets, so a childhood that granted nothing else
        // would leave a body that could think about nothing it did not already want.
        var bare = Paths(ReminescenceRegistry.EntryReminescenceId, new List<FragmentData>())
            .Where(path => !path.Any(f => f.Outcome.SkillTypes.Select(t => ModusMentisRegistry.Instance.GetModusMentis(t))
                                         .Any(mm => mm != null && mm.Functions.Contains(ModusMentisFunction.Thinking) && !mm.IsWill)))
            .ToList();
        foreach (var path in bare.Take(3))
            violations.Add($"[C3] the childhood path {string.Join(" → ", path.Select(f => $"'{f.Name}'"))} grants no "
                         + $"general thinker (Thinking, not a will) — has [{string.Join("; ", path.Select(Describe))}]"
                         + (bare.Count > 3 ? $" (and {bare.Count - 3} more paths)" : ""));

        // C4 — every path leaves a speaker: a modus mentis with the Speaking function. Without one the
        // protagonist has no replies in any conversation. A will counts: one that bears Speaking replies
        // in every conversation, whatever its target.
        var mute = Paths(ReminescenceRegistry.EntryReminescenceId, new List<FragmentData>())
            .Where(path => !path.Any(f => f.Outcome.SkillTypes.Select(t => ModusMentisRegistry.Instance.GetModusMentis(t))
                                         .Any(mm => mm != null && mm.Functions.Contains(ModusMentisFunction.Speaking))))
            .ToList();
        foreach (var path in mute.Take(3))
            violations.Add($"[C4] the childhood path {string.Join(" → ", path.Select(f => $"'{f.Name}'"))} grants no "
                         + $"speaking modus mentis — has [{string.Join("; ", path.Select(Describe))}]"
                         + (mute.Count > 3 ? $" (and {mute.Count - 3} more paths)" : ""));

        return violations;
    }

    /// <summary>Every path of fragments from <paramref name="id"/> to a terminal one. The catalogue is a DAG.</summary>
    private static IEnumerable<List<FragmentData>> Paths(string id, List<FragmentData> so)
    {
        if (ReminescenceRegistry.IsEnd(id) || ReminescenceRegistry.Get(id) is not { } data) { yield return so; yield break; }
        foreach (var fragment in data.Fragments)
        {
            var next = new List<FragmentData>(so) { fragment };
            if (fragment.Outcome.IsTerminal) { yield return next; continue; }
            foreach (var path in Paths(fragment.Outcome.NextReminescenceId, next)) yield return path;
        }
    }

    /// <summary>True when the fragment grants at least one modusMentis of the given memory type.</summary>
    private static bool Grants(FragmentData fragment, ModusMentisMemoryType memoryType) =>
        fragment.Outcome.SkillTypes
            .Select(t => ModusMentisRegistry.Instance.GetModusMentis(t))
            .Any(mm => mm != null && mm.MemoryType == memoryType);

    /// <summary>"survivalism=Procedural, iron_stomach=Procedural" — for readable violation lines.</summary>
    private static string Describe(FragmentData fragment) =>
        fragment.Outcome.SkillTypes.Count == 0
            ? "no modiMentis"
            : string.Join(", ", fragment.Outcome.SkillTypes.Select(t =>
            {
                var mm = ModusMentisRegistry.Instance.GetModusMentis(t);
                return mm == null ? $"{t.Name}=<unregistered>" : $"{mm.ModusMentisId}={mm.MemoryType}";
            }));

    /// <summary>
    /// Every (reminescenceId, fragment) reachable from the entry reminescence, walking the
    /// NextReminescenceId graph. A dangling transition is reported rather than throwing, so the
    /// caller sees it alongside any rule violations.
    /// </summary>
    private static IEnumerable<(string ReminescenceId, FragmentData Fragment)> ReachableFragments(
        List<string> violations)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();
        queue.Enqueue(ReminescenceRegistry.EntryReminescenceId);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (!seen.Add(id)) continue;

            var data = ReminescenceRegistry.Get(id);
            if (data == null)
            {
                violations.Add($"[C0] reminescence '{id}' is referenced but not registered");
                continue;
            }

            foreach (var fragment in data.Fragments)
            {
                yield return (data.Id, fragment);

                if (!fragment.Outcome.IsTerminal)
                    queue.Enqueue(fragment.Outcome.NextReminescenceId);
            }
        }
    }
}
