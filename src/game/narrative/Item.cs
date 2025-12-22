using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Base class for items that can be acquired through successful actions.
/// Items have specific names but should not include qualifiers (e.g., "Trout" not "Small Fish").
/// </summary>
public abstract class Item : ConcreteOutcome
{
    /// <summary>
    /// Unique identifier for this item.
    /// </summary>
    public abstract string ItemId { get; }
    
    /// <summary>
    /// Description of the item for player reference.
    /// </summary>
    public abstract string Description { get; }
    
    public override string ToNaturalLanguageString()
    {
        return $"acquire {DisplayName}";
    }
}
