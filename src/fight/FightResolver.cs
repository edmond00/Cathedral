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

    /// <summary>How many cinetic points a move of <paramref name="pathLength"/> steps costs.</summary>
    public static int MovementCineticCost(int pathLength, Fighter mover) =>
        (int)Math.Ceiling(pathLength / (double)Math.Max(1, mover.MoveSpeed));

    /// <summary>BFS Manhattan distance, returns <c>int.MaxValue</c> if unreachable.</summary>
    public static int BfsDistance(FightArea area, int sx, int sy, int tx, int ty,
                                   IList<Fighter> fighters, Fighter mover)
    {
        var path = BfsPath(area, sx, sy, tx, ty, fighters, mover);
        return path == null ? int.MaxValue : path.Count;
    }

    /// <summary>
    /// BFS shortest path from (sx,sy) to (tx,ty). Returns the list of steps (excluding start),
    /// or <c>null</c> if unreachable.
    /// </summary>
    public static List<(int X, int Y)>? BfsPath(FightArea area, int sx, int sy, int tx, int ty,
                                                  IList<Fighter> fighters, Fighter mover)
    {
        if (sx == tx && sy == ty) return new List<(int, int)>();

        var visited = new HashSet<(int, int)>();
        var prev    = new Dictionary<(int, int), (int, int)>();
        var queue   = new Queue<(int X, int Y)>();
        var start   = (sx, sy);

        visited.Add(start);
        queue.Enqueue(start);

        static IEnumerable<(int,int)> Neighbors(int x, int y)
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

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            foreach (var (nx, ny) in Neighbors(cx, cy))
            {
                var cell = (nx, ny);
                if (visited.Contains(cell)) continue;
                // Allow entry into destination even if it contains a fighter (for adjacency attack pathing)
                bool isDestination = nx == tx && ny == ty;
                if (!isDestination && !CanMoveTo(area, nx, ny, fighters, mover)) continue;
                if (!area.IsInBounds(nx, ny)) continue;
                if (area.GetCell(nx, ny).Type == TerrainType.HardObstacle) continue;

                visited.Add(cell);
                prev[cell] = (cx, cy);

                if (isDestination)
                {
                    // Reconstruct path
                    var path = new List<(int, int)>();
                    var cur = cell;
                    while (cur != start)
                    {
                        path.Add(cur);
                        cur = prev[cur];
                    }
                    path.Reverse();
                    return path;
                }

                queue.Enqueue(cell);
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
