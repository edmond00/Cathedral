using System;
using System.Collections.Generic;

namespace Cathedral.Game.Scene;

/// <summary>
/// Abstract base for any element in a <see cref="Scene"/> that can be the focus of a PoV:
/// areas, spots, sections, NPCs, etc.
/// Each element has a unique ID, registers itself to the scene dictionary,
/// and carries observation data (descriptions) plus mutable state properties.
/// </summary>
public abstract class Element
{
    /// <summary>Unique identifier for this element, generated on construction.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Rebuild-independent identity, assigned by <c>SceneFactory.AssignStableKeys</c> in deterministic
    /// build order. <see cref="Id"/> is a fresh Guid per construction and scenes are rebuilt from
    /// scratch on every visit, so anything that must be "the same thing" twice — a procedural
    /// description seed, an audit comparison across rebuilds — keys off this instead.
    ///
    /// <para>A connector (door, stair, path) belongs to two areas' PoI lists and would be keyed twice
    /// by the walk, with the later write winning; those pre-assign their own key at construction and
    /// the walk leaves any already-keyed element alone.</para>
    /// </summary>
    public string StableKey { get; set; } = "";

    /// <summary>Human-readable display name for UI and logging.</summary>
    public abstract string DisplayName { get; }

    /// <summary>Description strings used when this element is observed.</summary>
    public abstract List<string> Descriptions { get; }

    /// <summary>
    /// Current active state values for this element.
    /// Override in subclasses that define their own state enum (e.g. DoorState.Locked).
    /// The base returns an empty list (stateless element).
    /// </summary>
    public virtual List<Enum> StateProperties { get; set; } = new();

    /// <summary>
    /// Registers this element in the scene's element dictionary.
    /// Called by the factory after construction.
    /// </summary>
    public void Register(Scene scene)
    {
        scene.RegisterElement(this);
    }
}
