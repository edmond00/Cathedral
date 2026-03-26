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
    
    public override List<string> NodeKeywords => new() { "ice", "block", "field", "maze" };
    
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
    
    public sealed class SmallIceBlock : Item
    {
        public override string ItemId => "ice_block_field_lower_small_ice_block";
        public override string DisplayName => "Small Ice Block";
        public override string Description => "Smaller fractured ice block";
        public override List<string> OutcomeKeywords => new() { "firn", "serac", "chunk" };
    }
    
    public sealed class MorainePebbles : Item
    {
        public override string ItemId => "ice_block_field_lower_moraine_pebbles";
        public override string DisplayName => "Moraine Pebbles";
        public override string Description => "Glacial deposit pebbles collectible from the moraine";
        public override List<string> OutcomeKeywords => new() { "clast", "till", "glacier" };
    }
}
