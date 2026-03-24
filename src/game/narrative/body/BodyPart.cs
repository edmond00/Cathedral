using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Base class for body parts (major body regions).
/// A body part contains multiple organs. Its score is the sum of its organ scores.
/// Body parts: encephalon, visage, trunk, upper_limbs, lower_limbs
/// </summary>
public abstract class BodyPart
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public abstract List<Organ> Organs { get; }
    
    /// <summary>
    /// Score is the sum of all organ scores within this body part.
    /// </summary>
    public int Score => Organs.Sum(o => o.Score);
}
