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

    // ── Range ────────────────────────────────────────────────────────

    /// <summary>
    /// Whether a skill of this range can reach a cell <paramref name="dx"/>,<paramref name="dy"/>
    /// away. The single definition of "in range" — the player's target highlighting and the AI's
    /// candidate filter both go through here, and used to each compute it themselves.
    ///
    /// <para>
    /// Range is Euclidean, which keeps a bow's reach circular, <em>plus</em> the eight surrounding
    /// cells always count as adjacent. That exception is not cosmetic: movement is 8-connected, so
    /// fighters constantly end up diagonally neighbouring — at Euclidean distance 1.41, which a
    /// melee range of 1 rejected. Two fighters could stand side by side and neither could swing.
    /// It is the real cause of enemies that walk up to someone and then pass their turn.
    /// </para>
    ///
    /// <para>A minimum range (a bow that cannot fire point-blank) still overrides the exception.</para>
    /// </summary>
    public static bool IsInSkillRange(int dx, int dy, FightingSkill skill)
    {
        if (dx == 0 && dy == 0) return false;         // never target your own cell

        int minR = Math.Max(1, skill.MinRange);
        int distSq = dx * dx + dy * dy;

        // Diagonal adjacency, unless the skill refuses point-blank.
        if (minR <= 1 && Math.Abs(dx) <= 1 && Math.Abs(dy) <= 1) return true;

        return distSq <= skill.Range * skill.Range && distSq >= minR * minR;
    }

    /// <inheritdoc cref="IsInSkillRange(int,int,FightingSkill)"/>
    public static bool IsInSkillRange(Fighter attacker, Fighter target, FightingSkill skill)
        => IsInSkillRange(target.X - attacker.X, target.Y - attacker.Y, skill);

    /// <summary>Grid steps between two fighters, counting a diagonal as one — the movement metric.</summary>
    public static int ChebyshevDistance(Fighter a, Fighter b)
        => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    /// <summary>
    /// Where a charging attacker ends up: the cell beside <paramref name="target"/> reached by the
    /// shortest route, provided that route is no longer than <paramref name="maxSteps"/>.
    /// Null when there is no clear run — the charge then fails rather than teleporting anyone.
    /// </summary>
    public static (int X, int Y)? ChargeLandingCell(Fighter attacker, Fighter target,
                                                    FightState state, int maxSteps)
    {
        (int X, int Y)? best = null;
        int bestLen = int.MaxValue;

        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = target.X + dx, ny = target.Y + dy;
            if (nx == attacker.X && ny == attacker.Y) return (attacker.X, attacker.Y); // already there
            if (!CanMoveTo(state.Area, nx, ny, state.Fighters, attacker)) continue;

            var path = BfsPath(state.Area, attacker.X, attacker.Y, nx, ny, state.Fighters, attacker);
            if (path == null || path.Count == 0 || path.Count > maxSteps) continue;
            if (path.Count < bestLen) { bestLen = path.Count; best = (nx, ny); }
        }
        return best;
    }

    // ── Movement ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the cell is in bounds, passable for <paramref name="mover"/>, and unoccupied.
    /// Hard obstacles block everyone except a mover carrying an effect that clears them (Jump).
    /// </summary>
    public static bool CanMoveTo(FightArea area, int tx, int ty, IEnumerable<Fighter> fighters, Fighter mover)
    {
        if (!area.IsInBounds(tx, ty)) return false;
        if (area.GetCell(tx, ty).Type == TerrainType.HardObstacle && !mover.CanCrossHardObstacles)
            return false;
        return !fighters.Any(f => f.IsAlive && f != mover && f.X == tx && f.Y == ty);
    }

    /// <summary>How many cinetic points a move of <paramref name="pathLength"/> cardinal steps costs.</summary>
    public static int MovementCineticCost(int pathLength, Fighter mover) =>
        (int)Math.Ceiling(pathLength / (double)mover.EffectiveMoveSpeed);

    /// <summary>How many cinetic points a move whose total weighted cost is <paramref name="pathCost"/> costs.</summary>
    public static int MovementCineticCost(double pathCost, Fighter mover) =>
        (int)Math.Ceiling(pathCost / (double)mover.EffectiveMoveSpeed);

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
                if (area.GetCell(nx, ny).Type == TerrainType.HardObstacle && !mover.CanCrossHardObstacles)
                    continue;
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

    /// <param name="AttackerTurnEnded">
    /// Set when the defender turned the blow aside AND holds an effect that breaks the attacker off
    /// (Cold Blood). Surfaced as a flag rather than acted on here: this resolver is static and must
    /// not drive turn order — <c>FinishAttackResolution</c> is what ends the turn.
    /// </param>
    public record AttackResult(int SixesCount, int NaturalDefense, int DefenseSixes, bool IsHit,
                               Wound? Wound, bool AttackerTurnEnded = false);

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

            // A landed blow wounds. There is no post-hoc mitigation roll any more: damage
            // resistance downgraded severity invisibly, after the fact, which the player never saw
            // and could not plan around. Resilience is now measured over the long run instead —
            // see the wound_healing stat.
            wound = PickWound(attacker, defender, skill, playerChosenBodyPartId, rng);

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

        // ── Effect events ─────────────────────────────────────────────────────────
        // Both sides get told how it went, on copies of the lists — a handler may add or expire an
        // effect (Feint spends itself here), which would otherwise mutate the list being walked.
        bool attackerTurnEnded = false;
        if (state != null)
        {
            foreach (var e in attacker.ActiveEffects.ToList())
                e.OnAttackResolved(attacker, defender, isHit, state, rng);

            bool defenseSucceeded = !isHit;

            // Turning a melee blow aside is the opening a riposte needs.
            if (defenseSucceeded && skill.Range <= 1)
                defender.HasDefendedMeleeSinceOwnTurn = true;

            foreach (var e in defender.ActiveEffects.ToList())
            {
                e.OnDefended(defender, attacker, defenseSucceeded, state, rng);
                if (defenseSucceeded && e.EndsAttackerTurnOnSuccessfulDefense)
                    attackerTurnEnded = true;
            }

            for (int i = attacker.ActiveEffects.Count - 1; i >= 0; i--)
                if (attacker.ActiveEffects[i].IsExpired) attacker.ActiveEffects.RemoveAt(i);
            for (int i = defender.ActiveEffects.Count - 1; i >= 0; i--)
                if (defender.ActiveEffects[i].IsExpired) defender.ActiveEffects.RemoveAt(i);
        }

        return new AttackResult(sixes, def, defenseSixes, isHit, wound, attackerTurnEnded);
    }

    // ── Wound selection ───────────────────────────────────────────────

    /// <summary>
    /// Pick a wound for the defender from the pool the resolved hit location allows.
    ///
    /// <para>
    /// A plain uniform draw, unless the <paramref name="attacker"/> carries an effect that strikes
    /// to ruin (Blood Lust), in which case the worst wound the blow could have caused is the one it
    /// causes. Note the effect is queried on the attacker, not the defender: it is the attacker's
    /// ferocity that decides how the blow lands.
    /// </para>
    ///
    /// Returns null if no valid wound pool is available.
    /// </summary>
    public static Wound? PickWound(Fighter? attacker, Fighter defender, FightingSkill skill,
                                    string? playerChosenBodyPartId, Random rng)
    {
        var wounds = GetWoundPool(defender, skill, playerChosenBodyPartId, rng);
        if (wounds.Count == 0) return null;

        if (attacker != null && attacker.ActiveEffects.Any(e => e.ForcesHighestSeverityWound))
        {
            var worst = wounds.Max(w => w.Handicap);
            var severe = wounds.Where(w => w.Handicap == worst).ToList();
            return severe[rng.Next(severe.Count)];
        }

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

        // The defender's OWN catalogue, not the human one — a wolf's harm is broken forelegs and
        // torn-off fangs, none of which exist in the human list.
        var catalogue = WoundRegistry.ForAnatomy(defender.Member).ToList();

        var anatomyWounds = catalogue
            .Where(w => w.TargetKind == WoundTargetKind.Wildcard
                     || (w.TargetKind == WoundTargetKind.BodyPart  && validBodyPartIds.Contains(w.TargetId))
                     || (w.TargetKind == WoundTargetKind.Organ     && validOrganIds.Contains(w.TargetId))
                     || (w.TargetKind == WoundTargetKind.OrganPart && validOrganPartIds.Contains(w.TargetId)))
            .ToList();

        return anatomyWounds.Count > 0 ? anatomyWounds : catalogue;
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
    /// Draws ONE location from a skill's authored list — "trunk,upper_limbs" for a blow that takes
    /// the body or an arm — weighted the same bucket-proportional way as
    /// <see cref="PreRollHitLocation"/>, so a large region is likelier than a small organ.
    ///
    /// <para>
    /// Drawing before the roll rather than filtering after it is what lets armour work: the defence
    /// pool is sized from a single resolved section, so a skill that could land in two places has to
    /// pick which one before anyone rolls anything.
    /// </para>
    ///
    /// <para>
    /// Ids the defender does not have are skipped, so a list may name both the human and the beast
    /// form of a location. Returns null when none of them exist on this body — the caller then falls
    /// back to a wildcard graze.
    /// </para>
    /// </summary>
    public static string? PreRollAmong(Fighter defender, string targetIds, Random rng)
    {
        var pool = BuildAnatomyWoundPool(defender);
        if (pool.Count == 0) return null;

        var candidates = targetIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(s => s.Trim())
                                  .Where(s => s.Length > 0)
                                  .Select(id => (Id: id, Weight: FilterByTargetSingle(pool, id, defender).Count))
                                  .Where(c => c.Weight > 0)
                                  .ToList();
        if (candidates.Count == 0) return null;

        int total = candidates.Sum(c => c.Weight);
        int roll  = rng.Next(total);
        int acc   = 0;
        foreach (var c in candidates)
        {
            acc += c.Weight;
            if (roll < acc) return c.Id;
        }
        return candidates[^1].Id;
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
        var generic = Wildcards(anatomyWounds, skill.DamageTypes);

        if (resolvedLocationId is null) return generic;

        var filtered = FilterByTarget(anatomyWounds, resolvedLocationId, defender);
        if (filtered.Count == 0) return generic;

        // A landed blow can be a named injury of that location OR a generic one of the weapon's
        // kind — a sword to the arm breaks it or merely opens a Cut. Without this every hit would
        // be a maiming: once every attack has an authored localisation, the location filter
        // excludes wildcards entirely, and Cut / Puncture / Contusion drop out of combat for good.
        var pool = new List<Wound>(filtered);
        foreach (var w in generic)
            if (w.TargetKind == WoundTargetKind.Wildcard && !pool.Contains(w))
                pool.Add(w);
        return pool;
    }

    /// <summary>
    /// Generic wounds that belong to no particular place, used as the honest fallback.
    ///
    /// <para>
    /// Narrowed to the ones matching <paramref name="damageTypes"/>, so the graze a weapon leaves
    /// looks like the weapon: a blade Cuts, a spear Punctures, a mace leaves a Contusion. A skill
    /// that declares no damage type (or whose type has no matching wildcard on this anatomy) draws
    /// from all of them, which is what everything did before types existed.
    /// </para>
    /// </summary>
    private static List<Wound> Wildcards(List<Wound> pool, DamageType damageTypes = DamageType.None)
    {
        var wildcards = pool.Where(w => w.TargetKind == WoundTargetKind.Wildcard).ToList();
        if (wildcards.Count == 0) return pool;

        if (damageTypes != DamageType.None)
        {
            var typed = wildcards
                .OfType<WildcardWound>()
                .Where(w => (w.DamageType & damageTypes) != 0)
                .Cast<Wound>()
                .ToList();
            if (typed.Count > 0) return typed;
        }

        return wildcards;
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
    /// How many wounds in <paramref name="pool"/> a single anatomy id can produce. Exposed for
    /// <c>--item-audit</c>, which uses it to prove every authored localisation reaches something.
    /// </summary>
    public static int CountWoundsFor(List<Wound> pool, string targetId, Fighter defender)
        => FilterByTargetSingle(pool, targetId, defender).Count;

    /// <summary>
    /// The generic wounds a blow of <paramref name="damageTypes"/> can leave. Exposed for
    /// <c>--item-audit</c>, which uses it to show that each weapon grazes in its own way.
    /// </summary>
    public static IEnumerable<Wound> GrazeWoundsFor(List<Wound> pool, DamageType damageTypes)
        => Wildcards(pool, damageTypes).Where(w => w.TargetKind == WoundTargetKind.Wildcard);

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

        // An organ gathers what is inside it too — its own wounds and its parts'. Containment
        // cascades at every tier, not just from body parts: aiming at the legs has to be able to
        // break a knee, since that is where the leg wounds are actually authored (left_leg,
        // right_leg). Without this, targeting an organ whose harm lives one level down — legs,
        // feet, arms — found nothing and quietly degraded to a graze.
        var organParent = defender.Member.BodyParts.FirstOrDefault(b => b.Organs.Any(o => o.Id == targetId));
        if (organParent != null)
        {
            var organ  = organParent.Organs.First(o => o.Id == targetId);
            var result = wounds.Where(w => w.AffectsOrgan(targetId, organParent.Id)).ToList();
            foreach (var part in organ.Parts)
                foreach (var w in wounds.Where(w => w.AffectsOrganPart(part.Id, organ.Id, organParent.Id)))
                    if (!result.Contains(w)) result.Add(w);
            return result;
        }

        // organ part: organ-part-level wounds for the specific part
        foreach (var bp in defender.Member.BodyParts)
            foreach (var org in bp.Organs)
                foreach (var part in org.Parts)
                    if (part.Id == targetId)
                        return wounds.Where(w => w.AffectsOrganPart(part.Id, org.Id, bp.Id)).ToList();

        return new List<Wound>();
    }

    // -- Wound application --

    /// <summary>
    /// Record <paramref name="wound"/> on <paramref name="target"/>.
    /// The template is wrapped in a fresh <see cref="WoundInstance"/> stamped with the current day,
    /// which is both what makes it heal later and what keeps <see cref="WoundRegistry"/>'s shared
    /// template objects from being handed out to two different bodies at once.
    /// </summary>
    public static void ApplyWound(Fighter target, Wound wound) =>
        target.Member.Wounds.Add(WoundInstance.Inflicted(wound));

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
