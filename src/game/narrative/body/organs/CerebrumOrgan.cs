using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Cerebrum organ (encephalon). Single-part organ.
/// </summary>
public class CerebrumOrgan : Organ
{
    public override string Id => "cerebrum";
    public override string DisplayName => "Cerebrum";
    public override string BodyPartId => "encephalon";
    public override string Description =>
        "The organ of the understanding, in which perception is reasoned upon and knowledge retained. " +
        "It is the seat of the reasoning disciplines: the sciences and letters, the reckoning of number " +
        "and structure, and the schemes of the deliberate mind.";

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
