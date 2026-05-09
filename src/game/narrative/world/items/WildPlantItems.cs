using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class Nettle : Item
{
    public override string ItemId      => "nettle";
    public override string DisplayName => "Nettle";
    public override string Description => "A stinging nettle stem, leaves bristling with fine hairs";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.05f;
}

public sealed class Fern : Item
{
    public override string ItemId      => "fern";
    public override string DisplayName => "Fern";
    public override string Description => "A curled green fern frond, soft underside paler than its top";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.05f;
}

public sealed class Ivy : Item
{
    public override string ItemId      => "ivy";
    public override string DisplayName => "Ivy";
    public override string Description => "A trailing length of ivy, leaves leathery and dark";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.1f;
}

public sealed class Bramble : Item
{
    public override string ItemId      => "bramble";
    public override string DisplayName => "Bramble";
    public override string Description => "A thorny bramble cane, snagging on cloth";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.1f;
}

public sealed class Reed : Item
{
    public override string ItemId      => "reed";
    public override string DisplayName => "Reed";
    public override string Description => "A cluster of tall hollow reeds, papery at the edges";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.1f;
}

public sealed class Watercress : Item
{
    public override string ItemId      => "watercress";
    public override string DisplayName => "Watercress";
    public override string Description => "A wet handful of watercress, peppery-smelling";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.1f;
}
