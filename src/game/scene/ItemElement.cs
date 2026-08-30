using System;
using System.Collections.Generic;

namespace Cathedral.Game.Scene;

/// <summary>
/// Wraps a <see cref="Narrative.Item"/> as a scene <see cref="Element"/>,
/// allowing items to participate in the Element/Scene system (UUID registration,
/// PoV focus, verb targeting) while keeping the existing Item data model.
/// </summary>
public class ItemElement : Element
{
    /// <summary>The wrapped narrative item.</summary>
    public Narrative.Item Item { get; }

    /// <summary>
    /// Stable per-slot identity for depletion/regeneration, assigned at scene build
    /// (deterministic across rebuilds). Empty until assigned. See <c>SceneFactory.AssignDepletionKeys</c>.
    /// </summary>
    public string DepletionKey { get; set; } = "";

    public override string DisplayName => Item.DisplayName;

    public override List<string> Descriptions => new() { Item.Description };

    public ItemElement(Narrative.Item item)
    {
        Item = item;
    }
}
