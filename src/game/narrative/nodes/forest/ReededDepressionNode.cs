using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Reeded Depression - A wet hollow filled with reeds.
/// Associated with: Lowwood
/// </summary>
public class ReededDepressionNode : NarrationNode
{
    public override string NodeId => "reeded_depression";
    public override string ContextDescription => "wading through the reeded depression";
    public override string TransitionDescription => "descend into the reeds";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a tall hollow <reed> standing in shallow water"), KeywordInContext.Parse("a brown velvet <cattail> swaying in the breeze"), KeywordInContext.Parse("the muddy <marsh> ground underfoot"), KeywordInContext.Parse("the dark still <water> filling the hollow") };
    
    private static readonly string[] Moods = { "wet", "swaying", "marshy", "sodden", "reed-filled", "damp", "boggy", "hollow" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} reeded depression";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"wading through a {mood} reeded depression";
    }
    
    public override List<Item> GetItems() => new() { new ReedStem(), new CattailFluff() };

    public sealed class ReedStem : Item
    {
        public override string ItemId => "reed_stem";
        public override string DisplayName => "Reed Stem";
        public override string Description => "A tall, hollow reed stem";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a hollow jointed <culm> cut from the reed"), KeywordInContext.Parse("a light <internode> between the reed joints") };
    }
    
    public sealed class CattailFluff : Item
    {
        public override string ItemId => "reeded_depression_cattail_fluff";
        public override string DisplayName => "Cattail Fluff";
        public override string Description => "Soft brown seed fluff from a cattail head";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the soft white <down> from a ripe cattail head"), KeywordInContext.Parse("a tiny <seed> attached to a wisp of fluff") };
    }
}
