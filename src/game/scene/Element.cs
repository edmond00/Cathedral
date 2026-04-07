using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene;

/// <summary>
/// Abstract base for any element in a <see cref="Scene"/> that can be the focus of a PoV:
/// areas, spots, sections, NPCs, etc.
/// Each element has a unique ID, registers itself to the scene dictionary,
/// and carries observation data (descriptions, keywords) plus mutable state properties.
/// </summary>
public abstract class Element
{
    /// <summary>Unique identifier for this element, generated on construction.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>Human-readable display name for UI and logging.</summary>
    public abstract string DisplayName { get; }

    /// <summary>Description strings used when this element is observed.</summary>
    public abstract List<string> Descriptions { get; }

    /// <summary>Keywords with context, used for LLM observation hints and UI keyword display.</summary>
    public abstract List<KeywordInContext> Keywords { get; }

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
