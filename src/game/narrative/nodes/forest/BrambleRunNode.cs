using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Bramble Run - A thorny corridor of blackberry brambles.
/// </summary>
public class BrambleRunNode : NarrationNode
{
    public override string NodeId => "bramble_run";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new BoarArchetype(), spawnChance: 0.50f),
    };
    public override string ContextDescription => "carefully navigating the brambles";
    public override string TransitionDescription => "push through the brambles";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a curved <thorn> snagging the sleeve"), KeywordInContext.Parse("some dark ripe <berry> clusters hanging low"), KeywordInContext.Parse("a tangled <bramble> cane arching across the path"), KeywordInContext.Parse("a long trailing <vine> underfoot") };
    
    private static readonly string[] Moods = { "thorny", "tangled", "scratching", "productive", "wild", "dense", "prickly", "guarded" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} bramble run";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"navigating a {mood} bramble run";
    }
    
    public sealed class Blackberry : Item
    {
        public override string ItemId => "wild_blackberry";
        public override string DisplayName => "Wild Blackberry";
        public override string Description => "A cluster of ripe blackberries";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a plump dark <berry> from the cane"), KeywordInContext.Parse("a juicy <drupe> staining the fingers blue"), KeywordInContext.Parse("a hanging <cluster> of ripe blackberries") };
    }
    
    public sealed class BrambleThorn : Item
    {
        public override string ItemId => "bramble_run_thorn";
        public override string DisplayName => "Bramble Thorn";
        public override string Description => "A sharp curved thorn from a bramble cane";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a recurved <barb> broken from the cane"), KeywordInContext.Parse("a stiff <spine> on the underside of the stem") };
    }
}
