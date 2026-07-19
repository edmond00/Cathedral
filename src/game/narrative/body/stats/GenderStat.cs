namespace Cathedral.Game.Narrative;

public class GenderStat : DerivedStat
{
    public override string Name         => "gender";
    public override string DisplayName  => "Gender";
    public override string ShortDisplayName => "Gender";
    public override string? RelatedOrganId => "genitories";

    // Fallback when no biological sex was stamped on the member (see the member-aware override below):
    // derive maleness from the genitories organ score, as before.
    protected override int CalculateValue(int sourceScore) => sourceScore > 0 ? 1 : 0;

    /// <summary>
    /// Prefer the member's stamped <see cref="PartyMember.BiologicalSexMale"/> (set at NPC creation
    /// from a seeded flip) so gender actually varies; fall back to the genitories organ score when it
    /// is unset. Reading the flag rather than the organ score keeps sex decoupled from HP and
    /// genitories-based skills.
    /// </summary>
    protected override int CalculateValue(PartyMember member, int sourceScore)
        => member.BiologicalSexMale is bool male
            ? (male ? 1 : 0)
            : CalculateValue(sourceScore);

    public override string FormatValue(int value) => value > 0 ? "male" : "female";
}
