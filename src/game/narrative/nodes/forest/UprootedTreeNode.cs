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
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a great fan of exposed <root> still clinging to torn earth"), KeywordInContext.Parse("the wide <crater> left where the tree once stood"), KeywordInContext.Parse("the raw smell of fresh <earth> turned up from below"), KeywordInContext.Parse("the total <upheaval> of soil and root and stone together") };
    
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
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a clump of dark crumbly <loam> clinging to the roots"), KeywordInContext.Parse("the rich smell of <humus> disturbed from deep in the earth"), KeywordInContext.Parse("a fine hair-like <radicle> still trailing from the root mass") };
    }
    
    public sealed class TornRoot : Item
    {
        public override string ItemId => "uprooted_tree_torn_root";
        public override string DisplayName => "Torn Root";
        public override string Description => "A thick root torn from the earth";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the thick shredded <taproot> pulled violently from the ground"), KeywordInContext.Parse("some pale splintered <wood> exposed at the break"), KeywordInContext.Parse("a length of tough root <fiber> still in one piece") };
    }
    
    public sealed class CraterMud : Item
    {
        public override string ItemId => "uprooted_tree_crater_mud";
        public override string DisplayName => "Crater Mud";
        public override string Description => "Wet clay-rich mud from the deep crater left by the uprooting";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the grey sticky <clay> exposed at the crater bottom"), KeywordInContext.Parse("a layer of pale <sediment> revealed beneath the topsoil"), KeywordInContext.Parse("the lighter colour of <subsoil> turned up by the fall") };
    }
}
