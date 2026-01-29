using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Grass Clearing - A small grassy opening in the woodland.
/// Associated with: OpenWoodland
/// </summary>
public class GrassClearingNode : NarrationNode
{
    public override string NodeId => "grass_clearing";
    public override string ContextDescription => "standing in the grass clearing";
    public override string TransitionDescription => "step into the clearing";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "grass", "meadow", "open", "green", "swaying", "tall", "blades", "soft", "sunny", "breeze" };
    
    private static readonly string[] Moods = { "sun-drenched", "breezy", "peaceful", "swaying", "green", "open", "bright", "fresh" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} grass clearing";
    }
    
    public sealed class GrassSeed : Item
    {
        public override string ItemId => "grass_seed";
        public override string DisplayName => "Grass Seed";
        public override string Description => "Small seeds from wild grasses";
        public override List<string> OutcomeKeywords => new() { "small", "brown", "dry", "seeds", "grain", "chaff", "tiny", "scattered", "harvest", "wild" };
    }
}
