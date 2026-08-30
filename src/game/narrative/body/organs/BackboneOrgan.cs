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
    public override string Description =>
        "The axial column upon which the frame is articulated; upon its soundness depend the body's " +
        "vigour, endurance, and steadiness under burden. It is the seat of the disciplines of sustained " +
        "labour and bearing: of load and haulage, and of patient endurance.";

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
        public override int DefaultMaxScore => 4;
    }
}
