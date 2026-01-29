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
    
    public override List<string> NodeKeywords => new() { "massive", "horizontal", "bark", "insects", "holes", "decay", "moss", "trunk", "enormous", "fallen" };
    
    private static readonly string[] Moods = { "massive", "fallen", "decaying", "moss-covered", "enormous", "ancient", "collapsed", "weathered" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} fallen giant trunk";
    }
    
    public sealed class BeetleLarva : Item
    {
        public override string ItemId => "beetle_larva";
        public override string DisplayName => "Beetle Larva";
        public override string Description => "A fat white larva from beneath the bark";
        public override List<string> OutcomeKeywords => new() { "white", "fat", "segmented", "wriggling", "soft", "grub", "larva", "insect", "curved", "protein" };
    }
}
