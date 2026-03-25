using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Sapling Thicket - Dense young tree growth.
/// Associated with: MixedUnderwood
/// </summary>
public class SaplingThicketNode : NarrationNode
{
    public override string NodeId => "sapling_thicket";
    public override string ContextDescription => "pushing through sapling thicket";
    public override string TransitionDescription => "force through the saplings";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "sapling", "shoot", "competition", "trunk" };
    
    private static readonly string[] Moods = { "dense", "crowded", "competing", "vigorous", "tangled", "impenetrable", "young", "chaotic" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} sapling thicket";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"pushing through a {mood} sapling thicket";
    }
    
    public sealed class SaplingShoot : Item
    {
        public override string ItemId => "sapling_shoot";
        public override string DisplayName => "Sapling Shoot";
        public override string Description => "A straight young shoot from the thicket";
        public override List<string> OutcomeKeywords => new() { "shoot", "bark", "sapling" };
    }
    
    public sealed class SaplingBud : Item
    {
        public override string ItemId => "sapling_thicket_bud";
        public override string DisplayName => "Sapling Bud";
        public override string Description => "A swollen leaf bud ready to burst";
        public override List<string> OutcomeKeywords => new() { "bud", "spring", "potential" };
    }
}
