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
    
    public override List<string> NodeKeywords => new() { "cavity", "dark", "dry", "shelter", "entrance", "hole", "bark", "interior", "hollow", "opening" };
    
    private static readonly string[] Moods = { "dark", "mysterious", "dry", "sheltered", "inviting", "hidden", "cozy", "shadowy" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} tree hollow";
    }
    
    public sealed class DriedLeaves : Item
    {
        public override string ItemId => "hollow_dried_leaves";
        public override string DisplayName => "Dried Leaves";
        public override string Description => "Crispy dried leaves collected from the hollow";
        public override List<string> OutcomeKeywords => new() { "dry", "crispy", "brown", "crumbly", "rustling", "dead", "papery", "brittle", "leaves", "tinder" };
    }
    
    public sealed class BatGuano : Item
    {
        public override string ItemId => "tree_hollow_bat_guano";
        public override string DisplayName => "Bat Guano";
        public override string Description => "Small deposits of bat droppings in the hollow";
        public override List<string> OutcomeKeywords => new() { "dark", "pellets", "droppings", "fertilizer", "guano", "pungent", "nitrogen", "rich", "accumulated", "small" };
    }
    
    public sealed class SquirrelNest : Item
    {
        public override string ItemId => "tree_hollow_squirrel_nest";
        public override string DisplayName => "Squirrel Nest Material";
        public override string Description => "Shredded bark and leaves forming a drey inside the hollow";
        public override List<string> OutcomeKeywords => new() { "shredded", "bark", "leaves", "soft", "nest", "dry", "lined", "bedding", "cozy", "fibrous" };
    }
}
