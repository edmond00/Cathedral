using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Anamnesis organ (encephalon). Single-part organ.
/// </summary>
public class AnamnesisOrgan : Organ
{
    public override string Id => "anamnesis";
    public override string DisplayName => "Anamnesis";
    public override string BodyPartId => "encephalon";
    public override string Description =>
        "The organ of recollection, governing the persistence and recall of what is learned. It is the " +
        "seat of the disciplines that rest upon a deep and durable store: accumulated lore, " +
        "long-remembered technique, and the recollections of a life.";

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
