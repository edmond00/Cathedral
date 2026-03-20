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

                double stepCost = (nx != cx && ny != cy) ? 1.5 : 1.0;
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

    // ── Runaway ───────────────────────────────────────────────────────

    /// <summary>Roll runaway chance. Returns true on success.</summary>
    public static bool AttemptRunaway(Fighter fighter, Random rng) =>
        rng.Next(100) < fighter.RunawayChancePercent;

    // ── Attack resolution ─────────────────────────────────────────────

    public record AttackResult(int SixesCount, int NaturalDefense, bool IsHit, Wound? Wound);

    /// <summary>
    /// Count 6s in <paramref name="diceValues"/>; compare to <paramref name="defender"/>
    /// natural defense (strictly greater-than wins). If hit, select a wound.
    /// </summary>
    public static AttackResult ResolveAttack(
        Fighter attacker, Fighter defender, FightingSkill skill,
        int[] diceValues, string? playerChosenBodyPartId, Random rng)
    {
        int sixes  = diceValues.Count(v => v == 6);
        int def    = defender.NaturalDefense;
        bool isHit = sixes > def;
        Wound? wound = isHit
            ? PickWound(defender, skill, playerChosenBodyPartId, rng)
            : null;
        return new AttackResult(sixes, def, isHit, wound);
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

    private static List<Wound> GetWoundPool(Fighter defender, FightingSkill skill,
                                             string? playerChosenBodyPartId, Random rng)
    {
        var allWounds = WoundRegistry.All.Values.ToList();

        switch (skill.WoundTargetMode)
        {
            case WoundTargetMode.Random:
                return allWounds;

            case WoundTargetMode.FixedBodyPart:
                if (skill.TargetBodyPartId is null) return allWounds;
                {
                    var filtered = allWounds
                        .Where(w => w.AffectsBodyPart(skill.TargetBodyPartId))
                        .ToList();
                    return filtered.Count > 0 ? filtered : allWounds;
                }

            case WoundTargetMode.PlayerChooses:
                if (playerChosenBodyPartId is null) return allWounds;
                {
                    var filtered = allWounds
                        .Where(w => w.AffectsBodyPart(playerChosenBodyPartId))
                        .ToList();
                    return filtered.Count > 0 ? filtered : allWounds;
                }

            default:
                return allWounds;
        }
    }

    // ── Wound application ─────────────────────────────────────────────

    public static void ApplyWound(Fighter target, Wound wound) =>
        target.Member.Wounds.Add(wound);
}
