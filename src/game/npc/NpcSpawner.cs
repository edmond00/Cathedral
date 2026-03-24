using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc;

/// <summary>
/// Spawns NPCs at narration nodes based on their <see cref="NarrationNode.PossibleEncounters"/>.
/// Persistent NPCs are cached and reused across visits; transient NPCs are re-rolled each time.
/// Also wires <see cref="FightOutcome"/> and <see cref="DialogueOutcome"/> into the node's PossibleOutcomes.
/// </summary>
public class NpcSpawner
{
    /// <summary>Cache of persistent NPCs keyed by NpcId. Survives across node visits.</summary>
    private readonly Dictionary<string, NpcEntity> _persistentNpcs = new();

    /// <summary>
    /// Spawns (or restores) NPCs for the given node and injects their outcomes.
    /// Call this at the start of each observation phase.
    /// </summary>
    /// <param name="node">The narration node to populate.</param>
    /// <param name="locationId">Location ID used as part of the RNG seed for determinism.</param>
    public void PopulateNode(NarrationNode node, int locationId)
    {
        // Clear transient NPCs from previous visits; keep persistent ones
        node.SpawnedNpcs.RemoveAll(npc => !npc.IsPersistent);

        // Remove stale outcomes from previous NPC spawns
        node.PossibleOutcomes.RemoveAll(o => o is FightOutcome || o is DialogueOutcome);

        var encounters = node.PossibleEncounters;
        if (encounters.Count == 0)
        {
            ReAddOutcomesForExistingNpcs(node);
            return;
        }

        // Seed combines location + node for per-node determinism within a location
        var rng = new Random(HashCode.Combine(locationId, node.NodeId));

        foreach (var slot in encounters)
        {
            for (int i = 0; i < slot.MaxCount; i++)
            {
                if (rng.NextDouble() > slot.SpawnChance)
                    continue;

                var npc = SpawnOrRestore(slot, rng);
                if (npc == null || !npc.IsAlive)
                    continue;

                // Avoid duplicate additions (persistent NPC already in list)
                if (!node.SpawnedNpcs.Any(n => n.NpcId == npc.NpcId))
                    node.SpawnedNpcs.Add(npc);
            }
        }

        ReAddOutcomesForExistingNpcs(node);
    }

    private NpcEntity? SpawnOrRestore(NpcEncounterSlot slot, Random rng)
    {
        if (slot.Archetype.DefaultPersistent)
        {
            // Generate a deterministic candidate ID to check cache
            var candidateName = slot.Archetype.NamePool[rng.Next(slot.Archetype.NamePool.Length)];
            var candidateId = $"{slot.Archetype.ArchetypeId}_{candidateName.ToLowerInvariant().Replace(' ', '_')}";

            if (_persistentNpcs.TryGetValue(candidateId, out var cached))
                return cached;

            var npc = slot.Archetype.Spawn(rng);
            _persistentNpcs[npc.NpcId] = npc;
            return npc;
        }

        return slot.Archetype.Spawn(rng);
    }

    /// <summary>
    /// Wires FightOutcome / DialogueOutcome into the node's PossibleOutcomes
    /// for all currently spawned NPCs.
    /// </summary>
    private static void ReAddOutcomesForExistingNpcs(NarrationNode node)
    {
        foreach (var npc in node.SpawnedNpcs)
        {
            if (!npc.IsAlive) continue;

            // Dialogue-capable NPCs get a DialogueOutcome
            if (npc.CanDialogue)
                node.PossibleOutcomes.Add(new DialogueOutcome(npc));

            // All NPCs can be fought (hostile ones are attack targets; peaceful ones can be aggressed)
            node.PossibleOutcomes.Add(new FightOutcome(npc));
        }
    }

    /// <summary>
    /// Removes a dead NPC from the persistent cache and from the node.
    /// Call after a fight where the NPC dies.
    /// </summary>
    public void RemoveNpc(NpcEntity npc, NarrationNode node)
    {
        _persistentNpcs.Remove(npc.NpcId);
        node.SpawnedNpcs.Remove(npc);
    }
}
