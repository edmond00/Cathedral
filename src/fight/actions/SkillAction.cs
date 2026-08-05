using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;

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

    /// <summary>
    /// Organ part this skill is performed with (e.g. "left_hand"), or null to use the whole
    /// organ. Determines which part's level feeds the dice count for organ-medium skills.
    /// </summary>
    public string? OrganPartId { get; }

    /// <summary>
    /// The medium the skill is being executed through. Overrides <see cref="FightingSkill.Medium"/>
    /// in the dice calculation for skills that appear in multiple medium lists.
    /// Null means use the skill's primary medium (<see cref="FightingSkill.Medium"/>).
    /// </summary>
    public FightingMedium? ActiveMedium { get; }

    public SkillAction(Fighter attacker, Fighter target, FightingSkill skill,
        string? organPartId = null, FightingMedium? activeMedium = null)
    {
        Attacker     = attacker;
        Target       = target;
        Skill        = skill;
        OrganPartId  = organPartId;
        ActiveMedium = activeMedium;
    }

    /// <summary>Turns a body-part id into something readable in the combat log.</summary>
    private static string Prettify(string? bodyPartId) =>
        string.IsNullOrEmpty(bodyPartId) ? "body" : bodyPartId.Replace('_', ' ');

    public void Execute(FightState state, Random rng)
    {
        // Deduct CP
        int cost = Skill.CineticPointsCost;
        Attacker.CurrentCineticPoints = Math.Max(0, Attacker.CurrentCineticPoints - cost);

        // ── Buffs: vital heat, no dice ────────────────────────────────────────────
        // A buff has no target and nothing to hit, so there is nothing for dice to decide. It is
        // paid for in vital heat instead, and the effect is applied here — before the animation —
        // so the STATE pane shows it while the consumption box is still up.
        if (Skill.EffectType == FightingSkillEffect.Buff)
        {
            var effect = Skill.CreateBuffEffect(Attacker);
            int vh = Skill.VitalHeatCostFor(Attacker, OrganPartId, ActiveMedium);

            var drawn = new List<BodyHumor>(vh);
            for (int i = 0; i < vh; i++)
            {
                var humor = Attacker.Member.HumorQueues.ConsumeCycled(Attacker.Member, rng);
                if (humor == null) break;           // queues fully critical — nothing left to spend
                drawn.Add(humor);
            }

            if (effect != null)
            {
                Attacker.ActiveEffects.Add(effect);
                effect.OnApply(Attacker, Attacker, state, rng);
                if (effect.IsExpired) Attacker.ActiveEffects.Remove(effect);
            }

            // "X uses Y" — never "X uses Y on X". There is no second party here.
            state.AddLog($"{Attacker.DisplayName} uses {Skill.DisplayName}.  [-{cost} CP, -{drawn.Count} VH]",
                LogEntryType.SpecialEffect);

            state.BeginVitalHeatConsumption(Attacker, Skill.DisplayName, vh, drawn);
            return;
        }

        // Defensive guards are bought, not rolled for: defence IS a dice count, so the skill hands
        // its level straight to the pool the next incoming attack is measured against. (These used
        // to run the attack path against the user's own defence, which is how a parry could wound
        // the person parrying.)
        if (Skill.EffectType == FightingSkillEffect.DefensePosture
         || Skill.EffectType == FightingSkillEffect.Defense)
        {
            int dice = Skill.TotalDice(Attacker, OrganPartId, ActiveMedium);
            var guard = Skill.EffectType == FightingSkillEffect.DefensePosture
                ? new DefensePostureEffect(dice)
                : (FightStatusEffect)new GuardEffect(Skill.DisplayName, dice);
            Attacker.ActiveEffects.Add(guard);
            guard.OnApply(Attacker, Attacker, state, rng);
            state.AddLog($"{Attacker.DisplayName} uses {Skill.DisplayName}.  [-{cost} CP]");
            state.Phase = TurnPhase.TurnEnding;
            return;
        }

        // Set up dice roll for the window to animate (two-roll: attack dice vs defense dice)
        state.PendingSkill  = Skill;
        state.PendingTarget = Target;

        // ── Where the blow is aimed, decided BEFORE the dice ──────────────────────
        // Armour has to be counted into the defence pool, and the pool is sized here — so the
        // location cannot wait until the wound is picked. Resolving it for all three targeting
        // modes (not just the aimed ones) is what lets armour matter against ordinary attacks,
        // which are the overwhelming majority. Reset first: ContinueTurnOrEnd does not clear
        // pending state, so a second skill in the same turn would inherit the first one's section.
        state.PendingArmorSection = null;
        if (Target != Attacker)
        {
            if (Skill.WoundTargetMode == WoundTargetMode.FixedBodyPart)
                state.PendingBodyPartId = Skill.TargetBodyPartId;
            else if (Skill.WoundTargetMode == WoundTargetMode.Random)
                state.PendingBodyPartId = FightResolver.PreRollHitLocation(Target, rng);
            // PlayerChooses already wrote PendingBodyPartId from the localization overlay.

            state.PendingArmorSection =
                FightResolver.ResolveSectionBodyPartId(Target, state.PendingBodyPartId);
        }

        int armor = Target.ArmorDice(state.PendingArmorSection);

        // Natural attack dice apply only to offensive rolls against another fighter,
        // not to self-targeted (defense/utility) skills. Effect bonuses fold in on both sides:
        // a feint's carry-over on the attack, a parry or stance on the defence.
        state.DiceNumberOfDice          = Skill.TotalDice(Attacker, OrganPartId, ActiveMedium)
                                          + (Target != Attacker ? Attacker.NaturalAttack : 0)
                                          + Attacker.BonusAttackDice;
        state.DiceSecondaryNumberOfDice = Target.NaturalDefense + armor + Target.BonusDefenseDice;
        state.DiceDifficulty            = state.DiceSecondaryNumberOfDice; // kept for logging
        state.IsDiceRolling             = true;
        state.DiceFinalValues           = null;
        state.DiceSecondaryFinalValues  = null;
        state.Phase                     = TurnPhase.AnimatingDice;

        string aim = armor > 0
            ? $" (aimed at the {Prettify(state.PendingArmorSection)}, armour +{armor})"
            : "";
        // Self-targeted skills read "X uses Y", never "X uses Y on X".
        string on = Target != Attacker ? $" on {Target.DisplayName}" : "";
        state.AddLog($"{Attacker.DisplayName} uses {Skill.DisplayName}{on}{aim}.  [-{cost} CP]");
    }
}
