using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative.Work;

/// <summary>
/// The per-skill result of working a job for a number of months: how much XP it earned, whether the
/// skill was already known, how many levels it gained, and — for an unknown skill — whether the stint
/// was long enough to learn it outright.
/// </summary>
public sealed class WorkMmResult
{
    public required string DisplayName { get; init; }
    public int  XpGained    { get; init; }
    public bool WasKnown    { get; init; }
    public int  LevelsGained{ get; init; }
    public bool AtMaxLevel  { get; init; }
    /// <summary>An unknown skill: true when the stint's XP reached the pineal threshold and it was learned.</summary>
    public bool Learned     { get; init; }
    /// <summary>A skill permanently displaced from memory by learning this one (or null).</summary>
    public ModusMentis? Dropped { get; init; }
}

/// <summary>
/// The full outcome of a work stint: coins earned plus the three per-skill results. Produced without
/// mutation by <see cref="Preview"/> (for the UI) and with mutation by <see cref="Apply"/> (on confirm).
///
/// XP is front-loaded: the first (most-defining) skill earns 1 XP per month, the second 1 per 3 months,
/// the third 1 per 5 months. Known skills accrue that XP (levelling up as the pineal threshold is
/// crossed, capped at max level). An unknown skill is learned at level 1 only if its XP for the stint
/// reaches the threshold in one go; otherwise nothing is learned for it.
/// </summary>
public sealed class WorkOutcome
{
    public required Job      Job   { get; init; }
    public required int      Months{ get; init; }
    public required CoinType Coin  { get; init; }
    public required int      Coins { get; init; }
    public required List<WorkMmResult> Skills { get; init; }

    /// <summary>XP the skill at ordinal <paramref name="index"/> (0..2) earns over <paramref name="months"/>.</summary>
    public static int XpForSkill(int index, int months) => index switch
    {
        0 => months,
        1 => months / 3,
        _ => months / 5,
    };

    private static int CoinsFor(Job job, int months) => (int)Math.Floor(job.CoinsPerMonth * months);

    /// <summary>Computes the outcome for the UI preview without changing any game state.</summary>
    public static WorkOutcome Preview(Job job, int months, PartyMember member)
        => Build(job, months, member, apply: false, party: null);

    /// <summary>Applies the outcome: credits coins, awards XP to known skills, learns eligible unknown ones.</summary>
    public static WorkOutcome Apply(Job job, int months, PartyMember member, Party party)
        => Build(job, months, member, apply: true, party: party);

    private static WorkOutcome Build(Job job, int months, PartyMember member, bool apply, Party? party)
    {
        int coins = CoinsFor(job, months);
        if (apply && party != null && coins > 0)
            party.Add(job.PayCoin, coins);

        int threshold = member.GetModusMentisXpThreshold();
        var skills = new List<WorkMmResult>();

        for (int i = 0; i < job.ModusMentisIds.Length; i++)
        {
            string mmId = job.ModusMentisIds[i];
            int    xp   = XpForSkill(i, months);

            var known = member.LearnedModiMentis.FirstOrDefault(m => m.ModusMentisId == mmId);
            if (known != null)
            {
                int maxLevel = member.GetMaxLevelForModusMentis(known);
                int levels;
                if (apply)
                {
                    int before = known.Level;
                    for (int k = 0; k < xp; k++) member.AwardModusMentisXp(known, 1);
                    levels = known.Level - before;
                }
                else
                {
                    levels = SimulateLevels(known.Level, known.CurrentXp, xp, threshold, maxLevel);
                }
                skills.Add(new WorkMmResult
                {
                    DisplayName  = known.DisplayName,
                    XpGained     = xp,
                    WasKnown     = true,
                    LevelsGained = levels,
                    AtMaxLevel   = (known.Level >= maxLevel),
                });
                continue;
            }

            // Unknown skill: learnable only if the stint's XP reaches the threshold in one go.
            var template = ModusMentisRegistry.Instance.GetModusMentis(mmId);
            bool willLearn = template != null && xp >= threshold;
            ModusMentis? dropped = null;

            if (apply && willLearn && template != null)
            {
                var fresh = (ModusMentis)Activator.CreateInstance(template.GetType())!;
                fresh.Level     = 1;
                fresh.CurrentXp = 0;
                dropped = member.AcquireModusMentis(fresh);
            }

            skills.Add(new WorkMmResult
            {
                DisplayName = template?.DisplayName ?? mmId,
                XpGained    = xp,
                WasKnown    = false,
                Learned     = willLearn,
                Dropped     = dropped,
            });
        }

        return new WorkOutcome
        {
            Job    = job,
            Months = months,
            Coin   = job.PayCoin,
            Coins  = coins,
            Skills = skills,
        };
    }

    /// <summary>Predicts how many levels a known skill would gain, mirroring <see cref="PartyMember.AwardModusMentisXp"/>.</summary>
    private static int SimulateLevels(int level, int currentXp, int xp, int threshold, int maxLevel)
    {
        int gained = 0;
        for (int k = 0; k < xp; k++)
        {
            if (level >= maxLevel) break;
            currentXp++;
            if (currentXp >= threshold)
            {
                currentXp = 0;
                level++;
                gained++;
            }
        }
        return gained;
    }
}
