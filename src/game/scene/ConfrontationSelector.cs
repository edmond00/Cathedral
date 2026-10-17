using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene;

/// <summary>
/// Who, if anybody, is <b>confronting</b> the actor: a witness standing in their area with a reason
/// to stop them. The witness-side twin of a Visual enemy in <see cref="ThreatSelector"/>, and read
/// the same way — it closes the free exit (RUNAWAY), it blocks every act but a short list
/// (<c>ConfrontedImpossibleRule</c>, <see cref="Verbs.Verb.CanBeUsedWhenConfronted"/>), and any act
/// that fails in front of them gets you caught red-handed.
///
/// <para>Two reasons to stop somebody, and only these. <b>They came for you</b>: a witness drawn in
/// by a heard crime (<see cref="Scene.DisplacedNpcs"/>) is standing there because of what you did.
/// <b>You have no business here</b>: on private ground, anybody in sight is a witness to the trespass
/// that every act there is. A person merely standing in a public square is neither, and is nobody's
/// confrontation — otherwise every village square would freeze the moment anyone stood in it.</para>
///
/// <para>An enemy is never a confronter: they are a threat, and the threat rules already cover them.
/// The confrontation with a drawn-in witness ends when the conversation with them ends: they are
/// released back to their day unless it made them an enemy.</para>
/// </summary>
public static class ConfrontationSelector
{
    public static NpcEntity? Confronter(Scene scene, PoV pov, Protagonist? protagonist)
    {
        string? key = protagonist?.AffinityKey;

        // They came for you.
        foreach (var sceneNpc in scene.GetNpcsAt(pov.Where, pov.When))
        {
            if (!sceneNpc.IsAlive || sceneNpc.Entity is not NpcEntity npc || !npc.CanSpeak) continue;
            if (!scene.DisplacedNpcs.ContainsKey(sceneNpc.Id)) continue;
            if (key != null && npc.AffinityTable.IsEnemy(key)) continue;
            return npc;
        }

        // You have no business here.
        if (pov.Where.IsPrivate)
        {
            var witness = WitnessSelector.ComputeContext(scene, pov);
            if (witness.Type == WitnessType.Visual && witness.Witness != null
                && (key == null || !witness.Witness.AffinityTable.IsEnemy(key)))
                return witness.Witness;
        }

        return null;
    }

    /// <summary>
    /// The crime <paramref name="witness"/> accuses you of: what they heard if they came for it, else
    /// what <paramref name="verb"/> is, else trespass — the only thing a confronter who saw no crime
    /// can be confronting.
    /// </summary>
    public static Dialogue.Affinity.CriminalAffinityType CrimeFor(
        Scene scene, NpcEntity witness, Verbs.Verb? verb, bool areaIsPrivate)
    {
        var sceneNpc = scene.Npcs.FirstOrDefault(n => ReferenceEquals(n.Entity, witness));
        if (sceneNpc != null && scene.HeardCrimes.TryGetValue(sceneNpc.Id, out var heard)) return heard;

        var kind = verb != null ? Dialogue.Affinity.CrimeKinds.Of(verb, areaIsPrivate)
                                : Dialogue.Affinity.CriminalAffinityType.None;
        return kind != Dialogue.Affinity.CriminalAffinityType.None
            ? kind
            : Dialogue.Affinity.CriminalAffinityType.Intruder;
    }

    /// <summary>
    /// Ends a drawn-in witness's confrontation once the conversation with them is over: they go back
    /// to their day. An enemy stays where they stand — what the conversation turned into is a fight
    /// to be had, not a reason to walk off.
    /// </summary>
    public static void Release(Scene scene, NpcEntity npc, Protagonist protagonist)
    {
        var sceneNpc = scene.Npcs.FirstOrDefault(n => ReferenceEquals(n.Entity, npc));
        if (sceneNpc == null || !scene.DisplacedNpcs.ContainsKey(sceneNpc.Id)) return;
        if (npc.AffinityTable.IsEnemy(protagonist.AffinityKey)) return;

        scene.DisplacedNpcs.Remove(sceneNpc.Id);
        scene.HeardCrimes.Remove(sceneNpc.Id);
        System.Console.WriteLine($"ConfrontationSelector: '{npc.DisplayName}' goes back to their day.");
    }
}
