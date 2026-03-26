using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Fallen Giant Trunk - A massive collapsed tree trunk.
/// </summary>
public class FallenGiantTrunkNode : NarrationNode
{
    public override string NodeId => "fallen_giant_trunk";
    public override string ContextDescription => "climbing the fallen giant";
    public override string TransitionDescription => "climb onto the trunk";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "trunk", "bark", "decay", "insects" };
    
    private static readonly string[] Moods = { "massive", "fallen", "decaying", "moss-covered", "enormous", "ancient", "collapsed", "weathered" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} fallen giant trunk";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"climbing a {mood} fallen giant trunk";
    }
    
    public sealed class BeetleLarva : Item
    {
        public override string ItemId => "beetle_larva";
        public override string DisplayName => "Beetle Larva";
        public override string Description => "A fat white larva from beneath the bark";
        public override List<string> OutcomeKeywords => new() { "larva", "grub", "protein" };
    }
    
    public sealed class SoftRot : Item
    {
        public override string ItemId => "fallen_trunk_soft_rot";
        public override string DisplayName => "Soft Rot Wood";
        public override string Description => "Punky, decomposing wood from the trunk interior";
        public override List<string> OutcomeKeywords => new() { "decay", "fungus", "spore" };
    }
}
