using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Butterfly Glade - A sunny spot where butterflies gather.
/// </summary>
public class ButterflyGladeNode : NarrationNode
{
    public override string NodeId => "butterfly_glade";
    public override string ContextDescription => "watching butterflies in the glade";
    public override string TransitionDescription => "enter the butterfly glade";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "wing", "flower", "nectar", "glade" };
    
    private static readonly string[] Moods = { "colorful", "dancing", "fluttering", "bright", "lively", "vibrant", "magical", "enchanting" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} butterfly glade";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"wandering in a {mood} butterfly glade";
    }
    
    public sealed class ButterflyWings : Item
    {
        public override string ItemId => "butterfly_glade_butterfly_wings";
        public override string DisplayName => "Butterfly Wings";
        public override string Description => "Colorful wings shed by butterflies";
        public override List<string> OutcomeKeywords => new() { "wing", "scale", "powder", "pattern" };
    }
    
    public sealed class Nectar : Item
    {
        public override string ItemId => "butterfly_glade_nectar";
        public override string DisplayName => "Flower Nectar";
        public override string Description => "Sweet nectar from glade flowers";
        public override List<string> OutcomeKeywords => new() { "nectar", "sweetness", "blossom" };
    }
}
