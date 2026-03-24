using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

/// <summary>
/// Base class for mountain/peak feature nodes with altitude range.
/// Features are organized in a pyramidal structure by altitude levels.
/// </summary>
public abstract class PyramidalFeatureNode : NarrationNode
{
    /// <summary>
    /// Minimum altitude for this feature (0-10).
    /// </summary>
    public abstract int MinAltitude { get; }
    
    /// <summary>
    /// Maximum altitude for this feature (0-10).
    /// </summary>
    public abstract int MaxAltitude { get; }
    
    /// <summary>
    /// Whether this is the "bottom" or "in" entrance to the feature.
    /// Bottom/In nodes connect to Top/Out nodes of the same feature.
    /// </summary>
    public abstract bool IsBottomNode { get; }
    
    /// <summary>
    /// The paired node type (Top connects to Bottom, Bottom connects to Top).
    /// </summary>
    public abstract Type PairedNodeType { get; }
    
    /// <summary>
    /// Check if this feature can exist at a specific altitude.
    /// </summary>
    public bool CanExistAtAltitude(int altitude)
    {
        return altitude >= MinAltitude && altitude <= MaxAltitude;
    }
}
