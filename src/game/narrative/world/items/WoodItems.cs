using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class Log : WoodRawItem
{
    public override string ItemId      => "log";
    public override string DisplayName => "Log";
    public override string Description => "A heavy length of split log, bark still on one side";
    public override ItemSize Size => ItemSize.Large;
    public override float    Weight => 4.0f;
}

public sealed class Plank : WoodRawItem
{
    public override string ItemId      => "plank";
    public override string DisplayName => "Plank";
    public override string Description => "A rough-sawn plank of pale wood, splintered at the ends";
    public override ItemSize Size => ItemSize.Large;
    public override float    Weight => 2.0f;
}

public sealed class Twig : WoodRawItem
{
    public override string ItemId      => "twig";
    public override string DisplayName => "Twig";
    public override string Description => "A thin dry twig snapped from a deadfall";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.05f;
}

public sealed class BirchSap : WoodRawItem
{
    public override string ItemId      => "birch_sap";
    public override string DisplayName => "Birch Sap";
    public override string Description => "A small flask of clear birch sap, faintly sweet";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.3f;
}
