using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes;

/// <summary>
/// A caught or dead trout - the origin of trout parts (meat, bones, scales).
/// </summary>
public class CaughtTroutNode : NarrationNode
{
    public override string NodeId => "caught_trout";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "fish", "scales", "spotted", "silver", "fins", "sleek", "fresh", "dead", "catch", "body" };
    
    public override List<OutcomeBase> PossibleOutcomes => new()
    {
        new StreamNode()
    };
    
    private static readonly string[] Moods = { "fresh", "large", "small", "silvery", "spotted", "beautiful", "prized", "glistening" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} caught trout";
    }
    
    /// <summary>
    /// Trout meat - can only come from a caught trout.
    /// </summary>
    public sealed class TroutMeat : Item
    {
        public override string ItemId => "trout_meat";
        public override string DisplayName => "Trout Meat";
        public override string Description => "Fresh pink meat from a trout. Tender and flavorful, perfect for cooking.";
        public override List<string> OutcomeKeywords => new() { "meat", "flesh", "pink", "tender", "fresh", "fillet", "raw", "protein", "soft", "moist" };
    }
    
    /// <summary>
    /// Trout scales - can only come from a caught trout.
    /// </summary>
    public sealed class TroutScales : Item
    {
        public override string ItemId => "trout_scales";
        public override string DisplayName => "Trout Scales";
        public override string Description => "Silvery scales from a trout. Shimmering and iridescent, they could be used for decoration.";
        public override List<string> OutcomeKeywords => new() { "scales", "silver", "shimmering", "iridescent", "shiny", "spotted", "thin", "reflective", "translucent", "glittering" };
    }
    
    /// <summary>
    /// Trout bones - can only come from a caught trout.
    /// </summary>
    public sealed class TroutBones : Item
    {
        public override string ItemId => "trout_bones";
        public override string DisplayName => "Trout Bones";
        public override string Description => "Small, delicate bones from a trout. Could be useful for crafting tools or needles.";
        public override List<string> OutcomeKeywords => new() { "bones", "white", "small", "delicate", "thin", "sharp", "spine", "skeleton", "fragile", "needle" };
    }
}
