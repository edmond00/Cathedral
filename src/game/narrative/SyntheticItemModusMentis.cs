using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative;

/// <summary>
/// A lightweight, non-LLM ModusMentis that represents an item used in an action combination.
/// Its DisplayName and Level map to the item's display name and usage level respectively,
/// allowing the item to appear as the chain leaf in the modusMentis chain
/// (observation → thinking → action → item).
///
/// This is only used for display and chain-level accounting — execution still uses the
/// real action ModusMentis looked up by ActionModusMentisId.
/// </summary>
internal sealed class SyntheticItemModusMentis : ModusMentis
{
    private readonly string _itemId;
    private readonly string _displayName;

    public SyntheticItemModusMentis(string itemId, string displayName, int usageLevel)
    {
        _itemId      = itemId;
        _displayName = displayName;
        Level        = usageLevel;
    }

    public override string ModusMentisId   => $"item:{_itemId}";
    public override string DisplayName     => _displayName;
    public override string ShortDescription => _displayName;
    public override ModusMentisFunction[] Functions => System.Array.Empty<ModusMentisFunction>();
    public override string[] Organs        => System.Array.Empty<string>();
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
}
