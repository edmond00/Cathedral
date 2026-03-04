namespace Cathedral.Game.Narrative;

/// <summary>
/// Anamnesis stat. Based on the anamnesis organ score.
/// Determines the number of Residual Memory slots (the FIFO forgetting queue).
/// </summary>
public class AnamnesisStat : DerivedStat
{
    public override string Name => "anamnesis_capacity";
    public override string DisplayName => "Anamnesis";
    public override string? RelatedOrganId => "anamnesis";

    /// <summary>Slot count = organ score × 2 (range 2-20).</summary>
    public override int CalculateValue(int sourceScore) => sourceScore * 2;
}
