using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class BoarTusk : Item
{
    public override string ItemId      => "boar_tusk";
    public override string DisplayName => "Boar Tusk";
    public override string Description => "A curved yellow tusk pulled from a boar's jaw";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.2f;
}

public sealed class WolfPelt : Item
{
    public override string ItemId      => "wolf_pelt";
    public override string DisplayName => "Wolf Pelt";
    public override string Description => "The grey-furred pelt of a wolf, still fresh and strong-smelling";
    public override ItemSize Size => ItemSize.Large;
    public override float    Weight => 2.0f;
}

public sealed class DeerHide : Item
{
    public override string ItemId      => "deer_hide";
    public override string DisplayName => "Deer Hide";
    public override string Description => "A folded brown deer hide, soft-haired and pliable";
    public override ItemSize Size => ItemSize.Large;
    public override float    Weight => 1.8f;
}

public sealed class GoatHide : Item
{
    public override string ItemId      => "goat_hide";
    public override string DisplayName => "Goat Hide";
    public override string Description => "A coarse goat hide, hair pale and oily";
    public override ItemSize Size => ItemSize.Medium;
    public override float    Weight => 1.4f;
}

public sealed class LynxPelt : Item
{
    public override string ItemId      => "lynx_pelt";
    public override string DisplayName => "Lynx Pelt";
    public override string Description => "A spotted lynx pelt, prized and rare";
    public override ItemSize Size => ItemSize.Medium;
    public override float    Weight => 1.5f;
}

public sealed class SealPelt : Item
{
    public override string ItemId      => "seal_pelt";
    public override string DisplayName => "Seal Pelt";
    public override string Description => "A sleek dark seal pelt, oily and supple";
    public override ItemSize Size => ItemSize.Large;
    public override float    Weight => 2.5f;
}

public sealed class EagleFeather : Item
{
    public override string ItemId      => "eagle_feather";
    public override string DisplayName => "Eagle Feather";
    public override string Description => "A long brown-banded eagle feather";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.02f;
}

public sealed class Feather : Item
{
    public override string ItemId      => "feather";
    public override string DisplayName => "Feather";
    public override string Description => "A small bird feather, soft-vaned";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.01f;
}
