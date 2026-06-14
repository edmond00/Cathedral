namespace Cathedral.Game.Narrative;

/// <summary>
/// Nose organ — humor recognition when inspecting consumable items.
/// The nose detects the aromatic signature of humors in food, drink, and inhalants.
///
/// Source: Nose organ part score (0–3).
/// Formula: 0→0 recognized, 1→1 recognized, 2→3 recognized, 3→all (99) recognized.
/// </summary>
public class NoseRecognitionStat : DerivedStat
{
    public override string Name        => "nose_humor_recognition";
    public override string DisplayName => "Humor Recognition";
    public override string? RelatedOrganPartId => "nose";

    public override int CalculateValue(int sourceScore) => sourceScore switch
    {
        0 => 0,
        1 => 1,
        2 => 3,
        _ => 99,
    };
    public override string FormatValue(int value) => value >= 99 ? "all humors" : $"{value} humors";
}
