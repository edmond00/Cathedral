using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Earthworm Mound - Rich soil with abundant earthworms.
/// </summary>
public class EarthwormMoundNode : NarrationNode
{
    public override string NodeId => "earthworm_mound";
    public override string ContextDescription => "digging in the earthworm mound";
    public override string TransitionDescription => "dig in the rich soil";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "rich", "dark", "moist", "wriggling", "castings", "soil", "fertile", "loamy", "worms", "tunnels" };
    
    private static readonly string[] Moods = { "rich", "fertile", "moist", "active", "living", "productive", "dark", "loamy" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} earthworm mound";
    }
    
    public sealed class RichSoil : Item
    {
        public override string ItemId => "earthworm_rich_soil";
        public override string DisplayName => "Rich Soil";
        public override string Description => "Dark, fertile soil enriched by earthworms";
        public override List<string> OutcomeKeywords => new() { "dark", "rich", "moist", "fertile", "crumbly", "loamy", "soil", "nutrient", "earthen", "organic" };
    }
    
    public sealed class WormCasting : Item
    {
        public override string ItemId => "earthworm_mound_casting";
        public override string DisplayName => "Worm Casting";
        public override string Description => "Small mounds of processed soil left by earthworms";
        public override List<string> OutcomeKeywords => new() { "coiled", "granular", "processed", "nutrient", "pellets", "mound", "dark", "rich", "digested", "organic" };
    }
}
