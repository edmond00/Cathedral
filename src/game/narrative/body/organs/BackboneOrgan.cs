using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Backbone organ (trunk). Single-part organ.
/// </summary>
public class BackboneOrgan : Organ
{
    public override string Id => "backbone";
    public override string DisplayName => "Backbone";
    public override string BodyPartId => "trunk";
    
    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public BackboneOrgan()
    {
        _parts = new List<OrganPart> { new BackbonePart() };
    }
    
    public sealed class BackbonePart : OrganPart
    {
        public override string Id => "backbone";
        public override string DisplayName => "Backbone";
    }
}
