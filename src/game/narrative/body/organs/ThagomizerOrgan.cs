using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Thagomizer organ (trunk). Single-part organ unique to reptile anatomy: the armed end of the tail,
/// its spikes or its club. Lodged in the trunk because the tail is the backbone drawn out — the
/// beast's tail wound sits on the backbone for the same reason.
/// </summary>
public class ThagomizerOrgan : Organ
{
    public override string Id => "thagomizer";
    public override string DisplayName => "Thagomizer";
    public override string BodyPartId => "trunk";
    public override string Description =>
        "The armed extremity of the reptilian tail, set with spikes or knotted into a club and swung " +
        "from the whole length of the spine. The seat of the arts of the lash and of the guarded rear.";

    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;

    public ThagomizerOrgan()
    {
        _parts = new List<OrganPart> { new ThagomizerPart() };
    }

    public sealed class ThagomizerPart : OrganPart
    {
        public override string Id => "thagomizer";
        public override string DisplayName => "Thagomizer";
    }
}
