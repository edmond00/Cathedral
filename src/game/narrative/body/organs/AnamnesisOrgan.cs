using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Anamnesis organ (brain). Single-part organ.
/// </summary>
public class AnamnesisOrgan : Organ
{
    public override string Id => "anamnesis";
    public override string DisplayName => "Anamnesis";
    public override string BodyPartId => "brain";
    
    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public AnamnesisOrgan()
    {
        _parts = new List<OrganPart> { new AnamnesisPart() };
    }
    
    public sealed class AnamnesisPart : OrganPart
    {
        public override string Id => "anamnesis";
        public override string DisplayName => "Anamnesis";
    }
}
