using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Base class for derived stats. A derived stat is computed from either one organ's score
/// or one body part's score. Each subclass implements its own conversion formula.
/// </summary>
public abstract class DerivedStat
{
    public abstract string Name { get; }
    public abstract string DisplayName { get; }
    
    /// <summary>
    /// The organ id this stat derives from (null if derived from a body part instead).
    /// </summary>
    public virtual string? RelatedOrganId => null;
    
    /// <summary>
    /// The body part id this stat derives from (null if derived from an organ instead).
    /// </summary>
    public virtual string? RelatedBodyPartId => null;
    
    /// <summary>
    /// Get the source score from a party member's organs or body parts.
    /// </summary>
    public int GetSourceScore(PartyMember protagonist)
    {
        if (RelatedOrganId != null)
        {
            var organ = protagonist.GetOrganById(RelatedOrganId);
            return organ?.Score ?? 0;
        }
        if (RelatedBodyPartId != null)
        {
            var bodyPart = protagonist.GetBodyPartById(RelatedBodyPartId);
            return bodyPart?.Score ?? 0;
        }
        return 0;
    }
    
    /// <summary>
    /// Convert the source score into the derived stat value.
    /// Each subclass implements its own formula.
    /// </summary>
    public abstract int CalculateValue(int sourceScore);
    
    /// <summary>
    /// Get the final computed value of this derived stat for the given protagonist.
    /// </summary>
    public int GetValue(Protagonist protagonist) => CalculateValue(GetSourceScore(protagonist));
}
