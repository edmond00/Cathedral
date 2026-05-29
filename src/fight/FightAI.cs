using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight.Actions;

namespace Cathedral.Fight;

/// <summary>
/// Utility-scoring AI planner. Generates every plausible action the active enemy could
/// take this turn (end turn, attacks against every reachable target, defensive postures,
/// utility skills, and several move prefixes), scores each one with the fighter's
/// <see cref="AiPersonality"/>, jitters the scores so equal candidates break unpredictably,
/// and returns the highest-scoring candidate.
/// </summary>
public static class FightAI
{
    private const double JitterScale = 0.05;
    private const int    MoveSamples = 8;

    public static IFightAction DecideAction(
        Fighter ai,
        FightState state,
        FightingSkillRegistry registry,
        Random rng)
    {
        // 1. Knocked-down fighters can't act. Recovery rolls happen elsewhere.
        if (ai.IsKnockedDown)
            return new EndTurnAction(ai);

        var partyFighters = state.Fighters
            .Where(f => f.Faction == FighterFaction.Party && f.IsAlive)
            .ToList();
        if (partyFighters.Count == 0)
            return new EndTurnAction(ai);

        var personality = ai.Personality;
        var primary     = ChoosePriorityTarget(ai, partyFighters, state);

        var candidates = new List<Candidate>(32);
        candidates.Add(Candidate.End(ai, score: 0.0));

        // ── Attack candidates ──────────────────────────────────────────
        foreach (var skill in registry.GetAttackSkills())
        {
            if (!skill.IsUnlocked(ai)) continue;
            if (ai.CurrentCineticPoints < skill.CineticPointsCost) continue;
            string mediumKey = DefaultMediumKey(skill);
            if (state.UsedActionsThisTurn.Contains((mediumKey, skill.SkillId))) continue;

            foreach (var target in partyFighters)
            {
                double dx = ai.X - target.X, dy = ai.Y - target.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist > skill.Range + 0.001) continue;
                if (dist + 0.001 < skill.MinRange) continue;
                if (skill.Range > 1 &&
                    !FightResolver.HasLineOfSight(state.Area, ai.X, ai.Y, target.X, target.Y))
                    continue;

                double score = ScoreAttack(skill, ai, target, personality);
                int idx = state.Fighters.IndexOf(target);
                if (ai.LastAttackTargetIdx.HasValue && ai.LastAttackTargetIdx.Value == idx)
                    score += 0.15;

                candidates.Add(Candidate.Attack(ai, target, skill, score));
            }
        }

        // ── Defensive candidates (self-target) ─────────────────────────
        double hpRatio = ai.MaxHp > 0 ? ai.CurrentHp / (double)ai.MaxHp : 1.0;
        double incomingThreat = EstimateIncomingThreat(ai, partyFighters, state);
        foreach (var skill in registry.GetDefensiveSkills())
        {
            if (!skill.IsUnlocked(ai)) continue;
            if (ai.CurrentCineticPoints < skill.CineticPointsCost) continue;
            string mediumKey = DefaultMediumKey(skill);
            if (state.UsedActionsThisTurn.Contains((mediumKey, skill.SkillId))) continue;

            // Defense gets more attractive when hurt and when threats are nearby.
            double score = incomingThreat * personality.Caution * (1.2 - hpRatio)
                         - skill.CineticPointsCost * 0.1;
            candidates.Add(Candidate.SelfSkill(ai, skill, score));
        }

        // ── Utility / viscera candidates (self-target) ─────────────────
        foreach (var skill in registry.GetUtilitySkills())
        {
            if (!skill.IsUnlocked(ai)) continue;
            if (ai.CurrentCineticPoints < skill.CineticPointsCost) continue;
            string mediumKey = DefaultMediumKey(skill);
            if (state.UsedActionsThisTurn.Contains((mediumKey, skill.SkillId))) continue;

            double score = ScoreUtility(skill, ai, personality);
            candidates.Add(Candidate.SelfSkill(ai, skill, score));
        }

        // ── Move candidates ────────────────────────────────────────────
        if (primary != null && !ai.IsImmobilized)
        {
            foreach (var (path, dest) in EnumerateMoveCandidates(ai, state, primary))
            {
                double progress = ChebyshevDist(ai.X, ai.Y, primary.X, primary.Y)
                                - ChebyshevDist(dest.X, dest.Y, primary.X, primary.Y);

                double slipCost = 0;
                foreach (var step in path)
                {
                    int risk = FightResolver.EstimateSlipRiskPct(
                        state.Area.GetCell(step.X, step.Y).Type, ai.EquilibriumValue);
                    slipCost += risk / 100.0;
                }

                double score = progress * (0.6 + 0.6 * personality.Aggression)
                             - slipCost * (0.5 + personality.Caution);

                // Tiny preference for keeping the previously focused target in sight.
                if (ai.LastAttackTargetIdx is int lid
                    && lid >= 0 && lid < state.Fighters.Count
                    && state.Fighters[lid].Faction == FighterFaction.Party)
                {
                    var lt = state.Fighters[lid];
                    if (ChebyshevDist(dest.X, dest.Y, lt.X, lt.Y)
                      < ChebyshevDist(ai.X, ai.Y, lt.X, lt.Y))
                        score += 0.05;
                }

                candidates.Add(Candidate.Move(ai, path, score));
            }
        }

        // ── Jitter and pick the winner ─────────────────────────────────
        double jitterRange = JitterScale * (0.5 + personality.Cunning);
        Candidate best = candidates[0];
        double bestScore = best.Score + rng.NextDouble() * jitterRange;
        for (int i = 1; i < candidates.Count; i++)
        {
            double s = candidates[i].Score + rng.NextDouble() * jitterRange;
            if (s > bestScore) { best = candidates[i]; bestScore = s; }
        }

        // ── Apply the winning candidate's side effects ─────────────────
        if (best.Kind == CandidateKind.Attack && best.Skill != null && best.Target != null)
        {
            state.UsedActionsThisTurn.Add((DefaultMediumKey(best.Skill), best.Skill.SkillId));
            ai.LastAttackTargetIdx = state.Fighters.IndexOf(best.Target);
            state.PendingBodyPartId = best.Skill.WoundTargetMode == WoundTargetMode.PlayerChooses
                ? PickBodyPart(best.Target, personality, rng)
                : null;
        }
        else if (best.Kind == CandidateKind.SelfSkill && best.Skill != null)
        {
            state.UsedActionsThisTurn.Add((DefaultMediumKey(best.Skill), best.Skill.SkillId));
        }

        return best.Build();
    }

    // ── Candidate plumbing ─────────────────────────────────────────────

    private enum CandidateKind { End, Attack, SelfSkill, Move }

    private readonly struct Candidate
    {
        public CandidateKind Kind { get; }
        public double        Score { get; }
        public FightingSkill? Skill { get; }
        public Fighter?      Target { get; }
        private readonly Func<IFightAction> _build;

        private Candidate(CandidateKind kind, double score, FightingSkill? skill,
                          Fighter? target, Func<IFightAction> build)
        {
            Kind = kind; Score = score; Skill = skill; Target = target; _build = build;
        }

        public IFightAction Build() => _build();

        public static Candidate End(Fighter ai, double score) =>
            new(CandidateKind.End, score, null, null, () => new EndTurnAction(ai));

        public static Candidate Attack(Fighter ai, Fighter target, FightingSkill skill, double score) =>
            new(CandidateKind.Attack, score, skill, target, () => new SkillAction(ai, target, skill));

        public static Candidate SelfSkill(Fighter ai, FightingSkill skill, double score) =>
            new(CandidateKind.SelfSkill, score, skill, ai, () => new SkillAction(ai, ai, skill));

        public static Candidate Move(Fighter ai, List<(int X, int Y)> path, double score) =>
            new(CandidateKind.Move, score, null, null, () => new MoveAction(ai, path));
    }

    // ── Target selection ───────────────────────────────────────────────

    private static Fighter? ChoosePriorityTarget(Fighter ai, List<Fighter> party, FightState state)
    {
        Fighter? best = null;
        double bestScore = double.NegativeInfinity;
        foreach (var f in party)
        {
            double hpRatio = f.MaxHp > 0 ? f.CurrentHp / (double)f.MaxHp : 1.0;
            double dist    = ChebyshevDist(ai.X, ai.Y, f.X, f.Y);
            // Soften with distance, prefer wounded targets, lock onto previous victim.
            double score = -dist * 0.4 + (1.0 - hpRatio) * 1.5;
            int idx = state.Fighters.IndexOf(f);
            if (ai.LastAttackTargetIdx.HasValue && ai.LastAttackTargetIdx.Value == idx)
                score += 0.5;
            if (score > bestScore) { bestScore = score; best = f; }
        }
        return best;
    }

    // ── Scoring helpers ────────────────────────────────────────────────

    private static double ScoreAttack(FightingSkill skill, Fighter ai, Fighter target, AiPersonality p)
    {
        int totalDice = skill.TotalDice(ai);
        double hitProxy = Math.Clamp((totalDice - target.NaturalDefense * 1.5) / 10.0, 0.1, 1.0);
        double damageProxy = 0.6 + 0.05 * Math.Max(0, totalDice - target.NaturalDefense);

        double score = damageProxy * hitProxy * (1.0 + p.Aggression);

        // Status effect bonuses — heavier when the target isn't already saddled with them.
        foreach (var eff in skill.SpecialEffects)
        {
            switch (eff)
            {
                case BleedingEffect b:
                    if (!target.ActiveEffects.OfType<BleedingEffect>().Any())
                        score += 0.5 * Math.Max(1, b.Level);
                    else
                        score += 0.15 * Math.Max(1, b.Level);
                    break;
                case KnockdownEffect:
                    if (!target.IsKnockedDown) score += 0.6;
                    break;
                case ImmobilizeEffect:
                    if (!target.IsImmobilized) score += 0.4;
                    break;
                default:
                    score += 0.15;
                    break;
            }
        }

        score -= skill.CineticPointsCost * 0.10;
        score -= skill.VitalHeatCost     * 0.20;

        // Ranged attacks already at range — no movement cost to consider.
        return score;
    }

    private static double ScoreUtility(FightingSkill skill, Fighter ai, AiPersonality p)
    {
        // Generic base value scaled by Cunning (uses utility moves more deliberately).
        double score = 0.3 * p.Cunning;

        // Visceral / vital-heat-costing skills (Rage, BloodLust, ColdBlood, IronNerves):
        // valuable when CP is plentiful and the fighter is hurt. Cost is paid in VH.
        if (skill.VitalHeatCost > 0)
        {
            double hpRatio = ai.MaxHp > 0 ? ai.CurrentHp / (double)ai.MaxHp : 1.0;
            score += (1.0 - hpRatio) * 0.6 * p.Aggression;
            score -= skill.VitalHeatCost * 0.25;
        }
        else
        {
            score -= skill.CineticPointsCost * 0.10;
        }

        return score;
    }

    /// <summary>
    /// Rough "how much damage am I about to soak next round" — sum of attack potential from
    /// every party fighter that's already in melee range or one step away.
    /// </summary>
    private static double EstimateIncomingThreat(Fighter ai, List<Fighter> party, FightState state)
    {
        double threat = 0;
        foreach (var f in party)
        {
            double d = ChebyshevDist(ai.X, ai.Y, f.X, f.Y);
            if (d > 3) continue;
            double proximity = d <= 1 ? 1.0 : (d <= 2 ? 0.6 : 0.3);
            threat += proximity * (1.0 + f.CurrentCineticPoints * 0.05);
        }
        return threat;
    }

    // ── Movement enumeration ───────────────────────────────────────────

    private static IEnumerable<(List<(int X, int Y)> Path, (int X, int Y) Dest)>
        EnumerateMoveCandidates(Fighter ai, FightState state, Fighter target)
    {
        var fullPath = FightResolver.BfsPath(
            state.Area, ai.X, ai.Y, target.X, target.Y, state.Fighters, ai);
        if (fullPath == null || fullPath.Count == 0) yield break;

        // Stop one cell short so we don't overlap the target.
        int stopAt = Math.Max(0, fullPath.Count - 1);
        if (stopAt == 0) yield break;

        double budget = ai.CurrentCineticPoints * Math.Max(1, ai.MoveSpeed);
        double accCost = 0;
        int px = ai.X, py = ai.Y;
        int affordable = 0;
        for (int i = 0; i < stopAt; i++)
        {
            double step = (fullPath[i].X != px && fullPath[i].Y != py) ? 1.5 : 1.0;
            if (accCost + step > budget + 1e-9) break;
            accCost += step; affordable++;
            px = fullPath[i].X; py = fullPath[i].Y;
        }
        if (affordable == 0) yield break;

        // Sample a few path prefixes so the planner can choose between "rush in" and
        // "advance partially". One candidate per ~ceil(affordable/MoveSamples) tiles,
        // plus always the max-affordable prefix.
        int stride = Math.Max(1, affordable / MoveSamples);
        var emitted = new HashSet<int>();
        for (int len = stride; len <= affordable; len += stride)
        {
            if (!emitted.Add(len)) continue;
            var path = fullPath.Take(len).ToList();
            yield return (path, path[^1]);
        }
        if (emitted.Add(affordable))
        {
            var path = fullPath.Take(affordable).ToList();
            yield return (path, path[^1]);
        }
    }

    // ── Body-part picker ───────────────────────────────────────────────

    /// <summary>
    /// Pick a body-part id from <paramref name="defender"/>'s anatomy weighted toward
    /// vital regions (trunk, encephalon) when <see cref="AiPersonality.Cunning"/> is high.
    /// Falls back to a uniform pick when no weights resolve.
    /// </summary>
    private static string? PickBodyPart(Fighter defender, AiPersonality p, Random rng)
    {
        var parts = defender.Member.BodyParts;
        if (parts.Count == 0) return null;

        // Flat baseline, sharpened toward vitals as Cunning rises.
        double sharp = 0.5 + 2.0 * p.Cunning;
        var weights = new double[parts.Count];
        double sum = 0;
        for (int i = 0; i < parts.Count; i++)
        {
            double w = VitalityWeight(parts[i].Id);
            // (vitality)^sharp gives more concentrated weight on vitals at high cunning.
            w = Math.Pow(w, sharp);
            weights[i] = w;
            sum += w;
        }
        if (sum <= 0) return parts[rng.Next(parts.Count)].Id;

        double roll = rng.NextDouble() * sum;
        double acc = 0;
        for (int i = 0; i < parts.Count; i++)
        {
            acc += weights[i];
            if (roll <= acc) return parts[i].Id;
        }
        return parts[^1].Id;
    }

    private static double VitalityWeight(string bodyPartId) => bodyPartId switch
    {
        "trunk"       => 1.00, // contains heart / pulmones / viscera
        "encephalon"  => 0.95,
        "visage"      => 0.55,
        "upper_limbs" => 0.35,
        "lower_limbs" => 0.30,
        _             => 0.50,
    };

    // ── Misc helpers ───────────────────────────────────────────────────

    private static double ChebyshevDist(int ax, int ay, int bx, int by) =>
        Math.Max(Math.Abs(ax - bx), Math.Abs(ay - by));

    /// <summary>Default medium key for usage tracking (organ:&lt;id&gt; or mm:&lt;id&gt;).</summary>
    private static string DefaultMediumKey(FightingSkill s) =>
        s.Medium.Type == MediumType.OrganMedium
            ? $"organ:{s.Medium.OrganId ?? s.SkillId}"
            : $"mm:{s.RequiredModusMentisId}";
}
