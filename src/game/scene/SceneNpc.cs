using System.Collections.Generic;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene;

/// <summary>
/// An NPC present in a <see cref="Scene"/>.
/// Wraps an <see cref="INpcEntity"/> with Element identity and scene registration.
/// Supports both named (<see cref="NpcEntity"/>) and shallow (<see cref="ShallowNpcEntity"/>) entities.
/// </summary>
public class SceneNpc : Element
{
    public override string DisplayName => Entity.DisplayName;
    public override List<string> Descriptions { get; }

    /// <summary>The underlying NPC entity (anatomy + dialogue for named; anonymous for shallow).</summary>
    public INpcEntity Entity { get; }

    /// <summary>Whether this NPC is still alive.</summary>
    public bool IsAlive => Entity.IsAlive;

    /// <summary>
    /// Set once a sleeper has been woken this visit, so they stay awake for the rest of it. Not
    /// persisted: a scene is rebuilt from scratch on every arrival, and somebody you woke last week
    /// is asleep again tonight.
    /// </summary>
    public bool Roused { get; set; }

    public SceneNpc(INpcEntity entity, List<string>? descriptions = null)
    {
        Entity       = entity;
        Descriptions = descriptions ?? new() { entity.DisplayName };
    }

    /// <summary>
    /// Whether this person is asleep right now: it is their sleeping period, they are in the area
    /// their schedule puts them in for it, that area holds a bed, and nobody has woken them.
    ///
    /// <para>Inferred rather than stored, because every part of it is already true of the world — the
    /// schedule puts people in their own rooms at night and the rooms have pallets in them. What was
    /// missing was anything asking the question, so a villager at midnight in their own bed offered
    /// the same conversation as one at noon behind their counter.</para>
    ///
    /// <para>Only named people sleep. A wolf is allowed to be at rest without being helpless, and the
    /// wilderness factories deliberately have creatures with nowhere to sleep at all.</para>
    /// </summary>
    public bool IsSleeping(Scene scene, PoV pov)
    {
        if (Roused || !IsAlive) return false;
        if (Entity is not NpcEntity npc) return false;
        if (npc.Archetype is not NamedNpcArchetype named) return false;
        if (pov.When != named.SleepPeriod) return false;

        var bedroom = scene.GetAreaOf(this, pov.When);
        if (bedroom == null || bedroom.Id != pov.Where.Id) return false;

        return Building.BuildingRooms.BedsIn(bedroom).Any();
    }
}
