using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Spongy Moss Mat - Thick, waterlogged moss covering flooded ground.
/// Associated with: Mirewood
/// </summary>
public class SpongyMossMatNode : NarrationNode
{
    public override string NodeId => "spongy_moss_mat";
    public override string ContextDescription => "treading on spongy moss";
    public override string TransitionDescription => "step onto the moss mat";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "moss", "sponge", "water", "saturation" };
    
    private static readonly string[] Moods = { "spongy", "waterlogged", "squelching", "soft", "saturated", "yielding", "wet", "sodden" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} spongy moss mat";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"treading on a {mood} spongy moss mat";
    }
    
    public sealed class SphagnumMoss : Item
    {
        public override string ItemId => "wet_sphagnum_moss";
        public override string DisplayName => "Wet Sphagnum Moss";
        public override string Description => "Waterlogged moss from the mat";
        public override List<string> OutcomeKeywords => new() { "sphagnum", "moss", "water" };
    }
    
    public sealed class MossMatWater : Item
    {
        public override string ItemId => "spongy_moss_mat_bog_water";
        public override string DisplayName => "Bog Water";
        public override string Description => "Brown peaty water squeezed from moss";
        public override List<string> OutcomeKeywords => new() { "water", "tannin", "peat", "amber" };
    }
}
