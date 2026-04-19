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

    /// <summary>Whether this NPC is hostile by default.</summary>
    public bool IsHostile => Entity.IsHostile;

    /// <summary>Whether this NPC is still alive.</summary>
    public bool IsAlive => Entity.IsAlive;

    public SceneNpc(INpcEntity entity, List<string>? descriptions = null)
    {
        Entity       = entity;
        Descriptions = descriptions ?? new() { entity.DisplayName };
    }
}
