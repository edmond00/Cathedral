using System;

namespace Cathedral.Fight.Actions;

/// <summary>
/// Commits an attacker to using a <see cref="FightingSkill"/> against a target.
/// Sets the dice-roll state in <see cref="FightState"/> so the window can animate it.
/// The actual wound application happens in <see cref="FightResolver.ResolveAttack"/>
/// once the dice animation completes and the window reads <see cref="FightState.DiceFinalValues"/>.
/// </summary>
public class SkillAction : IFightAction
{
    public Fighter Attacker { get; }
    public Fighter Target   { get; }
    public FightingSkill Skill { get; }

    public SkillAction(Fighter attacker, Fighter target, FightingSkill skill)
    {
        Attacker = attacker;
        Target   = target;
        Skill    = skill;
    }

    public void Execute(FightState state, Random rng)
    {
        // Deduct CP
        int cost = Skill.CineticPointsCost;
        Attacker.CurrentCineticPoints = Math.Max(0, Attacker.CurrentCineticPoints - cost);

        // Handle defense posture differently — no dice roll needed
        if (Skill.EffectType == FightingSkillEffect.DefensePosture)
        {
            Attacker.IsDefensePostureActive = true;
            state.AddLog($"{Attacker.DisplayName} takes a defensive stance.  [-{cost} CP]");
            state.Phase = TurnPhase.TurnEnding;
            return;
        }

        // Set up dice roll for the window to animate
        state.PendingSkill  = Skill;
        state.PendingTarget = Target;
        state.DiceNumberOfDice = Skill.TotalDice(Attacker);
        state.DiceDifficulty   = Target.NaturalDefense;
        state.IsDiceRolling    = true;
        state.DiceFinalValues  = null;
        state.Phase            = TurnPhase.AnimatingDice;

        state.AddLog($"{Attacker.DisplayName} uses {Skill.DisplayName} on {Target.DisplayName}.  [-{cost} CP]");
    }
}
