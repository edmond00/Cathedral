using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Deepwood - Level 13. Uniform corridor forest with deep silence.
/// </summary>
public class DeepwoodNode : NarrationNode
{
    public override string NodeId => "deepwood";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new BearArchetype(), spawnChance: 0.35f),
        new NpcEncounterSlot(new WolfArchetype(), spawnChance: 0.50f),
    };
    public override string ContextDescription => "walking through deepwood";
    public override string TransitionDescription => "enter the deepwood";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a profound <silence> pressing in on all sides"), KeywordInContext.Parse("some deep green <moss> on every stone"), KeywordInContext.Parse("the thick <litter> softening each footfall"), KeywordInContext.Parse("a long dim <corridor> of uniform trunks ahead") };
    
    private static readonly string[] Moods = { "silent", "still", "hushed", "quiet", "somber", "serene", "remote", "isolated" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} deepwood";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"walking through a {mood} deepwood";
    }
    
    public sealed class DarkLoam : Item
    {
        public override string ItemId => "deepwood_dark_loam";
        public override string DisplayName => "Dark Loam";
        public override string Description => "Rich black soil from the deepwood floor";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some black <humus> from the deepwood floor"), KeywordInContext.Parse("a rich dark <soil> teeming with life below") };
    }
}
