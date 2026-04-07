using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene;

/// <summary>
/// An NPC present in a <see cref="Scene"/>.
/// Wraps an <see cref="NpcEntity"/> with Element identity and scene registration.
/// </summary>
public class SceneNpc : Element
{
    public override string DisplayName => Entity.DisplayName;
    public override List<string> Descriptions { get; }
    public override List<KeywordInContext> Keywords { get; }

    /// <summary>The underlying NPC entity (anatomy, combat, dialogue).</summary>
    public NpcEntity Entity { get; }

    /// <summary>Whether this NPC is hostile by default.</summary>
    public bool IsHostile => Entity.IsHostile;

    /// <summary>Whether this NPC is still alive.</summary>
    public bool IsAlive => Entity.IsAlive;

    public SceneNpc(NpcEntity entity, List<KeywordInContext>? keywords = null, List<string>? descriptions = null)
    {
        Entity       = entity;
        Keywords     = keywords ?? new();
        Descriptions = descriptions ?? new() { entity.DisplayName };
    }
}
