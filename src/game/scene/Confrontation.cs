using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene;

/// <summary>Why somebody in the actor's area stands in their way.</summary>
public enum ConfrontationKind
{
    /// <summary>An enemy in sight: a threat, answered by a fight. See <see cref="ThreatSelector"/>.</summary>
    Enemy,

    /// <summary>A witness drawn in by a heard crime (<see cref="Scene.DisplacedNpcs"/>): they came for you.</summary>
    CameForYou,

    /// <summary>Anybody in sight on private ground: you have no business here.</summary>
    NoBusinessHere,

    /// <summary>
    /// The householder, come at nightfall to find the player waiting in their house
    /// (<see cref="Scene.FoundWaitingBy"/>). Opens <c>found_waiting</c> rather than caught-red-handed.
    /// </summary>
    FoundWaiting,
}

/// <summary>
/// Who is confronting the actor, and why — the one answer the exit button, the rules of action, the
/// failure ladder and the confrontation dialogue all read, so they cannot disagree about it.
///
/// <para>Computed by <see cref="ConfrontationSelector.Current"/>. An enemy outranks every witness, as
/// everywhere else: a fight in the offing is not a conversation. What each kind closes is the same —
/// the free exit becomes a runaway roll — and what it opens differs: an enemy a fight, a witness the
/// dialogue <see cref="ConfrontationSelector.TreeFor"/> names.</para>
/// </summary>
public sealed record Confrontation(ConfrontationKind Kind, NpcEntity Npc)
{
    /// <summary>A witness rather than an enemy: somebody to be talked round, not fought.</summary>
    public bool IsWitness => Kind != ConfrontationKind.Enemy;
}
