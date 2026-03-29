using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class SnowCorniceFallLineNode : PyramidalFeatureNode
{
    public override int MinAltitude => 0;
    public override int MaxAltitude => 2;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(SnowCorniceCrestNode);
    
    public override string NodeId => "snow_cornice_fall_line";
    public override string ContextDescription => "standing on the cornice fall line";
    public override string TransitionDescription => "move to the cornice fall line";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the vertical <fall> line from the cornice above"), KeywordInContext.Parse("the path of a past <avalanche> below"), KeywordInContext.Parse("the steep <descent> of the cornice fall line"), KeywordInContext.Parse("the unbroken <slope> beneath the cornice") };
    
    private static readonly string[] Moods = { "steep", "treacherous", "angled", "exposed" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} cornice fall line";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing on a {mood} cornice fall line";
    }
    
    public override List<Item> GetItems() => new() { new CorniceDebris() };

    public sealed class CorniceDebris : Item
    {
        public override string ItemId => "cornice_debris";
        public override string DisplayName => "Cornice Debris";
        public override string Description => "Debris from cornice collapse";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the mixed <detritus> of a past cornice fall"), KeywordInContext.Parse("the collapsed <overhang> of the broken cornice"), KeywordInContext.Parse("a packed <snow> block from the fallen cornice") };
    }
    
}
