using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Cerebrum organ (brain). Single-part organ.
/// </summary>
public class CerebrumOrgan : Organ
{
    public override string Id => "cerebrum";
    public override string DisplayName => "Cerebrum";
    public override string BodyPartId => "brain";
    
    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public CerebrumOrgan()
    {
        _parts = new List<OrganPart> { new CerebrumPart() };
    }
    
    public sealed class CerebrumPart : OrganPart
    {
        public override string Id => "cerebrum";
        public override string DisplayName => "Cerebrum";
    }
}
