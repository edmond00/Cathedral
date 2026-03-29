using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Drainage Hollow - A transversal feature of a natural water channel.
/// </summary>
public class DrainageHollowNode : NarrationNode
{
    public override string NodeId => "drainage_hollow";
    public override string ContextDescription => "descending through the drainage hollow";
    public override string TransitionDescription => "follow the drainage";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a sunken <hollow> cut by seasonal water"), KeywordInContext.Parse("the narrow <channel> carved into soft earth"), KeywordInContext.Parse("the thick clinging <mud> along the sides") };
    
    private static readonly string[] Moods = { "damp", "muddy", "slippery", "wet", "eroded", "carved", "soggy", "moist" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} drainage hollow";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"descending through a {mood} drainage hollow";
    }
    
    public override List<Item> GetItems() => new() { new ClayDeposit(), new StagnantWater() };

    public sealed class ClayDeposit : Item
    {
        public override string ItemId => "drainage_clay";
        public override string DisplayName => "Clay Deposit";
        public override string Description => "Sticky clay accumulated in the drainage channel";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some fine <silt> left by receding water"), KeywordInContext.Parse("a grey <mineral> clay from the channel bed") };
    }
    
    public sealed class StagnantWater : Item
    {
        public override string ItemId => "drainage_hollow_stagnant_water";
        public override string DisplayName => "Stagnant Water";
        public override string Description => "Still water pooled in the hollow";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a brown <murk> settling below the surface"), KeywordInContext.Parse("a still shallow <pool> trapped in the hollow") };
    }
}
