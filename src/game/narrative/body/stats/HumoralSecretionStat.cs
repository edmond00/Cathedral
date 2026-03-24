namespace Cathedral.Game.Narrative;

/// <summary>
/// Base class for humoral organ secretion-percentage stats.
/// Formats values as percentages and strips the organ name prefix from the display name
/// (e.g. "Hepar Blood %" → "Blood %").
/// </summary>
public abstract class HumoralSecretionStat : DerivedStat
{
    /// <inheritdoc/>
    public override string FormatValue(int value) => $"{value}%";

    /// <inheritdoc/>
    public override string ShortDisplayName
    {
        get
        {
            string name = DisplayName;
            int spaceIdx = name.IndexOf(' ');
            return spaceIdx >= 0 ? name[(spaceIdx + 1)..] : name;
        }
    }
}
