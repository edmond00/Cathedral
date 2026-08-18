using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Scene;

/// <summary>
/// The blow that opens a fight. <c>attack</c> used to be a doorway and nothing else — it rolled, it
/// printed nothing a player could read, and the fight began with both sides untouched. This is the
/// swing itself: one attack skill drawn from what the body (or the weapon in its hand) can actually
/// do, landing without a second roll, and the fight starts with the mark already on them.
///
/// <para><b>Everything it needs is settled in the constructor</b>, not in <see cref="Apply"/>. The
/// reports are gathered before the outcome is narrated so their <see cref="Outcome.Verbatim"/> can
/// feed the prose, and the narrator is told which blow landed where — so the skill, the wound and the
/// decision to withhold it all have to be known by then. <see cref="Apply"/> only carries out what
/// was already decided, which is also what makes it safe for the same instance to be reused at commit
/// time (see <c>NarrativeController.GatherVerbReports</c>).</para>
///
/// <para><b>A first blow can never kill.</b> A target already down to its last hit point turns it
/// aside: the lesson is still learned, and the fight still begins, but no wound is dealt. Without
/// that a corpse would be left standing in the room — <see cref="NpcEntity.IsAlive"/> reads the hit
/// points, so a body killed here would be dead with no <c>RemoveNpcFromPlay</c>, no corpse and a
/// fight opening against someone already gone. Killing outright is <c>slay</c>'s job, and it is a
/// different verb with a different difficulty for a reason.</para>
///
/// <para>Shallow wildlife has no anatomy to wound, so the blow simply kills — the kill itself is
/// <see cref="NpcSlaynOutcome"/>, reported alongside this, which is what spawns the carcass.</para>
/// </summary>
public sealed class FirstBlowOutcome : Outcome
{
    private readonly INpcEntity        _target;
    private readonly FirstBlowAttack   _attack;
    private readonly Wound?            _wound;
    private readonly bool              _withheld;

    private FirstBlowOutcome(INpcEntity target, FirstBlowAttack attack, Wound? wound, bool withheld,
                             string text, string verbatim)
        : base(text, OutcomeSeverity.Positive, verbatim)
    {
        _target   = target;
        _attack   = attack;
        _wound    = wound;
        _withheld = withheld;
    }

    /// <summary>The skill that landed — its modus mentis is what the attack teaches.</summary>
    public FightingSkill Skill => _attack.Skill;

    /// <summary>
    /// Resolves the opening blow, or returns null when this body has nothing to strike with — which
    /// <c>FirstBlowMediumRule</c> has already refused before the dice, so a null here means the body
    /// changed between the refusal and the roll.
    /// </summary>
    public static FirstBlowOutcome? For(PartyMember actor, INpcEntity target, Item? tool)
    {
        var rng    = GameRng.Stream("first_blow");
        var attack = FirstBlow.Sample(actor, tool, rng);
        if (attack == null) return null;

        string skillName = attack.Skill.DisplayName;
        string with      = $"a {skillName.ToLowerInvariant()} with my {attack.Medium.Label}";

        // Shallow wildlife: no anatomy, so nothing to wound. The blow is the kill, and the carcass
        // is NpcSlaynOutcome's business.
        if (target is not NpcEntity npc)
            return new FirstBlowOutcome(target, attack, null, withheld: false,
                $"First blow: {skillName}",
                $"struck {target.DisplayName} down with {with}");

        // Down to the last hit point: they get their guard up in time. See the class note — this is
        // what keeps a first blow from leaving a body nobody removed from play.
        if (npc.Combatant.CurrentHp <= 1)
            return new FirstBlowOutcome(target, attack, null, withheld: true,
                $"First blow: {skillName} — turned aside",
                $"struck the first blow at {npc.DisplayName}, {with}, and it was turned aside");

        var wound = PickWound(actor, npc, attack.Skill, rng);
        if (wound == null)
            return new FirstBlowOutcome(target, attack, null, withheld: false,
                $"First blow: {skillName}",
                $"struck the first blow at {npc.DisplayName}, {with}");

        string where = wound.TargetId.Length > 0 ? wound.TargetId.Replace('_', ' ') : "body";
        return new FirstBlowOutcome(target, attack, wound, withheld: false,
            $"First blow: {skillName} — {wound.WoundName} ({where})",
            $"struck the first blow at {npc.DisplayName}, {with}, "
          + $"and left {Article(wound.WoundName)} {wound.WoundName.ToLowerInvariant()}");
    }

    /// <summary>"a" or "an" for a wound name, which is read straight into the narrator's sentence.</summary>
    private static string Article(string name)
        => name.Length > 0 && "aeiou".Contains(char.ToLowerInvariant(name[0])) ? "an" : "a";

    /// <summary>
    /// Where the blow lands, and what it leaves — the fight's own arithmetic, run once with no dice
    /// behind it. The hit location is pre-rolled exactly as <c>SkillAction</c> pre-rolls it, because
    /// the wound pool is filtered by location and a blow with no location draws only wildcards.
    ///
    /// <para>Armour is deliberately not consulted: it buys defence <em>dice</em>, and there is no
    /// roll here to add them to. A first blow lands.</para>
    /// </summary>
    private static Wound? PickWound(PartyMember actor, NpcEntity npc, FightingSkill skill, System.Random rng)
    {
        var attacker = new Fighter(actor, 0, 0, isPlayerControlled: true, FighterFaction.Party);
        var defender = new Fighter(npc.Combatant, 0, 0, isPlayerControlled: false, FighterFaction.Enemy);

        string? location = skill.WoundTargetMode == WoundTargetMode.FixedBodyPart
            ? (skill.TargetBodyPartId is { } ids ? FightResolver.PreRollAmong(defender, ids, rng) : null)
            : FightResolver.PreRollHitLocation(defender, rng);

        return FightResolver.PickWound(attacker, defender, skill, location, rng);
    }

    protected override void Apply(OutcomeContext ctx)
    {
        if (_target is not NpcEntity npc) return;

        if (_wound != null)
            FightResolver.ApplyWound(
                new Fighter(npc.Combatant, 0, 0, isPlayerControlled: false, FighterFaction.Enemy),
                _wound);

        // What the blow did beyond the wound — a trip knocks them down, a tear starts them bleeding.
        // The effect belongs to a Fighter, which does not exist until the fight is built, so it is
        // carried on the NPC and drained by FightModeAdapter as it wraps them. Nothing is carried
        // for a blow that was turned aside, and nothing for wildlife, which has no fight to carry it
        // into.
        if (!_withheld && _attack.Skill.SpecialEffects.Length > 0)
            npc.CarriedFightEffects.AddRange(_attack.Skill.SpecialEffects);
    }
}
