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
/// <para>An enemy is never a <i>witness</i>: they are a threat, and the threat rules cover them.
/// <see cref="Current"/> puts the two in one answer, the enemy first, for the readers that have to
/// tell them apart (the exit button); <see cref="Confronter"/> is the witness alone. The
/// confrontation with a drawn-in witness ends when the conversation with them ends: they are
/// released back to their day unless it made them an enemy.</para>
/// </summary>
public static class ConfrontationSelector
{
    /// <summary>
    /// The confrontation the actor is in, if any: an enemy in sight first, else a witness with a
    /// reason to stop them. What the exit button and the rules of action read.
    /// </summary>
    public static Confrontation? Current(Scene scene, PoV pov, Protagonist? protagonist)
    {
        if (protagonist != null)
        {
            var threat = ThreatSelector.ComputeContext(scene, pov, protagonist);
            if (threat.Level == ThreatLevel.Visual && threat.Threat != null)
                return new Confrontation(ConfrontationKind.Enemy, threat.Threat);
        }
        return Witness(scene, pov, protagonist?.AffinityKey);
    }

    public static NpcEntity? Confronter(Scene scene, PoV pov, Protagonist? protagonist)
        => Confronter(scene, pov, protagonist?.AffinityKey);

    /// <summary>As above, for whichever body's affinity key is asking (a companion narrating).</summary>
    public static NpcEntity? Confronter(Scene scene, PoV pov, string? key)
        => Witness(scene, pov, key)?.Npc;

    /// <summary>The witness confronting the actor, ignoring enemies — the two reasons below, in order.</summary>
    public static Confrontation? Witness(Scene scene, PoV pov, string? key)
    {
        // They came for you.
        foreach (var sceneNpc in scene.GetNpcsAt(pov.Where, pov.When))
        {
            if (!sceneNpc.IsAlive || sceneNpc.Entity is not NpcEntity npc || !npc.CanSpeak) continue;
            if (!scene.DisplacedNpcs.ContainsKey(sceneNpc.Id)) continue;
            if (key != null && npc.AffinityTable.IsEnemy(key)) continue;
            return new Confrontation(scene.FoundWaitingBy.Contains(sceneNpc.Id)
                ? ConfrontationKind.FoundWaiting
                : ConfrontationKind.CameForYou, npc);
        }

        // You have no business here — unless the householder said you had.
        if (PrivacyModel.IsTrespassing(scene, pov.Where))
        {
            var witness = WitnessSelector.ComputeContext(scene, pov);
            if (witness.Type == WitnessType.Visual && witness.Witness != null
                && (key == null || !witness.Witness.AffinityTable.IsEnemy(key)))
                return new Confrontation(ConfrontationKind.NoBusinessHere, witness.Witness);
        }

        return null;
    }

    /// <summary>
    /// The conversation a witness confronting you opens — whichever way it opens: a failed act in
    /// front of them, a failed runaway, or the accused coming forward (<paramref name="cameForward"/>,
    /// <c>explain_myself</c>). The one place a confrontation is turned into a tree.
    /// </summary>
    public static Dialogue.Tree.DialogueTree TreeFor(
        Scene? scene, NpcEntity witness, Verbs.Verb? verb, bool areaIsPrivate, bool cameForward = false)
    {
        // The householder who found you waiting asks about the waiting — unless what you did in front
        // of them was a crime of its own, which is the crime they name.
        if (scene != null && IsFoundWaitingBy(scene, witness)
            && (verb == null || Dialogue.Affinity.CrimeKinds.Of(verb, areaIsPrivate)
                    is Dialogue.Affinity.CriminalAffinityType.None or Dialogue.Affinity.CriminalAffinityType.Intruder))
            return new Dialogue.Tree.Trees.FoundWaitingTree();

        var crime = scene != null
            ? CrimeFor(scene, witness, verb, areaIsPrivate)
            : verb != null ? Dialogue.Affinity.CrimeKinds.Of(verb, areaIsPrivate) : Dialogue.Affinity.CriminalAffinityType.Intruder;
        return cameForward
            ? Dialogue.Tree.Trees.CaughtRedHandedTreeFactory.CreateCameForward(crime)
            : Dialogue.Tree.Trees.CaughtRedHandedTreeFactory.Create(crime);
    }

    /// <summary>Whether <paramref name="npc"/> is a householder who came and found the player waiting.</summary>
    public static bool IsFoundWaitingBy(Scene scene, NpcEntity npc)
    {
        var sceneNpc = scene.Npcs.FirstOrDefault(n => ReferenceEquals(n.Entity, npc));
        return sceneNpc != null && scene.FoundWaitingBy.Contains(sceneNpc.Id);
    }

    /// <summary>
    /// Whoever keeps the building <paramref name="area"/> is in: the owner of highest
    /// <see cref="NpcEntity.AuthorityLevel"/> (<see cref="NpcEntity.OwnedSectionIds"/>), else the
    /// resident of highest authority among those who sleep there — the same deference <see cref="WitnessSelector"/> shows. Only a living person
    /// who can speak and is not the player's enemy is a householder to answer to: an enemy at the door
    /// is the threat rules' business. Null when the building houses nobody.
    /// </summary>
    public static SceneNpc? Householder(Scene scene, Area area, string? key)
    {
        if (scene.SectionOf(area) is not { } section) return null;

        var people = scene.Npcs
            .Where(n => n.IsAlive && n.Entity is NpcEntity npc && npc.CanSpeak
                     && (key == null || !npc.AffinityTable.IsEnemy(key)))
            .ToList();

        // A village house is owned by everybody who lives in it, the apprentice as much as the master,
        // so among owners too it is the one of highest authority who answers for the house.
        var sectionId = section.Id.ToString();
        var owners = people.Where(n => ((NpcEntity)n.Entity).OwnedSectionIds.Contains(sectionId)).ToList();
        var keepers = owners.Count > 0
            ? owners
            : people.Where(n => scene.GetAreaOf(n, TimePeriod.Night) is { } bed && section.Areas.Contains(bed)).ToList();

        return keepers.OrderByDescending(n => ((NpcEntity)n.Entity).AuthorityLevel).FirstOrDefault();
    }

    /// <summary>
    /// Night has come round on the player waiting where they are trespassing: the householder comes
    /// to the room and finds them (<see cref="ConfrontationKind.FoundWaiting"/>). Roused, so the room's
    /// bed does not put them to sleep where they stand, and announced as an arrival, so the next
    /// observation opens on them. Returns who came, or null when the house has nobody to come.
    /// </summary>
    public static SceneNpc? SummonHouseholder(Scene scene, PoV pov, string? key)
    {
        if (!PrivacyModel.IsTrespassing(scene, pov.Where)) return null;
        if (Householder(scene, pov.Where, key) is not { } householder) return null;

        householder.Roused = true;
        scene.FoundWaitingBy.Add(householder.Id);
        scene.DrawNpcTo(householder, pov.Where);
        System.Console.WriteLine($"ConfrontationSelector: '{householder.Entity.DisplayName}' comes to bed and finds the player in {pov.Where.DisplayName}.");
        return householder;
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
        scene.FoundWaitingBy.Remove(sceneNpc.Id);
        System.Console.WriteLine($"ConfrontationSelector: '{npc.DisplayName}' goes back to their day.");
    }
}
