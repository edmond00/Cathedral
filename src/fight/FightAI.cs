using System;
using System.Linq;
using Cathedral.Fight.Actions;

namespace Cathedral.Fight;

/// <summary>
/// Simple rule-based AI that selects a fight action for an enemy fighter.
/// Decides entirely synchronously — no LLM, no async.
/// Priority order: attack if adjacent → move toward nearest party fighter → end turn.
/// </summary>
public static class FightAI
{
    public static IFightAction DecideAction(
        Fighter ai,
        FightState state,
        FightingSkillRegistry registry,
        Random rng)
    {
        var partyFighters = state.Fighters
            .Where(f => f.Faction == FighterFaction.Party && f.IsAlive)
            .OrderBy(f => ManhattanDistance(ai, f))
            .ToList();

        if (partyFighters.Count == 0)
            return new EndTurnAction(ai);

        // ── 1. Try to attack an adjacent party fighter ─────────────────
        var adjacentEnemy = partyFighters.FirstOrDefault(f => IsAdjacent(ai, f));
        if (adjacentEnemy != null)
        {
            var attackSkills = registry.GetAttackSkills()
                .Where(s => s.IsUnlocked(ai) && ai.CurrentCineticPoints >= s.CineticPointsCost)
                .ToList();

            if (attackSkills.Count > 0)
            {
                var chosen = attackSkills[rng.Next(attackSkills.Count)];
                return new SkillAction(ai, adjacentEnemy, chosen);
            }
        }

        // ── 2. Move toward the nearest party fighter ──────────────────
        var nearest = partyFighters.First();
        var path = FightResolver.BfsPath(
            state.Area,
            ai.X, ai.Y,
            nearest.X, nearest.Y,
            state.Fighters, ai);

        if (path != null && path.Count > 0)
        {
            // Budget as cost (cardinal=1.0, diagonal=1.5 per tile)
            double budget = ai.CurrentCineticPoints * (double)Math.Max(1, ai.MoveSpeed);

            // Stop one cell short so we don't overlap the target
            int stopAt = Math.Max(0, path.Count - 1);
            int px = ai.X, py = ai.Y;
            double accCost = 0;
            int affordable = 0;
            for (int i = 0; i < stopAt; i++)
            {
                double step = (path[i].X != px && path[i].Y != py) ? 1.5 : 1.0;
                if (accCost + step > budget + 1e-9) break;
                accCost += step; affordable++; px = path[i].X; py = path[i].Y;
            }

            if (affordable > 0)
            {
                var movePath = path.Take(affordable).ToList();
                return new MoveAction(ai, movePath);
            }
        }

        // ── 3. Nothing affordable — end turn ───────────────────────────
        return new EndTurnAction(ai);
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static int ManhattanDistance(Fighter a, Fighter b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    private static bool IsAdjacent(Fighter a, Fighter b) =>
        ManhattanDistance(a, b) == 1;
}
