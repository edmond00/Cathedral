using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class CliffTopNode : PyramidalFeatureNode
{
    public override int MinAltitude => 2;
    public override int MaxAltitude => 6;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(CliffBaseNode);
    
    public override string NodeId => "cliff_top";
    public override string ContextDescription => "standing at the cliff top";
    public override string TransitionDescription => "ascend to the cliff top";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "precipice", "void", "abyss", "edge" };
    
    private static readonly string[] Moods = { "vertiginous", "dizzying", "precipitous", "commanding" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} cliff top";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing at a {mood} cliff top";
    }
    
    public sealed class CrackedRock : Item
    {
        public override string ItemId => "cliff_top_cracked_rock";
        public override string DisplayName => "Cracked Rock";
        public override string Description => "Fractured stone near the cliff edge";
        public override List<string> OutcomeKeywords => new() { "rock", "fracture", "fissure" };
    }
    
    public sealed class RaptorFeather : Item
    {
        public override string ItemId => "cliff_top_raptor_feather";
        public override string DisplayName => "Raptor Feather";
        public override string Description => "Large feather from a bird of prey";
        public override List<string> OutcomeKeywords => new() { "feather", "raptor", "hunter" };
    }
    
}
