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
    }
}
