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
    
    public override List<string> NodeKeywords => new() { "bare", "empty", "lifeless", "dark", "soil", "exposed", "barren", "nothing", "devoid", "sterile" };
    
    private static readonly string[] Moods = { "bare", "lifeless", "empty", "barren", "devoid", "sterile", "dark", "desolate" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} bare forest floor";
    }
    
    public sealed class BareSoil : Item
    {
        public override string ItemId => "bare_black_soil";
        public override string DisplayName => "Bare Black Soil";
        public override string Description => "Dark, sterile soil from the lightless floor";
        public override List<string> OutcomeKeywords => new() { "dark", "black", "sterile", "soil", "bare", "lifeless", "cold", "hard", "compacted", "dead" };
    }
}
