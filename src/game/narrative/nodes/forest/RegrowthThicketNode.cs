using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Regrowth Thicket - Young vegetation reclaiming disturbed ground.
/// Associated with: Wildwood
/// </summary>
public class RegrowthThicketNode : NarrationNode
{
    public override string NodeId => "regrowth_thicket";
    public override string ContextDescription => "pushing through regrowth";
    public override string TransitionDescription => "enter the regrowth";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "new", "vigorous", "young", "reclaiming", "dense", "shoots", "growth", "recovery", "pioneer", "fresh" };
    
    private static readonly string[] Moods = { "vigorous", "new", "thriving", "reclaiming", "dense", "fresh", "recovering", "pioneer" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} regrowth thicket";
    }
    
    public sealed class PioneerSeed : Item
    {
        public override string ItemId => "pioneer_seed";
        public override string DisplayName => "Pioneer Seeds";
        public override string Description => "Seeds from fast-growing pioneer plants";
        public override List<string> OutcomeKeywords => new() { "small", "numerous", "pioneer", "seeds", "dispersed", "wild", "hardy", "brown", "light", "colonizing" };
    }
    
    public sealed class FreshShoot : Item
    {
        public override string ItemId => "regrowth_thicket_fresh_shoot";
        public override string DisplayName => "Fresh Shoot";
        public override string Description => "A vigorous new shoot pushing through soil";
        public override List<string> OutcomeKeywords => new() { "green", "tender", "vigorous", "new", "young", "fresh", "growing", "shoot", "bright", "resilient" };
    }
}
