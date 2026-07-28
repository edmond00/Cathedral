using System;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Generation;

/// <summary>
/// Grants one modus mentis to an NPC the way the game grants any other: a fresh instance at a rolled
/// level, filed into memory by the standard placement procedure. Shared by the archetype's own skill
/// sampling and by <see cref="Traits.PersonalityTrait"/>, so a trait-granted skill is indistinguishable
/// from a native one.
/// </summary>
public static class NpcSkillGrant
{
    /// <summary>Lowest level a generated skill can start at.</summary>
    public const int MinLevel = 1;

    /// <summary>Highest level a generated skill can start at, before the organ-derived cap.</summary>
    public const int MaxLevel = 3;

    /// <summary>
    /// Adds the modus mentis with id <paramref name="modusMentisId"/> to <paramref name="member"/>,
    /// or does nothing if it is already held (traits overlap; a duplicate skill is not a second skill).
    /// Returns the granted instance, or null when the id is unknown or already present.
    /// </summary>
    public static ModusMentis? Grant(PartyMember member, string modusMentisId, Random rng)
    {
        var template = ModusMentisRegistry.Instance.GetModusMentis(modusMentisId);
        if (template == null)
        {
            Console.Error.WriteLine($"NpcSkillGrant: unknown modus mentis '{modusMentisId}'.");
            return null;
        }

        if (member.ModiMentis.Any(m => m.ModusMentisId == modusMentisId)) return null;

        var instance = (ModusMentis)Activator.CreateInstance(template.GetType())!;
        instance.Level     = RollLevel(member, instance, rng);
        instance.CurrentXp = 0;

        // The reminescence placement procedure: typed long-term module, else working, else evict the
        // tail of working into residual. Registers the MM in ModiMentis as a side effect.
        member.AcquireModusMentis(instance);
        return instance;
    }

    /// <summary>
    /// A starting level of 1–3, clamped to what this body can actually sustain for this skill
    /// (<see cref="PartyMember.GetMaxLevelForModusMentis"/>, which reads the organ scores — so organs
    /// must already be rolled when this is called).
    /// </summary>
    public static int RollLevel(PartyMember member, ModusMentis modusMentis, Random rng)
    {
        int cap = member.GetMaxLevelForModusMentis(modusMentis);
        return Math.Clamp(rng.Next(MinLevel, MaxLevel + 1), 1, Math.Max(1, cap));
    }
}
