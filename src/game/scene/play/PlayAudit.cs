using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Scene;

/// <summary>
/// Headless audit of PLAY: where it is offered, what is played, and what it teaches.
///
/// <para>The design target is that about a quarter of the areas a player walks into has something to
/// play at. That is not something any one factory can see — a play is laid by a factory on one of its
/// own objects, by <see cref="PlayPlacement"/> over the finished scene, or by a creature's archetype —
/// so this builds every factory the verb audit sweeps and counts, per factory, the areas where
/// <see cref="PlayVerb"/> accepts something at some hour. The headline is the mean of the
/// factories' shares, so a city of two hundred rooms counts once, as a forest of six areas does: a
/// player meets kinds of place, not rooms. The pooled share over every area is printed beside it.</para>
///
/// <para>Fails (✗) the headline outside 20–32%, a kind of play no sampled scene offers, a play whose
/// lessons no body could hold, and a lesson that does not resolve in the registry.</para>
/// </summary>
public static class PlayAudit
{
    /// <summary>The share of areas holding a play, held to this band (the brief asks for about a quarter).</summary>
    private const double MinShare = 0.20, MaxShare = 0.32;

    /// <summary>At most this many ids per factory: placement is per area, so a score of builds shows its rate.</summary>
    private const int MaxSamples = 20;

    public static string BuildReport()
    {
        var sb       = new StringBuilder();
        var failures = new List<string>();
        var verb     = new PlayVerb();
        var human    = new Protagonist();

        var shares        = new List<double>();
        int pooledAreas   = 0, pooledPlay = 0;
        var byKind        = new Dictionary<Type, (int Areas, int Play)>();
        var byActivity    = new Dictionary<Type, int>();
        var byHolder      = new Dictionary<string, int>(StringComparer.Ordinal);
        var lessons       = new Dictionary<string, int>(StringComparer.Ordinal);
        var wording       = new Dictionary<string, string>(StringComparer.Ordinal);

        sb.AppendLine("=== PLAY AUDIT ===");
        sb.AppendLine($"Every factory the verb audit sweeps, up to {MaxSamples} location ids each, at every period.");
        sb.AppendLine();
        sb.AppendLine($"  {"factory",-34} {"areas",6} {"play",6} {"share",7}");

        foreach (var (label, build, samples) in VerbAudit.Factories())
        {
            int areas = 0, played = 0;
            for (int id = 1; id <= Math.Min(samples, MaxSamples); id++)
            {
                Scene scene;
                try { scene = build(id); }
                catch (Exception ex)
                {
                    failures.Add($"{label} {id}: generation threw — {ex.GetType().Name}: {ex.Message}");
                    continue;
                }

                foreach (var area in scene.AllAreas)
                {
                    areas++;
                    var offered = Offered(scene, area, verb, human).ToList();
                    var kind = area.GetType();
                    byKind.TryGetValue(kind, out var k);
                    byKind[kind] = (k.Areas + 1, k.Play + (offered.Count > 0 ? 1 : 0));
                    if (offered.Count == 0) continue;
                    played++;

                    foreach (var target in offered)
                    {
                        var play = ((IPlayable)target).Play!;
                        byActivity[play.GetType()] = byActivity.GetValueOrDefault(play.GetType()) + 1;
                        string holder = target is SceneNpc npc ? $"creature: {npc.Entity.Archetype.ArchetypeId}" : target.GetType().Name;
                        byHolder[holder] = byHolder.GetValueOrDefault(holder) + 1;
                        // Connectors are many and alike (every building's stair): one line a kind of play.
                        string name = target is ConnectorPointOfInterest ? play.GetType().Name : target.DisplayName;
                        wording.TryAdd($"{holder} / {name}", verb.Verbatim(scene, new PoV(area, TimePeriod.Noon), target));

                        var won = verb.ResolveLesson(new LessonContext(scene, new PoV(area, TimePeriod.Noon), human, target));
                        if (won != null) lessons[won.ModusMentisId] = lessons.GetValueOrDefault(won.ModusMentisId) + 1;
                    }
                }
            }

            double share = areas == 0 ? 0 : (double)played / areas;
            shares.Add(share);
            pooledAreas += areas;
            pooledPlay  += played;
            sb.AppendLine($"  {label,-34} {areas,6} {played,6} {share,7:P0}");
        }

        double mean   = shares.Count == 0 ? 0 : shares.Average();
        double pooled = pooledAreas == 0 ? 0 : (double)pooledPlay / pooledAreas;
        bool ok = mean >= MinShare && mean <= MaxShare;
        sb.AppendLine();
        sb.AppendLine($"  {(ok ? "✓" : "✗")} {mean:P1} of the areas of a kind of place hold a play (mean over {shares.Count} factories, held to {MinShare:P0}–{MaxShare:P0})");
        sb.AppendLine($"    {pooled:P1} of all {pooledAreas} sampled areas, pooled (cities weigh by their rooms)");
        if (!ok) failures.Add($"the share of areas holding a play is {mean:P1}, outside {MinShare:P0}–{MaxShare:P0}");

        // ── What is played ────────────────────────────────────────────────────────
        sb.AppendLine();
        sb.AppendLine("── kinds of play (areas offering each) ──");
        var kinds = typeof(PlayActivity).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(PlayActivity).IsAssignableFrom(t))
            .OrderBy(t => t.Name, StringComparer.Ordinal);
        foreach (var t in kinds)
        {
            int n = byActivity.GetValueOrDefault(t);
            sb.AppendLine($"  {(n > 0 ? "✓" : "✗")} {t.Name,-22} {n,6}");
            if (n == 0) failures.Add($"{t.Name} is a kind of play no sampled scene offers — dead content");
        }

        sb.AppendLine();
        sb.AppendLine("── what carries them ──");
        foreach (var (holder, n) in byHolder.OrderByDescending(p => p.Value))
            sb.AppendLine($"     {n,6}  {holder}");

        sb.AppendLine();
        sb.AppendLine("── how each is worded (the first of each carrier and name) ──");
        foreach (var (key, text) in wording.OrderBy(p => p.Key, StringComparer.Ordinal))
            sb.AppendLine($"     {key,-48} {text}");

        sb.AppendLine();
        sb.AppendLine("── lessons taught to a person (one per area offering) ──");
        foreach (var (id, n) in lessons.OrderByDescending(p => p.Value))
            sb.AppendLine($"     {n,6}  {id}");

        // ── Lessons: every play's list resolves, and some body can hold something on it ──
        AuditLessons(sb, failures);

        // ── Where, by kind of area ───────────────────────────────────────────────
        sb.AppendLine();
        sb.AppendLine("── by kind of area (every kind met, most common first) ──");
        foreach (var (kind, (areas, play)) in byKind.OrderByDescending(p => p.Value.Areas))
            sb.AppendLine($"     {kind.Name,-22} {areas,6} {play,6} {(double)play / areas,6:P0}");

        sb.AppendLine();
        if (failures.Count == 0) sb.AppendLine("No failures.");
        else
        {
            sb.AppendLine($"--- {failures.Count} FAILURE(S) ---");
            foreach (var f in failures) sb.AppendLine($"  ✗ {f}");
        }
        return sb.ToString();
    }

    /// <summary>Everything PLAY accepts in <paramref name="area"/> at any period.</summary>
    private static IEnumerable<Element> Offered(Scene scene, Area area, PlayVerb verb, PartyMember actor)
    {
        var candidates = area.PointsOfInterest.Cast<Element>()
            .Concat(scene.Npcs.Where(n => n.Play != null))
            .Where(e => e is IPlayable { Play: not null });

        foreach (var target in candidates)
            if (Enum.GetValues<TimePeriod>().Any(p => verb.IsPossible(scene, new PoV(area, p), target, actor)))
                yield return target;
    }

    /// <summary>
    /// A play's lessons are read from types, so a missing one is a build error — but a play whose every
    /// lesson needs a body its own capability rules out would still teach nothing but the fallback.
    /// </summary>
    private static void AuditLessons(StringBuilder sb, List<string> failures)
    {
        sb.AppendLine();
        sb.AppendLine("── each play's lessons (✓ learnable by a body that can be offered it) ──");
        var plays = PlayPlacement.Catalogue();
        foreach (var play in plays.DistinctBy(p => p.GetType()))
        {
            var ids = play.Lessons.Select(m => m.ModusMentisId).ToList();
            var bodies = new[] { AnatomyType.Human, AnatomyType.Beast }
                .Where(a => (AnatomyFactoryRegistry.GetFactory(a).Capabilities & play.RequiredCapabilities) == play.RequiredCapabilities)
                .ToList();
            var holdable = bodies.Where(a => play.Lessons.Any(m => ModusMentisAnatomy.IsLearnableBy(m, a))).ToList();
            bool ok = holdable.Count > 0;
            sb.AppendLine($"  {(ok ? "✓" : "✗")} {play.GetType().Name,-22} {string.Join(" > ", ids)}"
                          + $"   offered to {string.Join("/", bodies)}; holds a lesson: {string.Join("/", holdable)}");
            if (!ok) failures.Add($"{play.GetType().Name}: no body it is offered to can hold any of {string.Join(", ", ids)}");
            foreach (var id in ids.Where(id => ModusMentisRegistry.Instance.GetModusMentis(id) == null))
                failures.Add($"{play.GetType().Name}: lesson '{id}' does not resolve");
        }
    }
}
