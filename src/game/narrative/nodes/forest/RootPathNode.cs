using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Root Path - A transversal feature of interwoven roots forming a natural pathway.
/// </summary>
public class RootPathNode : NarrationNode
{
    public override string NodeId => "root_path";
    public override string ContextDescription => "walking the root path";
    public override string TransitionDescription => "take the root path";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("an exposed <root> forming a natural step"), KeywordInContext.Parse("the interlaced <network> of roots underfoot"), KeywordInContext.Parse("the winding root <pathway> through the trees"), KeywordInContext.Parse("a dark twisted <gnarl> in the old root") };
    
    private static readonly string[] Moods = { "twisted", "winding", "serpentine", "tangled", "knotted", "interlaced", "convoluted", "meandering" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} root path";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"walking a {mood} root path";
    }
    
    public sealed class BarkChunk : Item
    {
        public override string ItemId => "root_bark_chunk";
        public override string DisplayName => "Root Bark Chunk";
        public override string Description => "A piece of rough bark from the exposed roots";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a spongy <cork> layer on the root surface"), KeywordInContext.Parse("a rough stringy <fiber> from the root bark") };
    }
    
    public sealed class RootSap : Item
    {
        public override string ItemId => "root_path_root_sap";
        public override string DisplayName => "Root Sap";
        public override string Description => "Sticky sap oozing from damaged roots";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a sticky <exudate> oozing from a damaged root"), KeywordInContext.Parse("a bead of dark <resin> sealing a root wound") };
    }
}
