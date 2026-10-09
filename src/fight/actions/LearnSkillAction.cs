using System;

namespace Cathedral.Fight.Actions;

/// <summary>
/// Attempts a fighting skill the fighter does not know — the learning check the player makes by
/// clicking a skill in the "learnable" rows, made here by the fight AI.
///
/// <para>Without it an NPC who held no fighting modus mentis could not strike at all: every attack
/// skill is gated behind one, the AI only ever weighed what was unlocked, and so an apprentice
/// walked up beside the player and passed every turn with a full pool of CP. The player in the same
/// position tries a punch and may learn it; this is the same rule, played from the other side.</para>
///
/// <para>Only the roll is set up here. The window animates it and resolves it in
/// <c>FinishLearningRoll</c> like the player's own, except that a fighter the AI controls is not
/// handed straight into the skill afterwards: it decides again, now knowing it, and pays for it as
/// for any other blow.</para>
/// </summary>
public class LearnSkillAction : IFightAction
{
    public Fighter Learner { get; }
    public Fighter? Target { get; }
    public FightingSkill Skill { get; }
    public string? MediumKey { get; }

    public LearnSkillAction(Fighter learner, Fighter? target, FightingSkill skill, string? mediumKey)
    {
        Learner   = learner;
        Target    = target;
        Skill     = skill;
        MediumKey = mediumKey;
    }

    public void Execute(FightState state, Random rng)
    {
        state.PendingLearnSkill  = Skill;
        state.PendingSkill       = null; // signals "this is a learning roll, not an attack roll"
        state.PendingTarget      = Target;
        state.LearningDiceCount  = Math.Max(1, Learner.FightLearningStat);
        state.LearningDifficulty = FightResolver.LearningDifficulty(Skill, MediumKey);
        state.DiceNumberOfDice   = state.LearningDiceCount;
        state.DiceDifficulty     = state.LearningDifficulty;
        state.Phase              = TurnPhase.AnimatingDice;
        state.AddLog(
            $"{Learner.DisplayName} attempts to learn '{Skill.RequiredModusMentisId}' " +
            $"(cerebellum {state.LearningDiceCount}d, need {state.LearningDifficulty + 1} six(es)).",
            LogEntryType.Learning);
    }
}
