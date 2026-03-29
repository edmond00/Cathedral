using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class ChannelBedNode : PyramidalFeatureNode
{
    public override int MinAltitude => 8;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(ChannelBankNode);
    
    public override string NodeId => "channel_bed";
    public override string ContextDescription => "in the floodplain channel bed";
    public override string TransitionDescription => "descend to the channel bed";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the worn stone walls of the water <channel> around you"), KeywordInContext.Parse("the smooth gravel of the dry river <bed> underfoot"), KeywordInContext.Parse("the flat open <floodplain> stretching away on either side"), KeywordInContext.Parse("a bar of pale <sand> deposited at the channel bend") };
    
    private static readonly string[] Moods = { "shallow", "gentle", "flowing", "sandy" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} channel bed";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"in a {mood} channel bed";
    }
    
    public override List<Item> GetItems() => new() { new SandBar(), new SiltStone() };

    public sealed class SandBar : Item
    {
        public override string ItemId => "channel_bed_sand_bar";
        public override string DisplayName => "Sand Bar";
        public override string Description => "Exposed sand deposit in the channel";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a low exposed <sandbar> rising from the channel floor"), KeywordInContext.Parse("a pale <deposit> of sand left by the last flood"), KeywordInContext.Parse("a dry sand <island> stranded between the flow lines") };
    }
    
    public sealed class SiltStone : Item
    {
        public override string ItemId => "channel_bed_silt_stone";
        public override string DisplayName => "Silt Stone";
        public override string Description => "Fine sedimentary rock collectible from the channel bed";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a flat piece of fine-grained <siltstone> from the channel"), KeywordInContext.Parse("the layered <sediment> visible in the cut bank beside the bed"), KeywordInContext.Parse("a dark <mineral> streak running through the exposed stone") };
    }
}
