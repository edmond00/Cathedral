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
    
    public override List<string> NodeKeywords => new() { "fallen", "collapsed", "massive", "decaying", "bark", "moss-covered", "horizontal", "trunk", "rotting", "insects" };
    
    private static readonly string[] Moods = { "decaying", "moss-covered", "ancient", "crumbling", "weathered", "decomposing", "rotting", "collapsed" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} fallen log line";
    }
    
    public sealed class RottenWood : Item
    {
        public override string ItemId => "rotten_log_wood";
        public override string DisplayName => "Rotten Wood";
        public override string Description => "Soft, decomposing wood from the fallen logs";
        public override List<string> OutcomeKeywords => new() { "soft", "crumbly", "dark", "moist", "decayed", "fibrous", "spongy", "musty", "wood", "decomposed" };
    }
}
