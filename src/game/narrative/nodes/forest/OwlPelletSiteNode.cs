using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Owl Pellet Site - A collection of regurgitated owl pellets.
/// </summary>
public class OwlPelletSiteNode : NarrationNode
{
    public override string NodeId => "owl_pellet_site";
    public override string ContextDescription => "examining owl pellets";
    public override string TransitionDescription => "investigate the pellets";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "grey", "compressed", "bones", "fur", "pellets", "regurgitated", "dry", "cylindrical", "raptor", "remains" };
    
    private static readonly string[] Moods = { "dry", "scattered", "compressed", "aged", "numerous", "grey", "informative", "preserved" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} owl pellet site";
    }
    
    public sealed class TinyBones : Item
    {
        public override string ItemId => "pellet_tiny_bones";
        public override string DisplayName => "Tiny Bones";
        public override string Description => "Small rodent bones from dissected owl pellets";
        public override List<string> OutcomeKeywords => new() { "white", "small", "fragile", "skull", "vertebrae", "bones", "delicate", "cleaned", "skeletal", "rodent" };
    }
    
    public sealed class MatPellet : Item
    {
        public override string ItemId => "owl_pellet_mat_pellet";
        public override string DisplayName => "Matted Pellet";
        public override string Description => "A compressed pellet of fur and bone";
        public override List<string> OutcomeKeywords => new() { "grey", "compressed", "cylindrical", "matted", "fur", "hard", "regurgitated", "dry", "compact", "dense" };
    }
}
