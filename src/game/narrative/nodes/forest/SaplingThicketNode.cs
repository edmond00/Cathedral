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
    
    public override List<string> NodeKeywords => new() { "young", "thin", "trunks", "crowded", "competing", "dense", "shoots", "vigorous", "tangled", "growth" };
    
    private static readonly string[] Moods = { "dense", "crowded", "competing", "vigorous", "tangled", "impenetrable", "young", "chaotic" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} sapling thicket";
    }
    
    public sealed class SaplingShoot : Item
    {
        public override string ItemId => "sapling_shoot";
        public override string DisplayName => "Sapling Shoot";
        public override string Description => "A straight young shoot from the thicket";
        public override List<string> OutcomeKeywords => new() { "straight", "flexible", "green", "young", "smooth", "shoot", "slender", "thin", "bark", "vigorous" };
    }
    
    public sealed class SaplingBud : Item
    {
        public override string ItemId => "sapling_thicket_bud";
        public override string DisplayName => "Sapling Bud";
        public override string Description => "A swollen leaf bud ready to burst";
        public override List<string> OutcomeKeywords => new() { "swollen", "tight", "green", "bud", "emerging", "potential", "spring", "promise", "unopened", "ready" };
    }
}
