namespace Cathedral.Game.Narrative.World.Items;

public sealed class Clay : StoneRawItem
{
    public override string ItemId      => "clay";
    public override string DisplayName => "Clay";
    public override string Description => "A wet lump of grey-brown clay, cool and dense in the hand";
    public override float Weight => 0.6f;
}

public sealed class Lichen : StoneRawItem
{
    public override string ItemId      => "lichen";
    public override string DisplayName => "Lichen";
    public override string Description => "A papery crust of grey-green lichen prised from rock";
    public override float Weight => 0.05f;
}
