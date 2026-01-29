using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Pine Cone Cluster - A collection of fallen pine cones.
/// </summary>
public class PineConeClusterNode : NarrationNode
{
    public override string NodeId => "pine_cone_cluster";
    public override string ContextDescription => "gathering pine cones";
    public override string TransitionDescription => "collect pine cones";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "scales", "woody", "brown", "seeds", "scattered", "resinous", "cones", "dry", "fallen", "scaly" };
    
    private static readonly string[] Moods = { "scattered", "abundant", "dry", "fallen", "plentiful", "numerous", "clustered", "collected" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} pine cone cluster";
    }
    
    public sealed class PineCone : Item
    {
        public override string ItemId => "pine_cone";
        public override string DisplayName => "Pine Cone";
        public override string Description => "A woody pine cone with open scales";
        public override List<string> OutcomeKeywords => new() { "woody", "brown", "scaly", "seeds", "resinous", "cone", "dry", "hard", "scales", "aromatic" };
    }
}
