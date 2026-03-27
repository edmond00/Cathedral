using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Fallen Log Line - A transversal feature of collapsed trees forming a natural corridor.
/// </summary>
public class FallenLogLineNode : NarrationNode
{
    public override string NodeId => "fallen_log_line";
    public override string ContextDescription => "climbing along the fallen logs";
    public override string TransitionDescription => "follow the fallen logs";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a mossy <trunk> lying across the ground"), KeywordInContext.Parse("the soft green <moss> blanketing the logs"), KeywordInContext.Parse("the deep <decay> softening the old heartwood"), KeywordInContext.Parse("the loose grey <bark> peeling from the surface") };
    
    private static readonly string[] Moods = { "decaying", "moss-covered", "ancient", "crumbling", "weathered", "decomposing", "rotting", "collapsed" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} fallen log line";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"climbing along a {mood} fallen log line";
    }
    
    public sealed class RottenWood : Item
    {
        public override string ItemId => "rotten_log_wood";
        public override string DisplayName => "Rotten Wood";
        public override string Description => "Soft, decomposing wood from the fallen logs";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the soft <punky> wood crumbling easily"), KeywordInContext.Parse("some white <mycelium> threading through the rot") };
    }
    
    public sealed class BeetleLarvae : Item
    {
        public override string ItemId => "fallen_log_line_beetle_larvae";
        public override string DisplayName => "Beetle Larvae";
        public override string Description => "Pale larvae boring through the rotting wood";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a pale wriggling <larva> in the soft wood"), KeywordInContext.Parse("a fat pale <grub> boring through the heartwood") };
    }
}
