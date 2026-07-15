using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Fox NPC — skittish beast, non-hostile, flees when threatened.</summary>
public class FoxArchetype : NamedNpcArchetype
{
    // Foxes are short-lived: 1–5 years.
    public override int MinAgeDays => 1 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 5 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "fox";
    public override Species Species => SpeciesRegistry.Fox;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 4;

    public override string[] NamePool => new[]
    {
        "Red Fox", "Grey Fox", "Lean Fox", "Young Fox",
        "Old Fox", "Vixen", "Mangy Fox", "Sleek Fox"
    };

    public override string RoleNoun => "fox";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a russet fox freezes mid-step, watching you with cautious amber eyes",
        "a slim fox slips between the ferns, brush held low",
        "a sharp-nosed fox pauses on a log, ears swivelling toward you",
    };
}
