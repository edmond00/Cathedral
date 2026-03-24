using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Factories;

/// <summary>
/// Factory for generating mountain location graphs with pyramidal structure.
/// Temperate rocky landscape with 20 feature types across altitude 0-10.
/// </summary>
public class MountainGraphFactory : Narrative.Nodes.Mountain.PyramidalGraphFactory
{
    public MountainGraphFactory(string? sessionPath = null) : base(sessionPath)
    {
    }
    
    protected override List<Type> AllFeatureTypes => new()
    {
        // Summit Crest [0~1]
        typeof(Narrative.Nodes.Mountain.CrestRidgeNode),
        typeof(Narrative.Nodes.Mountain.CrestShoulderNode),
        
        // Exposed Ridge [0~3]
        typeof(Narrative.Nodes.Mountain.RidgeSpineNode),
        typeof(Narrative.Nodes.Mountain.RidgeFlankNode),
        
        // High Stone Ledge [1~3]
        typeof(Narrative.Nodes.Mountain.UpperLedgeNode),
        typeof(Narrative.Nodes.Mountain.LowerLedgeNode),
        
        // Cliff Face [2~6]
        typeof(Narrative.Nodes.Mountain.CliffTopNode),
        typeof(Narrative.Nodes.Mountain.CliffBaseNode),
        
        // Rock Buttress [2~5]
        typeof(Narrative.Nodes.Mountain.ButtressHeadNode),
        typeof(Narrative.Nodes.Mountain.ButtressFootNode),
        
        // Wind-Cut Slope [2~6]
        typeof(Narrative.Nodes.Mountain.WindCutUpperSlopeNode),
        typeof(Narrative.Nodes.Mountain.WindCutLowerSlopeNode),
        
        // Scree Slope [3~7]
        typeof(Narrative.Nodes.Mountain.UpperScreeNode),
        typeof(Narrative.Nodes.Mountain.LowerScreeNode),
        
        // Collapsed Rockfall [3~7]
        typeof(Narrative.Nodes.Mountain.RockfallCrownNode),
        typeof(Narrative.Nodes.Mountain.DebrisFieldNode),
        
        // Narrow Ravine [4~8]
        typeof(Narrative.Nodes.Mountain.RavineRimNode),
        typeof(Narrative.Nodes.Mountain.RavineFloorNode),
        
        // Boulder Field [4~8]
        typeof(Narrative.Nodes.Mountain.UpperBoulderSpreadNode),
        typeof(Narrative.Nodes.Mountain.LowerBoulderSpreadNode),
        
        // Stone Step Terrace [4~7]
        typeof(Narrative.Nodes.Mountain.UpperStepNode),
        typeof(Narrative.Nodes.Mountain.LowerStepNode),
        
        // Mountain Torrent [5~9]
        typeof(Narrative.Nodes.Mountain.TorrentSourceNode),
        typeof(Narrative.Nodes.Mountain.TorrentChannelNode),
        
        // River Cut [5~9]
        typeof(Narrative.Nodes.Mountain.RiverBankNode),
        typeof(Narrative.Nodes.Mountain.RiverbedNode),
        
        // Shaded Gully [5~8]
        typeof(Narrative.Nodes.Mountain.GullyLipNode),
        typeof(Narrative.Nodes.Mountain.GullyBottomNode),
        
        // Rock Arch [5~7]
        typeof(Narrative.Nodes.Mountain.ArchCrestNode),
        typeof(Narrative.Nodes.Mountain.ArchPassageNode),
        
        // Lower Cliff Wall [6~9]
        typeof(Narrative.Nodes.Mountain.WallTopNode),
        typeof(Narrative.Nodes.Mountain.WallBaseNode),
        
        // Alluvial Fan [7~10]
        typeof(Narrative.Nodes.Mountain.FanApexNode),
        typeof(Narrative.Nodes.Mountain.FanSpreadNode),
        
        // Wide Valley Floor [8~10]
        typeof(Narrative.Nodes.Mountain.ValleyUpperFloorNode),
        typeof(Narrative.Nodes.Mountain.ValleyLowerFloorNode),
        
        // Floodplain Channel [8~10]
        typeof(Narrative.Nodes.Mountain.ChannelBankNode),
        typeof(Narrative.Nodes.Mountain.ChannelBedNode),
        
        // Foothill Rise [9~10]
        typeof(Narrative.Nodes.Mountain.FoothillUpperRiseNode),
        typeof(Narrative.Nodes.Mountain.FoothillLowerSlopeNode)
    };
}
