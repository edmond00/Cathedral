using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class IceBlockFieldLowerNode : PyramidalFeatureNode
{
    public override int MinAltitude => 6;
    public override int MaxAltitude => 9;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(IceBlockFieldUpperNode);
    
    public override string NodeId => "lower_ice_block_field";
    public override string ContextDescription => "standing in the lower ice block field";
    public override string TransitionDescription => "descend through the lower ice blocks";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a clear sheet of <ice> underfoot"), KeywordInContext.Parse("a fractured ice <block> tipped at an angle"), KeywordInContext.Parse("the icy <field> of scattered seracs"), KeywordInContext.Parse("the confusing <maze> of lower ice blocks") };
    
    private static readonly string[] Moods = { "scattered", "fractured", "labyrinthine", "icy" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} lower ice blocks";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing in a {mood} lower ice block field";
    }
    
    public override List<Item> GetItems() => new() { new SmallIceBlock(), new MorainePebbles() };

    public sealed class SmallIceBlock : Item
    {
        public override string ItemId => "ice_block_field_lower_small_ice_block";
        public override string DisplayName => "Small Ice Block";
        public override string Description => "Smaller fractured ice block";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a granular <firn> layer below the ice block"), KeywordInContext.Parse("a toppling ice <serac> in the lower field"), KeywordInContext.Parse("a manageable <chunk> of fractured glacier ice") };
    }
    
    public sealed class MorainePebbles : Item
    {
        public override string ItemId => "ice_block_field_lower_moraine_pebbles";
        public override string DisplayName => "Moraine Pebbles";
        public override string Description => "Glacial deposit pebbles collectible from the moraine";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a rough glacial <clast> in the moraine"), KeywordInContext.Parse("the unsorted <till> at the glacier base"), KeywordInContext.Parse("the legacy of the retreating <glacier>") };
    }
}
