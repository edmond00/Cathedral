using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Fern Glade - A sunlit clearing filled with ferns.
/// </summary>
public class FernGladeNode : NarrationNode
{
    public override string NodeId => "fern_glade";
    public override string ContextDescription => "walking through the fern glade";
    public override string TransitionDescription => "enter the ferns";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "frond", "spore", "fern", "glade" };
    
    private static readonly string[] Moods = { "lush", "green", "feathery", "dense", "verdant", "delicate", "thriving", "prehistoric" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} fern glade";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"walking through a {mood} fern glade";
    }
    
    public sealed class FernFrond : Item
    {
        public override string ItemId => "fern_frond";
        public override string DisplayName => "Fern Frond";
        public override string Description => "A fresh fern frond with delicate leaflets";
        public override List<string> OutcomeKeywords => new() { "pinnule", "leaflet", "rachis" };
    }
    
    public sealed class GladeFernSpore : Item
    {
        public override string ItemId => "fern_glade_spore_patch";
        public override string DisplayName => "Spore Patch";
        public override string Description => "Undersides of mature fronds covered in brown spores";
        public override List<string> OutcomeKeywords => new() { "spore", "capsule", "reproduction" };
    }
}
