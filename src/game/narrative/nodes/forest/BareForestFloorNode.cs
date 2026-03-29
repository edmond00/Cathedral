using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Bare Forest Floor - Lightless ground with no vegetation.
/// Associated with: Blackwood
/// </summary>
public class BareForestFloorNode : NarrationNode
{
    public override string NodeId => "bare_forest_floor";
    public override string ContextDescription => "walking on bare ground";
    public override string TransitionDescription => "step onto bare floor";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the dark bare <soil> underfoot"), KeywordInContext.Parse("a terrible <bareness> stretching in all directions"), KeywordInContext.Parse("an oppressive <stillness> in the lightless air") };
    
    private static readonly string[] Moods = { "bare", "lifeless", "empty", "barren", "devoid", "sterile", "dark", "desolate" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} bare forest floor";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"walking on a {mood} bare forest floor";
    }
    
    public override List<Item> GetItems() => new() { new BareSoil(), new CompactedEarth() };

    public sealed class BareSoil : Item
    {
        public override string ItemId => "bare_black_soil";
        public override string DisplayName => "Bare Black Soil";
        public override string Description => "Dark, sterile soil from the lightless floor";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some dark crumbling <loam> in the hand"), KeywordInContext.Parse("the cold sterile <earth> underfoot"), KeywordInContext.Parse("a grim <sterility> in the black soil") };
    }
    
    public sealed class CompactedEarth : Item
    {
        public override string ItemId => "bare_floor_compacted_earth";
        public override string DisplayName => "Compacted Earth";
        public override string Description => "Hard-packed earth untouched by growth";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some compacted <humus> pressed into hard layers"), KeywordInContext.Parse("a solid <density> to the earth underfoot"), KeywordInContext.Parse("this flat grey <surface> untouched by growth") };
    }
}
