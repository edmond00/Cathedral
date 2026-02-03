using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Animal Track - A transversal feature following wildlife paths.
/// </summary>
public class AnimalTrackNode : NarrationNode
{
    public override string NodeId => "animal_track";
    public override string ContextDescription => "following the animal track";
    public override string TransitionDescription => "follow the track";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "trampled", "prints", "trail", "wildlife", "narrow", "worn", "path", "scent", "droppings", "marks" };
    
    private static readonly string[] Moods = { "well-worn", "faint", "fresh", "winding", "meandering", "hidden", "obvious", "subtle" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} animal track";
    }
    
    public sealed class AnimalDroppings : Item
    {
        public override string ItemId => "animal_droppings";
        public override string DisplayName => "Animal Droppings";
        public override string Description => "Fresh droppings indicating recent animal passage";
        public override List<string> OutcomeKeywords => new() { "fresh", "pungent", "dark", "pellets", "scattered", "recent", "organic", "earthy", "wildlife", "sign" };
    }
    
    public sealed class TuftOfFur : Item
    {
        public override string ItemId => "animal_track_tuft_of_fur";
        public override string DisplayName => "Tuft of Fur";
        public override string Description => "Animal fur snagged on nearby brush";
        public override List<string> OutcomeKeywords => new() { "fur", "tuft", "soft", "brown", "snagged", "coarse", "animal", "fibers", "caught", "wildlife" };
    }
}
