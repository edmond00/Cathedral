using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Factories;

/// <summary>
/// Factory for generating peak location graphs with pyramidal structure.
/// Snowy and icy landscape with 20 feature types across altitude 0-10.
/// </summary>
public class PeakGraphFactory : Narrative.Nodes.Mountain.PyramidalGraphFactory
{
    public PeakGraphFactory(string? sessionPath = null) : base(sessionPath)
    {
    }
    
    protected override List<Type> AllFeatureTypes => new()
    {
        // Summit Dome [0~1]
        typeof(Narrative.Nodes.Peak.SummitDomeCrestNode),
        typeof(Narrative.Nodes.Peak.SummitDomeShoulderNode),
        
        // Wind-Scoured Ridge [0~2]
        typeof(Narrative.Nodes.Peak.WindScouredRidgeCrestNode),
        typeof(Narrative.Nodes.Peak.WindScouredRidgeFlankedNode),
        
        // Snow Cornice [0~2]
        typeof(Narrative.Nodes.Peak.SnowCorniceCrestNode),
        typeof(Narrative.Nodes.Peak.SnowCorniceFallLineNode),
        
        // Ice-Crusted Ledge [1~3]
        typeof(Narrative.Nodes.Peak.IceCrustedLedgeUpperNode),
        typeof(Narrative.Nodes.Peak.IceCrustedLedgeLowerNode),
        
        // Frozen Ridge Face [1~3]
        typeof(Narrative.Nodes.Peak.FrozenRidgeFaceUpperNode),
        typeof(Narrative.Nodes.Peak.FrozenRidgeFaceLowerNode),
        
        // Ice Cliff [2~5]
        typeof(Narrative.Nodes.Peak.IceCliffTopNode),
        typeof(Narrative.Nodes.Peak.IceCliffBaseNode),
        
        // Hard Snow Slope [2~6]
        typeof(Narrative.Nodes.Peak.HardSnowSlopeUpperNode),
        typeof(Narrative.Nodes.Peak.HardSnowSlopeLowerNode),
        
        // Crevasse Field [2~6]
        typeof(Narrative.Nodes.Peak.CrevasseFieldEdgeNode),
        typeof(Narrative.Nodes.Peak.CrevasseFieldInteriorNode),
        
        // Wind-Packed Drift [3~6]
        typeof(Narrative.Nodes.Peak.WindPackedDriftCrestNode),
        typeof(Narrative.Nodes.Peak.WindPackedDriftHollowNode),
        
        // Frozen Ravine [3~8]
        typeof(Narrative.Nodes.Peak.FrozenRavineLipNode),
        typeof(Narrative.Nodes.Peak.FrozenRavineFloorNode),
        
        // Glacier Tongue [3~7]
        typeof(Narrative.Nodes.Peak.GlacierTongueUpperNode),
        typeof(Narrative.Nodes.Peak.GlacierTongueLowerNode),
        
        // Icy Gully [4~7]
        typeof(Narrative.Nodes.Peak.IcyGullyHeadNode),
        typeof(Narrative.Nodes.Peak.IcyGullyRunNode),
        
        // Frozen Waterfall [4~7]
        typeof(Narrative.Nodes.Peak.FrozenWaterfallLipNode),
        typeof(Narrative.Nodes.Peak.FrozenWaterfallBaseNode),
        
        // Snow Basin [5~8]
        typeof(Narrative.Nodes.Peak.SnowBasinRimNode),
        typeof(Narrative.Nodes.Peak.SnowBasinFloorNode),
        
        // Frozen Stream [5~9]
        typeof(Narrative.Nodes.Peak.FrozenStreamSourceNode),
        typeof(Narrative.Nodes.Peak.FrozenStreamChannelNode),
        
        // Ice Block Field [6~9]
        typeof(Narrative.Nodes.Peak.IceBlockFieldUpperNode),
        typeof(Narrative.Nodes.Peak.IceBlockFieldLowerNode),
        
        // Avalanche Path [6~10]
        typeof(Narrative.Nodes.Peak.AvalanchePathReleaseNode),
        typeof(Narrative.Nodes.Peak.AvalanchePathRunoutNode),
        
        // Slush Channel [7~10]
        typeof(Narrative.Nodes.Peak.SlushChannelHeadNode),
        typeof(Narrative.Nodes.Peak.SlushChannelSpreadNode),
        
        // Snow-Laden Valley [8~10]
        typeof(Narrative.Nodes.Peak.SnowLadenValleyUpperNode),
        typeof(Narrative.Nodes.Peak.SnowLadenValleyLowerNode),
        
        // Frozen Outwash Plain [9~10]
        typeof(Narrative.Nodes.Peak.FrozenOutwashPlainMarginNode),
        typeof(Narrative.Nodes.Peak.FrozenOutwashPlainFlatsNode)
    };
}
