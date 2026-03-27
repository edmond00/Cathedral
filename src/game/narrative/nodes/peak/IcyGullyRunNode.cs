using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class IcyGullyRunNode : PyramidalFeatureNode
{
    public override int MinAltitude => 4;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(IcyGullyHeadNode);
    
    public override string NodeId => "icy_gully_run";
    public override string ContextDescription => "standing in the icy gully run";
    public override string TransitionDescription => "descend into the icy gully run";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the narrow icy <gully> cutting down the slope"), KeywordInContext.Parse("a clear sheet of <ice> lining the channel"), KeywordInContext.Parse("the carved ice <channel> of the gully run"), KeywordInContext.Parse("the steep <descent> of the frozen gully") };
    
    private static readonly string[] Moods = { "narrow", "flowing", "confined", "icy" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} gully run";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing in a {mood} icy gully run";
    }
    
    public sealed class GullyObsidian : Item
    {
        public override string ItemId => "icy_gully_run_gully_obsidian";
        public override string DisplayName => "Gully Obsidian";
        public override string Description => "Volcanic glass collectible from the gully";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a sharp <vitrophyre> fragment in the gully"), KeywordInContext.Parse("the black <glass> of the volcanic obsidian"), KeywordInContext.Parse("a <volcanic> glass shard from the gully wall") };
    }
}
