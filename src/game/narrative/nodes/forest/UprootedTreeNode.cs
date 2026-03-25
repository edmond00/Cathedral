using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Uprooted Tree - A recently toppled tree with exposed roots.
/// Associated with: Wildwood
/// </summary>
public class UprootedTreeNode : NarrationNode
{
    public override string NodeId => "uprooted_tree";
    public override string ContextDescription => "examining the uprooted tree";
    public override string TransitionDescription => "investigate the fallen tree";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "root", "crater", "earth", "upheaval" };
    
    private static readonly string[] Moods = { "toppled", "fallen", "disrupted", "torn", "recent", "exposed", "crashed", "uprooted" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} uprooted tree";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"examining a {mood} uprooted tree";
    }
    
    public sealed class RootBall : Item
    {
        public override string ItemId => "root_ball_soil";
        public override string DisplayName => "Root Ball Soil";
        public override string Description => "Fresh soil clinging to uprooted roots";
        public override List<string> OutcomeKeywords => new() { "mud", "soil", "roots" };
    }
    
    public sealed class TornRoot : Item
    {
        public override string ItemId => "uprooted_tree_torn_root";
        public override string DisplayName => "Torn Root";
        public override string Description => "A thick root torn from the earth";
        public override List<string> OutcomeKeywords => new() { "root", "wood", "fiber" };
    }
    
    public sealed class CraterMud : Item
    {
        public override string ItemId => "uprooted_tree_crater_mud";
        public override string DisplayName => "Crater Mud";
        public override string Description => "Wet clay-rich mud from the deep crater left by the uprooting";
        public override List<string> OutcomeKeywords => new() { "clay", "mud", "subsoil" };
    }
}
