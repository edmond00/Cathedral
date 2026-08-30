namespace Cathedral.Game.Narrative;

/// <summary>
/// Health Points — maximum HP, the pool of wounds the body can absorb before dying.
/// Source: trunk body part (aggregate score).
/// Formula: score (1 HP per trunk level).
///
/// Note: <see cref="PartyMember.MaxHp"/> routes through this stat's raw formula so the
/// menu display and the gameplay value share a single definition. Current HP is then
/// <c>MaxHp − wound count</c> (see <see cref="PartyMember.CurrentHp"/>), so wounds are
/// counted there rather than as a penalty on this stat.
/// </summary>
public class HealthPointStat : DerivedStat
{
    public override string Name         => "health_point";
    public override string DisplayName  => "Health Points";
    public override string? RelatedBodyPartId => "trunk";
    protected override int CalculateValue(int sourceScore) => Math.Max(1, (sourceScore + 3) / 3);
    public override int WorstValue => 1;
    public override string FormatValue(int value) => $"{value} HP";
}
