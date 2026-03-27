using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Beetle Swarm - A congregation of beetles on rotting wood.
/// </summary>
public class BeetleSwarmNode : NarrationNode
{
    public override string NodeId => "beetle_swarm";
    public override string ContextDescription => "observing the beetle swarm";
    public override string TransitionDescription => "approach the beetles";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a shiny black <carapace> catching the light"), KeywordInContext.Parse("those swarming <beetles> covering the bark"), KeywordInContext.Parse("a hard <chitin> shell on every body"), KeywordInContext.Parse("this heaving <mass> of insects in motion") };
    
    private static readonly string[] Moods = { "swarming", "clustered", "busy", "shiny", "teeming", "crowded", "active", "abundant" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} beetle swarm";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"observing a {mood} beetle swarm";
    }
    
    public sealed class BeetleCarapace : Item
    {
        public override string ItemId => "beetle_swarm_beetle_carapace";
        public override string DisplayName => "Beetle Carapace";
        public override string Description => "Hard black shell from a dead beetle";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a hard <elytron> from the wing case"), KeywordInContext.Parse("the rigid <exoskeleton> of a dead beetle"), KeywordInContext.Parse("a brittle <chitin> shell crumbling in the fingers") };
    }
    
    public sealed class BeetleFrass : Item
    {
        public override string ItemId => "beetle_swarm_beetle_frass";
        public override string DisplayName => "Beetle Frass";
        public override string Description => "Wood powder from beetle boring activity";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some pale <castings> from beetle boring activity"), KeywordInContext.Parse("a fine <sawdust> of chewed wood fibers") };
    }
}
