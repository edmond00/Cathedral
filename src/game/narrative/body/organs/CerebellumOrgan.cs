using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Cerebellum organ (brain). Single-part organ.
/// </summary>
public class CerebellumOrgan : Organ
{
    public override string Id => "cerebellum";
    public override string DisplayName => "Cerebellum";
    public override string BodyPartId => "brain";
    
    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public CerebellumOrgan()
    {
        _parts = new List<OrganPart> { new CerebellumPart() };
    }
    
    public sealed class CerebellumPart : OrganPart
    {
        public override string Id => "cerebellum";
        public override string DisplayName => "Cerebellum";
    }
}
