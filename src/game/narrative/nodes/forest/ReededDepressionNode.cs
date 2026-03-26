using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Reeded Depression - A wet hollow filled with reeds.
/// Associated with: Lowwood
/// </summary>
public class ReededDepressionNode : NarrationNode
{
    public override string NodeId => "reeded_depression";
    public override string ContextDescription => "wading through the reeded depression";
    public override string TransitionDescription => "descend into the reeds";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "reed", "cattail", "marsh", "water" };
    
    private static readonly string[] Moods = { "wet", "swaying", "marshy", "sodden", "reed-filled", "damp", "boggy", "hollow" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} reeded depression";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"wading through a {mood} reeded depression";
    }
    
    public sealed class ReedStem : Item
    {
        public override string ItemId => "reed_stem";
        public override string DisplayName => "Reed Stem";
        public override string Description => "A tall, hollow reed stem";
        public override List<string> OutcomeKeywords => new() { "culm", "hollow", "internode" };
    }
    
    public sealed class CattailFluff : Item
    {
        public override string ItemId => "reeded_depression_cattail_fluff";
        public override string DisplayName => "Cattail Fluff";
        public override string Description => "Soft brown seed fluff from a cattail head";
        public override List<string> OutcomeKeywords => new() { "down", "seed", "dispersal" };
    }
}
