using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class Saw : ToolItem
{
    public override string ItemId      => "saw";
    public override string DisplayName => "Saw";
    public override string Description => "A two-handed saw with coarse iron teeth, the blade flexed and worn";
    public override float Weight => 1.2f;
}

public sealed class Axe : ToolItem
{
    public override string ItemId      => "axe";
    public override string DisplayName => "Axe";
    public override string Description => "A heavy felling axe with a broad iron head and ash haft";
    public override float Weight => 1.5f;
    public override int   UsageLevel => 5;
}

public sealed class Pick : ToolItem
{
    public override string ItemId      => "pick";
    public override string DisplayName => "Pick";
    public override string Description => "A miner's pick, one end tapered to a hard point, the other flattened";
    public override float Weight => 1.6f;
    public override int   UsageLevel => 5;
}

public sealed class Shovel : ToolItem
{
    public override string ItemId      => "shovel";
    public override string DisplayName => "Shovel";
    public override string Description => "A long-hafted shovel with a worn iron blade";
    public override float Weight => 1.3f;
}

public sealed class Hammer : ToolItem
{
    public override string ItemId      => "hammer";
    public override string DisplayName => "Hammer";
    public override string Description => "A blacksmith's hammer, head dulled and gleaming from years of strikes";
    public override float Weight => 1.0f;
}

public sealed class Tongs : ToolItem
{
    public override string ItemId      => "tongs";
    public override string DisplayName => "Tongs";
    public override string Description => "A long pair of iron tongs, the jaws blackened with scale";
    public override float Weight => 0.8f;
}

public sealed class Chisel : ToolItem
{
    public override string ItemId      => "chisel";
    public override string DisplayName => "Chisel";
    public override string Description => "A flat-bladed chisel with a wooden handle ringed in iron";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.3f;
}

public sealed class Mallet : ToolItem
{
    public override string ItemId      => "mallet";
    public override string DisplayName => "Mallet";
    public override string Description => "A wooden mallet, head squared off and dented from countless blows";
    public override float Weight => 0.9f;
}

public sealed class Shears : ToolItem
{
    public override string ItemId      => "shears";
    public override string DisplayName => "Shears";
    public override string Description => "A pair of broad shearing blades sprung together at the handle";
    public override float Weight => 0.4f;
}

public sealed class Rake : ToolItem
{
    public override string ItemId      => "rake";
    public override string DisplayName => "Rake";
    public override string Description => "A long-toothed wooden rake, the head bound with iron pegs";
    public override float Weight => 0.9f;
}

public sealed class Hoe : ToolItem
{
    public override string ItemId      => "hoe";
    public override string DisplayName => "Hoe";
    public override string Description => "A short hoe with a curved iron blade and a worn handle";
    public override float Weight => 0.8f;
}

public sealed class Scythe : ToolItem
{
    public override string ItemId      => "scythe";
    public override string DisplayName => "Scythe";
    public override string Description => "A long curving scythe blade fitted to a tall snath";
    public override ItemSize Size => ItemSize.Large;
    public override float    Weight => 1.4f;
    public override int      UsageLevel => 5;
}
