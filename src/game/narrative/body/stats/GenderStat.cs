namespace Cathedral.Game.Narrative;

public class GenderStat : DerivedStat
{
    public override string Name         => "gender";
    public override string DisplayName  => "Gender";
    public override string ShortDisplayName => "Gender";
    public override string? RelatedOrganId => "genitories";
    public override int CalculateValue(int sourceScore) => sourceScore > 0 ? 1 : 0;
    public override int CalculateValueDisabled() => 0;
    public override string FormatValue(int value) => value > 0 ? "male" : "female";
}
