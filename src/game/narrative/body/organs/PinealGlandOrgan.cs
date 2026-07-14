using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Pineal Gland organ (encephalon). Single-part organ.
/// </summary>
public class PinealGlandOrgan : Organ
{
    public override string Id => "pineal_gland";
    public override string DisplayName => "Pineal Gland";
    public override string BodyPartId => "encephalon";
    public override string Description =>
        "The organ of consciousness and inward regard, held to be the seat of self-awareness and of the " +
        "general readiness by which any discipline is mastered. It is the seat of the contemplative and " +
        "intuitive arts: meditation, the judgement of beauty, and the presentiments of insight.";

    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public PinealGlandOrgan()
    {
        _parts = new List<OrganPart> { new PinealGlandPart() };
    }
    
    public sealed class PinealGlandPart : OrganPart
    {
        public override string Id => "pineal_gland";
        public override string DisplayName => "Pineal Gland";
        public override int DefaultMaxScore => 3;
    }
}
