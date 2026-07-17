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
///   • Stranger  → name omitted; for a human, a person descriptor is prepended, e.g.
///     "an old man, the reeve of the field"; for a non-human just the role, e.g. "the wolf".
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
        string key = actingMember?.DisplayName ?? "Protagonist";

        string locationNoun = world?.DisplayName.ToLowerInvariant() ?? "";
        string roleClause   = npc.Archetype.BuildRoleClause(locationNoun);

        var affinity = npc.AffinityTable;
        if (affinity.IsStranger(key) || affinity.GetLevel(key) == AffinityLevel.Stranger)
        {
            // Stranger: no name yet. For a human, lead with a person descriptor built from age and
            // gender ("an old man, the reeve of the field"); non-humans keep the bare role clause.
            return npc.Combatant.AnatomyType == AnatomyType.Human
                ? $"{DescribePerson(npc)}, {roleClause}"
                : roleClause;
        }

        // Known NPC: the relation + proper name already identifies the person.
        return $"{affinity.GetLevel(key).ToFirstPersonRelation(npc.DisplayName)}, {roleClause}";
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
    private static bool GenderIsMale(PartyMember member)
    {
        var stat = member.DerivedStats.FirstOrDefault(s => s.Name == "gender");
        return stat == null || stat.GetValue(member) > 0;
    }

    private static bool StartsWithVowel(string s)
        => s.Length > 0 && "aeiou".IndexOf(char.ToLowerInvariant(s[0])) >= 0;
}
