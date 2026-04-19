using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Base class for items that can be acquired through successful actions.
/// Items have specific names but should not include qualifiers (e.g., "Trout" not "Small Fish").
/// Implements IObservation as self-referential: an Item IS its own single observation.
/// </summary>
public abstract class Item : ConcreteOutcome, IObservation
{
    /// <summary>Unique identifier for this item.</summary>
    public abstract string ItemId { get; }

    /// <summary>Short description of the item for player reference.</summary>
    public abstract string Description { get; }

    // ── Physical properties ───────────────────────────────────────

    /// <summary>Weight in kilograms.</summary>
    public virtual float Weight => 0.1f;

    /// <summary>Physical size — determines inventory slot count (Small=3, Medium=5, Large=7).</summary>
    public virtual ItemSize Size => ItemSize.Small;

    /// <summary>Number of inventory slots this item occupies.</summary>
    public int SlotCount => (int)Size;

    /// <summary>Category tags for this item.</summary>
    public virtual List<ItemType> Types => new() { ItemType.Other };

    /// <summary>
    /// Lines of text shown in the inventory info panel.
    /// Override to provide item-specific details beyond the description.
    /// </summary>
    public virtual string[] Info => new[] { Description };

    /// <summary>
    /// Preferred equipment anchor. When null, the item auto-fills any free compatible slot
    /// or is placed inside an equipped container.
    /// </summary>
    public virtual EquipmentAnchor? PreferredAnchor => null;

    /// <summary>
    /// Usage level (1–10): adds bonus dice to the action roll when this item is combined
    /// with an action. Higher levels represent more specialised or potent tools.
    /// </summary>
    public virtual int UsageLevel => 1;

    public override string ToNaturalLanguageString() => $"acquire {DisplayName}";

    // ── IObservation (self-referential) ───────────────────────────────────────
    string IObservation.ObservationId => ItemId;
    IReadOnlyList<ConcreteOutcome> IObservation.ObservationOutcomes =>
        new System.Collections.ObjectModel.ReadOnlyCollection<ConcreteOutcome>(new List<ConcreteOutcome> { this });
}
