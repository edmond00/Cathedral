using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Bramble Run - A thorny corridor of blackberry brambles.
/// </summary>
public class BrambleRunNode : NarrationNode
{
    public override string NodeId => "bramble_run";
    public override string ContextDescription => "carefully navigating the brambles";
    public override string TransitionDescription => "push through the brambles";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "thorny", "berries", "black", "ripe", "tangled", "sharp", "vines", "purple", "juicy", "scratching" };
    
    private static readonly string[] Moods = { "thorny", "tangled", "scratching", "productive", "wild", "dense", "prickly", "guarded" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} bramble run";
    }
    
    public sealed class Blackberry : Item
    {
        public override string ItemId => "wild_blackberry";
        public override string DisplayName => "Wild Blackberry";
        public override string Description => "A cluster of ripe blackberries";
        public override List<string> OutcomeKeywords => new() { "black", "ripe", "juicy", "sweet", "purple", "cluster", "berry", "tart", "drupes", "staining" };
    }
    
    public sealed class BrambleThorn : Item
    {
        public override string ItemId => "bramble_run_thorn";
        public override string DisplayName => "Bramble Thorn";
        public override string Description => "A sharp curved thorn from a bramble cane";
        public override List<string> OutcomeKeywords => new() { "sharp", "curved", "pointed", "hooked", "dangerous", "needle", "spine", "piercing", "rigid", "defensive" };
    }
}
