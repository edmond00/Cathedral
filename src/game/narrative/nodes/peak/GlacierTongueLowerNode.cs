using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class GlacierTongueLowerNode : PyramidalFeatureNode
{
    public override int MinAltitude => 3;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(GlacierTongueUpperNode);
    
    public override string NodeId => "lower_glacier_tongue";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new WolfArchetype(), spawnChance: 0.50f),
    };
    public override string ContextDescription => "standing on the lower ice flow";
    public override string TransitionDescription => "descend to the lower ice flow";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the retreating <glacier> tongue above"), KeywordInContext.Parse("the dirty <terminus> of the ice flow"), KeywordInContext.Parse("a clear sheet of <ice> cracking underfoot"), KeywordInContext.Parse("the slow <melting> of the glacier edge") };
    
    private static readonly string[] Moods = { "terminating", "melting", "ending", "transitional" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} lower ice flow";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing on a {mood} lower ice flow";
    }
    
    public override List<Item> GetItems() => new() { new MoraineDirt(), new GlacialErratic() };

    public sealed class MoraineDirt : Item
    {
        public override string ItemId => "glacier_tongue_lower_moraine_dirt";
        public override string DisplayName => "Moraine Dirt";
        public override string Description => "Dirt and rock from glacier edge";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the grey unsorted <till> at the glacier edge"), KeywordInContext.Parse("a dark <loam> mixed into the moraine"), KeywordInContext.Parse("the mixed <debris> of the glacial moraine") };
    }
    
    public sealed class GlacialErratic : Item
    {
        public override string ItemId => "glacier_tongue_lower_glacial_erratic";
        public override string DisplayName => "Glacial Erratic";
        public override string Description => "Glacier-deposited boulder fragment collectible from ice edge";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a far-travelled <dropstone> at the ice edge"), KeywordInContext.Parse("a glacier-deposited <boulder> in the moraine"), KeywordInContext.Parse("the legacy of the ancient <glacier> above") };
    }
}
