using System.Linq;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc;

/// <summary>
/// Builds the short, grammatical <b>label</b> that stands in for a named NPC's proper name in
/// LLM prompts (both constrained enum choices and neutral text handed to a Modus Mentis rewrite).
///
/// The label is composed from the NPC's role clause (archetype + current location) and the
/// relationship the <b>acting/narrating</b> party member has with the NPC:
///   • Stranger  → name omitted; for a human, a person descriptor trails in parentheses, e.g.
///     "the reeve of the field (an old man)"; for a non-human just the role, e.g. "the wolf".
///   • Otherwise → first-person relation + name, e.g. "my friend Godric Reeve, the blacksmith of
///     the village" (the name already identifies the person, so no descriptor is added).
///
/// Only the warmth ladder (<see cref="AffinityLevel"/>) is consulted; the enemy flag and criminal
/// record are deliberately ignored. Deterministic — no RNG.
/// </summary>
public static class NpcLabelResolver
{
    public static string Resolve(NpcEntity npc, WorldContext? world, int locationId, PartyMember? actingMember)
    {
        // Affinity is keyed by the party member's display name ("Protagonist" for the protagonist),
        // matching MeetStrangerVerb / StrengthenRelationshipVerb / DialogueTreeController.
        string key = actingMember?.AffinityKey ?? "Protagonist";

        string locationNoun = world?.DisplayName.ToLowerInvariant() ?? "";
        string roleClause   = npc.Archetype.BuildRoleClause(locationNoun);

        var affinity = npc.AffinityTable;
        // A beast never introduces itself, so it is never known by name — however it has been
        // appeased — until it joins the party, where it stops being a scene NPC at all.
        if (npc.Combatant.AnatomyType != AnatomyType.Human)
            return roleClause;
        if (affinity.IsStranger(key) || affinity.GetLevel(key) == AffinityLevel.Stranger)
        {
            // Stranger: no name yet. For a human, the role clause leads and a person descriptor
            // built from age and gender trails in parentheses ("the reeve of the field (an old
            // man)"); non-humans keep the bare role clause.
            return npc.Combatant.AnatomyType == AnatomyType.Human
                ? $"{roleClause} ({DescribePerson(npc)})"
                : roleClause;
        }

        // Known NPC: the relation + proper name already identifies the person. The proper name is
        // swapped for its scene false name so the LLM (and sanitizer) only see a simple placeholder;
        // NameFaking restores the real name in the generated output.
        return $"{affinity.GetLevel(key).ToFirstPersonRelation(NameFaking.Fake(npc.DisplayName))}, {roleClause}";
    }

    /// <summary>
    /// The short name a player-facing sentence uses for <paramref name="npc"/>: its proper name once
    /// <paramref name="actor"/> (the protagonist when null) has been introduced to a person, and its
    /// role until then — "the reeve", "the hyena". Shallow wildlife has only its kind ("the pig"), and a
    /// beast is always its kind, as in <see cref="Resolve"/>.
    ///
    /// <para>Every sentence the player reads about an NPC goes through here or through
    /// <see cref="Resolve"/> — refusals, chips, segment captions, menu titles. A proper name read
    /// before the introduction is a leak: the introduction is how a name is learned, and a hyena has
    /// none to give.</para>
    /// </summary>
    public static string ShortLabel(INpcEntity npc, PartyMember? actor = null)
    {
        if (npc is not NpcEntity named)
            return $"the {npc.DisplayName.Trim().ToLowerInvariant()}";
        if (named.Combatant.AnatomyType != AnatomyType.Human)
            return named.Archetype.BuildRoleClause("");

        var affinity = named.AffinityTable;
        string key = actor?.AffinityKey ?? "Protagonist";
        return affinity.IsStranger(key) || affinity.GetLevel(key) == AffinityLevel.Stranger
            ? named.Archetype.BuildRoleClause("")
            : named.DisplayName;
    }

    /// <summary><see cref="ShortLabel"/> opening a sentence: "The reeve is there".</summary>
    public static string ShortLabelCapitalized(INpcEntity npc, PartyMember? actor = null)
    {
        var label = ShortLabel(npc, actor);
        return label.Length == 0 ? label : char.ToUpperInvariant(label[0]) + label[1..];
    }

    /// <summary>
    /// Builds a short person descriptor for a human NPC from its age and gender — e.g. "a young boy",
    /// "a young girl", "a young man", "a woman", "an old man", "an old woman". The leading article is
    /// chosen for the following word ("an old …", "a young …"). Age bands: child (&lt;13 yrs),
    /// youth (&lt;18), adult (&lt;50), elder (≥50); gender comes from the "gender" derived stat
    /// (male &gt; 0). Intended for human NPCs — non-humans are described by their role clause instead.
    /// </summary>
    public static string DescribePerson(NpcEntity npc)
    {
        double years = npc.Combatant.GetAgeDays() / LifetimeStat.DaysPerYear;
        bool male = GenderIsMale(npc.Combatant);

        // (adjective, noun) by age band — the noun becomes gendered boy/girl for children, man/woman otherwise.
        string adjective, noun;
        if (years < 13)      { adjective = "young"; noun = male ? "boy"  : "girl";  }
        else if (years < 18) { adjective = "young"; noun = male ? "man"  : "woman"; }
        else if (years < 50) { adjective = "";      noun = male ? "man"  : "woman"; }
        else                 { adjective = "old";   noun = male ? "man"  : "woman"; }

        string phrase  = string.IsNullOrEmpty(adjective) ? noun : $"{adjective} {noun}";
        string article = StartsWithVowel(phrase) ? "an" : "a";
        return $"{article} {phrase}";
    }

    /// <summary>Reads the NPC's "gender" derived stat (male when &gt; 0); defaults to male if absent.</summary>
    public static bool GenderIsMale(PartyMember member)
    {
        var stat = member.DerivedStats.FirstOrDefault(s => s.Name == "gender");
        return stat == null || stat.GetValue(member) > 0;
    }

    private static bool StartsWithVowel(string s)
        => s.Length > 0 && "aeiou".IndexOf(char.ToLowerInvariant(s[0])) >= 0;
}
