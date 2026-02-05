using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class ValleyLowerFloorNode : PyramidalFeatureNode
{
    public override int MinAltitude => 8;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(ValleyUpperFloorNode);
    
    public override string NodeId => "valley_lower_floor";
    public override string ContextDescription => "on the wide valley lower floor";
    public override string TransitionDescription => "descend to the valley lower floor";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "valley", "floor", "lower", "flat", "fertile", "lush", "green", "peaceful", "sheltered", "protected" };
    
    private static readonly string[] Moods = { "fertile", "lush", "peaceful", "sheltered" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} valley lower floor";
    }
    
    public sealed class ValleySoil : Item
    {
        public override string ItemId => "valley_soil";
        public override string DisplayName => "Rich Soil";
        public override string Description => "Dark fertile earth in the valley";
        public override List<string> OutcomeKeywords => new() { "rich", "soil", "dark", "fertile", "earth", "loamy", "deep", "productive", "healthy", "nourishing" };
    }
    
    public sealed class StreamMeander : Item
    {
        public override string ItemId => "valley_lower_floor_stream_meander";
        public override string DisplayName => "Stream Meander";
        public override string Description => "Winding water course through the valley";
        public override List<string> OutcomeKeywords => new() { "stream", "meander", "winding", "water", "course", "serpentine", "flowing", "gentle", "curves", "peaceful" };
    }
}
