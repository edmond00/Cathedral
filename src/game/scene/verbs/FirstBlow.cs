using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// One fighting medium a body can open a fight with, paired with the skills that medium carries.
/// </summary>
/// <param name="Medium">The medium itself, as the fight system understands it.</param>
/// <param name="Label">What to call it in prose — "hands", "upper limbs", the weapon's own name.</param>
/// <param name="SkillIds">The medium category's ordered skill list, easiest to learn first.</param>
public sealed record FirstBlowMedium(FightingMedium Medium, string Label, IReadOnlyList<string> SkillIds);

/// <summary>One candidate opening blow: a skill, and the medium it would be struck with.</summary>
public sealed record FirstBlowAttack(FightingSkill Skill, FirstBlowMedium Medium);

/// <summary>
/// What a body can open a fight <b>with</b> — the medium, the skill, and whether there is anything
/// there at all. The <c>attack</c> verb strikes before the fight screen exists, so the same question
/// the fight asks every turn ("which medium, which skill") has to be answerable outside it.
///
/// <para><b>Read by two layers, which is the whole reason it is a class of its own.</b>
/// <see cref="Rules.FirstBlowMediumRule"/> asks it <i>before</i> the dice, so a body with nothing to
/// strike with is refused by the coded rules rather than rolling and mysteriously achieving nothing;
/// <c>FirstBlowOutcome</c> asks it <i>after</i>, to sample the blow that lands. Two copies of the
/// gathering would mean the refusal and the blow could disagree about what the body can do.</para>
///
/// <para>Nothing here rolls anything except <see cref="Sample"/>, and nothing here touches the
/// world: it is a pure reading of the acting body and the implement in its hand.</para>
/// </summary>
public static class FirstBlow
{
    /// <summary>
    /// The mediums this body may strike with, given what it combined with the action.
    ///
    /// <para><b>An implement replaces the body rather than adding to it.</b> A combined weapon is the
    /// whole list — a man who draws a sword is not also kicking — and a body with empty hands offers
    /// every medium it owns. Note this reads the <i>combined</i> item, never the equipped one: the
    /// weapon on your belt is not the weapon in the blow unless the player put it there, and choosing
    /// to is what the tool combination is for.</para>
    ///
    /// <para>An implement that is not a weapon returns nothing at all, which never reaches a player:
    /// <see cref="Rules.ToolCombinationRules.Resolve"/> refuses that combination before the action
    /// exists. It is answered here anyway so the two layers cannot disagree.</para>
    /// </summary>
    public static IReadOnlyList<FirstBlowMedium> MediumsFor(PartyMember actor, Item? tool)
    {
        if (tool != null)
        {
            if (tool is not IWeaponItem weapon) return Array.Empty<FirstBlowMedium>();
            var category = WeaponMediumRegistry.GetById(weapon.WeaponCategory);
            if (category == null) return Array.Empty<FirstBlowMedium>();
            return new[] { new FirstBlowMedium(FightingMedium.Weapon, tool.DisplayName.ToLowerInvariant(),
                                               category.SkillIds) };
        }

        var mediums = new List<FirstBlowMedium>();

        // Organ mediums — hands, feet, fangs, claws, teeth, arms, legs, viscera. Most of these carry
        // no attack at all (viscera is all buffs, legs all movement and guard); the filter in
        // AttacksFor is what drops them, not this one.
        foreach (var category in OrganMediumRegistry.GetAll())
        {
            var organ = actor.GetOrganById(category.OrganId);
            if (organ == null || organ.Score <= 0) continue;
            if (IsDisabled(actor, organId: category.OrganId, bodyPartId: organ.BodyPartId)) continue;
            // The category's own display name, not the organ id: the id is a database key and reads
            // like one in prose ("with my teeths"), while the category is what the fight screen
            // already calls that medium.
            mediums.Add(new FirstBlowMedium(FightingMedium.Organ(category.OrganId),
                                            category.DisplayName.ToLowerInvariant(), category.SkillIds));
        }

        // Body-part mediums — a whole region used as one medium (upper_limbs: seize, chokehold).
        // Included for the same reason the fight screen lists them: grappling is a way of striking
        // somebody, and a body that has arms has it whether or not it has trained hands.
        foreach (var category in BodyPartMediumRegistry.GetAll())
        {
            var bodyPart = actor.GetBodyPartById(category.BodyPartId);
            if (bodyPart == null || bodyPart.Score <= 0) continue;
            if (IsDisabled(actor, organId: null, bodyPartId: category.BodyPartId)) continue;
            mediums.Add(new FirstBlowMedium(FightingMedium.BodyPart(category.BodyPartId),
                                            category.DisplayName.ToLowerInvariant(), category.SkillIds));
        }

        return mediums;
    }

    /// <summary>
    /// Every attack the body could open with — its mediums' skill lists, minus everything that is not
    /// an attack and everything this body could never hold the lesson of.
    ///
    /// <para><b>An unlearned skill is a candidate.</b> Requiring the modus mentis would mean a
    /// character who has never been in a fight cannot throw a punch, which is backwards: the first
    /// blow is where the punch is learned. What is required instead is that the lesson could be
    /// <i>kept</i> — <see cref="ModusMentisAnatomy.IsLearnableBy"/> — so a body is never taught
    /// something its anatomy caps at level 1 forever.</para>
    /// </summary>
    public static IReadOnlyList<FirstBlowAttack> AttacksFor(PartyMember actor, Item? tool)
        => AttacksFrom(actor, MediumsFor(actor, tool));

    /// <summary>Same filter as <see cref="AttacksFor"/>, over mediums already gathered.</summary>
    public static IReadOnlyList<FirstBlowAttack> AttacksFrom(
        PartyMember actor, IReadOnlyList<FirstBlowMedium> mediums)
    {
        var attacks = new List<FirstBlowAttack>();
        var seen    = new HashSet<string>();

        foreach (var medium in mediums)
            foreach (var skillId in medium.SkillIds)
            {
                // A skill listed by two mediums (flesh_tear is in both fangs and teeth) is one
                // candidate, not two — otherwise the medium list would weight the draw.
                if (!seen.Add(skillId)) continue;

                var skill = FightingSkillRegistry.Instance.GetById(skillId);
                if (skill == null || skill.EffectType != FightingSkillEffect.Attack) continue;
                if (!CanHoldTheLesson(actor, skill)) continue;

                attacks.Add(new FirstBlowAttack(skill, medium));
            }

        return attacks;
    }

    /// <summary>
    /// Draws one opening blow uniformly, or null when there is none — which the coded rule layer has
    /// already refused, so a null here means something changed between the refusal and the roll.
    /// </summary>
    public static FirstBlowAttack? Sample(PartyMember actor, Item? tool, Random rng)
    {
        var attacks = AttacksFor(actor, tool);
        return attacks.Count == 0 ? null : attacks[rng.Next(attacks.Count)];
    }

    /// <summary>
    /// Why this body cannot open a fight, in the first person, or null when it can. The two failures
    /// are genuinely different news — nothing to strike with, versus a medium that knows no strike —
    /// and the second is only reachable through an implement, since every bare medium the body owns
    /// is offered at once.
    /// </summary>
    public static string? Refusal(PartyMember actor, Item? tool)
    {
        var mediums = MediumsFor(actor, tool);
        if (mediums.Count == 0)
            return tool != null
                ? $"I could not raise {tool.WithArticle()} against anyone."
                : "there is nothing left in me to strike with.";

        if (AttacksFrom(actor, mediums).Count == 0)
            return tool != null
                ? $"I would not know how to strike a blow with {tool.WithArticle()}."
                : "I would not know how to strike a blow.";

        return null;
    }

    /// <summary>
    /// Whether the modus mentis this skill is built on is one this body could keep. Already-known
    /// counts, which matters for the borderline case of a skill learned before a wound or a change of
    /// body — refusing it there would take away something the character demonstrably has.
    /// </summary>
    private static bool CanHoldTheLesson(PartyMember actor, FightingSkill skill)
    {
        if (actor.LearnedModiMentis.Any(m => m.ModusMentisId == skill.RequiredModusMentisId))
            return true;

        var template = ModusMentisRegistry.Instance.GetModusMentis(skill.RequiredModusMentisId);
        return template != null && ModusMentisAnatomy.IsLearnableBy(template, actor);
    }

    /// <summary>
    /// Whether a High-handicap wound has taken this medium out of use — the same test
    /// <see cref="FightingSkill.IsAnyMediumAvailable"/> applies inside a fight, so a blow refused
    /// here is refused there too.
    /// </summary>
    private static bool IsDisabled(PartyMember actor, string? organId, string bodyPartId)
        => actor.Wounds.Any(w => w.Handicap == WoundHandicap.High
            && ((organId != null && w.TargetKind == WoundTargetKind.Organ && w.TargetId == organId)
             || (w.TargetKind == WoundTargetKind.BodyPart && w.TargetId == bodyPartId)));
}
