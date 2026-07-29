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

        // Handle defense posture differently — no dice roll needed
        if (Skill.EffectType == FightingSkillEffect.DefensePosture)
        {
            Attacker.IsDefensePostureActive = true;
            state.AddLog($"{Attacker.DisplayName} takes a defensive stance.  [-{cost} CP]");
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
        // not to self-targeted (defense/utility) skills.
        state.DiceNumberOfDice          = Skill.TotalDice(Attacker, OrganPartId, ActiveMedium)
                                          + (Target != Attacker ? Attacker.NaturalAttack : 0);
        state.DiceSecondaryNumberOfDice = Target.NaturalDefense + armor;
        state.DiceDifficulty            = state.DiceSecondaryNumberOfDice; // kept for logging
        state.IsDiceRolling             = true;
        state.DiceFinalValues           = null;
        state.DiceSecondaryFinalValues  = null;
        state.Phase                     = TurnPhase.AnimatingDice;

        string aim = armor > 0
            ? $" (aimed at the {Prettify(state.PendingArmorSection)}, armour +{armor})"
            : "";
        state.AddLog($"{Attacker.DisplayName} uses {Skill.DisplayName} on {Target.DisplayName}{aim}.  [-{cost} CP]");
    }
}
