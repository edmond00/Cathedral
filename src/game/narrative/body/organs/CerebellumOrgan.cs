using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Cerebellum organ (encephalon). Single-part organ.
/// </summary>
public class CerebellumOrgan : Organ
{
    public override string Id => "cerebellum";
    public override string DisplayName => "Cerebellum";
    public override string BodyPartId => "encephalon";
    public override string Description =>
        "The organ governing the co-ordination of practised movement, and the assimilation of new " +
        "dexterity under duress. It is the seat of the disciplines of precision and trained technique, " +
        "whether of the exacting crafts or of the studied forms of combat.";

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
