using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Fox Den - An abandoned or active fox burrow.
/// </summary>
public class FoxDenNode : NarrationNode
{
    public override string NodeId => "fox_den";
    public override string ContextDescription => "observing the fox den";
    public override string TransitionDescription => "investigate the den";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "burrow", "entrance", "musky", "earth", "tunnel", "hole", "scent", "tracks", "den", "underground" };
    
    private static readonly string[] Moods = { "musky", "hidden", "occupied", "abandoned", "earthy", "secretive", "dark", "sheltered" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} fox den";
    }
    
    public sealed class FoxFur : Item
    {
        public override string ItemId => "fox_fur_tuft";
        public override string DisplayName => "Fox Fur Tuft";
        public override string Description => "A tuft of reddish fox fur caught on roots";
        public override List<string> OutcomeKeywords => new() { "red", "soft", "fluffy", "fur", "tuft", "reddish", "guard", "hairs", "russet", "warm" };
    }
}
