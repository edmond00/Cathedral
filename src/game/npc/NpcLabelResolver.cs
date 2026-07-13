using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc;

/// <summary>
/// Builds the short, grammatical <b>label</b> that stands in for a named NPC's proper name in
/// LLM prompts (both constrained enum choices and neutral text handed to a Modus Mentis rewrite).
///
/// The label is composed from the NPC's role clause (archetype + current location) and the
/// relationship the <b>acting/narrating</b> party member has with the NPC:
///   • Stranger  → name omitted, e.g. "the blacksmith of the village".
///   • Otherwise → first-person relation + name, e.g. "my friend Godric Reeve, the blacksmith of
///     the village".
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
            return roleClause;

        return $"{affinity.GetLevel(key).ToFirstPersonRelation(npc.DisplayName)}, {roleClause}";
    }
}
