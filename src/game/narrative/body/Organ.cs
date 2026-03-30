using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Base class for organs. An organ belongs to one body part and contains one or more organ parts.
/// For single-part organs, the organ part has the same id as the organ.
/// Score is the sum of its organ part scores.
/// </summary>
public abstract class Organ
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public abstract string BodyPartId { get; }
    public abstract List<OrganPart> Parts { get; }
    
    /// <summary>
    /// When true, each organ part of this organ appears as a candidate target for wildcard
    /// wounds in the narrative failure outcome critic tree.
    /// </summary>
    public virtual bool AcceptsWildcardWounds => false;

    /// <summary>
    /// Score is the sum of all organ part scores.
    /// </summary>
    public int Score => Parts.Sum(p => p.Score);
}
