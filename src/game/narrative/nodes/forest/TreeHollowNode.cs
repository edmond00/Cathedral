using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Tree Hollow - A cavity in a tree trunk offering shelter.
/// </summary>
public class TreeHollowNode : NarrationNode
{
    public override string NodeId => "tree_hollow";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new HermitArchetype(), spawnChance: 0.15f),
    };
    public override string ContextDescription => "peering into the tree hollow";
    public override string TransitionDescription => "investigate the hollow";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the dark open <hollow> in the trunk"), KeywordInContext.Parse("the wide <cavity> worn smooth inside"), KeywordInContext.Parse("the sense of <shelter> inside the hollow tree"), KeywordInContext.Parse("the rough encircling <bark> around the entrance") };
    
    private static readonly string[] Moods = { "dark", "mysterious", "dry", "sheltered", "inviting", "hidden", "cozy", "shadowy" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} tree hollow";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"peering into a {mood} tree hollow";
    }
    
    public sealed class DriedLeaves : Item
    {
        public override string ItemId => "hollow_dried_leaves";
        public override string DisplayName => "Dried Leaves";
        public override string Description => "Crispy dried leaves collected from the hollow";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some dry crisp <tinder> leaves in the hollow"), KeywordInContext.Parse("a preserved <dryness> inside the sheltered cavity") };
    }
    
    public sealed class BatGuano : Item
    {
        public override string ItemId => "tree_hollow_bat_guano";
        public override string DisplayName => "Bat Guano";
        public override string Description => "Small deposits of bat droppings in the hollow";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some dark <castings> left by roosting bats"), KeywordInContext.Parse("a small pile of bat <droppings> below the roost") };
    }
    
    public sealed class SquirrelNest : Item
    {
        public override string ItemId => "tree_hollow_squirrel_nest";
        public override string DisplayName => "Squirrel Nest Material";
        public override string Description => "Shredded bark and leaves forming a drey inside the hollow";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the compact <drey> built inside the hollow"), KeywordInContext.Parse("some soft <bedding> material from the squirrel nest") };
    }
}
