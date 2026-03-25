using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class SnowCorniceFallLineNode : PyramidalFeatureNode
{
    public override int MinAltitude => 0;
    public override int MaxAltitude => 2;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(SnowCorniceCrestNode);
    
    public override string NodeId => "snow_cornice_fall_line";
    public override string ContextDescription => "standing on the cornice fall line";
    public override string TransitionDescription => "move to the cornice fall line";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "fall", "avalanche", "descent", "slope" };
    
    private static readonly string[] Moods = { "steep", "treacherous", "angled", "exposed" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} cornice fall line";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing on a {mood} cornice fall line";
    }
    
    public sealed class CorniceDebris : Item
    {
        public override string ItemId => "cornice_debris";
        public override string DisplayName => "Cornice Debris";
        public override string Description => "Debris from cornice collapse";
        public override List<string> OutcomeKeywords => new() { "debris", "cornice", "snow" };
    }
    
}
