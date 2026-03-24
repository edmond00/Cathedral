using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Fox Den - An abandoned or active fox burrow.
/// </summary>
public class FoxDenNode : NarrationNode
{
    public override string NodeId => "fox_den";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new WolfArchetype(), spawnChance: 0.50f),
    };
    public override string ContextDescription => "observing the fox den";
    public override string TransitionDescription => "investigate the den";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "burrow", "entrance", "musky", "earth", "tunnel", "hole", "scent", "tracks", "den", "underground" };
    
    private static readonly string[] Moods = { "musky", "hidden", "occupied", "abandoned", "earthy", "secretive", "dark", "sheltered" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} fox den";
    }
    
    public sealed class FoxFur : Item
    {
        public override string ItemId => "fox_fur_tuft";
        public override string DisplayName => "Fox Fur Tuft";
        public override string Description => "A tuft of reddish fox fur caught on roots";
        public override List<string> OutcomeKeywords => new() { "red", "soft", "fluffy", "fur", "tuft", "reddish", "guard", "hairs", "russet", "warm" };
    }
    
    public sealed class BoneShard : Item
    {
        public override string ItemId => "fox_den_bone_shard";
        public override string DisplayName => "Bone Shard";
        public override string Description => "A gnawed bone fragment from a fox meal";
        public override List<string> OutcomeKeywords => new() { "white", "gnawed", "teeth", "marks", "hollow", "fragment", "prey", "remains", "picked", "clean" };
    }
    
    public sealed class FeatherRemains : Item
    {
        public override string ItemId => "fox_den_feather_remains";
        public override string DisplayName => "Scattered Feathers";
        public override string Description => "Bird feathers scattered near the den entrance from a kill";
        public override List<string> OutcomeKeywords => new() { "scattered", "prey", "bird", "feathers", "torn", "plucked", "brown", "white", "kill", "disheveled" };
    }
}
