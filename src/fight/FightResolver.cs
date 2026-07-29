using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Fight;

/// <summary>
/// Pure-static helpers for combat resolution: movement, attack, wound selection, runaway.
/// No state is stored here.
/// </summary>
public static class FightResolver
{
    /// <summary>
    /// Movement cost of stepping from (fromX,fromY) to (toX,toY). Cardinal = 1, diagonal = 1.5,
    /// further multiplied by 3 when entering Soft terrain so heavy ground really slows you down.
    /// Shared by Dijkstra, the reachable-set calculator and the click-time affordability check
    /// so a previewed path always matches what's actually paid.
    /// </summary>
    public static double MovementStepCost(FightArea area, int fromX, int fromY, int toX, int toY)
    {
        double basis = (fromX != toX && fromY != toY) ? 1.5 : 1.0;
        if (area.GetCell(toX, toY).Type == TerrainType.SoftObstacle) basis *= 3.0;
        return basis;
    }

    /// <summary>
    /// Probability (in percent, 0..100) that a fighter with the given equilibrium loses
    /// footing when entering a Treacherous or Dangerous cell. Returns 0 for any other
    /// terrain. Shared by the actual slip-roll in <c>CheckTerrainInterrupt</c> and by
    /// the AI planner that prefers safer routes.
    /// </summary>
    public static int EstimateSlipRiskPct(TerrainType terrain, int equilibrium)
    {
        int eq = System.Math.Max(1, equilibrium);
        return terrain switch
        {
            TerrainType.DangerousTerrain   => System.Math.Max(10, 80 - eq * 8),
            TerrainType.TreacherousTerrain => System.Math.Max(5,  50 - eq * 5),
            _                              => 0,
        };
    }

    // ── Line of sight ─────────────────────────────────────────────────

    /// <summary>
    /// Returns true if there is an unobstructed line of sight between (x0,y0) and (x1,y1).
    /// Uses Bresenham's line algorithm; only <see cref="TerrainType.HardObstacle"/> cells block sight.
    /// The start and end cells themselves are never treated as blocking.
    /// </summary>
    public static bool HasLineOfSight(FightArea area, int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int cx = x0, cy = y0;

        while (true)
        {
            // Reached destination — no blocker found
            if (cx == x1 && cy == y1) return true;

            // Check intermediate cells (not start, not end)
            if ((cx != x0 || cy != y0) && area.IsInBounds(cx, cy)
                && area.GetCell(cx, cy).Type == TerrainType.HardObstacle)
                return false;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; cx += sx; }
            if (e2 <  dx) { err += dx; cy += sy; }
        }
    }

    // ── Movement ─────────────────────────────────────────────────────

    /// <summary>Returns true if the cell is in bounds, not a hard obstacle, and not occupied.</summary>
    public static bool CanMoveTo(FightArea area, int tx, int ty, IEnumerable<Fighter> fighters, Fighter mover)
    {
        if (!area.IsInBounds(tx, ty)) return false;
        if (area.GetCell(tx, ty).Type == TerrainType.HardObstacle) return false;
        return !fighters.Any(f => f.IsAlive && f != mover && f.X == tx && f.Y == ty);
    }

    /// <summary>How many cinetic points a move of <paramref name="pathLength"/> cardinal steps costs.</summary>
    public static int MovementCineticCost(int pathLength, Fighter mover) =>
        (int)Math.Ceiling(pathLength / (double)Math.Max(1, mover.MoveSpeed));

    /// <summary>How many cinetic points a move whose total weighted cost is <paramref name="pathCost"/> costs.</summary>
    public static int MovementCineticCost(double pathCost, Fighter mover) =>
        (int)Math.Ceiling(pathCost / Math.Max(1, mover.MoveSpeed));

    /// <summary>
    /// Compute the total movement cost of a path (cardinal step = 1.0, diagonal step = 1.5).
    /// <paramref name="fromX"/>/<paramref name="fromY"/> is the position before the first step.
    /// </summary>
    public static double PathCost(int fromX, int fromY, IList<(int X, int Y)> path)
    {
        double cost = 0;
        int px = fromX, py = fromY;
        foreach (var (x, y) in path)
        {
            cost += (x != px && y != py) ? 1.5 : 1.0;
            px = x; py = y;
        }
        return cost;
    }

    /// <summary>BFS Manhattan distance, returns <c>int.MaxValue</c> if unreachable.</summary>
    public static int BfsDistance(FightArea area, int sx, int sy, int tx, int ty,
                                   IList<Fighter> fighters, Fighter mover)
    {
        var path = BfsPath(area, sx, sy, tx, ty, fighters, mover);
        return path == null ? int.MaxValue : path.Count;
    }

    /// <summary>
    /// Dijkstra shortest-cost path from (sx,sy) to (tx,ty).
    /// Cardinal steps cost 1.0, diagonal steps cost 1.5.
    /// Returns the list of steps (excluding start), or <c>null</c> if unreachable.
    /// </summary>
    public static List<(int X, int Y)>? BfsPath(FightArea area, int sx, int sy, int tx, int ty,
                                                  IList<Fighter> fighters, Fighter mover)
    {
        if (sx == tx && sy == ty) return new List<(int, int)>();

        var dist = new Dictionary<(int, int), double>();
        var prev = new Dictionary<(int, int), (int, int)>();
        var pq   = new PriorityQueue<(int X, int Y), double>();
        var start = (sx, sy);

        dist[start] = 0;
        pq.Enqueue(start, 0);

        static IEnumerable<(int, int)> Neighbors(int x, int y)
        {
            yield return (x - 1, y);
            yield return (x + 1, y);
            yield return (x, y - 1);
            yield return (x, y + 1);
            yield return (x - 1, y - 1);
            yield return (x + 1, y - 1);
            yield return (x - 1, y + 1);
            yield return (x + 1, y + 1);
        }

        while (pq.Count > 0)
        {
            pq.TryDequeue(out var cur, out var curCost);
            var (cx, cy) = cur;

            // When we dequeue the destination it is optimal (Dijkstra guarantee)
            if (cx == tx && cy == ty)
            {
                var path = new List<(int, int)>();
                var c = cur;
                while (c != start)
                {
                    path.Add(c);
                    c = prev[c];
                }
                path.Reverse();
                return path;
            }

            if (curCost > dist.GetValueOrDefault(cur, double.MaxValue)) continue; // stale entry

            foreach (var (nx, ny) in Neighbors(cx, cy))
            {
                if (!area.IsInBounds(nx, ny)) continue;
                if (area.GetCell(nx, ny).Type == TerrainType.HardObstacle) continue;
                bool isDestination = nx == tx && ny == ty;
                if (!isDestination && !CanMoveTo(area, nx, ny, fighters, mover)) continue;

                double stepCost = MovementStepCost(area, cx, cy, nx, ny);
                double newCost  = curCost + stepCost;
                var neighbor    = (nx, ny);

                if (newCost < dist.GetValueOrDefault(neighbor, double.MaxValue))
                {
                    dist[neighbor] = newCost;
                    prev[neighbor] = cur;
                    pq.Enqueue(neighbor, newCost);
                }
            }
        }

        return null; // Unreachable
    }

    // ── Attack resolution ─────────────────────────────────────────────

    public record AttackResult(int SixesCount, int NaturalDefense, int DefenseSixes, bool IsHit, Wound? Wound);

    /// <summary>
    /// Count 6s in <paramref name="diceValues"/> (attack) and in <paramref name="defenseDiceValues"/>
    /// (defender's defense roll); attacker wins if attack sixes &gt; defense sixes. If hit, select a
    /// wound and apply special effects from the skill. Also consumes the attacker's vital heat cost.
    /// </summary>
    public static AttackResult ResolveAttack(
        Fighter attacker, Fighter defender, FightingSkill skill,
        int[] diceValues, string? playerChosenBodyPartId, Random rng,
        FightState? state = null,
        int[]? defenseDiceValues = null)
    {
        int sixes        = diceValues.Count(v => v == 6);
        int defenseSixes = defenseDiceValues?.Count(v => v == 6) ?? 0;
        int def          = defender.NaturalDefense;
        bool isHit       = sixes > defenseSixes;

        Wound? wound = null;
        if (isHit)
        {
            // Award +1 XP to each MM contributing to this skill (player attacks only).
            if (attacker.IsPlayerControlled)
                foreach (var mm in skill.GetContributingModiMentis(attacker))
                    attacker.Member.AwardModusMentisXp(mm);

            wound = PickWound(defender, skill, playerChosenBodyPartId, rng);

            // Damage resistance check: defender's damage_resistance stat
            // If defender rolls at least 1 success (1d6 per resistance point >= 4),
            // downgrade wound severity: High→Medium, Medium→Low
            int resistance = GetFighterCombatStat(defender, "damage_resistance");
            if (resistance > 0 && wound != null)
            {
                int resistSixes = 0;
                for (int i = 0; i < resistance; i++)
                    if (rng.Next(1, 7) >= 4) resistSixes++;

                if (resistSixes > 0)
                {
                    var downgraded = DowngradeWound(defender, wound, skill, playerChosenBodyPartId, rng);
                    if (downgraded != null) wound = downgraded;
                }
            }

            // Apply special effects from the skill to the target
            if (state != null)
            {
                foreach (var effect in skill.SpecialEffects)
                {
                    // Bleeding stacks — bump the existing instance's level instead of adding a parallel one.
                    if (effect is BleedingEffect newBleed)
                    {
                        var existing = defender.ActiveEffects.OfType<BleedingEffect>().FirstOrDefault();
                        if (existing != null)
                        {
                            existing.AddLevel(newBleed.Level);
                            state.AddLog($"{defender.DisplayName}'s bleeding intensifies (level {existing.Level}).",
                                LogEntryType.SpecialEffect);
                            continue;
                        }
                    }
                    var newEffect = effect;
                    defender.ActiveEffects.Add(newEffect);
                    newEffect.OnApply(defender, attacker, state, rng);
                    if (newEffect.IsExpired)
                        defender.ActiveEffects.Remove(newEffect);
                }
            }
        }

        // Consume attacker's vital heat cost
        if (skill.VitalHeatCost > 0)
        {
            for (int i = 0; i < skill.VitalHeatCost; i++)
                attacker.Member.HumorQueues.ConsumeVitalHeatCycled(attacker.Member, rng);
        }

        return new AttackResult(sixes, def, defenseSixes, isHit, wound);
    }

    private static int GetFighterCombatStat(Fighter f, string statName)
    {
        var stat = f.Member.DerivedStats.FirstOrDefault(s => s.Name == statName);
        return stat?.GetValue(f.Member) ?? 0;
    }

    /// <summary>
    /// Attempt to downgrade a wound one severity level.
    /// High → Medium (pick a medium wound from same body part),
    /// Medium → Low (pick a wildcard wound).
    /// Returns null if no downgrade is possible.
    /// </summary>
    private static Wound? DowngradeWound(Fighter defender, Wound current, FightingSkill skill,
                                          string? playerChosenBodyPartId, Random rng)
    {
        if (current.Handicap == WoundHandicap.Low) return null;

        if (current.Handicap == WoundHandicap.High)
        {
            // Downgrade to Medium: find a medium wound for the same body part
            var mediumWounds = WoundRegistry.All.Values
                .Where(w => w.Handicap == WoundHandicap.Medium && w.AffectsBodyPart(current.TargetId))
                .ToList();
            return mediumWounds.Count > 0 ? mediumWounds[rng.Next(mediumWounds.Count)] : null;
        }

        // Medium → Low: pick any wildcard
        var wildcards = WoundRegistry.WildcardTemplates.Cast<Wound>().ToList();
        return wildcards.Count > 0 ? wildcards[rng.Next(wildcards.Count)] : null;
    }

    // ── Wound selection ───────────────────────────────────────────────

    /// <summary>
    /// Pick an appropriate wound for the defender based on the skill's targeting mode.
    /// Returns null if no valid wound pool is available.
    /// </summary>
    public static Wound? PickWound(Fighter defender, FightingSkill skill,
                                    string? playerChosenBodyPartId, Random rng)
    {
        var wounds = GetWoundPool(defender, skill, playerChosenBodyPartId, rng);
        if (wounds.Count == 0) return null;
        return wounds[rng.Next(wounds.Count)];
    }

    /// <summary>
    /// Every wound that can meaningfully be inflicted on this defender — the flat pool a Random
    /// attack used to draw from directly. Extracted so <see cref="PreRollHitLocation"/> can bucket
    /// the same pool it will later be filtered against.
    /// </summary>
    public static List<Wound> BuildAnatomyWoundPool(Fighter defender)
    {
        var validBodyPartIds  = defender.Member.BodyParts.Select(bp => bp.Id).ToHashSet();
        var allOrgans         = defender.Member.BodyParts.SelectMany(bp => bp.Organs).ToList();
        var validOrganIds     = allOrgans.Select(o => o.Id).ToHashSet();
        var validOrganPartIds = allOrgans.SelectMany(o => o.Parts).Select(p => p.Id).ToHashSet();

        var anatomyWounds = WoundRegistry.All.Values
            .Where(w => w.TargetKind == WoundTargetKind.Wildcard
                     || (w.TargetKind == WoundTargetKind.BodyPart  && validBodyPartIds.Contains(w.TargetId))
                     || (w.TargetKind == WoundTargetKind.Organ     && validOrganIds.Contains(w.TargetId))
                     || (w.TargetKind == WoundTargetKind.OrganPart && validOrganPartIds.Contains(w.TargetId)))
            .ToList();

        return anatomyWounds.Count > 0 ? anatomyWounds : WoundRegistry.All.Values.ToList();
    }

    /// <summary>
    /// Decides where a Random blow is aimed <em>before</em> the dice are thrown, so the defender's
    /// armour for that section can be counted into the defence pool. Returns null for the wildcard
    /// bucket — a graze that belongs to no section and that no garment turns.
    ///
    /// The weighting is bucket-proportional, and that is the whole point: each body part is chosen
    /// with probability equal to its share of the flat wound pool. Because a body-part filter
    /// returns exactly that part's wounds, picking a bucket and then drawing uniformly inside it
    /// reproduces a uniform draw from the flat pool <em>exactly</em>. Random targeting therefore
    /// wounds precisely as it did before this change; only the armour lookup is new. Picking
    /// uniformly across the five body parts instead would have quintupled the headshot rate.
    /// </summary>
    public static string? PreRollHitLocation(Fighter defender, Random rng)
    {
        var pool = BuildAnatomyWoundPool(defender);
        if (pool.Count == 0) return null;

        int roll = rng.Next(pool.Count);
        int acc  = 0;

        foreach (var bodyPart in defender.Member.BodyParts)
        {
            acc += FilterByTargetSingle(pool, bodyPart.Id, defender).Count;
            if (roll < acc) return bodyPart.Id;
        }

        return null;   // wildcard remainder
    }

    /// <summary>
    /// The top-level body part a localisation falls inside, which is the section whose armour
    /// applies. Handles the overlay's <c>"organ_part_id,body_part_id"</c> pair (the last token is
    /// the body part) as well as a bare body-part, organ, or organ-part id.
    /// </summary>
    public static string? ResolveSectionBodyPartId(Fighter defender, string? localization)
    {
        if (string.IsNullOrWhiteSpace(localization)) return null;

        var ids = localization.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => s.Trim())
                              .Where(s => s.Length > 0)
                              .ToList();
        if (ids.Count == 0) return null;

        // The overlay puts the enclosing body part last; a bare id is the only token there is.
        string id = ids[^1];

        if (defender.Member.BodyParts.Any(bp => bp.Id == id)) return id;

        // An organ id: its parent body part is the section.
        var organParent = defender.Member.BodyParts.FirstOrDefault(b => b.Organs.Any(o => o.Id == id));
        if (organParent != null) return organParent.Id;

        // An organ-part id: walk up two levels.
        foreach (var bp in defender.Member.BodyParts)
            foreach (var org in bp.Organs)
                if (org.Parts.Any(p => p.Id == id))
                    return bp.Id;

        return null;
    }

    /// <summary>
    /// The wounds a blow aimed at <paramref name="resolvedLocationId"/> can inflict.
    ///
    /// The targeting mode no longer matters here: the location is decided before the roll for every
    /// mode, so this simply filters. When the location has no wounds authored for it the pool falls
    /// back to <em>wildcards</em> rather than the whole anatomy — falling back to everything would
    /// let a blow that was charged trunk armour come down on the head instead.
    /// </summary>
    private static List<Wound> GetWoundPool(Fighter defender, FightingSkill skill,
                                             string? resolvedLocationId, Random rng)
    {
        var anatomyWounds = BuildAnatomyWoundPool(defender);
        if (resolvedLocationId is null) return Wildcards(anatomyWounds);

        var filtered = FilterByTarget(anatomyWounds, resolvedLocationId, defender);
        return filtered.Count > 0 ? filtered : Wildcards(anatomyWounds);
    }

    /// <summary>Generic wounds that belong to no particular place, used as the honest fallback.</summary>
    private static List<Wound> Wildcards(List<Wound> pool)
    {
        var wildcards = pool.Where(w => w.TargetKind == WoundTargetKind.Wildcard).ToList();
        return wildcards.Count > 0 ? wildcards : pool;
    }

    /// <summary>
    /// Filter the wound pool by one or more target IDs.
    /// <paramref name="targetIds"/> may be a single id ("trunk") or a comma-separated list
    /// ("left_eye,visage") combining an organ-part and its enclosing body part. Each id is
    /// resolved against the defender's anatomy as a body-part, organ, or organ-part id;
    /// matching wounds are unioned into the result pool.
    /// </summary>
    private static List<Wound> FilterByTarget(List<Wound> wounds, string targetIds, Fighter defender)
    {
        var ids = targetIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(s => s.Trim())
                           .Where(s => s.Length > 0)
                           .ToList();
        if (ids.Count == 0) return new List<Wound>();

        var seen = new HashSet<Wound>();
        var combined = new List<Wound>();
        foreach (var id in ids)
            foreach (var w in FilterByTargetSingle(wounds, id, defender))
                if (seen.Add(w)) combined.Add(w);
        return combined;
    }

    /// <summary>
    /// Find wounds in <paramref name="wounds"/> that match the single anatomy id
    /// <paramref name="targetId"/> — resolved as body-part, then organ, then organ-part.
    ///
    /// A body part gathers everything <em>inside</em> it: its own wounds, its organs' wounds, and
    /// its organs' parts' wounds. That last tier matters twice over — aiming at the visage should
    /// be able to take an eye, and, because <see cref="PreRollHitLocation"/> weights each body part
    /// by the size of this very set, omitting organ-part wounds would make them unreachable from an
    /// unaimed attack altogether.
    /// </summary>
    private static List<Wound> FilterByTargetSingle(List<Wound> wounds, string targetId, Fighter defender)
    {
        var bodyPart = defender.Member.GetBodyPartById(targetId);
        if (bodyPart != null)
        {
            var result = wounds.Where(w => w.AffectsBodyPart(targetId)).ToList();

            foreach (var organ in bodyPart.Organs)
            {
                foreach (var w in wounds.Where(w => w.AffectsOrgan(organ.Id, targetId)))
                    if (!result.Contains(w)) result.Add(w);

                foreach (var part in organ.Parts)
                    foreach (var w in wounds.Where(w => w.AffectsOrganPart(part.Id, organ.Id, targetId)))
                        if (!result.Contains(w)) result.Add(w);
            }
            return result;
        }

        // organ: organ-level wounds for the organ in its parent body part
        var organParent = defender.Member.BodyParts.FirstOrDefault(b => b.Organs.Any(o => o.Id == targetId));
        if (organParent != null)
            return wounds.Where(w => w.AffectsOrgan(targetId, organParent.Id)).ToList();

        // organ part: organ-part-level wounds for the specific part
        foreach (var bp in defender.Member.BodyParts)
            foreach (var org in bp.Organs)
                foreach (var part in org.Parts)
                    if (part.Id == targetId)
                        return wounds.Where(w => w.AffectsOrganPart(part.Id, org.Id, bp.Id)).ToList();

        return new List<Wound>();
    }

    // -- Wound application --

    public static void ApplyWound(Fighter target, Wound wound) =>
        target.Member.Wounds.Add(wound);

    // -- Skill learning --

    public record LearningResult(bool Success, int[] DiceValues, int Difficulty, int SixesCount);

    /// <summary>
    /// Roll to learn an unknown fighting skill in combat.
    /// Difficulty is the 1-based index of the skill in its medium skill list.
    /// Succeeds if sixes strictly exceed difficulty.
    /// </summary>
    public static LearningResult AttemptSkillLearning(Fighter fighter, int difficulty, int[] diceValues)
    {
        int sixes = diceValues.Count(v => v == 6);
        bool success = sixes > difficulty;
        return new LearningResult(success, diceValues, difficulty, sixes);
    }
}
