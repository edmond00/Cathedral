using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Fox NPC — skittish beast, non-hostile, flees when threatened.</summary>
public class FoxArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "fox";
    public override Species Species => SpeciesRegistry.Fox;
    public override bool DefaultHostile => false;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 4;

    public override string[] NamePool => new[]
    {
        "Red Fox", "Grey Fox", "Lean Fox", "Young Fox",
        "Old Fox", "Vixen", "Mangy Fox", "Sleek Fox"
    };

    protected override string BuildObservationHint(string name, string nodeContext)
        => $"a {name.ToLowerInvariant()} freezes mid-step, watching you with cautious amber eyes";
}
