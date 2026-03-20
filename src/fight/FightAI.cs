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
            // Budget: how many steps can we afford?
            int stepsPerCp = Math.Max(1, ai.MoveSpeed);
            int maxSteps   = ai.CurrentCineticPoints * stepsPerCp;

            // Trim to stop adjacent to target (leave last cell empty so we don't overlap)
            int stopAt = Math.Max(0, path.Count - 1);  // don't step into target's cell
            int trimmed = Math.Min(stopAt, maxSteps);

            if (trimmed > 0)
            {
                var movePath = path.Take(trimmed).ToList();
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
